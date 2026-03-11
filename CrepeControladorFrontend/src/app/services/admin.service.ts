import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export interface UsuarioResumo {
  id: number;
  email: string;
  nome: string;
  empresaId: number;
  perfilId: number;
  perfilNome?: string;
}

export interface EmpresaDetalhe {
  id: number;
  cnpj: string;
  nome: string;
  razaoSocial: string;
  seguimento: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient, private readonly auth: AuthService) {}

  listarUsuarios(): Observable<UsuarioResumo[]> {
    const empresaId = this.auth.obterEmpresaId();
    const params = new HttpParams().set('empresaId', String(empresaId ?? ''));
    return this.http.get<UsuarioResumo[]>(this.buildUrl('usuarios'), { params });
  }

  atualizarUsuario(usuario: Partial<UsuarioResumo> & { id: number }): Observable<UsuarioResumo> {
    return this.http.put<UsuarioResumo>(this.buildUrl(`usuarios/${usuario.id}`), usuario);
  }

  excluirUsuario(id: number): Observable<void> {
    return this.http.delete<void>(this.buildUrl(`usuarios/${id}`));
  }

  obterEmpresa(): Observable<EmpresaDetalhe> {
    const empresaId = this.auth.obterEmpresaId();
    const params = new HttpParams().set('empresaId', String(empresaId ?? ''));
    return this.http.get<EmpresaDetalhe>(this.buildUrl(`empresas`), { params });
  }

  atualizarEmpresa(empresa: EmpresaDetalhe): Observable<EmpresaDetalhe> {
    return this.http.put<EmpresaDetalhe>(this.buildUrl(`empresas/${empresa.id}`), empresa);
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
}
