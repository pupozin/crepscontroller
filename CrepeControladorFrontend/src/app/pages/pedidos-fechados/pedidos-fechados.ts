import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { PedidoDetalhe, PedidoResumo, PedidoService } from '../../services/pedido.service';

type AbaFechados = 'finalizado' | 'cancelado';

@Component({
  selector: 'app-pedidos-fechados',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pedidos-fechados.html',
  styleUrls: ['./pedidos-fechados.scss']
})
export class PedidosFechados implements OnInit, OnDestroy {
  abaAtiva: AbaFechados = 'finalizado';
  pedidosFechados: PedidoResumo[] = [];
  pedidosFinalizados: PedidoResumo[] = [];
  pedidosCancelados: PedidoResumo[] = [];
  readonly pedidosPorPagina = 10;
  paginaAtual = 1;

  carregando = false;
  modalDetalhesAberto = false;
  pedidoSelecionado: PedidoDetalhe | null = null;
  carregandoDetalhes = false;

  private readonly subscriptions = new Subscription();
  private pedidoIdParaAbrir?: number;

  constructor(
    private readonly pedidoService: PedidoService,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.carregarPedidosFechados();

    this.subscriptions.add(
      this.pedidoService.atualizacoes$.subscribe(() => this.carregarPedidosFechados())
    );

    this.subscriptions.add(
      this.route.queryParamMap.subscribe((params) => {
        const pedidoId = Number(params.get('pedido'));
        if (pedidoId) {
          this.pedidoIdParaAbrir = pedidoId;
          this.abrirDetalhesPorId(pedidoId);
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  selecionarAba(aba: AbaFechados): void {
    if (this.abaAtiva === aba) {
      return;
    }
    this.abaAtiva = aba;
    this.paginaAtual = 1;
    this.ajustarPaginaAtual();
  }

  abrirDetalhes(pedido: PedidoResumo): void {
    if (!pedido?.id) {
      return;
    }
    this.abrirDetalhesPorId(pedido.id);
  }

  fecharModalDetalhes(): void {
    this.modalDetalhesAberto = false;
    this.pedidoSelecionado = null;
  }

  formatarData(data?: string | null): string {
    if (!data) {
      return '--';
    }
    return new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    }).format(new Date(data));
  }

  corStatus(status?: string): string {
    const chave = (status ?? '').toLowerCase();
    if (chave === 'cancelado') {
      return '#ef4444';
    }
    if (chave === 'finalizado') {
      return '#0ea5e9';
    }
    return '#facc15';
  }

  clienteOuPadrao(pedido: PedidoResumo): string {
    return pedido.cliente || 'Balcao';
  }

  get pedidosAtuais(): PedidoResumo[] {
    return this.abaAtiva === 'finalizado' ? this.pedidosFinalizados : this.pedidosCancelados;
  }

  get totalPaginas(): number {
    return Math.ceil(this.pedidosAtuais.length / this.pedidosPorPagina);
  }

  get pedidosVisiveis(): PedidoResumo[] {
    if (!this.pedidosAtuais.length) {
      return [];
    }
    const inicio = (this.paginaAtual - 1) * this.pedidosPorPagina;
    return this.pedidosAtuais.slice(inicio, inicio + this.pedidosPorPagina);
  }

  get paginasCarousel(): number[] {
    return Array.from({ length: this.totalPaginas }, (_, index) => index + 1);
  }

  irParaPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas) {
      return;
    }
    this.paginaAtual = pagina;
  }

  alterarPagina(delta: number): void {
    this.irParaPagina(this.paginaAtual + delta);
  }

  private carregarPedidosFechados(): void {
    this.carregando = true;
    this.pedidoService.listarPorGrupoStatus('FECHADOS').subscribe({
      next: (pedidos) => {
        this.pedidosFechados = pedidos;
        this.pedidosFinalizados = pedidos.filter(
          (pedido) => this.normalizarStatus(pedido.status) === 'finalizado'
        );
        this.pedidosCancelados = pedidos.filter(
          (pedido) => this.normalizarStatus(pedido.status) === 'cancelado'
        );
        this.ajustarPaginaAtual();
        this.carregando = false;
      },
      error: (err) => {
        console.error('Erro ao listar pedidos fechados', err);
        this.pedidosFechados = [];
        this.pedidosFinalizados = [];
        this.pedidosCancelados = [];
        this.ajustarPaginaAtual();
        this.carregando = false;
      }
    });
  }

  private abrirDetalhesPorId(pedidoId: number): void {
    if (!pedidoId) {
      return;
    }

    this.carregandoDetalhes = true;
    this.pedidoService.obterPedido(pedidoId).subscribe({
      next: (detalhe) => {
        this.pedidoSelecionado = detalhe;
        this.modalDetalhesAberto = true;
        this.carregandoDetalhes = false;
        this.pedidoIdParaAbrir = undefined;
      },
      error: (err) => {
        console.error('Erro ao carregar pedido', err);
        this.carregandoDetalhes = false;
      }
    });
  }

  private ajustarPaginaAtual(): void {
    if (!this.pedidosAtuais.length) {
      this.paginaAtual = 1;
      return;
    }
    const total = this.totalPaginas || 1;
    if (this.paginaAtual > total) {
      this.paginaAtual = total;
    }
    if (this.paginaAtual < 1) {
      this.paginaAtual = 1;
    }
  }

  private normalizarStatus(status?: string): string {
    return (status ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  }
}
