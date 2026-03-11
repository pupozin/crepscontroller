import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { map } from 'rxjs/operators';
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
  token: string;
  expiresAtUtc: string | Date;
}

interface LoginRequest {
  email: string;
  senha: string;
}

interface LoginResponse extends UsuarioAutenticado {
  token: string;
  expiresAtUtc: string;
}

interface PrimeiroAcessoResposta {
  podeDefinir?: boolean;
  PodeDefinir?: boolean;
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
      .post<LoginResponse>(this.buildUrl('auth/login'), payload)
      .pipe(tap((usuario) => this.salvar(usuario)));
  }

  verificarPrimeiroAcesso(email: string): Observable<boolean> {
    return this.http
      .post<PrimeiroAcessoResposta>(this.buildUrl('auth/primeiro-acesso/verificar'), { email })
      .pipe(map((resp) => !!(resp?.podeDefinir ?? resp?.PodeDefinir)));
  }

  definirPrimeiroAcesso(email: string, senha: string): Observable<UsuarioAutenticado> {
    return this.http
      .post<LoginResponse>(this.buildUrl('auth/primeiro-acesso/definir'), { email, senha })
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

  obterToken(): string | null {
    const usuario = this.usuarioSubject.value;
    if (!usuario) return null;
    const exp = new Date(usuario.expiresAtUtc);
    if (isNaN(exp.getTime()) || exp <= new Date()) {
      this.logout();
      return null;
    }
    return usuario.token;
  }

  estaAutenticado(): boolean {
    return this.usuarioValido(this.usuarioSubject.value);
  }

  private salvar(usuario: LoginResponse): void {
    const normalizado: UsuarioAutenticado = {
      id: usuario.id,
      email: usuario.email,
      nome: usuario.nome,
      empresaId: usuario.empresaId,
      empresaNome: usuario.empresaNome,
      perfilId: usuario.perfilId,
      perfilNome: usuario.perfilNome,
      token: usuario.token,
      expiresAtUtc: usuario.expiresAtUtc
    };

    this.usuarioSubject.next(normalizado);
    localStorage.setItem(this.storageKey, JSON.stringify(normalizado));
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
      usuario.nome &&
      usuario.token &&
      this.tokenValido(usuario.expiresAtUtc)
    );
  }

  private tokenValido(expiresAtUtc: string | Date): boolean {
    const exp = new Date(expiresAtUtc);
    return !isNaN(exp.getTime()) && exp > new Date();
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
