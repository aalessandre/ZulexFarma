import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';
import { BANCOS_BRASIL, Banco } from './bancos-brasil';

interface LogCampo { campo: string; valorAnterior?: string; valorAtual?: string; }
interface LogEntry { id: number; realizadoEm: string; acao: string; nomeUsuario: string; campos: LogCampo[]; }

interface PlanoContaResumo { id: number; descricao: string; codigoHierarquico?: string; }
interface FilialResumo { id: number; nomeFilial: string; }

interface ContaBancaria {
  id?: number;
  descricao: string;
  tipoConta: number;
  tipoContaDescricao?: string;
  banco: string | null;
  agencia: string | null;
  agenciaDigito: string | null;
  numeroConta: string | null;
  contaDigito: string | null;
  chavePix: string | null;
  saldoInicial: number;
  dataSaldoInicial: string | null;
  planoContaId: number | null;
  planoContaDescricao?: string;
  filialId: number | null;
  filialNome?: string;
  observacao: string | null;
  ativo: boolean;
  criadoEm?: string;
}

interface AbaEdicao {
  conta: ContaBancaria;
  form: ContaBancaria;
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
  { campo: 'descricao', label: 'Descrição', largura: 220, minLargura: 120, padrao: true },
  { campo: 'tipoContaDescricao', label: 'Tipo', largura: 130, minLargura: 80, padrao: true },
  { campo: 'banco', label: 'Banco', largura: 160, minLargura: 80, padrao: true },
  { campo: 'agenciaCompleta', label: 'Agência', largura: 100, minLargura: 60, padrao: true },
  { campo: 'contaCompleta', label: 'Conta', largura: 120, minLargura: 60, padrao: true },
  { campo: 'filialNome', label: 'Filial', largura: 120, minLargura: 80, padrao: false },
  { campo: 'ativo', label: 'Ativo', largura: 60, minLargura: 50, padrao: true },
];

@Component({
  selector: 'app-contas-bancarias',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './contas-bancarias.component.html',
  styleUrl: './contas-bancarias.component.scss'
})
export class ContasBancariasComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_contasbancarias_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_contasbancarias';

  modo = signal<Modo>('lista');
  registros = signal<ContaBancaria[]>([]);
  selecionado = signal<ContaBancaria | null>(null);
  form = signal<ContaBancaria>(this.novoRegistro());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  filtroTipo = signal<'' | '1' | '2' | '3' | '4' | '5'>('');
  sortColuna = signal<string>('descricao');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  abasEdicao = signal<AbaEdicao[]>([]);
  abaAtivaId = signal<number | null>(null);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});
  private formOriginal: ContaBancaria | null = null;

  // Lookups
  planosContas = signal<PlanoContaResumo[]>([]);
  filiais = signal<FilialResumo[]>([]);

  // Autocomplete banco
  bancoBusca = signal('');
  bancoDropdownAberto = signal(false);
  bancoIndiceSelecionado = signal(-1);
  bancosFiltrados = computed(() => {
    const termo = this.normalizar(this.bancoBusca());
    if (termo.length < 1) return BANCOS_BRASIL;
    return BANCOS_BRASIL.filter(b =>
      this.normalizar(b.codigo).includes(termo) ||
      this.normalizar(b.nome).includes(termo) ||
      this.normalizar(`${b.codigo} - ${b.nome}`).includes(termo)
    );
  });

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

  private apiUrl = `${environment.apiUrl}/contasbancarias`;
  private tokenLiberacao: string | null = null;

  // Tipo conta é bancário (mostra campos banco/agência/conta)?
  ehBancario = computed(() => {
    const t = this.form().tipoConta;
    return t !== 3; // CaixaInterno = 3
  });

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('contas-bancarias', acao)) return true;
    const resultado = await this.modal.permissao('contas-bancarias', acao);
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

  ngOnInit() {
    this.carregar();
    this.carregarLookups();
  }
  ngOnDestroy() { sessionStorage.removeItem(this.STATE_KEY); }

  sairDaTela() {
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  private carregarLookups() {
    this.http.get<any>(`${environment.apiUrl}/planoscontas`).subscribe({
      next: r => {
        const lista = (r.data ?? [])
          .filter((p: any) => p.ativo)
          .map((p: any) => ({ id: p.id, descricao: p.descricao, codigoHierarquico: p.codigoHierarquico }));
        this.planosContas.set(lista);
      }
    });
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({
      next: r => {
        this.filiais.set((r.data ?? []).map((f: any) => ({ id: f.id, nomeFilial: f.nomeFilial })));
      }
    });
  }

  // ── State persistence ─────────────────────────────────────────────
  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abasIds: abas.map(a => a.conta.id),
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

  private restaurarAba(r: ContaBancaria, ativar: boolean) {
    if (this.abasEdicao().find(a => a.conta.id === r.id)) return;
    const aba: AbaEdicao = { conta: { ...r }, form: this.clonar(r), isDirty: false };
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
          this.modal.permissao('contas-bancarias', 'c').then(r => {
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
    const tipo = this.filtroTipo();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.registros().filter(r => {
      if (status === 'ativos' && !r.ativo) return false;
      if (status === 'inativos' && r.ativo) return false;
      if (tipo && r.tipoConta !== Number(tipo)) return false;
      if (termo.length < 2) return true;
      return this.normalizar(r.descricao).includes(termo) ||
             this.normalizar(r.banco ?? '').includes(termo);
    });

    if (!col) return lista;
    return [...lista].sort((a, b) => {
      let va: any, vb: any;
      if (col === 'agenciaCompleta') {
        va = this.agenciaCompleta(a); vb = this.agenciaCompleta(b);
      } else if (col === 'contaCompleta') {
        va = this.contaCompleta(a); vb = this.contaCompleta(b);
      } else {
        va = (a as any)[col] ?? ''; vb = (b as any)[col] ?? '';
      }
      const cmp = typeof va === 'boolean'
        ? (va === vb ? 0 : va ? -1 : 1)
        : typeof va === 'number'
          ? va - (vb as number)
          : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  agenciaCompleta(r: ContaBancaria): string {
    if (!r.agencia) return '';
    return r.agenciaDigito ? `${r.agencia}-${r.agenciaDigito}` : r.agencia;
  }

  contaCompleta(r: ContaBancaria): string {
    if (!r.numeroConta) return '';
    return r.contaDigito ? `${r.numeroConta}-${r.contaDigito}` : r.numeroConta;
  }

  private normalizar(s: string): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(r: ContaBancaria, campo: string): string {
    if (campo === 'agenciaCompleta') return this.agenciaCompleta(r);
    if (campo === 'contaCompleta') return this.contaCompleta(r);
    const v = (r as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Não';
    return v ?? '';
  }

  selecionar(r: ContaBancaria) { this.selecionado.set(r); }

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

  // ── CRUD ───────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.form.set(this.novoRegistro());
    this.formOriginal = this.clonar(this.novoRegistro());
    this.bancoBusca.set('');
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

    const jaAberta = this.abasEdicao().find(a => a.conta.id === r.id);
    if (jaAberta) {
      this.ativarAba(r.id);
      return;
    }

    const aba: AbaEdicao = { conta: { ...r }, form: this.clonar(r), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    this.abaAtivaId.set(r.id!);
    this.form.set(this.clonar(r));
    this.formOriginal = this.clonar(r);
    this.bancoBusca.set(r.banco ?? '');
    this.modoEdicao.set(true);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    const aba = this.abasEdicao().find(a => a.conta.id === id);
    if (!aba) return;
    this.abaAtivaId.set(id);
    this.form.set(this.clonar(aba.form));
    this.formOriginal = this.clonar(aba.form);
    this.bancoBusca.set(aba.form.banco ?? '');
    this.isDirty.set(aba.isDirty);
    this.modoEdicao.set(true);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  fecharAba(id: number) {
    this.abasEdicao.update(abas => abas.filter(a => a.conta.id !== id));
    if (this.abaAtivaId() === id) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) {
        this.ativarAba(restantes[restantes.length - 1].conta.id!);
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
          abas.map(a => a.conta.id === id ? { ...a, isDirty: false } : a)
        );
      }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (!id || this.modo() !== 'form') return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.conta.id === id
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
    if (Object.keys(erros).length) {
      this.errosCampos.set(erros);
      return;
    }
    this.errosCampos.set({});
    this.salvando.set(true);

    const headers = this.headerLiberacao();
    const body: any = {
      descricao: f.descricao,
      tipoConta: f.tipoConta,
      banco: f.tipoConta === 3 ? null : f.banco,
      agencia: f.tipoConta === 3 ? null : f.agencia,
      agenciaDigito: f.tipoConta === 3 ? null : f.agenciaDigito,
      numeroConta: f.tipoConta === 3 ? null : f.numeroConta,
      contaDigito: f.tipoConta === 3 ? null : f.contaDigito,
      chavePix: f.chavePix,
      saldoInicial: f.saldoInicial,
      dataSaldoInicial: f.dataSaldoInicial || null,
      planoContaId: f.planoContaId || null,
      filialId: f.filialId || null,
      observacao: f.observacao,
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
        this.erro.set(err.error?.message || 'Erro ao salvar conta bancária.');
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
        this.modal.erro('Erro', err.error?.message || 'Erro ao excluir conta bancária.');
      }
    });
  }

  // ── Form helpers ───────────────────────────────────────────────────
  upd(campo: keyof ContaBancaria, v: any) {
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
      abas.map(a => a.conta.id === id ? { ...a, isDirty: true } : a)
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

  // ── Autocomplete banco ─────────────────────────────────────────────
  onBancoInput(valor: string) {
    this.bancoBusca.set(valor);
    this.bancoDropdownAberto.set(true);
    this.bancoIndiceSelecionado.set(-1);
  }

  onBancoFocus() {
    this.bancoBusca.set(this.form().banco ?? '');
    this.bancoDropdownAberto.set(true);
  }

  onBancoBlur() {
    // Delay para permitir click no dropdown
    setTimeout(() => this.bancoDropdownAberto.set(false), 200);
  }

  selecionarBanco(banco: Banco) {
    const valor = `${banco.codigo} - ${banco.nome}`;
    this.upd('banco', valor);
    this.bancoBusca.set(valor);
    this.bancoDropdownAberto.set(false);
  }

  onBancoKeydown(e: KeyboardEvent) {
    const filtrados = this.bancosFiltrados();
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.bancoIndiceSelecionado.update(i => Math.min(i + 1, filtrados.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this.bancoIndiceSelecionado.update(i => Math.max(i - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const idx = this.bancoIndiceSelecionado();
      if (idx >= 0 && idx < filtrados.length) {
        this.selecionarBanco(filtrados[idx]);
      }
    } else if (e.key === 'Escape') {
      this.bancoDropdownAberto.set(false);
    }
  }

  // ── Utils ──────────────────────────────────────────────────────────
  private novoRegistro(): ContaBancaria {
    return {
      descricao: '', tipoConta: 1, banco: null, agencia: null, agenciaDigito: null,
      numeroConta: null, contaDigito: null, chavePix: null, saldoInicial: 0,
      dataSaldoInicial: null, planoContaId: null, filialId: null, observacao: null, ativo: true
    };
  }

  private clonar<T>(obj: T): T {
    return JSON.parse(JSON.stringify(obj));
  }
}
