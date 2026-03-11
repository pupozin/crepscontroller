import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

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
  empresaId: number;
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

export interface ItemCreatePayload {
  nome: string;
  preco: number;
  empresaId?: number;
}

export interface ItemUpdatePayload {
  nome?: string;
  preco?: number;
  ativo?: boolean;
  empresaId?: number;
}

export interface PedidoCreatePayload {
  cliente?: string;
  tipoPedido: string;
  observacao?: string;
  itens: PedidoItemCreatePayload[];
  empresaId?: number;
}

export interface PedidoUpdatePayload {
  cliente?: string;
  tipoPedido: string;
  status: string;
  observacao?: string;
  itens: PedidoItemCreatePayload[];
  empresaId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class PedidoService {
  private readonly apiUrl = environment.apiUrl;
  private readonly atualizacao$ = new Subject<void>();

  readonly atualizacoes$ = this.atualizacao$.asObservable();

  constructor(
    private readonly http: HttpClient,
    private readonly auth: AuthService
  ) {}

  buscarPedidos(termo: string): Observable<PedidoResumo[]> {
    return this.http.get<PedidoResumo[]>(this.buildUrl('pedidos/pesquisar'), {
      params: this.buildParams({ termo })
    });
  }

  listarPorGrupoStatus(grupo: 'ABERTOS' | 'FECHADOS'): Observable<PedidoResumo[]> {
    return this.http.get<PedidoResumo[]>(this.buildUrl('pedidos/grupo-status'), {
      params: this.buildParams({ grupo })
    });
  }

  listarPedidosAbertosPorTipo(tipoPedido: string): Observable<PedidoResumo[]> {
    return this.http.get<PedidoResumo[]>(this.buildUrl('pedidos/abertos'), {
      params: this.buildParams({ tipoPedido })
    });
  }

  obterPedido(id: number): Observable<PedidoDetalhe> {
    return this.http.get<PedidoDetalhe>(this.buildUrl(`pedidos/${id}`), {
      params: this.buildParams()
    });
  }

  criarPedido(payload: PedidoCreatePayload): Observable<PedidoResumo> {
    return this.http.post<PedidoResumo>(this.buildUrl('pedidos'), {
      ...payload,
      empresaId: this.obterEmpresaId()
    });
  }

  atualizarPedido(id: number, payload: PedidoUpdatePayload): Observable<PedidoResumo> {
    return this.http.put<PedidoResumo>(this.buildUrl(`pedidos/${id}`), {
      ...payload,
      empresaId: this.obterEmpresaId()
    });
  }

  listarItens(): Observable<PedidoItemSelecionavel[]> {
    return this.http.get<PedidoItemSelecionavel[]>(this.buildUrl('itens'), {
      params: this.buildParams()
    });
  }

  criarItem(payload: ItemCreatePayload): Observable<PedidoItemSelecionavel> {
    return this.http.post<PedidoItemSelecionavel>(this.buildUrl('itens'), {
      ...payload,
      empresaId: this.obterEmpresaId()
    });
  }

  atualizarItem(id: number, payload: ItemUpdatePayload): Observable<PedidoItemSelecionavel> {
    return this.http.put<PedidoItemSelecionavel>(this.buildUrl(`itens/${id}`), {
      ...payload,
      empresaId: this.obterEmpresaId()
    });
  }

  notificarAtualizacao(): void {
    this.atualizacao$.next();
  }

  private buildUrl(path: string): string {
    const trimmedPath = path.replace(/^\/+/, '');
    const sanitizedBase = (this.apiUrl ?? '').replace(/\\/g, '/').trim();

    if (!sanitizedBase) {
      return `/${trimmedPath}`;
    }

    const baseWithSlash = sanitizedBase.endsWith('/') ? sanitizedBase : `${sanitizedBase}/`;
    const isAbsoluteBase = /^https?:\/\//i.test(baseWithSlash);

    if (isAbsoluteBase) {
      try {
        return new URL(trimmedPath, baseWithSlash).toString();
      } catch {
        return `${baseWithSlash}${trimmedPath}`;
      }
    }

    const normalizedBase = baseWithSlash.startsWith('/') ? baseWithSlash : `/${baseWithSlash}`;
    return `${normalizedBase}${trimmedPath}`;
  }

  private buildParams(extra?: Record<string, string | number | undefined>): HttpParams {
    const empresaId = this.obterEmpresaId();
    const valores: Record<string, string> = { empresaId: String(empresaId) };
    if (extra) {
      Object.entries(extra).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          valores[key] = String(value);
        }
      });
    }
    let params = new HttpParams();
    Object.entries(valores).forEach(([k, v]) => {
      params = params.set(k, v);
    });
    return params;
  }

  private obterEmpresaId(): number {
    const empresaId = this.auth.obterEmpresaId();
    if (!empresaId) {
      throw new Error('EmpresaId nao encontrado. Realize o login novamente.');
    }
    return empresaId;
  }
}
