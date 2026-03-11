import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService, EmpresaDetalhe } from '../../services/admin.service';

@Component({
  selector: 'app-empresa',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './empresa.html',
  styleUrls: ['./empresa.scss']
})
export class EmpresaPage {
  empresa: EmpresaDetalhe | null = null;
  carregando = false;
  erro = '';

  constructor(private readonly adminService: AdminService) {
    this.carregarEmpresa();
  }

  carregarEmpresa(): void {
    this.carregando = true;
    this.adminService.obterEmpresa().subscribe({
      next: (e) => {
        this.empresa = e;
        this.carregando = false;
        this.erro = '';
      },
      error: (err) => {
        console.error(err);
        this.erro = 'Empresa não encontrada.';
        this.carregando = false;
        this.empresa = null;
      }
    });
  }

  salvar(): void {
    if (!this.empresa) return;
    this.carregando = true;
    this.adminService.atualizarEmpresa(this.empresa).subscribe({
      next: (e) => {
        this.empresa = e;
        this.carregando = false;
      },
      error: (err) => {
        console.error(err);
        this.erro = 'Não foi possível salvar.';
        this.carregando = false;
      }
    });
  }
}
