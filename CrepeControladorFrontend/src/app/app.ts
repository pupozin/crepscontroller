import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { Header } from './components/header/header';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [Header, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  menuAberto = false;
  private routeSub?: Subscription;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    if (this.auth.estaAutenticado() && this.router.url.startsWith('/login')) {
      this.router.navigate(['/abertos']);
    }

    this.routeSub = this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe((evt) => {
        const url = (evt as NavigationEnd).urlAfterRedirects || (evt as NavigationEnd).url;
        if (this.auth.estaAutenticado() && url.startsWith('/login')) {
          this.router.navigate(['/abertos']);
        }
      });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  onMenuToggled(aberto: boolean): void {
    this.menuAberto = aberto;
  }
}
