import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface Status {
  configurado: boolean;
  ativo: boolean;
  provider?: string;
  cnpjCliente?: string;
  idParceiro?: number;
  tokenDefinido: boolean;
  ano: number;
  mes: number;
  requisicoesUsadas: number;
  limiteMensal: number;
  requisicoesDisponiveis: number;
  percentualUsado: number;
  nivelAlerta: 'ok' | 'atencao' | 'critico';
  ultimaChamadaEm?: string;
}

interface Job {
  id: number;
  tipo: number;
  tipoNome: string;
  status: number;
  statusNome: string;
  provider: string;
  dataInicio?: string;
  dataFim?: string;
  totalItens: number;
  itensProcessados: number;
  itensAtualizados: number;
  itensNaoEncontrados: number;
  itensComErro: number;
  requisicoesUsadas: number;
  progresso: number;
  mensagemErro?: string;
  usuarioNome?: string;
  criadoEm: string;
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'id',                label: 'ID',            largura: 70,  minLargura: 50,  padrao: true },
  { campo: 'criadoEm',          label: 'Criado em',     largura: 150, minLargura: 120, padrao: true },
  { campo: 'tipoNome',          label: 'Tipo',          largura: 140, minLargura: 100, padrao: true },
  { campo: 'statusNome',        label: 'Status',        largura: 120, minLargura: 90,  padrao: true },
  { campo: 'progresso',         label: 'Progresso',     largura: 200, minLargura: 140, padrao: true },
  { campo: 'totalItens',        label: 'Total',         largura: 90,  minLargura: 60,  padrao: true },
  { campo: 'itensAtualizados',  label: 'Atualizados',   largura: 110, minLargura: 80,  padrao: true },
  { campo: 'itensNaoEncontrados', label: 'Não encontr.', largura: 110, minLargura: 80, padrao: false },
  { campo: 'itensComErro',      label: 'Erros',         largura: 80,  minLargura: 60,  padrao: false },
  { campo: 'requisicoesUsadas', label: 'Req. usadas',   largura: 100, minLargura: 80,  padrao: false },
  { campo: 'usuarioNome',       label: 'Usuário',       largura: 130, minLargura: 90,  padrao: true },
];

@Component({
  selector: 'app-gestor-tributario',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestor-tributario.component.html',
  styleUrl: './gestor-tributario.component.scss'
})
export class GestorTributarioComponent implements OnInit, OnDestroy {
  private api = `${environment.apiUrl}/gestor-tributario`;
  private readonly STORAGE_KEY = 'zulex_colunas_gestor_tributario';

  status = signal<Status | null>(null);
  jobs = signal<Job[]>([]);
  carregando = signal(false);

  // Colunas padrão
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal<string>('id');
  sortDirecao = signal<'asc' | 'desc'>('desc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  jobsOrdenados = computed(() => {
    const col = this.sortColuna(); const dir = this.sortDirecao();
    const lista = [...this.jobs()];
    if (!col) return lista;
    return lista.sort((a, b) => {
      const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  // Modal revisar base
  modalRevisar = signal(false);
  filtroSomenteSemFiscal = signal(false);
  filtroForcarAtualizacao = signal(false);
  disparandoJob = signal(false);

  // Polling
  private pollingInterval: any = null;

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() {
    this.carregarStatus();
    this.carregarJobs();
    // Polling a cada 3s se tem job executando
    this.pollingInterval = setInterval(() => {
      if (this.jobs().some(j => j.status === 1 || j.status === 2)) {
        this.carregarJobs();
      }
    }, 3000);
  }

  ngOnDestroy() {
    if (this.pollingInterval) clearInterval(this.pollingInterval);
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregarStatus() {
    this.http.get<any>(`${this.api}/status`).subscribe({
      next: r => this.status.set(r.data),
      error: () => {}
    });
  }

  carregarJobs() {
    this.carregando.set(true);
    this.http.get<any>(`${this.api}/jobs?limite=50`).subscribe({
      next: r => { this.jobs.set(r.data ?? []); this.carregando.set(false); this.carregarStatus(); },
      error: () => this.carregando.set(false)
    });
  }

  // ── Modal Revisar Base ───────────────────────────────────────────
  abrirRevisarBase() {
    this.filtroSomenteSemFiscal.set(false);
    this.filtroForcarAtualizacao.set(false);
    this.modalRevisar.set(true);
  }
  fecharRevisar() { this.modalRevisar.set(false); }

  async confirmarRevisarBase() {
    this.disparandoJob.set(true);
    const body = {
      somenteSemFiscal: this.filtroSomenteSemFiscal(),
      forcarAtualizacao: this.filtroForcarAtualizacao()
    };
    this.http.post<any>(`${this.api}/revisar-base`, body).subscribe({
      next: r => {
        this.disparandoJob.set(false);
        this.modal.sucesso('OK', `Job iniciado (ID ${r.data.jobId}). Pode fechar esta tela — o progresso continua em background.`);
        this.modalRevisar.set(false);
        this.carregarJobs();
      },
      error: (e: any) => {
        this.disparandoJob.set(false);
        this.modal.erro('Erro', e?.error?.message || 'Erro ao iniciar job.');
      }
    });
  }

  async cancelarJob(j: Job) {
    const r = await this.modal.confirmar('Cancelar job', `Cancelar o job #${j.id}?`, 'Sim', 'Não');
    if (!r.confirmado) return;
    this.http.post<any>(`${this.api}/jobs/${j.id}/cancelar`, {}).subscribe({
      next: () => this.carregarJobs(),
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao cancelar.')
    });
  }

  // ── Columns ──────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }
  private salvarColunasStorage() { localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.colunas())); }
  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }
  restaurarPadrao() { this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao }))); this.salvarColunasStorage(); }
  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortColuna.set(coluna); this.sortDirecao.set('asc'); }
  }
  sortIcon(campo: string): string {
    return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅';
  }
  iniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    this.resizeState = { campo, startX: e.clientX, startWidth: largura };
    document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none';
  }
  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    if (!this.resizeState) return;
    const delta = e.clientX - this.resizeState.startX;
    const def = COLUNAS.find(c => c.campo === this.resizeState!.campo);
    const min = def?.minLargura ?? 50;
    const novaLargura = Math.max(min, this.resizeState.startWidth + delta);
    this.colunas.update(cols => cols.map(c => c.campo === this.resizeState!.campo ? { ...c, largura: novaLargura } : c));
  }
  @HostListener('document:mouseup')
  onMouseUp() {
    if (this.resizeState) {
      this.salvarColunasStorage();
      this.resizeState = null;
      document.body.style.cursor = ''; document.body.style.userSelect = '';
    }
  }
  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => { const arr = [...cols]; const [m] = arr.splice(this.dragColIdx!, 1); arr.splice(idx, 0, m); this.dragColIdx = idx; return arr; });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunasStorage(); }

  getCellValue(j: Job, campo: string): string {
    switch (campo) {
      case 'criadoEm': return j.criadoEm ? new Date(j.criadoEm).toLocaleString('pt-BR') : '—';
      default:         return (j as any)[campo]?.toString() ?? '—';
    }
  }
}
