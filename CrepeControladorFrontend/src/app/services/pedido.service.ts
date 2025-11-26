import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PedidoResumo {
  id: number;
  codigo: string;
  cliente?: string | null;
  tipoPedido: string;
  status: string;
  observacao?: string | null;
  dataCriacao?: string;
  dataConclusao?: string | null;
  valorTotal: number;
}

export interface PedidoItemDetalhe {
  itemId: number;
  nomeItem: string;
  precoItem: number;
  itemAtivo: boolean;
  quantidade: number;
  precoUnitario: number;
  totalItem: number;
}

export interface PedidoDetalhe extends PedidoResumo {
  itens: PedidoItemDetalhe[];
}

export interface PedidoItemSelecionavel {
  id: number;
  nome: string;
  preco: number;
  ativo: boolean;
}

export interface PedidoItemCreatePayload {
  itemId: number;
  quantidade: number;
}

export interface PedidoCreatePayload {
  cliente?: string;
  tipoPedido: string;
  observacao?: string;
  itens: PedidoItemCreatePayload[];
}

export interface PedidoUpdatePayload {
  cliente?: string;
  tipoPedido: string;
  status: string;
  observacao?: string;
  itens: PedidoItemCreatePayload[];
}

@Injectable({
  providedIn: 'root'
})
export class PedidoService {
  private readonly apiUrl = environment.apiUrl;
  private readonly atualizacao$ = new Subject<void>();

  readonly atualizacoes$ = this.atualizacao$.asObservable();

  constructor(private readonly http: HttpClient) {}

  buscarPedidos(termo: string): Observable<PedidoResumo[]> {
    return this.http.get<PedidoResumo[]>(this.buildUrl('pedidos/pesquisar'), {
      params: { termo }
    });
  }

  listarPorGrupoStatus(grupo: 'ABERTOS' | 'FECHADOS'): Observable<PedidoResumo[]> {
    return this.http.get<PedidoResumo[]>(this.buildUrl('pedidos/grupo-status'), {
      params: { grupo }
    });
  }

  listarPedidosAbertosPorTipo(tipoPedido: string): Observable<PedidoResumo[]> {
    return this.http.get<PedidoResumo[]>(this.buildUrl('pedidos/abertos'), {
      params: { tipoPedido }
    });
  }

  obterPedido(id: number): Observable<PedidoDetalhe> {
    return this.http.get<PedidoDetalhe>(this.buildUrl(`pedidos/${id}`));
  }

  criarPedido(payload: PedidoCreatePayload): Observable<PedidoResumo> {
    return this.http.post<PedidoResumo>(this.buildUrl('pedidos'), payload);
  }

  atualizarPedido(id: number, payload: PedidoUpdatePayload): Observable<PedidoResumo> {
    return this.http.put<PedidoResumo>(this.buildUrl(`pedidos/${id}`), payload);
  }

  listarItens(): Observable<PedidoItemSelecionavel[]> {
    return this.http.get<PedidoItemSelecionavel[]>(this.buildUrl('itens'));
  }

  notificarAtualizacao(): void {
    this.atualizacao$.next();
  }

  private buildUrl(path: string): string {
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;
    try {
      return new URL(normalizedPath, this.apiUrl).toString();
    } catch (err) {
      const sanitizedBase = (this.apiUrl ?? '').replace(/\\/g, '/');
      const baseEndsWithSlash = sanitizedBase.endsWith('/') ? sanitizedBase : `${sanitizedBase}/`;
      return `${baseEndsWithSlash}${normalizedPath.replace(/^\//, '')}`;
    }
  }
}
