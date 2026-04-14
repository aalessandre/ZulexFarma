import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';

interface Linha {
  produtoId: number;
  produtoNome: string;
  produtoCodigoBarras?: string;
  classeTerapeutica?: string;
  produtoLoteId: number;
  numeroLote: string;
  dataFabricacao?: string;
  dataValidade?: string;
  saldoAtual: number;
  ehLoteFicticio: boolean;
  diasParaVencer: number;
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'produtoNome',       label: 'Produto',     largura: 260, minLargura: 150, padrao: true },
  { campo: 'classeTerapeutica', label: 'Classe',      largura: 130, minLargura: 100, padrao: true },
  { campo: 'numeroLote',        label: 'Lote',        largura: 120, minLargura: 80,  padrao: true },
  { campo: 'dataFabricacao',    label: 'Fabricação',  largura: 110, minLargura: 90,  padrao: false },
  { campo: 'dataValidade',      label: 'Validade',    largura: 110, minLargura: 90,  padrao: true },
  { campo: 'saldoAtual',        label: 'Saldo',       largura: 100, minLargura: 80,  padrao: true },
  { campo: 'situacao',          label: 'Situação',    largura: 160, minLargura: 120, padrao: true },
];

@Component({
  selector: 'app-sngpc-estoque',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sngpc-estoque.component.html',
  styleUrl: './sngpc-estoque.component.scss'
})
export class SngpcEstoqueComponent implements OnInit, OnDestroy {
  private api = `${environment.apiUrl}/sngpc/estoque`;
  private readonly STORAGE_KEY = 'zulex_colunas_sngpc_estoque';

  linhas = signal<Linha[]>([]);
  carregando = signal(false);
  filtro = signal<string>('');
  filtroClasse = signal<string>('');
  incluirVencidos = signal(true);

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal<string>('dataValidade');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  linhasFiltradas = computed(() => {
    const termo = this.filtro().toLowerCase().trim();
    const classe = this.filtroClasse();
    let lista = this.linhas().filter(l => {
      if (classe && l.classeTerapeutica !== classe) return false;
      if (termo && !l.produtoNome.toLowerCase().includes(termo) && !l.numeroLote.toLowerCase().includes(termo)) return false;
      return true;
    });
    const col = this.sortColuna();
    const dir = this.sortDirecao();
    if (col) {
      lista = [...lista].sort((a, b) => {
        const va = (a as any)[col] ?? '';
        const vb = (b as any)[col] ?? '';
        const cmp = typeof va === 'number' ? va - (vb as number)
          : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
        return dir === 'asc' ? cmp : -cmp;
      });
    }
    return lista;
  });

  totais = computed(() => {
    const l = this.linhasFiltradas();
    return {
      lotes: l.length,
      produtos: new Set(l.map(x => x.produtoId)).size,
      qtde: l.reduce((s, x) => s + x.saldoAtual, 0),
      vencidos: l.filter(x => x.diasParaVencer < 0).length,
      vencendo30: l.filter(x => x.diasParaVencer >= 0 && x.diasParaVencer <= 30).length
    };
  });

  constructor(private http: HttpClient, private tabService: TabService) {}

  ngOnInit() { this.carregar(); }
  ngOnDestroy() {}

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.api}?incluirVencidos=${this.incluirVencidos()}`).subscribe({
      next: r => { this.linhas.set(r.data ?? []); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

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

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasStorage();
  }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string {
    return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅';
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
    this.colunas.update(cols => cols.map(c => c.campo === this.resizeState!.campo ? { ...c, largura: novaLargura } : c));
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

  // ── Cell helpers ─────────────────────────────────────────────────
  getCellValue(l: Linha, campo: string): string {
    switch (campo) {
      case 'dataFabricacao': return l.dataFabricacao ? new Date(l.dataFabricacao).toLocaleDateString('pt-BR') : '—';
      case 'dataValidade':   return l.dataValidade ? new Date(l.dataValidade).toLocaleDateString('pt-BR') : '—';
      case 'saldoAtual':     return l.saldoAtual.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 3 });
      case 'classeTerapeutica': return l.classeTerapeutica ?? '—';
      default: return (l as any)[campo]?.toString() ?? '—';
    }
  }

  statusValidade(l: Linha): string {
    if (l.diasParaVencer === 2147483647) return 'sem-val';
    if (l.diasParaVencer < 0) return 'vencido';
    if (l.diasParaVencer <= 30) return 'vence-breve';
    if (l.diasParaVencer <= 90) return 'atencao';
    return 'ok';
  }

  statusTexto(l: Linha): string {
    switch (this.statusValidade(l)) {
      case 'vencido':     return `Vencido há ${-l.diasParaVencer} dias`;
      case 'vence-breve': return `Vence em ${l.diasParaVencer} dias`;
      case 'atencao':     return `${l.diasParaVencer} dias`;
      case 'sem-val':     return 'Sem validade';
      default:            return `OK (${l.diasParaVencer} dias)`;
    }
  }
}
