import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface UsuarioAutenticado {
  id: number;
  email: string;
  nome: string;
  empresaId: number;
  empresaNome?: string;
  perfilId: number;
  perfilNome?: string;
}

interface LoginRequest {
  email: string;
  senha: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = environment.apiUrl;
  private readonly storageKey = 'usuario-autenticado';
  private readonly usuarioSubject = new BehaviorSubject<UsuarioAutenticado | null>(
    this.lerDoStorage()
  );

  readonly usuario$ = this.usuarioSubject.asObservable();

  constructor(private readonly http: HttpClient, private readonly router: Router) {}

  login(email: string, senha: string): Observable<UsuarioAutenticado> {
    const payload: LoginRequest = { email, senha };
    return this.http
      .post<UsuarioAutenticado>(this.buildUrl('auth/login'), payload)
      .pipe(tap((usuario) => this.salvar(usuario)));
  }

  logout(): void {
    this.usuarioSubject.next(null);
    localStorage.removeItem(this.storageKey);
    this.router.navigate(['/login']);
  }

  obterUsuarioAtual(): UsuarioAutenticado | null {
    return this.usuarioSubject.value;
  }

  obterEmpresaId(): number | null {
    return this.usuarioSubject.value?.empresaId ?? null;
  }

  estaAutenticado(): boolean {
    return this.usuarioValido(this.usuarioSubject.value);
  }

  private salvar(usuario: UsuarioAutenticado): void {
    this.usuarioSubject.next(usuario);
    localStorage.setItem(this.storageKey, JSON.stringify(usuario));
  }

  private lerDoStorage(): UsuarioAutenticado | null {
    try {
      const raw = localStorage.getItem(this.storageKey);
      const parsed = raw ? (JSON.parse(raw) as UsuarioAutenticado) : null;
      return this.usuarioValido(parsed) ? parsed : null;
    } catch {
      return null;
    }
  }

  private usuarioValido(usuario: UsuarioAutenticado | null): usuario is UsuarioAutenticado {
    return !!(
      usuario &&
      usuario.id > 0 &&
      usuario.empresaId > 0 &&
      usuario.perfilId > 0 &&
      usuario.email &&
      usuario.nome
    );
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
