import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService, UsuarioResumo } from '../../services/admin.service';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './usuarios.html',
  styleUrls: ['./usuarios.scss']
})
export class UsuariosPage implements OnInit {
  usuarios: UsuarioResumo[] = [];
  carregando = false;
  erro = '';

  modalEditarAberto = false;
  usuarioEdicao: UsuarioResumo | null = null;

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    this.carregando = true;
    this.adminService.listarUsuarios().subscribe({
      next: (res) => {
        this.usuarios = res;
        this.carregando = false;
      },
      error: (err) => {
        console.error(err);
        this.erro = 'Não foi possível carregar usuários.';
        this.carregando = false;
      }
    });
  }

  abrirEdicao(usuario: UsuarioResumo): void {
    this.usuarioEdicao = { ...usuario };
    this.modalEditarAberto = true;
  }

  fecharModal(): void {
    this.modalEditarAberto = false;
    this.usuarioEdicao = null;
    this.erro = '';
  }

  salvar(): void {
    if (!this.usuarioEdicao) return;
    this.carregando = true;
    this.adminService.atualizarUsuario(this.usuarioEdicao).subscribe({
      next: () => {
        this.carregando = false;
        this.modalEditarAberto = false;
        this.carregar();
      },
      error: (err) => {
        console.error(err);
        this.erro = 'Não foi possível salvar.';
        this.carregando = false;
      }
    });
  }

  excluir(usuario: UsuarioResumo): void {
    if (!confirm(`Excluir ${usuario.nome}?`)) return;
    this.carregando = true;
    this.adminService.excluirUsuario(usuario.id).subscribe({
      next: () => {
        this.carregando = false;
        this.carregar();
      },
      error: (err) => {
        console.error(err);
        this.erro = 'Não foi possível excluir.';
        this.carregando = false;
      }
    });
  }
}
