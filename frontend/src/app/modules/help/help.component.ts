import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TabService } from '../../core/services/tab.service';

@Component({
  selector: 'app-help',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './help.component.html',
  styleUrl: './help.component.scss'
})
export class HelpComponent {
  secaoAberta = signal<string | null>(null);

  constructor(private tabService: TabService) {}

  toggleSecao(id: string) {
    this.secaoAberta.update(v => v === id ? null : id);
  }

  isAberta(id: string): boolean {
    return this.secaoAberta() === id;
  }

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }
}
