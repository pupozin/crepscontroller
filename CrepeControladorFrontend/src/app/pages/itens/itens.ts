import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PedidoItemSelecionavel, PedidoService } from '../../services/pedido.service';

type AbaItens = 'ativos' | 'desativados';

@Component({
  selector: 'app-itens',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './itens.html',
  styleUrls: ['./itens.scss']
})
export class Itens implements OnInit {
  abaAtiva: AbaItens = 'ativos';
  itens: PedidoItemSelecionavel[] = [];
  itensAtivos: PedidoItemSelecionavel[] = [];
  itensInativos: PedidoItemSelecionavel[] = [];
  readonly itensPorPagina = 10;
  paginaAtual = 1;

  carregando = false;
  modalItemAberto = false;
  salvandoItem = false;
  mensagemItem = '';
  processandoToggle: Record<number, boolean> = {};
  itemEmEdicao: PedidoItemSelecionavel | null = null;

  novoItem = this.criarNovoItem();

  constructor(private readonly pedidoService: PedidoService) {}

  ngOnInit(): void {
    this.carregarItens();
  }

  selecionarAba(aba: AbaItens): void {
    if (this.abaAtiva === aba) {
      return;
    }
    this.abaAtiva = aba;
    this.paginaAtual = 1;
    this.ajustarPaginaAtual();
  }

  abrirModalItem(item?: PedidoItemSelecionavel): void {
    this.modalItemAberto = true;
    this.mensagemItem = '';
    if (item) {
      this.itemEmEdicao = item;
      this.novoItem = {
        nome: item.nome,
        preco: String(item.preco ?? '')
      };
    } else {
      this.itemEmEdicao = null;
      this.novoItem = this.criarNovoItem();
    }
  }

  fecharModalItem(): void {
    this.modalItemAberto = false;
    this.mensagemItem = '';
    this.itemEmEdicao = null;
    this.novoItem = this.criarNovoItem();
  }

  salvarItem(): void {
    if (this.salvandoItem) {
      return;
    }

    const nome = this.novoItem.nome.trim();
    const precoNormalizado = String(this.novoItem.preco ?? '').replace(',', '.');
    const preco = Number(precoNormalizado);

    if (!nome) {
      this.mensagemItem = 'Informe o nome do item.';
      return;
    }

    if (!Number.isFinite(preco) || preco <= 0) {
      this.mensagemItem = 'Informe um preco valido.';
      return;
    }

    this.salvandoItem = true;
    const itemAtual = this.itemEmEdicao;
    const requisicao = itemAtual
      ? this.pedidoService.atualizarItem(itemAtual.id, {
          nome,
          preco,
          ativo: itemAtual.ativo
        })
      : this.pedidoService.criarItem({ nome, preco });

    requisicao.subscribe({
      next: (item) => {
        this.salvandoItem = false;
        this.modalItemAberto = false;
        this.novoItem = this.criarNovoItem();
        this.itemEmEdicao = null;
        if (itemAtual) {
          this.substituirItemLocal(item);
        } else {
          this.itens.push(item);
          this.recalcularGrupos();
        }
        location.reload() 
      },
      error: (err) => {
        console.error('Erro ao salvar item', err);
        this.salvandoItem = false;
        this.mensagemItem = 'Nao foi possivel salvar o item. Tente novamente.';
      }
    });
  }

  alternarItem(item: PedidoItemSelecionavel): void {
    if (!item?.id || this.processandoToggle[item.id]) {
      return;
    }
    const novoStatus = !item.ativo;
    this.processandoToggle[item.id] = true;
    this.pedidoService
      .atualizarItem(item.id, {
        nome: item.nome,
        preco: item.preco,
        ativo: novoStatus
      })
      .subscribe({
      next: (atualizado) => {
        this.processandoToggle[item.id] = false;
        this.substituirItemLocal(atualizado);
      },
      error: (err) => {
        console.error('Erro ao atualizar item', err);
        this.processandoToggle[item.id] = false;
      }
    });
  }

  get itensAtuais(): PedidoItemSelecionavel[] {
    return this.abaAtiva === 'ativos' ? this.itensAtivos : this.itensInativos;
  }

  get totalPaginas(): number {
    return Math.ceil(this.itensAtuais.length / this.itensPorPagina);
  }

  get itensVisiveis(): PedidoItemSelecionavel[] {
    if (!this.itensAtuais.length) {
      return [];
    }
    const inicio = (this.paginaAtual - 1) * this.itensPorPagina;
    return this.itensAtuais.slice(inicio, inicio + this.itensPorPagina);
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

  private criarNovoItem(): { nome: string; preco: string } {
    return { nome: '', preco: '' };
  }

  private carregarItens(): void {
    this.carregando = true;
    this.pedidoService.listarItens().subscribe({
      next: (itens) => {
        this.itens = itens;
        this.recalcularGrupos();
        this.carregando = false;
      },
      error: (err) => {
        console.error('Erro ao listar itens', err);
        this.itens = [];
        this.recalcularGrupos();
        this.carregando = false;
      }
    });
  }

  private recalcularGrupos(): void {
    this.itensAtivos = this.itens.filter((item) => item.ativo);
    this.itensInativos = this.itens.filter((item) => !item.ativo);
    this.ajustarPaginaAtual();
  }

  private ajustarPaginaAtual(): void {
    if (!this.itensAtuais.length) {
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

  private substituirItemLocal(itemAtualizado: PedidoItemSelecionavel): void {
    this.itens = this.itens.map((item) =>
      item.id === itemAtualizado.id ? { ...item, ...itemAtualizado } : item
    );
    this.recalcularGrupos();
  }
}
