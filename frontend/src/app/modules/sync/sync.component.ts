import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';

interface SyncItem {
  id: number;
  tabela: string;
  operacao: string;
  registroId: number;
  registroCodigo: string;
  dadosJson: string | null;
  filialOrigemId: number;
  criadoEm: string;
  enviado: boolean;
  enviadoEm: string | null;
  erro: string | null;
}

@Component({
  selector: 'app-sync',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sync.component.html',
  styleUrl: './sync.component.scss'
})
export class SyncComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/sync`;

  // Status geral
  status = signal<any>(null);
  carregando = signal(false);

  // Fila
  fila = signal<SyncItem[]>([]);
  totalRegistros = signal(0);

  // Filtros
  dataInicio = signal(this.hoje());
  dataFim = signal(this.hoje());
  filtroStatus = signal<'todos' | 'pendentes' | 'enviados' | 'recebidos' | 'erros'>('todos');
  filtroTabela = signal('');

  // Paginação
  pagina = signal(1);
  porPagina = signal(20);
  totalPaginas = computed(() => Math.ceil(this.totalRegistros() / this.porPagina()) || 1);

  // Expandido
  itemExpandido = signal<number | null>(null);

  // Stats
  totalPendentes = computed(() => this.fila().filter(f => !f.enviado && !f.erro).length);
  totalEnviados = computed(() => this.fila().filter(f => f.enviado).length);
  totalErros = computed(() => this.fila().filter(f => f.erro).length);

  constructor(private http: HttpClient, private tabService: TabService) {}

  ngOnInit() {
    this.carregarStatus();
    this.buscar();
  }

  carregarStatus() {
    this.http.get<any>(`${this.apiUrl}/status`).subscribe({
      next: r => this.status.set(r?.data),
      error: () => {}
    });
  }

  buscar() {
    this.carregando.set(true);
    const params = new URLSearchParams();
    params.set('dataInicio', this.dataInicio());
    params.set('dataFim', this.dataFim());
    params.set('status', this.filtroStatus());
    params.set('tabela', this.filtroTabela());
    params.set('pagina', this.pagina().toString());
    params.set('porPagina', this.porPagina().toString());

    this.http.get<any>(`${this.apiUrl}/fila?${params.toString()}`).subscribe({
      next: r => {
        this.carregando.set(false);
        this.fila.set(r?.data?.registros ?? []);
        this.totalRegistros.set(r?.data?.total ?? 0);
      },
      error: () => {
        this.carregando.set(false);
        this.fila.set([]);
      }
    });
  }

  irPagina(p: number) {
    if (p < 1 || p > this.totalPaginas()) return;
    this.pagina.set(p);
    this.buscar();
  }

  toggleExpandir(id: number) {
    this.itemExpandido.update(v => v === id ? null : id);
  }

  forcarEnvio() {
    this.http.post<any>(`${this.apiUrl}/forcar-envio`, {}).subscribe({
      next: () => { this.carregarStatus(); this.buscar(); },
      error: () => {}
    });
  }

  resetarRecebimento() {
    if (!confirm('Resetar ponteiro de recebimento? O próximo ciclo vai rebuscar todas as operações do Railway.')) return;
    this.http.post<any>(`${this.apiUrl}/resetar-recebimento`, {}).subscribe({
      next: () => { this.carregarStatus(); this.buscar(); },
      error: () => {}
    });
  }

  limparAntigos() {
    this.http.post<any>(`${this.apiUrl}/limpar?dias=7`, {}).subscribe({
      next: r => {
        this.carregarStatus();
        this.buscar();
      },
      error: () => {}
    });
  }

  getOpLabel(op: string): string {
    switch (op) {
      case 'I': return 'INSERT';
      case 'U': return 'UPDATE';
      case 'D': return 'DELETE';
      default: return op;
    }
  }

  getOpClass(op: string): string {
    switch (op) {
      case 'I': return 'op-insert';
      case 'U': return 'op-update';
      case 'D': return 'op-delete';
      default: return '';
    }
  }

  getStatusLabel(item: SyncItem): string {
    if (item.erro) return 'Erro';
    if (item.enviado && item.filialOrigemId !== this.status()?.filialCodigo) return 'Recebido';
    if (item.enviado) return 'Enviado';
    return 'Pendente';
  }

  getStatusClass(item: SyncItem): string {
    if (item.erro) return 'status-erro';
    if (item.enviado && item.filialOrigemId !== this.status()?.filialCodigo) return 'status-recebido';
    if (item.enviado) return 'status-enviado';
    return 'status-pendente';
  }

  formatarData(dt: string): string {
    if (!dt) return '-';
    return new Date(dt).toLocaleString('pt-BR');
  }

  private hoje(): string {
    return new Date().toISOString().split('T')[0];
  }

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }
}
