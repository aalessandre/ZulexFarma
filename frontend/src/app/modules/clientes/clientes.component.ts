import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CLIENTES_COLUNAS, ColunaDef } from './clientes.columns';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface LogCampo { campo: string; valorAnterior?: string; valorAtual?: string; }
interface LogEntry { id: number; realizadoEm: string; acao: string; nomeUsuario: string; campos: LogCampo[]; }

interface Contato { id?: number; tipo: string; valor: string; descricao?: string; principal: boolean; }
interface Endereco {
  id?: number; tipo: string; cep: string; rua: string; numero: string;
  complemento?: string; bairro: string; cidade: string; uf: string; principal: boolean;
}

interface ConvenioCliente { id?: number; convenioId: number; convenioNome: string; matricula: string; cartao: string; }
interface AutorizacaoCliente { id?: number; nome: string; }
interface DescontoCliente {
  id?: number; produtoId?: number; tipoAgrupador?: number; agrupadorId?: number;
  agrupadorOuProdutoNome: string; descontoMinimo: number; descontoMaxSemSenha: number; descontoMaxComSenha: number;
}
interface UsoContinuoCliente {
  id?: number; produtoId: number; produtoNome: string; fabricante: string; apresentacao: number;
  qtdeAoDia: number; ultimaCompra: string; proximaCompra: string; colaboradorNome: string;
}

interface AgrupadorItem { id: number; nome: string; }
interface ConvenioLookup { id: number; pessoaNome: string; }
interface ProdutoLookup { id: number; nome: string; fabricanteNome: string; apresentacao: number; }

interface Cliente {
  id?: number; tipo: string; nome: string; razaoSocial?: string; cpfCnpj: string;
  inscricaoEstadual?: string; rg?: string;
  email?: string; telefone?: string; cidade?: string; uf?: string;
  bloqueado: boolean; ativo: boolean; criadoEm?: string;
}

interface ClienteDetalhe extends Cliente {
  dataNascimento?: string;
  observacao?: string; aviso?: string;
  limiteCredito: number; descontoGeral: number;
  permiteFidelidade: boolean;
  prazoPagamento: number; qtdeDias: number; diaFechamento: number; diaVencimento: number; qtdeMeses: number;
  permiteVendaParcelada: boolean; qtdeMaxParcelas: number;
  permiteVendaPrazo: boolean; permiteVendaVista: boolean;
  bloquearDescontoParcelada: boolean; venderSomenteComSenha: boolean;
  cobrarJurosAtraso: boolean; bloquearComissao: boolean;
  diasCarenciaBloqueio: number;
  calcularJuros: boolean;
  pedirSenhaVendaPrazo: boolean; senhaVendaPrazo: string;
  enderecos: Endereco[];
  contatos: Contato[];
  convenios: ConvenioCliente[];
  autorizacoes: AutorizacaoCliente[];
  descontos: DescontoCliente[];
  usosContinuos: UsoContinuoCliente[];
  bloqueios: { tipoPagamentoId: number; tipoPagamentoNome: string; }[];
}

interface TipoPagLookup { id: number; nome: string; }

interface AbaEdicao { cliente: Cliente; form: ClienteDetalhe; isDirty: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

type Modo = 'lista' | 'form';

@Component({
  selector: 'app-clientes',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './clientes.component.html',
  styleUrl: './clientes.component.scss'
})
export class ClientesComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_clientes_state';
  modo = signal<Modo>('lista');
  registros = signal<Cliente[]>([]);
  selecionado = signal<Cliente | null>(null);
  clienteForm = signal<ClienteDetalhe>(this.novoCliente());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  buscandoCep = signal(false);
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
  private formOriginal: ClienteDetalhe | null = null;

  // Modais
  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  carregandoLog = signal(false);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal<string>(this.hoje(-30));
  logDataFim    = signal<string>(this.hoje(0));

  // Colunas
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_clientes';
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  private apiUrl = `${environment.apiUrl}/clientes`;

  tiposContato = ['TELEFONE', 'CELULAR', 'EMAIL', 'WHATSAPP', 'OUTRO'];
  tiposEndereco = ['PRINCIPAL', 'COBRANCA', 'ENTREGA', 'OUTRO'];

  // Accordions
  accEnderecos = signal(false);
  accContatos = signal(false);
  accConvenios = signal(false);
  accGeral = signal(false);
  accAutorizacoes = signal(false);
  accDescontos = signal(false);
  accUsoContinuo = signal(false);

  // Bloqueio condições de pagamento
  tiposPagamento = signal<TipoPagLookup[]>([]);
  bloqueioIds = signal<Set<number>>(new Set());
  Math = Math;

  // CPF/CNPJ
  buscandoCnpj = signal(false);
  pessoaEncontrada = signal<any>(null);

  // Convenio lookup
  conveniosLookup = signal<ConvenioLookup[]>([]);
  convAddId = signal(0);
  convAddMatricula = signal('');
  convAddCartao = signal('');
  // Autorizacao
  autAddNome = signal('');

  // Descontos
  descTipoAgrupador = signal(1);
  descAgrupadorId = signal(0);
  descMinimo = signal(0);
  descSemSenha = signal(0);
  descComSenha = signal(0);
  agrupadores = signal<AgrupadorItem[]>([]);
  descProdutoBusca = signal('');
  descProdutoResultados = signal<ProdutoLookup[]>([]);
  descProdutoSelecionado = signal<ProdutoLookup | null>(null);
  descProdMinimo = signal(0);
  descProdSemSenha = signal(0);
  descProdComSenha = signal(0);

  // Uso Continuo
  ucProdutoBusca = signal('');
  ucProdutoResultados = signal<ProdutoLookup[]>([]);
  ucProdutoSelecionado = signal<ProdutoLookup | null>(null);
  ucQtdeAoDia = signal(1);
  ucUltimaCompra = signal('');
  ucColaboradorNome = signal('');

  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('clientes', acao)) return true;
    const resultado = await this.modal.permissao('clientes', acao);
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
    this.carregarConveniosLookup();
    this.carregarTiposPagamento();
  }

  ngOnDestroy() { sessionStorage.removeItem(this.STATE_KEY); }

  sairDaTela() {
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abasIds: abas.map(a => a.cliente.id),
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
          const c = this.registros().find(x => x.id === id);
          if (c) this.restaurarAba(c, id === state.abaAtivaId);
        }
      }
    } catch {}
  }

  private restaurarAba(c: any, ativar: boolean) {
    this.http.get<any>(`${this.apiUrl}/${c.id}`).subscribe({
      next: r => {
        if (this.abasEdicao().find(a => a.cliente.id === c.id)) return;
        const detalhe = this.mapDetalhe(c, r.data);
        const novaAba: AbaEdicao = { cliente: { ...c }, form: this.clonarDetalhe(detalhe), isDirty: false };
        this.abasEdicao.update(tabs => [...tabs, novaAba]);
        if (ativar) this.ativarAba(c.id!);
      },
      error: () => {}
    });
  }

  private primeiroCarregamento = true;

  private carregarConveniosLookup() {
    this.http.get<any>(`${environment.apiUrl}/convenios`).subscribe({
      next: r => {
        const lista = (r.data ?? []).filter((c: any) => c.ativo).map((c: any) => ({ id: c.id, pessoaNome: c.pessoaNome }));
        this.conveniosLookup.set(lista);
      },
      error: () => {}
    });
  }

  private carregarTiposPagamento() {
    this.http.get<any>(`${environment.apiUrl}/tipospagamento`).subscribe({
      next: r => this.tiposPagamento.set(
        (r.data ?? []).filter((t: any) => t.ativo).map((t: any) => ({ id: t.id, nome: t.nome }))
      )
    });
  }

  toggleBloqueio(tpId: number) {
    this.bloqueioIds.update(s => { const ns = new Set(s); if (ns.has(tpId)) ns.delete(tpId); else ns.add(tpId); return ns; });
    this.isDirty.set(true);
  }

  isBloqueado(tpId: number): boolean { return this.bloqueioIds().has(tpId); }

  // ── Dados ─────────────────────────────────────────────────────────
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
          this.modal.permissao('clientes', 'c').then(r => {
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

    const lista = this.registros().filter(c => {
      if (status === 'ativos'   && !c.ativo) return false;
      if (status === 'inativos' &&  c.ativo) return false;
      if (termo.length < 3) return true;
      const termoDigitos = termo.replace(/\D/g, '');
      return (
        this.normalizar(c.nome).includes(termo) ||
        this.normalizar(c.razaoSocial ?? '').includes(termo) ||
        (termoDigitos.length > 0 && c.cpfCnpj.replace(/\D/g, '').includes(termoDigitos)) ||
        this.normalizar(c.cidade ?? '').includes(termo)
      );
    });

    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean'
        ? (va === vb ? 0 : va ? -1 : 1)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(c: Cliente, campo: string): string {
    const v = (c as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Nao';
    if (campo === 'tipo') return v === 'F' ? 'PF' : 'PJ';
    return v ?? '';
  }

  selecionar(c: Cliente) { this.selecionado.set(c); }
  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '\u25B2' : '\u25BC') : '\u21C5'; }

  //── Colunas: resize ───────────────────────────────────────────────
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
    const def = CLIENTES_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(CLIENTES_COLUNAS.map(d => ({ ...d, visivel: d.padrao })));
    localStorage.removeItem(this.STORAGE_KEY_COLUNAS);
  }

  private carregarColunas(): ColunaEstado[] {
    let salvo: { campo: string; visivel: boolean; largura: number }[] = [];
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) salvo = JSON.parse(json);
    } catch {}
    return CLIENTES_COLUNAS.map(def => {
      const s = salvo.find(x => x.campo === def.campo);
      return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
    });
  }

  private salvarColunasStorage() {
    const estado = this.colunas().map(c => ({ campo: c.campo, visivel: c.visivel, largura: Math.round(c.largura) }));
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(estado));
  }

  // ── CRUD ──────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.salvarEstadoAbaAtiva();
    const novo = this.novoCliente();
    this.clienteForm.set(novo);
    this.formOriginal = this.clonarDetalhe(novo);
    this.erro.set(''); this.errosCampos.set({});
    this.isDirty.set(false); this.modoEdicao.set(false); this.pessoaEncontrada.set(null);
    this.abaAtivaId.set(null);
    this.resetAccordions();
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const c = this.selecionado();
    if (!c?.id) return;
    const jaAberta = this.abasEdicao().find(a => a.cliente.id === c.id);
    if (jaAberta) { this.ativarAba(c.id!); return; }

    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${c.id}`).subscribe({
      next: r => {
        this.carregando.set(false);
        const detalhe = this.mapDetalhe(c, r.data);
        this.salvarEstadoAbaAtiva();
        const novaAba: AbaEdicao = { cliente: { ...c }, form: this.clonarDetalhe(detalhe), isDirty: false };
        this.abasEdicao.update(tabs => [...tabs, novaAba]);
        this.abaAtivaId.set(c.id!);
        this.clienteForm.set(this.clonarDetalhe(detalhe));
        this.formOriginal = this.clonarDetalhe(detalhe);
        this.bloqueioIds.set(new Set((detalhe.bloqueios ?? []).map(b => b.tipoPagamentoId)));
        this.erro.set(''); this.errosCampos.set({});
        this.isDirty.set(false); this.modoEdicao.set(true);
        this.resetAccordions();
        this.modo.set('form');
      },
      error: () => this.carregando.set(false)
    });
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    this.abaAtivaId.set(id);
    const aba = this.abasEdicao().find(a => a.cliente.id === id);
    if (!aba) return;
    this.selecionado.set(aba.cliente);
    this.clienteForm.set(this.clonarDetalhe(aba.form));
    this.formOriginal = this.clonarDetalhe(aba.form);
    this.bloqueioIds.set(new Set((aba.form.bloqueios ?? []).map(b => b.tipoPagamentoId)));
    this.isDirty.set(aba.isDirty);
    this.errosCampos.set({}); this.erro.set('');
    this.modoEdicao.set(true); this.modo.set('form');
  }

  fecharAba(id: number) {
    const eraAtiva = this.abaAtivaId() === id && this.modo() === 'form';
    this.abasEdicao.update(tabs => tabs.filter(t => t.cliente.id !== id));
    if (eraAtiva) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) this.ativarAba(restantes[restantes.length - 1].cliente.id!);
      else { this.abaAtivaId.set(null); this.modo.set('lista'); }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (id == null) return;
    const form = this.clienteForm();
    const dirty = this.isDirty();
    this.abasEdicao.update(tabs =>
      tabs.map(t => t.cliente.id === id ? { ...t, form: this.clonarDetalhe(form), isDirty: dirty } : t)
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    if (!this.validar()) return;
    this.erro.set('');
    const f = this.clienteForm();
    this.salvando.set(true);

    const payload: any = {
      tipo: f.tipo, nome: f.nome, razaoSocial: f.razaoSocial,
      cpfCnpj: f.cpfCnpj, inscricaoEstadual: f.inscricaoEstadual,
      rg: f.rg, dataNascimento: f.dataNascimento || null,
      observacao: f.observacao, aviso: f.aviso, ativo: f.ativo, bloqueado: f.bloqueado,
      limiteCredito: f.limiteCredito, descontoGeral: f.descontoGeral,
      permiteFidelidade: f.permiteFidelidade,
      prazoPagamento: f.prazoPagamento, qtdeDias: f.qtdeDias,
      diaFechamento: f.diaFechamento, diaVencimento: f.diaVencimento, qtdeMeses: f.qtdeMeses,
      permiteVendaParcelada: f.permiteVendaParcelada, qtdeMaxParcelas: f.qtdeMaxParcelas,
      permiteVendaPrazo: f.permiteVendaPrazo, permiteVendaVista: f.permiteVendaVista,
      bloquearDescontoParcelada: f.bloquearDescontoParcelada, venderSomenteComSenha: f.venderSomenteComSenha,
      cobrarJurosAtraso: f.cobrarJurosAtraso, bloquearComissao: f.bloquearComissao,
      diasCarenciaBloqueio: f.diasCarenciaBloqueio,
      calcularJuros: f.calcularJuros,
      pedirSenhaVendaPrazo: f.pedirSenhaVendaPrazo, senhaVendaPrazo: f.senhaVendaPrazo,
      enderecos: f.enderecos, contatos: f.contatos,
      convenios: f.convenios, autorizacoes: f.autorizacoes,
      descontos: f.descontos, usosContinuos: f.usosContinuos,
      bloqueioTipoPagamentoIds: Array.from(this.bloqueioIds())
    };

    const headers = this.headerLiberacao();
    const req = this.modoEdicao()
      ? this.http.put<any>(`${this.apiUrl}/${f.id}`, payload, { headers })
      : this.http.post<any>(this.apiUrl, payload, { headers });

    req.subscribe({
      next: (r) => {
        this.salvando.set(false);
        this.carregar();
        if (!this.modoEdicao() && r.data) {
          this.http.get<any>(`${this.apiUrl}/${r.data.id}`).subscribe({
            next: det => {
              const detalhe = this.mapDetalhe(r.data, det.data);
              const novaAba: AbaEdicao = { cliente: { ...r.data }, form: this.clonarDetalhe(detalhe), isDirty: false };
              this.abasEdicao.update(tabs => [...tabs, novaAba]);
              this.abaAtivaId.set(r.data.id);
              this.selecionado.set(r.data);
              this.clienteForm.set(this.clonarDetalhe(detalhe));
              this.formOriginal = this.clonarDetalhe(detalhe);
              this.modoEdicao.set(true);
              this.isDirty.set(false); this.errosCampos.set({});
            }
          });
        } else {
          const id = this.abaAtivaId();
          if (id != null) {
            this.http.get<any>(`${this.apiUrl}/${id}`).subscribe({
              next: det => {
                const detalhe = this.mapDetalhe(f, det.data);
                this.abasEdicao.update(tabs =>
                  tabs.map(t => t.cliente.id === id
                    ? { ...t, cliente: { ...f }, form: this.clonarDetalhe(detalhe), isDirty: false } : t)
                );
                this.clienteForm.set(this.clonarDetalhe(detalhe));
                this.formOriginal = this.clonarDetalhe(detalhe);
              }
            });
          }
          this.isDirty.set(false); this.errosCampos.set({});
        }
      },
      error: (e) => {
        this.salvando.set(false);
        this.erro.set(e?.error?.message ?? 'Erro ao salvar cliente.');
      }
    });
  }

  cancelarEdicao() {
    if (this.formOriginal) this.clienteForm.set(this.clonarDetalhe(this.formOriginal));
    this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({});
    const id = this.abaAtivaId();
    if (id != null) {
      this.abasEdicao.update(tabs =>
        tabs.map(t => t.cliente.id === id ? { ...t, form: this.clonarDetalhe(this.formOriginal!), isDirty: false } : t)
      );
    }
  }

  fecharForm() {
    const id = this.abaAtivaId();
    if (this.modoEdicao() && id != null) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.cliente.id !== id));
      this.abaAtivaId.set(null);
    }
    this.modo.set('lista');
  }

  fechar() { this.salvarEstadoAbaAtiva(); this.modo.set('lista'); this.carregar(); }

  // ── Excluir ───────────────────────────────────────────────────────
  async excluir() {
    const c = this.selecionado();
    if (!c?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusao',
      `Deseja excluir o cliente ${c.nome}? O registro sera removido permanentemente. Se estiver em uso, sera apenas desativado.`,
      'Sim, excluir',
      'Nao, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${c.id}`, { headers }).subscribe({
      next: async (r) => {
        this.excluindo.set(false);
        if (c.id) this.abasEdicao.update(tabs => tabs.filter(t => t.cliente.id !== c.id));
        this.selecionado.set(null);
        this.abaAtivaId.set(null);
        this.modo.set('lista');
        this.carregar();
        const tipo = r?.resultado ?? 'excluido';
        if (tipo === 'excluido') {
          await this.modal.sucesso('Excluido', 'Registro excluido com sucesso.');
        } else {
          await this.modal.aviso('Desativado', 'O registro esta em uso e foi apenas desativado.');
        }
      },
      error: (e) => {
        this.excluindo.set(false);
        this.modal.erro('Erro', e?.error?.message ?? 'Erro ao excluir cliente.');
      }
    });
  }

  // ── Log ───────────────────────────────────────────────────────────
  abrirLog() {
    const c = this.selecionado();
    if (!c?.id) return;
    this.logDataInicio.set(this.hoje(-30));
    this.logDataFim.set(this.hoje(0));
    this.modalLog.set(true);
    this.filtrarLog();
  }

  filtrarLog() {
    const c = this.selecionado();
    if (!c?.id) return;
    this.carregandoLog.set(true);
    this.logSelecionado.set(null);
    const params = `dataInicio=${this.logDataInicio()}&dataFim=${this.logDataFim()}`;
    this.http.get<any>(`${this.apiUrl}/${c.id}/log?${params}`).subscribe({
      next: r => {
        const lista: LogEntry[] = r.data ?? [];
        this.logRegistros.set(lista);
        this.logSelecionado.set(lista.length > 0 ? lista[0] : null);
        this.carregandoLog.set(false);
      },
      error: () => this.carregandoLog.set(false)
    });
  }

  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }
  fecharLog() { this.modalLog.set(false); }

  acaoCss(acao: string): string {
    if (acao === 'CRIACAO')     return 'badge-criacao';
    if (acao === 'ALTERACAO')   return 'badge-alteracao';
    if (acao === 'EXCLUSAO')    return 'badge-exclusao';
    if (acao === 'DESATIVACAO') return 'badge-desativacao';
    return '';
  }

  // ── Formulario ────────────────────────────────────────────────────
  updateForm(campo: string, valor: any) {
    this.clienteForm.update(f => ({ ...f, [campo]: valor }));
    this.isDirty.set(true);
    if (this.errosCampos()[campo]) {
      this.errosCampos.update(e => { const n = { ...e }; delete n[campo]; return n; });
    }
  }

  onTipoChange(tipo: string) {
    this.clienteForm.update(f => ({
      ...f,
      tipo,
      cpfCnpj: '',
      razaoSocial: tipo === 'F' ? undefined : f.razaoSocial,
      inscricaoEstadual: tipo === 'F' ? undefined : f.inscricaoEstadual,
      rg: tipo === 'J' ? undefined : f.rg,
      dataNascimento: tipo === 'J' ? undefined : f.dataNascimento
    }));
    this.isDirty.set(true);
  }

  // ── Enderecos ─────────────────────────────────────────────────────
  adicionarEndereco() {
    this.clienteForm.update(f => ({
      ...f,
      enderecos: [...f.enderecos, { tipo: 'PRINCIPAL', cep: '', rua: '', numero: '', bairro: '', cidade: '', uf: '', principal: f.enderecos.length === 0 }]
    }));
    this.isDirty.set(true);
  }

  removerEndereco(idx: number) {
    this.clienteForm.update(f => ({
      ...f,
      enderecos: f.enderecos.filter((_, i) => i !== idx)
    }));
    this.isDirty.set(true);
  }

  updateEndereco(idx: number, campo: string, valor: any) {
    this.clienteForm.update(f => ({
      ...f,
      enderecos: f.enderecos.map((e, i) => i === idx ? { ...e, [campo]: valor } : e)
    }));
    this.isDirty.set(true);
  }

  onCepEnderecoInput(event: Event, idx: number) {
    const input = event.target as HTMLInputElement;
    const mascarado = this.mascaraCep(input.value);
    input.value = mascarado;
    this.updateEndereco(idx, 'cep', mascarado);
    const digits = mascarado.replace(/\D/g, '');
    if (digits.length === 8) this.buscarCepEndereco(digits, idx);
  }

  private buscarCepEndereco(cep: string, idx: number) {
    this.buscandoCep.set(true);
    this.http.get<any>(`https://viacep.com.br/ws/${cep}/json/`).subscribe({
      next: (r) => {
        this.buscandoCep.set(false);
        if (r.erro) return;
        this.clienteForm.update(f => ({
          ...f,
          enderecos: f.enderecos.map((e, i) => i === idx ? {
            ...e,
            rua: r.logradouro ?? e.rua,
            bairro: r.bairro ?? e.bairro,
            cidade: r.localidade ?? e.cidade,
            uf: r.uf ?? e.uf
          } : e)
        }));
      },
      error: () => this.buscandoCep.set(false)
    });
  }

  // ── Contatos ──────────────────────────────────────────────────────
  adicionarContato() {
    this.clienteForm.update(f => ({
      ...f,
      contatos: [...f.contatos, { tipo: 'CELULAR', valor: '', principal: f.contatos.length === 0 }]
    }));
    this.isDirty.set(true);
  }

  removerContato(idx: number) {
    this.clienteForm.update(f => ({
      ...f,
      contatos: f.contatos.filter((_, i) => i !== idx)
    }));
    this.isDirty.set(true);
  }

  updateContato(idx: number, campo: string, valor: any) {
    this.clienteForm.update(f => ({
      ...f,
      contatos: f.contatos.map((c, i) => i === idx ? { ...c, [campo]: valor } : c)
    }));
    this.isDirty.set(true);
  }

  onContatoValorInput(event: Event, idx: number) {
    const input = event.target as HTMLInputElement;
    const tipo = this.clienteForm().contatos[idx].tipo;
    let mascarado = input.value;
    if (tipo === 'TELEFONE' || tipo === 'CELULAR' || tipo === 'WHATSAPP') {
      mascarado = this.mascaraTelefone(input.value);
    }
    input.value = mascarado;
    this.updateContato(idx, 'valor', mascarado);
  }

  // ── Convenios do cliente ──────────────────────────────────────────
  adicionarConvenio() {
    const convId = this.convAddId();
    if (!convId) return;
    const ja = this.clienteForm().convenios.find(c => c.convenioId === convId);
    if (ja) return;
    const conv = this.conveniosLookup().find(c => c.id === convId);
    if (!conv) return;
    this.clienteForm.update(f => ({
      ...f,
      convenios: [...f.convenios, {
        convenioId: convId, convenioNome: conv.pessoaNome,
        matricula: this.convAddMatricula(), cartao: this.convAddCartao()
      }]
    }));
    this.convAddId.set(0); this.convAddMatricula.set(''); this.convAddCartao.set('');
    this.isDirty.set(true);
  }

  removerConvenio(idx: number) {
    this.clienteForm.update(f => ({ ...f, convenios: f.convenios.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }

  // ── Autorizacoes ──────────────────────────────────────────────────
  adicionarAutorizacao() {
    const nome = this.autAddNome().trim();
    if (!nome) return;
    this.clienteForm.update(f => ({
      ...f,
      autorizacoes: [...f.autorizacoes, { nome }]
    }));
    this.autAddNome.set('');
    this.isDirty.set(true);
  }

  removerAutorizacao(idx: number) {
    this.clienteForm.update(f => ({ ...f, autorizacoes: f.autorizacoes.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }

  // ── Descontos por Agrupador ───────────────────────────────────────
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

  adicionarDescontoAgrupador() {
    const agr = this.agrupadores().find(a => a.id === this.descAgrupadorId());
    if (!agr) return;
    const ja = this.clienteForm().descontos.find(d => d.tipoAgrupador === this.descTipoAgrupador() && d.agrupadorId === agr.id);
    if (ja) return;
    this.clienteForm.update(f => ({
      ...f,
      descontos: [...f.descontos, {
        tipoAgrupador: this.descTipoAgrupador(), agrupadorId: agr.id,
        agrupadorOuProdutoNome: agr.nome,
        descontoMinimo: this.descMinimo(), descontoMaxSemSenha: this.descSemSenha(), descontoMaxComSenha: this.descComSenha()
      }]
    }));
    this.isDirty.set(true);
  }

  // ── Descontos por Produto ─────────────────────────────────────────
  buscarProdutoDesconto() {
    const termo = this.descProdutoBusca().trim();
    if (termo.length < 2) { this.descProdutoResultados.set([]); return; }
    this.http.get<any>(`${environment.apiUrl}/produtos?busca=${encodeURIComponent(termo)}&limit=10`).subscribe({
      next: r => this.descProdutoResultados.set((r.data ?? []).map((p: any) => ({
        id: p.id, nome: p.nome, fabricanteNome: p.fabricanteNome ?? '', apresentacao: p.apresentacao ?? 0
      }))),
      error: () => this.descProdutoResultados.set([])
    });
  }

  selecionarProdutoDesconto(p: ProdutoLookup) {
    this.descProdutoSelecionado.set(p);
    this.descProdutoBusca.set(p.nome);
    this.descProdutoResultados.set([]);
  }

  adicionarDescontoProduto() {
    const p = this.descProdutoSelecionado();
    if (!p) return;
    const ja = this.clienteForm().descontos.find(d => d.produtoId === p.id);
    if (ja) return;
    this.clienteForm.update(f => ({
      ...f,
      descontos: [...f.descontos, {
        produtoId: p.id, agrupadorOuProdutoNome: p.nome,
        descontoMinimo: this.descProdMinimo(), descontoMaxSemSenha: this.descProdSemSenha(), descontoMaxComSenha: this.descProdComSenha()
      }]
    }));
    this.descProdutoSelecionado.set(null); this.descProdutoBusca.set('');
    this.descProdSemSenha.set(0); this.descProdComSenha.set(0);
    this.isDirty.set(true);
  }

  removerDesconto(idx: number) {
    this.clienteForm.update(f => ({ ...f, descontos: f.descontos.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }

  tipoAgrupadorNome(t: number | undefined): string {
    switch (t) { case 1: return 'Grupo Principal'; case 2: return 'Grupo'; case 3: return 'SubGrupo'; case 4: return 'Secao'; default: return 'Produto'; }
  }

  // ── Uso Continuo ──────────────────────────────────────────────────
  buscarProdutoUC() {
    const termo = this.ucProdutoBusca().trim();
    if (termo.length < 2) { this.ucProdutoResultados.set([]); return; }
    this.http.get<any>(`${environment.apiUrl}/produtos?busca=${encodeURIComponent(termo)}&limit=10`).subscribe({
      next: r => this.ucProdutoResultados.set((r.data ?? []).map((p: any) => ({
        id: p.id, nome: p.nome, fabricanteNome: p.fabricanteNome ?? '', apresentacao: p.apresentacao ?? 0
      }))),
      error: () => this.ucProdutoResultados.set([])
    });
  }

  selecionarProdutoUC(p: ProdutoLookup) {
    this.ucProdutoSelecionado.set(p);
    this.ucProdutoBusca.set(p.nome);
    this.ucProdutoResultados.set([]);
  }

  adicionarUsoContinuo() {
    const p = this.ucProdutoSelecionado();
    if (!p) return;
    const qtde = this.ucQtdeAoDia();
    const ultima = this.ucUltimaCompra();
    let proxima = '';
    if (ultima && p.apresentacao > 0 && qtde > 0) {
      const dt = new Date(ultima);
      dt.setDate(dt.getDate() + Math.ceil(p.apresentacao / qtde));
      proxima = dt.toISOString().slice(0, 10);
    }
    this.clienteForm.update(f => ({
      ...f,
      usosContinuos: [...f.usosContinuos, {
        produtoId: p.id, produtoNome: p.nome, fabricante: p.fabricanteNome,
        apresentacao: p.apresentacao, qtdeAoDia: qtde,
        ultimaCompra: ultima, proximaCompra: proxima,
        colaboradorNome: this.ucColaboradorNome()
      }]
    }));
    this.ucProdutoSelecionado.set(null); this.ucProdutoBusca.set('');
    this.ucQtdeAoDia.set(1); this.ucUltimaCompra.set(''); this.ucColaboradorNome.set('');
    this.isDirty.set(true);
  }

  removerUsoContinuo(idx: number) {
    this.clienteForm.update(f => ({ ...f, usosContinuos: f.usosContinuos.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }

  // ── Busca CNPJ ───────────────────────────────────────────────────
  onCpfCnpjBlur() {
    if (this.modoEdicao()) return;

    const cpfCnpj = this.clienteForm().cpfCnpj;
    const digits = cpfCnpj.replace(/\D/g, '');
    if (digits.length !== 11 && digits.length !== 14) {
      this.pessoaEncontrada.set(null);
      return;
    }

    this.http.get<any>(`${environment.apiUrl}/pessoas/buscar-cpfcnpj?valor=${cpfCnpj}`).subscribe({
      next: r => {
        const p = r?.data;
        if (!p) {
          this.pessoaEncontrada.set(null);
          return;
        }

        if (p.temCliente) {
          this.erro.set('Este CPF/CNPJ ja possui um cliente cadastrado.');
          this.pessoaEncontrada.set(null);
          return;
        }

        this.pessoaEncontrada.set(p);
        this.clienteForm.update(f => ({
          ...f,
          nome: p.nome || f.nome,
          razaoSocial: p.razaoSocial || f.razaoSocial,
          inscricaoEstadual: p.inscricaoEstadual || f.inscricaoEstadual,
          rg: p.rg || f.rg,
          dataNascimento: p.dataNascimento ? p.dataNascimento.slice(0, 10) : f.dataNascimento,
          observacao: p.observacao || f.observacao
        }));

        const form = this.clienteForm();
        const noEnderecos = form.enderecos.length <= 1 && !form.enderecos[0]?.cep;
        if (p.enderecos?.length > 0 && noEnderecos) {
          this.clienteForm.update(f => ({
            ...f,
            enderecos: p.enderecos.map((e: any) => ({
              id: e.id, tipo: e.tipo, cep: e.cep, rua: e.rua, numero: e.numero,
              complemento: e.complemento, bairro: e.bairro, cidade: e.cidade, uf: e.uf, principal: e.principal
            }))
          }));
        }

        if (p.contatos?.length > 0 && form.contatos.length === 0) {
          this.clienteForm.update(f => ({
            ...f,
            contatos: p.contatos.map((c: any) => ({
              id: c.id, tipo: c.tipo, valor: c.valor, descricao: c.descricao, principal: c.principal
            }))
          }));
        }
      },
      error: () => this.pessoaEncontrada.set(null)
    });
  }

  onCpfCnpjInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const tipo = this.clienteForm().tipo;
    const mascarado = tipo === 'F' ? this.mascaraCpf(input.value) : this.mascaraCnpj(input.value);
    input.value = mascarado;
    this.updateForm('cpfCnpj', mascarado);

    if (tipo === 'J') {
      const digits = mascarado.replace(/\D/g, '');
      if (digits.length === 14) this.buscarCnpj(digits);
    }
  }

  private buscarCnpj(cnpj: string) {
    this.buscandoCnpj.set(true);
    this.http.get<any>(`https://brasilapi.com.br/api/cnpj/v1/${cnpj}`).subscribe({
      next: r => {
        this.buscandoCnpj.set(false);
        if (r.razao_social) {
          this.aplicarDadosCnpj(r.razao_social, r.nome_fantasia, r.cep, r.logradouro,
            r.numero, r.complemento, r.bairro, r.municipio, r.uf);
        }
      },
      error: () => {
        this.http.get<any>(`https://receitaws.com.br/v1/cnpj/${cnpj}`).subscribe({
          next: r => {
            this.buscandoCnpj.set(false);
            if (r.nome && r.status !== 'ERROR') {
              this.aplicarDadosCnpj(r.nome, r.fantasia, r.cep, r.logradouro,
                r.numero, r.complemento, r.bairro, r.municipio, r.uf);
            }
          },
          error: () => this.buscandoCnpj.set(false)
        });
      }
    });
  }

  private aplicarDadosCnpj(razaoSocial: string, nomeFantasia: string, cep: string,
    rua: string, numero: string, complemento: string, bairro: string, cidade: string, uf: string) {
    this.clienteForm.update(f => ({
      ...f,
      razaoSocial: razaoSocial?.toUpperCase() || f.razaoSocial,
      nome: nomeFantasia?.toUpperCase() || razaoSocial?.toUpperCase() || f.nome,
    }));
    this.isDirty.set(true);

    if (cep && this.clienteForm().enderecos.length > 0) {
      const end = this.clienteForm().enderecos[0];
      if (!end.cep || !end.rua) {
        this.clienteForm.update(f => ({
          ...f,
          enderecos: f.enderecos.map((e, i) => i === 0 ? {
            ...e,
            cep: this.mascaraCep(cep || ''),
            rua: rua?.toUpperCase() || e.rua,
            numero: numero || e.numero,
            complemento: complemento?.toUpperCase() || e.complemento,
            bairro: bairro?.toUpperCase() || e.bairro,
            cidade: cidade?.toUpperCase() || e.cidade,
            uf: uf?.toUpperCase() || e.uf,
          } : e)
        }));
      }
    }
  }

  mascaraCpf(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 11);
    if (d.length <= 3)  return d;
    if (d.length <= 6)  return `${d.slice(0,3)}.${d.slice(3)}`;
    if (d.length <= 9)  return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6)}`;
    return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6,9)}-${d.slice(9)}`;
  }

  mascaraCnpj(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 14);
    if (d.length <= 2)  return d;
    if (d.length <= 5)  return `${d.slice(0,2)}.${d.slice(2)}`;
    if (d.length <= 8)  return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5)}`;
    if (d.length <= 12) return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8)}`;
    return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8,12)}-${d.slice(12)}`;
  }

  mascaraTelefone(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 11);
    if (d.length <= 2)  return d.length ? `(${d}` : '';
    if (d.length <= 6)  return `(${d.slice(0,2)}) ${d.slice(2)}`;
    if (d.length <= 10) return `(${d.slice(0,2)}) ${d.slice(2,6)}-${d.slice(6)}`;
    return `(${d.slice(0,2)}) ${d.slice(2,7)}-${d.slice(7)}`;
  }

  mascaraCep(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 8);
    if (d.length <= 5) return d;
    return `${d.slice(0,5)}-${d.slice(5)}`;
  }

  // ── Validacao ─────────────────────────────────────────────────────
  private validar(): boolean {
    const f = this.clienteForm();
    const erros: Record<string, string> = {};

    if (!f.nome?.trim()) erros['nome'] = 'Obrigatorio';
    if (!f.cpfCnpj?.trim()) erros['cpfCnpj'] = 'Obrigatorio';
    if (f.tipo === 'J' && !f.razaoSocial?.trim()) erros['razaoSocial'] = 'Obrigatorio';

    for (let i = 0; i < f.enderecos.length; i++) {
      const e = f.enderecos[i];
      const temAlgo = e.cep || e.rua || e.numero || e.bairro || e.cidade || e.uf;
      if (temAlgo) {
        if (!e.cep?.trim())    erros[`end_cep_${i}`]    = 'Obrigatorio';
        if (!e.rua?.trim())    erros[`end_rua_${i}`]    = 'Obrigatorio';
        if (!e.numero?.trim()) erros[`end_numero_${i}`] = 'Obrigatorio';
        if (!e.bairro?.trim()) erros[`end_bairro_${i}`] = 'Obrigatorio';
        if (!e.cidade?.trim()) erros[`end_cidade_${i}`] = 'Obrigatorio';
        if (!e.uf?.trim())     erros[`end_uf_${i}`]     = 'Obrigatorio';
      }
    }

    this.errosCampos.set(erros);
    if (Object.keys(erros).length > 0) {
      this.erro.set('Preencha todos os campos obrigatorios.');
      return false;
    }
    this.erro.set('');
    return true;
  }

  erroCampo(campo: string): string { return this.errosCampos()[campo] ?? ''; }
  hasEnderecoErrors(): boolean { return Object.keys(this.errosCampos()).some(k => k.startsWith('end_')); }

  // ── Helpers ───────────────────────────────────────────────────────
  private resetAccordions() {
    this.accEnderecos.set(false); this.accContatos.set(false);
    this.accConvenios.set(false); this.accGeral.set(false);
    this.accAutorizacoes.set(false); this.accDescontos.set(false);
    this.accUsoContinuo.set(false);
  }

  private mapDetalhe(base: any, data: any): ClienteDetalhe {
    return {
      ...base,
      enderecos: data.enderecos ?? [],
      contatos: data.contatos ?? [],
      convenios: data.convenios ?? [],
      autorizacoes: data.autorizacoes ?? [],
      descontos: data.descontos ?? [],
      usosContinuos: data.usosContinuos ?? [],
      observacao: data.observacao,
      aviso: data.aviso,
      dataNascimento: data.dataNascimento,
      razaoSocial: data.razaoSocial,
      inscricaoEstadual: data.inscricaoEstadual,
      rg: data.rg,
      bloqueado: data.bloqueado ?? false,
      limiteCredito: data.limiteCredito ?? 0,
      descontoGeral: data.descontoGeral ?? 0,
      permiteFidelidade: data.permiteFidelidade ?? true,
      prazoPagamento: data.prazoPagamento ?? 1,
      qtdeDias: data.qtdeDias ?? 30,
      diaFechamento: data.diaFechamento ?? 1,
      diaVencimento: data.diaVencimento ?? 10,
      qtdeMeses: data.qtdeMeses ?? 1,
      permiteVendaParcelada: data.permiteVendaParcelada ?? true,
      qtdeMaxParcelas: data.qtdeMaxParcelas ?? 3,
      permiteVendaPrazo: data.permiteVendaPrazo ?? true,
      permiteVendaVista: data.permiteVendaVista ?? true,
      bloquearDescontoParcelada: data.bloquearDescontoParcelada ?? false,
      venderSomenteComSenha: data.venderSomenteComSenha ?? false,
      cobrarJurosAtraso: data.cobrarJurosAtraso ?? true,
      bloquearComissao: data.bloquearComissao ?? false,
      diasCarenciaBloqueio: data.diasCarenciaBloqueio ?? 0,
      calcularJuros: data.calcularJuros ?? false,
      pedirSenhaVendaPrazo: data.pedirSenhaVendaPrazo ?? false,
      senhaVendaPrazo: data.senhaVendaPrazo ?? '',
      bloqueios: data.bloqueios ?? []
    };
  }

  private novoCliente(): ClienteDetalhe {
    return {
      tipo: 'F', nome: '', razaoSocial: '', cpfCnpj: '', inscricaoEstadual: '',
      rg: '', dataNascimento: '', observacao: '', aviso: '',
      bloqueado: false, ativo: true,
      limiteCredito: 0, descontoGeral: 0, permiteFidelidade: true,
      prazoPagamento: 2, qtdeDias: 30, diaFechamento: 1, diaVencimento: 10, qtdeMeses: 1,
      permiteVendaParcelada: true, qtdeMaxParcelas: 3,
      permiteVendaPrazo: true, permiteVendaVista: true,
      bloquearDescontoParcelada: false, venderSomenteComSenha: false,
      cobrarJurosAtraso: true, bloquearComissao: false,
      diasCarenciaBloqueio: 0, calcularJuros: false,
      pedirSenhaVendaPrazo: false, senhaVendaPrazo: '',
      enderecos: [{ tipo: 'PRINCIPAL', cep: '', rua: '', numero: '', bairro: '', cidade: '', uf: '', principal: true }],
      contatos: [],
      convenios: [],
      autorizacoes: [],
      descontos: [],
      usosContinuos: [],
      bloqueios: []
    };
  }

  private clonarDetalhe(d: ClienteDetalhe): ClienteDetalhe {
    return {
      ...d,
      enderecos: d.enderecos.map(e => ({ ...e })),
      contatos: d.contatos.map(c => ({ ...c })),
      convenios: d.convenios.map(c => ({ ...c })),
      autorizacoes: d.autorizacoes.map(a => ({ ...a })),
      descontos: d.descontos.map(d => ({ ...d })),
      usosContinuos: d.usosContinuos.map(u => ({ ...u })),
      bloqueios: (d.bloqueios ?? []).map(b => ({ ...b }))
    };
  }

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
