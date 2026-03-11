import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService, UsuarioResumo, Perfil } from '../../services/admin.service';

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

  perfis: Perfil[] = [];
  modalEditarAberto = false;
  usuarioEdicao: UsuarioResumo | null = null;
  modalExcluirAberto = false;
  usuarioExclusao: UsuarioResumo | null = null;

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.carregarPerfis();
    this.carregar();
  }

  carregarPerfis(): void {
    this.adminService.listarPerfis().subscribe({
      next: (res) => {
        this.perfis = res ?? [];
      },
      error: (err) => console.error(err)
    });
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

  abrirCriar(): void {
    const perfilPadrao = this.perfis.length ? this.perfis[0] : { id: 0, nome: '' };
    this.usuarioEdicao = {
      id: 0,
      email: '',
      nome: '',
      perfilId: perfilPadrao.id,
      perfilNome: perfilPadrao.nome
    };
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
    const isNovo = !this.usuarioEdicao.id;
    const req = isNovo
      ? this.adminService.criarUsuario({
          email: this.usuarioEdicao.email,
          nome: this.usuarioEdicao.nome,
          perfilId: this.usuarioEdicao.perfilId
        })
      : this.adminService.atualizarUsuario(this.usuarioEdicao);

    req.subscribe({
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

  abrirExcluir(usuario: UsuarioResumo): void {
    this.usuarioExclusao = usuario;
    this.modalExcluirAberto = true;
    this.erro = '';
  }

  cancelarExcluir(): void {
    this.modalExcluirAberto = false;
    this.usuarioExclusao = null;
  }

  confirmarExcluir(): void {
    if (!this.usuarioExclusao) return;
    this.carregando = true;
    this.adminService.excluirUsuario(this.usuarioExclusao.id).subscribe({
      next: () => {
        this.carregando = false;
        this.modalExcluirAberto = false;
        this.usuarioExclusao = null;
        this.carregar();
      },
      error: (err) => {
        console.error(err);
        this.erro = 'Não foi possível excluir.';
        this.carregando = false;
        this.modalExcluirAberto = false;
      }
    });
  }
}
