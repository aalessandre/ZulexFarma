import { Component, computed, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { TabService } from '../../core/services/tab.service';
import { ErpSettingsService, FonteEscala, Tema } from '../../core/services/erp-settings.service';
import { ModalGlobalComponent } from '../../core/components/modal-global.component';
import { CassiComponent } from '../cassi/cassi.component';

@Component({
  selector: 'app-erp-shell',
  standalone: true,
  imports: [RouterOutlet, CommonModule, ModalGlobalComponent, CassiComponent],
  templateUrl: './erp-shell.component.html',
  styleUrl: './erp-shell.component.scss'
})
export class ErpShellComponent {
  usuario = computed(() => this.authService.usuarioLogado());
  painelAberto = signal(false);

  tituloAtual = computed(() => {
    const id = this.tabService.tabAtiva();
    const tab = this.tabService.tabs().find(t => t.id === id);
    return tab ? tab.titulo : 'ZulexPharma';
  });

  constructor(
    public tabService: TabService,
    public authService: AuthService,
    private router: Router,
    public settings: ErpSettingsService
  ) {}

  irHome() { this.router.navigate(['/dashboard']); }
  logout()  { this.authService.logout(); }

  abrirPainel()  { this.painelAberto.set(true); }
  fecharPainel() { this.painelAberto.set(false); }
  setTema(t: Tema)         { this.settings.tema.set(t); }
  setFonte(f: FonteEscala) { this.settings.fonte.set(f); }
}
