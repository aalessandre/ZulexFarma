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
interface PessoaLookup { id: number; nome: string; cpfCnpj: string; tipo: string; }
interface TipoPagLookup { id: number; nome: string; }
interface AgrupadorItem { id: number; nome: string; }

interface ConvenioList {
  id: number; pessoaId: number; pessoaNome: string; pessoaCpfCnpj?: string; pessoaTipo?: string;
  aviso?: string; modoFechamento: number; modoFechamentoDescricao: string;
  limiteCredito: number; bloqueado: boolean; ativo: boolean; criadoEm: string;
}

interface ConvenioDetalhe {
  id: number; pessoaId: number; pessoaNome: string; pessoaCpfCnpj?: string; pessoaTipo?: string;
  pessoaRazaoSocial?: string; pessoaIeRg?: string;
  aviso?: string; observacao?: string;
  modoFechamento: number; diasCorridos?: number; diaFechamento?: number; diaVencimento?: number; mesesParaVencimento: number;
  qtdeViasCupom: number; bloqueado: boolean; bloquearVendaParcelada: boolean; bloquearDescontoParcelada: boolean;
  bloquearComissao: boolean; venderSomenteComSenha: boolean; cobrarJurosAtraso: boolean; diasCarenciaBloqueio: number;
  limiteCredito: number; maximoParcelas: number; ativo: boolean; criadoEm: string;
  descontos: DescontoItem[]; bloqueios: BloqueioItem[];
}

interface DescontoItem { id?: number; tipoAgrupador: number; agrupadorId: number; agrupadorNome: string; descontoMinimo: number; descontoMaxSemSenha: number; descontoMaxComSenha: number; }
interface BloqueioItem { tipoPagamentoId: number; tipoPagamentoNome: string; }

interface ConvenioForm {
  pessoaId: number; tipo: string; cpfCnpj: string; nome: string; razaoSocial: string; inscricaoEstadual: string; rg: string;
  aviso: string; observacao: string;
  modoFechamento: number; diasCorridos: number; diaFechamento: number; diaVencimento: number; mesesParaVencimento: number;
  qtdeViasCupom: number; bloqueado: boolean; bloquearVendaParcelada: boolean; bloquearDescontoParcelada: boolean;
  bloquearComissao: boolean; venderSomenteComSenha: boolean; cobrarJurosAtraso: boolean; diasCarenciaBloqueio: number;
  limiteCredito: number; maximoParcelas: number; ativo: boolean;
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }
type Modo = 'lista' | 'form';

const COLUNAS: ColunaDef[] = [
  { campo: 'pessoaNome', label: 'Nome', largura: 240, minLargura: 120, padrao: true },
  { campo: 'pessoaCpfCnpj', label: 'CPF/CNPJ', largura: 150, minLargura: 100, padrao: true },
  { campo: 'modoFechamentoDescricao', label: 'Fechamento', largura: 130, minLargura: 80, padrao: true },
  { campo: 'limiteCredito', label: 'Limite', largura: 110, minLargura: 70, padrao: true },
  { campo: 'bloqueado', label: 'Bloqueado', largura: 80, minLargura: 60, padrao: true },
  { campo: 'ativo', label: 'Ativo', largura: 60, minLargura: 50, padrao: true },
];

@Component({
  selector: 'app-convenios',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './convenios.component.html',
  styleUrl: './convenios.component.scss'
})
export class ConveniosComponent implements OnInit, OnDestroy {
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_convenios';

  modo = signal<Modo>('lista');
  registros = signal<ConvenioList[]>([]);
  selecionado = signal<ConvenioList | null>(null);
  detalhe = signal<ConvenioDetalhe | null>(null);
  form = signal<ConvenioForm>(this.novoForm());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('pessoaNome');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});

  // Pessoa
  buscandoCnpj = signal(false);
  pessoaEncontrada = signal<any>(null);

  // Sub-tabelas
  descontos = signal<DescontoItem[]>([]);
  bloqueioIds = signal<Set<number>>(new Set());

  // Lookups
  tiposPagamento = signal<TipoPagLookup[]>([]);

  // Agrupadores
  agrupadoresAberto = signal('');
  agrupadores = signal<AgrupadorItem[]>([]);
  descTipoAgrupador = signal(1);
  descAgrupadorId = signal(0);
  descMinimo = signal(0);
  descSemSenha = signal(0);
  descComSenha = signal(0);

  // Accordions
  accGeral = signal(true);
  accEnderecos = signal(false);
  accContatos = signal(false);
  accLimites = signal(false);
  accDescontos = signal(false);

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

  private apiUrl = `${environment.apiUrl}/convenios`;
  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('convenios', acao)) return true;
    const r = await this.modal.permissao('convenios', acao);
    if (r.tokenLiberacao) this.tokenLiberacao = r.tokenLiberacao;
    return r.confirmado;
  }
  private headerLiberacao(): { [h: string]: string } { if (this.tokenLiberacao) { const h = { 'X-Liberacao': this.tokenLiberacao }; this.tokenLiberacao = null; return h; } return {}; }

  ngOnInit() { this.carregar(); this.carregarLookups(); }
  ngOnDestroy() {}
  sairDaTela() { this.tabService.fecharTabAtiva(); }

  private carregarLookups() {
    this.http.get<any>(`${environment.apiUrl}/tipospagamento`).subscribe({
      next: r => this.tiposPagamento.set((r.data ?? []).filter((t: any) => t.ativo).map((t: any) => ({ id: t.id, nome: t.nome })))
    });
  }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => { this.registros.set(r.data ?? []); this.carregando.set(false); },
      error: (e) => { this.carregando.set(false); if (e.status === 403) { this.modal.permissao('convenios', 'c').then(r => { if (r.confirmado) this.carregar(); else this.tabService.fecharTabAtiva(); }); } }
    });
  }

  registrosFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna(); const dir = this.sortDirecao();
    const lista = this.registros().filter(r => {
      if (status === 'ativos' && !r.ativo) return false;
      if (status === 'inativos' && r.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(r.pessoaNome).includes(termo) || (r.pessoaCpfCnpj ?? '').includes(termo);
    });
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean' ? (va === vb ? 0 : va ? -1 : 1) : typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string { return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim(); }
  getCellValue(r: ConvenioList, campo: string): string {
    if (campo === 'limiteCredito') return (r as any)[campo]?.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) ?? '';
    const v = (r as any)[campo]; if (typeof v === 'boolean') return v ? 'Sim' : 'Não'; return v ?? '';
  }
  selecionar(r: ConvenioList) { this.selecionado.set(r); }
  ordenar(coluna: string) { if (this.sortColuna() === coluna) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc'); else { this.sortColuna.set(coluna); this.sortDirecao.set('asc'); } }
  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }

  // Colunas
  private carregarColunas(): ColunaEstado[] { try { const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS); if (json) { const saved: ColunaEstado[] = JSON.parse(json); return COLUNAS.map(def => { const s = saved.find(c => c.campo === def.campo); return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura }; }); } } catch {} return COLUNAS.map(c => ({ ...c, visivel: c.padrao })); }
  private salvarColunasStorage() { localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas())); }
  toggleColunaVisivel(campo: string) { this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c)); this.salvarColunasStorage(); }
  restaurarPadrao() { this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao }))); this.salvarColunasStorage(); }
  iniciarResize(e: MouseEvent, campo: string, largura: number) { e.stopPropagation(); e.preventDefault(); this.resizeState = { campo, startX: e.clientX, startWidth: largura }; document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none'; }
  @HostListener('document:mousemove', ['$event']) onMouseMove(e: MouseEvent) { if (!this.resizeState) return; const d = e.clientX - this.resizeState.startX; const def = COLUNAS.find(c => c.campo === this.resizeState!.campo); const nw = Math.max(def?.minLargura ?? 50, this.resizeState.startWidth + d); this.colunas.update(cols => cols.map(c => c.campo === this.resizeState!.campo ? { ...c, largura: nw } : c)); }
  @HostListener('document:mouseup') onMouseUp() { if (this.resizeState) { this.salvarColunasStorage(); this.resizeState = null; document.body.style.cursor = ''; document.body.style.userSelect = ''; } }
  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => { const arr = [...cols]; const [moved] = arr.splice(this.dragColIdx!, 1); arr.splice(idx, 0, moved); this.dragColIdx = idx; return arr; });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunasStorage(); }

  // ── Pessoa (busca CPF/CNPJ - padrão fornecedor) ────────────────────
  onTipoChange(v: string) {
    this.form.update(f => ({ ...f, tipo: v, cpfCnpj: '', nome: '', razaoSocial: '', inscricaoEstadual: '', rg: '', pessoaId: 0 }));
    this.pessoaEncontrada.set(null);
    this.isDirty.set(true);
  }

  onCpfCnpjInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const tipo = this.form().tipo;
    const mascarado = tipo === 'F' ? this.mascaraCpf(input.value) : this.mascaraCnpj(input.value);
    input.value = mascarado;
    this.upd('cpfCnpj', mascarado);

    if (tipo === 'J') {
      const digits = mascarado.replace(/\D/g, '');
      if (digits.length === 14) this.buscarCnpj(digits);
    }
  }

  onCpfCnpjBlur() {
    if (this.modoEdicao()) return;
    const cpfCnpj = this.form().cpfCnpj;
    const digits = cpfCnpj.replace(/\D/g, '');
    if (digits.length !== 11 && digits.length !== 14) { this.pessoaEncontrada.set(null); return; }

    this.http.get<any>(`${environment.apiUrl}/pessoas/buscar-cpfcnpj?valor=${cpfCnpj}`).subscribe({
      next: r => {
        const p = r?.data;
        if (!p) { this.pessoaEncontrada.set(null); return; }
        this.pessoaEncontrada.set(p);
        this.form.update(f => ({
          ...f, pessoaId: p.pessoaId,
          nome: p.nome || f.nome, razaoSocial: p.razaoSocial || f.razaoSocial,
          inscricaoEstadual: p.inscricaoEstadual || f.inscricaoEstadual, rg: p.rg || f.rg
        }));
      },
      error: () => this.pessoaEncontrada.set(null)
    });
  }

  private buscarCnpj(cnpj: string) {
    this.buscandoCnpj.set(true);
    this.http.get<any>(`https://brasilapi.com.br/api/cnpj/v1/${cnpj}`).subscribe({
      next: r => {
        this.buscandoCnpj.set(false);
        if (r.razao_social) {
          this.form.update(f => ({
            ...f, razaoSocial: r.razao_social?.toUpperCase() || f.razaoSocial,
            nome: r.nome_fantasia?.toUpperCase() || r.razao_social?.toUpperCase() || f.nome
          }));
          this.isDirty.set(true);
        }
      },
      error: () => {
        // Fallback silencioso - se APIs externas falharem, usuário preenche manualmente
        this.http.get<any>(`https://receitaws.com.br/v1/cnpj/${cnpj}`).subscribe({
          next: r => {
            this.buscandoCnpj.set(false);
            if (r.nome && r.status !== 'ERROR') {
              this.form.update(f => ({
                ...f, razaoSocial: r.nome?.toUpperCase() || f.razaoSocial,
                nome: r.fantasia?.toUpperCase() || r.nome?.toUpperCase() || f.nome
              }));
              this.isDirty.set(true);
            }
          },
          error: () => this.buscandoCnpj.set(false) // Silencioso - preenche manualmente
        });
      }
    });
  }

  // ── Máscaras ──────────────────────────────────────────────────────
  private mascaraCpf(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 11);
    if (d.length <= 3) return d;
    if (d.length <= 6) return `${d.slice(0,3)}.${d.slice(3)}`;
    if (d.length <= 9) return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6)}`;
    return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6,9)}-${d.slice(9)}`;
  }

  private mascaraCnpj(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 14);
    if (d.length <= 2) return d;
    if (d.length <= 5) return `${d.slice(0,2)}.${d.slice(2)}`;
    if (d.length <= 8) return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5)}`;
    if (d.length <= 12) return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8)}`;
    return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8,12)}-${d.slice(12)}`;
  }

  // ── CRUD ───────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.form.set(this.novoForm()); this.descontos.set([]); this.bloqueioIds.set(new Set());
    this.pessoaEncontrada.set(null);
    this.modoEdicao.set(false); this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({});
    this.accGeral.set(true); this.accEnderecos.set(false); this.accContatos.set(false); this.accLimites.set(false); this.accDescontos.set(false);
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const r = this.selecionado(); if (!r?.id) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${r.id}`).subscribe({
      next: res => {
        this.carregando.set(false);
        const d: ConvenioDetalhe = res.data;
        this.detalhe.set(d);
        this.form.set({
          pessoaId: d.pessoaId, tipo: d.pessoaTipo ?? 'J', cpfCnpj: d.pessoaCpfCnpj ?? '',
          nome: d.pessoaNome, razaoSocial: d.pessoaRazaoSocial ?? '',
          inscricaoEstadual: d.pessoaIeRg ?? '', rg: d.pessoaIeRg ?? '',
          aviso: d.aviso ?? '', observacao: d.observacao ?? '',
          modoFechamento: d.modoFechamento, diasCorridos: d.diasCorridos ?? 30,
          diaFechamento: d.diaFechamento ?? 1, diaVencimento: d.diaVencimento ?? 10,
          mesesParaVencimento: d.mesesParaVencimento, qtdeViasCupom: d.qtdeViasCupom,
          bloqueado: d.bloqueado, bloquearVendaParcelada: d.bloquearVendaParcelada,
          bloquearDescontoParcelada: d.bloquearDescontoParcelada, bloquearComissao: d.bloquearComissao,
          venderSomenteComSenha: d.venderSomenteComSenha, cobrarJurosAtraso: d.cobrarJurosAtraso,
          diasCarenciaBloqueio: d.diasCarenciaBloqueio, limiteCredito: d.limiteCredito,
          maximoParcelas: d.maximoParcelas, ativo: d.ativo
        });
        this.pessoaEncontrada.set(null);
        this.descontos.set(d.descontos);
        this.bloqueioIds.set(new Set(d.bloqueios.map(b => b.tipoPagamentoId)));
        this.modoEdicao.set(true); this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({});
        this.accGeral.set(true);
        this.modo.set('form');
      },
      error: () => { this.carregando.set(false); this.modal.erro('Erro', 'Erro ao carregar convênio.'); }
    });
  }

  fechar() { this.modo.set('lista'); this.carregar(); }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const erros: Record<string, string> = {};
    const f = this.form();
    if (!f.cpfCnpj.replace(/\D/g, '')) { this.modal.aviso('Campo obrigatório', 'Informe o CPF/CNPJ.'); return; }
    if (!f.nome.trim()) { this.modal.aviso('Campo obrigatório', f.tipo === 'J' ? 'Informe o Nome Fantasia.' : 'Informe o Nome.'); return; }
    if (Object.keys(erros).length) { this.errosCampos.set(erros); return; }
    this.errosCampos.set({}); this.salvando.set(true);
    const headers = this.headerLiberacao();
    const body = {
      ...f, aviso: f.aviso || null, observacao: f.observacao || null,
      descontos: this.descontos(),
      bloqueioTipoPagamentoIds: Array.from(this.bloqueioIds())
    };
    const id = this.detalhe()?.id;
    const op$ = this.modoEdicao() && id
      ? this.http.put(`${this.apiUrl}/${id}`, body, { headers })
      : this.http.post(this.apiUrl, body, { headers });
    op$.subscribe({
      next: () => { this.salvando.set(false); this.isDirty.set(false); this.carregar(); this.modo.set('lista'); },
      error: (err) => { this.salvando.set(false); this.modal.erro('Erro ao salvar', err.error?.message || 'Erro ao salvar convênio.'); }
    });
  }

  async excluir() {
    const r = this.selecionado(); if (!r?.id) return;
    const res = await this.modal.confirmar('Excluir', `Excluir convênio de "${r.pessoaNome}"?`, 'Sim', 'Não');
    if (!res.confirmado) return; if (!await this.verificarPermissao('e')) return;
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${r.id}`, { headers: this.headerLiberacao() }).subscribe({
      next: async (res: any) => { this.excluindo.set(false); this.selecionado.set(null); this.carregar();
        if (res.resultado === 'desativado') await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.'); },
      error: () => { this.excluindo.set(false); this.modal.erro('Erro', 'Erro ao excluir.'); }
    });
  }

  upd(campo: string, v: any) { this.form.update(f => ({ ...f, [campo]: v })); this.isDirty.set(true); }
  erroCampo(campo: string): string { return this.errosCampos()[campo] ?? ''; }

  // ── Descontos ─────────────────────────────────────────────────────
  carregarAgrupadores(tipo: number) {
    this.descTipoAgrupador.set(tipo);
    this.descAgrupadorId.set(0);
    this.agrupadores.set([]);
    let url = '';
    switch (tipo) {
      case 1: url = `${environment.apiUrl}/grupos-principais`; break;
      case 2: url = `${environment.apiUrl}/grupos-produtos`; break;
      case 3: url = `${environment.apiUrl}/subgrupos`; break;
      case 4: url = `${environment.apiUrl}/secoes`; break;
    }
    if (!url) return;
    this.http.get<any>(url).subscribe({
      next: r => this.agrupadores.set((r.data ?? []).map((a: any) => ({ id: a.id, nome: a.nome }))),
      error: () => this.agrupadores.set([])
    });
  }

  adicionarDesconto() {
    const agr = this.agrupadores().find(a => a.id === this.descAgrupadorId());
    if (!agr) return;
    const ja = this.descontos().find(d => d.tipoAgrupador === this.descTipoAgrupador() && d.agrupadorId === agr.id);
    if (ja) return;
    this.descontos.update(ds => [...ds, {
      tipoAgrupador: this.descTipoAgrupador(), agrupadorId: agr.id, agrupadorNome: agr.nome,
      descontoMinimo: this.descMinimo(), descontoMaxSemSenha: this.descSemSenha(), descontoMaxComSenha: this.descComSenha()
    }]);
    this.isDirty.set(true);
  }

  removerDesconto(idx: number) { this.descontos.update(ds => ds.filter((_, i) => i !== idx)); this.isDirty.set(true); }

  tipoAgrupadorNome(t: number): string {
    switch (t) { case 1: return 'Grupo Principal'; case 2: return 'Grupo'; case 3: return 'SubGrupo'; case 4: return 'Seção'; default: return ''; }
  }

  // ── Bloqueios ─────────────────────────────────────────────────────
  toggleBloqueio(tpId: number) {
    this.bloqueioIds.update(s => { const ns = new Set(s); if (ns.has(tpId)) ns.delete(tpId); else ns.add(tpId); return ns; });
    this.isDirty.set(true);
  }
  isBloqueado(tpId: number): boolean { return this.bloqueioIds().has(tpId); }

  // ── Log ────────────────────────────────────────────────────────────
  abrirLog() { const r = this.selecionado(); if (!r?.id) return; this.modalLog.set(true); this.logRegistros.set([]); this.logSelecionado.set(null); this.filtrarLog(); }
  fecharLog() { this.modalLog.set(false); }
  filtrarLog() { const r = this.selecionado(); if (!r?.id) return; this.carregandoLog.set(true); let url = `${this.apiUrl}/${r.id}/log`; const p: string[] = []; if (this.logDataInicio()) p.push(`dataInicio=${this.logDataInicio()}`); if (this.logDataFim()) p.push(`dataFim=${this.logDataFim()}`); if (p.length) url += '?' + p.join('&'); this.http.get<any>(url).subscribe({ next: res => { this.logRegistros.set(res.data ?? []); this.carregandoLog.set(false); if (res.data?.length > 0) this.selecionarLogEntry(res.data[0]); }, error: () => this.carregandoLog.set(false) }); }
  selecionarLogEntry(e: LogEntry) { this.logSelecionado.set(e); }
  acaoCss(acao: string): string { const map: Record<string, string> = { 'CRIAÇÃO': 'log-badge badge-criacao', 'ALTERAÇÃO': 'log-badge badge-alteracao', 'EXCLUSÃO': 'log-badge badge-exclusao', 'DESATIVAÇÃO': 'log-badge badge-desativacao' }; return map[acao] ?? 'log-badge'; }

  private novoForm(): ConvenioForm {
    return { pessoaId: 0, tipo: 'J', cpfCnpj: '', nome: '', razaoSocial: '', inscricaoEstadual: '', rg: '', aviso: '', observacao: '', modoFechamento: 1, diasCorridos: 30, diaFechamento: 1, diaVencimento: 10, mesesParaVencimento: 1, qtdeViasCupom: 1, bloqueado: false, bloquearVendaParcelada: false, bloquearDescontoParcelada: false, bloquearComissao: false, venderSomenteComSenha: false, cobrarJurosAtraso: true, diasCarenciaBloqueio: 0, limiteCredito: 0, maximoParcelas: 1, ativo: true };
  }
  private clonar<T>(obj: T): T { return JSON.parse(JSON.stringify(obj)); }
}
