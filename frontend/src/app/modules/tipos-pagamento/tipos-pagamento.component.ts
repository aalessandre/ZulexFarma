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

interface TipoPagamento {
  id?: number;
  nome: string;
  modalidade: number;
  modalidadeDescricao?: string;
  descontoMinimo: number;
  descontoMaxSemSenha: number;
  descontoMaxComSenha: number;
  aceitaPromocao: boolean;
  ordem: number;
  padraoSistema: boolean;
  planoContaId?: number;
  planoContaDescricao?: string;
  ativo: boolean;
  criadoEm?: string;
}

interface AbaEdicao { registro: TipoPagamento; form: TipoPagamento; isDirty: boolean; }
interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }
type Modo = 'lista' | 'form';

const COLUNAS: ColunaDef[] = [
  { campo: 'nome', label: 'Nome', largura: 220, minLargura: 120, padrao: true },
  { campo: 'modalidadeDescricao', label: 'Modalidade', largura: 140, minLargura: 80, padrao: true },
  { campo: 'descontoMaxSemSenha', label: '% Desc. s/ Senha', largura: 120, minLargura: 80, padrao: true },
  { campo: 'descontoMaxComSenha', label: '% Desc. c/ Senha', largura: 120, minLargura: 80, padrao: true },
  { campo: 'aceitaPromocao', label: 'Promoção', largura: 80, minLargura: 60, padrao: true },
  { campo: 'ativo', label: 'Ativo', largura: 60, minLargura: 50, padrao: true },
];

@Component({
  selector: 'app-tipos-pagamento',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './tipos-pagamento.component.html',
  styleUrl: './tipos-pagamento.component.scss'
})
export class TiposPagamentoComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_tipospagamento_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_tipospagamento';

  modo = signal<Modo>('lista');
  registros = signal<TipoPagamento[]>([]);
  selecionado = signal<TipoPagamento | null>(null);
  form = signal<TipoPagamento>(this.novoRegistro());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('nome');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  abasEdicao = signal<AbaEdicao[]>([]);
  abaAtivaId = signal<number | null>(null);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});
  private formOriginal: TipoPagamento | null = null;

  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal('');
  logDataFim = signal('');
  carregandoLog = signal(false);

  private apiUrl = `${environment.apiUrl}/tipospagamento`;
  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('tipos-pagamento', acao)) return true;
    const resultado = await this.modal.permissao('tipos-pagamento', acao);
    if (resultado.tokenLiberacao) this.tokenLiberacao = resultado.tokenLiberacao;
    return resultado.confirmado;
  }

  private headerLiberacao(): { [h: string]: string } {
    if (this.tokenLiberacao) { const h = { 'X-Liberacao': this.tokenLiberacao }; this.tokenLiberacao = null; return h; }
    return {};
  }

  ngOnInit() { this.carregar(); }
  ngOnDestroy() { sessionStorage.removeItem(this.STATE_KEY); }
  sairDaTela() { sessionStorage.removeItem(this.STATE_KEY); this.tabService.fecharTabAtiva(); }

  private persistirEstado() { this.salvarEstadoAbaAtiva(); const abas = this.abasEdicao(); if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; } sessionStorage.setItem(this.STATE_KEY, JSON.stringify({ abasIds: abas.map(a => a.registro.id), abaAtivaId: this.abaAtivaId() })); }
  private restaurarEstado() { try { const json = sessionStorage.getItem(this.STATE_KEY); if (!json) return; const state = JSON.parse(json); sessionStorage.removeItem(this.STATE_KEY); if (state.abasIds?.length > 0) { for (const id of state.abasIds) { const r = this.registros().find(x => x.id === id); if (r) this.restaurarAba(r, id === state.abaAtivaId); } } } catch {} }
  private restaurarAba(r: TipoPagamento, ativar: boolean) { if (this.abasEdicao().find(a => a.registro.id === r.id)) return; const aba: AbaEdicao = { registro: { ...r }, form: this.clonar(r), isDirty: false }; this.abasEdicao.update(abas => [...abas, aba]); if (ativar) this.ativarAba(r.id!); }

  private primeiroCarregamento = true;
  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => { this.registros.set(r.data ?? []); this.carregando.set(false); if (this.primeiroCarregamento) { this.primeiroCarregamento = false; this.restaurarEstado(); } },
      error: (e) => { this.carregando.set(false); if (e.status === 403) { this.modal.permissao('tipos-pagamento', 'c').then(r => { if (r.confirmado) this.carregar(); else this.tabService.fecharTabAtiva(); }); } }
    });
  }

  registrosFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();
    const lista = this.registros().filter(r => {
      if (status === 'ativos' && !r.ativo) return false;
      if (status === 'inativos' && r.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(r.nome).includes(termo);
    });
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean' ? (va === vb ? 0 : va ? -1 : 1) : typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string { return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim(); }
  getCellValue(r: TipoPagamento, campo: string): string {
    if (campo === 'descontoMaxSemSenha' || campo === 'descontoMaxComSenha') return `${(r as any)[campo]}%`;
    const v = (r as any)[campo]; if (typeof v === 'boolean') return v ? 'Sim' : 'Não'; return v ?? '';
  }
  selecionar(r: TipoPagamento) { this.selecionado.set(r); }
  ordenar(coluna: string) { if (this.sortColuna() === coluna) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc'); else { this.sortColuna.set(coluna); this.sortDirecao.set('asc'); } }
  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }

  private carregarColunas(): ColunaEstado[] { try { const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS); if (json) { const saved: ColunaEstado[] = JSON.parse(json); return COLUNAS.map(def => { const s = saved.find(c => c.campo === def.campo); return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura }; }); } } catch {} return COLUNAS.map(c => ({ ...c, visivel: c.padrao })); }
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

  async incluir() { if (!await this.verificarPermissao('i')) return; this.form.set(this.novoRegistro()); this.formOriginal = this.clonar(this.novoRegistro()); this.modoEdicao.set(false); this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({}); this.modo.set('form'); }
  async editar() { if (!await this.verificarPermissao('a')) return; const r = this.selecionado(); if (!r?.id) return; const ja = this.abasEdicao().find(a => a.registro.id === r.id); if (ja) { this.ativarAba(r.id); return; } const aba: AbaEdicao = { registro: { ...r }, form: this.clonar(r), isDirty: false }; this.abasEdicao.update(abas => [...abas, aba]); this.abaAtivaId.set(r.id!); this.form.set(this.clonar(r)); this.formOriginal = this.clonar(r); this.modoEdicao.set(true); this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({}); this.modo.set('form'); }
  ativarAba(id: number) { this.salvarEstadoAbaAtiva(); const aba = this.abasEdicao().find(a => a.registro.id === id); if (!aba) return; this.abaAtivaId.set(id); this.form.set(this.clonar(aba.form)); this.formOriginal = this.clonar(aba.form); this.isDirty.set(aba.isDirty); this.modoEdicao.set(true); this.erro.set(''); this.errosCampos.set({}); this.modo.set('form'); }
  fecharAba(id: number) { this.abasEdicao.update(abas => abas.filter(a => a.registro.id !== id)); if (this.abaAtivaId() === id) { const rest = this.abasEdicao(); if (rest.length > 0) this.ativarAba(rest[rest.length - 1].registro.id!); else { this.modo.set('lista'); this.abaAtivaId.set(null); } } }
  fechar() { this.modo.set('lista'); this.carregar(); }
  fecharForm() { if (this.modoEdicao()) { const id = this.abaAtivaId(); if (id) this.fecharAba(id); else this.modo.set('lista'); } else this.modo.set('lista'); }
  cancelarEdicao() { if (this.formOriginal) { this.form.set(this.clonar(this.formOriginal)); this.isDirty.set(false); const id = this.abaAtivaId(); if (id) this.abasEdicao.update(abas => abas.map(a => a.registro.id === id ? { ...a, isDirty: false } : a)); } }
  private salvarEstadoAbaAtiva() { const id = this.abaAtivaId(); if (!id || this.modo() !== 'form') return; this.abasEdicao.update(abas => abas.map(a => a.registro.id === id ? { ...a, form: this.clonar(this.form()), isDirty: this.isDirty() } : a)); }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const erros: Record<string, string> = {};
    const f = this.form();
    if (!f.nome.trim()) erros['nome'] = 'Nome é obrigatório.';
    if (f.descontoMaxSemSenha < 0 || f.descontoMaxSemSenha > 90) erros['descontoMaxSemSenha'] = 'Entre 0% e 90%.';
    if (f.descontoMaxComSenha < 0 || f.descontoMaxComSenha > 90) erros['descontoMaxComSenha'] = 'Entre 0% e 90%.';
    if (Object.keys(erros).length) { this.errosCampos.set(erros); return; }
    this.errosCampos.set({}); this.salvando.set(true);
    const headers = this.headerLiberacao();
    const body: any = { nome: f.nome, modalidade: f.modalidade, descontoMinimo: f.descontoMinimo, descontoMaxSemSenha: f.descontoMaxSemSenha, descontoMaxComSenha: f.descontoMaxComSenha, aceitaPromocao: f.aceitaPromocao, ordem: f.ordem, planoContaId: f.planoContaId || null, ativo: f.ativo };
    const op$ = this.modoEdicao() ? this.http.put(`${this.apiUrl}/${f.id}`, body, { headers }) : this.http.post<any>(this.apiUrl, body, { headers });
    op$.subscribe({
      next: (r: any) => { this.salvando.set(false); this.isDirty.set(false); if (this.modoEdicao()) this.fecharAba(f.id!); this.carregar(); this.modo.set('lista'); },
      error: (err) => { this.erro.set(err.error?.message || 'Erro ao salvar.'); this.salvando.set(false); }
    });
  }

  async excluir() {
    const r = this.selecionado(); if (!r?.id) return;
    const resultado = await this.modal.confirmar('Confirmar Exclusão', `Deseja excluir "${r.nome}"?`, 'Sim, excluir', 'Não, manter');
    if (!resultado.confirmado) return; if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao(); this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${r.id}`, { headers }).subscribe({
      next: async (res: any) => { this.excluindo.set(false); this.selecionado.set(null); this.fecharAba(r.id!); this.carregar(); if (res.resultado === 'desativado') await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.'); },
      error: () => { this.excluindo.set(false); this.modal.erro('Erro', 'Erro ao excluir.'); }
    });
  }

  upd(campo: keyof TipoPagamento, v: any) { this.form.update(f => ({ ...f, [campo]: v })); this.isDirty.set(true); this.atualizarDirtyAba(); }
  erroCampo(campo: string): string { return this.errosCampos()[campo] ?? ''; }
  private atualizarDirtyAba() { const id = this.abaAtivaId(); if (!id) return; this.abasEdicao.update(abas => abas.map(a => a.registro.id === id ? { ...a, isDirty: true } : a)); }

  abrirLog() { const r = this.selecionado(); if (!r?.id) return; this.modalLog.set(true); this.logRegistros.set([]); this.logSelecionado.set(null); this.filtrarLog(); }
  fecharLog() { this.modalLog.set(false); }
  filtrarLog() { const r = this.selecionado(); if (!r?.id) return; this.carregandoLog.set(true); let url = `${this.apiUrl}/${r.id}/log`; const params: string[] = []; if (this.logDataInicio()) params.push(`dataInicio=${this.logDataInicio()}`); if (this.logDataFim()) params.push(`dataFim=${this.logDataFim()}`); if (params.length) url += '?' + params.join('&'); this.http.get<any>(url).subscribe({ next: res => { this.logRegistros.set(res.data ?? []); this.carregandoLog.set(false); if (res.data?.length > 0) this.selecionarLogEntry(res.data[0]); }, error: () => this.carregandoLog.set(false) }); }
  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }
  acaoCss(acao: string): string { const map: Record<string, string> = { 'CRIAÇÃO': 'log-badge badge-criacao', 'ALTERAÇÃO': 'log-badge badge-alteracao', 'EXCLUSÃO': 'log-badge badge-exclusao', 'DESATIVAÇÃO': 'log-badge badge-desativacao' }; return map[acao] ?? 'log-badge'; }

  // ── Plano de Contas lookup ──────────────────────────────────────
  pcResultados = signal<any[]>([]);
  pcDropdown = signal(false);
  private pcTimer: any = null;

  onPcBuscaInput(valor: string) {
    this.upd('planoContaDescricao', valor);
    if (this.pcTimer) clearTimeout(this.pcTimer);
    if (valor.trim().length < 2) { this.pcResultados.set([]); this.pcDropdown.set(false); return; }
    this.pcTimer = setTimeout(() => {
      this.http.get<any>(`${environment.apiUrl}/planoscontas/pesquisar?termo=${encodeURIComponent(valor.trim())}`).subscribe({
        next: r => { this.pcResultados.set(r.data ?? []); this.pcDropdown.set((r.data ?? []).length > 0); }
      });
    }, 300);
  }

  selecionarPc(pc: any) {
    this.upd('planoContaId', pc.id);
    this.upd('planoContaDescricao', `${pc.codigoHierarquico} - ${pc.descricao}`);
    this.pcDropdown.set(false);
  }

  private novoRegistro(): TipoPagamento { return { nome: '', modalidade: 1, descontoMinimo: 0, descontoMaxSemSenha: 0, descontoMaxComSenha: 0, aceitaPromocao: true, ordem: 99, padraoSistema: false, planoContaId: undefined, ativo: true }; }
  private clonar<T>(obj: T): T { return JSON.parse(JSON.stringify(obj)); }
}
