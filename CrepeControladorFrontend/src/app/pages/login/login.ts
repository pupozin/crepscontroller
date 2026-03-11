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
  email = 'admin@exemplo.com';
  senha = '123456';
  carregando = false;
  erro = '';

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
}
