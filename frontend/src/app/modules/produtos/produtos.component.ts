import { Component, signal, computed, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

type AbaAtiva = 'produtos' | 'grupo-principal' | 'grupo' | 'sub-grupo' | 'secao' | 'familia';

const ABAS_IMPLEMENTADAS: AbaAtiva[] = ['grupo-principal', 'grupo', 'sub-grupo', 'secao'];

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

const CLASSIFICACAO_COLUNAS: ColunaDef[] = [
  { campo: 'id',                label: 'ID',           largura: 60,  minLargura: 50,  padrao: true },
  { campo: 'nome',              label: 'Nome',         largura: 250, minLargura: 150, padrao: true },
  { campo: 'comissaoPercentual', label: '% Comissão',  largura: 100, minLargura: 70,  padrao: true },
  { campo: 'markupPadrao',      label: '% Markup',     largura: 100, minLargura: 70,  padrao: true },
  { campo: 'projecaoLucro',     label: '% Proj Lucro', largura: 100, minLargura: 70,  padrao: true },
  { campo: 'ativo',             label: 'Ativo',        largura: 60,  minLargura: 50,  padrao: true },
];

const API_MAP: Record<string, string> = {
  'grupo-principal': '/api/grupos-principais',
  'grupo':           '/api/grupos-produtos',
  'sub-grupo':       '/api/sub-grupos',
  'secao':           '/api/secoes',
};

const NOME_ABA_MAP: Record<string, string> = {
  'grupo-principal': 'Grupo Principal',
  'grupo':           'Grupo',
  'sub-grupo':       'Sub Grupo',
  'secao':           'Seção',
};

@Component({
  selector: 'app-produtos',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './produtos.component.html',
  styleUrl: './produtos.component.scss'
})
export class ProdutosComponent implements OnInit {

  // ── Abas ──────────────────────────────────────────────────────────
  abaAtiva = signal<AbaAtiva>('grupo-principal');

  abas: AbaConfig[] = [
    { id: 'produtos',        label: 'Produtos',       cor: '#4a90d9' },
    { id: 'grupo-principal', label: 'Grupo Principal', cor: '#e8845f' },
    { id: 'grupo',           label: 'Grupo',           cor: '#f0c75e' },
    { id: 'sub-grupo',       label: 'Sub Grupo',       cor: '#7bc67e' },
    { id: 'secao',           label: 'Seção',           cor: '#5bb8c9' },
    { id: 'familia',         label: 'Família',         cor: '#b088c9' },
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

  // ── Token Liberação ───────────────────────────────────────────────
  private tokenLiberacao: string | null = null;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.carregar();
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
    if (ABAS_IMPLEMENTADAS.includes(id)) {
      this.carregar();
    }
  }

  isAbaImplementada(): boolean {
    return ABAS_IMPLEMENTADAS.includes(this.abaAtiva());
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
      if (termo.length < 3) return true;
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
    this.registroForm.set({ ...r });
    this.formOriginal = { ...r };
    this.erro.set('');
    this.isDirty.set(false);
    this.modoEdicao.set(true);
    this.modo.set('form');
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
        this.erro.set(e?.error?.message ?? 'Erro ao salvar registro.');
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
      this.erro.set('O campo Nome é obrigatório.');
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

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
