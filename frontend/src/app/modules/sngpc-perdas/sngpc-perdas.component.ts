import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface Perda {
  id: number;
  filialId: number;
  produtoId: number;
  produtoNome?: string;
  produtoLoteId: number;
  numeroLote?: string;
  dataValidade?: string;
  quantidade: number;
  dataPerda: string;
  motivo: number;
  motivoNome: string;
  numeroBoletim?: string;
  observacao?: string;
  usuarioNome?: string;
}

interface LoteAtivo { produtoLoteId: number; numeroLote: string; dataValidade?: string; saldoAtual: number; }

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'id',            label: 'ID',           largura: 70,  minLargura: 50,  padrao: true },
  { campo: 'dataPerda',     label: 'Data',         largura: 110, minLargura: 90,  padrao: true },
  { campo: 'produtoNome',   label: 'Produto',      largura: 260, minLargura: 150, padrao: true },
  { campo: 'numeroLote',    label: 'Lote',         largura: 110, minLargura: 80,  padrao: true },
  { campo: 'dataValidade',  label: 'Validade',     largura: 110, minLargura: 80,  padrao: false },
  { campo: 'quantidade',    label: 'Qtde',         largura: 90,  minLargura: 70,  padrao: true },
  { campo: 'motivoNome',    label: 'Motivo',       largura: 130, minLargura: 90,  padrao: true },
  { campo: 'numeroBoletim', label: 'BO',           largura: 100, minLargura: 70,  padrao: true },
  { campo: 'usuarioNome',   label: 'Usuário',      largura: 140, minLargura: 90,  padrao: false },
  { campo: 'observacao',    label: 'Observação',   largura: 220, minLargura: 120, padrao: false },
];

const MOTIVOS = [
  { valor: 1, nome: 'Furto', exigeBo: true },
  { valor: 2, nome: 'Roubo', exigeBo: true },
  { valor: 3, nome: 'Avaria', exigeBo: false },
  { valor: 4, nome: 'Vencimento', exigeBo: false },
  { valor: 5, nome: 'Quebra', exigeBo: false },
  { valor: 6, nome: 'Outro', exigeBo: false }
];

@Component({
  selector: 'app-sngpc-perdas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sngpc-perdas.component.html',
  styleUrl: './sngpc-perdas.component.scss'
})
export class SngpcPerdasComponent implements OnInit, OnDestroy {
  private api = `${environment.apiUrl}/sngpc/perdas`;
  private produtosApi = `${environment.apiUrl}/produtos`;
  private estoqueApi = `${environment.apiUrl}/sngpc/estoque`;
  private readonly STORAGE_KEY = 'zulex_colunas_sngpc_perdas';

  perdas = signal<Perda[]>([]);
  carregando = signal(false);
  readonly motivos = MOTIVOS;

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal<string>('dataPerda');
  sortDirecao = signal<'asc' | 'desc'>('desc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  perdasOrdenadas = computed(() => {
    const col = this.sortColuna(); const dir = this.sortDirecao();
    const lista = [...this.perdas()];
    if (!col) return lista;
    return lista.sort((a, b) => {
      const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  // Modal nova perda
  modalAberto = signal(false);
  filialId = signal(1);
  produtoBusca = signal('');
  produtoResultados = signal<any[]>([]);
  produtoSelecionadoId = signal<number | null>(null);
  produtoSelecionadoNome = signal<string>('');
  lotes = signal<LoteAtivo[]>([]);
  loteId = signal<number | null>(null);
  quantidade = signal(0);
  dataPerda = signal(new Date().toISOString().slice(0, 10));
  motivo = signal(4);
  numeroBoletim = signal('');
  observacao = signal('');

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() { this.carregar(); }
  ngOnDestroy() {}

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.api).subscribe({
      next: r => { this.perdas.set(r.data ?? []); this.carregando.set(false); },
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
  restaurarPadrao() {
    this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasStorage();
  }
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

  // ── Cell values ─────────────────────────────────────────────────
  getCellValue(p: Perda, campo: string): string {
    switch (campo) {
      case 'dataPerda':    return p.dataPerda ? new Date(p.dataPerda).toLocaleDateString('pt-BR') : '—';
      case 'dataValidade': return p.dataValidade ? new Date(p.dataValidade).toLocaleDateString('pt-BR') : '—';
      case 'quantidade':   return p.quantidade.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 3 });
      default:             return (p as any)[campo]?.toString() ?? '—';
    }
  }

  // ── Modal ───────────────────────────────────────────────────────
  abrirNovaPerda() {
    this.produtoBusca.set(''); this.produtoResultados.set([]);
    this.produtoSelecionadoId.set(null); this.produtoSelecionadoNome.set('');
    this.lotes.set([]); this.loteId.set(null);
    this.quantidade.set(0);
    this.dataPerda.set(new Date().toISOString().slice(0, 10));
    this.motivo.set(4); this.numeroBoletim.set(''); this.observacao.set('');
    this.modalAberto.set(true);
  }
  fecharModal() { this.modalAberto.set(false); }

  onProdutoInput(valor: string) {
    this.produtoBusca.set(valor);
    if (valor.length < 2) { this.produtoResultados.set([]); return; }
    this.http.get<any>(`${this.produtosApi}/pesquisar?termo=${encodeURIComponent(valor)}`).subscribe({
      next: r => this.produtoResultados.set((r.data ?? []).slice(0, 15))
    });
  }
  selecionarProduto(p: any) {
    this.produtoSelecionadoId.set(p.id); this.produtoSelecionadoNome.set(p.nome);
    this.produtoBusca.set(p.nome); this.produtoResultados.set([]);
    this.http.get<any>(`${this.estoqueApi}?filialId=${this.filialId()}`).subscribe({
      next: r => {
        const lotes = (r.data ?? []).filter((l: any) => l.produtoId === p.id);
        this.lotes.set(lotes.map((l: any) => ({ produtoLoteId: l.produtoLoteId, numeroLote: l.numeroLote, dataValidade: l.dataValidade, saldoAtual: l.saldoAtual })));
      }
    });
  }

  get motivoAtual() { return MOTIVOS.find(m => m.valor === this.motivo()); }

  salvarPerda() {
    if (!this.produtoSelecionadoId()) { this.modal.aviso('Produto', 'Selecione um produto.'); return; }
    if (!this.loteId()) { this.modal.aviso('Lote', 'Selecione um lote.'); return; }
    if (this.quantidade() <= 0) { this.modal.aviso('Qtde', 'Informe uma quantidade válida.'); return; }
    if (this.motivoAtual?.exigeBo && !this.numeroBoletim().trim()) {
      this.modal.aviso('BO', 'Informe o número do Boletim de Ocorrência para Furto ou Roubo.');
      return;
    }
    const body = {
      filialId: this.filialId(),
      produtoId: this.produtoSelecionadoId(),
      produtoLoteId: this.loteId(),
      quantidade: this.quantidade(),
      dataPerda: this.dataPerda(),
      motivo: this.motivo(),
      numeroBoletim: this.numeroBoletim() || null,
      observacao: this.observacao() || null
    };
    this.http.post<any>(this.api, body).subscribe({
      next: () => { this.modalAberto.set(false); this.carregar(); },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao registrar perda.')
    });
  }

  async excluir(p: Perda) {
    const r = await this.modal.confirmar('Excluir perda', `Excluir a perda #${p.id}? O saldo será estornado ao lote.`, 'Sim', 'Não');
    if (!r.confirmado) return;
    this.http.delete(`${this.api}/${p.id}`).subscribe({
      next: () => this.carregar(),
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao excluir.')
    });
  }
}
