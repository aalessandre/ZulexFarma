import { Component, signal, computed, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface Promo { id: number; nome: string; dataHoraInicio: string; dataHoraFim?: string; totalProdutos: number; ativo: boolean; }
interface PromoDetalhe {
  id: number; nome: string; dataHoraInicio: string; dataHoraFim?: string; diaSemana: number;
  gerarComissao: boolean; exclusivaConvenio: boolean; intersabores: boolean;
  ativo: boolean; filialIds: number[]; pagamentoIds: number[]; convenioIds: number[];
  faixas: Faixa[]; produtos: PromoProd[];
}
interface Faixa { quantidade: number; percentualDesconto: number; }
interface PromoProd { id: number; produtoId: number; produtoCodigo?: string; produtoNome: string; fabricante?: string; precoVenda: number; estoqueAtual: number; }
interface LookupItem { id: number; nome: string; }

type Modo = 'lista' | 'config' | 'produtos';

const DIAS = [
  { label: 'Dom', valor: 1 }, { label: 'Seg', valor: 2 }, { label: 'Ter', valor: 4 },
  { label: 'Qua', valor: 8 }, { label: 'Qui', valor: 16 }, { label: 'Sex', valor: 32 }, { label: 'Sáb', valor: 64 }
];

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const LISTA_COLUNAS: ColunaDef[] = [
  { campo: 'nome', label: 'Nome', largura: 240, minLargura: 120, padrao: true },
  { campo: 'dataHoraInicio', label: 'Início', largura: 150, minLargura: 100, padrao: true },
  { campo: 'dataHoraFim', label: 'Fim', largura: 150, minLargura: 100, padrao: true },
  { campo: 'totalProdutos', label: 'Produtos', largura: 80, minLargura: 60, padrao: true },
  { campo: 'ativo', label: 'Ativo', largura: 60, minLargura: 50, padrao: true },
];

const PROD_COLUNAS: ColunaDef[] = [
  { campo: 'produtoCodigo', label: 'Código', largura: 80, minLargura: 60, padrao: true },
  { campo: 'produtoNome', label: 'Produto', largura: 240, minLargura: 120, padrao: true },
  { campo: 'fabricante', label: 'Fabricante', largura: 140, minLargura: 80, padrao: true },
  { campo: 'estoqueAtual', label: 'Estoque', largura: 80, minLargura: 50, padrao: true },
  { campo: 'precoVenda', label: 'Preço Venda', largura: 110, minLargura: 70, padrao: true },
];

@Component({
  selector: 'app-promocao-progressiva',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './promocao-progressiva.component.html',
  styleUrl: './promocao-progressiva.component.scss'
})
export class PromocaoProgressivaComponent implements OnInit {
  private readonly LISTA_COLUNAS_KEY = 'zulex_colunas_promo_prog_lista';
  private readonly PROD_COLUNAS_KEY = 'zulex_colunas_promo_prog_prod';

  modo = signal<Modo>('lista');
  registros = signal<Promo[]>([]);
  selecionado = signal<Promo | null>(null);
  detalhe = signal<PromoDetalhe | null>(null);
  carregando = signal(false);
  salvando = signal(false);
  modoEdicao = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  dias = DIAS;

  // ── Colunas lista (padrão Fabricantes) ────────────────────────────
  listaColunas = signal<ColunaEstado[]>(this.carregarColunas(this.LISTA_COLUNAS_KEY, LISTA_COLUNAS));
  listaColVisiveis = computed(() => this.listaColunas().filter(c => c.visivel));
  painelColLista = signal(false);
  sortColuna = signal<string>('nome');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  private listaResizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private listaDragIdx: number | null = null;

  // ── Colunas produtos (padrão Fabricantes) ─────────────────────────
  prodColunas = signal<ColunaEstado[]>(this.carregarColunas(this.PROD_COLUNAS_KEY, PROD_COLUNAS));
  prodColVisiveis = computed(() => this.prodColunas().filter(c => c.visivel));
  painelColProd = signal(false);
  sortProdCol = signal<string>('produtoNome');
  sortProdDir = signal<'asc' | 'desc'>('asc');
  private prodResizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private prodDragIdx: number | null = null;

  // Config
  fNome = signal('');
  fDataInicio = signal('');
  fDataFim = signal('');
  fDiaSemana = signal(127);
  fGerarComissao = signal(false);
  fExclusivaConvenio = signal(false);
  fIntersabores = signal(false);
  fAtivo = signal(true);
  fFilialIds = signal<Set<number>>(new Set());
  fPagamentoIds = signal<Set<number>>(new Set());
  fConvenioIds = signal<number[]>([]);

  // Faixas
  faixas = signal<Faixa[]>([]);

  // Lookups
  filiais = signal<LookupItem[]>([]);
  tiposPagamento = signal<LookupItem[]>([]);
  convenios = signal<LookupItem[]>([]);

  // Produtos
  produtos = signal<PromoProd[]>([]);
  produtosSelecionados = signal<Set<number>>(new Set());
  todosProdutosSelecionados = computed(() => this.produtos().length > 0 && this.produtosSelecionados().size === this.produtos().length);
  produtoBusca = signal('');
  produtoResultados = signal<any[]>([]);
  produtoDropdown = signal(false);
  private produtoTimer: any = null;

  private apiUrl = `${environment.apiUrl}/promocoes`;
  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  ngOnInit() { this.carregar(); this.carregarLookups(); }
  sairDaTela() { this.tabService.fecharTabAtiva(); }

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('promocoes', acao)) return true;
    const r = await this.modal.permissao('promocoes', acao);
    if (r.tokenLiberacao) this.tokenLiberacao = r.tokenLiberacao;
    return r.confirmado;
  }
  private headerLiberacao(): { [h: string]: string } { if (this.tokenLiberacao) { const h = { 'X-Liberacao': this.tokenLiberacao }; this.tokenLiberacao = null; return h; } return {}; }

  private carregarLookups() {
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({ next: r => this.filiais.set((r.data ?? []).map((f: any) => ({ id: f.id, nome: f.nomeFilial }))) });
    this.http.get<any>(`${environment.apiUrl}/tipospagamento`).subscribe({ next: r => this.tiposPagamento.set((r.data ?? []).filter((t: any) => t.ativo).map((t: any) => ({ id: t.id, nome: t.nome }))) });
    this.http.get<any>(`${environment.apiUrl}/convenios`).subscribe({ next: r => this.convenios.set((r.data ?? []).filter((c: any) => c.ativo).map((c: any) => ({ id: c.id, nome: c.pessoaNome }))) });
  }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => { this.registros.set((r.data ?? []).filter((p: any) => p.tipo === 2)); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  registrosFiltrados = computed(() => {
    const termo = this.normalizar(this.busca()); const status = this.filtroStatus();
    const col = this.sortColuna(); const dir = this.sortDirecao();
    const lista = this.registros().filter(r => {
      if (status === 'ativos' && !r.ativo) return false;
      if (status === 'inativos' && r.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(r.nome).includes(termo);
    });
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean' ? (va === vb ? 0 : va ? -1 : 1) : typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string { return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim(); }

  // ── Métodos grid compartilhados ───────────────────────────────────
  private carregarColunas(key: string, defaults: ColunaDef[]): ColunaEstado[] {
    try { const json = localStorage.getItem(key); if (json) { const saved: ColunaEstado[] = JSON.parse(json); return defaults.map(def => { const s = saved.find(c => c.campo === def.campo); return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura }; }); } } catch {}
    return defaults.map(c => ({ ...c, visivel: c.padrao }));
  }
  private salvarColunas(key: string, cols: ColunaEstado[]) { localStorage.setItem(key, JSON.stringify(cols)); }

  // Lista grid
  getCellValue(r: Promo, campo: string): string {
    if (campo === 'dataHoraInicio' || campo === 'dataHoraFim') { const v = (r as any)[campo]; return v ? new Date(v).toLocaleString('pt-BR') : 'Sem prazo'; }
    const v = (r as any)[campo]; if (typeof v === 'boolean') return v ? 'Sim' : 'Não'; return v ?? '';
  }
  ordenar(col: string) { if (this.sortColuna() === col) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc'); else { this.sortColuna.set(col); this.sortDirecao.set('asc'); } }
  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }
  toggleColLista(campo: string) { this.listaColunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c)); this.salvarColunas(this.LISTA_COLUNAS_KEY, this.listaColunas()); }
  restaurarColLista() { this.listaColunas.set(LISTA_COLUNAS.map(c => ({ ...c, visivel: c.padrao }))); this.salvarColunas(this.LISTA_COLUNAS_KEY, this.listaColunas()); }
  iniciarResizeLista(e: MouseEvent, campo: string, largura: number) { e.stopPropagation(); e.preventDefault(); this.listaResizeState = { campo, startX: e.clientX, startWidth: largura }; document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none'; }
  onDragStartLista(idx: number) { this.listaDragIdx = idx; }
  onDragOverLista(event: DragEvent, idx: number) { event.preventDefault(); if (this.listaDragIdx === null || this.listaDragIdx === idx) return; this.listaColunas.update(cols => { const arr = [...cols]; const [moved] = arr.splice(this.listaDragIdx!, 1); arr.splice(idx, 0, moved); this.listaDragIdx = idx; return arr; }); }
  onDropLista() { this.listaDragIdx = null; this.salvarColunas(this.LISTA_COLUNAS_KEY, this.listaColunas()); }

  // Produtos grid
  getProdCellValue(p: PromoProd, campo: string): string {
    if (campo === 'precoVenda') return ((p as any)[campo] ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    return (p as any)[campo] ?? '';
  }
  ordenarProd(col: string) { if (this.sortProdCol() === col) this.sortProdDir.update(d => d === 'asc' ? 'desc' : 'asc'); else { this.sortProdCol.set(col); this.sortProdDir.set('asc'); } }
  sortProdIcon(campo: string): string { return this.sortProdCol() === campo ? (this.sortProdDir() === 'asc' ? '▲' : '▼') : '⇅'; }
  toggleColProd(campo: string) { this.prodColunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c)); this.salvarColunas(this.PROD_COLUNAS_KEY, this.prodColunas()); }
  restaurarColProd() { this.prodColunas.set(PROD_COLUNAS.map(c => ({ ...c, visivel: c.padrao }))); this.salvarColunas(this.PROD_COLUNAS_KEY, this.prodColunas()); }
  iniciarResizeProd(e: MouseEvent, campo: string, largura: number) { e.stopPropagation(); e.preventDefault(); this.prodResizeState = { campo, startX: e.clientX, startWidth: largura }; document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none'; }
  onDragStartProd(idx: number) { this.prodDragIdx = idx; }
  onDragOverProd(event: DragEvent, idx: number) { event.preventDefault(); if (this.prodDragIdx === null || this.prodDragIdx === idx) return; this.prodColunas.update(cols => { const arr = [...cols]; const [moved] = arr.splice(this.prodDragIdx!, 1); arr.splice(idx, 0, moved); this.prodDragIdx = idx; return arr; }); }
  onDropProd() { this.prodDragIdx = null; this.salvarColunas(this.PROD_COLUNAS_KEY, this.prodColunas()); }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    const rs = this.listaResizeState || this.prodResizeState;
    if (!rs) return;
    const delta = e.clientX - rs.startX;
    const nw = Math.max(40, rs.startWidth + delta);
    if (this.listaResizeState) this.listaColunas.update(cols => cols.map(c => c.campo === rs.campo ? { ...c, largura: nw } : c));
    if (this.prodResizeState) this.prodColunas.update(cols => cols.map(c => c.campo === rs.campo ? { ...c, largura: nw } : c));
    rs.startX = e.clientX; rs.startWidth = nw;
  }
  @HostListener('document:mouseup')
  onMouseUp() {
    if (this.listaResizeState) { this.salvarColunas(this.LISTA_COLUNAS_KEY, this.listaColunas()); this.listaResizeState = null; }
    if (this.prodResizeState) { this.salvarColunas(this.PROD_COLUNAS_KEY, this.prodColunas()); this.prodResizeState = null; }
    document.body.style.cursor = ''; document.body.style.userSelect = '';
  }
  selecionar(r: Promo) { this.selecionado.set(r); }
  formatarDataHora(d?: string): string { if (!d) return ''; return new Date(d).toLocaleString('pt-BR'); }

  // Dias
  isDiaAtivo(v: number): boolean { return (this.fDiaSemana() & v) !== 0; }
  toggleDia(v: number) { this.fDiaSemana.update(d => d ^ v); }
  todosOsDias() { this.fDiaSemana.set(127); }

  // Filiais/Pagamentos
  isFilialSel(id: number): boolean { return this.fFilialIds().has(id); }
  toggleFilial(id: number) { this.fFilialIds.update(s => { const ns = new Set(s); if (ns.has(id)) ns.delete(id); else ns.add(id); return ns; }); }
  todasFiliais() { this.fFilialIds.set(new Set(this.filiais().map(f => f.id))); }
  isPagSel(id: number): boolean { return this.fPagamentoIds().has(id); }
  togglePag(id: number) { this.fPagamentoIds.update(s => { const ns = new Set(s); if (ns.has(id)) ns.delete(id); else ns.add(id); return ns; }); }
  todosPag() { this.fPagamentoIds.set(new Set(this.tiposPagamento().map(t => t.id))); }

  // Convênios
  adicionarConvenio(id: number) { if (!this.fConvenioIds().includes(id)) this.fConvenioIds.update(ids => [...ids, id]); }
  removerConvenio(id: number) { this.fConvenioIds.update(ids => ids.filter(i => i !== id)); }
  nomeConvenio(id: number): string { return this.convenios().find(c => c.id === id)?.nome ?? ''; }

  // ── Faixas ────────────────────────────────────────────────────────
  adicionarFaixa() {
    const faixasAtuais = this.faixas();
    const proxQtd = faixasAtuais.length > 0 ? faixasAtuais[faixasAtuais.length - 1].quantidade + 1 : 1;
    const proxPct = faixasAtuais.length > 0 ? faixasAtuais[faixasAtuais.length - 1].percentualDesconto : 0;
    this.faixas.update(fs => [...fs, { quantidade: proxQtd, percentualDesconto: proxPct }]);
  }

  removerFaixa(idx: number) { this.faixas.update(fs => fs.filter((_, i) => i !== idx)); }

  updFaixa(idx: number, campo: keyof Faixa, v: number) {
    this.faixas.update(fs => fs.map((f, i) => i === idx ? { ...f, [campo]: v } : f));
  }

  // ── CRUD ───────────────────────────────────────────────────────────
  agora(): string { return new Date().toISOString().slice(0, 16); }

  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.resetForm();
    this.fDataInicio.set(this.agora());
    this.faixas.set([{ quantidade: 1, percentualDesconto: 0 }, { quantidade: 2, percentualDesconto: 0 }, { quantidade: 3, percentualDesconto: 0 }]);
    this.modoEdicao.set(false);
    this.modo.set('config');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const r = this.selecionado(); if (!r) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${r.id}`).subscribe({
      next: res => {
        this.carregando.set(false);
        const d: PromoDetalhe = res.data;
        this.detalhe.set(d);
        this.fNome.set(d.nome);
        this.fDataInicio.set(d.dataHoraInicio.slice(0, 16));
        this.fDataFim.set(d.dataHoraFim?.slice(0, 16) ?? '');
        this.fDiaSemana.set(d.diaSemana);
        this.fGerarComissao.set(d.gerarComissao);
        this.fExclusivaConvenio.set(d.exclusivaConvenio);
        this.fIntersabores.set(d.intersabores);
        this.fAtivo.set(d.ativo);
        this.fFilialIds.set(new Set(d.filialIds));
        this.fPagamentoIds.set(new Set(d.pagamentoIds));
        this.fConvenioIds.set(d.convenioIds);
        this.faixas.set(d.faixas);
        this.produtos.set(d.produtos);
        this.modoEdicao.set(true);
        this.modo.set('config');
      },
      error: () => { this.carregando.set(false); this.modal.erro('Erro', 'Erro ao carregar promoção.'); }
    });
  }

  avancarParaProdutos() {
    if (!this.fNome().trim()) { this.modal.aviso('Campo obrigatório', 'Informe o nome da promoção.'); return; }
    if (this.fFilialIds().size === 0) { this.modal.aviso('Campo obrigatório', 'Selecione ao menos uma filial.'); return; }
    if (this.fPagamentoIds().size === 0) { this.modal.aviso('Campo obrigatório', 'Selecione ao menos uma forma de pagamento.'); return; }
    if (this.faixas().length === 0) { this.modal.aviso('Campo obrigatório', 'Adicione ao menos uma faixa de desconto.'); return; }
    this.modo.set('produtos');
  }

  voltarParaConfig() { this.modo.set('config'); }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    if (this.produtos().length === 0) { this.modal.aviso('Atenção', 'Adicione ao menos um produto.'); return; }
    this.salvando.set(true);
    const headers = this.headerLiberacao();
    const body = {
      nome: this.fNome(), tipo: 2,
      dataHoraInicio: this.fDataInicio(), dataHoraFim: this.fDataFim() || null,
      diaSemana: this.fDiaSemana(), permitirMudarPreco: false,
      gerarComissao: this.fGerarComissao(), exclusivaConvenio: this.fExclusivaConvenio(),
      reducaoVendaPrazo: 0, qtdeMaxPorVenda: null,
      lancarPorQuantidade: false, dataInicioContagem: null,
      intersabores: this.fIntersabores(), ativo: this.fAtivo(),
      filialIds: Array.from(this.fFilialIds()),
      pagamentoIds: Array.from(this.fPagamentoIds()),
      convenioIds: this.fConvenioIds(),
      faixas: this.faixas(),
      produtos: this.produtos().map(p => ({
        produtoId: p.produtoId, percentualPromocao: 0, valorPromocao: 0, percentualLucro: 0
      }))
    };
    const id = this.detalhe()?.id;
    const op$ = this.modoEdicao() && id
      ? this.http.put(`${this.apiUrl}/${id}`, body, { headers })
      : this.http.post(this.apiUrl, body, { headers });
    op$.subscribe({
      next: () => { this.salvando.set(false); this.carregar(); this.modo.set('lista'); },
      error: (err) => { this.salvando.set(false); this.modal.erro('Erro', err.error?.message || 'Erro ao salvar.'); }
    });
  }

  async excluir() {
    const r = this.selecionado(); if (!r) return;
    const res = await this.modal.confirmar('Excluir', `Excluir promoção "${r.nome}"?`, 'Sim', 'Não');
    if (!res.confirmado) return; if (!await this.verificarPermissao('e')) return;
    this.http.delete<any>(`${this.apiUrl}/${r.id}`, { headers: this.headerLiberacao() }).subscribe({
      next: async (res: any) => { this.selecionado.set(null); this.carregar();
        if (res.resultado === 'desativado') await this.modal.aviso('Desativado', 'Registro desativado.'); },
      error: () => this.modal.erro('Erro', 'Erro ao excluir.')
    });
  }

  fechar() { this.modo.set('lista'); this.carregar(); }

  // ── Produtos ──────────────────────────────────────────────────────
  onProdutoInput(v: string) {
    this.produtoBusca.set(v);
    if (this.produtoTimer) clearTimeout(this.produtoTimer);
    if (v.trim().length < 3) { this.produtoResultados.set([]); this.produtoDropdown.set(false); return; }
    this.produtoTimer = setTimeout(() => {
      const filialId = Array.from(this.fFilialIds())[0] || 1;
      this.http.get<any>(`${environment.apiUrl}/produtos/buscar?termo=${encodeURIComponent(v.trim())}&filialId=${filialId}`).subscribe({
        next: r => { this.produtoResultados.set(r.data ?? []); this.produtoDropdown.set((r.data ?? []).length > 0); },
        error: () => this.produtoDropdown.set(false)
      });
    }, 300);
  }
  onProdutoBlur() { setTimeout(() => this.produtoDropdown.set(false), 200); }

  adicionarProduto(p: any) {
    if (this.produtos().find(pp => pp.produtoId === p.id)) return;
    this.produtos.update(ps => [...ps, {
      id: 0, produtoId: p.id, produtoCodigo: p.codigo || '', produtoNome: p.nome || '',
      fabricante: p.fabricante || '', precoVenda: p.valorVenda || 0, estoqueAtual: p.estoqueAtual || 0
    }]);
    this.produtoBusca.set(''); this.produtoDropdown.set(false);
  }

  isProdSel(id: number): boolean { return this.produtosSelecionados().has(id); }
  toggleProdSel(id: number) { this.produtosSelecionados.update(s => { const ns = new Set(s); if (ns.has(id)) ns.delete(id); else ns.add(id); return ns; }); }
  toggleTodosProd(checked: boolean) { if (checked) this.produtosSelecionados.set(new Set(this.produtos().map(p => p.produtoId))); else this.produtosSelecionados.set(new Set()); }
  removerSelecionados() { const sel = this.produtosSelecionados(); this.produtos.update(ps => ps.filter(p => !sel.has(p.produtoId))); this.produtosSelecionados.set(new Set()); }

  private resetForm() {
    this.fNome.set(''); this.fDataInicio.set(''); this.fDataFim.set('');
    this.fDiaSemana.set(127); this.fGerarComissao.set(false);
    this.fExclusivaConvenio.set(false); this.fIntersabores.set(false);
    this.fAtivo.set(true); this.fFilialIds.set(new Set());
    this.fPagamentoIds.set(new Set()); this.fConvenioIds.set([]);
    this.faixas.set([]); this.produtos.set([]); this.detalhe.set(null);
  }
}
