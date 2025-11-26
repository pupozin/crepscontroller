import { CommonModule, DOCUMENT } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  Inject,
  OnDestroy,
  OnInit,
  Output
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  PedidoItemSelecionavel,
  PedidoResumo,
  PedidoService
} from '../../services/pedido.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, CommonModule, FormsModule],
  templateUrl: './header.html',
  styleUrl: './header.scss'
})
export class Header implements OnInit, OnDestroy {
  @Output() readonly menuToggled = new EventEmitter<boolean>();

  menuAberto = false;
  modalCriarAberto = false;

  readonly tiposPedido = ['Entrega', 'Restaurante'];
  itensDisponiveis: PedidoItemSelecionavel[] = [];
  carregandoItens = false;

  novoPedido = {
    cliente: '',
    tipoPedido: this.tiposPedido[0],
    observacao: '',
    itens: [{ itemId: null as number | null, quantidade: 1 }]
  };

  termoBusca = '';
  modalBuscaAberto = false;
  resultadosBusca: PedidoResumo[] = [];
  carregandoBusca = false;
  carregandoCriacao = false;

  toast = {
    visivel: false,
    mensagem: '',
    tipo: ''
  };

  constructor(
    private readonly pedidoService: PedidoService,
    private readonly router: Router,
    @Inject(DOCUMENT) private readonly document: Document
  ) {}

  ngOnInit(): void {
    this.carregarItens();
  }

  ngOnDestroy(): void {
    this.liberarScrollMobile();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (!this.menuAberto) {
      this.liberarScrollMobile();
      return;
    }
    this.aplicarComportamentoMenuMobile();
  }

  toggleMenu(): void {
    this.menuAberto = !this.menuAberto;
    this.menuToggled.emit(this.menuAberto);
    this.aplicarComportamentoMenuMobile();
  }

  closeMenu(): void {
    if (!this.menuAberto) {
      return;
    }
    this.menuAberto = false;
    this.menuToggled.emit(this.menuAberto);
    this.aplicarComportamentoMenuMobile();
  }

  abrirModalCriar(): void {
    this.novoPedido = {
      cliente: '',
      tipoPedido: this.tiposPedido[0],
      observacao: '',
      itens: [{ itemId: null, quantidade: 1 }]
    };
    this.modalCriarAberto = true;
  }

  fecharModalCriar(): void {
    this.modalCriarAberto = false;
  }

  adicionarItem(): void {
    this.novoPedido.itens.push({ itemId: null, quantidade: 1 });
  }

  removerItem(index: number): void {
    if (this.novoPedido.itens.length === 1) {
      return;
    }
    this.novoPedido.itens.splice(index, 1);
  }

  criarPedido(): void {
    if (this.carregandoCriacao) {
      return;
    }

    const itensValidos = this.novoPedido.itens
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
      this.exibirToast('Selecione ao menos um item valido.', 'erro');
      return;
    }

    const payload = {
      cliente: this.novoPedido.cliente.trim() || undefined,
      tipoPedido: this.novoPedido.tipoPedido,
      observacao: this.novoPedido.observacao.trim() || undefined,
      itens: itensValidos
    };

    this.carregandoCriacao = true;
    this.pedidoService.criarPedido(payload).subscribe({
      next: (pedido) => {
        this.carregandoCriacao = false;
        this.exibirToast(`${pedido.codigo} criado com sucesso.`, 'sucesso');
        this.fecharModalCriar();
        this.pedidoService.notificarAtualizacao();
      },
      error: (err) => {
        console.error('Erro ao criar pedido', err);
        this.carregandoCriacao = false;
        this.exibirToast('Nao foi possivel criar o pedido.', 'erro');
      }
    });
  }

  buscarGlobal(): void {
    const termo = this.termoBusca.trim();
    if (!termo) {
      return;
    }

    this.carregandoBusca = true;
    this.modalBuscaAberto = true;
    this.pedidoService.buscarPedidos(termo).subscribe({
      next: (resultados) => {
        this.resultadosBusca = resultados;
        this.carregandoBusca = false;
      },
      error: (err) => {
        console.error('Erro na busca global', err);
        this.resultadosBusca = [];
        this.carregandoBusca = false;
      }
    });
  }

  fecharModalBusca(): void {
    this.modalBuscaAberto = false;
    this.termoBusca = '';
    this.resultadosBusca = [];
    this.aplicarComportamentoMenuMobile();
  }

  abrirDetalhes(pedido: PedidoResumo): void {
    this.modalBuscaAberto = false;
    const status = (pedido.status || '').toLowerCase();
    const destino = status === 'finalizado' || status === 'cancelado' ? '/fechados' : '/abertos';
    this.router.navigate([destino], {
      queryParams: { pedido: pedido.id }
    });
  }

  private carregarItens(): void {
    this.carregandoItens = true;
    this.pedidoService.listarItens().subscribe({
      next: (itens) => {
        this.itensDisponiveis = itens.filter((item) => item.ativo);
        this.carregandoItens = false;
      },
      error: (err) => {
        console.error('Erro ao carregar itens', err);
        this.itensDisponiveis = [];
        this.carregandoItens = false;
      }
    });
  }

  private exibirToast(mensagem: string, tipo: 'sucesso' | 'erro'): void {
    this.toast.mensagem = mensagem;
    this.toast.tipo = tipo;
    this.toast.visivel = true;

    setTimeout(() => {
      this.toast.visivel = false;
    }, 3000);
  }

  private aplicarComportamentoMenuMobile(): void {
    if (!this.document) {
      return;
    }

    if (!this.menuAberto) {
      this.liberarScrollMobile();
      return;
    }

    if (this.isMobileViewport()) {
      window.scrollTo({ top: 0, behavior: 'auto' });
      this.document.body.classList.add('menu-mobile-bloqueado');
    } else {
      this.liberarScrollMobile();
    }
  }

  private liberarScrollMobile(): void {
    if (!this.document) {
      return;
    }
    this.document.body.classList.remove('menu-mobile-bloqueado');
  }

  private isMobileViewport(): boolean {
    return typeof window !== 'undefined' && window.innerWidth <= 1050;
  }
}
