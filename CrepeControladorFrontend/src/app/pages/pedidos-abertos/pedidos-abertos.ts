import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import {
  PedidoDetalhe,
  PedidoItemSelecionavel,
  PedidoResumo,
  PedidoService,
  PedidoUpdatePayload
} from '../../services/pedido.service';

type AbaPedidos = 'resumo' | 'tipos';
type PedidoItemForm = { itemId: number | null; quantidade: number };

interface PedidoFormulario {
  cliente: string;
  tipoPedido: string;
  status: string;
  observacao: string;
  itens: PedidoItemForm[];
}

@Component({
  selector: 'app-pedidos-abertos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pedidos-abertos.html',
  styleUrls: ['./pedidos-abertos.scss']
})
export class PedidosAbertos implements OnInit, OnDestroy {
  abaAtiva: AbaPedidos = 'resumo';
  readonly tiposPedido = ['Entrega', 'Restaurante'];
  readonly statusAbertos = ['Preparando', 'Pronto'];

  tipoAtivo = this.tiposPedido[0];

  pedidosAbertos: PedidoResumo[] = [];
  pedidosPreparando: PedidoResumo[] = [];
  pedidosProntos: PedidoResumo[] = [];
  pedidosPorTipo: Record<string, PedidoResumo[]> = {};
  tiposCarregando: Record<string, boolean> = {};
  itensDisponiveis: PedidoItemSelecionavel[] = [];

  carregando = false;
  salvandoPedido = false;
  acaoRapidaEmExecucao = false;
  modalDetalhesAberto = false;
  pedidoSelecionado: PedidoDetalhe | null = null;
  carregandoDetalhes = false;
  mensagemFormulario = '';

  pedidoFormulario: PedidoFormulario = this.criarFormularioVazio();

  private readonly subscriptions = new Subscription();
  private pedidoIdParaAbrir?: number;

  constructor(
    private readonly pedidoService: PedidoService,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.carregarPedidosAbertos();
    this.carregarItensDisponiveis();

    this.subscriptions.add(
      this.pedidoService.atualizacoes$.subscribe(() => this.recarregarDados())
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

  selecionarAba(aba: AbaPedidos): void {
    this.abaAtiva = aba;
    if (aba === 'tipos') {
      this.carregarPedidosPorTipoSeNecessario(this.tipoAtivo);
      this.tiposPedido.forEach((tipo) => this.carregarPedidosPorTipoSeNecessario(tipo));
    }
  }

  selecionarTipo(tipo: string): void {
    this.tipoAtivo = tipo;
    this.carregarPedidosPorTipoSeNecessario(tipo);
  }

  abrirDetalhes(pedido: PedidoResumo): void {
    if (!pedido?.id) {
      return;
    }
    this.abrirDetalhesPorId(pedido.id);
  }

  abrirDetalhesPorId(pedidoId: number): void {
    if (!pedidoId) {
      return;
    }

    this.carregandoDetalhes = true;
    this.pedidoService.obterPedido(pedidoId).subscribe({
      next: (detalhe) => {
        this.pedidoSelecionado = detalhe;
        this.preencherFormulario(detalhe);
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

  fecharModalDetalhes(): void {
    this.modalDetalhesAberto = false;
    this.pedidoSelecionado = null;
    this.pedidoFormulario = this.criarFormularioVazio();
    this.mensagemFormulario = '';
  }

  formatarData(data?: string): string {
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
    if (chave === 'pronto') {
      return '#22c55e';
    }
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

  contagemTipo(tipo: string): number {
    return this.pedidosPorTipo[tipo]?.length ?? 0;
  }

  estaCarregandoTipo(tipo: string): boolean {
    return !!this.tiposCarregando[tipo];
  }

  private carregarPedidosAbertos(): void {
    this.carregando = true;
    this.pedidoService.listarPorGrupoStatus('ABERTOS').subscribe({
      next: (pedidos) => {
        this.pedidosAbertos = pedidos;
        this.pedidosPreparando = pedidos.filter(
          (pedido) => this.normalizarStatus(pedido.status) === 'preparando'
        );
        this.pedidosProntos = pedidos.filter(
          (pedido) => this.normalizarStatus(pedido.status) === 'pronto'
        );
        this.carregando = false;
      },
      error: (err) => {
        console.error('Erro ao listar pedidos abertos', err);
        this.pedidosAbertos = [];
        this.pedidosPreparando = [];
        this.pedidosProntos = [];
        this.carregando = false;
      }
    });
  }

  private carregarPedidosPorTipo(tipo: string): void {
    this.tiposCarregando[tipo] = true;
    this.pedidoService.listarPedidosAbertosPorTipo(tipo).subscribe({
      next: (pedidos) => {
        this.pedidosPorTipo[tipo] = pedidos;
        this.tiposCarregando[tipo] = false;
      },
      error: (err) => {
        console.error(`Erro ao listar pedidos do tipo ${tipo}`, err);
        this.pedidosPorTipo[tipo] = [];
        this.tiposCarregando[tipo] = false;
      }
    });
  }

  private carregarPedidosPorTipoSeNecessario(tipo: string): void {
    if (!this.pedidosPorTipo[tipo] && !this.tiposCarregando[tipo]) {
      this.carregarPedidosPorTipo(tipo);
    }
  }

  adicionarItem(): void {
    this.pedidoFormulario.itens.push({ itemId: null, quantidade: 1 });
  }

  removerItem(index: number): void {
    if (this.pedidoFormulario.itens.length === 1) {
      return;
    }
    this.pedidoFormulario.itens.splice(index, 1);
  }

  salvarEdicoesPedido(): void {
    if (!this.pedidoSelecionado) {
      return;
    }
    const payload = this.montarPayloadAtualizacao(this.pedidoFormulario.status);
    if (!payload) {
      return;
    }

    this.salvandoPedido = true;
    this.pedidoService.atualizarPedido(this.pedidoSelecionado.id, payload).subscribe({
      next: () => {
        this.salvandoPedido = false;
        this.modalDetalhesAberto = false;
        this.pedidoService.notificarAtualizacao();
      },
      error: (err) => {
        console.error('Erro ao salvar pedido', err);
        this.salvandoPedido = false;
        this.mensagemFormulario = 'Nao foi possivel salvar. Veja o console.';
      }
    });
  }

  finalizarPedido(status: 'Finalizado' | 'Cancelado'): void {
    if (!this.pedidoSelecionado) {
      return;
    }
    const payload = this.montarPayloadAtualizacao(status);
    if (!payload) {
      return;
    }
    this.salvandoPedido = true;
    this.pedidoService.atualizarPedido(this.pedidoSelecionado.id, payload).subscribe({
      next: () => {
        this.salvandoPedido = false;
        this.modalDetalhesAberto = false;
        this.pedidoService.notificarAtualizacao();
      },
      error: (err) => {
        console.error('Erro ao atualizar status do pedido', err);
        this.salvandoPedido = false;
        this.mensagemFormulario = 'Nao foi possivel atualizar o status. Veja o console.';
      }
    });
  }

  finalizarPedidoDireto(pedido: PedidoResumo): void {
    if (!pedido?.id || this.acaoRapidaEmExecucao) {
      return;
    }
    this.acaoRapidaEmExecucao = true;
    this.pedidoService.obterPedido(pedido.id).subscribe({
      next: (detalhe) => {
        const payload = this.criarPayloadDeDetalhe(detalhe, 'Finalizado');
        this.pedidoService.atualizarPedido(detalhe.id, payload).subscribe({
          next: () => {
            this.acaoRapidaEmExecucao = false;
            this.pedidoService.notificarAtualizacao();
          },
          error: (erroAtualizacao) => {
            console.error('Erro ao finalizar pedido', erroAtualizacao);
            this.acaoRapidaEmExecucao = false;
          }
        });
      },
      error: (err) => {
        console.error('Erro ao carregar pedido para finalizar', err);
        this.acaoRapidaEmExecucao = false;
      }
    });
  }

  private criarFormularioVazio(): PedidoFormulario {
    return {
      cliente: '',
      tipoPedido: this.tiposPedido[0],
      status: this.statusAbertos[0],
      observacao: '',
      itens: [{ itemId: null, quantidade: 1 }]
    };
  }

  private preencherFormulario(detalhe: PedidoDetalhe): void {
    const itens =
      detalhe.itens.length > 0
        ? detalhe.itens.map((item) => ({
            itemId: item.itemId,
            quantidade: item.quantidade
          }))
        : [{ itemId: null, quantidade: 1 }];

    this.pedidoFormulario = {
      cliente: detalhe.cliente ?? '',
      tipoPedido: detalhe.tipoPedido,
      status: detalhe.status,
      observacao: detalhe.observacao ?? '',
      itens
    };
    this.mensagemFormulario = '';
  }

  private carregarItensDisponiveis(): void {
    this.pedidoService.listarItens().subscribe({
      next: (itens) => {
        this.itensDisponiveis = itens.filter((item) => item.ativo);
      },
      error: (err) => {
        console.error('Erro ao carregar itens', err);
        this.itensDisponiveis = [];
      }
    });
  }

  private recarregarDados(): void {
    this.carregarPedidosAbertos();
    Object.keys(this.pedidosPorTipo).forEach((tipo) => this.carregarPedidosPorTipo(tipo));
  }

  private normalizarStatus(status?: string): string {
    return (status ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  }

  private montarPayloadAtualizacao(status: string): PedidoUpdatePayload | null {
    if (!this.pedidoSelecionado) {
      return null;
    }

    const itensValidos = this.pedidoFormulario.itens
      .map((item) => ({
        itemId: item.itemId ?? undefined,
        quantidade: Number(item.quantidade) || 0
      }))
      .filter((item) => typeof item.itemId === 'number' && item.quantidade > 0)
      .map((item) => ({
        itemId: item.itemId as number,
        quantidade: item.quantidade
      }));

    if (!itensValidos.length) {
      this.mensagemFormulario = 'Selecione ao menos um item valido.';
      return null;
    }

    return {
      cliente: this.pedidoFormulario.cliente.trim() || undefined,
      tipoPedido: this.pedidoFormulario.tipoPedido,
      status,
      observacao: this.pedidoFormulario.observacao.trim() || undefined,
      itens: itensValidos
    };
  }

  private criarPayloadDeDetalhe(detalhe: PedidoDetalhe, status: string): PedidoUpdatePayload {
    return {
      cliente: detalhe.cliente ?? undefined,
      tipoPedido: detalhe.tipoPedido,
      status,
      observacao: detalhe.observacao ?? undefined,
      itens: detalhe.itens.map((item) => ({
        itemId: item.itemId,
        quantidade: item.quantidade
      }))
    };
  }
}
