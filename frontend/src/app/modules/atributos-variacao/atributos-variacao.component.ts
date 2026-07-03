import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface ValorAtributo { id?: number; valor: string; hex?: string | null; ordem: number; }
interface AtributoVariacao { id?: number; nome: string; ordem: number; ativo: boolean; valores: ValorAtributo[]; criadoEm?: string; }

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }
type Modo = 'lista' | 'form';

const COLUNAS: ColunaDef[] = [
  { campo: 'ordem', label: 'Ordem', largura: 80, minLargura: 60, padrao: true },
  { campo: 'nome', label: 'Atributo', largura: 220, minLargura: 120, padrao: true },
  { campo: 'valoresPreview', label: 'Valores', largura: 380, minLargura: 160, padrao: true },
  { campo: 'qtdValores', label: 'Qtd.', largura: 70, minLargura: 50, padrao: true },
  { campo: 'ativo', label: 'Ativo', largura: 70, minLargura: 50, padrao: true },
];

@Component({
  selector: 'app-atributos-variacao',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './atributos-variacao.component.html',
  styleUrl: './atributos-variacao.component.scss'
})
export class AtributosVariacaoComponent implements OnInit, OnDestroy {
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_atributosvariacao';

  modo = signal<Modo>('lista');
  registros = signal<AtributoVariacao[]>([]);
  selecionado = signal<AtributoVariacao | null>(null);
  form = signal<AtributoVariacao>(this.novoRegistro());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('ordem');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  private formOriginal: AtributoVariacao | null = null;

  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  private apiUrl = `${environment.apiUrl}/atributos-variacao`;

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() { this.carregar(); }
  ngOnDestroy() {}
  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => { this.registros.set(r.data ?? []); this.carregando.set(false); },
      error: () => { this.carregando.set(false); this.modal.erro('Atributos', 'Erro ao carregar atributos.'); }
    });
  }

  registrosFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();
    const lista = this.registros().filter(r => {
      if (status === 'ativos' && !r.ativo) return false;
      if (status === 'inativos' && r.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(r.nome).includes(termo);
    });
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = this.sortValue(a, col); const vb = this.sortValue(b, col);
      const cmp = typeof va === 'boolean' ? (va === vb ? 0 : va ? -1 : 1) : typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private sortValue(r: AtributoVariacao, campo: string): any {
    if (campo === 'valoresPreview') return this.normalizar(this.previewValores(r));
    if (campo === 'qtdValores') return r.valores?.length ?? 0;
    return (r as any)[campo] ?? '';
  }

  private normalizar(s: string): string { return (s ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim(); }

  getCellValue(r: AtributoVariacao, campo: string): string {
    if (campo === 'valoresPreview') return this.previewValores(r) || '—';
    if (campo === 'qtdValores') return String(r.valores?.length ?? 0);
    const v = (r as any)[campo]; if (typeof v === 'boolean') return v ? 'Sim' : 'Não'; return v ?? '';
  }

  selecionar(r: AtributoVariacao) { this.selecionado.set(r); }
  ordenar(coluna: string) { if (this.sortColuna() === coluna) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc'); else { this.sortColuna.set(coluna); this.sortDirecao.set('asc'); } }
  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }

  private carregarColunas(): ColunaEstado[] { try { const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS); if (json) { const saved: ColunaEstado[] = JSON.parse(json); return COLUNAS.map(def => { const s = saved.find(c => c.campo === def.campo); return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura }; }); } } catch {} return COLUNAS.map(c => ({ ...c, visivel: c.padrao })); }
  private salvarColunasStorage() { localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas())); }
  toggleColunaVisivel(campo: string) { this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c)); this.salvarColunasStorage(); }
  restaurarPadrao() { this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao }))); this.salvarColunasStorage(); }
  iniciarResize(e: MouseEvent, campo: string, largura: number) { e.stopPropagation(); e.preventDefault(); this.resizeState = { campo, startX: e.clientX, startWidth: largura }; document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none'; }
  @HostListener('document:mousemove', ['$event']) onMouseMove(e: MouseEvent) { if (!this.resizeState) return; const delta = e.clientX - this.resizeState.startX; const def = COLUNAS.find(c => c.campo === this.resizeState!.campo); const min = def?.minLargura ?? 50; const nw = Math.max(min, this.resizeState.startWidth + delta); this.colunas.update(cols => cols.map(c => c.campo === this.resizeState!.campo ? { ...c, largura: nw } : c)); }
  @HostListener('document:mouseup') onMouseUp() { if (this.resizeState) { this.salvarColunasStorage(); this.resizeState = null; document.body.style.cursor = ''; document.body.style.userSelect = ''; } }
  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => { const arr = [...cols]; const [moved] = arr.splice(this.dragColIdx!, 1); arr.splice(idx, 0, moved); this.dragColIdx = idx; return arr; });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunasStorage(); }

  // ── Modo lista/form ──────────────────────────────────────────────
  incluir() { this.form.set(this.novoRegistro()); this.formOriginal = this.clonar(this.novoRegistro()); this.modoEdicao.set(false); this.isDirty.set(false); this.erro.set(''); this.modo.set('form'); }
  editar() { const r = this.selecionado(); if (!r?.id) return; this.form.set(this.clonar(r)); this.formOriginal = this.clonar(r); this.modoEdicao.set(true); this.isDirty.set(false); this.erro.set(''); this.modo.set('form'); }
  fechar() { this.modo.set('lista'); this.carregar(); }
  fecharForm() { this.modo.set('lista'); }
  cancelarEdicao() { if (this.formOriginal) { this.form.set(this.clonar(this.formOriginal)); this.isDirty.set(false); } }

  updateForm<K extends keyof AtributoVariacao>(campo: K, valor: AtributoVariacao[K]) { this.form.update(f => ({ ...f, [campo]: valor })); this.isDirty.set(true); }

  // ── Valores (master-detail) ──────────────────────────────────────
  addValor() { this.form.update(f => ({ ...f, valores: [...f.valores, { valor: '', hex: null, ordem: f.valores.length + 1 }] })); this.isDirty.set(true); }
  removeValor(i: number) { this.form.update(f => ({ ...f, valores: f.valores.filter((_, idx) => idx !== i) })); this.isDirty.set(true); }
  updateValor(i: number, campo: keyof ValorAtributo, valor: any) { this.form.update(f => ({ ...f, valores: f.valores.map((v, idx) => idx === i ? { ...v, [campo]: valor } : v) })); this.isDirty.set(true); }

  async salvar() {
    const f = this.form();
    if (!f.nome.trim()) { this.erro.set('Nome do atributo é obrigatório.'); return; }
    if (f.valores.some(v => !v.valor.trim())) { this.erro.set('Todo valor precisa de um texto.'); return; }
    this.erro.set('');
    const body = {
      nome: f.nome.trim(),
      ordem: f.ordem || 0,
      ativo: f.ativo,
      valores: f.valores.map((v, i) => ({ id: v.id, valor: v.valor.trim(), hex: v.hex || null, ordem: v.ordem || i + 1 }))
    };
    this.salvando.set(true);
    const op$ = this.modoEdicao()
      ? this.http.put<any>(`${this.apiUrl}/${f.id}`, body)
      : this.http.post<any>(this.apiUrl, body);
    op$.subscribe({
      next: () => { this.salvando.set(false); this.isDirty.set(false); this.selecionado.set(null); this.carregar(); this.modo.set('lista'); },
      error: (e: any) => { this.erro.set(e?.error?.message || 'Erro ao salvar.'); this.salvando.set(false); }
    });
  }

  async excluir() {
    const r = this.selecionado(); if (!r?.id) return;
    const resultado = await this.modal.confirmar('Confirmar Exclusão', `Deseja excluir "${r.nome}"?`, 'Sim, excluir', 'Não, manter');
    if (!resultado.confirmado) return;
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${r.id}`).subscribe({
      next: async (res: any) => { this.excluindo.set(false); this.selecionado.set(null); this.carregar(); this.modo.set('lista'); if (res?.resultado === 'desativado') await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.'); },
      error: (e: any) => { this.excluindo.set(false); this.modal.erro('Erro', e?.error?.message || 'Erro ao excluir.'); }
    });
  }

  previewValores(a: AtributoVariacao): string {
    const vs = (a.valores ?? []).map(v => v.valor);
    return vs.length <= 8 ? vs.join(', ') : vs.slice(0, 8).join(', ') + ` +${vs.length - 8}`;
  }

  private novoRegistro(): AtributoVariacao { return { nome: '', ordem: 0, ativo: true, valores: [] }; }
  private clonar<T>(obj: T): T { return JSON.parse(JSON.stringify(obj)); }
}
