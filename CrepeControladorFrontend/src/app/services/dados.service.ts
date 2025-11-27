import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DashboardResumoPeriodo {
  qtdePedidos: number;
  faturamentoTotal: number;
  ticketMedio: number;
  qtdeDiasPeriodo: number;
  mediaClientesPorDia: number;
}

export interface DashboardItemRanking {
  itemId: number;
  nome: string;
  quantidadeVendida: number;
  faturamento: number;
}

export interface DashboardTipoPedido {
  tipoPedido: string;
  qtdePedidos: number;
  faturamento: number;
}

export interface DashboardHorarioPico {
  hora: number;
  quantidadePedidos: number;
  faturamento: number;
}

@Injectable({
  providedIn: 'root'
})
export class DadosService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  obterResumo(dataInicio: string, dataFim: string): Observable<DashboardResumoPeriodo> {
    return this.http.get<DashboardResumoPeriodo>(this.buildUrl('dashboard/resumo'), {
      params: this.buildPeriodoParams(dataInicio, dataFim)
    });
  }

  obterItensRanking(dataInicio: string, dataFim: string): Observable<DashboardItemRanking[]> {
    return this.http.get<DashboardItemRanking[]>(this.buildUrl('dashboard/itens-ranking'), {
      params: this.buildPeriodoParams(dataInicio, dataFim)
    });
  }

  obterTipoPedido(dataInicio: string, dataFim: string): Observable<DashboardTipoPedido[]> {
    return this.http.get<DashboardTipoPedido[]>(this.buildUrl('dashboard/tipo-pedido'), {
      params: this.buildPeriodoParams(dataInicio, dataFim)
    });
  }

  obterHorariosDiaSemana(diaSemana: number, ano?: number): Observable<DashboardHorarioPico[]> {
    let params = new HttpParams().set('diaSemana', diaSemana);
    if (ano) {
      params = params.set('ano', ano);
    }
    return this.http.get<DashboardHorarioPico[]>(this.buildUrl('dashboard/horarios/dia-semana'), {
      params
    });
  }

  obterHorariosMes(ano: number, mes: number): Observable<DashboardHorarioPico[]> {
    const params = new HttpParams().set('ano', ano).set('mes', mes);
    return this.http.get<DashboardHorarioPico[]>(this.buildUrl('dashboard/horarios/mes'), {
      params
    });
  }

  private buildPeriodoParams(dataInicio: string, dataFim: string): HttpParams {
    return new HttpParams().set('dataInicio', dataInicio).set('dataFim', dataFim);
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
