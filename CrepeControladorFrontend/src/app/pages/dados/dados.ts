import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, of } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import {
  DashboardHorarioPico,
  DashboardItemRanking,
  DashboardResumoPeriodo,
  DashboardTipoPedido,
  DadosService
} from '../../services/dados.service';

type PeriodicidadeFiltro = 'DIA' | 'SEMANA' | 'MES' | 'PERSONALIZADO';

interface FiltroDadosForm {
  periodicidade: PeriodicidadeFiltro;
  dia: string;
  semanaInicio: string;
  mes: string;
  intervaloInicio?: string;
  intervaloFim?: string;
}

interface TipoPedidoDistribuicao extends DashboardTipoPedido {
  percentual: number;
}

interface HorarioPicoUI extends DashboardHorarioPico {
  horaFormatada: string;
}

@Component({
  selector: 'app-dados',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dados.html',
  styleUrl: './dados.scss'
})
export class Dados implements OnInit, OnDestroy {
  readonly periodos = [
    { label: 'Dia', valor: 'DIA' as PeriodicidadeFiltro },
    { label: 'Semana', valor: 'SEMANA' as PeriodicidadeFiltro },
    { label: 'Mes', valor: 'MES' as PeriodicidadeFiltro },
    { label: 'Personalizado', valor: 'PERSONALIZADO' as PeriodicidadeFiltro }
  ];

  filtro: FiltroDadosForm = {
    periodicidade: 'DIA',
    dia: this.formatarData(new Date()),
    semanaInicio: this.calcularInicioSemana(new Date()),
    mes: this.formatarMes(new Date()),
    intervaloInicio: undefined,
    intervaloFim: undefined
  };

  carregando = false;
  erro?: string;
  erroFiltro?: string;

  resumo?: DashboardResumoPeriodo;
  tipoPedidoDistribuicao: TipoPedidoDistribuicao[] = [];
  itensDestaque: DashboardItemRanking[] = [];
  horariosPico: HorarioPicoUI[] = [];
  horariosDisponiveis = true;
  tituloGrafico = '';

  private readonly destruir$ = new Subject<void>();

  constructor(private readonly dadosService: DadosService) {}

  ngOnInit(): void {
    this.buscarDados();
  }

  ngOnDestroy(): void {
    this.destruir$.next();
    this.destruir$.complete();
  }

  alterarPeriodo(periodicidade: PeriodicidadeFiltro): void {
    this.filtro.periodicidade = periodicidade;
    this.erroFiltro = undefined;
    if (periodicidade === 'PERSONALIZADO') {
      const hoje = this.formatarData(new Date());
      this.filtro.intervaloInicio ??= hoje;
      this.filtro.intervaloFim ??= hoje;
    }
    this.buscarDados();
  }

  aplicarFiltros(): void {
    if (!this.validarFiltro()) {
      return;
    }
    this.buscarDados();
  }

  onCampoDataChange(): void {
    this.erroFiltro = undefined;
  }

  obterAlturaBarraPedidos(valor: number): number {
    if (!valor) {
      return 0;
    }
    const maximo = this.horariosPico.reduce((maior, item) => Math.max(maior, item.quantidadePedidos), 0);
    if (!maximo) {
      return 0;
    }
    return Math.max(5, Math.round((valor / maximo) * 100));
  }

  obterAlturaBarraFaturamento(valor: number): number {
    if (!valor) {
      return 0;
    }
    const maximo = this.horariosPico.reduce((maior, item) => Math.max(maior, Number(item.faturamento)), 0);
    if (!maximo) {
      return 0;
    }
    return Math.max(5, Math.round((Number(valor) / maximo) * 100));
  }

  private buscarDados(): void {
    if (!this.validarFiltro()) {
      return;
    }

    this.carregando = true;
    this.erro = undefined;
    const periodo = this.mapearFiltroParaPeriodo();
    const consultaHorarios = this.obterConsultaHorarios();

    forkJoin({
      resumo: this.dadosService.obterResumo(periodo.inicio, periodo.fim),
      tipos: this.dadosService.obterTipoPedido(periodo.inicio, periodo.fim),
      itens: this.dadosService.obterItensRanking(periodo.inicio, periodo.fim),
      horarios: consultaHorarios
    })
      .pipe(
        takeUntil(this.destruir$),
        finalize(() => {
          this.carregando = false;
        })
      )
      .subscribe({
        next: ({ resumo, tipos, itens, horarios }) => {
          this.resumo = resumo;
          this.tipoPedidoDistribuicao = this.processarDistribuicaoTipoPedido(tipos ?? []);
          this.itensDestaque = itens ?? [];
          this.horariosPico = this.mapearHorarios(horarios ?? []);
        },
        error: (err) => {
          console.error('Erro ao carregar dados', err);
          this.erro = 'Nao foi possivel carregar os dados. Tente novamente.';
          this.resumo = undefined;
          this.tipoPedidoDistribuicao = [];
          this.itensDestaque = [];
          this.horariosPico = [];
        }
      });
  }

  private validarFiltro(): boolean {
    this.erroFiltro = undefined;

    if (this.filtro.periodicidade === 'DIA' && !this.filtro.dia) {
      this.erroFiltro = 'Informe o dia desejado.';
      return false;
    }

    if (this.filtro.periodicidade === 'SEMANA' && !this.filtro.semanaInicio) {
      this.erroFiltro = 'Informe o inicio da semana.';
      return false;
    }

    if (this.filtro.periodicidade === 'MES' && !this.filtro.mes) {
      this.erroFiltro = 'Informe o mes desejado.';
      return false;
    }

    if (this.filtro.periodicidade === 'PERSONALIZADO') {
      if (!this.filtro.intervaloInicio || !this.filtro.intervaloFim) {
        this.erroFiltro = 'Informe o intervalo completo.';
        return false;
      }
      if (this.filtro.intervaloInicio > this.filtro.intervaloFim) {
        this.erroFiltro = 'Data inicial nao pode ser maior que a final.';
        return false;
      }
    }

    return true;
  }

  private mapearFiltroParaPeriodo(): { inicio: string; fim: string } {
    if (this.filtro.periodicidade === 'DIA') {
      return { inicio: this.filtro.dia, fim: this.filtro.dia };
    }

    if (this.filtro.periodicidade === 'SEMANA') {
      const inicio = this.filtro.semanaInicio;
      const fim = this.calcularDataFinalSemana(inicio);
      return { inicio, fim };
    }

    if (this.filtro.periodicidade === 'MES') {
      const inicio = this.converterMesParaData(this.filtro.mes) ?? this.formatarData(new Date());
      const fim = this.calcularFimMes(inicio);
      return { inicio, fim };
    }

    return {
      inicio: this.filtro.intervaloInicio!,
      fim: this.filtro.intervaloFim!
    };
  }

  private obterConsultaHorarios() {
    if (this.filtro.periodicidade === 'DIA') {
      const data = new Date(this.filtro.dia);
      const diaSemana = this.mapearDiaSemana(data);
      const ano = data.getFullYear();
      this.horariosDisponiveis = true;
      this.tituloGrafico = 'Horarios de pico do dia';
      return this.dadosService.obterHorariosDiaSemana(diaSemana, ano);
    }

    if (this.filtro.periodicidade === 'MES') {
      const [ano, mes] = this.filtro.mes.split('-').map((valor) => Number(valor));
      if (ano && mes) {
        this.horariosDisponiveis = true;
        this.tituloGrafico = 'Horarios de pico do mes';
        return this.dadosService.obterHorariosMes(ano, mes);
      }
    }

    this.horariosDisponiveis = false;
    this.tituloGrafico = 'Horarios de pico';
    return of<DashboardHorarioPico[]>([]);
  }

  private processarDistribuicaoTipoPedido(dados: DashboardTipoPedido[]): TipoPedidoDistribuicao[] {
    const total = dados.reduce((acc, item) => acc + item.qtdePedidos, 0);
    if (!total) {
      return dados.map((item) => ({ ...item, percentual: 0 }));
    }
    return dados.map((item) => ({
      ...item,
      percentual: Math.round((item.qtdePedidos / total) * 100)
    }));
  }

  private mapearHorarios(dados: DashboardHorarioPico[]): HorarioPicoUI[] {
    return dados.map((item) => ({
      ...item,
      horaFormatada: `${item.hora.toString().padStart(2, '0')}h`
    }));
  }

  private formatarData(data: Date): string {
    const ano = data.getFullYear();
    const mes = `${data.getMonth() + 1}`.padStart(2, '0');
    const dia = `${data.getDate()}`.padStart(2, '0');
    return `${ano}-${mes}-${dia}`;
  }

  private calcularInicioSemana(data: Date): string {
    const diaSemana = data.getDay(); // 0 domingo
    const diferenca = (diaSemana + 6) % 7;
    const inicio = new Date(data);
    inicio.setDate(data.getDate() - diferenca);
    return this.formatarData(inicio);
  }

  private calcularDataFinalSemana(inicio: string): string {
    if (!inicio) {
      return '';
    }
    const dataInicio = new Date(inicio);
    const fim = new Date(dataInicio);
    fim.setDate(dataInicio.getDate() + 6);
    return this.formatarData(fim);
  }

  private formatarMes(data: Date): string {
    const ano = data.getFullYear();
    const mes = `${data.getMonth() + 1}`.padStart(2, '0');
    return `${ano}-${mes}`;
  }

  private converterMesParaData(valor?: string): string | undefined {
    if (!valor) {
      return undefined;
    }
    const [ano, mes] = valor.split('-');
    if (!ano || !mes) {
      return undefined;
    }
    return `${ano}-${mes}-01`;
  }

  private calcularFimMes(inicio: string): string {
    const dataInicio = new Date(inicio);
    const fim = new Date(dataInicio);
    fim.setMonth(fim.getMonth() + 1);
    fim.setDate(0);
    return this.formatarData(fim);
  }

  private mapearDiaSemana(data: Date): number {
    const dia = data.getDay(); // 0 domingo
    return ((dia + 6) % 7) + 1; // transforma para 1=segunda ... 7=domingo
  }
}
