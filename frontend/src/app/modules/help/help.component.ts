import { Component } from '@angular/core';
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
  constructor(private tabService: TabService) {}

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }
}
