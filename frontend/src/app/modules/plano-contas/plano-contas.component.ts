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

interface PlanoConta {
  id?: number;
  descricao: string;
  nivel: number;
  nivelDescricao?: string;
  natureza: number;
  naturezaDescricao?: string;
  contaPaiId: number | null;
  contaPaiDescricao?: string;
  ordem: number;
  codigoHierarquico?: string;
  visivelRelatorio: boolean;
  ativo: boolean;
  criadoEm?: string;
}

interface AbaEdicao {
  planoConta: PlanoConta;
  form: PlanoConta;
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

const PLANO_CONTAS_COLUNAS: ColunaDef[] = [
  { campo: 'codigoHierarquico', label: 'Código', largura: 100, minLargura: 60, padrao: true },
  { campo: 'descricao', label: 'Descrição', largura: 280, minLargura: 150, padrao: true },
  { campo: 'nivelDescricao', label: 'Nível', largura: 120, minLargura: 80, padrao: true },
  { campo: 'naturezaDescricao', label: 'Natureza', largura: 100, minLargura: 70, padrao: true },
  { campo: 'visivelRelatorio', label: 'Visível', largura: 70, minLargura: 50, padrao: true },
  { campo: 'ativo', label: 'Ativo', largura: 60, minLargura: 50, padrao: true },
];

@Component({
  selector: 'app-plano-contas',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './plano-contas.component.html',
  styleUrl: './plano-contas.component.scss'
})
export class PlanoContasComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_planocontas_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_planocontas';

  modo = signal<Modo>('lista');
  registros = signal<PlanoConta[]>([]);
  selecionado = signal<PlanoConta | null>(null);
  form = signal<PlanoConta>(this.novoRegistro());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  filtroNivel = signal<'' | '1' | '2' | '3'>('');
  sortColuna = signal<string>('codigoHierarquico');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  abasEdicao = signal<AbaEdicao[]>([]);
  abaAtivaId = signal<number | null>(null);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});
  private formOriginal: PlanoConta | null = null;

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

  private apiUrl = `${environment.apiUrl}/planoscontas`;
  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('plano-contas', acao)) return true;
    const resultado = await this.modal.permissao('plano-contas', acao);
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

  ngOnInit() { this.carregar(); }
  ngOnDestroy() { sessionStorage.removeItem(this.STATE_KEY); }

  sairDaTela() {
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  // ── State persistence ─────────────────────────────────────────────
  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abasIds: abas.map(a => a.planoConta.id),
      abaAtivaId: this.abaAtivaId()
    }));
  }

  private restaurarEstado() {
    try {
      const json = sessionStorage.getItem(this.STATE_KEY);
      if (!json) return;
      const state = JSON.parse(json);
      sessionStorage.removeItem(this.STATE_KEY);
      if (state.abasIds?.length > 0) {
        for (const id of state.abasIds) {
          const r = this.registros().find(x => x.id === id);
          if (r) this.restaurarAba(r, id === state.abaAtivaId);
        }
      }
    } catch {}
  }

  private restaurarAba(r: PlanoConta, ativar: boolean) {
    if (this.abasEdicao().find(a => a.planoConta.id === r.id)) return;
    const aba: AbaEdicao = { planoConta: { ...r }, form: this.clonar(r), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    if (ativar) this.ativarAba(r.id!);
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
          this.modal.permissao('plano-contas', 'c').then(r => {
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
    const nivel = this.filtroNivel();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.registros().filter(r => {
      if (status === 'ativos' && !r.ativo) return false;
      if (status === 'inativos' && r.ativo) return false;
      if (nivel && r.nivel !== Number(nivel)) return false;
      if (termo.length < 2) return true;
      return this.normalizar(r.descricao).includes(termo) ||
             (r.codigoHierarquico ?? '').includes(termo);
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

  // ── Conta pai (filtrada por nível) ────────────────────────────────
  contasPaiDisponiveis = computed(() => {
    const nivel = this.form().nivel;
    if (nivel === 1) return []; // Grupo não tem pai
    const nivelPai = nivel === 2 ? 1 : 2; // SubGrupo → Grupo, PlanoConta → SubGrupo
    return this.registros()
      .filter(r => r.nivel === nivelPai && r.ativo)
      .sort((a, b) => (a.codigoHierarquico ?? '').localeCompare(b.codigoHierarquico ?? ''));
  });

  private normalizar(s: string): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(r: PlanoConta, campo: string): string {
    const v = (r as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Não';
    return v ?? '';
  }

  getIndentClass(r: PlanoConta): string {
    if (r.nivel === 2) return 'indent-1';
    if (r.nivel === 3) return 'indent-2';
    return '';
  }

  selecionar(r: PlanoConta) { this.selecionado.set(r); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }

  // ── Columns ────────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return PLANO_CONTAS_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return PLANO_CONTAS_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(PLANO_CONTAS_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
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
    const def = PLANO_CONTAS_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── CRUD ───────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.form.set(this.novoRegistro());
    this.formOriginal = this.clonar(this.novoRegistro());
    this.modoEdicao.set(false);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const r = this.selecionado();
    if (!r?.id) return;

    const jaAberta = this.abasEdicao().find(a => a.planoConta.id === r.id);
    if (jaAberta) {
      this.ativarAba(r.id);
      return;
    }

    const aba: AbaEdicao = { planoConta: { ...r }, form: this.clonar(r), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    this.abaAtivaId.set(r.id!);
    this.form.set(this.clonar(r));
    this.formOriginal = this.clonar(r);
    this.modoEdicao.set(true);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    const aba = this.abasEdicao().find(a => a.planoConta.id === id);
    if (!aba) return;
    this.abaAtivaId.set(id);
    this.form.set(this.clonar(aba.form));
    this.formOriginal = this.clonar(aba.form);
    this.isDirty.set(aba.isDirty);
    this.modoEdicao.set(true);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  fecharAba(id: number) {
    this.abasEdicao.update(abas => abas.filter(a => a.planoConta.id !== id));
    if (this.abaAtivaId() === id) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) {
        this.ativarAba(restantes[restantes.length - 1].planoConta.id!);
      } else {
        this.modo.set('lista');
        this.abaAtivaId.set(null);
      }
    }
  }

  fechar() {
    this.modo.set('lista');
    this.carregar();
  }

  fecharForm() {
    if (this.modoEdicao()) {
      const id = this.abaAtivaId();
      if (id) this.fecharAba(id);
      else this.modo.set('lista');
    } else {
      this.modo.set('lista');
    }
  }

  cancelarEdicao() {
    if (this.formOriginal) {
      this.form.set(this.clonar(this.formOriginal));
      this.isDirty.set(false);
      const id = this.abaAtivaId();
      if (id) {
        this.abasEdicao.update(abas =>
          abas.map(a => a.planoConta.id === id ? { ...a, isDirty: false } : a)
        );
      }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (!id || this.modo() !== 'form') return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.planoConta.id === id
        ? { ...a, form: this.clonar(this.form()), isDirty: this.isDirty() }
        : a
      )
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const erros: Record<string, string> = {};
    const f = this.form();
    if (!f.descricao.trim()) erros['descricao'] = 'Descrição é obrigatória.';
    if (f.ordem < 1) erros['ordem'] = 'Ordem deve ser maior que zero.';
    if (f.nivel !== 1 && !f.contaPaiId) erros['contaPaiId'] = 'Conta pai é obrigatória para SubGrupo e Plano de Contas.';
    if (Object.keys(erros).length) {
      this.errosCampos.set(erros);
      return;
    }
    this.errosCampos.set({});
    this.salvando.set(true);

    const headers = this.headerLiberacao();
    const body: any = {
      descricao: f.descricao,
      nivel: f.nivel,
      natureza: f.natureza,
      contaPaiId: f.nivel === 1 ? null : f.contaPaiId,
      ordem: f.ordem,
      visivelRelatorio: f.visivelRelatorio,
      ativo: f.ativo
    };

    const salvarDados$ = this.modoEdicao()
      ? this.http.put(`${this.apiUrl}/${f.id}`, body, { headers })
      : this.http.post<any>(this.apiUrl, body, { headers });

    salvarDados$.subscribe({
      next: (r: any) => {
        const registroId = this.modoEdicao() ? f.id! : r.data?.id;
        this.finalizarSalvar(registroId);
      },
      error: (err) => {
        this.erro.set(err.error?.message || 'Erro ao salvar plano de contas.');
        this.salvando.set(false);
      }
    });
  }

  private finalizarSalvar(registroId: number) {
    this.salvando.set(false);
    this.isDirty.set(false);
    if (this.modoEdicao()) {
      this.fecharAba(registroId);
    }
    this.carregar();
    this.modo.set('lista');
  }

  async excluir() {
    const r = this.selecionado();
    if (!r?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusão',
      `Deseja excluir "${r.descricao}"? O registro será removido permanentemente. Se estiver em uso, será apenas desativado.`,
      'Sim, excluir',
      'Não, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${r.id}`, { headers }).subscribe({
      next: async (res: any) => {
        this.excluindo.set(false);
        this.selecionado.set(null);
        this.fecharAba(r.id!);
        this.carregar();
        if (res.resultado === 'desativado') {
          await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.');
        }
      },
      error: (err) => {
        this.excluindo.set(false);
        this.modal.erro('Erro', err.error?.message || 'Erro ao excluir plano de contas.');
      }
    });
  }

  // ── Form helpers ───────────────────────────────────────────────────
  upd(campo: keyof PlanoConta, v: any) {
    this.form.update(f => ({ ...f, [campo]: v }));
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  updNivel(v: number) {
    this.form.update(f => ({ ...f, nivel: v, contaPaiId: null }));
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
      abas.map(a => a.planoConta.id === id ? { ...a, isDirty: true } : a)
    );
  }

  // ── Log ────────────────────────────────────────────────────────────
  abrirLog() {
    const r = this.selecionado();
    if (!r?.id) return;
    this.modalLog.set(true);
    this.logRegistros.set([]);
    this.logSelecionado.set(null);
    this.filtrarLog();
  }

  fecharLog() { this.modalLog.set(false); }

  filtrarLog() {
    const r = this.selecionado();
    if (!r?.id) return;
    this.carregandoLog.set(true);
    let url = `${this.apiUrl}/${r.id}/log`;
    const params: string[] = [];
    if (this.logDataInicio()) params.push(`dataInicio=${this.logDataInicio()}`);
    if (this.logDataFim()) params.push(`dataFim=${this.logDataFim()}`);
    if (params.length) url += '?' + params.join('&');

    this.http.get<any>(url).subscribe({
      next: res => {
        this.logRegistros.set(res.data ?? []);
        this.carregandoLog.set(false);
        if (res.data?.length > 0) this.selecionarLogEntry(res.data[0]);
      },
      error: () => this.carregandoLog.set(false)
    });
  }

  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }

  acaoCss(acao: string): string {
    const map: Record<string, string> = {
      'CRIAÇÃO': 'log-badge badge-criacao',
      'ALTERAÇÃO': 'log-badge badge-alteracao',
      'EXCLUSÃO': 'log-badge badge-exclusao',
      'DESATIVAÇÃO': 'log-badge badge-desativacao'
    };
    return map[acao] ?? 'log-badge';
  }

  // ── Utils ──────────────────────────────────────────────────────────
  private novoRegistro(): PlanoConta {
    return { descricao: '', nivel: 1, natureza: 1, contaPaiId: null, ordem: 1, visivelRelatorio: true, ativo: true };
  }

  private clonar<T>(obj: T): T {
    return JSON.parse(JSON.stringify(obj));
  }
}
