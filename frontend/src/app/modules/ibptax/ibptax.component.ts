import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface IbptStatus {
  versao: string | null;
  uf: string | null;
  dataImportacao: string | null;
  vigenciaFim: string | null;
  totalRegistros: number;
  expirada: boolean;
}

interface IbptVigencia {
  tabelaExpirada: boolean;
  vigenciaFim: string | null;
  ultimaVerificacao: string | null;
  ultimaSincronizacao: string | null;
  versaoAtual: string | null;
  totalRegistros: number;
  sincronizando: boolean;
}

@Component({
  selector: 'app-ibptax',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ibptax.component.html',
  styleUrl: './ibptax.component.scss'
})
export class IbptaxComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/ibpt`;

  status = signal<IbptStatus | null>(null);
  vigencia = signal<IbptVigencia | null>(null);
  loading = signal(false);
  sincronizando = signal(false);
  mensagem = signal('');
  mensagemErro = signal(false);

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.carregarStatus();
  }

  carregarStatus() {
    this.loading.set(true);
    this.http.get<any>(`${this.apiUrl}/status`).subscribe({
      next: r => {
        this.status.set(r.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
    this.http.get<any>(`${this.apiUrl}/vigencia`).subscribe({
      next: r => this.vigencia.set(r.data)
    });
  }

  sincronizar() {
    this.sincronizando.set(true);
    this.mensagem.set('');
    this.mensagemErro.set(false);

    this.http.post<any>(`${this.apiUrl}/sincronizar`, {}).subscribe({
      next: r => {
        this.sincronizando.set(false);
        this.mensagem.set(r.message);
        this.mensagemErro.set(false);
        this.modal.sucesso('IBPTax Atualizado', r.message);
        this.carregarStatus();
      },
      error: err => {
        this.sincronizando.set(false);
        const msg = err?.error?.message || 'Erro ao sincronizar.';
        this.mensagem.set(msg);
        this.mensagemErro.set(true);
        this.modal.erro('Erro na Sincronização', msg);
      }
    });
  }

  formatarData(d: string | null): string {
    if (!d) return '—';
    const dt = new Date(d);
    return dt.toLocaleDateString('pt-BR') + ' ' + dt.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  }

  formatarDataCurta(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('pt-BR');
  }

  diasRestantes(): number | null {
    const fim = this.status()?.vigenciaFim;
    if (!fim) return null;
    const diff = new Date(fim).getTime() - Date.now();
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  }

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }
}
