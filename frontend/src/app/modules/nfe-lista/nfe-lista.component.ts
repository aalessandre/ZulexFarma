import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface NfeList {
  id: number;
  codigo?: string;
  numero: number;
  serie: number;
  natOp: string;
  destinatarioNome?: string;
  dataEmissao: string;
  valorNota: number;
  status: number; // 0=NaoEmitido, 1=Rascunho, 2=Enviado, 3=Autorizado, 4=Rejeitado, 5=Cancelado, 6=Inutilizado
  chaveAcesso: string;
}

interface ColunaDef {
  campo: string;
  label: string;
  largura: number;
  minLargura: number;
  padrao: boolean;
}

interface ColunaEstado extends ColunaDef {
  visivel: boolean;
}

const COLUNAS: ColunaDef[] = [
  { campo: 'numero', label: 'Numero', largura: 80, minLargura: 60, padrao: true },
  { campo: 'serie', label: 'Serie', largura: 60, minLargura: 40, padrao: true },
  { campo: 'natOp', label: 'Natureza', largura: 200, minLargura: 100, padrao: true },
  { campo: 'destinatarioNome', label: 'Destinatario', largura: 200, minLargura: 100, padrao: true },
  { campo: 'dataEmissao', label: 'Emissao', largura: 120, minLargura: 80, padrao: true },
  { campo: 'valorNota', label: 'Valor', largura: 100, minLargura: 70, padrao: true },
  { campo: 'status', label: 'Status', largura: 100, minLargura: 70, padrao: true },
];

const STATUS_LABELS: Record<number, string> = {
  0: 'Nao Emitido',
  1: 'Rascunho',
  2: 'Enviada',
  3: 'Autorizada',
  4: 'Rejeitada',
  5: 'Cancelada',
  6: 'Inutilizada',
};

@Component({
  selector: 'app-nfe-lista',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './nfe-lista.component.html',
  styleUrl: './nfe-lista.component.scss'
})
export class NfeListaComponent implements OnInit, OnDestroy {
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_nfe_lista_v1';

  notas = signal<NfeList[]>([]);
  notaSelecionada = signal<NfeList | null>(null);
  carregando = signal(false);
  busca = signal('');
  filtroStatus = signal<'todos' | '0' | '1' | '2' | '3' | '4' | '5' | '6'>('todos');
  sortColuna = signal<string>('numero');
  sortDirecao = signal<'asc' | 'desc'>('desc');

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  private apiUrl = `${environment.apiUrl}/venda-fiscal`;
  private tokenLiberacao: string | null = null;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService,
    private router: Router
  ) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('nfe', acao)) return true;
    const resultado = await this.modal.permissao('nfe', acao);
    if (resultado.tokenLiberacao) this.tokenLiberacao = resultado.tokenLiberacao;
    return resultado.confirmado;
  }

  private headerLiberacao(): { [h: string]: string } {
    if (this.tokenLiberacao) {
      const h = { 'X-Liberacao': this.tokenLiberacao };
      this.tokenLiberacao = null;
      return h;
    }
    return {};
  }

  private readonly TAB_ID = '/erp/nfe-lista';

  ngOnInit() {
    if (!this.tabService.tabs().find(t => t.id === this.TAB_ID)) {
      this.tabService.abrirTab({ id: this.TAB_ID, titulo: 'NF-e Emitidas', rota: this.TAB_ID, iconKey: 'fiscal' });
    }
    this.carregar();
  }

  ngOnDestroy() {}

  @HostListener('document:keydown', ['$event'])
  onKeydown(e: KeyboardEvent) {
    if (this.modal.visivel()) return;
    if (e.key === 'Escape') {
      (e as any).__handled = true;
    }
    if (e.key === 'Enter' && this.notaSelecionada()) {
      const el = e.target as HTMLElement;
      if (el?.tagName === 'INPUT' || el?.tagName === 'SELECT' || el?.tagName === 'TEXTAREA') return;
      e.preventDefault();
      this.onDblClick(this.notaSelecionada()!);
    }
    if ((e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
      const el = e.target as HTMLElement;
      if (el?.classList?.contains('input-busca')) return;
      e.preventDefault();
      const lista = this.notasFiltradas();
      if (lista.length === 0) return;
      const atual = this.notaSelecionada();
      const idx = atual ? lista.findIndex(f => f.id === atual.id) : -1;
      const novoIdx = e.key === 'ArrowDown' ? Math.min(idx + 1, lista.length - 1) : Math.max(idx - 1, 0);
      this.selecionar(lista[novoIdx]);
      setTimeout(() => {
        const row = document.querySelector('.erp-grid tbody tr.selecionado') as HTMLElement;
        if (row) row.scrollIntoView({ block: 'nearest' });
      });
    }
  }

  // ── Data ───────────────────────────────────────────────────────────
  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.notas.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('nfe', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  notasFiltradas = computed(() => {
    const termo = this.normalizar(this.busca());
    const statusFiltro = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.notas().filter(n => {
      if (statusFiltro !== 'todos' && n.status !== +statusFiltro) return false;
      if (termo.length < 2) return true;
      return this.normalizar(n.natOp).includes(termo)
        || this.normalizar(n.destinatarioNome ?? '').includes(termo)
        || String(n.numero).includes(termo)
        || (n.chaveAcesso ?? '').includes(termo);
    });

    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number'
        ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(n: NfeList, campo: string): string {
    if (campo === 'status') return STATUS_LABELS[n.status] ?? String(n.status);
    if (campo === 'dataEmissao') {
      if (!n.dataEmissao) return '';
      const d = new Date(n.dataEmissao);
      const pad = (v: number) => String(v).padStart(2, '0');
      return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
    }
    if (campo === 'valorNota') {
      return 'R$ ' + (n.valorNota ?? 0).toFixed(2).replace('.', ',');
    }
    const v = (n as any)[campo];
    return v ?? '';
  }

  getStatusClass(n: NfeList): string {
    const map: Record<number, string> = {
      0: 'status-rascunho',
      1: 'status-rascunho',
      2: 'status-enviada',
      3: 'status-autorizada',
      4: 'status-rejeitada',
      5: 'status-cancelada',
      6: 'status-inutilizada',
    };
    return map[n.status] ?? '';
  }

  selecionar(n: NfeList) { this.notaSelecionada.set(n); }

  onDblClick(n: NfeList) {
    if (n.status === 1 || n.status === 4) {
      // Rascunho ou Rejeitado → edita
      this.navegarEmissao(n.id);
    } else if (n.status === 3) {
      // Autorizado → DANFE
      this.abrirDanfe(n.id);
    }
  }

  // ── Sidebar actions ────────────────────────────────────────────────
  novaNfe() {
    this.tabService.abrirTab({
      id: '/erp/nfe-emissao',
      titulo: 'Emitir NF-e',
      rota: '/erp/nfe-emissao',
      iconKey: 'fiscal',
    });
  }

  editarNfe() {
    const n = this.notaSelecionada();
    if (!n) return;
    if (n.status !== 1 && n.status !== 4) {
      this.modal.aviso('Acao nao permitida', 'Somente notas em rascunho ou rejeitadas podem ser editadas.');
      return;
    }
    this.navegarEmissao(n.id);
  }

  private navegarEmissao(id: number) {
    this.tabService.abrirTab({
      id: `/erp/nfe-emissao?id=${id}`,
      titulo: 'Editar NF-e',
      rota: `/erp/nfe-emissao?id=${id}`,
      iconKey: 'fiscal',
    });
  }

  abrirDanfe(id?: number) {
    const nfeId = id ?? this.notaSelecionada()?.id;
    if (!nfeId) return;
    window.open(`${environment.apiUrl}/venda-fiscal/${nfeId}/danfe`, '_blank');
  }

  async cancelarNfe() {
    const n = this.notaSelecionada();
    if (!n) return;
    if (n.status !== 3) {
      this.modal.aviso('Acao nao permitida', 'Somente notas autorizadas podem ser canceladas.');
      return;
    }
    const resultado = await this.modal.confirmar(
      'Cancelar NF-e',
      `Deseja cancelar a NF-e numero ${n.numero}? Esta acao e irreversivel.`,
      'Sim, cancelar',
      'Nao, manter'
    );
    if (!resultado.confirmado) return;

    this.carregando.set(true);
    const headers = this.headerLiberacao();
    this.http.post<any>(`${this.apiUrl}/${n.id}/cancelar`, {}, { headers }).subscribe({
      next: () => {
        this.carregando.set(false);
        this.carregar();
      },
      error: (err) => {
        this.carregando.set(false);
        this.modal.erro('Erro', err.error?.message ?? 'Erro ao cancelar NF-e.');
      }
    });
  }

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }

  // ── Sort / Columns ─────────────────────────────────────────────────
  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string {
    return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '\u25B2' : '\u25BC') : '\u21C5';
  }

  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
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

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasStorage();
  }

  iniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    this.resizeState = { campo, startX: e.clientX, startWidth: largura };
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    if (!this.resizeState) return;
    const delta = e.clientX - this.resizeState.startX;
    const def = COLUNAS.find(c => c.campo === this.resizeState!.campo);
    const min = def?.minLargura ?? 50;
    const novaLargura = Math.max(min, this.resizeState.startWidth + delta);
    this.colunas.update(cols =>
      cols.map(c => c.campo === this.resizeState!.campo ? { ...c, largura: novaLargura } : c)
    );
  }

  @HostListener('document:mouseup')
  onMouseUp() {
    if (this.resizeState) {
      this.salvarColunasStorage();
      this.resizeState = null;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    }
  }

  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => {
      const arr = [...cols];
      const [moved] = arr.splice(this.dragColIdx!, 1);
      arr.splice(idx, 0, moved);
      this.dragColIdx = idx;
      return arr;
    });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunasStorage(); }
}
