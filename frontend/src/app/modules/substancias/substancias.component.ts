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

interface Substancia {
  id?: number;
  nome: string;
  dcb: string;
  cas: string;
  controleEspecialSngpc: boolean;
  classeTerapeutica: string | null;
  ativo: boolean;
  criadoEm?: string;
}

interface AbaEdicao {
  substancia: Substancia;
  form: Substancia;
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

const SUBSTANCIAS_COLUNAS: ColunaDef[] = [
  { campo: 'nome',                 label: 'Nome',              largura: 220, minLargura: 120, padrao: true },
  { campo: 'dcb',                  label: 'DCB',               largura: 180, minLargura: 100, padrao: true },
  { campo: 'cas',                  label: 'CAS',               largura: 120, minLargura: 80,  padrao: true },
  { campo: 'controleEspecialSngpc',label: 'Ctrl. Especial',    largura: 110, minLargura: 80,  padrao: true },
  { campo: 'classeTerapeutica',    label: 'Classe Terapêutica',largura: 150, minLargura: 100, padrao: true },
  { campo: 'ativo',                label: 'Ativo',             largura: 60,  minLargura: 50,  padrao: true },
];

@Component({
  selector: 'app-substancias',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './substancias.component.html',
  styleUrl: './substancias.component.scss'
})
export class SubstanciasComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY          = 'zulex_substancias_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_substancias';

  modo                  = signal<Modo>('lista');
  substancias           = signal<Substancia[]>([]);
  substanciaSelecionada = signal<Substancia | null>(null);
  substanciaForm        = signal<Substancia>(this.novaSubstancia());
  carregando            = signal(false);
  salvando              = signal(false);
  excluindo             = signal(false);
  busca                 = signal('');
  filtroStatus          = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna            = signal<string>('nome');
  sortDirecao           = signal<'asc' | 'desc'>('asc');
  abasEdicao            = signal<AbaEdicao[]>([]);
  abaAtivaId            = signal<number | null>(null);
  modoEdicao            = signal(false);
  isDirty               = signal(false);
  erro                  = signal('');
  errosCampos           = signal<Record<string, string>>({});
  private formOriginal: Substancia | null = null;

  // Colunas
  colunas        = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas  = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;

  // Log
  modalLog       = signal(false);
  logRegistros   = signal<LogEntry[]>([]);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio  = signal('');
  logDataFim     = signal('');
  carregandoLog  = signal(false);

  readonly classesTerapeuticas = ['Antimicrobianos', 'Psicotrópicos'];

  private apiUrl = `${environment.apiUrl}/substancias`;
  private tokenLiberacao: string | null = null;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('substancias', acao)) return true;
    const resultado = await this.modal.permissao('substancias', acao);
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

  ngOnInit()    { this.carregar(); }
  ngOnDestroy() { this.persistirEstado(); }

  sairDaTela() {
    this.persistirEstado();
    this.tabService.fecharTabAtiva();
  }

  // ── State persistence ────────────────────────────────────────────
  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abasIds: abas.map(a => a.substancia.id),
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
          const s = this.substancias().find(x => x.id === id);
          if (s) { this.substanciaSelecionada.set(s); this.editar(); }
        }
        if (state.abaAtivaId) {
          setTimeout(() => {
            const temAba = this.abasEdicao().find(a => a.substancia.id === state.abaAtivaId);
            if (temAba) this.ativarAba(state.abaAtivaId);
          }, 100);
        }
      }
    } catch {}
  }

  // ── Data ─────────────────────────────────────────────────────────
  private primeiroCarregamento = true;

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.substancias.set(r.data ?? []);
        this.carregando.set(false);
        if (this.primeiroCarregamento) {
          this.primeiroCarregamento = false;
          this.restaurarEstado();
        }
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('substancias', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  substanciasFiltradas = computed(() => {
    const termo   = this.normalizar(this.busca());
    const status  = this.filtroStatus();
    const col     = this.sortColuna();
    const dir     = this.sortDirecao();

    const lista = this.substancias().filter(s => {
      if (status === 'ativos'   && !s.ativo) return false;
      if (status === 'inativos' &&  s.ativo) return false;
      if (termo.length < 2) return true;
      return (
        this.normalizar(s.nome).includes(termo) ||
        this.normalizar(s.dcb).includes(termo)  ||
        this.normalizar(s.cas).includes(termo)
      );
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

  getCellValue(s: Substancia, campo: string): string {
    const v = (s as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Não';
    return v ?? '';
  }

  selecionar(s: Substancia) { this.substanciaSelecionada.set(s); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  // ── Columns ──────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return SUBSTANCIAS_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return SUBSTANCIAS_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(SUBSTANCIAS_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
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
    const def = SUBSTANCIAS_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── CRUD ─────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.substanciaForm.set(this.novaSubstancia());
    this.formOriginal = this.clonar(this.novaSubstancia());
    this.modoEdicao.set(false);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const s = this.substanciaSelecionada();
    if (!s?.id) return;

    const jaAberta = this.abasEdicao().find(a => a.substancia.id === s.id);
    if (jaAberta) { this.ativarAba(s.id); return; }

    const aba: AbaEdicao = { substancia: { ...s }, form: this.clonar(s), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    this.abaAtivaId.set(s.id!);
    this.substanciaForm.set(this.clonar(s));
    this.formOriginal = this.clonar(s);
    this.modoEdicao.set(true);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    const aba = this.abasEdicao().find(a => a.substancia.id === id);
    if (!aba) return;
    this.abaAtivaId.set(id);
    this.substanciaForm.set(this.clonar(aba.form));
    this.formOriginal = this.clonar(aba.form);
    this.isDirty.set(aba.isDirty);
    this.modoEdicao.set(true);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  fecharAba(id: number) {
    this.abasEdicao.update(abas => abas.filter(a => a.substancia.id !== id));
    if (this.abaAtivaId() === id) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) {
        this.ativarAba(restantes[restantes.length - 1].substancia.id!);
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
      this.substanciaForm.set(this.clonar(this.formOriginal));
      this.isDirty.set(false);
      this.atualizarDirtyAba();
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (!id || this.modo() !== 'form') return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.substancia.id === id
        ? { ...a, form: this.clonar(this.substanciaForm()), isDirty: this.isDirty() }
        : a
      )
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const erros: Record<string, string> = {};
    const f = this.substanciaForm();
    if (!f.nome.trim()) erros['nome'] = 'Nome é obrigatório.';
    if (!f.dcb.trim())  erros['dcb']  = 'DCB é obrigatório.';
    if (!f.cas.trim())  erros['cas']  = 'CAS é obrigatório.';
    if (Object.keys(erros).length) { this.errosCampos.set(erros); return; }
    this.errosCampos.set({});
    this.salvando.set(true);

    const headers = this.headerLiberacao();
    const body: any = {
      nome: f.nome,
      dcb: f.dcb,
      cas: f.cas,
      controleEspecialSngpc: f.controleEspecialSngpc,
      classeTerapeutica: f.classeTerapeutica || null,
      ativo: f.ativo
    };

    const salvar$ = this.modoEdicao()
      ? this.http.put(`${this.apiUrl}/${f.id}`, body, { headers })
      : this.http.post<any>(this.apiUrl, body, { headers });

    salvar$.subscribe({
      next: (r: any) => {
        const substanciaId = this.modoEdicao() ? f.id! : r.data?.id;
        this.finalizarSalvar(substanciaId);
      },
      error: () => {
        this.erro.set('Erro ao salvar substância.');
        this.salvando.set(false);
      }
    });
  }

  private finalizarSalvar(substanciaId: number) {
    this.salvando.set(false);
    this.isDirty.set(false);
    if (this.modoEdicao()) this.fecharAba(substanciaId);
    this.carregar();
    this.modo.set('lista');
  }

  async excluir() {
    const s = this.substanciaSelecionada();
    if (!s?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusão',
      `Deseja excluir a substância ${s.nome}? O registro será removido permanentemente. Se estiver em uso, será apenas desativado.`,
      'Sim, excluir',
      'Não, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${s.id}`, { headers }).subscribe({
      next: async (r: any) => {
        this.excluindo.set(false);
        this.substanciaSelecionada.set(null);
        this.fecharAba(s.id!);
        this.carregar();
        if (r.resultado === 'desativado') {
          await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.');
        }
      },
      error: () => {
        this.excluindo.set(false);
        this.modal.erro('Erro', 'Erro ao excluir substância.');
      }
    });
  }

  // ── Form helpers ─────────────────────────────────────────────────
  upd(campo: keyof Substancia, v: any) {
    this.substanciaForm.update(f => ({ ...f, [campo]: v }));
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
      abas.map(a => a.substancia.id === id ? { ...a, isDirty: true } : a)
    );
  }

  // ── Log ──────────────────────────────────────────────────────────
  abrirLog() {
    const s = this.substanciaSelecionada();
    if (!s?.id) return;
    this.modalLog.set(true);
    this.logRegistros.set([]);
    this.logSelecionado.set(null);
    this.filtrarLog();
  }

  fecharLog() { this.modalLog.set(false); }

  filtrarLog() {
    const s = this.substanciaSelecionada();
    if (!s?.id) return;
    this.carregandoLog.set(true);
    let url = `${this.apiUrl}/${s.id}/log`;
    const params: string[] = [];
    if (this.logDataInicio()) params.push(`dataInicio=${this.logDataInicio()}`);
    if (this.logDataFim())    params.push(`dataFim=${this.logDataFim()}`);
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
      'CRIAÇÃO':    'log-badge badge-criacao',
      'ALTERAÇÃO':  'log-badge badge-alteracao',
      'EXCLUSÃO':   'log-badge badge-exclusao',
      'DESATIVAÇÃO':'log-badge badge-desativacao'
    };
    return map[acao] ?? 'log-badge';
  }

  // ── Utils ────────────────────────────────────────────────────────
  private novaSubstancia(): Substancia {
    return { nome: '', dcb: '', cas: '', controleEspecialSngpc: false, classeTerapeutica: null, ativo: true };
  }

  private clonar<T>(obj: T): T {
    return JSON.parse(JSON.stringify(obj));
  }
}
