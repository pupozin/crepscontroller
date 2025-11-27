import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DadosService } from '../../services/dados.service';

import { Dados } from './dados';

describe('Dados', () => {
  let component: Dados;
  let fixture: ComponentFixture<Dados>;

  const dadosServiceStub = {
    obterResumo: () =>
      of({
        qtdePedidos: 0,
        faturamentoTotal: 0,
        ticketMedio: 0,
        qtdeDiasPeriodo: 0,
        mediaClientesPorDia: 0
      }),
    obterTipoPedido: () => of([]),
    obterItensRanking: () => of([]),
    obterHorariosPeriodo: () => of([]),
    obterPicosDiaSemana: () => of([]),
    obterDistribuicaoDiaSemana: () => of([]),
    obterPeriodoTotal: () => of({ dataInicio: null, dataFim: null })
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Dados],
      providers: [{ provide: DadosService, useValue: dadosServiceStub }]
    }).compileComponents();

    fixture = TestBed.createComponent(Dados);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
