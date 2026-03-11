import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.scss']
})
export class LoginPage {
  email = '';
  senha = '';
  carregando = false;
  erro = '';

  primeiroAcessoAberto = false;
  primeiroEtapa: 'email' | 'senha' = 'email';
  emailPrimeiro = '';
  senhaPrimeiro = '';
  senhaPrimeiroConfirma = '';
  erroPrimeiro = '';
  carregandoPrimeiro = false;

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  async entrar(): Promise<void> {
    if (this.carregando) {
      return;
    }
    this.erro = '';

    this.carregando = true;
    this.auth.login(this.email.trim(), this.senha).subscribe({
      next: () => {
        this.carregando = false;
        this.router.navigate(['/abertos']);
      },
      error: (err) => {
        console.error('Falha no login', err);
        this.erro = 'Credenciais inválidas. Tente novamente.';
        this.carregando = false;
      }
    });
  }

  abrirPrimeiroAcesso(): void {
    this.primeiroAcessoAberto = true;
    this.primeiroEtapa = 'email';
    this.emailPrimeiro = '';
    this.senhaPrimeiro = '';
    this.senhaPrimeiroConfirma = '';
    this.erroPrimeiro = '';
  }

  fecharPrimeiroAcesso(): void {
    this.primeiroAcessoAberto = false;
    this.carregandoPrimeiro = false;
    this.erroPrimeiro = '';
  }

  confirmarEmailPrimeiro(): void {
    if (this.carregandoPrimeiro || !this.emailPrimeiro) return;
    this.erroPrimeiro = '';
    this.carregandoPrimeiro = true;
    this.auth.verificarPrimeiroAcesso(this.emailPrimeiro.trim()).subscribe({
      next: () => {
        this.carregandoPrimeiro = false;
        this.primeiroEtapa = 'senha';
      },
      error: (err) => {
        console.error(err);
        this.erroPrimeiro = 'Usuário inexistente ou já possui senha.';
        this.carregandoPrimeiro = false;
      }
    });
  }

  definirSenhaPrimeiro(): void {
    if (this.carregandoPrimeiro) return;
    this.erroPrimeiro = '';
    if (!this.senhaEhValida(this.senhaPrimeiro)) {
      this.erroPrimeiro = 'A senha deve ter 8 a 128 caracteres, incluir letras e números.';
      return;
    }
    if (this.senhaPrimeiro !== this.senhaPrimeiroConfirma) {
      this.erroPrimeiro = 'As senhas n\u00e3o conferem.';
      return;
    }

    this.carregandoPrimeiro = true;
    this.auth.definirPrimeiroAcesso(this.emailPrimeiro.trim(), this.senhaPrimeiro).subscribe({
      next: () => {
        this.carregandoPrimeiro = false;
        this.primeiroAcessoAberto = false;
        this.email = this.emailPrimeiro.trim();
        this.senha = this.senhaPrimeiro;
        this.entrar();
      },
      error: (err) => {
        console.error(err);
        this.erroPrimeiro = 'Não foi possível definir a senha. Tente novamente.';
        this.carregandoPrimeiro = false;
      }
    });
  }

  private senhaEhValida(senha: string): boolean {
    if (!senha) return false;
    if (senha.length < 8 || senha.length > 128) return false;
    const temLetra = /[a-zA-Z]/.test(senha);
    const temNumero = /\d/.test(senha);
    return temLetra && temNumero;
  }
}
