import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export interface Mesa {
  id: number;
  numero: string;
  empresaId: number;
}

@Injectable({ providedIn: 'root' })
export class MesaService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient, private readonly auth: AuthService) {}

  listar(): Observable<Mesa[]> {
    return this.http.get<Mesa[]>(this.buildUrl('mesas'), {
      params: this.buildParams()
    });
  }

  listarLivres(mesaIdAtual?: number | null): Observable<Mesa[]> {
    const extras: Record<string, string | number | undefined> = { apenasLivres: 'true' };
    if (mesaIdAtual !== undefined && mesaIdAtual !== null) {
      extras['incluirMesaId'] = mesaIdAtual;
    }
    return this.http.get<Mesa[]>(this.buildUrl('mesas'), {
      params: this.buildParams(extras)
    });
  }

  criar(numero: string): Observable<Mesa> {
    return this.http.post<Mesa>(this.buildUrl('mesas'), {
      numero: numero.trim(),
      empresaId: this.obterEmpresaId()
    });
  }

  atualizar(mesa: Mesa): Observable<Mesa> {
    return this.http.put<Mesa>(this.buildUrl(`mesas/${mesa.id}`), {
      id: mesa.id,
      numero: mesa.numero.trim(),
      empresaId: this.obterEmpresaId()
    });
  }

  excluir(id: number): Observable<void> {
    return this.http.delete<void>(this.buildUrl(`mesas/${id}`), {
      params: this.buildParams({ empresaId: this.obterEmpresaId() })
    });
  }

  private buildUrl(path: string): string {
    const trimmedPath = path.replace(/^\/+/, '');
    const base = (this.apiUrl ?? '').replace(/\\/g, '/').trim();
    if (!base) {
      return `/${trimmedPath}`;
    }
    const baseWithSlash = base.endsWith('/') ? base : `${base}/`;
    const isAbs = /^https?:\/\//i.test(baseWithSlash);
    if (isAbs) {
      try {
        return new URL(trimmedPath, baseWithSlash).toString();
      } catch {
        return `${baseWithSlash}${trimmedPath}`;
      }
    }
    const normalized = baseWithSlash.startsWith('/') ? baseWithSlash : `/${baseWithSlash}`;
    return `${normalized}${trimmedPath}`;
  }

  private buildParams(extra?: Record<string, string | number | undefined>): HttpParams {
    const empresaId = this.obterEmpresaId();
    const valores: Record<string, string> = { empresaId: String(empresaId) };
    if (extra) {
      Object.entries(extra).forEach(([k, v]) => {
        if (v !== undefined && v !== null) {
          valores[k] = String(v);
        }
      });
    }
    let params = new HttpParams();
    Object.entries(valores).forEach(([k, v]) => (params = params.set(k, v)));
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
