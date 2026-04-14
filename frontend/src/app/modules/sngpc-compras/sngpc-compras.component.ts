import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface CompraSngpc {
  compraId: number;
  codigo?: string;
  numeroNf: string;
  fornecedorNome: string;
  dataEmissao?: string;
  dataFinalizacao?: string;
  qtdeProdutosSngpc: number;
  qtdeLotesCriados: number;
  quantidadeTotal: number;
  sngpcOptOut: boolean;
  statusSngpc: string;
}

interface CompraSngpcItem {
  produtoId: number;
  produtoNome: string;
  classeTerapeutica?: string;
  numeroLote: string;
  dataFabricacao?: string;
  dataValidade?: string;
  quantidade: number;
  dataEntrada: string;
}

interface CompraSngpcDetalhe {
  compraId: number;
  numeroNf: string;
  fornecedorNome: string;
  dataEmissao?: string;
  dataFinalizacao?: string;
  sngpcOptOut: boolean;
  itens: CompraSngpcItem[];
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'numeroNf',          label: 'NF',              largura: 100, minLargura: 70,  padrao: true },
  { campo: 'fornecedorNome',    label: 'Fornecedor',      largura: 260, minLargura: 150, padrao: true },
  { campo: 'dataEmissao',       label: 'Emissão',         largura: 110, minLargura: 90,  padrao: true },
  { campo: 'dataFinalizacao',   label: 'Finalizada em',   largura: 150, minLargura: 120, padrao: true },
  { campo: 'qtdeProdutosSngpc', label: 'Produtos SNGPC',  largura: 120, minLargura: 90,  padrao: true },
  { campo: 'qtdeLotesCriados',  label: 'Lotes',           largura: 90,  minLargura: 60,  padrao: true },
  { campo: 'quantidadeTotal',   label: 'Qtde Total',      largura: 110, minLargura: 80,  padrao: true },
  { campo: 'statusSngpc',       label: 'Status SNGPC',    largura: 130, minLargura: 100, padrao: true },
];

@Component({
  selector: 'app-sngpc-compras',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sngpc-compras.component.html',
  styleUrl: './sngpc-compras.component.scss'
})
export class SngpcComprasComponent implements OnInit, OnDestroy {
  private api = `${environment.apiUrl}/sngpc/compras-transferencias`;
  private readonly STORAGE_KEY = 'zulex_colunas_sngpc_compras';

  compras = signal<CompraSngpc[]>([]);
  carregando = signal(false);

  // Filtros
  dataInicio = signal<string>(this.hoje(-30));
  dataFim = signal<string>(this.hoje(0));
  filtroStatus = signal<'todas' | 'lancadas' | 'optout'>('todas');

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal<string>('dataFinalizacao');
  sortDirecao = signal<'asc' | 'desc'>('desc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  comprasFiltradas = computed(() => {
    const status = this.filtroStatus();
    let lista = this.compras().filter(c => {
      if (status === 'lancadas' && c.sngpcOptOut) return false;
      if (status === 'optout' && !c.sngpcOptOut) return false;
      return true;
    });
    const col = this.sortColuna(); const dir = this.sortDirecao();
    if (col) {
      lista = [...lista].sort((a, b) => {
        const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
        const cmp = typeof va === 'number' ? va - (vb as number)
          : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
        return dir === 'asc' ? cmp : -cmp;
      });
    }
    return lista;
  });

  // Linha expandida (detalhe inline)
  linhaExpandida = signal<number | null>(null);
  detalhe = signal<CompraSngpcDetalhe | null>(null);
  detalheLoading = signal(false);

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() { this.carregar(); }
  ngOnDestroy() {}

  carregar() {
    this.carregando.set(true);
    const params: string[] = [];
    if (this.dataInicio()) params.push(`dataInicio=${this.dataInicio()}`);
    if (this.dataFim())    params.push(`dataFim=${this.dataFim()}`);
    const qs = params.length ? '?' + params.join('&') : '';
    this.http.get<any>(`${this.api}${qs}`).subscribe({
      next: r => { this.compras.set(r.data ?? []); this.carregando.set(false); },
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

  getCellValue(c: CompraSngpc, campo: string): string {
    switch (campo) {
      case 'dataEmissao':      return c.dataEmissao ? new Date(c.dataEmissao).toLocaleDateString('pt-BR') : '—';
      case 'dataFinalizacao':  return c.dataFinalizacao ? new Date(c.dataFinalizacao).toLocaleString('pt-BR') : '—';
      case 'quantidadeTotal':  return c.quantidadeTotal.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 3 });
      default:                 return (c as any)[campo]?.toString() ?? '—';
    }
  }

  // ── Detalhes (expansão inline) ──────────────────────────────────
  toggleExpandir(c: CompraSngpc) {
    if (this.linhaExpandida() === c.compraId) {
      this.linhaExpandida.set(null);
      this.detalhe.set(null);
      return;
    }
    this.linhaExpandida.set(c.compraId);
    this.detalhe.set(null);
    this.detalheLoading.set(true);
    this.http.get<any>(`${this.api}/${c.compraId}`).subscribe({
      next: r => {
        this.detalhe.set(r.data);
        this.detalheLoading.set(false);
      },
      error: (e: any) => {
        this.detalheLoading.set(false);
        this.modal.erro('Erro', e?.error?.message || 'Erro ao carregar detalhes.');
      }
    });
  }

  async lancarRetroativo(c: CompraSngpc) {
    if (!c.sngpcOptOut) return;
    const r = await this.modal.confirmar(
      'Lançar retroativamente?',
      `Esta compra foi finalizada com opt-out do SNGPC. Confirmar que deseja lançar retroativamente os produtos controlados agora?`,
      'Sim, lançar', 'Cancelar'
    );
    if (!r.confirmado) return;

    this.http.post<any>(`${this.api}/${c.compraId}/lancar-retroativo`, {}).subscribe({
      next: resp => {
        this.modal.sucesso('OK', `${resp.lotesCriados} lote(s) criado(s) retroativamente.`);
        this.carregar();
      },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao lançar retroativamente.')
    });
  }

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
