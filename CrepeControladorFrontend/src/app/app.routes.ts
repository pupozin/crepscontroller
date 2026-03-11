import { Routes } from '@angular/router';
import { PedidosAbertos } from './pages/pedidos-abertos/pedidos-abertos';
import { PedidosFechados } from './pages/pedidos-fechados/pedidos-fechados';
import { Itens } from './pages/itens/itens';
import { Dados } from './pages/dados/dados';
import { LoginPage } from './pages/login/login';
import { authGuard, loginGuard } from './services/auth.guard';
import { UsuariosPage } from './pages/usuarios/usuarios';
import { EmpresaPage } from './pages/empresa/empresa';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [loginGuard] },
  { path: 'abertos', component: PedidosAbertos, canActivate: [authGuard] },
  { path: 'fechados', component: PedidosFechados, canActivate: [authGuard] },
  { path: 'itens', component: Itens, canActivate: [authGuard] },
  { path: 'dados', component: Dados, canActivate: [authGuard] },
  { path: 'usuarios', component: UsuariosPage, canActivate: [authGuard] },
  { path: 'empresa', component: EmpresaPage, canActivate: [authGuard] },
  { path: '', redirectTo: 'abertos', pathMatch: 'full' },
  { path: '**', redirectTo: 'abertos' }
];
