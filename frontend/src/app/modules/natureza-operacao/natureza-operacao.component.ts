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

interface NaturezaOperacaoRegra {
  id?: number;
  cenarioTributario: number;
  cfopInterno?: string;
  cfopInterestadual?: string;
  cstIcmsInterno?: string;
  cstIcmsInterestadual?: string;
  codigoBeneficioInterno?: string;
  codigoBeneficioInterestadual?: string;
}

interface NaturezaOperacao {
  id?: number;
  codigo?: string;
  descricao: string;
  tipoNf: number;
  finalidadeNfe: number;
  identificadorDestino: number;
  relacionarDocumentoFiscal: boolean;
  utilizarPrecoCusto: boolean;
  reajustarCustoMedio: boolean;
  geraFinanceiro: boolean;
  movimentaEstoque: boolean;
  tipoMovimentoEstoque?: number;
  cstPisPadrao?: string;
  cstCofinsPadrao?: string;
  cstIpiPadrao?: string;
  enquadramentoIpiPadrao?: string;
  indicadorPresenca: number;
  indicadorFinalidade: number;
  observacao?: string;
  ativo: boolean;
  criadoEm?: string;
  regras: NaturezaOperacaoRegra[];
}

interface AbaEdicao {
  natOp: NaturezaOperacao;
  form: NaturezaOperacao;
  isDirty: boolean;
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

type Modo = 'lista' | 'form';

const COLUNAS: ColunaDef[] = [
  { campo: 'codigo', label: 'Codigo', largura: 80, minLargura: 50, padrao: true },
  { campo: 'descricao', label: 'Descricao', largura: 300, minLargura: 120, padrao: true },
  { campo: 'tipoNf', label: 'Tipo', largura: 80, minLargura: 60, padrao: true },
  { campo: 'finalidadeNfe', label: 'Finalidade', largura: 110, minLargura: 80, padrao: true },
  { campo: 'movimentaEstoque', label: 'Mov. Estoque', largura: 100, minLargura: 70, padrao: true },
  { campo: 'ativo', label: 'Ativo', largura: 60, minLargura: 50, padrao: true },
];

const CENARIOS = [
  { id: 1, label: 'Tributados para PF ou PJ sem IE' },
  { id: 2, label: 'Tributados para PJ com IE' },
  { id: 3, label: 'Produtos com Substituicao Tributaria' },
  { id: 4, label: 'Produtos isentos de ICMS' },
  { id: 5, label: 'Produtos nao tributados pelo ICMS' },
  { id: 6, label: 'Operacoes com outros tipos de tributacao' },
  { id: 7, label: 'Documento fiscal referenciado em NFe' },
];

@Component({
  selector: 'app-natureza-operacao',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './natureza-operacao.component.html',
  styleUrl: './natureza-operacao.component.scss'
})
export class NaturezaOperacaoComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_natureza_operacao_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_natureza_operacao_v2';

  readonly CENARIOS = CENARIOS;

  modo = signal<Modo>('lista');
  registros = signal<NaturezaOperacao[]>([]);
  selecionado = signal<NaturezaOperacao | null>(null);
  form = signal<NaturezaOperacao>(this.novoRegistro());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('descricao');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  abasEdicao = signal<AbaEdicao[]>([]);
  abaAtivaId = signal<number | null>(null);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});
  private formOriginal: NaturezaOperacao | null = null;

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

  private apiUrl = `${environment.apiUrl}/natureza-operacao`;
  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('natureza-operacao', acao)) return true;
    const resultado = await this.modal.permissao('natureza-operacao', acao);
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

  private readonly TAB_ID = '/erp/natureza-operacao';
  private fechamentoConfirmado = false;

  ngOnInit() {
    if (!this.tabService.tabs().find(t => t.id === this.TAB_ID)) {
      this.tabService.abrirTab({ id: this.TAB_ID, titulo: 'Natureza de Operacao', rota: this.TAB_ID, iconKey: 'fiscal' });
    }
    this.carregar();
    window.addEventListener('beforeunload', this.onBeforeUnload);
    this.tabService.registrarBeforeClose(this.TAB_ID, async () => {
      if (this.isDirty()) {
        const r = await this.modal.confirmar('Fechar tela', 'Voce tem alteracoes nao salvas. Deseja realmente fechar?', 'Sim, fechar', 'Nao, continuar editando');
        if (!r.confirmado) return false;
      }
      this.fechamentoConfirmado = true;
      this.abasEdicao.set([]);
      sessionStorage.removeItem(this.STATE_KEY);
      return true;
    });
  }

  ngOnDestroy() {
    window.removeEventListener('beforeunload', this.onBeforeUnload);
    this.tabService.removerBeforeClose(this.TAB_ID);
    if (!this.fechamentoConfirmado) this.persistirEstado();
  }

  private onBeforeUnload = (e: BeforeUnloadEvent) => {
    this.persistirEstado();
    if (this.isDirty()) e.preventDefault();
  };

  @HostListener('document:keydown', ['$event'])
  onKeydown(e: KeyboardEvent) {
    if (this.modal.visivel()) return;
    if (e.ctrlKey && e.key === 's' && this.modo() === 'form') { e.preventDefault(); if (this.isDirty()) this.salvar(); }
    if (e.key === 'Escape') { if (this.modo() === 'form') { (e as any).__handled = true; if (this.isDirty()) this.cancelarEdicao(); else this.fecharAba(this.abaAtivaId()!); } else if (this.abasEdicao().length > 0) { (e as any).__handled = true; const u = this.abasEdicao()[this.abasEdicao().length - 1]; this.fecharAba(u.natOp.id!); } }
    if (e.key === 'F2' && this.modo() === 'lista') { e.preventDefault(); this.editar(); }
    if (e.key === 'Enter' && this.modo() === 'lista' && this.selecionado()) {
      const el = e.target as HTMLElement;
      if (el?.tagName === 'INPUT' || el?.tagName === 'SELECT' || el?.tagName === 'TEXTAREA') return;
      e.preventDefault(); this.editar();
    }
    if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && this.modo() === 'lista') {
      const el = e.target as HTMLElement;
      if (el?.classList?.contains('input-busca')) return;
      e.preventDefault();
      const lista = this.registrosFiltrados();
      if (lista.length === 0) return;
      const atual = this.selecionado();
      const idx = atual ? lista.findIndex(f => f.id === atual.id) : -1;
      const novoIdx = e.key === 'ArrowDown' ? Math.min(idx + 1, lista.length - 1) : Math.max(idx - 1, 0);
      this.selecionar(lista[novoIdx]);
      setTimeout(() => { const row = document.querySelector('.erp-grid tbody tr.selecionado') as HTMLElement; if (row) row.scrollIntoView({ block: 'nearest' }); });
    }
  }

  campoAlterado(campo: string): boolean {
    if (!this.formOriginal || !this.modoEdicao()) return false;
    const atual = (this.form() as any)[campo];
    const original = (this.formOriginal as any)[campo];
    return (atual ?? '') !== (original ?? '');
  }

  // ── Regras helpers ─────────────────────────────────────────────────
  novaRegra(cenario: number): NaturezaOperacaoRegra {
    return {
      cenarioTributario: cenario,
      cfopInterno: '',
      cfopInterestadual: '',
      cstIcmsInterno: '',
      cstIcmsInterestadual: '',
      codigoBeneficioInterno: '',
      codigoBeneficioInterestadual: '',
    };
  }

  getRegra(cenario: number): NaturezaOperacaoRegra {
    const regras = this.form().regras;
    let regra = regras.find(r => r.cenarioTributario === cenario);
    if (!regra) {
      regra = this.novaRegra(cenario);
      this.form.update(f => ({ ...f, regras: [...f.regras, regra!] }));
    }
    return regra;
  }

  updateRegra(cenario: number, campo: string, valor: string) {
    this.form.update(f => {
      const regras = f.regras.map(r =>
        r.cenarioTributario === cenario ? { ...r, [campo]: valor.toUpperCase() } : r
      );
      return { ...f, regras };
    });
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  private inicializarRegras(regrasExistentes: NaturezaOperacaoRegra[]): NaturezaOperacaoRegra[] {
    return CENARIOS.map(c => {
      const existente = regrasExistentes.find(r => r.cenarioTributario === c.id);
      return existente ? { ...existente } : this.novaRegra(c.id);
    });
  }

  // ── Sair ───────────────────────────────────────────────────────────
  async sairDaTela() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Sair da tela', 'Voce tem alteracoes nao salvas. Deseja realmente sair?', 'Sim, sair', 'Nao, continuar editando');
      if (!r.confirmado) return;
    }
    this.fechamentoConfirmado = true;
    this.abasEdicao.set([]);
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  // ── State persistence ─────────────────────────────────────────────
  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abas: abas.map(a => ({ natOp: a.natOp, form: a.form, isDirty: a.isDirty })),
      abaAtivaId: this.abaAtivaId()
    }));
  }

  private restaurarEstado() {
    try {
      const json = sessionStorage.getItem(this.STATE_KEY);
      if (!json) return;
      const state = JSON.parse(json);
      sessionStorage.removeItem(this.STATE_KEY);

      if (state.abas?.length > 0) {
        for (const a of state.abas) {
          if (this.abasEdicao().find(x => x.natOp.id === a.natOp.id)) continue;
          const novaAba: AbaEdicao = { natOp: a.natOp, form: this.clonar(a.form), isDirty: a.isDirty };
          this.abasEdicao.update(tabs => [...tabs, novaAba]);
          if (a.natOp.id === state.abaAtivaId) {
            this.selecionado.set(a.natOp);
            this.form.set(this.clonar(a.form));
            this.formOriginal = this.clonar(a.form);
            this.isDirty.set(a.isDirty);
            this.abaAtivaId.set(a.natOp.id);
            this.modoEdicao.set(a.natOp.id !== this.NOVO_ID);
            this.modo.set('form');
          }
        }
        return;
      }

      if (state.abasIds?.length > 0) {
        for (const id of state.abasIds) {
          const f = this.registros().find(x => x.id === id);
          if (f) this.restaurarAba(f, id === state.abaAtivaId);
        }
      }
    } catch {}
  }

  private restaurarAba(f: NaturezaOperacao, ativar: boolean) {
    if (this.abasEdicao().find(a => a.natOp.id === f.id)) return;
    const aba: AbaEdicao = { natOp: { ...f }, form: this.clonar(f), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    if (ativar) this.ativarAba(f.id!);
  }

  // ── Data ───────────────────────────────────────────────────────────
  private primeiroCarregamento = true;

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.registros.set(r.data ?? []);
        this.carregando.set(false);
        if (this.primeiroCarregamento) {
          this.primeiroCarregamento = false;
          this.restaurarEstado();
        }
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('natureza-operacao', 'c').then(r => {
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

    const lista = this.registros().filter(f => {
      if (status === 'ativos'   && !f.ativo) return false;
      if (status === 'inativos' &&  f.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(f.descricao).includes(termo)
        || this.normalizar(f.codigo ?? '').includes(termo);
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
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(f: NaturezaOperacao, campo: string): string {
    const v = (f as any)[campo];
    if (campo === 'tipoNf') return v === 0 ? 'Entrada' : v === 1 ? 'Saida' : '';
    if (campo === 'finalidadeNfe') {
      const map: Record<number, string> = { 1: 'Normal', 2: 'Complementar', 3: 'Ajuste', 4: 'Devolucao' };
      return map[v] ?? '';
    }
    if (campo === 'movimentaEstoque' || campo === 'ativo') return v ? 'Sim' : 'Nao';
    return v ?? '';
  }

  selecionar(f: NaturezaOperacao) { this.selecionado.set(f); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '\u25B2' : '\u25BC') : '\u21C5'; }

  // ── Columns ────────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
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
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasStorage();
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

  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => { const arr = [...cols]; const [moved] = arr.splice(this.dragColIdx!, 1); arr.splice(idx, 0, moved); this.dragColIdx = idx; return arr; });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunasStorage(); }

  private readonly NOVO_ID = -1;
  dataHoje = new Date().toLocaleDateString('pt-BR');

  // ── CRUD ───────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.salvarEstadoAbaAtiva();
    const jaExiste = this.abasEdicao().find(a => a.natOp.id === this.NOVO_ID);
    if (jaExiste) {
      if (jaExiste.isDirty) { this.ativarAba(this.NOVO_ID); this.modoEdicao.set(false); return; }
      else { this.abasEdicao.update(tabs => tabs.filter(t => t.natOp.id !== this.NOVO_ID)); }
    }
    const novo = this.novoRegistro();
    (novo as any).id = this.NOVO_ID;
    this.form.set(novo);
    this.formOriginal = this.clonar(novo);
    this.modoEdicao.set(false);
    this.isDirty.set(false);
    this.erro.set(''); this.errosCampos.set({});
    this.abaAtivaId.set(this.NOVO_ID);
    const novaAba: AbaEdicao = {
      natOp: { id: this.NOVO_ID, descricao: 'Novo cadastro', tipoNf: 1, finalidadeNfe: 1, identificadorDestino: 1, relacionarDocumentoFiscal: false, utilizarPrecoCusto: false, reajustarCustoMedio: false, geraFinanceiro: false, movimentaEstoque: false, indicadorPresenca: 0, indicadorFinalidade: 0, ativo: true, regras: [] },
      form: this.clonar(novo),
      isDirty: false
    };
    this.abasEdicao.update(tabs => [...tabs, novaAba]);
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const f = this.selecionado();
    if (!f?.id) return;

    const jaAberta = this.abasEdicao().find(a => a.natOp.id === f.id);
    if (jaAberta) {
      this.ativarAba(f.id);
      return;
    }

    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${f.id}`).subscribe({
      next: r => {
        this.carregando.set(false);
        const d = r.data;
        const detalhe: NaturezaOperacao = {
          ...f,
          ...d,
          regras: this.inicializarRegras(d.regras ?? [])
        };
        const aba: AbaEdicao = { natOp: { ...f, regras: [] }, form: this.clonar(detalhe), isDirty: false };
        this.abasEdicao.update(abas => [...abas, aba]);
        this.abaAtivaId.set(f.id!);
        this.form.set(this.clonar(detalhe));
        this.formOriginal = this.clonar(detalhe);
        this.modoEdicao.set(true);
        this.isDirty.set(false);
        this.erro.set('');
        this.errosCampos.set({});
        this.modo.set('form');
      },
      error: () => this.carregando.set(false)
    });
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    const aba = this.abasEdicao().find(a => a.natOp.id === id);
    if (!aba) return;
    this.abaAtivaId.set(id);
    this.form.set(this.clonar(aba.form));
    this.formOriginal = this.clonar(aba.form);
    this.isDirty.set(aba.isDirty);
    this.modoEdicao.set(id !== this.NOVO_ID);
    this.erro.set(''); this.errosCampos.set({});
    this.modo.set('form');
  }

  async fecharAba(id: number) {
    const aba = this.abasEdicao().find(a => a.natOp.id === id);
    if (aba?.isDirty) {
      const r = await this.modal.confirmar('Fechar aba', 'Voce tem alteracoes nao salvas. Deseja realmente fechar?', 'Sim, fechar', 'Nao, continuar editando');
      if (!r.confirmado) return;
    }
    this.abasEdicao.update(abas => abas.filter(a => a.natOp.id !== id));
    if (this.abaAtivaId() === id) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) {
        this.ativarAba(restantes[restantes.length - 1].natOp.id!);
      } else {
        this.modo.set('lista');
        this.abaAtivaId.set(null);
      }
    }
  }

  fechar() {
    this.salvarEstadoAbaAtiva();
    this.modo.set('lista');
    this.carregar();
  }

  async fecharForm() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Fechar cadastro', 'Voce tem alteracoes nao salvas. Deseja realmente fechar?', 'Sim, fechar', 'Nao, continuar editando');
      if (!r.confirmado) return;
    }
    const id = this.abaAtivaId();
    if (id != null) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.natOp.id !== id));
      this.abaAtivaId.set(null);
    } else {
      this.modo.set('lista');
    }
  }

  async cancelarEdicao() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Cancelar edicao', 'Voce tem alteracoes nao salvas. Deseja realmente cancelar?', 'Sim, cancelar', 'Nao, continuar editando');
      if (!r.confirmado) return;
    }
    const id = this.abaAtivaId();
    if (id === this.NOVO_ID) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.natOp.id !== this.NOVO_ID));
      this.abaAtivaId.set(null); this.modo.set('lista'); return;
    }
    if (this.formOriginal) {
      this.form.set(this.clonar(this.formOriginal));
      this.isDirty.set(false);
      if (id) {
        this.abasEdicao.update(abas =>
          abas.map(a => a.natOp.id === id ? { ...a, isDirty: false } : a)
        );
      }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (!id || this.modo() !== 'form') return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.natOp.id === id
        ? { ...a, form: this.clonar(this.form()), isDirty: this.isDirty() }
        : a
      )
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const erros: Record<string, string> = {};
    const f = this.form();
    if (!f.descricao.trim()) erros['descricao'] = 'Descricao e obrigatoria.';
    if (Object.keys(erros).length) {
      this.errosCampos.set(erros);
      return;
    }
    this.errosCampos.set({});
    this.salvando.set(true);

    const headers = this.headerLiberacao();
    const body: any = {
      descricao: f.descricao,
      tipoNf: f.tipoNf,
      finalidadeNfe: f.finalidadeNfe,
      identificadorDestino: f.identificadorDestino,
      relacionarDocumentoFiscal: f.relacionarDocumentoFiscal,
      utilizarPrecoCusto: f.utilizarPrecoCusto,
      reajustarCustoMedio: f.reajustarCustoMedio,
      geraFinanceiro: f.geraFinanceiro,
      movimentaEstoque: f.movimentaEstoque,
      tipoMovimentoEstoque: f.movimentaEstoque ? f.tipoMovimentoEstoque : null,
      cstPisPadrao: f.cstPisPadrao,
      cstCofinsPadrao: f.cstCofinsPadrao,
      cstIpiPadrao: f.cstIpiPadrao,
      enquadramentoIpiPadrao: f.enquadramentoIpiPadrao,
      indicadorPresenca: f.indicadorPresenca,
      indicadorFinalidade: f.indicadorFinalidade,
      observacao: f.observacao,
      ativo: f.ativo,
      regras: f.regras.filter(r =>
        r.cfopInterno || r.cfopInterestadual || r.cstIcmsInterno || r.cstIcmsInterestadual || r.codigoBeneficioInterno || r.codigoBeneficioInterestadual
      )
    };

    const salvarDados$ = this.modoEdicao()
      ? this.http.put(`${this.apiUrl}/${f.id}`, body, { headers })
      : this.http.post<any>(this.apiUrl, body, { headers });

    salvarDados$.subscribe({
      next: (r: any) => {
        const regId = this.modoEdicao() ? f.id! : r.data?.id;
        this.finalizarSalvar(regId);
      },
      error: () => {
        this.erro.set('Erro ao salvar natureza de operacao.');
        this.salvando.set(false);
      }
    });
  }

  private finalizarSalvar(regId: number) {
    this.salvando.set(false);
    this.isDirty.set(false);
    if (this.modoEdicao()) {
      this.fecharAba(regId);
    }
    this.carregar();
    this.modo.set('lista');
  }

  async excluir() {
    const f = this.selecionado();
    if (!f?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusao',
      `Deseja excluir a natureza de operacao "${f.descricao}"? O registro sera removido permanentemente. Se estiver em uso, sera apenas desativado.`,
      'Sim, excluir',
      'Nao, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${f.id}`, { headers }).subscribe({
      next: async (r: any) => {
        this.excluindo.set(false);
        this.selecionado.set(null);
        this.fecharAba(f.id!);
        this.carregar();
        if (r.resultado === 'desativado') {
          await this.modal.aviso('Desativado', 'O registro esta em uso e foi apenas desativado.');
        }
      },
      error: () => {
        this.excluindo.set(false);
        this.modal.erro('Erro', 'Erro ao excluir natureza de operacao.');
      }
    });
  }

  // ── Form helpers ───────────────────────────────────────────────────
  upd(campo: keyof NaturezaOperacao, v: any) {
    if (typeof v === 'string' && campo !== 'criadoEm') v = v.toUpperCase();
    this.form.update(f => ({ ...f, [campo]: v }));
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  erroCampo(campo: string): string {
    return this.errosCampos()[campo] ?? '';
  }

  private atualizarDirtyAba() {
    const id = this.abaAtivaId();
    if (!id) return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.natOp.id === id ? { ...a, isDirty: true } : a)
    );
  }

  // ── Log ────────────────────────────────────────────────────────────
  abrirLog() {
    const f = this.selecionado();
    if (!f?.id) return;
    this.modalLog.set(true);
    this.logRegistros.set([]);
    this.logSelecionado.set(null);
    this.filtrarLog();
  }

  fecharLog() { this.modalLog.set(false); }

  filtrarLog() {
    const f = this.selecionado();
    if (!f?.id) return;
    this.carregandoLog.set(true);
    let url = `${this.apiUrl}/${f.id}/log`;
    const params: string[] = [];
    if (this.logDataInicio()) params.push(`dataInicio=${this.logDataInicio()}`);
    if (this.logDataFim()) params.push(`dataFim=${this.logDataFim()}`);
    if (params.length) url += '?' + params.join('&');

    this.http.get<any>(url).subscribe({
      next: r => {
        this.logRegistros.set(r.data ?? []);
        this.carregandoLog.set(false);
        if (r.data?.length > 0) this.selecionarLogEntry(r.data[0]);
      },
      error: () => this.carregandoLog.set(false)
    });
  }

  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }

  acaoCss(acao: string): string {
    const map: Record<string, string> = {
      'CRIACAO': 'log-badge badge-criacao',
      'ALTERACAO': 'log-badge badge-alteracao',
      'EXCLUSAO': 'log-badge badge-exclusao',
      'DESATIVACAO': 'log-badge badge-desativacao'
    };
    return map[acao] ?? 'log-badge';
  }

  // ── Utils ──────────────────────────────────────────────────────────
  private novoRegistro(): NaturezaOperacao {
    return {
      descricao: '',
      tipoNf: 1,
      finalidadeNfe: 1,
      identificadorDestino: 1,
      relacionarDocumentoFiscal: false,
      utilizarPrecoCusto: false,
      reajustarCustoMedio: false,
      geraFinanceiro: false,
      movimentaEstoque: false,
      indicadorPresenca: 0,
      indicadorFinalidade: 0,
      ativo: true,
      regras: this.inicializarRegras([])
    };
  }

  private clonar<T>(obj: T): T {
    return JSON.parse(JSON.stringify(obj));
  }
}
