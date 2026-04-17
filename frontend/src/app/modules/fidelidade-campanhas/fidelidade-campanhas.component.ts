import { Component, OnInit, signal, computed, Input, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

type TipoFidelidade = 'Pontos' | 'Cashback';
type ModoContagem = 'PorVenda' | 'Geral';
type FormaRetirada = 'Premio' | 'DescontoNaVenda';

interface Campanha {
  id?: number;
  codigo?: string;
  nome: string;
  descricao?: string | null;
  tipo: TipoFidelidade;
  modoContagem: ModoContagem;
  valorBase: number;
  pontosGanhos: number;
  percentualCashback: number;
  formaRetirada: FormaRetirada;
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
  itens: ItemAgrupador[];
  criadoEm?: string;
}

interface ItemAgrupador {
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
}

interface Lookup { id: number; nome: string; }

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS_PONTOS: ColunaDef[] = [
  { campo: 'codigo',          label: 'Código',     largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'nome',            label: 'Nome',       largura: 260, minLargura: 120, padrao: true },
  { campo: 'modoContagem',    label: 'Modo',       largura: 110, minLargura: 80,  padrao: true },
  { campo: 'valorBase',       label: 'Valor Base', largura: 110, minLargura: 80,  padrao: true },
  { campo: 'pontosGanhos',    label: 'Pontos',     largura: 90,  minLargura: 60,  padrao: true },
  { campo: 'vigencia',        label: 'Vigência',   largura: 160, minLargura: 120, padrao: true },
  { campo: 'ativo',           label: 'Ativo',      largura: 70,  minLargura: 50,  padrao: true },
];
const COLUNAS_CASHBACK: ColunaDef[] = [
  { campo: 'codigo',             label: 'Código',     largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'nome',               label: 'Nome',       largura: 260, minLargura: 120, padrao: true },
  { campo: 'modoContagem',       label: 'Modo',       largura: 110, minLargura: 80,  padrao: true },
  { campo: 'valorBase',          label: 'Valor Base', largura: 110, minLargura: 80,  padrao: true },
  { campo: 'percentualCashback', label: '% Cashback', largura: 110, minLargura: 80,  padrao: true },
  { campo: 'vigencia',           label: 'Vigência',   largura: 160, minLargura: 120, padrao: true },
  { campo: 'ativo',              label: 'Ativo',      largura: 70,  minLargura: 50,  padrao: true },
];

@Component({
  selector: 'app-fidelidade-campanhas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './fidelidade-campanhas.component.html',
  styleUrl: './fidelidade-campanhas.component.scss'
})
export class FidelidadeCampanhasComponent implements OnInit {
  @Input() tipo: TipoFidelidade = 'Pontos';

  private api = `${environment.apiUrl}/fidelidade/campanhas`;
  private apiFiliais = `${environment.apiUrl}/filiais`;
  private apiPagamentos = `${environment.apiUrl}/tiposPagamento`;
  private apiFabricantes = `${environment.apiUrl}/fabricantes`;
  private apiGrupos = `${environment.apiUrl}/grupos-produtos`;
  private apiGruposPrincipais = `${environment.apiUrl}/grupos-principais`;
  private apiSubGrupos = `${environment.apiUrl}/sub-grupos`;
  private apiSecoes = `${environment.apiUrl}/secoes`;
  private apiFamilias = `${environment.apiUrl}/produto-familias`;

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

  private get STORAGE_COLUNAS() { return `zulex_colunas_fidelidade_${this.tipo}`; }

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

  // Novo item agrupador sendo construído
  novoItemTipo = signal<'fabricante' | 'grupoPrincipal' | 'grupo' | 'subGrupo' | 'secao' | 'familia'>('fabricante');
  novoItemId = signal<number | null>(null);

  tituloTela = computed(() => this.tipo === 'Pontos' ? 'Campanhas de Pontos' : 'Campanhas de Cashback');

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

  // ── Grid helpers (sort/columns/resize/drag) ─────────
  private colunasDefault(): ColunaDef[] {
    return this.tipo === 'Pontos' ? COLUNAS_PONTOS : COLUNAS_CASHBACK;
  }

  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return this.colunasDefault().map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return this.colunasDefault().map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunas() { localStorage.setItem(this.STORAGE_COLUNAS, JSON.stringify(this.colunas())); }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunas();
  }

  restaurarColunas() {
    this.colunas.set(this.colunasDefault().map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunas();
  }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortColuna.set(coluna); this.sortDirecao.set('asc'); }
  }
  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }

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
    const def = this.colunasDefault().find(c => c.campo === this.resizeState!.campo);
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
      case 'modoContagem': return c.modoContagem === 'PorVenda' ? 'Por venda' : 'Geral';
      case 'valorBase':    return 'R$ ' + (c.valorBase ?? 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
      case 'pontosGanhos': return String(c.pontosGanhos ?? 0);
      case 'percentualCashback': return (c.percentualCashback ?? 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + '%';
      case 'vigencia': {
        const ini = c.dataHoraInicio ? new Date(c.dataHoraInicio).toLocaleDateString('pt-BR') : '—';
        const fim = c.dataHoraFim ? new Date(c.dataHoraFim).toLocaleDateString('pt-BR') : '∞';
        return `${ini} → ${fim}`;
      }
      case 'ativo': return c.ativo ? 'Sim' : 'Não';
      default: return ((c as any)[campo] ?? '').toString();
    }
  }

  /** Converte string do input em número (substitui type="number"). */
  updNum<K extends keyof Campanha>(campo: K, v: string, decimais = false) {
    const clean = (v ?? '').replace(decimais ? /[^0-9.,]/g : /\D/g, '').replace(',', '.');
    const num = clean === '' ? 0 : (decimais ? parseFloat(clean) : parseInt(clean, 10));
    this.form.update(f => ({ ...f, [campo]: (isNaN(num) ? 0 : num) as any }));
    this.isDirty.set(true);
  }

  // ── Carregamento ────────────────────────────────────
  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.api}?tipo=${this.tipo}`).subscribe({
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

  // ── CRUD ────────────────────────────────────────────
  fechar() {
    this.modo.set('lista');
  }

  incluir() {
    const nova = this.novaCampanha();
    nova.tipo = this.tipo;
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
    const r = await this.modal.confirmar('Confirmar exclusão', `Excluir a campanha "${c.nome}"?`, 'Sim, excluir', 'Cancelar');
    if (!r.confirmado) return;
    this.http.delete(`${this.api}/${c.id}`).subscribe({
      next: () => { this.modal.sucesso('OK', 'Campanha excluída.'); this.carregar(); },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao excluir.')
    });
  }

  async salvar() {
    const f = this.form();
    if (!f.nome?.trim()) { this.erro.set('Nome é obrigatório.'); return; }
    if (!f.valorBase || f.valorBase <= 0) { this.erro.set('Valor base deve ser maior que zero.'); return; }
    if (f.tipo === 'Pontos' && f.pontosGanhos <= 0) { this.erro.set('Pontos ganhos deve ser maior que zero.'); return; }
    if (f.tipo === 'Cashback' && (f.percentualCashback <= 0 || f.percentualCashback > 100)) { this.erro.set('Percentual de cashback entre 0 e 100.'); return; }

    this.erro.set('');
    this.salvando.set(true);

    const body = {
      ...f,
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
      this.modal.confirmar('Fechar', 'Há alterações não salvas. Deseja descartar?', 'Sim', 'Não').then(r => {
        if (r.confirmado) this.modo.set('lista');
      });
    } else {
      this.modo.set('lista');
    }
  }

  // ── Form helpers ────────────────────────────────────
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

  // ── Agrupadores ─────────────────────────────────────
  adicionarItem() {
    const id = this.novoItemId();
    if (!id) { this.modal.aviso('Item', 'Selecione um agrupador primeiro.'); return; }
    const tipo = this.novoItemTipo();
    const lookup = this.lookupPorTipo(tipo);
    const selecionado = lookup.find(l => l.id === id);
    if (!selecionado) return;

    const item: ItemAgrupador = {
      incluir: true,
      descricao: `${this.labelTipo(tipo)}: ${selecionado.nome}`
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
    this.isDirty.set(true);
  }

  removerItem(idx: number) {
    this.form.update(f => ({ ...f, itens: f.itens.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }

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
    return { fabricante: 'Fabricante', grupoPrincipal: 'Grupo Principal', grupo: 'Grupo', subGrupo: 'SubGrupo', secao: 'Seção', familia: 'Família' }[tipo] ?? tipo;
  }

  opcoesAgrupador = computed(() => this.lookupPorTipo(this.novoItemTipo()));

  // ── Utils ───────────────────────────────────────────
  sairDaTela() { this.tab.fecharTabAtiva(); }

  private novaCampanha(): Campanha {
    const hoje = new Date();
    return {
      nome: '',
      descricao: '',
      tipo: this.tipo,
      modoContagem: 'PorVenda',
      valorBase: 0,
      pontosGanhos: 0,
      percentualCashback: 0,
      formaRetirada: 'DescontoNaVenda',
      valorPorPonto: 0.01,
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
      dataHoraInicio: c.dataHoraInicio ? new Date(c.dataHoraInicio).toISOString().slice(0, 16) : '',
      dataHoraFim: c.dataHoraFim ? new Date(c.dataHoraFim).toISOString().slice(0, 16) : null,
      filialIds: c.filialIds ?? [],
      tipoPagamentoIds: c.tipoPagamentoIds ?? [],
      itens: c.itens ?? []
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
  }
}
