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
  versaoAtual: string | null;
  totalRegistros: number;
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
  importando = signal(false);
  ufSelecionada = signal('PR');
  mensagemImport = signal('');

  ufs = ['AC','AL','AM','AP','BA','CE','DF','ES','GO','MA','MG','MS','MT','PA','PB','PE','PI','PR','RJ','RN','RO','RR','RS','SC','SE','SP','TO'];

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

  importarCsv(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const file = input.files[0];
    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.modal.aviso('Arquivo inválido', 'Selecione um arquivo CSV do IBPTax.');
      input.value = '';
      return;
    }

    this.importando.set(true);
    this.mensagemImport.set('');

    const formData = new FormData();
    formData.append('arquivo', file);
    formData.append('uf', this.ufSelecionada());

    this.http.post<any>(`${this.apiUrl}/importar`, formData).subscribe({
      next: r => {
        this.importando.set(false);
        this.mensagemImport.set(r.message);
        this.modal.sucesso('IBPTax Importado', r.message);
        this.carregarStatus();
        input.value = '';
      },
      error: err => {
        this.importando.set(false);
        const msg = err?.error?.message || 'Erro ao importar CSV.';
        this.mensagemImport.set(msg);
        this.modal.erro('Erro na Importação', msg);
        input.value = '';
      }
    });
  }

  abrirSiteIbpt() {
    window.open('https://deolhonoimposto.ibpt.org.br/', '_blank');
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
