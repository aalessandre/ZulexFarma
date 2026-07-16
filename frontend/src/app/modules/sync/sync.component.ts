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
  noOrigemId: number;
  filialDonoId: number | null;
  criadoEm: string;
  enviado: boolean;
  enviadoEm: string | null;
  erro: string | null;
}

interface QuarentenaItem {
  id: number;
  tabela: string;
  operacao: string;
  registroId: number;
  motivo: string;        // PrecisaRetry | Conflito | TipoDesconhecido | Erro
  tentativas: number;
  ultimoErro: string | null;
  opCriadoEm: string;
  noOrigemId: number;
  criadoEm: string;
  atualizadoEm: string;
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

  // Quarentena (dead-letter do recebimento)
  quarentena = signal<QuarentenaItem[]>([]);

  // Filtros
  dataInicio = signal(this.hoje());
  dataFim = signal(this.hoje());
  filtroStatus = signal<'todos' | 'pendentes' | 'enviados' | 'recebidos' | 'erros' | 'quarentena'>('todos');
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
    if (this.filtroStatus() === 'quarentena') { this.buscarQuarentena(); return; }
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

  buscarQuarentena() {
    this.carregando.set(true);
    const params = new URLSearchParams();
    params.set('tabela', this.filtroTabela());
    params.set('pagina', this.pagina().toString());
    params.set('porPagina', this.porPagina().toString());

    this.http.get<any>(`${this.apiUrl}/quarentena?${params.toString()}`).subscribe({
      next: r => {
        this.carregando.set(false);
        this.quarentena.set(r?.data?.registros ?? []);
        this.totalRegistros.set(r?.data?.total ?? 0);
      },
      error: () => {
        this.carregando.set(false);
        this.quarentena.set([]);
      }
    });
  }

  reprocessarQuarentena(id?: number) {
    const url = id
      ? `${this.apiUrl}/quarentena/reprocessar?id=${id}`
      : `${this.apiUrl}/quarentena/reprocessar`;
    this.http.post<any>(url, {}).subscribe({
      next: () => { this.carregarStatus(); this.buscar(); },
      error: () => {}
    });
  }

  presoQuarentena(item: QuarentenaItem): boolean {
    // espelha o teto do backend (PrecisaRetry tem cap alto; o resto, baixo)
    return item.motivo === 'PrecisaRetry' ? item.tentativas >= 240 : item.tentativas >= 5;
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
    // Sem ?dias= fixo: o backend usa a config sync.limpeza.dias (o slider da tela de Configuracoes).
    this.http.post<any>(`${this.apiUrl}/limpar`, {}).subscribe({
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
    if (item.enviado && item.noOrigemId !== this.status()?.filialCodigo) return 'Recebido';
    if (item.enviado) return 'Enviado';
    return 'Pendente';
  }

  getStatusClass(item: SyncItem): string {
    if (item.erro) return 'status-erro';
    if (item.enviado && item.noOrigemId !== this.status()?.filialCodigo) return 'status-recebido';
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
