import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Mesa, MesaService } from '../../services/mesa.service';

@Component({
  selector: 'app-mesas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mesas.html',
  styleUrls: ['./mesas.scss']
})
export class MesasPage implements OnInit {
  mesas: Mesa[] = [];
  carregando = false;
  salvandoMesa = false;
  mensagemMesa = '';
  confirmacao = { visivel: false, mensagem: '', acao: () => {} };

  modalMesaAberto = false;
  mesaEmEdicao: Mesa | null = null;
  novaMesaNumero = '';

  constructor(private readonly mesaService: MesaService) {}

  ngOnInit(): void {
    this.listar();
  }

  listar(): void {
    this.carregando = true;
    this.mesaService.listar().subscribe({
      next: (mesas) => {
        this.mesas = mesas;
        this.carregando = false;
      },
      error: (err) => {
        console.error('Erro ao listar mesas', err);
        this.mesas = [];
        this.carregando = false;
      }
    });
  }

  abrirModalMesa(mesa?: Mesa): void {
    this.modalMesaAberto = true;
    this.mensagemMesa = '';
    if (mesa) {
      this.mesaEmEdicao = { ...mesa };
      this.novaMesaNumero = mesa.numero;
    } else {
      this.mesaEmEdicao = null;
      this.novaMesaNumero = '';
    }
  }

  fecharModalMesa(): void {
    this.modalMesaAberto = false;
    this.mesaEmEdicao = null;
    this.novaMesaNumero = '';
    this.mensagemMesa = '';
  }

  salvarMesa(): void {
    const numero = this.novaMesaNumero.trim();
    if (!numero) {
      this.mensagemMesa = 'Informe o numero da mesa.';
      return;
    }

    this.salvandoMesa = true;
    const acao$ = this.mesaEmEdicao
      ? this.mesaService.atualizar({ ...this.mesaEmEdicao, numero })
      : this.mesaService.criar(numero);

    acao$.subscribe({
      next: () => {
        this.salvandoMesa = false;
        this.fecharModalMesa();
        this.listar();
        window.location.reload();
      },
      error: (err) => {
        console.error('Erro ao salvar mesa', err);
        this.salvandoMesa = false;
        this.mensagemMesa = 'Nao foi possivel salvar a mesa.';
      }
    });
  }

  excluirMesa(mesa: Mesa): void {
    this.abrirConfirmacao(`Remover ${mesa.numero}?`, () => {
      this.mesaService.excluir(mesa.id).subscribe({
        next: () => {
          this.cancelarConfirmacao();
          this.listar();
        },
        error: (err) => {
          console.error('Erro ao excluir mesa', err);
          this.mensagemMesa = 'Nao foi possivel excluir a mesa.';
          this.cancelarConfirmacao();
        }
      });
    });
  }

  abrirConfirmacao(mensagem: string, acao: () => void): void {
    this.confirmacao = { visivel: true, mensagem, acao };
  }

  confirmarAcao(): void {
    if (this.confirmacao.visivel) {
      this.confirmacao.acao();
    }
  }

  cancelarConfirmacao(): void {
    this.confirmacao = { visivel: false, mensagem: '', acao: () => {} };
  }
}
