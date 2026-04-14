import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { CaixaVendaComponent } from '../caixa-venda/caixa-venda.component';

interface CaixaInfo {
  id: number;
  codigo?: string;
  colaboradorNome: string;
  dataAbertura: string;
  valorAbertura: number;
  status: number;
}

interface VendaPendente {
  id: number;
  codigo?: string;
  nrCesta?: string;
  colaboradorNome?: string;
  clienteNome?: string;
  tipoPagamentoNome?: string;
  totalLiquido: number;
  totalItens: number;
  criadoEm: string;
}

interface ColunaDef {
  campo: string; label: string; largura: number; minLargura: number; padrao: boolean;
}
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const PEND_COLUNAS: ColunaDef[] = [
  { campo: 'codigo',            label: 'CÓDIGO',     largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'nrCesta',           label: 'CESTA',      largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'colaboradorNome',   label: 'VENDEDOR',   largura: 140, minLargura: 80,  padrao: true },
  { campo: 'clienteNome',       label: 'CLIENTE',    largura: 200, minLargura: 100, padrao: true },
  { campo: 'totalItens',        label: 'ITENS',      largura: 60,  minLargura: 50,  padrao: true },
  { campo: 'totalLiquido',      label: 'TOTAL',      largura: 100, minLargura: 70,  padrao: true },
  { campo: 'criadoEm',          label: 'DATA/HORA',  largura: 140, minLargura: 100, padrao: true },
  { campo: 'tipoPagamentoNome', label: 'PAGAMENTO',  largura: 110, minLargura: 80,  padrao: true },
];

interface VendaRealizada {
  id: number;
  codigo?: string;
  clienteNome?: string;
  colaboradorNome?: string;
  tipoPagamentoNome?: string;
  totalBruto: number;
  totalDesconto: number;
  totalLiquido: number;
  totalItens: number;
  criadoEm: string;
  dataEmissaoCupom?: string;
  dataFinalizacao?: string;
  status: number;
  convenioNome?: string;
}

interface VendaRealizadaItem {
  produtoCodigo: string;
  produtoNome: string;
  fabricante?: string;
  quantidade: number;
  precoVenda: number;
  valorDesconto: number;
  total: number;
}

const VR_COLUNAS: ColunaDef[] = [
  { campo: 'codigo',            label: 'CÓDIGO',       largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'dataEmissaoCupom',  label: 'DATA CUPOM',   largura: 140, minLargura: 100, padrao: true },
  { campo: 'colaboradorNome',   label: 'VENDEDOR',     largura: 140, minLargura: 80,  padrao: true },
  { campo: 'clienteNome',       label: 'CLIENTE',      largura: 180, minLargura: 100, padrao: true },
  { campo: 'convenioNome',      label: 'CONVÊNIO',     largura: 120, minLargura: 80,  padrao: false },
  { campo: 'tipoPagamentoNome', label: 'F. PGTO',      largura: 110, minLargura: 80,  padrao: true },
  { campo: 'totalBruto',        label: 'TOTAL BRUTO',  largura: 100, minLargura: 70,  padrao: true },
  { campo: 'totalDesconto',     label: 'TOTAL DESC',   largura: 100, minLargura: 70,  padrao: true },
  { campo: 'totalLiquido',      label: 'TOTAL LÍQ.',   largura: 100, minLargura: 70,  padrao: true },
];

const VR_ITENS_COLUNAS: ColunaDef[] = [
  { campo: 'produtoCodigo', label: 'CÓDIGO',      largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'produtoNome',   label: 'PRODUTO',     largura: 220, minLargura: 120, padrao: true },
  { campo: 'fabricante',    label: 'FABRICANTE',  largura: 140, minLargura: 80,  padrao: true },
  { campo: 'quantidade',    label: 'QTDE',        largura: 60,  minLargura: 40,  padrao: true },
  { campo: 'totalBruto',    label: 'TOTAL BRUTO', largura: 100, minLargura: 70,  padrao: true },
  { campo: 'totalDesconto', label: 'TOTAL DESC',  largura: 100, minLargura: 70,  padrao: true },
  { campo: 'total',         label: 'TOTAL LÍQ.',  largura: 100, minLargura: 70,  padrao: true },
];

type PainelAtivo = 'vender' | 'pendentes' | 'entregas' | 'valores' | 'vendas' | 'recargas';

interface CaixaMovimentoItem {
  id: number;
  codigo?: string;
  tipo: number;
  tipoDescricao: string;
  dataMovimento: string;
  valor: number;
  tipoPagamentoId?: number;
  tipoPagamentoNome?: string;
  modalidadePagamento?: number;
  descricao: string;
  observacao?: string;
  statusConferencia: number;
  statusConferenciaDescricao: string;
  dataConferencia?: string;
  usuarioNome?: string;
}

interface TipoPagamentoOpcao {
  id: number;
  nome: string;
  modalidade: number;
}

interface ContaReceberPendente {
  id: number;
  codigo?: string;
  descricao: string;
  clienteNome?: string;
  valor: number;
  valorLiquido: number;
  valorRecebido: number;
  dataVencimento: string;
  tipoPagamentoNome?: string;
}

interface PessoaLookup { id: number; nome: string; cpfCnpj?: string; }
interface PlanoContaLookup { id: number; descricao: string; codigoHierarquico: string; }

const VC_COLUNAS: ColunaDef[] = [
  { campo: 'codigo',       label: 'CÓDIGO',     largura: 120, minLargura: 90,  padrao: true },
  { campo: 'dataMovimento',label: 'DATA/HORA',  largura: 140, minLargura: 100, padrao: true },
  { campo: 'tipoDescricao',label: 'TIPO',       largura: 110, minLargura: 80,  padrao: true },
  { campo: 'tipoPagamentoNome', label: 'FORMA', largura: 110, minLargura: 80,  padrao: true },
  { campo: 'descricao',    label: 'DESCRIÇÃO',  largura: 240, minLargura: 120, padrao: true },
  { campo: 'valor',        label: 'VALOR',      largura: 100, minLargura: 70,  padrao: true },
  { campo: 'statusConferenciaDescricao', label: 'CONF.', largura: 90, minLargura: 60, padrao: true },
];

interface SidebarItem {
  id: PainelAtivo;
  label: string;
  sigla: string;
  icon: string;
  cor: string;
}

@Component({
  selector: 'app-caixa',
  standalone: true,
  imports: [CommonModule, FormsModule, CaixaVendaComponent],
  templateUrl: './caixa.component.html',
  styleUrl: './caixa.component.scss'
})
export class CaixaComponent implements OnInit, OnDestroy {
  private readonly apiUrl = environment.apiUrl;

  // ── Caixa state ────────────────────────────────────────────────
  caixaAberto = signal<CaixaInfo | null>(null);
  caixaAbertoLoading = signal(false);

  // ── Sidebar ────────────────────────────────────────────────────
  sidebarExpandido = signal(false);
  painelAtivo = signal<PainelAtivo>('vender');

  sidebarItems: SidebarItem[] = [
    { id: 'pendentes', label: 'Lançamentos Pendentes', sigla: 'LP', icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z', cor: '#00897B' },
    { id: 'entregas',  label: 'Gerenciador de Entregas', sigla: 'GE', icon: 'M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6-1a1 1 0 001 1h1M5 17a2 2 0 104 0m-4 0a2 2 0 114 0m6 0a2 2 0 104 0m-4 0a2 2 0 114 0', cor: '#5C6BC0' },
    { id: 'valores',   label: 'Valores em Caixa', sigla: 'VC', icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z', cor: '#43A047' },
    { id: 'vendas',    label: 'Vendas Realizadas', sigla: 'VR', icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4', cor: '#FB8C00' },
    { id: 'vender',    label: 'Vender e Checkout', sigla: 'VE', icon: 'M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 100 4 2 2 0 000-4z', cor: '#1E88E5' },
    { id: 'recargas',  label: 'Consultar Recargas', sigla: 'CR', icon: 'M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z', cor: '#8E24AA' },
  ];

  // ── Modal abertura ──────────────────────────────────────────────
  modalAbertura = signal(false);
  filiais = signal<{ id: number; nome: string }[]>([]);
  filialId = signal(0);
  colaboradorNomeLogado = signal('');
  dataHoraAbertura = signal('');
  valorAberturaInput = signal('0,00');

  // ── Pendentes (grid padrão ERP) ─────────────────────────────────
  private readonly STORAGE_PEND = 'zulex_colunas_caixa_pendentes';
  pendentes = signal<VendaPendente[]>([]);
  pendentesLoading = signal(false);
  cestaBusca = signal('');
  pendColunas = signal<ColunaEstado[]>(this.carregarColunasPend());
  pendColunasVisiveis = computed(() => this.pendColunas().filter(c => c.visivel));
  pendPainelColunas = signal(false);
  pendSortColuna = signal('');
  pendSortDir = signal<'asc' | 'desc'>('asc');
  private pendDragIdx: number | null = null;
  private pendResizeState: { campo: string; startX: number; startW: number } | null = null;

  // ── Vendas realizadas (grid padrão ERP master-detail) ────────
  private readonly STORAGE_VR = 'zulex_colunas_caixa_vendas';
  private readonly STORAGE_VR_ITENS = 'zulex_colunas_caixa_vendas_itens';
  caixasDisponiveis = signal<CaixaInfo[]>([]);
  caixaSelecionadoId = signal<number | null>(null);
  vendasRealizadas = signal<VendaRealizada[]>([]);
  vendasLoading = signal(false);
  vrColunas = signal<ColunaEstado[]>(this.carregarColunasVr());
  vrColunasVisiveis = computed(() => this.vrColunas().filter(c => c.visivel));
  vrPainelColunas = signal(false);
  vrSortColuna = signal('');
  vrSortDir = signal<'asc' | 'desc'>('asc');
  private vrDragIdx: number | null = null;
  private vrResizeState: { campo: string; startX: number; startW: number } | null = null;
  vrExpandido = signal<number | null>(null);
  vrItens = signal<VendaRealizadaItem[]>([]);
  vrItensLoading = signal(false);
  vrItensColunas = signal<ColunaEstado[]>(this.carregarColunasVrItens());
  vrItensColunasVisiveis = computed(() => this.vrItensColunas().filter(c => c.visivel));
  vrItensPainelColunas = signal(false);
  vrItensSortColuna = signal('');
  vrItensSortDir = signal<'asc' | 'desc'>('asc');
  private vrItensDragIdx: number | null = null;
  private vrItensResizeState: { campo: string; startX: number; startW: number } | null = null;

  // ══ Valores em Caixa (movimentos + bipagem) ══════════════════════
  movimentos = signal<CaixaMovimentoItem[]>([]);
  movimentosLoading = signal(false);
  scanInput = signal('');
  vcColunas = signal<ColunaEstado[]>(VC_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
  vcColunasVisiveis = computed(() => this.vcColunas().filter(c => c.visivel));

  // ══ Tipos de pagamento lookup ════════════════════════════════════
  tiposPagamento = signal<TipoPagamentoOpcao[]>([]);

  // ══ Modais ═══════════════════════════════════════════════════════
  modalSangria = signal(false);
  sangriaValor = signal('');
  sangriaObs = signal('');

  modalSuprimento = signal(false);
  suprimentoValor = signal('');
  suprimentoObs = signal('');

  modalRecebimento = signal(false);
  recebClienteBusca = signal('');
  recebClienteResultados = signal<PessoaLookup[]>([]);
  recebClienteSelecionado = signal<PessoaLookup | null>(null);
  recebContasPendentes = signal<ContaReceberPendente[]>([]);
  recebContaSelecionada = signal<ContaReceberPendente | null>(null);
  recebValor = signal('');
  recebTipoPagamentoId = signal<number | null>(null);
  private recebTimer: any = null;

  modalPagamento = signal(false);
  pagPessoaBusca = signal('');
  pagPessoaResultados = signal<PessoaLookup[]>([]);
  pagPessoaSelecionada = signal<PessoaLookup | null>(null);
  pagPlanoBusca = signal('');
  pagPlanoResultados = signal<PlanoContaLookup[]>([]);
  pagPlanoSelecionado = signal<PlanoContaLookup | null>(null);
  pagValor = signal('');
  pagDescricao = signal('');
  pagObs = signal('');
  pagTipoPagamentoId = signal<number | null>(null);
  private pagTimer: any = null;

  modalFechamentoSimples = signal(false);
  fechamentoDeclarados = signal<{ tipoPagamentoId: number; nome: string; valor: string }[]>([]);
  fechamentoObs = signal('');

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.carregarFiliais();
    this.verificarCaixaAberto();
    this.carregarCaixasDisponiveis();
    this.carregarTiposPagamento();
  }

  private carregarTiposPagamento() {
    this.http.get<any>(`${this.apiUrl}/tipospagamento`).subscribe({
      next: r => {
        const tipos: TipoPagamentoOpcao[] = (r.data ?? []).filter((t: any) => t.ativo)
          .map((t: any) => ({ id: t.id, nome: t.nome, modalidade: t.modalidade }));
        this.tiposPagamento.set(tipos);
      }
    });
  }

  private carregarFiliais() {
    const usuario = this.auth.usuarioLogado();
    this.filialId.set(parseInt(usuario?.filialId || '1', 10));
    this.colaboradorNomeLogado.set(usuario?.nome ?? 'Usuário');
    this.http.get<any>(`${this.apiUrl}/filiais`).subscribe({
      next: r => this.filiais.set((r.data ?? []).map((f: any) => ({ id: f.id, nome: f.nomeFilial ?? f.nomeFantasia ?? `Filial ${f.id}` }))),
      error: () => {
        const id = this.filialId();
        this.filiais.set([{ id, nome: usuario?.nomeFilial ?? `Filial ${id}` }]);
      }
    });
  }

  ngOnDestroy() {}

  // ── Caixa: abertura/fechamento ─────────────────────────────────
  verificarCaixaAberto() {
    this.http.get<any>(`${this.apiUrl}/caixas/aberto`).subscribe({
      next: r => {
        if (r.data) this.caixaAberto.set(r.data);
      },
      error: () => {}
    });
  }

  abrirCaixa() {
    const agora = new Date();
    this.dataHoraAbertura.set(
      agora.toLocaleDateString('pt-BR') + ' ' + agora.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
    );
    this.valorAberturaInput.set('0,00');
    this.modalAbertura.set(true);
  }

  confirmarAberturaCaixa() {
    const valor = parseFloat(this.valorAberturaInput().replace(',', '.')) || 0;
    this.modalAbertura.set(false);
    this.caixaAbertoLoading.set(true);
    this.http.post<any>(`${this.apiUrl}/caixas/abrir`, { valorAbertura: valor }).subscribe({
      next: r => {
        this.caixaAberto.set(r.data);
        this.caixaAbertoLoading.set(false);
        // Imprime canhoto da abertura
        this.imprimirCanhotoAbertura(r.data.id);
        this.modal.sucesso('Caixa Aberto', `Caixa aberto com fundo de R$ ${valor.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}.`);
      },
      error: (err: any) => {
        this.caixaAbertoLoading.set(false);
        this.modal.erro('Erro', err?.error?.message || 'Erro ao abrir caixa.');
      }
    });
  }

  private imprimirCanhotoAbertura(caixaId: number) {
    // Busca o movimento de abertura do caixa recém-aberto
    this.http.get<any>(`${this.apiUrl}/caixamovimentos/caixa/${caixaId}`).subscribe({
      next: r => {
        const movs = r.data ?? [];
        const abertura = movs.find((m: any) => m.tipo === 1);
        if (abertura) this.imprimirCanhoto(abertura.id);
      }
    });
  }

  cancelarAbertura() {
    this.modalAbertura.set(false);
  }

  async fecharCaixa() {
    const caixa = this.caixaAberto();
    if (!caixa) return;
    // Se o caixa for modo "conferencia_simples", abre modal de declaração
    const modelo = (caixa as any).modeloFechamento || 'confirmacao_posse';
    if (modelo === 'conferencia_simples') {
      this.prepararFechamento();
      this.modalFechamentoSimples.set(true);
      return;
    }
    // Confirmação de posse: fecha direto
    const resultado = await this.modal.confirmar('Fechar Caixa', 'Deseja fechar o caixa? Lembre-se de fazer uma sangria final com todo o dinheiro restante antes de fechar.', 'Fechar', 'Cancelar');
    if (!resultado.confirmado) return;
    this.http.post<any>(`${this.apiUrl}/caixas/${caixa.id}/fechar`, {}).subscribe({
      next: () => {
        this.caixaAberto.set(null);
        this.modal.sucesso('Caixa Fechado', 'O caixa foi fechado. Passe para a conferência em Financeiro > Conferência de Caixa.');
      },
      error: () => this.modal.erro('Erro', 'Erro ao fechar caixa.')
    });
  }

  // ── Sidebar ────────────────────────────────────────────────────
  selecionarPainel(id: PainelAtivo) {
    this.painelAtivo.set(id);
    if (id === 'pendentes') this.carregarPendentes();
    if (id === 'vendas') this.carregarVendasRealizadas();
    if (id === 'valores') this.carregarMovimentos();
  }

  // ── Valores em Caixa ───────────────────────────────────────────
  carregarMovimentos() {
    const caixa = this.caixaAberto();
    if (!caixa) return;
    this.movimentosLoading.set(true);
    this.http.get<any>(`${this.apiUrl}/caixamovimentos/caixa/${caixa.id}`).subscribe({
      next: r => { this.movimentos.set(r.data ?? []); this.movimentosLoading.set(false); },
      error: () => this.movimentosLoading.set(false)
    });
  }

  bipar() {
    const codigo = this.scanInput().trim();
    if (!codigo) return;
    this.http.post<any>(`${this.apiUrl}/caixamovimentos/bipar`, { codigo }).subscribe({
      next: _ => {
        this.scanInput.set('');
        this.carregarMovimentos();
      },
      error: (err: any) => {
        this.scanInput.set('');
        this.modal.aviso('Canhoto', err?.error?.message || 'Erro ao bipar canhoto.');
      }
    });
  }

  corStatusMov(status: number): string {
    if (status === 3) return '#27ae60'; // Conferido
    if (status === 2) return '#f39c12'; // PendenteConferente
    return '#e74c3c'; // Pendente
  }

  getCellMov(mov: CaixaMovimentoItem, campo: string): string {
    const v = (mov as any)[campo];
    if (campo === 'valor') return (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    if (campo === 'dataMovimento') return new Date(v).toLocaleString('pt-BR');
    if (campo === 'tipoDescricao') {
      const map: Record<number, string> = { 1: 'Abertura', 2: 'Fechamento', 3: 'Venda', 4: 'Sangria', 5: 'Suprimento', 6: 'Recebimento', 7: 'Pagamento' };
      return map[mov.tipo] ?? '';
    }
    return v?.toString() ?? '';
  }

  imprimirCanhoto(movId: number) {
    // Busca o HTML via HTTP autenticado e abre janela com o conteúdo
    this.http.get(`${this.apiUrl}/caixamovimentos/${movId}/canhoto`, { responseType: 'text' }).subscribe({
      next: (html: string) => {
        const win = window.open('', '_blank', 'width=400,height=700');
        if (win) {
          win.document.open();
          win.document.write(html);
          win.document.close();
        }
      },
      error: (err: any) => this.modal.erro('Canhoto', err?.error?.message || 'Erro ao gerar canhoto.')
    });
  }

  // ── Pendentes ──────────────────────────────────────────────────
  carregarPendentes() {
    this.pendentesLoading.set(true);
    this.http.get<any>(`${this.apiUrl}/vendas`, { params: { status: 'aberta' } }).subscribe({
      next: r => { this.pendentes.set(r.data ?? []); this.pendentesLoading.set(false); },
      error: () => this.pendentesLoading.set(false)
    });
  }

  buscarPorCesta() {
    const nr = this.cestaBusca().trim();
    if (!nr) return;
    this.http.get<any>(`${this.apiUrl}/vendas`, { params: { nrCesta: nr } }).subscribe({
      next: r => {
        const lista = r.data ?? [];
        if (lista.length === 0) {
          this.modal.aviso('Cesta não encontrada', `Nenhuma pré-venda encontrada com o Nº Cesta "${nr}".`);
        } else {
          this.pendentes.set(lista);
        }
      }
    });
  }

  // ── Vendas realizadas ──────────────────────────────────────────
  carregarCaixasDisponiveis() {
    this.http.get<any>(`${this.apiUrl}/caixas`).subscribe({
      next: r => {
        const lista: CaixaInfo[] = r.data ?? [];
        this.caixasDisponiveis.set(lista);
        // Default: caixa atual aberto, senão o primeiro da lista
        const atual = this.caixaAberto();
        if (atual && lista.some(c => c.id === atual.id)) {
          this.caixaSelecionadoId.set(atual.id);
        } else if (lista.length > 0) {
          this.caixaSelecionadoId.set(lista[0].id);
        }
        this.carregarVendasRealizadas();
      }
    });
  }

  trocarCaixa(caixaId: number) {
    this.caixaSelecionadoId.set(caixaId);
    this.vrExpandido.set(null);
    this.carregarVendasRealizadas();
  }

  carregarVendasRealizadas() {
    const cxId = this.caixaSelecionadoId();
    console.log('[VR] carregarVendasRealizadas caixaId=', cxId);
    if (!cxId) return;
    this.vendasLoading.set(true);
    this.http.get<any>(`${this.apiUrl}/vendas`, { params: { caixaId: cxId.toString(), status: 'finalizada' } }).subscribe({
      next: r => {
        console.log('[VR] vendas recebidas:', r.data?.length ?? 0, r.data);
        this.vendasRealizadas.set(r.data ?? []);
        this.vendasLoading.set(false);
      },
      error: (err) => { console.error('[VR] erro:', err); this.vendasLoading.set(false); }
    });
  }

  pendentesModal() { this.modal.aviso('Em Desenvolvimento', 'As vendas pendentes serão implementadas em breve no caixa.'); }

  enviarParaCheckout(venda: VendaPendente) {
    // Trocar para o painel Vender e Checkout e emitir evento com a venda
    this.painelAtivo.set('vender');
    // Usar setTimeout para garantir que o componente caixa-venda já renderizou
    setTimeout(() => {
      const evt = new CustomEvent('carregar-prevenda', { detail: { vendaId: venda.id, nrCesta: venda.nrCesta } });
      window.dispatchEvent(evt);
    }, 200);
  }

  // ══ Sangria ═══════════════════════════════════════════════════
  sangria() {
    if (!this.caixaAberto()) { this.modal.aviso('Caixa fechado', 'Abra o caixa primeiro.'); return; }
    this.sangriaValor.set('');
    this.sangriaObs.set('');
    this.modalSangria.set(true);
  }
  cancelarSangria() { this.modalSangria.set(false); }
  confirmarSangria() {
    const valor = parseFloat(this.sangriaValor().replace(',', '.')) || 0;
    if (valor <= 0) { this.modal.aviso('Valor inválido', 'Informe um valor maior que zero.'); return; }
    const caixaId = this.caixaAberto()!.id;
    this.http.post<any>(`${this.apiUrl}/caixamovimentos/sangria`, {
      caixaId, valor, observacao: this.sangriaObs() || null
    }).subscribe({
      next: r => {
        this.modalSangria.set(false);
        this.imprimirCanhoto(r.data.id);
        this.carregarMovimentos();
        this.modal.sucesso('Sangria', 'Sangria registrada. Entregue o dinheiro ao conferente.');
      },
      error: (err: any) => this.modal.erro('Erro', err?.error?.message || 'Erro ao registrar sangria.')
    });
  }

  // ══ Suprimento ════════════════════════════════════════════════
  suprimento() {
    if (!this.caixaAberto()) { this.modal.aviso('Caixa fechado', 'Abra o caixa primeiro.'); return; }
    this.suprimentoValor.set('');
    this.suprimentoObs.set('');
    this.modalSuprimento.set(true);
  }
  cancelarSuprimento() { this.modalSuprimento.set(false); }
  confirmarSuprimento() {
    const valor = parseFloat(this.suprimentoValor().replace(',', '.')) || 0;
    if (valor <= 0) { this.modal.aviso('Valor inválido', 'Informe um valor maior que zero.'); return; }
    const caixaId = this.caixaAberto()!.id;
    this.http.post<any>(`${this.apiUrl}/caixamovimentos/suprimento`, {
      caixaId, valor, observacao: this.suprimentoObs() || null
    }).subscribe({
      next: r => {
        this.modalSuprimento.set(false);
        this.imprimirCanhoto(r.data.id);
        this.carregarMovimentos();
        this.modal.sucesso('Suprimento', 'Suprimento registrado. Retirada do cofre efetivada.');
      },
      error: (err: any) => this.modal.erro('Erro', err?.error?.message || 'Erro ao registrar suprimento.')
    });
  }

  // ══ Recebimento (conta a receber) ═════════════════════════════
  recebimento() {
    if (!this.caixaAberto()) { this.modal.aviso('Caixa fechado', 'Abra o caixa primeiro.'); return; }
    this.recebClienteBusca.set('');
    this.recebClienteResultados.set([]);
    this.recebClienteSelecionado.set(null);
    this.recebContasPendentes.set([]);
    this.recebContaSelecionada.set(null);
    this.recebValor.set('');
    this.recebTipoPagamentoId.set(null);
    this.modalRecebimento.set(true);
  }
  cancelarRecebimento() { this.modalRecebimento.set(false); }

  onRecebClienteInput(valor: string) {
    this.recebClienteBusca.set(valor);
    if (this.recebTimer) clearTimeout(this.recebTimer);
    if (valor.trim().length < 2) { this.recebClienteResultados.set([]); return; }
    this.recebTimer = setTimeout(() => {
      this.http.get<any>(`${this.apiUrl}/clientes/pesquisar?termo=${encodeURIComponent(valor.trim())}`).subscribe({
        next: r => {
          const lista = (r.data ?? []).map((c: any) => ({ id: c.clienteId, nome: c.nome, cpfCnpj: c.cpfCnpj }));
          this.recebClienteResultados.set(lista);
        }
      });
    }, 300);
  }

  selecionarRecebCliente(c: PessoaLookup) {
    this.recebClienteSelecionado.set(c);
    this.recebClienteBusca.set(c.nome);
    this.recebClienteResultados.set([]);
    // Carrega contas a receber abertas do cliente
    this.http.get<any>(`${this.apiUrl}/contasreceber?status=aberta&busca=${encodeURIComponent(c.nome)}`).subscribe({
      next: r => {
        const lista: ContaReceberPendente[] = (r.data ?? []).filter((x: any) => x.clienteNome === c.nome).map((x: any) => ({
          id: x.id, codigo: x.codigo, descricao: x.descricao, clienteNome: x.clienteNome,
          valor: x.valor, valorLiquido: x.valorLiquido, valorRecebido: x.valorRecebido,
          dataVencimento: x.dataVencimento, tipoPagamentoNome: x.tipoPagamentoNome
        }));
        this.recebContasPendentes.set(lista);
      }
    });
  }

  selecionarRecebConta(c: ContaReceberPendente) {
    this.recebContaSelecionada.set(c);
    const restante = c.valorLiquido - c.valorRecebido;
    this.recebValor.set(restante.toFixed(2).replace('.', ','));
  }

  confirmarRecebimento() {
    const conta = this.recebContaSelecionada();
    const tipoPag = this.recebTipoPagamentoId();
    const valor = parseFloat(this.recebValor().replace(',', '.')) || 0;
    if (!conta) { this.modal.aviso('Selecione uma conta', 'Escolha uma conta a receber.'); return; }
    if (!tipoPag) { this.modal.aviso('Forma de pagamento', 'Selecione a forma de pagamento.'); return; }
    if (valor <= 0) { this.modal.aviso('Valor inválido', 'Informe um valor maior que zero.'); return; }
    const caixaId = this.caixaAberto()!.id;
    this.http.post<any>(`${this.apiUrl}/caixamovimentos/recebimento`, {
      caixaId, contaReceberId: conta.id, valor, tipoPagamentoId: tipoPag
    }).subscribe({
      next: () => {
        this.modalRecebimento.set(false);
        this.carregarMovimentos();
        this.modal.sucesso('Recebimento', 'Recebimento registrado.');
      },
      error: (err: any) => this.modal.erro('Erro', err?.error?.message || 'Erro ao registrar recebimento.')
    });
  }

  // ══ Pagamento (despesa do dia) ════════════════════════════════
  pagamento() {
    if (!this.caixaAberto()) { this.modal.aviso('Caixa fechado', 'Abra o caixa primeiro.'); return; }
    this.pagPessoaBusca.set('');
    this.pagPessoaResultados.set([]);
    this.pagPessoaSelecionada.set(null);
    this.pagPlanoBusca.set('');
    this.pagPlanoResultados.set([]);
    this.pagPlanoSelecionado.set(null);
    this.pagValor.set('');
    this.pagDescricao.set('');
    this.pagObs.set('');
    this.pagTipoPagamentoId.set(null);
    this.modalPagamento.set(true);
  }
  cancelarPagamento() { this.modalPagamento.set(false); }

  onPagPessoaInput(valor: string) {
    this.pagPessoaBusca.set(valor);
    if (this.pagTimer) clearTimeout(this.pagTimer);
    if (valor.trim().length < 2) { this.pagPessoaResultados.set([]); return; }
    this.pagTimer = setTimeout(() => {
      this.http.get<any>(`${this.apiUrl}/fornecedores/pesquisar?termo=${encodeURIComponent(valor.trim())}`).subscribe({
        next: r => {
          const lista = (r.data ?? []).map((f: any) => ({ id: f.pessoaId ?? f.id, nome: f.nome, cpfCnpj: f.cpfCnpj }));
          this.pagPessoaResultados.set(lista);
        }
      });
    }, 300);
  }
  selecionarPagPessoa(p: PessoaLookup) {
    this.pagPessoaSelecionada.set(p);
    this.pagPessoaBusca.set(p.nome);
    this.pagPessoaResultados.set([]);
  }

  onPagPlanoInput(valor: string) {
    this.pagPlanoBusca.set(valor);
    if (valor.trim().length < 2) { this.pagPlanoResultados.set([]); return; }
    this.http.get<any>(`${this.apiUrl}/planoscontas/pesquisar?termo=${encodeURIComponent(valor.trim())}`).subscribe({
      next: r => this.pagPlanoResultados.set(r.data ?? [])
    });
  }
  selecionarPagPlano(p: PlanoContaLookup) {
    this.pagPlanoSelecionado.set(p);
    this.pagPlanoBusca.set(`${p.codigoHierarquico} - ${p.descricao}`);
    this.pagPlanoResultados.set([]);
  }

  confirmarPagamento() {
    const pessoa = this.pagPessoaSelecionada();
    const plano = this.pagPlanoSelecionado();
    const tipoPag = this.pagTipoPagamentoId();
    const valor = parseFloat(this.pagValor().replace(',', '.')) || 0;
    if (!pessoa) { this.modal.aviso('Fornecedor', 'Selecione um fornecedor.'); return; }
    if (!plano) { this.modal.aviso('Plano de contas', 'Selecione um plano de contas.'); return; }
    if (!tipoPag) { this.modal.aviso('Forma de pagamento', 'Selecione a forma de pagamento.'); return; }
    if (valor <= 0) { this.modal.aviso('Valor inválido', 'Informe um valor maior que zero.'); return; }
    const caixaId = this.caixaAberto()!.id;
    this.http.post<any>(`${this.apiUrl}/caixamovimentos/pagamento`, {
      caixaId, pessoaId: pessoa.id, planoContaId: plano.id, valor,
      tipoPagamentoId: tipoPag, descricao: this.pagDescricao(), observacao: this.pagObs() || null
    }).subscribe({
      next: () => {
        this.modalPagamento.set(false);
        this.carregarMovimentos();
        this.modal.sucesso('Pagamento', 'Pagamento registrado e lançado em contas a pagar.');
      },
      error: (err: any) => this.modal.erro('Erro', err?.error?.message || 'Erro ao registrar pagamento.')
    });
  }

  // ══ Fechamento simples (declaração de valores) ═══════════════
  prepararFechamento() {
    const tipos = this.tiposPagamento();
    this.fechamentoDeclarados.set(tipos.map(t => ({ tipoPagamentoId: t.id, nome: t.nome, valor: '0,00' })));
    this.fechamentoObs.set('');
  }

  onFechamentoValor(tipoId: number, valor: string) {
    this.fechamentoDeclarados.update(lista =>
      lista.map(d => d.tipoPagamentoId === tipoId ? { ...d, valor } : d)
    );
  }

  confirmarFechamentoSimples() {
    const caixaId = this.caixaAberto()!.id;
    const declarados = this.fechamentoDeclarados().map(d => ({
      tipoPagamentoId: d.tipoPagamentoId,
      valorDeclarado: parseFloat(d.valor.replace(',', '.')) || 0
    }));
    this.http.post<any>(`${this.apiUrl}/caixas/${caixaId}/fechar`, {
      caixaId, declarados, observacao: this.fechamentoObs() || null
    }).subscribe({
      next: () => {
        this.modalFechamentoSimples.set(false);
        this.caixaAberto.set(null);
        this.modal.sucesso('Caixa Fechado', 'Fechamento realizado. A conferência será feita em Financeiro > Conferência de Caixa.');
      },
      error: (err: any) => this.modal.erro('Erro', err?.error?.message || 'Erro ao fechar caixa.')
    });
  }

  recargas() { this.modal.aviso('Em Desenvolvimento', 'A funcionalidade de recargas será implementada em breve.'); }
  opcoes() { this.modal.aviso('Em Desenvolvimento', 'As opções adicionais serão implementadas em breve.'); }

  // ── Ações barra inferior (placeholder) ─────────────────────────
  limpar() { this.modal.aviso('Em Desenvolvimento', 'Limpar será implementado em breve.'); }
  eliminar() { this.modal.aviso('Em Desenvolvimento', 'Eliminar será implementado em breve.'); }
  pbms() { this.modal.aviso('Em Desenvolvimento', 'PBMs será implementado em breve.'); }
  webConvenios() { this.modal.aviso('Em Desenvolvimento', 'Web Convênios será implementado em breve.'); }
  movimEstoque() { this.modal.aviso('Em Desenvolvimento', 'Movimentação de Estoque será implementada em breve.'); }
  abrirCliente() { this.modal.aviso('Em Desenvolvimento', 'Abrirá o cadastro de cliente.'); }
  abrirProduto() { this.modal.aviso('Em Desenvolvimento', 'Abrirá o cadastro de produto.'); }
  devolucao() { this.modal.aviso('Em Desenvolvimento', 'Devolução será implementada em breve.'); }
  consultaEstoque() { this.modal.aviso('Em Desenvolvimento', 'Consulta de Estoque será implementada em breve.'); }
  cobertura() { this.modal.aviso('Em Desenvolvimento', 'Cobertura/Oferta será implementada em breve.'); }
  digitalizar() { this.modal.aviso('Em Desenvolvimento', 'Digitalizar será implementado em breve.'); }

  vender() {
    if (!this.caixaAberto()) {
      this.modal.aviso('Caixa Fechado', 'Abra o caixa antes de realizar vendas.');
      return;
    }
    this.modal.aviso('Em Desenvolvimento', 'A finalização da venda no caixa será implementada em breve.');
  }

  // ── Sair ───────────────────────────────────────────────────────
  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }

  formatarData(data: string): string {
    if (!data) return '';
    const d = new Date(data);
    return d.toLocaleDateString('pt-BR') + ' ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  }

  formatarMoeda(valor: number): string {
    return valor.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  // ── Grid pendentes: padrão ERP ─────────────────────────────────
  private carregarColunasPend(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_PEND);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return PEND_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return PEND_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasPend() {
    localStorage.setItem(this.STORAGE_PEND, JSON.stringify(this.pendColunas()));
  }

  pendOrdenar(campo: string) {
    if (this.pendSortColuna() === campo) this.pendSortDir.set(this.pendSortDir() === 'asc' ? 'desc' : 'asc');
    else { this.pendSortColuna.set(campo); this.pendSortDir.set('asc'); }
  }

  pendSortIcon(campo: string): string {
    if (this.pendSortColuna() !== campo) return '⇅';
    return this.pendSortDir() === 'asc' ? '▲' : '▼';
  }

  pendSorted(): VendaPendente[] {
    const col = this.pendSortColuna(); const dir = this.pendSortDir();
    const lista = this.pendentes();
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  }

  pendCellValue(item: VendaPendente, campo: string): string {
    const v = (item as any)[campo];
    if (v === null || v === undefined) return '—';
    if (campo === 'criadoEm') return this.formatarData(v);
    if (campo === 'totalLiquido') return this.formatarMoeda(v);
    if (campo === 'clienteNome' && !v) return 'Consumidor';
    return String(v);
  }

  pendToggleColuna(campo: string) {
    this.pendColunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasPend();
  }

  pendRestaurarColunas() {
    this.pendColunas.set(PEND_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasPend();
  }

  pendIniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    this.pendResizeState = { campo, startX: e.clientX, startW: largura };
    document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none';
  }

  @HostListener('document:mousemove', ['$event'])
  onResizeMove(e: MouseEvent) {
    if (this.pendResizeState) {
      const delta = e.clientX - this.pendResizeState.startX;
      const def = PEND_COLUNAS.find(c => c.campo === this.pendResizeState!.campo);
      const min = def?.minLargura ?? 50;
      const novaLargura = Math.max(min, this.pendResizeState.startW + delta);
      this.pendColunas.update(cols => cols.map(c => c.campo === this.pendResizeState!.campo ? { ...c, largura: novaLargura } : c));
    }
    if (this.vrResizeState) {
      const delta = e.clientX - this.vrResizeState.startX;
      const def = VR_COLUNAS.find(c => c.campo === this.vrResizeState!.campo);
      const min = def?.minLargura ?? 50;
      const novaLargura = Math.max(min, this.vrResizeState.startW + delta);
      this.vrColunas.update(cols => cols.map(c => c.campo === this.vrResizeState!.campo ? { ...c, largura: novaLargura } : c));
    }
    if (this.vrItensResizeState) {
      const delta = e.clientX - this.vrItensResizeState.startX;
      const def = VR_ITENS_COLUNAS.find(c => c.campo === this.vrItensResizeState!.campo);
      const min = def?.minLargura ?? 50;
      const novaLargura = Math.max(min, this.vrItensResizeState.startW + delta);
      this.vrItensColunas.update(cols => cols.map(c => c.campo === this.vrItensResizeState!.campo ? { ...c, largura: novaLargura } : c));
    }
  }

  @HostListener('document:mouseup')
  onResizeEnd() {
    if (this.pendResizeState) { this.salvarColunasPend(); this.pendResizeState = null; document.body.style.cursor = ''; document.body.style.userSelect = ''; }
    if (this.vrResizeState) { this.salvarColunasVr(); this.vrResizeState = null; document.body.style.cursor = ''; document.body.style.userSelect = ''; }
    if (this.vrItensResizeState) { this.salvarColunasVrItens(); this.vrItensResizeState = null; document.body.style.cursor = ''; document.body.style.userSelect = ''; }
  }

  pendDragStart(idx: number) { this.pendDragIdx = idx; }
  pendDragOver(e: DragEvent, idx: number) {
    e.preventDefault();
    if (this.pendDragIdx === null || this.pendDragIdx === idx) return;
    this.pendColunas.update(cols => { const arr = [...cols]; const [m] = arr.splice(this.pendDragIdx!, 1); arr.splice(idx, 0, m); this.pendDragIdx = idx; return arr; });
  }
  pendDrop() { this.pendDragIdx = null; this.salvarColunasPend(); }

  // ── Grid vendas realizadas: padrão ERP ─────────────────────────
  private carregarColunasVr(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_VR);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return VR_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return VR_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasVr() {
    localStorage.setItem(this.STORAGE_VR, JSON.stringify(this.vrColunas()));
  }

  vrOrdenar(campo: string) {
    if (this.vrSortColuna() === campo) this.vrSortDir.set(this.vrSortDir() === 'asc' ? 'desc' : 'asc');
    else { this.vrSortColuna.set(campo); this.vrSortDir.set('asc'); }
  }

  vrSortIcon(campo: string): string {
    if (this.vrSortColuna() !== campo) return '⇅';
    return this.vrSortDir() === 'asc' ? '▲' : '▼';
  }

  vrSorted(): VendaRealizada[] {
    const col = this.vrSortColuna(); const dir = this.vrSortDir();
    const lista = this.vendasRealizadas();
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  }

  vrCellValue(item: VendaRealizada, campo: string): string {
    const v = (item as any)[campo];
    if (v === null || v === undefined) return '—';
    if (campo === 'dataEmissaoCupom' || campo === 'dataFinalizacao' || campo === 'criadoEm') return this.formatarData(v);
    if (campo === 'totalBruto' || campo === 'totalDesconto' || campo === 'totalLiquido') return this.formatarMoeda(v);
    if (campo === 'clienteNome' && !v) return 'Consumidor';
    return String(v);
  }

  vrToggleColuna(campo: string) {
    this.vrColunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasVr();
  }

  vrRestaurarColunas() {
    this.vrColunas.set(VR_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasVr();
  }

  vrIniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    this.vrResizeState = { campo, startX: e.clientX, startW: largura };
    document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none';
  }

  vrDragStart(idx: number) { this.vrDragIdx = idx; }
  vrDragOver(e: DragEvent, idx: number) {
    e.preventDefault();
    if (this.vrDragIdx === null || this.vrDragIdx === idx) return;
    this.vrColunas.update(cols => { const arr = [...cols]; const [m] = arr.splice(this.vrDragIdx!, 1); arr.splice(idx, 0, m); this.vrDragIdx = idx; return arr; });
  }
  vrDrop() { this.vrDragIdx = null; this.salvarColunasVr(); }

  vrExpandir(venda: VendaRealizada) {
    if (this.vrExpandido() === venda.id) {
      this.vrExpandido.set(null);
      return;
    }
    this.vrExpandido.set(venda.id);
    this.vrItensLoading.set(true);
    this.http.get<any>(`${this.apiUrl}/vendas/${venda.id}`).subscribe({
      next: r => {
        const itens = (r.data?.itens ?? []).map((i: any) => ({
          produtoCodigo: i.produtoCodigo,
          produtoNome: i.produtoNome,
          fabricante: i.fabricante,
          quantidade: i.quantidade,
          totalBruto: (i.precoVenda ?? 0) * (i.quantidade ?? 1),
          totalDesconto: i.valorDesconto ?? 0,
          total: i.total
        }));
        this.vrItens.set(itens);
        this.vrItensLoading.set(false);
      },
      error: () => this.vrItensLoading.set(false)
    });
  }

  // ── Grid itens vendas realizadas ───────────────────────────────
  private carregarColunasVrItens(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_VR_ITENS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return VR_ITENS_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return VR_ITENS_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasVrItens() {
    localStorage.setItem(this.STORAGE_VR_ITENS, JSON.stringify(this.vrItensColunas()));
  }

  vrItensOrdenar(campo: string) {
    if (this.vrItensSortColuna() === campo) this.vrItensSortDir.set(this.vrItensSortDir() === 'asc' ? 'desc' : 'asc');
    else { this.vrItensSortColuna.set(campo); this.vrItensSortDir.set('asc'); }
  }

  vrItensSortIcon(campo: string): string {
    if (this.vrItensSortColuna() !== campo) return '⇅';
    return this.vrItensSortDir() === 'asc' ? '▲' : '▼';
  }

  vrItensSorted(): VendaRealizadaItem[] {
    const col = this.vrItensSortColuna(); const dir = this.vrItensSortDir();
    const lista = this.vrItens();
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  }

  vrItensCellValue(item: VendaRealizadaItem, campo: string): string {
    const v = (item as any)[campo];
    if (v === null || v === undefined) return '—';
    if (campo === 'totalBruto' || campo === 'totalDesconto' || campo === 'total') return this.formatarMoeda(v);
    return String(v);
  }

  vrItensToggleColuna(campo: string) {
    this.vrItensColunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasVrItens();
  }

  vrItensRestaurarColunas() {
    this.vrItensColunas.set(VR_ITENS_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasVrItens();
  }

  vrItensIniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    this.vrItensResizeState = { campo, startX: e.clientX, startW: largura };
    document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none';
  }

  vrItensDragStart(idx: number) { this.vrItensDragIdx = idx; }
  vrItensDragOver(e: DragEvent, idx: number) {
    e.preventDefault();
    if (this.vrItensDragIdx === null || this.vrItensDragIdx === idx) return;
    this.vrItensColunas.update(cols => { const arr = [...cols]; const [m] = arr.splice(this.vrItensDragIdx!, 1); arr.splice(idx, 0, m); this.vrItensDragIdx = idx; return arr; });
  }
  vrItensDrop() { this.vrItensDragIdx = null; this.salvarColunasVrItens(); }
}
