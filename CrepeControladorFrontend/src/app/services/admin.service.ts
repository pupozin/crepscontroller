import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export interface UsuarioResumo {
  id: number;
  email: string;
  nome: string;
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

export interface Perfil {
  id: number;
  nome: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient, private readonly auth: AuthService) {}

  listarUsuarios(): Observable<UsuarioResumo[]> {
    const empresaId = this.auth.obterEmpresaId();
    const params = new HttpParams().set('empresaId', String(empresaId ?? ''));
    return this.http
      .get<any[]>(this.buildUrl('usuarios'), { params })
      .pipe(
        map((lista) =>
          (lista ?? []).map((u) => ({
            id: u.id ?? u.Id,
            email: u.email ?? u.Email,
            nome: u.nome ?? u.Nome,
            perfilId: u.perfilId ?? u.PerfilId,
            perfilNome: u.perfilNome ?? u.PerfilNome ?? ''
          }))
        )
      );
  }

  criarUsuario(usuario: { email: string; nome: string; perfilId: number }): Observable<UsuarioResumo> {
    const empresaId = this.auth.obterEmpresaId();
    return this.http.post<UsuarioResumo>(this.buildUrl('usuarios'), {
      ...usuario,
      empresaId
    });
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

  listarPerfis(): Observable<Perfil[]> {
    return this.http.get<Perfil[]>(this.buildUrl('perfis'));
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
