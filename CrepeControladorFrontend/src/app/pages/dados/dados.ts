import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, of } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import {
  DashboardDiaSemanaDistribuicao,
  DashboardDiaSemanaPico,
  DashboardHorarioPico,
  DashboardItemRanking,
  DashboardResumoPeriodo,
  DashboardTipoPedido,
  DadosService
} from '../../services/dados.service';

type PeriodicidadeFiltro = 'DIA' | 'SEMANA' | 'MES' | 'PERSONALIZADO' | 'TOTAL';

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

interface DiaSemanaPicoUI extends DashboardDiaSemanaPico {
  horaFormatada: string;
}

interface DiaSemanaDistribuicaoSerie {
  hora: number;
  horaFormatada: string;
  quantidadePedidos: number;
  percentual: number;
}

interface DiaSemanaDistribuicaoUI {
  diaSemana: number;
  nomeDia: string;
  series: DiaSemanaDistribuicaoSerie[];
}

interface PeriodoSelecionado {
  inicio: string;
  fim: string;
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
    { label: 'Total', valor: 'TOTAL' as PeriodicidadeFiltro },
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
  descricaoGraficoHoras = 'Pedidos e faturamento por hora';

  resumo?: DashboardResumoPeriodo;
  tipoPedidoDistribuicao: TipoPedidoDistribuicao[] = [];
  itensDestaque: DashboardItemRanking[] = [];
  horariosPico: HorarioPicoUI[] = [];
  tituloGrafico = '';
  periodoTotal?: PeriodoSelecionado;
  picosDiaSemana: DiaSemanaPicoUI[] = [];
  distribuicaoDiaSemana: DiaSemanaDistribuicaoUI[] = [];
  analiseDiaSemanaHabilitada = false;

  private readonly destruir$ = new Subject<void>();

  constructor(private readonly dadosService: DadosService) {}

  ngOnInit(): void {
    this.carregarPeriodoTotal();
    this.buscarDados();
  }

  private carregarPeriodoTotal(): void {
    this.dadosService
      .obterPeriodoTotal()
      .pipe(takeUntil(this.destruir$))
      .subscribe({
        next: (periodo) => {
          const inicio = this.normalizarDataIso(periodo?.dataInicio);
          const fim = this.normalizarDataIso(periodo?.dataFim);
          if (inicio && fim) {
            this.periodoTotal = { inicio, fim };
          }
        },
        error: (err) => {
          console.error('Erro ao carregar periodo total', err);
        }
      });
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

    const periodo = this.mapearFiltroParaPeriodo();
    if (!periodo) {
      return;
    }

    this.carregando = true;
    this.erro = undefined;
    this.tituloGrafico = this.obterTituloGrafico(this.filtro.periodicidade);
    this.descricaoGraficoHoras = this.obterDescricaoGrafico(this.filtro.periodicidade);

    const possuiJanelaSemanal = this.possuiMinimoDeDiasParaDiaSemana(periodo);
    const horarios$ = this.dadosService.obterHorariosPeriodo(periodo.inicio, periodo.fim);
    const picosSemana$ = possuiJanelaSemanal
      ? this.dadosService.obterPicosDiaSemana(periodo.inicio, periodo.fim)
      : of<DashboardDiaSemanaPico[]>([]);
    const distribuicaoSemana$ = possuiJanelaSemanal
      ? this.dadosService.obterDistribuicaoDiaSemana(periodo.inicio, periodo.fim)
      : of<DashboardDiaSemanaDistribuicao[]>([]);

    forkJoin({
      resumo: this.dadosService.obterResumo(periodo.inicio, periodo.fim),
      tipos: this.dadosService.obterTipoPedido(periodo.inicio, periodo.fim),
      itens: this.dadosService.obterItensRanking(periodo.inicio, periodo.fim),
      horarios: horarios$,
      picosSemana: picosSemana$,
      distribuicaoSemana: distribuicaoSemana$
    })
      .pipe(
        takeUntil(this.destruir$),
        finalize(() => {
          this.carregando = false;
        })
      )
      .subscribe({
        next: ({ resumo, tipos, itens, horarios, picosSemana, distribuicaoSemana }) => {
          this.resumo = resumo;
          this.tipoPedidoDistribuicao = this.processarDistribuicaoTipoPedido(tipos ?? []);
          this.itensDestaque = itens ?? [];
          this.horariosPico = this.mapearHorarios(horarios ?? []);
          this.analiseDiaSemanaHabilitada = possuiJanelaSemanal;
          const picosMapeados = possuiJanelaSemanal ? this.mapearPicosDiaSemana(picosSemana ?? []) : [];
          const distribuicaoMapeada = possuiJanelaSemanal
            ? this.mapearDistribuicaoDiaSemana(distribuicaoSemana ?? [], picosMapeados)
            : [];
          this.picosDiaSemana = picosMapeados;
          this.distribuicaoDiaSemana = distribuicaoMapeada;
        },
        error: (err) => {
          console.error('Erro ao carregar dados', err);
          this.erro = 'Nao foi possivel carregar os dados. Tente novamente.';
          this.resumo = undefined;
          this.tipoPedidoDistribuicao = [];
          this.itensDestaque = [];
          this.horariosPico = [];
          this.picosDiaSemana = [];
          this.distribuicaoDiaSemana = [];
          this.analiseDiaSemanaHabilitada = false;
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

    if (this.filtro.periodicidade === 'TOTAL' && !this.periodoTotal) {
      this.erroFiltro = 'Periodo total ainda nao disponivel.';
      return false;
    }

    return true;
  }

  private mapearFiltroParaPeriodo(): PeriodoSelecionado | undefined {
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

    if (this.filtro.periodicidade === 'TOTAL') {
      return this.periodoTotal;
    }

    if (this.filtro.intervaloInicio && this.filtro.intervaloFim) {
      return {
        inicio: this.filtro.intervaloInicio,
        fim: this.filtro.intervaloFim
      };
    }

    return undefined;
  }

  private obterTituloGrafico(periodicidade: PeriodicidadeFiltro): string {
    switch (periodicidade) {
      case 'DIA':
        return 'Horarios de pico do dia';
      case 'SEMANA':
        return 'Horarios de pico da semana';
      case 'MES':
        return 'Horarios de pico do mes';
      case 'TOTAL':
        return 'Horarios de pico - total';
      default:
        return 'Horarios de pico do periodo';
    }
  }

  private obterDescricaoGrafico(periodicidade: PeriodicidadeFiltro): string {
    if (periodicidade === 'DIA') {
      return 'Pedidos e faturamento hora a hora do dia escolhido.';
    }
    if (periodicidade === 'TOTAL') {
      return 'Pedidos e faturamento considerando todo o historico.';
    }
    return 'Pedidos e faturamento hora a hora no periodo selecionado.';
  }

  private possuiMinimoDeDiasParaDiaSemana(periodo: PeriodoSelecionado): boolean {
    return this.obterQuantidadeDias(periodo) >= 7;
  }

  private obterQuantidadeDias(periodo: PeriodoSelecionado): number {
    const inicio = new Date(periodo.inicio);
    const fim = new Date(periodo.fim);
    if (Number.isNaN(inicio.getTime()) || Number.isNaN(fim.getTime())) {
      return 0;
    }
    const diferencaMs = fim.getTime() - inicio.getTime();
    if (diferencaMs < 0) {
      return 0;
    }
    return Math.floor(diferencaMs / (1000 * 60 * 60 * 24)) + 1;
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

  private mapearPicosDiaSemana(dados: DashboardDiaSemanaPico[]): DiaSemanaPicoUI[] {
    return [...dados]
      .sort((a, b) => a.diaSemana - b.diaSemana)
      .map((item) => ({
        ...item,
        horaFormatada: `${item.hora.toString().padStart(2, '0')}h`
      }));
  }

  private mapearDistribuicaoDiaSemana(
    dados: DashboardDiaSemanaDistribuicao[],
    diasReferencia: DiaSemanaPicoUI[]
  ): DiaSemanaDistribuicaoUI[] {
    const agrupado = new Map<number, DiaSemanaDistribuicaoUI>();

    dados.forEach((item) => {
      const existente =
        agrupado.get(item.diaSemana) ??
        ({
          diaSemana: item.diaSemana,
          nomeDia: item.nomeDia,
          series: []
        } as DiaSemanaDistribuicaoUI);

      existente.series.push({
        hora: item.hora,
        horaFormatada: `${item.hora.toString().padStart(2, '0')}h`,
        quantidadePedidos: item.quantidadePedidos,
        percentual: 0
      });

      agrupado.set(item.diaSemana, existente);
    });

    diasReferencia.forEach((dia) => {
      if (!agrupado.has(dia.diaSemana)) {
        agrupado.set(dia.diaSemana, {
          diaSemana: dia.diaSemana,
          nomeDia: dia.nomeDia,
          series: []
        });
      }
    });

    const resultado = [...agrupado.values()].sort((a, b) => a.diaSemana - b.diaSemana);

    resultado.forEach((dia) => {
      const maximo = dia.series.reduce((maior, serie) => Math.max(maior, serie.quantidadePedidos), 0);
      dia.series = dia.series
        .sort((a, b) => a.hora - b.hora)
        .map((serie) => ({
          ...serie,
          percentual: maximo ? Math.round((serie.quantidadePedidos / maximo) * 100) : 0
        }));
    });

    return resultado;
  }

  private normalizarDataIso(valor?: string | null): string | undefined {
    if (!valor) {
      return undefined;
    }
    return valor.split('T')[0] ?? undefined;
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
