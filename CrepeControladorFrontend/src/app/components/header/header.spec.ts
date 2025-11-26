import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { Header } from './header';
import { PedidoService } from '../../services/pedido.service';

describe('Header', () => {
  let component: Header;
  let fixture: ComponentFixture<Header>;

  beforeEach(async () => {
    const pedidoServiceStub = {
      listarItens: () => of([]),
      criarPedido: () => of({ codigo: 'Pedido #0001' }),
      buscarPedidos: () => of([]),
      notificarAtualizacao: () => {}
    } as Partial<PedidoService>;

    await TestBed.configureTestingModule({
      imports: [Header, RouterTestingModule],
      providers: [
        {
          provide: PedidoService,
          useValue: pedidoServiceStub
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Header);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
