import { CommonModule } from '@angular/common';
import { Component, HostListener, inject } from '@angular/core';
import { RouterLink, RouterOutlet, Router } from '@angular/router';
import { TablerIconsModule } from 'angular-tabler-icons';
import { AppAuthBrandingComponent } from 'src/app/layouts/full/vertical/sidebar/auth-branding.component';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-home-page',
  imports: [MaterialModule,
    TablerIconsModule,
    RouterLink,
    RouterOutlet,
    CommonModule,
    AppAuthBrandingComponent,
  ],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.scss'
})
export class HomePageComponent {

  private router = inject(Router);
  hideCloserBtn: boolean = true;
  showBackToTop: boolean;
  isTopbarFixed: boolean;
  hideCloser() {
    this.hideCloserBtn = false;
  }
  isActiveRoute(route: string): boolean {
    return this.router.url.includes(`/front-pages/${route}`);
  }
  getNavigate() {

  }
  gethomepage() {
    this.router.navigate(['/front-pages/homepage'])
  }

  scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.showBackToTop = window.scrollY > 300;
    this.isTopbarFixed = scrollY > 45;
  }
}
