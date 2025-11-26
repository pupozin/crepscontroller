import { Routes } from '@angular/router';
import { PedidosAbertos } from './pages/pedidos-abertos/pedidos-abertos';
import { PedidosFechados } from './pages/pedidos-fechados/pedidos-fechados';
import { Itens } from './pages/itens/itens';

export const routes: Routes = [
  { path: 'abertos', component: PedidosAbertos },
  { path: 'fechados', component: PedidosFechados },
  { path: 'itens', component: Itens },
  { path: '', redirectTo: 'abertos', pathMatch: 'full' },
  { path: '**', redirectTo: 'abertos' }
];
