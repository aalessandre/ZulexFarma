import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface ConfigItem { chave: string; valor: string; descricao?: string; }

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './configuracoes.component.html',
  styleUrl: './configuracoes.component.scss'
})
export class ConfiguracoesComponent implements OnInit {
  carregando = signal(false);
  salvando = signal(false);
  configs = signal<Record<string, string>>({});
  private apiUrl = `${environment.apiUrl}/configuracoes`;

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() { this.carregar(); }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  formatarIntervalo(segundos: number): string {
    if (segundos < 60) return `${segundos}s`;
    const min = Math.floor(segundos / 60);
    const sec = segundos % 60;
    return sec > 0 ? `${min}min ${sec}s` : `${min}min`;
  }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        const map: Record<string, string> = {};
        for (const item of (r.data ?? [])) map[item.chave] = item.valor;
        this.configs.set(map);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  getConfig(chave: string, padrao = ''): string {
    return this.configs()[chave] ?? padrao;
  }

  setConfig(chave: string, valor: string) {
    this.configs.update(c => ({ ...c, [chave]: valor }));
  }

  async salvar() {
    this.salvando.set(true);
    const items: ConfigItem[] = Object.entries(this.configs()).map(([chave, valor]) => ({ chave, valor }));
    this.http.put(this.apiUrl, items).subscribe({
      next: () => {
        this.salvando.set(false);
        this.modal.sucesso('Salvo', 'Configurações salvas com sucesso.');
      },
      error: () => {
        this.salvando.set(false);
        this.modal.erro('Erro', 'Erro ao salvar configurações.');
      }
    });
  }
}
