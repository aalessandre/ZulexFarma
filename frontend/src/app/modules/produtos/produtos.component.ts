import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

type AbaAtiva = 'produtos' | 'grupo-principal' | 'grupo' | 'sub-grupo' | 'secao' | 'familia' | 'kit';

const ABAS_IMPLEMENTADAS: AbaAtiva[] = ['produtos', 'grupo-principal', 'grupo', 'sub-grupo', 'secao', 'familia'];

interface AbaConfig {
  id: AbaAtiva;
  label: string;
  cor: string;
}

interface Classificacao {
  id?: number;
  nome: string;
  comissaoPercentual: number;
  descontoMinimo: number;
  descontoMaximo: number;
  descontoMaximoComSenha: number;
  projecaoLucro: number;
  markupPadrao: number;
  priorizar?: string;
  controlarLotesVencimento: boolean;
  informarPrescritorVenda: boolean;
  imprimirEtiqueta: boolean;
  permitirDescontoPrazo: boolean;
  permitirPromocao: boolean;
  permitirDescontosProgressivos: boolean;
  ativo: boolean;
  criadoEm?: string;
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

interface LogCampo {
  campo: string;
  valorAnterior?: string;
  valorAtual?: string;
}

interface LogEntry {
  id: number;
  realizadoEm: string;
  acao: string;
  nomeUsuario: string;
  campos: LogCampo[];
}

// ── Produto interfaces ──────────────────────────────────────────────
interface ProdutoList { id: number; nome: string; codigoBarras?: string; fabricanteNome?: string; grupoPrincipalNome?: string; ativo: boolean; eliminado: boolean; criadoEm: string; }

interface ProdutoBarrasItem { id?: number; barras: string; }
interface ProdutoMsItem { id?: number; numeroMs: string; }
interface ProdutoSubstanciaItem { id?: number; substanciaId: number; substanciaNome?: string; }
interface ProdutoFornecedorItem { id?: number; filialId: number; fornecedorId: number; fornecedorNome?: string; codigoProdutoFornecedor?: string; nomeProduto?: string; fracao: number; }
interface ProdutoFiscalItem { id?: number; filialId: number; ncmId?: number; ncmCodigo?: string; cest?: string; origemMercadoria?: string; cstIcms?: string; csosn?: string; aliquotaIcms: number; cstPis?: string; aliquotaPis: number; cstCofins?: string; aliquotaCofins: number; cstIpi?: string; aliquotaIpi: number; }
interface ProdutoDadosItem {
  id?: number; filialId: number; filialNome?: string;
  estoqueAtual: number; estoqueMinimo: number; estoqueMaximo: number; demanda: number; curvaAbc?: string; estoqueDeposito: number;
  ultimaCompraUnitario: number; ultimaCompraSt: number; ultimaCompraOutros: number; ultimaCompraIpi: number; ultimaCompraFpc: number; ultimaCompraBoleto: number; ultimaCompraDifal: number; ultimaCompraFrete: number;
  custoMedio: number; projecaoLucro: number; markup: number; valorVenda: number; pmc: number;
  valorPromocao: number; valorPromocaoPrazo: number; promocaoInicio?: string; promocaoFim?: string;
  descontoMinimo: number; descontoMaxSemSenha: number; descontoMaxComSenha: number;
  comissao: number; valorIncentivo: number;
  produtoLocalId?: number; produtoLocalNome?: string; secaoId?: number; produtoFamiliaId?: number;
  nomeEtiqueta?: string; mensagem?: string;
  bloquearDesconto: boolean; bloquearPromocao: boolean; naoAtualizarAbcfarma: boolean; naoAtualizarGestorTributario: boolean; bloquearCompras: boolean; produtoFormula: boolean; bloquearComissao: boolean; bloquearCoberturaOferta: boolean; usoContinuo: boolean; avisoFracao: boolean;
  ultimaCompraEm?: string; ultimaVendaEm?: string;
}

interface ProdutoForm {
  nome: string; codigoBarras?: string; qtdeEmbalagem: number; precoFp?: number;
  lista: string; fracao: number; ativo: boolean; eliminado: boolean; criadoEm?: string;
  fabricanteId?: number; grupoPrincipalId?: number; grupoProdutoId?: number; subGrupoId?: number; ncmId?: number;
  fabricanteNome?: string; grupoPrincipalNome?: string; grupoProdutoNome?: string; subGrupoNome?: string; ncmCodigo?: string;
  barras: ProdutoBarrasItem[]; registrosMs: ProdutoMsItem[];
  substancias: ProdutoSubstanciaItem[]; fornecedores: ProdutoFornecedorItem[];
  fiscais: ProdutoFiscalItem[]; dados: ProdutoDadosItem[];
}

interface LookupItem { id: number; texto: string; }

const CLASSIFICACAO_COLUNAS: ColunaDef[] = [
  { campo: 'id',                label: 'ID',           largura: 60,  minLargura: 50,  padrao: true },
  { campo: 'nome',              label: 'Nome',         largura: 250, minLargura: 150, padrao: true },
  { campo: 'comissaoPercentual', label: '% Comissão',  largura: 100, minLargura: 70,  padrao: true },
  { campo: 'markupPadrao',      label: '% Markup',     largura: 100, minLargura: 70,  padrao: true },
  { campo: 'projecaoLucro',     label: '% Proj Lucro', largura: 100, minLargura: 70,  padrao: true },
  { campo: 'ativo',             label: 'Ativo',        largura: 60,  minLargura: 50,  padrao: true },
];

const API_MAP: Record<string, string> = {
  'grupo-principal': '/grupos-principais',
  'grupo':           '/grupos-produtos',
  'sub-grupo':       '/sub-grupos',
  'secao':           '/secoes',
  'familia':         '/produto-familias',
};

const NOME_ABA_MAP: Record<string, string> = {
  'grupo-principal': 'Grupo Principal',
  'grupo':           'Grupo',
  'sub-grupo':       'Sub Grupo',
  'secao':           'Seção',
  'familia':         'Família',
};

@Component({
  selector: 'app-produtos',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './produtos.component.html',
  styleUrl: './produtos.component.scss'
})
export class ProdutosComponent implements OnInit, OnDestroy {

  // ── Abas ──────────────────────────────────────────────────────────
  abaAtiva = signal<AbaAtiva>('produtos');

  abas: AbaConfig[] = [
    { id: 'produtos',        label: 'Produtos',       cor: '#2980b9' },
    { id: 'grupo-principal', label: 'Grupo Principal', cor: '#e74c3c' },
    { id: 'grupo',           label: 'Grupo',           cor: '#f39c12' },
    { id: 'sub-grupo',       label: 'Sub Grupo',       cor: '#27ae60' },
    { id: 'secao',           label: 'Seção',           cor: '#1abc9c' },
    { id: 'familia',         label: 'Família',         cor: '#8e44ad' },
    { id: 'kit',             label: 'Kit',             cor: '#d35400' },
  ];

  apiUrlAtual = computed(() => {
    const aba = this.abaAtiva();
    const path = API_MAP[aba];
    return path ? `${environment.apiUrl}${path}` : '';
  });

  // ── Estado CRUD ───────────────────────────────────────────────────
  modo = signal<'lista' | 'form'>('lista');
  registros = signal<Classificacao[]>([]);
  registroSelecionado = signal<Classificacao | null>(null);
  registroForm = signal<Classificacao>(this.novoRegistro());
  private formOriginal: Classificacao | null = null;

  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');

  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('nome');
  sortDirecao = signal<'asc' | 'desc'>('asc');

  // ── Colunas ───────────────────────────────────────────────────────
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_classificacao';
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;

  // ── Log ───────────────────────────────────────────────────────────
  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  carregandoLog = signal(false);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal<string>(this.hoje(-30));
  logDataFim    = signal<string>(this.hoje(0));

  // ── Estado Produto ─────────────────────────────────────────────────
  private produtoApiUrl = `${environment.apiUrl}/produtos`;
  produtoRegistros = signal<ProdutoList[]>([]);
  produtoSelecionado = signal<ProdutoList | null>(null);
  produtoForm = signal<ProdutoForm>(this.novoProdutoForm());
  private produtoFormOriginal = '';
  produtoEditandoId = signal<number | null>(null);
  produtoBusca = signal('');
  filialSelecionada = signal<number>(0);
  filialLocal = signal<number>(0);
  filiaisDisponiveis = signal<{ id: number; nome: string }[]>([]);
  isFilialDiferente = computed(() => this.filialLocal() > 0 && this.filialSelecionada() !== this.filialLocal());

  dadosFilialAtual = computed(() => {
    const fId = this.filialSelecionada();
    return this.produtoForm().dados.find(d => d.filialId === fId) ?? null;
  });

  dadosFilialIdx = computed(() => {
    const fId = this.filialSelecionada();
    return this.produtoForm().dados.findIndex(d => d.filialId === fId);
  });

  fiscalFilialAtual = computed(() => {
    const fId = this.filialSelecionada();
    return this.produtoForm().fiscais.find(f => f.filialId === fId) ?? null;
  });

  fiscalFilialIdx = computed(() => {
    const fId = this.filialSelecionada();
    return this.produtoForm().fiscais.findIndex(f => f.filialId === fId);
  });
  private buscaProdutoTimer: any = null;

  produtosFiltrados = computed(() => {
    let lista = this.produtoRegistros();
    const st = this.filtroStatus();
    if (st === 'ativos') lista = lista.filter(r => r.ativo && !r.eliminado);
    else if (st === 'inativos') lista = lista.filter(r => !r.ativo || r.eliminado);
    return lista;
  });

  // ── Lookups (autocomplete classificação) ────────────────────────────
  lookupFabricante = signal<LookupItem[]>([]);
  lookupGrupoPrincipal = signal<LookupItem[]>([]);
  lookupGrupo = signal<LookupItem[]>([]);
  lookupSubGrupo = signal<LookupItem[]>([]);
  lookupNcm = signal<LookupItem[]>([]);
  lookupAberto = signal<string | null>(null);
  lookupSubstancia = signal<LookupItem[]>([]);
  substanciaBusca = signal('');
  lookupFornecedor = signal<LookupItem[]>([]);
  fornecedorBuscaIdx = signal<number | null>(null);
  lookupLocal = signal<LookupItem[]>([]);
  lookupFamilia = signal<LookupItem[]>([]);
  lookupSecao = signal<LookupItem[]>([]);
  private lookupTimers: Record<string, any> = {};
  private lookupCaches: Record<string, { data: any[]; ts: number }> = {};

  // ── Token Liberação ───────────────────────────────────────────────
  private tokenLiberacao: string | null = null;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    const restaurou = this.restaurarEstado();
    if (!restaurou && !this.isProdutoAba()) this.carregar();
    this.carregarFiliais();
  }

  private carregarFiliais() {
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({
      next: resp => {
        const filiais = (resp?.data ?? []).map((f: any) => ({ id: f.id, nome: f.nomeFilial || `Filial ${f.id}` }));
        this.filiaisDisponiveis.set(filiais);
        // Buscar filial local via status do sync
        this.http.get<any>(`${environment.apiUrl}/sync/status`).subscribe({
          next: sr => {
            const cod = sr?.data?.filialCodigo;
            const filialLocal = filiais.find((f: any) => f.id === cod);
            const fId = filialLocal?.id ?? filiais[0]?.id ?? 0;
            this.filialSelecionada.set(fId);
            this.filialLocal.set(fId);
          },
          error: () => { if (filiais.length) this.filialSelecionada.set(filiais[0].id); }
        });
      }
    });
  }

  // ── Permissões ────────────────────────────────────────────────────

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('gerenciar-produtos', acao)) return true;
    const resultado = await this.modal.permissao('gerenciar-produtos', acao);
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

  // ── Navegação de abas ─────────────────────────────────────────────

  selecionarAba(id: AbaAtiva) {
    this.abaAtiva.set(id);
    this.modo.set('lista');
    this.registroSelecionado.set(null);
    this.produtoSelecionado.set(null);
    if (id === 'produtos') return;
    if (ABAS_IMPLEMENTADAS.includes(id)) {
      this.carregar();
    }
  }

  isAbaImplementada(): boolean {
    return ABAS_IMPLEMENTADAS.includes(this.abaAtiva());
  }

  isAbaSimples(): boolean {
    return this.abaAtiva() === 'familia';
  }

  getNomeAba(): string {
    return NOME_ABA_MAP[this.abaAtiva()] ?? this.abaAtiva();
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  // ── Dados ─────────────────────────────────────────────────────────

  carregar() {
    const url = this.apiUrlAtual();
    if (!url) return;
    this.carregando.set(true);
    this.http.get<any>(url).subscribe({
      next: r => {
        this.registros.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('gerenciar-produtos', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  registrosFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.registros().filter(r => {
      if (status === 'ativos'   && !r.ativo) return false;
      if (status === 'inativos' &&  r.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(r.nome).includes(termo);
    });

    if (!col) return lista;

    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean'
        ? (va === vb ? 0 : va ? -1 : 1)
        : typeof va === 'number'
          ? va - (vb as number)
          : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string {
    return (s ?? '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  getCellValue(r: Classificacao, campo: string): string {
    const v = (r as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Não';
    if (typeof v === 'number') return v.toString();
    return v ?? '';
  }

  selecionar(r: Classificacao) { this.registroSelecionado.set(r); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  // ── Colunas: resize ───────────────────────────────────────────────

  iniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation();
    e.preventDefault();
    this.resizeState = { campo, startX: e.clientX, startWidth: largura };
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    if (!this.resizeState) return;
    const delta = e.clientX - this.resizeState.startX;
    const def = CLASSIFICACAO_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── Colunas: visibilidade ─────────────────────────────────────────

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols =>
      cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c)
    );
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(CLASSIFICACAO_COLUNAS.map(d => ({ ...d, visivel: d.padrao })));
    localStorage.removeItem(this.STORAGE_KEY_COLUNAS);
  }

  private carregarColunas(): ColunaEstado[] {
    let salvo: { campo: string; visivel: boolean; largura: number }[] = [];
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) salvo = JSON.parse(json);
    } catch {}

    return CLASSIFICACAO_COLUNAS.map(def => {
      const s = salvo.find(x => x.campo === def.campo);
      return {
        ...def,
        visivel: s ? s.visivel : def.padrao,
        largura: s?.largura ?? def.largura
      };
    });
  }

  private salvarColunasStorage() {
    const estado = this.colunas().map(c => ({
      campo: c.campo, visivel: c.visivel, largura: Math.round(c.largura)
    }));
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(estado));
  }

  // ── CRUD ──────────────────────────────────────────────────────────

  async incluir() {
    if (!this.isAbaImplementada()) return;
    if (!await this.verificarPermissao('i')) return;
    const novo = this.novoRegistro();
    this.registroForm.set(novo);
    this.formOriginal = { ...novo };
    this.erro.set('');
    this.isDirty.set(false);
    this.modoEdicao.set(false);
    this.modo.set('form');
  }

  async editar() {
    if (!this.isAbaImplementada()) return;
    const r = this.registroSelecionado();
    if (!r?.id) return;
    if (!await this.verificarPermissao('a')) return;
    this.carregando.set(true);
    const url = this.apiUrlAtual();
    this.http.get<any>(`${url}/${r.id}`).subscribe({
      next: resp => {
        this.carregando.set(false);
        const d = resp.data;
        const form: Classificacao = {
          id: d.id, nome: d.nome, comissaoPercentual: d.comissaoPercentual,
          descontoMinimo: d.descontoMinimo, descontoMaximo: d.descontoMaximo,
          descontoMaximoComSenha: d.descontoMaximoComSenha,
          projecaoLucro: d.projecaoLucro, markupPadrao: d.markupPadrao,
          priorizar: d.priorizar,
          controlarLotesVencimento: d.controlarLotesVencimento ?? false,
          informarPrescritorVenda: d.informarPrescritorVenda ?? false,
          imprimirEtiqueta: d.imprimirEtiqueta ?? false,
          permitirDescontoPrazo: d.permitirDescontoPrazo ?? false,
          permitirPromocao: d.permitirPromocao ?? false,
          permitirDescontosProgressivos: d.permitirDescontosProgressivos ?? false,
          ativo: d.ativo, criadoEm: d.criadoEm
        };
        this.registroForm.set(form);
        this.formOriginal = { ...form };
        this.erro.set('');
        this.isDirty.set(false);
        this.modoEdicao.set(true);
        this.modo.set('form');
      },
      error: () => this.carregando.set(false)
    });
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    if (!this.validar()) return;
    this.erro.set('');
    const f = this.registroForm();
    this.salvando.set(true);

    const url = this.apiUrlAtual();
    const headers = this.headerLiberacao();
    const req = this.modoEdicao()
      ? this.http.put<any>(`${url}/${f.id}`, f, { headers })
      : this.http.post<any>(url, f, { headers });

    req.subscribe({
      next: (r) => {
        this.salvando.set(false);
        this.carregar();
        if (!this.modoEdicao() && r.data) {
          this.registroSelecionado.set(r.data);
          this.registroForm.set({ ...r.data });
          this.formOriginal = { ...r.data };
          this.modoEdicao.set(true);
        } else {
          this.formOriginal = { ...f };
        }
        this.isDirty.set(false);
      },
      error: (e) => {
        this.salvando.set(false);
        this.modal.erro('Erro', e?.error?.message ?? 'Erro ao salvar registro.');
      }
    });
  }

  cancelar() {
    if (this.formOriginal) this.registroForm.set({ ...this.formOriginal });
    this.isDirty.set(false);
    this.erro.set('');
  }

  fecharForm() {
    this.modo.set('lista');
  }

  async excluir() {
    const r = this.registroSelecionado();
    if (!r?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusão',
      `Deseja excluir "${r.nome}"? O registro será removido permanentemente. Se estiver em uso, será apenas desativado.`,
      'Sim, excluir',
      'Não, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    this.excluindo.set(true);
    const url = this.apiUrlAtual();
    const headers = this.headerLiberacao();
    this.http.delete<any>(`${url}/${r.id}`, { headers }).subscribe({
      next: async (resp) => {
        this.excluindo.set(false);
        this.registroSelecionado.set(null);
        this.modo.set('lista');
        this.carregar();

        const tipo = resp?.resultado ?? 'excluido';
        if (tipo === 'excluido') {
          await this.modal.sucesso('Excluído', 'Registro excluído com sucesso.');
        } else {
          await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.');
        }
      },
      error: (e) => {
        this.excluindo.set(false);
        this.modal.erro('Erro', e?.error?.message ?? 'Erro ao excluir registro.');
      }
    });
  }

  // ── Log ───────────────────────────────────────────────────────────

  abrirLog() {
    const r = this.registroSelecionado();
    if (!r?.id) return;
    this.logDataInicio.set(this.hoje(-30));
    this.logDataFim.set(this.hoje(0));
    this.modalLog.set(true);
    this.filtrarLog();
  }

  filtrarLog() {
    const r = this.registroSelecionado();
    if (!r?.id) return;
    this.carregandoLog.set(true);
    this.logSelecionado.set(null);
    const url = this.apiUrlAtual();
    const params = `dataInicio=${this.logDataInicio()}&dataFim=${this.logDataFim()}`;
    this.http.get<any>(`${url}/${r.id}/log?${params}`).subscribe({
      next: resp => {
        const lista: LogEntry[] = resp.data ?? [];
        this.logRegistros.set(lista);
        this.logSelecionado.set(lista.length > 0 ? lista[0] : null);
        this.carregandoLog.set(false);
      },
      error: () => { this.carregandoLog.set(false); }
    });
  }

  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }
  fecharLog() { this.modalLog.set(false); }

  acaoCss(acao: string): string {
    if (acao === 'CRIAÇÃO')     return 'badge-criacao';
    if (acao === 'ALTERAÇÃO')   return 'badge-alteracao';
    if (acao === 'EXCLUSÃO')    return 'badge-exclusao';
    if (acao === 'DESATIVAÇÃO') return 'badge-desativacao';
    return '';
  }

  // ── Formulário ────────────────────────────────────────────────────

  updateForm(campo: keyof Classificacao, valor: any) {
    this.registroForm.update(f => ({ ...f, [campo]: valor }));
    this.isDirty.set(true);
  }

  private validar(): boolean {
    const f = this.registroForm();
    if (!f.nome?.trim()) {
      this.modal.erro('Validação', 'O campo Nome é obrigatório.');
      return false;
    }
    this.erro.set('');
    return true;
  }

  private novoRegistro(): Classificacao {
    return {
      nome: '', comissaoPercentual: 0, descontoMinimo: 0, descontoMaximo: 0,
      descontoMaximoComSenha: 0, projecaoLucro: 30, markupPadrao: 50,
      priorizar: '', controlarLotesVencimento: false, informarPrescritorVenda: false,
      imprimirEtiqueta: false, permitirDescontoPrazo: false, permitirPromocao: false,
      permitirDescontosProgressivos: false, ativo: true
    };
  }

  // ── Produto CRUD ───────────────────────────────────────────────────

  ngOnDestroy() {
    clearTimeout(this.buscaProdutoTimer);
    this.salvarEstado();
  }

  private readonly STORAGE_KEY_ESTADO = 'zulex_produtos_estado';

  private salvarEstado() {
    const estado = {
      abaAtiva: this.abaAtiva(),
      modo: this.modo(),
      modoEdicao: this.modoEdicao(),
      produtoEditandoId: this.produtoEditandoId(),
      produtoForm: this.produtoForm(),
      produtoFormOriginal: this.produtoFormOriginal,
      isDirty: this.isDirty(),
      filialSelecionada: this.filialSelecionada(),
      // Classificação
      registroForm: this.registroForm(),
      registroSelecionado: this.registroSelecionado(),
      formOriginal: this.formOriginal,
    };
    sessionStorage.setItem(this.STORAGE_KEY_ESTADO, JSON.stringify(estado));
  }

  private restaurarEstado() {
    const json = sessionStorage.getItem(this.STORAGE_KEY_ESTADO);
    if (!json) return false;
    sessionStorage.removeItem(this.STORAGE_KEY_ESTADO);
    try {
      const e = JSON.parse(json);
      if (e.abaAtiva) this.abaAtiva.set(e.abaAtiva);
      if (e.modo) this.modo.set(e.modo);
      if (e.modoEdicao !== undefined) this.modoEdicao.set(e.modoEdicao);
      if (e.produtoEditandoId) this.produtoEditandoId.set(e.produtoEditandoId);
      if (e.produtoForm) this.produtoForm.set(e.produtoForm);
      if (e.produtoFormOriginal) this.produtoFormOriginal = e.produtoFormOriginal;
      if (e.isDirty !== undefined) this.isDirty.set(e.isDirty);
      if (e.filialSelecionada) this.filialSelecionada.set(e.filialSelecionada);
      if (e.registroForm) this.registroForm.set(e.registroForm);
      if (e.registroSelecionado) this.registroSelecionado.set(e.registroSelecionado);
      if (e.formOriginal) this.formOriginal = e.formOriginal;
      return true;
    } catch { return false; }
  }

  onProdutoBuscaInput(valor: string) {
    this.produtoBusca.set(valor);
    clearTimeout(this.buscaProdutoTimer);
    if (valor.trim().length >= 3) {
      this.buscaProdutoTimer = setTimeout(() => this.pesquisarProdutos(), 400);
    } else {
      this.produtoRegistros.set([]);
      this.produtoSelecionado.set(null);
    }
  }

  pesquisarProdutos() {
    const termo = this.produtoBusca().trim();
    if (termo.length < 3) { this.produtoRegistros.set([]); return; }
    this.carregando.set(true);
    this.produtoSelecionado.set(null);
    this.http.get<any>(this.produtoApiUrl, { params: { busca: termo } }).subscribe({
      next: r => { this.carregando.set(false); this.produtoRegistros.set(r?.data ?? []); },
      error: () => { this.carregando.set(false); this.produtoRegistros.set([]); }
    });
  }

  selecionarProduto(r: ProdutoList) { this.produtoSelecionado.set(r); }

  async incluirProduto() {
    if (!await this.verificarPermissao('i')) return;
    const novo = this.novoProdutoForm();

    // Buscar filiais para pre-popular Dados, Fiscal e Fornecedores
    try {
      const resp = await this.http.get<any>(`${environment.apiUrl}/filiais`).toPromise();
      const filiais: any[] = resp?.data ?? [];
      novo.dados = filiais.map((f: any) => this.novoDadosFilial(f.id));
      novo.fiscais = filiais.map((f: any) => this.novoFiscalFilial(f.id));
    } catch { /* silenciar — backend criará ao salvar */ }

    this.produtoForm.set(novo);
    this.produtoFormOriginal = JSON.stringify(novo);
    this.erro.set(''); this.isDirty.set(false); this.modoEdicao.set(false);
    this.produtoEditandoId.set(null); this.modo.set('form');
  }

  private novoDadosFilial(filialId: number): ProdutoDadosItem {
    return {
      filialId, estoqueAtual: 0, estoqueMinimo: 0, estoqueMaximo: 0, demanda: 0, estoqueDeposito: 0,
      ultimaCompraUnitario: 0, ultimaCompraSt: 0, ultimaCompraOutros: 0, ultimaCompraIpi: 0,
      ultimaCompraFpc: 0, ultimaCompraBoleto: 0, ultimaCompraDifal: 0, ultimaCompraFrete: 0,
      custoMedio: 0, projecaoLucro: 0, markup: 0, valorVenda: 0, pmc: 0,
      valorPromocao: 0, valorPromocaoPrazo: 0, descontoMinimo: 0, descontoMaxSemSenha: 0, descontoMaxComSenha: 0,
      comissao: 0, valorIncentivo: 0,
      bloquearDesconto: false, bloquearPromocao: false, naoAtualizarAbcfarma: false,
      naoAtualizarGestorTributario: false, bloquearCompras: false, produtoFormula: false,
      bloquearComissao: false, bloquearCoberturaOferta: false, usoContinuo: false, avisoFracao: false
    };
  }

  private novoFiscalFilial(filialId: number): ProdutoFiscalItem {
    return { filialId, aliquotaIcms: 0, aliquotaPis: 0, aliquotaCofins: 0, aliquotaIpi: 0 };
  }

  async editarProduto() {
    const s = this.produtoSelecionado();
    if (!s) return;
    if (!await this.verificarPermissao('a')) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.produtoApiUrl}/${s.id}`).subscribe({
      next: r => {
        this.carregando.set(false);
        const d = r.data;
        const f: ProdutoForm = {
          nome: d.nome, codigoBarras: d.codigoBarras, qtdeEmbalagem: d.qtdeEmbalagem,
          precoFp: d.precoFp, lista: d.lista, fracao: d.fracao, ativo: d.ativo, eliminado: d.eliminado,
          fabricanteId: d.fabricanteId, grupoPrincipalId: d.grupoPrincipalId,
          grupoProdutoId: d.grupoProdutoId, subGrupoId: d.subGrupoId, ncmId: d.ncmId,
          fabricanteNome: d.fabricanteNome, grupoPrincipalNome: d.grupoPrincipalNome,
          grupoProdutoNome: d.grupoProdutoNome, subGrupoNome: d.subGrupoNome, ncmCodigo: d.ncmCodigo,
          criadoEm: d.criadoEm,
          barras: d.barras ?? [], registrosMs: d.registrosMs ?? [],
          substancias: d.substancias ?? [], fornecedores: d.fornecedores ?? [],
          fiscais: d.fiscais ?? [],
          dados: (d.dados ?? []).map((dd: any) => ({
            ...dd,
            promocaoInicio: this.toDateInput(dd.promocaoInicio),
            promocaoFim: this.toDateInput(dd.promocaoFim)
          }))
        };
        this.produtoForm.set(f);
        this.produtoFormOriginal = JSON.stringify(f);
        this.erro.set(''); this.isDirty.set(false); this.modoEdicao.set(true);
        this.produtoEditandoId.set(s.id); this.modo.set('form');
      },
      error: () => this.carregando.set(false)
    });
  }

  private readonly CAMPOS_PRECO = ['valorVenda', 'valorPromocao', 'markup', 'projecaoLucro',
    'descontoMinimo', 'descontoMaxSemSenha', 'descontoMaxComSenha', 'comissao', 'valorIncentivo'];

  async salvarProduto() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const f = { ...this.produtoForm() } as any;
    if (!f.nome?.trim()) { this.modal.erro('Validação', 'Nome é obrigatório.'); return; }

    // Verificar propagação de preço (só em edição)
    if (this.modoEdicao() && f.dados?.length > 0) {
      const original = JSON.parse(this.produtoFormOriginal);
      const precoMudou = this.verificarPrecoMudou(original, f);
      if (precoMudou) {
        f.filiaisPrecoAplicar = await this.perguntarPropagacaoPreco();
      }
    }

    this.erro.set(''); this.salvando.set(true);
    const headers = this.headerLiberacao();

    const req = this.modoEdicao()
      ? this.http.put<any>(`${this.produtoApiUrl}/${this.produtoEditandoId()}`, f, { headers })
      : this.http.post<any>(this.produtoApiUrl, f, { headers });

    req.subscribe({
      next: (r: any) => {
        this.salvando.set(false); this.isDirty.set(false);
        if (!this.modoEdicao() && r?.data) {
          this.produtoSelecionado.set(r.data);
          this.produtoEditandoId.set(r.data.id);
          this.modoEdicao.set(true);
        }
        this.produtoFormOriginal = JSON.stringify(this.produtoForm());
      },
      error: (err: any) => { this.salvando.set(false); this.modal.erro('Erro ao Salvar', err?.error?.message || 'Erro ao salvar produto.'); }
    });
  }

  private verificarPrecoMudou(original: any, atual: any): boolean {
    if (!original.dados?.length || !atual.dados?.length) return false;
    const origDados = original.dados[0] || {};
    const atualDados = atual.dados[0] || {};
    return this.CAMPOS_PRECO.some(c => (origDados[c] ?? 0) !== (atualDados[c] ?? 0));
  }

  private async perguntarPropagacaoPreco(): Promise<number[] | null> {
    // Buscar regra configurada
    try {
      const resp = await this.http.get<any>(`${environment.apiUrl}/configuracoes`).toPromise();
      const configs: any[] = resp?.data ?? [];
      const regra = configs.find((c: any) => c.chave === 'produto.preco.regra')?.valor ?? 'atual';

      if (regra === 'atual') return null;
      if (regra === 'todas') {
        // Buscar todas as filiais ativas
        const fResp = await this.http.get<any>(`${environment.apiUrl}/filiais`).toPromise();
        return (fResp?.data ?? []).map((f: any) => f.id);
      }
      if (regra === 'perguntar') {
        const result = await this.modal.confirmar(
          'Propagação de Preço',
          'O preço foi alterado. Deseja aplicar a alteração de preço para todas as outras filiais?',
          'Sim, todas as filiais', 'Não, apenas esta'
        );
        if (result.confirmado) {
          const fResp = await this.http.get<any>(`${environment.apiUrl}/filiais`).toPromise();
          return (fResp?.data ?? []).map((f: any) => f.id);
        }
        return null;
      }
    } catch { /* silenciar erro na verificação */ }
    return null;
  }

  async excluirProduto() {
    const id = this.produtoEditandoId();
    if (!id) return;
    const result = await this.modal.confirmar('Confirmar Exclusão', 'Deseja excluir este produto? Ele será marcado como eliminado.');
    if (!result.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    this.excluindo.set(true);
    this.http.delete<any>(`${this.produtoApiUrl}/${id}`, { headers: this.headerLiberacao() }).subscribe({
      next: () => { this.excluindo.set(false); this.modo.set('lista'); this.pesquisarProdutos(); },
      error: (e: any) => { this.excluindo.set(false); this.modal.erro('Erro', e?.error?.message || 'Erro ao excluir.'); }
    });
  }

  cancelarProduto() {
    this.produtoForm.set(JSON.parse(this.produtoFormOriginal));
    this.isDirty.set(false); this.erro.set('');
  }

  updateProdutoForm(campo: string, valor: any) {
    this.produtoForm.update(f => ({ ...f, [campo]: valor }));
    this.isDirty.set(true);
  }

  // Barras
  adicionarBarras(input: HTMLInputElement) {
    const v = input.value.trim();
    if (!v) return;
    if (this.produtoForm().barras.some(b => b.barras === v)) return;
    this.produtoForm.update(f => ({ ...f, barras: [...f.barras, { barras: v }] }));
    this.isDirty.set(true);
    input.value = '';
    input.focus();
  }

  // MS
  adicionarMs(input: HTMLInputElement) {
    const v = input.value.trim();
    if (!v) return;
    if (this.produtoForm().registrosMs.some(m => m.numeroMs === v)) return;
    this.produtoForm.update(f => ({ ...f, registrosMs: [...f.registrosMs, { numeroMs: v }] }));
    this.isDirty.set(true);
    input.value = '';
    input.focus();
  }

  // Substâncias
  onSubstanciaInput(valor: string) {
    this.substanciaBusca.set(valor);
    clearTimeout(this.lookupTimers['substancia']);
    if (valor.trim().length < 1) { this.lookupSubstancia.set([]); this.lookupAberto.set(null); return; }
    this.lookupTimers['substancia'] = setTimeout(() => {
      const cache = this.lookupCaches['substancia'];
      if (cache && Date.now() - cache.ts < 30000) {
        this.filtrarSubstanciaLocal(valor, cache.data);
        return;
      }
      this.http.get<any>(`${environment.apiUrl}/substancias`).subscribe({
        next: resp => {
          const lista = resp?.data ?? [];
          this.lookupCaches['substancia'] = { data: lista, ts: Date.now() };
          this.filtrarSubstanciaLocal(valor, lista);
        },
        error: () => this.lookupSubstancia.set([])
      });
    }, 250);
  }

  private filtrarSubstanciaLocal(valor: string, lista: any[]) {
    const termo = valor.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const jaVinculados = new Set(this.produtoForm().substancias.map(s => s.substanciaId));
    const filtrada = lista
      .filter((r: any) => r.ativo !== false && !jaVinculados.has(r.id)
        && r.nome.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').includes(termo))
      .slice(0, 20)
      .map((r: any) => ({ id: r.id, texto: r.nome }));
    this.lookupSubstancia.set(filtrada);
    this.lookupAberto.set(filtrada.length > 0 ? 'substancia' : null);
  }

  selecionarSubstancia(item: LookupItem) {
    if (this.produtoForm().substancias.some(s => s.substanciaId === item.id)) return;
    this.produtoForm.update(f => ({ ...f, substancias: [...f.substancias, { substanciaId: item.id, substanciaNome: item.texto }] }));
    this.isDirty.set(true);
    this.substanciaBusca.set('');
    this.lookupSubstancia.set([]);
    this.lookupAberto.set(null);
  }

  // Fornecedores
  addFornecedor() {
    this.produtoForm.update(f => ({ ...f, fornecedores: [...f.fornecedores, { filialId: 0, fornecedorId: 0, fracao: 1 }] }));
    this.isDirty.set(true);
  }

  onFornecedorInput(idx: number, valor: string) {
    this.fornecedorBuscaIdx.set(idx);
    this.updateFornecedor(idx, 'fornecedorNome', valor);
    this.updateFornecedor(idx, 'fornecedorId', 0);
    clearTimeout(this.lookupTimers['fornecedor']);
    if (valor.trim().length < 1) { this.lookupFornecedor.set([]); this.lookupAberto.set(null); return; }
    this.lookupTimers['fornecedor'] = setTimeout(() => {
      const cache = this.lookupCaches['fornecedor'];
      if (cache && Date.now() - cache.ts < 30000) {
        this.filtrarFornecedorLocal(valor, cache.data);
        return;
      }
      this.http.get<any>(`${environment.apiUrl}/fornecedores`).subscribe({
        next: resp => {
          const lista = resp?.data ?? [];
          this.lookupCaches['fornecedor'] = { data: lista, ts: Date.now() };
          this.filtrarFornecedorLocal(valor, lista);
        },
        error: () => this.lookupFornecedor.set([])
      });
    }, 250);
  }

  private filtrarFornecedorLocal(valor: string, lista: any[]) {
    const termo = valor.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const filtrada = lista
      .filter((r: any) => r.ativo !== false
        && r.nome.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').includes(termo))
      .slice(0, 20)
      .map((r: any) => ({ id: r.id, texto: r.nome }));
    this.lookupFornecedor.set(filtrada);
    this.lookupAberto.set(filtrada.length > 0 ? 'fornecedor' : null);
  }

  selecionarFornecedor(idx: number, item: LookupItem) {
    this.updateFornecedor(idx, 'fornecedorId', item.id);
    this.updateFornecedor(idx, 'fornecedorNome', item.texto);
    this.lookupFornecedor.set([]);
    this.lookupAberto.set(null);
    this.fornecedorBuscaIdx.set(null);
  }

  // Remoção com confirmação (substância, barras, ms, fornecedor)
  async confirmarRemoverItem(tipo: string, i: number, descricao: string) {
    const nomes: Record<string, string> = { substancia: 'substância', barras: 'código de barras', ms: 'registro MS', fornecedor: 'fornecedor' };
    const resultado = await this.modal.confirmar(
      'Confirmar Remoção',
      `Deseja remover ${nomes[tipo] || 'item'} "${descricao}"?`,
      'Sim, remover', 'Não, manter'
    );
    if (!resultado.confirmado) return;
    switch (tipo) {
      case 'substancia': this.produtoForm.update(f => ({ ...f, substancias: f.substancias.filter((_, idx) => idx !== i) })); break;
      case 'barras':     this.produtoForm.update(f => ({ ...f, barras: f.barras.filter((_, idx) => idx !== i) })); break;
      case 'ms':         this.produtoForm.update(f => ({ ...f, registrosMs: f.registrosMs.filter((_, idx) => idx !== i) })); break;
      case 'fornecedor': this.produtoForm.update(f => ({ ...f, fornecedores: f.fornecedores.filter((_, idx) => idx !== i) })); break;
    }
    this.isDirty.set(true);
  }
  updateFornecedor(i: number, campo: string, valor: any) {
    this.produtoForm.update(f => ({ ...f, fornecedores: f.fornecedores.map((r, idx) => idx === i ? { ...r, [campo]: valor } : r) }));
    this.isDirty.set(true);
  }

  // Dados (por filial)
  updateDados(i: number, campo: string, valor: any) {
    this.produtoForm.update(f => ({ ...f, dados: f.dados.map((r, idx) => idx === i ? { ...r, [campo]: valor } : r) }));
    this.isDirty.set(true);
  }

  // Fiscal
  updateFiscal(idx: number, campo: string, valor: any) {
    this.produtoForm.update(f => ({
      ...f,
      fiscais: f.fiscais.map((fiscal, i) => i === idx ? { ...fiscal, [campo]: valor } : fiscal)
    }));
    this.isDirty.set(true);
  }

  // ── Lookups (autocomplete) ───────────────────────────────────────────

  private readonly LOOKUP_CONFIG: Record<string, { url: string; signalKey: string; idField: string; nomeField: string; mapTexto: (r: any) => string }> = {
    fabricante:      { url: '/fabricantes',        signalKey: 'lookupFabricante',      idField: 'fabricanteId',      nomeField: 'fabricanteNome',      mapTexto: r => r.nome },
    grupoPrincipal:  { url: '/grupos-principais',  signalKey: 'lookupGrupoPrincipal',  idField: 'grupoPrincipalId',  nomeField: 'grupoPrincipalNome',  mapTexto: r => r.nome },
    grupo:           { url: '/grupos-produtos',    signalKey: 'lookupGrupo',           idField: 'grupoProdutoId',    nomeField: 'grupoProdutoNome',    mapTexto: r => r.nome },
    subGrupo:        { url: '/sub-grupos',         signalKey: 'lookupSubGrupo',        idField: 'subGrupoId',       nomeField: 'subGrupoNome',       mapTexto: r => r.nome },
    ncm:             { url: '/ncm',                signalKey: 'lookupNcm',             idField: 'ncmId',            nomeField: 'ncmCodigo',          mapTexto: r => `${r.codigoNcm} - ${r.descricao}` },
  };

  private getLookupSignal(key: string) {
    const map: Record<string, any> = {
      lookupFabricante: this.lookupFabricante,
      lookupGrupoPrincipal: this.lookupGrupoPrincipal,
      lookupGrupo: this.lookupGrupo,
      lookupSubGrupo: this.lookupSubGrupo,
      lookupNcm: this.lookupNcm,
    };
    return map[key];
  }

  onLookupInput(tipo: string, valor: string) {
    const cfg = this.LOOKUP_CONFIG[tipo];
    if (!cfg) return;
    // Atualiza o texto exibido e limpa o ID selecionado enquanto digita
    this.produtoForm.update(f => ({ ...f, [cfg.nomeField]: valor, [cfg.idField]: null }));
    clearTimeout(this.lookupTimers[tipo]);

    const minChars = tipo === 'ncm' ? 4 : 1;
    if (valor.trim().length < minChars) {
      this.getLookupSignal(cfg.signalKey).set([]);
      this.lookupAberto.set(null);
      return;
    }

    this.lookupTimers[tipo] = setTimeout(() => {
      const cache = this.lookupCaches[tipo];
      if (cache && Date.now() - cache.ts < 30000) {
        this.filtrarLookupLocal(tipo, valor, cache.data);
        return;
      }

      const url = tipo === 'ncm'
        ? `${environment.apiUrl}${cfg.url}?busca=${encodeURIComponent(valor)}`
        : `${environment.apiUrl}${cfg.url}`;

      this.http.get<any>(url).subscribe({
        next: resp => {
          const lista = resp?.data ?? [];
          if (tipo !== 'ncm') {
            this.lookupCaches[tipo] = { data: lista, ts: Date.now() };
          }
          this.filtrarLookupLocal(tipo, valor, lista);
        },
        error: () => this.getLookupSignal(cfg.signalKey).set([])
      });
    }, 250);
  }

  private filtrarLookupLocal(tipo: string, valor: string, lista: any[]) {
    const cfg = this.LOOKUP_CONFIG[tipo];
    const termo = valor.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const filtrada = lista
      .filter((r: any) => {
        if (r.ativo === false) return false;
        const txt = cfg.mapTexto(r).toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        return txt.includes(termo);
      })
      .slice(0, 20)
      .map((r: any) => ({ id: r.id, texto: cfg.mapTexto(r) }));
    this.getLookupSignal(cfg.signalKey).set(filtrada);
    this.lookupAberto.set(filtrada.length > 0 ? tipo : null);
  }

  onLookupFocus(tipo: string, valor: string) {
    const cfg = this.LOOKUP_CONFIG[tipo];
    if (!cfg) return;
    // Se já tem ID selecionado, mostra lista com cache se possível
    if ((this.produtoForm() as any)[cfg.idField]) return;
    // Se tem texto sem seleção, dispara busca
    if (valor.trim().length >= (tipo === 'ncm' ? 4 : 1)) {
      this.onLookupInput(tipo, valor);
    }
  }

  selecionarLookup(tipo: string, item: LookupItem) {
    const cfg = this.LOOKUP_CONFIG[tipo];
    const displayValue = tipo === 'ncm' ? item.texto.split(' - ')[0] : item.texto;
    this.produtoForm.update(f => ({ ...f, [cfg.idField]: item.id, [cfg.nomeField]: displayValue }));
    this.isDirty.set(true);
    this.getLookupSignal(cfg.signalKey).set([]);
    this.lookupAberto.set(null);
  }

  limparLookup(tipo: string) {
    const cfg = this.LOOKUP_CONFIG[tipo];
    this.produtoForm.update(f => ({ ...f, [cfg.idField]: null, [cfg.nomeField]: '' }));
    this.isDirty.set(true);
    this.getLookupSignal(cfg.signalKey).set([]);
    this.lookupAberto.set(null);
  }

  getLookupDisplay(tipo: string): string {
    const cfg = this.LOOKUP_CONFIG[tipo];
    return (this.produtoForm() as any)[cfg.nomeField] ?? '';
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: Event) {
    if (!(e.target as HTMLElement).closest('.lookup-wrap')) {
      this.lookupAberto.set(null);
    }
  }

  // ── Lookups para Dados por filial (Local, Família, Seção) ────────

  onDadosLookupInput(dadosIdx: number, tipo: string, valor: string) {
    clearTimeout(this.lookupTimers[tipo]);
    if (tipo === 'local') this.produtoForm.update(f => ({ ...f, dados: f.dados.map((d, i) => i === dadosIdx ? { ...d, produtoLocalNome: valor, produtoLocalId: undefined } : d) }));
    if (valor.trim().length < 1) { this.getLookupSignalDados(tipo).set([]); this.lookupAberto.set(null); return; }
    this.lookupTimers[tipo] = setTimeout(() => {
      const url = tipo === 'local' ? '/produto-locais' : tipo === 'familia' ? '/produto-familias' : '/secoes';
      const cache = this.lookupCaches[tipo];
      if (cache && Date.now() - cache.ts < 30000) {
        this.filtrarDadosLookupLocal(tipo, valor, cache.data);
        return;
      }
      this.http.get<any>(`${environment.apiUrl}${url}`).subscribe({
        next: resp => {
          const lista = resp?.data ?? [];
          this.lookupCaches[tipo] = { data: lista, ts: Date.now() };
          this.filtrarDadosLookupLocal(tipo, valor, lista);
        },
        error: () => this.getLookupSignalDados(tipo).set([])
      });
    }, 250);
  }

  private filtrarDadosLookupLocal(tipo: string, valor: string, lista: any[]) {
    const termo = valor.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const filtrada = lista
      .filter((r: any) => r.ativo !== false && r.nome.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').includes(termo))
      .slice(0, 20)
      .map((r: any) => ({ id: r.id, texto: r.nome }));
    this.getLookupSignalDados(tipo).set(filtrada);
    this.lookupAberto.set(filtrada.length > 0 ? tipo : null);
  }

  selecionarDadosLookup(dadosIdx: number, tipo: string, item: LookupItem) {
    const campo = tipo === 'local' ? 'produtoLocalId' : tipo === 'familia' ? 'produtoFamiliaId' : 'secaoId';
    this.updateDados(dadosIdx, campo, item.id);
    if (tipo === 'local') this.produtoForm.update(f => ({ ...f, dados: f.dados.map((d, i) => i === dadosIdx ? { ...d, produtoLocalNome: item.texto } : d) }));
    this.getLookupSignalDados(tipo).set([]);
    this.lookupAberto.set(null);
  }

  private getLookupSignalDados(tipo: string) {
    return tipo === 'local' ? this.lookupLocal : tipo === 'secao' ? this.lookupSecao : this.lookupFamilia;
  }

  /** Converte ISO datetime "2026-03-31T00:00:00" para "2026-03-31" para input[type=date] */
  private toDateInput(val: string | null | undefined): string | undefined {
    if (!val) return undefined;
    return val.substring(0, 10);
  }

  isProdutoAba(): boolean { return this.abaAtiva() === 'produtos'; }

  private novoProdutoForm(): ProdutoForm {
    return {
      nome: '', qtdeEmbalagem: 1, lista: 'Indefinida', fracao: 1, ativo: true, eliminado: false,
      barras: [], registrosMs: [], substancias: [], fornecedores: [], fiscais: [], dados: []
    };
  }

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
