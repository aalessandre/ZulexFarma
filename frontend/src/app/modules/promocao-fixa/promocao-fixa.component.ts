import { Component, signal, computed, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface Promo { id: number; nome: string; dataHoraInicio: string; dataHoraFim?: string; diaSemana: number; totalProdutos: number; ativo: boolean; criadoEm: string; }
interface PromoDetalhe {
  id: number; nome: string; tipo: number; dataHoraInicio: string; dataHoraFim?: string; diaSemana: number;
  permitirMudarPreco: boolean; gerarComissao: boolean; exclusivaConvenio: boolean; reducaoVendaPrazo: number;
  qtdeMaxPorVenda?: number; lancarPorQuantidade: boolean; dataInicioContagem?: string;
  ativo: boolean; filialIds: number[]; pagamentoIds: number[]; convenioIds: number[];
  produtos: PromoProduto[];
}
interface PromoProduto {
  id: number; produtoId: number; produtoCodigo: string; produtoNome: string; fabricante?: string; precoVenda: number; custoMedio: number;
  estoqueAtual: number; curva?: string; percentualPromocao: number; valorPromocao: number; percentualLucro: number;
  qtdeLimite?: number; qtdeVendida: number;
  percentualAposLimite?: number; valorAposLimite?: number;
}
interface LookupItem { id: number; nome: string; }

type Modo = 'lista' | 'config' | 'produtos';

const DIAS = [
  { label: 'Dom', valor: 1 }, { label: 'Seg', valor: 2 }, { label: 'Ter', valor: 4 },
  { label: 'Qua', valor: 8 }, { label: 'Qui', valor: 16 }, { label: 'Sex', valor: 32 }, { label: 'Sáb', valor: 64 }
];

@Component({
  selector: 'app-promocao-fixa',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './promocao-fixa.component.html',
  styleUrl: './promocao-fixa.component.scss'
})
export class PromocaoFixaComponent implements OnInit {
  modo = signal<Modo>('lista');
  registros = signal<Promo[]>([]);
  selecionado = signal<Promo | null>(null);
  detalhe = signal<PromoDetalhe | null>(null);
  carregando = signal(false);
  salvando = signal(false);
  modoEdicao = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  erro = signal('');
  dias = DIAS;

  // Form config
  fNome = signal('');
  fDataInicio = signal('');
  fDataFim = signal('');
  fDiaSemana = signal(127);
  fPermitirMudarPreco = signal(false);
  fGerarComissao = signal(false);
  fExclusivaConvenio = signal(false);
  fReducaoVendaPrazo = signal(0);
  fQtdeMaxPorVenda = signal<number | null>(null);
  fLancarPorQuantidade = signal(false);
  fDataInicioContagem = signal('');
  fAtivo = signal(true);
  fFilialIds = signal<Set<number>>(new Set());
  fPagamentoIds = signal<Set<number>>(new Set());
  fConvenioIds = signal<number[]>([]);
  fConvenioBusca = signal('');

  // Lookups
  filiais = signal<LookupItem[]>([]);
  tiposPagamento = signal<LookupItem[]>([]);
  convenios = signal<LookupItem[]>([]);

  // Colunas produtos (padrão Compras: sort texto + resize + drag-and-drop)
  private readonly PROD_COLUNAS_KEY = 'zulex_promo_prod_colunas';
  private prodColunasDefault(): { campo: string; label: string; largura: number; visivel: boolean; editavel: boolean }[] {
    return [
      { campo: 'produtoCodigo', label: 'Código', largura: 80, visivel: true, editavel: false },
      { campo: 'produtoNome', label: 'Produto', largura: 220, visivel: true, editavel: false },
      { campo: 'fabricante', label: 'Fabricante', largura: 130, visivel: true, editavel: false },
      { campo: 'estoqueAtual', label: 'Estoque', largura: 70, visivel: true, editavel: false },
      { campo: 'curva', label: 'Curva', largura: 60, visivel: false, editavel: false },
      { campo: 'custoMedio', label: 'Custo Médio', largura: 100, visivel: true, editavel: false },
      { campo: 'precoVenda', label: 'Preço Venda', largura: 100, visivel: true, editavel: false },
      { campo: 'percentualPromocao', label: '% Promoção', largura: 100, visivel: true, editavel: true },
      { campo: 'valorPromocao', label: 'Valor Promo', largura: 100, visivel: true, editavel: true },
      { campo: 'percentualLucro', label: '% Lucro', largura: 90, visivel: true, editavel: true },
      { campo: 'qtdeLimite', label: 'Qtd Limite', largura: 80, visivel: false, editavel: true },
      { campo: 'percentualAposLimite', label: '% Após Limite', largura: 100, visivel: false, editavel: true },
      { campo: 'valorAposLimite', label: 'Valor Após Limite', largura: 110, visivel: false, editavel: true },
    ];
  }
  prodColunas = signal<{ campo: string; label: string; largura: number; visivel: boolean; editavel: boolean }[]>(this.carregarProdColunas());
  prodColunasVisiveis = computed(() => this.prodColunas().filter(c => c.visivel));
  painelColProd = signal(false);
  private dragColIdx: number | null = null;
  private resizingCol: string | null = null;
  private resizeStartX = 0;
  private resizeStartW = 0;
  sortProdCol = signal('');
  sortProdDir = signal<'asc' | 'desc'>('asc');

  private carregarProdColunas() {
    try { const json = localStorage.getItem(this.PROD_COLUNAS_KEY); if (json) return JSON.parse(json); } catch {}
    return this.prodColunasDefault();
  }
  private salvarProdColunas() { localStorage.setItem(this.PROD_COLUNAS_KEY, JSON.stringify(this.prodColunas())); }
  toggleColProd(campo: string) { this.prodColunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c)); this.salvarProdColunas(); }
  restaurarColProd() { this.prodColunas.set(this.prodColunasDefault()); this.salvarProdColunas(); }

  // Sort
  sortProdutos(col: string) {
    if (this.sortProdCol() === col) this.sortProdDir.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortProdCol.set(col); this.sortProdDir.set('asc'); }
    const dir = this.sortProdDir();
    this.produtos.update(ps => [...ps].sort((a, b) => {
      const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    }));
  }
  sortProdIcon(campo: string): string { return this.sortProdCol() === campo ? (this.sortProdDir() === 'asc' ? '▲' : '▼') : '⇅'; }

  // Drag-and-drop colunas
  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.prodColunas.update(cols => { const arr = [...cols]; const [moved] = arr.splice(this.dragColIdx!, 1); arr.splice(idx, 0, moved); this.dragColIdx = idx; return arr; });
  }
  onDropCol() { this.dragColIdx = null; this.salvarProdColunas(); }

  // Resize colunas
  iniciarResizeProd(event: MouseEvent, col: string, largura: number) {
    event.preventDefault(); event.stopPropagation();
    this.resizingCol = col; this.resizeStartX = event.clientX; this.resizeStartW = largura;
  }
  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    if (!this.resizingCol) return;
    const diff = event.clientX - this.resizeStartX;
    const novaLargura = Math.max(40, this.resizeStartW + diff);
    this.prodColunas.update(cols => cols.map(c => c.campo === this.resizingCol ? { ...c, largura: novaLargura } : c));
    this.resizeStartX = event.clientX;
    this.resizeStartW = novaLargura;
  }
  @HostListener('document:mouseup')
  onMouseUp() { if (this.resizingCol) { this.resizingCol = null; this.salvarProdColunas(); document.body.style.cursor = ''; document.body.style.userSelect = ''; } }

  // Produtos
  produtos = signal<PromoProduto[]>([]);
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
      next: r => { this.registros.set((r.data ?? []).filter((p: any) => p.tipo === 1)); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  registrosFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    return this.registros().filter(r => {
      if (status === 'ativos' && !r.ativo) return false;
      if (status === 'inativos' && r.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(r.nome).includes(termo);
    });
  });

  private normalizar(s: string): string { return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim(); }

  selecionar(r: Promo) { this.selecionado.set(r); }

  formatarData(d?: string): string { if (!d) return ''; return new Date(d).toLocaleDateString('pt-BR'); }
  formatarDataHora(d?: string): string { if (!d) return ''; return new Date(d).toLocaleString('pt-BR'); }

  // ── Dias da semana ────────────────────────────────────────────────
  isDiaAtivo(valor: number): boolean { return (this.fDiaSemana() & valor) !== 0; }
  toggleDia(valor: number) { this.fDiaSemana.update(v => v ^ valor); }
  todosOsDias() { this.fDiaSemana.set(127); }

  // ── Filiais ───────────────────────────────────────────────────────
  isFilialSelecionada(id: number): boolean { return this.fFilialIds().has(id); }
  toggleFilial(id: number) { this.fFilialIds.update(s => { const ns = new Set(s); if (ns.has(id)) ns.delete(id); else ns.add(id); return ns; }); }
  todasFiliais() { this.fFilialIds.set(new Set(this.filiais().map(f => f.id))); }

  // ── Pagamentos ────────────────────────────────────────────────────
  isPagSelecionado(id: number): boolean { return this.fPagamentoIds().has(id); }
  togglePagamento(id: number) { this.fPagamentoIds.update(s => { const ns = new Set(s); if (ns.has(id)) ns.delete(id); else ns.add(id); return ns; }); }
  todosPagamentos() { this.fPagamentoIds.set(new Set(this.tiposPagamento().map(t => t.id))); }

  // ── Convênios ─────────────────────────────────────────────────────
  adicionarConvenio(id: number) { if (!this.fConvenioIds().includes(id)) { this.fConvenioIds.update(ids => [...ids, id]); } }
  removerConvenio(id: number) { this.fConvenioIds.update(ids => ids.filter(i => i !== id)); }
  nomeConvenio(id: number): string { return this.convenios().find(c => c.id === id)?.nome ?? ''; }

  // ── CRUD ───────────────────────────────────────────────────────────
  agora(): string { return new Date().toISOString().slice(0, 16); }

  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.resetForm();
    this.fDataInicio.set(this.agora());
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
        this.fPermitirMudarPreco.set(d.permitirMudarPreco);
        this.fGerarComissao.set(d.gerarComissao);
        this.fExclusivaConvenio.set(d.exclusivaConvenio);
        this.fReducaoVendaPrazo.set(d.reducaoVendaPrazo);
        this.fQtdeMaxPorVenda.set(d.qtdeMaxPorVenda ?? null);
        this.fLancarPorQuantidade.set(d.lancarPorQuantidade);
        this.fDataInicioContagem.set(d.dataInicioContagem?.slice(0, 16) ?? '');
        this.fAtivo.set(d.ativo);
        this.fFilialIds.set(new Set(d.filialIds));
        this.fPagamentoIds.set(new Set(d.pagamentoIds));
        this.fConvenioIds.set(d.convenioIds);
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
    if (this.fDiaSemana() === 0) { this.modal.aviso('Campo obrigatório', 'Selecione ao menos um dia da semana.'); return; }
    // Se lançar por quantidade, mostrar colunas automaticamente
    if (this.fLancarPorQuantidade()) {
      const qtdCols = ['qtdeLimite', 'percentualAposLimite', 'valorAposLimite'];
      this.prodColunas.update(cols => cols.map(c => qtdCols.includes(c.campo) ? { ...c, visivel: true } : c));
    }
    this.modo.set('produtos');
  }

  voltarParaConfig() { this.modo.set('config'); }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    this.salvando.set(true);
    const headers = this.headerLiberacao();
    const body = {
      nome: this.fNome(), tipo: 1,
      dataHoraInicio: this.fDataInicio(), dataHoraFim: this.fDataFim() || null,
      diaSemana: this.fDiaSemana(), permitirMudarPreco: this.fPermitirMudarPreco(),
      gerarComissao: this.fGerarComissao(), exclusivaConvenio: this.fExclusivaConvenio(),
      reducaoVendaPrazo: this.fReducaoVendaPrazo(), qtdeMaxPorVenda: this.fQtdeMaxPorVenda(),
      lancarPorQuantidade: this.fLancarPorQuantidade(),
      dataInicioContagem: this.fLancarPorQuantidade() ? this.fDataInicioContagem() || null : null,
      ativo: this.fAtivo(),
      filialIds: Array.from(this.fFilialIds()),
      pagamentoIds: Array.from(this.fPagamentoIds()),
      convenioIds: this.fConvenioIds(),
      produtos: this.produtos().map(p => ({
        produtoId: p.produtoId, percentualPromocao: p.percentualPromocao,
        valorPromocao: p.valorPromocao, percentualLucro: p.percentualLucro,
        qtdeLimite: p.qtdeLimite, percentualAposLimite: p.percentualAposLimite,
        valorAposLimite: p.valorAposLimite
      }))
    };
    const id = this.detalhe()?.id;
    const op$ = this.modoEdicao() && id
      ? this.http.put(`${this.apiUrl}/${id}`, body, { headers })
      : this.http.post(this.apiUrl, body, { headers });
    op$.subscribe({
      next: () => { this.salvando.set(false); this.carregar(); this.modo.set('lista'); },
      error: (err) => { this.salvando.set(false); this.modal.erro('Erro ao salvar', err.error?.message || 'Erro ao salvar promoção.'); }
    });
  }

  async excluir() {
    const r = this.selecionado(); if (!r) return;
    const res = await this.modal.confirmar('Excluir', `Excluir promoção "${r.nome}"?`, 'Sim', 'Não');
    if (!res.confirmado) return; if (!await this.verificarPermissao('e')) return;
    this.http.delete<any>(`${this.apiUrl}/${r.id}`, { headers: this.headerLiberacao() }).subscribe({
      next: async (res: any) => { this.selecionado.set(null); this.carregar();
        if (res.resultado === 'desativado') await this.modal.aviso('Desativado', 'O registro foi apenas desativado.'); },
      error: () => this.modal.erro('Erro', 'Erro ao excluir.')
    });
  }

  fechar() { this.modo.set('lista'); this.carregar(); }

  // ── Produtos (Etapa 2) ────────────────────────────────────────────
  onProdutoInput(v: string) {
    this.produtoBusca.set(v);
    if (this.produtoTimer) clearTimeout(this.produtoTimer);
    if (v.trim().length < 3) { this.produtoResultados.set([]); this.produtoDropdown.set(false); return; }
    this.produtoTimer = setTimeout(() => {
      const filialId = Array.from(this.fFilialIds())[0] || 1;
      this.http.get<any>(`${environment.apiUrl}/produtos/buscar?termo=${encodeURIComponent(v.trim())}&filialId=${filialId}&limit=20`).subscribe({
        next: r => { this.produtoResultados.set(r.data ?? []); this.produtoDropdown.set((r.data ?? []).length > 0); },
        error: () => { this.produtoResultados.set([]); this.produtoDropdown.set(false); }
      });
    }, 300);
  }
  onProdutoBlur() { setTimeout(() => this.produtoDropdown.set(false), 200); }

  adicionarProduto(p: any) {
    if (this.produtos().find(pp => pp.produtoId === p.id)) return;
    this.produtos.update(ps => [...ps, {
      id: 0, produtoId: p.id, produtoCodigo: p.codigo || '', produtoNome: p.nome || p.descricao || '',
      fabricante: p.fabricante || '', precoVenda: p.valorVenda || 0, custoMedio: p.custoMedio || 0,
      estoqueAtual: p.estoqueAtual || 0, curva: p.curvaAbc || '',
      percentualPromocao: 0, valorPromocao: 0, percentualLucro: 0,
      qtdeLimite: undefined, qtdeVendida: 0,
      percentualAposLimite: undefined, valorAposLimite: undefined
    }]);
    this.produtoBusca.set('');
    this.produtoDropdown.set(false);
  }

  removerProduto(idx: number) { this.produtos.update(ps => ps.filter((_, i) => i !== idx)); this.produtosSelecionados.set(new Set()); }

  getProdCellValue(p: PromoProduto, campo: string): string {
    if (campo === 'custoMedio' || campo === 'precoVenda') return ((p as any)[campo] ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    return (p as any)[campo] ?? '';
  }

  isProdutoSelecionado(produtoId: number): boolean { return this.produtosSelecionados().has(produtoId); }
  toggleProdutoSelecionado(produtoId: number) { this.produtosSelecionados.update(s => { const ns = new Set(s); if (ns.has(produtoId)) ns.delete(produtoId); else ns.add(produtoId); return ns; }); }
  toggleTodosProdutos(checked: boolean) {
    if (checked) this.produtosSelecionados.set(new Set(this.produtos().map(p => p.produtoId)));
    else this.produtosSelecionados.set(new Set());
  }
  removerProdutosSelecionados() {
    const sel = this.produtosSelecionados();
    this.produtos.update(ps => ps.filter(p => !sel.has(p.produtoId)));
    this.produtosSelecionados.set(new Set());
  }

  // Cálculos interconectados
  onPercentualChange(idx: number, pct: number) {
    this.produtos.update(ps => ps.map((p, i) => {
      if (i !== idx) return p;
      const valorPromo = p.precoVenda * (1 - pct / 100);
      const pctLucro = p.custoMedio > 0 ? ((valorPromo - p.custoMedio) / p.custoMedio) * 100 : 0;
      return { ...p, percentualPromocao: pct, valorPromocao: Math.round(valorPromo * 100) / 100, percentualLucro: Math.round(pctLucro * 100) / 100 };
    }));
  }

  onValorChange(idx: number, valor: number) {
    this.produtos.update(ps => ps.map((p, i) => {
      if (i !== idx) return p;
      const pctPromo = p.precoVenda > 0 ? ((p.precoVenda - valor) / p.precoVenda) * 100 : 0;
      const pctLucro = p.custoMedio > 0 ? ((valor - p.custoMedio) / p.custoMedio) * 100 : 0;
      return { ...p, valorPromocao: valor, percentualPromocao: Math.round(pctPromo * 100) / 100, percentualLucro: Math.round(pctLucro * 100) / 100 };
    }));
  }

  onLucroChange(idx: number, lucro: number) {
    this.produtos.update(ps => ps.map((p, i) => {
      if (i !== idx) return p;
      const valorPromo = p.custoMedio * (1 + lucro / 100);
      const pctPromo = p.precoVenda > 0 ? ((p.precoVenda - valorPromo) / p.precoVenda) * 100 : 0;
      return { ...p, percentualLucro: lucro, valorPromocao: Math.round(valorPromo * 100) / 100, percentualPromocao: Math.round(pctPromo * 100) / 100 };
    }));
  }

  onPercentualAposChange(idx: number, pct: number) {
    this.produtos.update(ps => ps.map((p, i) => {
      if (i !== idx) return p;
      const valor = p.precoVenda * (1 - pct / 100);
      return { ...p, percentualAposLimite: pct, valorAposLimite: Math.round(valor * 100) / 100 };
    }));
  }

  onValorAposChange(idx: number, valor: number) {
    this.produtos.update(ps => ps.map((p, i) => {
      if (i !== idx) return p;
      const pct = p.precoVenda > 0 ? ((p.precoVenda - valor) / p.precoVenda) * 100 : 0;
      return { ...p, valorAposLimite: valor, percentualAposLimite: Math.round(pct * 100) / 100 };
    }));
  }

  updProduto(idx: number, campo: string, v: any) {
    this.produtos.update(ps => ps.map((p, i) => i === idx ? { ...p, [campo]: v } : p));
  }

  // ── Busca avançada (modal) ──────────────────────────────────────
  modalBuscaAvancada = signal(false);
  baDescricao = signal('');
  baFabricanteId = signal<number | null>(null);
  baFornecedorId = signal<number | null>(null);
  baGrupoPrincipalId = signal<number | null>(null);
  baGrupoProdutoId = signal<number | null>(null);
  baSubGrupoId = signal<number | null>(null);
  baSecaoId = signal<number | null>(null);
  baFamiliaId = signal<number | null>(null);
  baPrecoMin = signal<number | null>(null);
  baPrecoMax = signal<number | null>(null);
  baEstoqueMin = signal<number | null>(null);
  baStatus = signal('ativos');
  baBuscando = signal(false);
  baResultados = signal<any[]>([]);
  baSelecionados = signal<Set<number>>(new Set());

  // Lookups para busca avançada
  baFabricantes = signal<LookupItem[]>([]);
  baGruposPrincipais = signal<LookupItem[]>([]);
  baGrupos = signal<LookupItem[]>([]);
  baSubGrupos = signal<LookupItem[]>([]);
  baSecoes = signal<LookupItem[]>([]);
  baFamilias = signal<LookupItem[]>([]);
  baFornecedores = signal<LookupItem[]>([]);

  abrirBuscaAvancada() {
    this.modalBuscaAvancada.set(true);
    this.baResultados.set([]);
    this.baSelecionados.set(new Set());
    this.baDescricao.set('');
    // Carregar lookups se vazio
    if (this.baFabricantes().length === 0) {
      this.http.get<any>(`${environment.apiUrl}/fabricantes`).subscribe({ next: r => this.baFabricantes.set((r.data ?? []).map((f: any) => ({ id: f.id, nome: f.nome }))) });
      this.http.get<any>(`${environment.apiUrl}/grupos-principais`).subscribe({ next: r => this.baGruposPrincipais.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
      this.http.get<any>(`${environment.apiUrl}/grupos-produtos`).subscribe({ next: r => this.baGrupos.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
      this.http.get<any>(`${environment.apiUrl}/subgrupos`).subscribe({ next: r => this.baSubGrupos.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
      this.http.get<any>(`${environment.apiUrl}/secoes`).subscribe({ next: r => this.baSecoes.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
      this.http.get<any>(`${environment.apiUrl}/fornecedores`).subscribe({ next: r => this.baFornecedores.set((r.data ?? []).filter((f: any) => f.ativo).map((f: any) => ({ id: f.id, nome: f.nome }))) });
    }
  }

  fecharBuscaAvancada() { this.modalBuscaAvancada.set(false); }

  executarBuscaAvancada() {
    this.baBuscando.set(true);
    const filialId = Array.from(this.fFilialIds())[0] || 1;
    const params: string[] = [`filialId=${filialId}`];
    if (this.baDescricao()) params.push(`descricao=${encodeURIComponent(this.baDescricao())}`);
    if (this.baFabricanteId()) params.push(`fabricanteId=${this.baFabricanteId()}`);
    if (this.baFornecedorId()) params.push(`fornecedorId=${this.baFornecedorId()}`);
    if (this.baGrupoPrincipalId()) params.push(`grupoPrincipalId=${this.baGrupoPrincipalId()}`);
    if (this.baGrupoProdutoId()) params.push(`grupoProdutoId=${this.baGrupoProdutoId()}`);
    if (this.baSubGrupoId()) params.push(`subGrupoId=${this.baSubGrupoId()}`);
    if (this.baSecaoId()) params.push(`secaoId=${this.baSecaoId()}`);
    if (this.baPrecoMin() !== null) params.push(`precoMin=${this.baPrecoMin()}`);
    if (this.baPrecoMax() !== null) params.push(`precoMax=${this.baPrecoMax()}`);
    if (this.baEstoqueMin() !== null) params.push(`estoqueMinimo=${this.baEstoqueMin()}`);
    params.push(`status=${this.baStatus()}`);

    this.http.get<any>(`${environment.apiUrl}/produtos/busca-avancada?${params.join('&')}`).subscribe({
      next: r => { this.baResultados.set(r.data ?? []); this.baBuscando.set(false); },
      error: () => { this.baBuscando.set(false); this.modal.erro('Erro', 'Erro na busca avançada.'); }
    });
  }

  toggleBaSelecionado(id: number) { this.baSelecionados.update(s => { const ns = new Set(s); if (ns.has(id)) ns.delete(id); else ns.add(id); return ns; }); }
  isBaSelecionado(id: number): boolean { return this.baSelecionados().has(id); }
  toggleBaTodos(checked: boolean) {
    if (checked) this.baSelecionados.set(new Set(this.baResultados().map((p: any) => p.id)));
    else this.baSelecionados.set(new Set());
  }
  todosBaSelecionados(): boolean { return this.baResultados().length > 0 && this.baSelecionados().size === this.baResultados().length; }

  adicionarSelecionados() {
    const selecionados = this.baSelecionados();
    const resultados = this.baResultados();
    for (const p of resultados) {
      if (!selecionados.has(p.id)) continue;
      if (this.produtos().find(pp => pp.produtoId === p.id)) continue;
      this.produtos.update(ps => [...ps, {
        id: 0, produtoId: p.id, produtoCodigo: p.codigo || '', produtoNome: p.nome || '',
        fabricante: p.fabricante || '', precoVenda: p.valorVenda || 0, custoMedio: p.custoMedio || 0,
        estoqueAtual: p.estoqueAtual || 0, curva: p.curvaAbc || '',
        percentualPromocao: 0, valorPromocao: 0, percentualLucro: 0,
        lancarPorQuantidade: false, qtdeLimite: undefined, qtdeVendida: 0,
        dataInicioContagem: undefined, percentualAposLimite: undefined, valorAposLimite: undefined
      }]);
    }
    this.fecharBuscaAvancada();
  }

  private resetForm() {
    this.fNome.set(''); this.fDataInicio.set(''); this.fDataFim.set('');
    this.fDiaSemana.set(127); this.fPermitirMudarPreco.set(false);
    this.fGerarComissao.set(false); this.fExclusivaConvenio.set(false);
    this.fReducaoVendaPrazo.set(0); this.fQtdeMaxPorVenda.set(null);
    this.fLancarPorQuantidade.set(false); this.fDataInicioContagem.set('');
    this.fAtivo.set(true); this.fFilialIds.set(new Set()); this.fPagamentoIds.set(new Set());
    this.fConvenioIds.set([]); this.produtos.set([]); this.detalhe.set(null); this.erro.set('');
  }
}
