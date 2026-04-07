import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface LogCampo { campo: string; valorAnterior?: string; valorAtual?: string; }
interface LogEntry { id: number; realizadoEm: string; acao: string; nomeUsuario: string; campos: LogCampo[]; }
interface LookupItem { id: number; nome: string; extra?: string; }

interface PessoaLookup { id: number; nome: string; tipo: string; cpfCnpj: string; }

interface ContaPagar {
  id?: number;
  descricao: string;
  pessoaId: number | null;
  pessoaNome?: string;
  planoContaId: number | null;
  planoContaDescricao?: string;
  filialId: number;
  filialNome?: string;
  compraId: number | null;
  valor: number;
  desconto: number;
  juros: number;
  multa: number;
  valorFinal: number;
  dataEmissao: string;
  dataVencimento: string;
  dataPagamento: string | null;
  nrDocumento: string | null;
  nrNotaFiscal: string | null;
  observacao: string | null;
  status: number;
  statusDescricao?: string;
  vencido?: boolean;
  recorrenciaGrupo?: string | null;
  recorrenciaParcela?: string | null;
  ativo: boolean;
  criadoEm?: string;
}

interface AbaEdicao {
  conta: ContaPagar;
  form: ContaPagar;
  isDirty: boolean;
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }
type Modo = 'lista' | 'form';

const COLUNAS: ColunaDef[] = [
  { campo: 'dataEmissao', label: 'Lançamento', largura: 100, minLargura: 80, padrao: true },
  { campo: 'dataVencimento', label: 'Vencimento', largura: 100, minLargura: 80, padrao: true },
  { campo: 'descricao', label: 'Descrição', largura: 220, minLargura: 120, padrao: true },
  { campo: 'pessoaNome', label: 'Beneficiário', largura: 160, minLargura: 100, padrao: true },
  { campo: 'planoContaDescricao', label: 'Plano de Contas', largura: 150, minLargura: 80, padrao: true },
  { campo: 'valorFinal', label: 'Valor', largura: 110, minLargura: 70, padrao: true },
  { campo: 'statusDescricao', label: 'Status', largura: 80, minLargura: 60, padrao: true },
  { campo: 'recorrenciaParcela', label: 'Parcela', largura: 70, minLargura: 50, padrao: false },
  { campo: 'nrDocumento', label: 'Documento', largura: 110, minLargura: 70, padrao: false },
  { campo: 'nrNotaFiscal', label: 'Nota Fiscal', largura: 110, minLargura: 70, padrao: false },
  { campo: 'filialNome', label: 'Filial', largura: 120, minLargura: 80, padrao: false },
];

@Component({
  selector: 'app-contas-pagar',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './contas-pagar.component.html',
  styleUrl: './contas-pagar.component.scss'
})
export class ContasPagarComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_contaspagar_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_contaspagar';

  modo = signal<Modo>('lista');
  registros = signal<ContaPagar[]>([]);
  selecionado = signal<ContaPagar | null>(null);
  form = signal<ContaPagar>(this.novoRegistro());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'' | '1' | '2' | '3'>('1');
  filtroFilialId = signal<number | null>(null);
  filtroDataDe = signal('');
  filtroDataAte = signal('');
  sortColuna = signal<string>('dataVencimento');
  sortDirecao = signal<'asc' | 'desc'>('asc');

  // Filtro avançado
  filtroAvancadoAberto = signal(false);
  filtroAvPessoaId = signal<number | null>(null);
  filtroAvPessoaNome = signal('');
  filtroAvPlanoContaId = signal<number | null>(null);
  filtroAvPlanoContaNome = signal('');
  filtroAvNrDocumento = signal('');
  filtroAvNrNotaFiscal = signal('');
  filtroAvValorMin = signal<number | null>(null);
  filtroAvValorMax = signal<number | null>(null);
  filtroAvVencidos = signal(false);

  temFiltroAvancado = computed(() =>
    !!this.filtroAvPessoaId() || !!this.filtroAvPlanoContaId() ||
    !!this.filtroAvNrDocumento() || !!this.filtroAvNrNotaFiscal() ||
    this.filtroAvValorMin() !== null || this.filtroAvValorMax() !== null ||
    this.filtroAvVencidos()
  );
  abasEdicao = signal<AbaEdicao[]>([]);
  abaAtivaId = signal<number | null>(null);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});
  private formOriginal: ContaPagar | null = null;

  // Lookups
  filiais = signal<LookupItem[]>([]);

  // Pesquisa plano de contas (autocomplete sob demanda)
  pcBusca = signal('');
  pcResultados = signal<{ id: number; descricao: string; codigoHierarquico: string }[]>([]);
  pcDropdownAberto = signal(false);
  pcIndice = signal(-1);
  pcSelecionadoNome = signal('');
  private pcTimer: any = null;

  // Pesquisa pessoa (autocomplete sob demanda)
  filtroPessoaTipo = signal<'' | 'fornecedor' | 'cliente'>('');
  pessoaBusca = signal('');
  pessoaResultados = signal<PessoaLookup[]>([]);
  pessoaDropdownAberto = signal(false);
  pessoaIndice = signal(-1);
  pessoaSelecionadaNome = signal('');
  private pessoaTimer: any = null;

  // Tipo lançamento (Normal / Recorrente)
  tipoLancamento = signal<'normal' | 'recorrente'>('normal');
  recQtdMeses = signal(12);
  recDiaVencimento = signal(10);

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  // Log
  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal('');
  logDataFim = signal('');
  carregandoLog = signal(false);

  // Valor final calculado
  valorFinalCalc = computed(() => {
    const f = this.form();
    return f.valor - f.desconto + f.juros + f.multa;
  });

  private apiUrl = `${environment.apiUrl}/contaspagar`;
  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('contas-pagar', acao)) return true;
    const resultado = await this.modal.permissao('contas-pagar', acao);
    if (resultado.tokenLiberacao) this.tokenLiberacao = resultado.tokenLiberacao;
    return resultado.confirmado;
  }

  private headerLiberacao(): { [h: string]: string } {
    if (this.tokenLiberacao) { const h = { 'X-Liberacao': this.tokenLiberacao }; this.tokenLiberacao = null; return h; }
    return {};
  }

  ngOnInit() { this.carregar(); this.carregarLookups(); }
  ngOnDestroy() { sessionStorage.removeItem(this.STATE_KEY); }

  sairDaTela() { sessionStorage.removeItem(this.STATE_KEY); this.tabService.fecharTabAtiva(); }

  private carregarLookups() {
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({
      next: r => this.filiais.set((r.data ?? []).map((f: any) => ({ id: f.id, nome: f.nomeFilial })))
    });
  }

  // ── Pesquisa plano de contas (autocomplete) ───────────────────────
  onPcInput(valor: string) {
    this.pcBusca.set(valor);
    this.pcIndice.set(-1);
    if (this.pcTimer) clearTimeout(this.pcTimer);
    if (valor.trim().length < 2) { this.pcResultados.set([]); this.pcDropdownAberto.set(false); return; }
    this.pcTimer = setTimeout(() => this.pesquisarPc(valor.trim()), 300);
  }

  private pesquisarPc(termo: string) {
    this.http.get<any>(`${environment.apiUrl}/planoscontas/pesquisar?termo=${encodeURIComponent(termo)}`).subscribe({
      next: r => {
        this.pcResultados.set(r.data ?? []);
        this.pcDropdownAberto.set((r.data ?? []).length > 0);
      }
    });
  }

  onPcFocus() { if (this.pcResultados().length > 0) this.pcDropdownAberto.set(true); }
  onPcBlur() { setTimeout(() => this.pcDropdownAberto.set(false), 200); }

  selecionarPc(pc: { id: number; descricao: string; codigoHierarquico: string }) {
    this.upd('planoContaId', pc.id);
    const label = `${pc.codigoHierarquico} - ${pc.descricao}`;
    this.pcSelecionadoNome.set(label);
    this.pcBusca.set(label);
    this.pcDropdownAberto.set(false);
  }

  limparPc() {
    this.upd('planoContaId', null);
    this.pcSelecionadoNome.set('');
    this.pcBusca.set('');
    this.pcResultados.set([]);
  }

  onPcKeydown(e: KeyboardEvent) {
    const lista = this.pcResultados();
    if (e.key === 'ArrowDown') { e.preventDefault(); this.pcIndice.update(i => Math.min(i + 1, lista.length - 1)); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); this.pcIndice.update(i => Math.max(i - 1, 0)); }
    else if (e.key === 'Enter') { e.preventDefault(); const idx = this.pcIndice(); if (idx >= 0 && idx < lista.length) this.selecionarPc(lista[idx]); }
    else if (e.key === 'Escape') { this.pcDropdownAberto.set(false); }
  }

  // ── Pesquisa pessoa (autocomplete) ────────────────────────────────
  onPessoaInput(valor: string) {
    this.pessoaBusca.set(valor);
    this.pessoaIndice.set(-1);
    if (this.pessoaTimer) clearTimeout(this.pessoaTimer);
    if (valor.trim().length < 3) { this.pessoaResultados.set([]); this.pessoaDropdownAberto.set(false); return; }
    this.pessoaTimer = setTimeout(() => this.pesquisarPessoas(valor.trim()), 300);
  }

  private pesquisarPessoas(termo: string) {
    const tipo = this.filtroPessoaTipo();
    let url = `${environment.apiUrl}/pessoas/pesquisar?termo=${encodeURIComponent(termo)}`;
    if (tipo) url += `&tipo=${tipo}`;
    this.http.get<any>(url).subscribe({
      next: r => {
        const lista: PessoaLookup[] = (r.data ?? []).map((p: any) => ({
          id: p.id, nome: p.nome, tipo: p.ehFornecedor ? 'fornecedor' : 'cliente', cpfCnpj: p.cpfCnpj ?? ''
        }));
        this.pessoaResultados.set(lista);
        this.pessoaDropdownAberto.set(lista.length > 0);
      }
    });
  }

  onPessoaFocus() {
    if (this.pessoaResultados().length > 0) this.pessoaDropdownAberto.set(true);
  }

  onPessoaBlur() {
    setTimeout(() => this.pessoaDropdownAberto.set(false), 200);
  }

  selecionarPessoa(p: PessoaLookup) {
    this.upd('pessoaId', p.id);
    this.pessoaSelecionadaNome.set(p.nome);
    this.pessoaBusca.set(p.nome);
    this.pessoaDropdownAberto.set(false);
  }

  limparPessoa() {
    this.upd('pessoaId', null);
    this.pessoaSelecionadaNome.set('');
    this.pessoaBusca.set('');
    this.pessoaResultados.set([]);
  }

  onPessoaKeydown(e: KeyboardEvent) {
    const lista = this.pessoaResultados();
    if (e.key === 'ArrowDown') { e.preventDefault(); this.pessoaIndice.update(i => Math.min(i + 1, lista.length - 1)); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); this.pessoaIndice.update(i => Math.max(i - 1, 0)); }
    else if (e.key === 'Enter') { e.preventDefault(); const idx = this.pessoaIndice(); if (idx >= 0 && idx < lista.length) this.selecionarPessoa(lista[idx]); }
    else if (e.key === 'Escape') { this.pessoaDropdownAberto.set(false); }
  }

  // ── Data ───────────────────────────────────────────────────────────
  private primeiroCarregamento = true;

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => { this.registros.set(r.data ?? []); this.carregando.set(false);
        if (this.primeiroCarregamento) { this.primeiroCarregamento = false; this.restaurarEstado(); }
      },
      error: (e) => { this.carregando.set(false);
        if (e.status === 403) { this.modal.permissao('contas-pagar', 'c').then(r => { if (r.confirmado) this.carregar(); else this.tabService.fecharTabAtiva(); }); }
      }
    });
  }

  registrosFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const filialId = this.filtroFilialId();
    const dataDe = this.filtroDataDe();
    const dataAte = this.filtroDataAte();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    // Filtro avançado
    const avPessoaId = this.filtroAvPessoaId();
    const avPlanoContaId = this.filtroAvPlanoContaId();
    const avDoc = this.normalizar(this.filtroAvNrDocumento());
    const avNf = this.normalizar(this.filtroAvNrNotaFiscal());
    const avValorMin = this.filtroAvValorMin();
    const avValorMax = this.filtroAvValorMax();
    const avVencidos = this.filtroAvVencidos();

    const lista = this.registros().filter(r => {
      if (!r.ativo) return false;
      // Status
      if (status && r.status !== Number(status)) return false;
      // Filial
      if (filialId && r.filialId !== filialId) return false;
      // Intervalo de datas (vencimento)
      if (dataDe && r.dataVencimento < dataDe) return false;
      if (dataAte && r.dataVencimento > dataAte) return false;
      // Busca texto
      if (termo.length >= 2) {
        const match = this.normalizar(r.descricao).includes(termo) ||
          this.normalizar(r.pessoaNome ?? '').includes(termo) ||
          (r.nrDocumento ?? '').includes(termo) ||
          (r.nrNotaFiscal ?? '').includes(termo);
        if (!match) return false;
      }
      // Avançado
      if (avPessoaId && r.pessoaId !== avPessoaId) return false;
      if (avPlanoContaId && r.planoContaId !== avPlanoContaId) return false;
      if (avDoc && !this.normalizar(r.nrDocumento ?? '').includes(avDoc)) return false;
      if (avNf && !this.normalizar(r.nrNotaFiscal ?? '').includes(avNf)) return false;
      if (avValorMin !== null && r.valorFinal < avValorMin) return false;
      if (avValorMax !== null && r.valorFinal > avValorMax) return false;
      if (avVencidos && !r.vencido) return false;
      return true;
    });

    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  limparFiltroAvancado() {
    this.filtroAvPessoaId.set(null); this.filtroAvPessoaNome.set('');
    this.filtroAvPlanoContaId.set(null); this.filtroAvPlanoContaNome.set('');
    this.filtroAvNrDocumento.set(''); this.filtroAvNrNotaFiscal.set('');
    this.filtroAvValorMin.set(null); this.filtroAvValorMax.set(null);
    this.filtroAvVencidos.set(false);
  }

  // Autocomplete do filtro avançado (pessoa)
  filtroAvPessoaResultados = signal<PessoaLookup[]>([]);
  filtroAvPessoaDropdown = signal(false);
  private filtroAvPessoaTimer: any = null;

  onFiltroAvPessoaInput(valor: string) {
    this.filtroAvPessoaNome.set(valor);
    if (this.filtroAvPessoaTimer) clearTimeout(this.filtroAvPessoaTimer);
    if (valor.trim().length < 3) { this.filtroAvPessoaResultados.set([]); this.filtroAvPessoaDropdown.set(false); return; }
    this.filtroAvPessoaTimer = setTimeout(() => {
      this.http.get<any>(`${environment.apiUrl}/pessoas/pesquisar?termo=${encodeURIComponent(valor.trim())}`).subscribe({
        next: r => { this.filtroAvPessoaResultados.set((r.data ?? []).map((p: any) => ({ id: p.id, nome: p.nome, tipo: p.ehFornecedor ? 'fornecedor' : 'cliente', cpfCnpj: p.cpfCnpj ?? '' }))); this.filtroAvPessoaDropdown.set((r.data ?? []).length > 0); }
      });
    }, 300);
  }
  onFiltroAvPessoaBlur() { setTimeout(() => this.filtroAvPessoaDropdown.set(false), 200); }
  selecionarFiltroAvPessoa(p: PessoaLookup) { this.filtroAvPessoaId.set(p.id); this.filtroAvPessoaNome.set(p.nome); this.filtroAvPessoaDropdown.set(false); }
  limparFiltroAvPessoa() { this.filtroAvPessoaId.set(null); this.filtroAvPessoaNome.set(''); this.filtroAvPessoaResultados.set([]); }

  // Autocomplete do filtro avançado (plano de contas)
  filtroAvPcResultados = signal<{ id: number; descricao: string; codigoHierarquico: string }[]>([]);
  filtroAvPcDropdown = signal(false);
  private filtroAvPcTimer: any = null;

  onFiltroAvPcInput(valor: string) {
    this.filtroAvPlanoContaNome.set(valor);
    if (this.filtroAvPcTimer) clearTimeout(this.filtroAvPcTimer);
    if (valor.trim().length < 2) { this.filtroAvPcResultados.set([]); this.filtroAvPcDropdown.set(false); return; }
    this.filtroAvPcTimer = setTimeout(() => {
      this.http.get<any>(`${environment.apiUrl}/planoscontas/pesquisar?termo=${encodeURIComponent(valor.trim())}`).subscribe({
        next: r => { this.filtroAvPcResultados.set(r.data ?? []); this.filtroAvPcDropdown.set((r.data ?? []).length > 0); }
      });
    }, 300);
  }
  onFiltroAvPcBlur() { setTimeout(() => this.filtroAvPcDropdown.set(false), 200); }
  selecionarFiltroAvPc(pc: { id: number; descricao: string; codigoHierarquico: string }) { this.filtroAvPlanoContaId.set(pc.id); this.filtroAvPlanoContaNome.set(`${pc.codigoHierarquico} - ${pc.descricao}`); this.filtroAvPcDropdown.set(false); }
  limparFiltroAvPc() { this.filtroAvPlanoContaId.set(null); this.filtroAvPlanoContaNome.set(''); this.filtroAvPcResultados.set([]); }

  private normalizar(s: string): string { return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim(); }

  getCellValue(r: ContaPagar, campo: string): string {
    if (campo === 'dataEmissao' || campo === 'dataVencimento' || campo === 'dataPagamento') {
      const v = (r as any)[campo];
      if (!v) return '';
      return new Date(v).toLocaleDateString('pt-BR');
    }
    if (campo === 'valorFinal' || campo === 'valor') {
      const v = (r as any)[campo];
      return typeof v === 'number' ? v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) : '';
    }
    const v = (r as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Não';
    return v ?? '';
  }

  getRowClass(r: ContaPagar): string {
    if (r.status === 2) return 'row-pago';
    if (r.status === 3) return 'row-cancelado';
    if (r.vencido) return 'row-vencido';
    return '';
  }

  selecionar(r: ContaPagar) { this.selecionado.set(r); }
  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortColuna.set(coluna); this.sortDirecao.set('asc'); }
  }
  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }

  // ── Columns ────────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try { const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) { const saved: ColunaEstado[] = JSON.parse(json);
        return COLUNAS.map(def => { const s = saved.find(c => c.campo === def.campo); return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura }; }); }
    } catch {} return COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }
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

  // ── State persistence ─────────────────────────────────────────────
  private persistirEstado() { this.salvarEstadoAbaAtiva(); const abas = this.abasEdicao(); if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; } sessionStorage.setItem(this.STATE_KEY, JSON.stringify({ abasIds: abas.map(a => a.conta.id), abaAtivaId: this.abaAtivaId() })); }
  private restaurarEstado() { try { const json = sessionStorage.getItem(this.STATE_KEY); if (!json) return; const state = JSON.parse(json); sessionStorage.removeItem(this.STATE_KEY); if (state.abasIds?.length > 0) { for (const id of state.abasIds) { const r = this.registros().find(x => x.id === id); if (r) this.restaurarAba(r, id === state.abaAtivaId); } } } catch {} }
  private restaurarAba(r: ContaPagar, ativar: boolean) { if (this.abasEdicao().find(a => a.conta.id === r.id)) return; const aba: AbaEdicao = { conta: { ...r }, form: this.clonar(r), isDirty: false }; this.abasEdicao.update(abas => [...abas, aba]); if (ativar) this.ativarAba(r.id!); }

  // ── CRUD ───────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.form.set(this.novoRegistro());
    this.formOriginal = this.clonar(this.novoRegistro());
    this.tipoLancamento.set('normal');
    this.recQtdMeses.set(12);
    this.recDiaVencimento.set(10);
    this.pessoaSelecionadaNome.set(''); this.pessoaBusca.set(''); this.pessoaResultados.set([]);
    this.pcSelecionadoNome.set(''); this.pcBusca.set(''); this.pcResultados.set([]);
    this.modoEdicao.set(false);
    this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({});
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const r = this.selecionado(); if (!r?.id) return;
    const ja = this.abasEdicao().find(a => a.conta.id === r.id);
    if (ja) { this.ativarAba(r.id); return; }
    const aba: AbaEdicao = { conta: { ...r }, form: this.clonar(r), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    this.abaAtivaId.set(r.id!); this.form.set(this.clonar(r)); this.formOriginal = this.clonar(r);
    this.pessoaSelecionadaNome.set(r.pessoaNome ?? ''); this.pessoaBusca.set(r.pessoaNome ?? '');
    const pcLabel = r.planoContaDescricao ? `${r.planoContaDescricao}` : '';
    this.pcSelecionadoNome.set(pcLabel); this.pcBusca.set(pcLabel);
    this.modoEdicao.set(true); this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({});
    this.modo.set('form');
  }

  ativarAba(id: number) { this.salvarEstadoAbaAtiva(); const aba = this.abasEdicao().find(a => a.conta.id === id); if (!aba) return; this.abaAtivaId.set(id); this.form.set(this.clonar(aba.form)); this.formOriginal = this.clonar(aba.form); this.pessoaSelecionadaNome.set(aba.form.pessoaNome ?? ''); this.pessoaBusca.set(aba.form.pessoaNome ?? ''); const pcL = aba.form.planoContaDescricao ?? ''; this.pcSelecionadoNome.set(pcL); this.pcBusca.set(pcL); this.isDirty.set(aba.isDirty); this.modoEdicao.set(true); this.erro.set(''); this.errosCampos.set({}); this.modo.set('form'); }
  fecharAba(id: number) { this.abasEdicao.update(abas => abas.filter(a => a.conta.id !== id)); if (this.abaAtivaId() === id) { const rest = this.abasEdicao(); if (rest.length > 0) this.ativarAba(rest[rest.length - 1].conta.id!); else { this.modo.set('lista'); this.abaAtivaId.set(null); } } }
  fechar() { this.modo.set('lista'); this.carregar(); }
  fecharForm() { if (this.modoEdicao()) { const id = this.abaAtivaId(); if (id) this.fecharAba(id); else this.modo.set('lista'); } else this.modo.set('lista'); }
  cancelarEdicao() { if (this.formOriginal) { this.form.set(this.clonar(this.formOriginal)); this.isDirty.set(false); const id = this.abaAtivaId(); if (id) this.abasEdicao.update(abas => abas.map(a => a.conta.id === id ? { ...a, isDirty: false } : a)); } }
  private salvarEstadoAbaAtiva() { const id = this.abaAtivaId(); if (!id || this.modo() !== 'form') return; this.abasEdicao.update(abas => abas.map(a => a.conta.id === id ? { ...a, form: this.clonar(this.form()), isDirty: this.isDirty() } : a)); }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const erros: Record<string, string> = {};
    const f = this.form();
    if (!f.descricao.trim()) erros['descricao'] = 'Descrição é obrigatória.';
    if (f.valor <= 0) erros['valor'] = 'Valor deve ser maior que zero.';
    if (!f.filialId) erros['filialId'] = 'Filial é obrigatória.';
    if (Object.keys(erros).length) { this.errosCampos.set(erros); return; }
    this.errosCampos.set({}); this.salvando.set(true);
    const headers = this.headerLiberacao();

    // Recorrente (novo)
    if (!this.modoEdicao() && this.tipoLancamento() === 'recorrente') {
      const body = {
        modelo: {
          descricao: f.descricao, pessoaId: f.pessoaId || null, planoContaId: f.planoContaId || null,
          filialId: f.filialId, valor: f.valor, desconto: 0, juros: 0, multa: 0,
          dataEmissao: f.dataEmissao, dataVencimento: f.dataVencimento,
          nrDocumento: f.nrDocumento, observacao: f.observacao, status: 1, ativo: true
        },
        quantidadeMeses: this.recQtdMeses(),
        diaVencimento: this.recDiaVencimento()
      };
      this.http.post<any>(`${this.apiUrl}/recorrente`, body, { headers }).subscribe({
        next: (r: any) => {
          this.salvando.set(false);
          this.modal.aviso('Lançamento Recorrente', `${r.data?.length ?? 0} parcelas criadas com sucesso.`);
          this.carregar(); this.modo.set('lista');
        },
        error: (err) => { this.erro.set(err.error?.message || 'Erro ao criar recorrência.'); this.salvando.set(false); }
      });
      return;
    }

    // Normal (novo ou edição)
    const body: any = {
      descricao: f.descricao, pessoaId: f.pessoaId || null, planoContaId: f.planoContaId || null,
      filialId: f.filialId, compraId: f.compraId || null, valor: f.valor, desconto: f.desconto,
      juros: f.juros, multa: f.multa, dataEmissao: f.dataEmissao, dataVencimento: f.dataVencimento,
      dataPagamento: f.dataPagamento || null, nrDocumento: f.nrDocumento, nrNotaFiscal: f.nrNotaFiscal,
      observacao: f.observacao, status: f.status, ativo: f.ativo
    };
    const op$ = this.modoEdicao() ? this.http.put(`${this.apiUrl}/${f.id}`, body, { headers }) : this.http.post<any>(this.apiUrl, body, { headers });
    op$.subscribe({
      next: (r: any) => { this.salvando.set(false); this.isDirty.set(false); if (this.modoEdicao()) this.fecharAba(f.id!); this.carregar(); this.modo.set('lista'); },
      error: (err) => { this.erro.set(err.error?.message || 'Erro ao salvar.'); this.salvando.set(false); }
    });
  }

  async excluir() {
    const r = this.selecionado(); if (!r?.id) return;
    const resultado = await this.modal.confirmar('Confirmar Exclusão', `Deseja excluir "${r.descricao}"?`, 'Sim, excluir', 'Não, manter');
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${r.id}`, { headers }).subscribe({
      next: async (res: any) => { this.excluindo.set(false); this.selecionado.set(null); this.fecharAba(r.id!); this.carregar();
        if (res.resultado === 'desativado') await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.');
      },
      error: (err) => { this.excluindo.set(false); this.modal.erro('Erro', err.error?.message || 'Erro ao excluir.'); }
    });
  }

  // ── Form helpers ───────────────────────────────────────────────────
  upd(campo: keyof ContaPagar, v: any) { this.form.update(f => ({ ...f, [campo]: v })); this.isDirty.set(true); this.atualizarDirtyAba(); }
  erroCampo(campo: string): string { return this.errosCampos()[campo] ?? ''; }
  private atualizarDirtyAba() { const id = this.abaAtivaId(); if (!id) return; this.abasEdicao.update(abas => abas.map(a => a.conta.id === id ? { ...a, isDirty: true } : a)); }

  hoje(): string { return new Date().toISOString().substring(0, 10); }

  // ── Log ────────────────────────────────────────────────────────────
  abrirLog() { const r = this.selecionado(); if (!r?.id) return; this.modalLog.set(true); this.logRegistros.set([]); this.logSelecionado.set(null); this.filtrarLog(); }
  fecharLog() { this.modalLog.set(false); }
  filtrarLog() { const r = this.selecionado(); if (!r?.id) return; this.carregandoLog.set(true); let url = `${this.apiUrl}/${r.id}/log`; const params: string[] = []; if (this.logDataInicio()) params.push(`dataInicio=${this.logDataInicio()}`); if (this.logDataFim()) params.push(`dataFim=${this.logDataFim()}`); if (params.length) url += '?' + params.join('&'); this.http.get<any>(url).subscribe({ next: res => { this.logRegistros.set(res.data ?? []); this.carregandoLog.set(false); if (res.data?.length > 0) this.selecionarLogEntry(res.data[0]); }, error: () => this.carregandoLog.set(false) }); }
  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }
  acaoCss(acao: string): string { const map: Record<string, string> = { 'CRIAÇÃO': 'log-badge badge-criacao', 'ALTERAÇÃO': 'log-badge badge-alteracao', 'EXCLUSÃO': 'log-badge badge-exclusao', 'DESATIVAÇÃO': 'log-badge badge-desativacao' }; return map[acao] ?? 'log-badge'; }

  // ── Utils ──────────────────────────────────────────────────────────
  private novoRegistro(): ContaPagar {
    return { descricao: '', pessoaId: null, planoContaId: null, filialId: 0,
      compraId: null, valor: 0, desconto: 0, juros: 0, multa: 0, valorFinal: 0,
      dataEmissao: this.hoje(), dataVencimento: this.hoje(), dataPagamento: null,
      nrDocumento: null, nrNotaFiscal: null, observacao: null, status: 1, ativo: true };
  }
  private clonar<T>(obj: T): T { return JSON.parse(JSON.stringify(obj)); }
}
