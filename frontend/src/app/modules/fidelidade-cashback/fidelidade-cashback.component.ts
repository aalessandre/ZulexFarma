import { Component, OnInit, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface CashbackItem {
  id?: number;
  grupoPrincipalId?: number | null;
  grupoProdutoId?: number | null;
  subGrupoId?: number | null;
  secaoId?: number | null;
  produtoFamiliaId?: number | null;
  fabricanteId?: number | null;
  produtoId?: number | null;
  incluir: boolean;
  descricao?: string;
  valorVendaReferencia: number;
  percentualCashbackItem: number;
  valorCashbackItem: number;
}

interface Campanha {
  id?: number;
  codigo?: string;
  nome: string;
  descricao?: string | null;
  tipo: 'Cashback';
  modoContagem: 'PorVenda';
  valorBase: number;
  pontosGanhos: number;
  percentualCashback: number;
  formaRetirada: 'DescontoNaVenda';
  valorPorPonto: number;
  diasValidadePontos: number;
  limiarAlerta: number;
  dataHoraInicio: string;
  dataHoraFim?: string | null;
  diaSemana: number;
  horaInicio?: string | null;
  horaFim?: string | null;
  ativo: boolean;
  filialIds: number[];
  tipoPagamentoIds: number[];
  itens: CashbackItem[];
  criadoEm?: string;
}

interface Lookup { id: number; nome: string; }
interface ProdutoLookup { id: number; nome: string; valorVenda: number; }

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'codigo',    label: 'Codigo',   largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'nome',      label: 'Nome',     largura: 260, minLargura: 120, padrao: true },
  { campo: 'vigencia',  label: 'Vigencia', largura: 160, minLargura: 120, padrao: true },
  { campo: 'totalItens', label: 'Itens',   largura: 70,  minLargura: 50,  padrao: true },
  { campo: 'ativo',     label: 'Ativo',    largura: 70,  minLargura: 50,  padrao: true },
];

@Component({
  selector: 'app-fidelidade-cashback',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './fidelidade-cashback.component.html',
  styleUrl: './fidelidade-cashback.component.scss'
})
export class FidelidadeCashbackComponent implements OnInit {

  private api = `${environment.apiUrl}/fidelidade/campanhas`;
  private apiFiliais = `${environment.apiUrl}/filiais`;
  private apiPagamentos = `${environment.apiUrl}/tiposPagamento`;
  private apiFabricantes = `${environment.apiUrl}/fabricantes`;
  private apiGrupos = `${environment.apiUrl}/grupos-produtos`;
  private apiGruposPrincipais = `${environment.apiUrl}/grupos-principais`;
  private apiSubGrupos = `${environment.apiUrl}/sub-grupos`;
  private apiSecoes = `${environment.apiUrl}/secoes`;
  private apiFamilias = `${environment.apiUrl}/produto-familias`;
  private apiProdutos = `${environment.apiUrl}/produtos`;

  campanhas = signal<Campanha[]>([]);
  selecionada = signal<Campanha | null>(null);
  modo = signal<'lista' | 'form'>('lista');
  salvando = signal(false);
  carregando = signal(false);
  isDirty = signal(false);
  erro = signal('');

  // Filtros + grid
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('nome');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  painelColunas = signal(false);
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  private readonly STORAGE_COLUNAS = 'zulex_colunas_fidelidade_cashback';

  filiais = signal<Lookup[]>([]);
  tiposPagamento = signal<Lookup[]>([]);
  fabricantes = signal<Lookup[]>([]);
  gruposPrincipais = signal<Lookup[]>([]);
  gruposProdutos = signal<Lookup[]>([]);
  subGrupos = signal<Lookup[]>([]);
  secoes = signal<Lookup[]>([]);
  familias = signal<Lookup[]>([]);

  // Dropdowns
  filialDropdown = signal(false);
  pagamentoDropdown = signal(false);

  // Form
  form = signal<Campanha>(this.novaCampanha());
  private formOriginal: Campanha | null = null;

  // Novo item agrupador
  novoItemTipo = signal<'fabricante' | 'grupoPrincipal' | 'grupo' | 'subGrupo' | 'secao' | 'familia'>('fabricante');
  novoItemId = signal<number | null>(null);
  novoItemPercentual = signal(0);
  novoItemValor = signal(0);

  // Produto lookup
  produtoBusca = signal('');
  produtoResultados = signal<ProdutoLookup[]>([]);
  produtoSelecionado = signal<ProdutoLookup | null>(null);
  novoProdutoPercentual = signal(0);
  novoProdutoValor = signal(0);
  private produtoBuscaTimer: any = null;

  campanhasFiltradas = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.campanhas().filter(c => {
      if (status === 'ativos'   && !c.ativo) return false;
      if (status === 'inativos' &&  c.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(c.nome).includes(termo);
    });

    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean'
        ? (va === vb ? 0 : va ? -1 : 1)
        : typeof va === 'number' ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  constructor(private http: HttpClient, private tab: TabService, private modal: ModalService) {}

  ngOnInit() {
    this.carregar();
    this.carregarLookups();
  }

  // ── Grid helpers ───────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_COLUNAS);
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

  private salvarColunas() { localStorage.setItem(this.STORAGE_COLUNAS, JSON.stringify(this.colunas())); }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunas();
  }

  restaurarColunas() {
    this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunas();
  }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortColuna.set(coluna); this.sortDirecao.set('asc'); }
  }
  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '\u25B2' : '\u25BC') : '\u21C5'; }

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
      this.salvarColunas();
      this.resizeState = null;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    }
  }

  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => { const arr = [...cols]; const [m] = arr.splice(this.dragColIdx!, 1); arr.splice(idx, 0, m); this.dragColIdx = idx; return arr; });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunas(); }

  private normalizar(s: string | null | undefined): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(c: Campanha, campo: string): string {
    switch (campo) {
      case 'totalItens': return String((c.itens ?? []).length);
      case 'vigencia': {
        const ini = c.dataHoraInicio ? new Date(c.dataHoraInicio).toLocaleDateString('pt-BR') : '\u2014';
        const fim = c.dataHoraFim ? new Date(c.dataHoraFim).toLocaleDateString('pt-BR') : '\u221E';
        return `${ini} \u2192 ${fim}`;
      }
      case 'ativo': return c.ativo ? 'Sim' : 'N\u00E3o';
      default: return ((c as any)[campo] ?? '').toString();
    }
  }

  /** Converte string do input em numero. */
  updNum<K extends keyof Campanha>(campo: K, v: string, decimais = false) {
    const clean = (v ?? '').replace(decimais ? /[^0-9.,]/g : /\D/g, '').replace(',', '.');
    const num = clean === '' ? 0 : (decimais ? parseFloat(clean) : parseInt(clean, 10));
    this.form.update(f => ({ ...f, [campo]: (isNaN(num) ? 0 : num) as any }));
    this.isDirty.set(true);
  }

  // ── Carregamento ──────────────────────────────────
  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.api}?tipo=Cashback`).subscribe({
      next: r => { this.campanhas.set(r.data ?? []); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  private carregarLookups() {
    this.http.get<any>(this.apiFiliais).subscribe({ next: r => this.filiais.set((r.data ?? []).map((f: any) => ({ id: f.id, nome: f.nomeFilial ?? f.nome }))) });
    this.http.get<any>(this.apiPagamentos).subscribe({ next: r => this.tiposPagamento.set((r.data ?? []).map((t: any) => ({ id: t.id, nome: t.nome }))) });
    this.http.get<any>(this.apiFabricantes).subscribe({ next: r => this.fabricantes.set((r.data ?? []).map((f: any) => ({ id: f.id, nome: f.nome }))) });
    this.http.get<any>(this.apiGruposPrincipais).subscribe({ next: r => this.gruposPrincipais.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
    this.http.get<any>(this.apiGrupos).subscribe({ next: r => this.gruposProdutos.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
    this.http.get<any>(this.apiSubGrupos).subscribe({ next: r => this.subGrupos.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
    this.http.get<any>(this.apiSecoes).subscribe({ next: r => this.secoes.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
    this.http.get<any>(this.apiFamilias).subscribe({ next: r => this.familias.set((r.data ?? []).map((g: any) => ({ id: g.id, nome: g.nome }))) });
  }

  // ── CRUD ───────────────────────────────────────────
  fechar() {
    this.modo.set('lista');
  }

  incluir() {
    const nova = this.novaCampanha();
    this.form.set(nova);
    this.formOriginal = { ...nova, filialIds: [...nova.filialIds], tipoPagamentoIds: [...nova.tipoPagamentoIds], itens: [...nova.itens] };
    this.selecionada.set(null);
    this.isDirty.set(false);
    this.erro.set('');
    this.modo.set('form');
  }

  editar(c?: Campanha) {
    const alvo = c ?? this.selecionada();
    if (!alvo?.id) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.api}/${alvo.id}`).subscribe({
      next: r => {
        const normalizada = this.normalizarCampanha(r.data);
        this.form.set(normalizada);
        this.formOriginal = { ...normalizada, filialIds: [...normalizada.filialIds], tipoPagamentoIds: [...normalizada.tipoPagamentoIds], itens: [...normalizada.itens] };
        this.selecionada.set(alvo);
        this.isDirty.set(false);
        this.erro.set('');
        this.modo.set('form');
        this.carregando.set(false);
      },
      error: () => { this.carregando.set(false); this.modal.erro('Erro', 'Erro ao carregar campanha.'); }
    });
  }

  async excluir(c: Campanha) {
    if (!c.id) return;
    const r = await this.modal.confirmar('Confirmar exclusao', `Excluir a campanha "${c.nome}"?`, 'Sim, excluir', 'Cancelar');
    if (!r.confirmado) return;
    this.http.delete(`${this.api}/${c.id}`).subscribe({
      next: () => { this.modal.sucesso('OK', 'Campanha excluida.'); this.carregar(); },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao excluir.')
    });
  }

  async salvar() {
    const f = this.form();
    if (!f.nome?.trim()) { this.erro.set('Nome e obrigatorio.'); return; }

    this.erro.set('');
    this.salvando.set(true);

    const body = {
      ...f,
      tipo: 'Cashback' as const,
      modoContagem: 'PorVenda' as const,
      valorBase: 0,
      pontosGanhos: 0,
      percentualCashback: 0,
      formaRetirada: 'DescontoNaVenda' as const,
      valorPorPonto: 0,
      limiarAlerta: 0,
      dataHoraInicio: new Date(f.dataHoraInicio).toISOString(),
      dataHoraFim: f.dataHoraFim ? new Date(f.dataHoraFim).toISOString() : null
    };
    const obs = this.selecionada()?.id
      ? this.http.put(`${this.api}/${this.selecionada()!.id}`, body)
      : this.http.post(this.api, body);

    obs.subscribe({
      next: () => {
        this.salvando.set(false);
        this.modal.sucesso('OK', 'Campanha salva.');
        this.modo.set('lista');
        this.carregar();
      },
      error: (e: any) => {
        this.salvando.set(false);
        this.erro.set(e?.error?.message || 'Erro ao salvar.');
      }
    });
  }

  cancelar() {
    if (this.formOriginal) {
      this.form.set({ ...this.formOriginal, filialIds: [...this.formOriginal.filialIds], tipoPagamentoIds: [...this.formOriginal.tipoPagamentoIds], itens: [...this.formOriginal.itens] });
    }
    this.isDirty.set(false);
  }

  fecharForm() {
    if (this.isDirty()) {
      this.modal.confirmar('Fechar', 'Ha alteracoes nao salvas. Deseja descartar?', 'Sim', 'Nao').then(r => {
        if (r.confirmado) this.modo.set('lista');
      });
    } else {
      this.modo.set('lista');
    }
  }

  // ── Form helpers ──────────────────────────────────
  upd<K extends keyof Campanha>(k: K, v: Campanha[K]) {
    this.form.update(f => ({ ...f, [k]: v }));
    this.isDirty.set(true);
  }

  toggleFilial(id: number) {
    this.form.update(f => {
      const ids = f.filialIds.includes(id) ? f.filialIds.filter(x => x !== id) : [...f.filialIds, id];
      return { ...f, filialIds: ids };
    });
    this.isDirty.set(true);
  }

  togglePagamento(id: number) {
    this.form.update(f => {
      const ids = f.tipoPagamentoIds.includes(id) ? f.tipoPagamentoIds.filter(x => x !== id) : [...f.tipoPagamentoIds, id];
      return { ...f, tipoPagamentoIds: ids };
    });
    this.isDirty.set(true);
  }

  toggleDiaSemana(bit: number) {
    this.form.update(f => ({ ...f, diaSemana: f.diaSemana ^ bit }));
    this.isDirty.set(true);
  }

  diaSemanaLigado(bit: number): boolean {
    return (this.form().diaSemana & bit) !== 0;
  }

  // ── Agrupadores ───────────────────────────────────
  opcoesAgrupador = computed(() => this.lookupPorTipo(this.novoItemTipo()));

  adicionarItemAgrupador() {
    const id = this.novoItemId();
    if (!id) { this.modal.aviso('Item', 'Selecione um agrupador primeiro.'); return; }
    const tipo = this.novoItemTipo();
    const lookup = this.lookupPorTipo(tipo);
    const selecionado = lookup.find(l => l.id === id);
    if (!selecionado) return;

    const item: CashbackItem = {
      incluir: true,
      descricao: `${this.labelTipo(tipo)}: ${selecionado.nome}`,
      valorVendaReferencia: 0,
      percentualCashbackItem: this.novoItemPercentual(),
      valorCashbackItem: this.novoItemValor()
    };
    switch (tipo) {
      case 'fabricante': item.fabricanteId = id; break;
      case 'grupoPrincipal': item.grupoPrincipalId = id; break;
      case 'grupo': item.grupoProdutoId = id; break;
      case 'subGrupo': item.subGrupoId = id; break;
      case 'secao': item.secaoId = id; break;
      case 'familia': item.produtoFamiliaId = id; break;
    }

    this.form.update(f => ({ ...f, itens: [...f.itens, item] }));
    this.novoItemId.set(null);
    this.novoItemPercentual.set(0);
    this.novoItemValor.set(0);
    this.isDirty.set(true);
  }

  // ── Produto lookup ────────────────────────────────
  buscarProduto() {
    const termo = this.produtoBusca().trim();
    if (termo.length < 2) { this.produtoResultados.set([]); return; }
    clearTimeout(this.produtoBuscaTimer);
    this.produtoBuscaTimer = setTimeout(() => {
      this.http.get<any>(`${this.apiProdutos}?busca=${encodeURIComponent(termo)}&limit=10`).subscribe({
        next: r => this.produtoResultados.set((r.data ?? []).map((p: any) => ({
          id: p.id, nome: p.nome, valorVenda: p.valorVenda ?? 0
        }))),
        error: () => this.produtoResultados.set([])
      });
    }, 300);
  }

  selecionarProduto(p: ProdutoLookup) {
    this.produtoSelecionado.set(p);
    this.produtoBusca.set(p.nome);
    this.produtoResultados.set([]);
    this.novoProdutoPercentual.set(0);
    this.novoProdutoValor.set(0);
  }

  adicionarItemProduto() {
    const p = this.produtoSelecionado();
    if (!p) { this.modal.aviso('Item', 'Selecione um produto primeiro.'); return; }

    const item: CashbackItem = {
      produtoId: p.id,
      incluir: true,
      descricao: `Produto: ${p.nome}`,
      valorVendaReferencia: p.valorVenda,
      percentualCashbackItem: this.novoProdutoPercentual(),
      valorCashbackItem: this.novoProdutoValor()
    };

    this.form.update(f => ({ ...f, itens: [...f.itens, item] }));
    this.produtoSelecionado.set(null);
    this.produtoBusca.set('');
    this.novoProdutoPercentual.set(0);
    this.novoProdutoValor.set(0);
    this.isDirty.set(true);
  }

  removerItem(idx: number) {
    this.form.update(f => ({ ...f, itens: f.itens.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }

  // ── Recalculo cashback agrupador ──────────────────
  onAgrupadorPercentualChange(v: string) {
    const clean = (v ?? '').replace(/[^0-9.,]/g, '').replace(',', '.');
    const pct = clean === '' ? 0 : parseFloat(clean);
    this.novoItemPercentual.set(isNaN(pct) ? 0 : pct);
    // Agrupadores nao tem valorVendaReferencia, valor fica manual
  }

  onAgrupadorValorChange(v: string) {
    const clean = (v ?? '').replace(/[^0-9.,]/g, '').replace(',', '.');
    const val = clean === '' ? 0 : parseFloat(clean);
    this.novoItemValor.set(isNaN(val) ? 0 : val);
  }

  // ── Recalculo cashback produto ────────────────────
  onProdutoPercentualChange(v: string) {
    const clean = (v ?? '').replace(/[^0-9.,]/g, '').replace(',', '.');
    const pct = clean === '' ? 0 : parseFloat(clean);
    this.novoProdutoPercentual.set(isNaN(pct) ? 0 : pct);
    const ref = this.produtoSelecionado()?.valorVenda ?? 0;
    this.novoProdutoValor.set(+(ref * (isNaN(pct) ? 0 : pct) / 100).toFixed(2));
  }

  onProdutoValorChange(v: string) {
    const clean = (v ?? '').replace(/[^0-9.,]/g, '').replace(',', '.');
    const val = clean === '' ? 0 : parseFloat(clean);
    this.novoProdutoValor.set(isNaN(val) ? 0 : val);
    const ref = this.produtoSelecionado()?.valorVenda ?? 0;
    this.novoProdutoPercentual.set(ref > 0 ? +((isNaN(val) ? 0 : val) / ref * 100).toFixed(2) : 0);
  }

  // ── Recalculo inline (itens ja adicionados) ───────
  onItemPercentualChange(idx: number, v: string) {
    const clean = (v ?? '').replace(/[^0-9.,]/g, '').replace(',', '.');
    const pct = clean === '' ? 0 : parseFloat(clean);
    this.form.update(f => {
      const itens = [...f.itens];
      const it = { ...itens[idx] };
      it.percentualCashbackItem = isNaN(pct) ? 0 : pct;
      it.valorCashbackItem = +(it.valorVendaReferencia * it.percentualCashbackItem / 100).toFixed(2);
      itens[idx] = it;
      return { ...f, itens };
    });
    this.isDirty.set(true);
  }

  onItemValorChange(idx: number, v: string) {
    const clean = (v ?? '').replace(/[^0-9.,]/g, '').replace(',', '.');
    const val = clean === '' ? 0 : parseFloat(clean);
    this.form.update(f => {
      const itens = [...f.itens];
      const it = { ...itens[idx] };
      it.valorCashbackItem = isNaN(val) ? 0 : val;
      it.percentualCashbackItem = it.valorVendaReferencia > 0 ? +((isNaN(val) ? 0 : val) / it.valorVendaReferencia * 100).toFixed(2) : 0;
      itens[idx] = it;
      return { ...f, itens };
    });
    this.isDirty.set(true);
  }

  // ── Helpers ───────────────────────────────────────
  private lookupPorTipo(tipo: string): Lookup[] {
    switch (tipo) {
      case 'fabricante': return this.fabricantes();
      case 'grupoPrincipal': return this.gruposPrincipais();
      case 'grupo': return this.gruposProdutos();
      case 'subGrupo': return this.subGrupos();
      case 'secao': return this.secoes();
      case 'familia': return this.familias();
      default: return [];
    }
  }

  private labelTipo(tipo: string): string {
    return ({ fabricante: 'Fabricante', grupoPrincipal: 'Grupo Principal', grupo: 'Grupo', subGrupo: 'SubGrupo', secao: 'Secao', familia: 'Familia' } as any)[tipo] ?? tipo;
  }

  tipoItemDescricao(it: CashbackItem): string {
    if (it.produtoId) return 'Produto';
    if (it.fabricanteId) return 'Fabricante';
    if (it.grupoPrincipalId) return 'Grupo Principal';
    if (it.grupoProdutoId) return 'Grupo';
    if (it.subGrupoId) return 'SubGrupo';
    if (it.secaoId) return 'Secao';
    if (it.produtoFamiliaId) return 'Familia';
    return '—';
  }

  sairDaTela() { this.tab.fecharTabAtiva(); }

  private novaCampanha(): Campanha {
    const hoje = new Date();
    return {
      nome: '',
      descricao: '',
      tipo: 'Cashback',
      modoContagem: 'PorVenda',
      valorBase: 0,
      pontosGanhos: 0,
      percentualCashback: 0,
      formaRetirada: 'DescontoNaVenda',
      valorPorPonto: 0,
      diasValidadePontos: 0,
      limiarAlerta: 0,
      dataHoraInicio: hoje.toISOString().slice(0, 16),
      dataHoraFim: null,
      diaSemana: 127,
      horaInicio: null,
      horaFim: null,
      ativo: true,
      filialIds: [],
      tipoPagamentoIds: [],
      itens: []
    };
  }

  private normalizarCampanha(c: any): Campanha {
    return {
      ...c,
      tipo: 'Cashback',
      modoContagem: 'PorVenda',
      formaRetirada: 'DescontoNaVenda',
      dataHoraInicio: c.dataHoraInicio ? new Date(c.dataHoraInicio).toISOString().slice(0, 16) : '',
      dataHoraFim: c.dataHoraFim ? new Date(c.dataHoraFim).toISOString().slice(0, 16) : null,
      filialIds: c.filialIds ?? [],
      tipoPagamentoIds: c.tipoPagamentoIds ?? [],
      itens: (c.itens ?? []).map((it: any) => ({
        ...it,
        valorVendaReferencia: it.valorVendaReferencia ?? 0,
        percentualCashbackItem: it.percentualCashbackItem ?? 0,
        valorCashbackItem: it.valorCashbackItem ?? 0
      }))
    };
  }

  nomeTipoPagamento(id: number): string {
    return this.tiposPagamento().find(t => t.id === id)?.nome ?? '';
  }

  nomeFilial(id: number): string {
    return this.filiais().find(f => f.id === id)?.nome ?? '';
  }

  @HostListener('document:click', ['$event'])
  fecharDropdowns(e: MouseEvent) {
    const target = e.target as HTMLElement;
    if (!target.closest('.ms-wrap-filial')) this.filialDropdown.set(false);
    if (!target.closest('.ms-wrap-pag')) this.pagamentoDropdown.set(false);
    if (!target.closest('.produto-busca-wrap')) this.produtoResultados.set([]);
  }
}
