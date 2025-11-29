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

export interface DashboardDiaSemanaPico {
  diaSemana: number;
  nomeDia: string;
  hora: number;
  quantidadePedidos: number;
  faturamento: number;
}

export interface DashboardDiaSemanaDistribuicao extends DashboardDiaSemanaPico {}

export interface DashboardPeriodoTotal {
  dataInicio: string | null;
  dataFim: string | null;
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

  obterHorariosPeriodo(dataInicio: string, dataFim: string): Observable<DashboardHorarioPico[]> {
    return this.http.get<DashboardHorarioPico[]>(this.buildUrl('dashboard/horarios/periodo'), {
      params: this.buildPeriodoParams(dataInicio, dataFim)
    });
  }

  obterPicosDiaSemana(dataInicio: string, dataFim: string): Observable<DashboardDiaSemanaPico[]> {
    return this.http.get<DashboardDiaSemanaPico[]>(this.buildUrl('dashboard/dia-semana/picos'), {
      params: this.buildPeriodoParams(dataInicio, dataFim)
    });
  }

  obterDistribuicaoDiaSemana(dataInicio: string, dataFim: string): Observable<DashboardDiaSemanaDistribuicao[]> {
    return this.http.get<DashboardDiaSemanaDistribuicao[]>(this.buildUrl('dashboard/dia-semana/distribuicao'), {
      params: this.buildPeriodoParams(dataInicio, dataFim)
    });
  }

  obterPeriodoTotal(): Observable<DashboardPeriodoTotal> {
    return this.http.get<DashboardPeriodoTotal>(this.buildUrl('dashboard/periodo-total'));
  }

  private buildPeriodoParams(dataInicio: string, dataFim: string): HttpParams {
    return new HttpParams().set('dataInicio', dataInicio).set('dataFim', dataFim);
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
}
