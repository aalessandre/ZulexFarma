import { Component, signal, computed, OnInit, OnDestroy, HostListener, ElementRef, ViewChild, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { ModalSenhaService } from '../../core/services/modal-senha.service';
import { ModalSenhaComponent } from '../../core/components/modal-senha.component';
import { SafePipe } from '../../core/pipes/safe.pipe';
import { SngpcModalComponent } from './sngpc-modal/sngpc-modal.component';
import { firstValueFrom } from 'rxjs';

interface ItemDesconto {
  tipo: number; // 1=Desconto, 2=Promocao
  percentual: number;
  origem: string;
  regra: string;
  origemId?: number;
  liberadoPorId?: number;
}

interface PreVendaItem {
  produtoId: number;
  produtoCodigo: string;
  produtoNome: string;
  fabricante?: string;
  precoVenda: number;
  quantidade: number;
  percentualDesconto: number;
  percentualPromocao: number;
  valorDesconto: number;
  precoUnitario: number;
  total: number;
  estoqueAtual?: number;
  unidade?: string;
  vendedor?: string;
  descontoMaxPermitido?: number;
  componenteDesconto?: string;
  descontos: ItemDesconto[];
  colaboradorId?: number;
  temPromocao?: boolean;
  /** SNGPC: "Psicotrópicos" ou "Antimicrobiano" quando controlado, senão null/undefined. */
  classeTerapeutica?: string | null;
  // Conferência
  quantidadePrevenda?: number;
  origemItem?: 'prevenda' | 'adicionado';
  permitirConferenciaDigitando?: boolean;
}

interface HierarquiaInfo {
  id: number; nome: string; padrao: boolean; aplicarAutomatico: boolean; descontoAutoTipo?: number; totalItens: number;
}

interface DescontoResolucao {
  hierarquiaId?: number; hierarquiaNome?: string; descontoMinimo: number; descontoMaxSemSenha: number;
  descontoMaxComSenha: number; descontoAplicar: number; aplicarAutomatico: boolean; componente?: string;
}

interface PromocaoFaixa {
  quantidade: number;
  percentualDesconto: number;
}

interface PromocaoProduto {
  promocaoId: number;
  nome: string;
  tipo: number; // 1=Fixa, 2=Progressiva
  tipoDescricao: string;
  percentualPromocao: number;
  valorPromocao: number;
  qtdeLimite?: number;
  qtdeVendida: number;
  permitirMudarPreco: boolean;
  faixas: PromocaoFaixa[];
}

interface TipoPagBtn {
  id: number;
  nome: string;
  ordem: number;
  padraoSistema: boolean;
  planoContaId?: number;
  modalidade?: number;
}

interface AdqTarifa {
  id: number;
  modalidade: number;
  modalidadeDescricao: string;
  tarifa: number;
  prazoRecebimento: number;
  contaBancariaId?: number;
}
interface AdqBandeira {
  id: number;
  bandeira: string;
  tarifas: AdqTarifa[];
}
interface AdqLookup {
  id: number;
  nome: string;
  bandeiras: AdqBandeira[];
}

interface CartaoItem {
  id: string; // uuid local
  adquirenteId: number | null;
  bandeiraId: number | null;
  modalidade: number | null;
  modalidadeDescricao: string;
  valor: number;
  autorizacao: string;
}

interface ConvenioLookup {
  id: number;
  nome: string;
}

interface ClienteLookup {
  clienteId: number;
  codigo?: string;
  nome: string;
  cpfCnpj?: string;
  convenios: ConvenioLookup[];
  ativo: boolean;
}

interface ColaboradorLookup {
  id: number;
  codigo?: string;
  nome: string;
}

interface ProdutoLookup {
  id: number;
  codigo: string;
  nome: string;
  fabricante?: string;
  valorVenda: number;
  estoqueAtual?: number;
  unidade?: string;
  temPromocao?: boolean;
}

interface ColunaDef {
  campo: string;
  label: string;
  largura: number;
  minLargura: number;
  padrao: boolean;
  editavel?: boolean;
  tipo?: 'texto' | 'numero';
}

interface ColunaEstado extends ColunaDef {
  visivel: boolean;
}

interface Atendimento {
  id: number;
  label: string;
  preVendaId: number | null;
  itens: PreVendaItem[];
  clienteId: number | null;
  clienteNome: string;
  colaboradorId: number | null;
  colaboradorNome: string;
  tipoPagamentoId: number | null;
}

const PREVENDA_COLUNAS: ColunaDef[] = [
  { campo: 'produtoCodigo',      label: 'CÓDIGO',       largura: 80,  minLargura: 60,  padrao: true, tipo: 'texto' },
  { campo: 'produtoNome',        label: 'PRODUTO',      largura: 280, minLargura: 150, padrao: true, tipo: 'texto' },
  { campo: 'fabricante',         label: 'FABRICANTE',   largura: 130, minLargura: 80,  padrao: true, tipo: 'texto' },
  { campo: 'precoVenda',         label: 'PREÇO VENDA',  largura: 100, minLargura: 70,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'quantidade',         label: 'QTDE',         largura: 70,  minLargura: 50,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'percentualDesconto', label: '%DESC',        largura: 80,  minLargura: 60,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'precoUnitario',      label: 'PREÇO UNIT',   largura: 100, minLargura: 70,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'total',              label: 'TOTAL',        largura: 100, minLargura: 70,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'vendedor',           label: 'VENDEDOR',     largura: 120, minLargura: 80,  padrao: true, tipo: 'texto' },
];

const CORES_PAGAMENTO = ['#2196F3', '#4CAF50', '#FF9800', '#9C27B0', '#F44336', '#009688', '#795548'];

@Component({
  selector: 'app-caixa-venda',
  standalone: true,
  imports: [CommonModule, FormsModule, SafePipe, ModalSenhaComponent, SngpcModalComponent],
  templateUrl: './caixa-venda.component.html',
  styleUrl: './caixa-venda.component.scss'
})
export class CaixaVendaComponent implements OnInit, OnDestroy {
  @Input() caixaId: number | null = null;

  private readonly STATE_KEY = 'zulex_caixa_venda_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_caixa_itens';
  private readonly apiUrl = environment.apiUrl;

  @ViewChild('inputCliente') inputClienteRef!: ElementRef<HTMLInputElement>;
  @ViewChild('inputVendedor') inputVendedorRef!: ElementRef<HTMLInputElement>;
  @ViewChild('inputProduto') inputProdutoRef!: ElementRef<HTMLInputElement>;
  @ViewChild('sngpcScreen') sngpcScreenRef?: SngpcModalComponent;
  private saindo = false;

  // ── Configurações de venda ──────────────────────────────────────
  cfgMultiplosVendedores = signal(false);
  cfgDuplicarLinha = signal(false);
  cfgFocarQuantidade = signal(false);
  cfgAlterarPrecoPromo = signal(false);
  cfgObrigarEscanear = signal(false);
  cfgPromoMultiplas = signal<'exibir' | 'menor'>('exibir');
  cfgExigirConferencia = signal(true); // padrão true até carregar config
  cfgConferenciaSoBarras = signal(false);
  cfgSngpcAtivar = signal(false);
  cfgSngpcVendasModo = signal<'Obrigatorio' | 'NaoLancar' | 'Misto'>('Obrigatorio');
  private configsCarregadas = signal(false);

  // ── SNGPC (fluxo Avançar → tela inline) ────────────────────────
  /** 'venda' = tela de cart normal; 'sngpc' = tela de receitas inline (ocupa o main). */
  etapaVenda = signal<'venda' | 'sngpc'>('venda');
  /** Body da venda preparado ao clicar Avançar — só é enviado ao backend quando clicar Finalizar. */
  private sngpcVendaBody: any = null;
  /** Body de finalização preparado ao clicar Avançar — aguarda o usuário preencher receitas. */
  private sngpcFinalizarBody: any = null;


  // ── Conferência ─────────────────────────────────────────────────
  modoConferencia = signal(false);
  private carregarPrevendaListener: any = null;
  cfgInformarCesta = signal(false);

  // ── Modal cesta ─────────────────────────────────────────────────
  modalCesta = signal(false);
  cestaNumero = signal('');

  // ── Emissão NFC-e (overlay) ─────────────────────────────────────
  emitindoNfce = signal(false);

  // ── Modal DANFE NFC-e ───────────────────────────────────────────
  modalDanfe = signal(false);
  danfeUrl = signal('');

  abrirDanfe(nfceId: number) {
    this.danfeUrl.set(`${this.apiUrl}/venda-fiscal/${nfceId}/danfe`);
    this.modalDanfe.set(true);
  }

  fecharDanfe() {
    this.modalDanfe.set(false);
    this.danfeUrl.set('');
    this.resetTudo();
  }

  imprimirDanfe() {
    const iframe = document.getElementById('danfe-iframe') as HTMLIFrameElement;
    if (iframe?.contentWindow) {
      iframe.contentWindow.print();
    }
  }

  compartilharWhatsapp() {
    // TODO: implementar módulo WhatsApp futuramente
  }

  // ── Modal pendentes ─────────────────────────────────────────────
  readonly STORAGE_PEND_VENDAS = 'zulex_colunas_pend_vendas';
  readonly STORAGE_PEND_ITENS = 'zulex_colunas_pend_itens';
  modalPendentes = signal(false);
  vendasPendentes = signal<any[]>([]);
  pendentesLoading = signal(false);
  pendenteSelecionada = signal<any | null>(null);
  pendenteItens = signal<any[]>([]);
  pendenteItensLoading = signal(false);

  // Grid vendas pendentes
  pendVendasCols = signal<ColunaEstado[]>(this.carregarColsPend(this.STORAGE_PEND_VENDAS, [
    { campo: 'codigo',            label: 'CÓDIGO',     largura: 80,  minLargura: 60,  padrao: true },
    { campo: 'nrCesta',           label: 'CESTA',      largura: 80,  minLargura: 60,  padrao: true },
    { campo: 'colaboradorNome',   label: 'VENDEDOR',   largura: 140, minLargura: 80,  padrao: true },
    { campo: 'clienteNome',       label: 'CLIENTE',    largura: 200, minLargura: 100, padrao: true },
    { campo: 'totalItens',        label: 'ITENS',      largura: 60,  minLargura: 50,  padrao: true },
    { campo: 'totalLiquido',      label: 'TOTAL',      largura: 100, minLargura: 70,  padrao: true },
    { campo: 'criadoEm',          label: 'DATA/HORA',  largura: 140, minLargura: 100, padrao: true },
    { campo: 'tipoPagamentoNome', label: 'PAGAMENTO',  largura: 110, minLargura: 80,  padrao: true },
  ]));
  pendVendasColsVisiveis = computed(() => this.pendVendasCols().filter(c => c.visivel));
  pendVendasSort = signal(''); pendVendasSortDir = signal<'asc' | 'desc'>('asc');
  pendVendasPainel = signal(false);
  private pendVendasDragIdx: number | null = null;
  private pendVendasResizeState: { campo: string; startX: number; startW: number } | null = null;

  // Grid itens pendente
  pendItensCols = signal<ColunaEstado[]>(this.carregarColsPend(this.STORAGE_PEND_ITENS, [
    { campo: 'produtoCodigo',      label: 'CÓDIGO',      largura: 80,  minLargura: 60,  padrao: true },
    { campo: 'produtoNome',        label: 'PRODUTO',     largura: 260, minLargura: 120, padrao: true },
    { campo: 'fabricante',         label: 'FABRICANTE',  largura: 140, minLargura: 80,  padrao: true },
    { campo: 'quantidade',         label: 'QTDE',        largura: 60,  minLargura: 50,  padrao: true },
    { campo: 'precoVenda',         label: 'PREÇO VENDA', largura: 100, minLargura: 70,  padrao: true },
    { campo: 'percentualDesconto', label: '%DESC',       largura: 80,  minLargura: 60,  padrao: true },
    { campo: 'precoUnitario',      label: 'PREÇO UNIT',  largura: 100, minLargura: 70,  padrao: true },
    { campo: 'total',              label: 'TOTAL',       largura: 100, minLargura: 70,  padrao: true },
  ]));
  pendItensColsVisiveis = computed(() => this.pendItensCols().filter(c => c.visivel));
  pendItensSort = signal(''); pendItensSortDir = signal<'asc' | 'desc'>('asc');
  pendItensPainel = signal(false);
  private pendItensDragIdx: number | null = null;
  private pendItensResizeState: { campo: string; startX: number; startW: number } | null = null;

  // ── Filial ──────────────────────────────────────────────────────
  filiais = signal<{ id: number; nome: string }[]>([]);
  filialId = signal(1);

  // ── Modais de promoção ───────────────────────────────────────────
  modalPromoProgressiva = signal(false);

  // ── Modal desdobramento de pagamento ────────────────────────────
  modalPagamento = signal(false);
  pagamentoValores = signal<{ tipoPagamentoId: number; nome: string; valor: number; modalidade: number }[]>([]);
  promoProgressivaAtual = signal<PromocaoProduto | null>(null);
  promoProgressivaItemIdx = signal(-1);
  modalPromoFixas = signal(false);
  promoFixasLista = signal<PromocaoProduto[]>([]);
  promoFixasItemIdx = signal(-1);

  // ── Modal dados do cartão ───────────────────────────────────────
  modalCartao = signal(false);
  cartoes = signal<CartaoItem[]>([]);
  cartaoErro = signal('');
  cartaoTotalAlvo = signal(0); // total que precisa ser distribuído nos cartões
  adquirentes = signal<AdqLookup[]>([]);

  // Mapeamento bandeira → código IBGE/SEFAZ (tBand NFC-e)
  private readonly bandeiraCodigo: Record<string, string> = {
    'VISA': '01', 'MASTERCARD': '02', 'MASTER': '02', 'AMERICAN EXPRESS': '03', 'AMEX': '03',
    'SORO CREDIT': '04', 'ELO': '05', 'DINERS': '05', 'DINERS CLUB': '05',
    'HIPERCARD': '06', 'AURA': '07', 'CABAL': '08', 'OUTROS': '99'
  };

  cartaoTotalInformado = computed(() => this.cartoes().reduce((s, c) => s + (c.valor || 0), 0));
  cartaoFalta = computed(() => Math.round((this.cartaoTotalAlvo() - this.cartaoTotalInformado()) * 100) / 100);

  /** Lista unificada de bandeiras (todas as bandeiras de todos adquirentes, dedup por nome).
   *  Cada item aponta para o primeiro adquirente que tem essa bandeira. */
  bandeirasUnificadas = computed(() => {
    const mapa = new Map<string, { nome: string; adquirenteId: number; bandeiraId: number }>();
    for (const adq of this.adquirentes()) {
      for (const b of adq.bandeiras) {
        const key = b.bandeira.trim().toUpperCase();
        if (!mapa.has(key)) {
          mapa.set(key, { nome: b.bandeira, adquirenteId: adq.id, bandeiraId: b.id });
        }
      }
    }
    return Array.from(mapa.values());
  });

  // ── Abas de atendimento ─────────────────────────────────────────
  atendimentos = signal<Atendimento[]>([]);
  abaAtivaId = signal(1);
  private nextAbaId = 1;

  // ── Pre-venda state (aba ativa) ─────────────────────────────────
  preVendaId = signal<number | null>(null);
  itens = signal<PreVendaItem[]>([]);
  itensSelecionadoIdx = signal<number | null>(null);

  /** True quando o cart contém pelo menos um produto com ClasseTerapeutica controlada. */
  hasControladosNoCart = computed(() =>
    this.itens().some(i =>
      i.classeTerapeutica === 'Psicotrópicos' || i.classeTerapeutica === 'Antimicrobiano'
    )
  );

  /** True quando precisa abrir a tela SNGPC ao clicar Avançar. */
  precisaTelaSngpc = computed(() =>
    this.cfgSngpcAtivar()
    && this.cfgSngpcVendasModo() !== 'NaoLancar'
    && this.hasControladosNoCart()
  );

  // ── Client (ComboGrid) ──────────────────────────────────────────
  clienteId = signal<number | null>(null);
  clienteNome = signal('');
  clienteBusca = signal('');
  clienteResultados = signal<ClienteLookup[]>([]);
  clienteDropdown = signal(false);
  clienteAtivos = signal(true);
  clienteIndice = signal(-1);
  clienteExpandidoId = signal<number | null>(null);
  clienteSort = signal<{ col: string; dir: 'asc' | 'desc' } | null>(null);
  private clienteTimer: any = null;

  // ── Collaborator (ComboGrid) ────────────────────────────────────
  colaboradorId = signal<number | null>(null);
  colaboradorNome = signal('');
  colaboradorBusca = signal('');
  colaboradorResultados = signal<ColaboradorLookup[]>([]);
  colaboradorDropdown = signal(false);
  colaboradorAtivos = signal(true);
  colaboradorIndice = signal(-1);
  colaboradorSort = signal<{ col: string; dir: 'asc' | 'desc' } | null>(null);
  private colaboradorTimer: any = null;

  // ── Payment type ────────────────────────────────────────────────
  tipoPagamentoId = signal<number | null>(null);
  tiposPagamento = signal<TipoPagBtn[]>([]);

  // ── Venda a prazo ───────────────────────────────────────────────
  private tokenLiberacaoCredito: string | null = null;
  private senhaClientePrazo: string | null = null;
  prazoPermiteParcelada = signal(false);
  prazoMaxParcelas = signal(1);
  prazoNumeroParcelas = signal(1);
  parcelasOpcoes = computed(() => Array.from({ length: this.prazoMaxParcelas() }, (_, i) => i + 1));

  // ── Hierarquia de desconto ──────────────────────────────────────
  hierarquiaAtiva = signal<HierarquiaInfo | null>(null);
  convenioIdCliente = signal<number | null>(null);

  // ── Product (ComboGrid) ──────────────────────────────────────────
  produtoBusca = signal('');
  produtoResultados = signal<ProdutoLookup[]>([]);
  produtoDropdown = signal(false);
  produtoAtivos = signal(true);
  produtoIndice = signal(-1);
  produtoSort = signal<{ col: string; dir: 'asc' | 'desc' } | null>(null);
  private produtoTimer: any = null;

  // ── ComboGrid: resize state ─────────────────────────────────────
  private cgResizeState: { target: HTMLElement; startX: number; startW: number } | null = null;

  // ── Grid columns ────────────────────────────────────────────────
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal<string>('');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  // ── Computed totals ─────────────────────────────────────────────
  totalItens = computed(() => this.itens().length);
  totalBruto = computed(() =>
    this.itens().reduce((sum, i) => sum + (i.precoVenda * i.quantidade), 0)
  );
  totalDesconto = computed(() =>
    this.itens().reduce((sum, i) => sum + i.valorDesconto, 0)
  );
  totalLiquido = computed(() =>
    this.itens().reduce((sum, i) => sum + i.total, 0)
  );

  // ── Item info (selected) ────────────────────────────────────────
  itemSelecionado = computed(() => {
    const idx = this.itensSelecionadoIdx();
    if (idx === null) return null;
    return this.itens()[idx] ?? null;
  });

  // ── Saving state ────────────────────────────────────────────────
  salvando = signal(false);

  // ── Sorted items ────────────────────────────────────────────────
  itensSorted = computed(() => {
    const col = this.sortColuna();
    const dir = this.sortDirecao();
    const lista = this.itens();
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number'
        ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService,
    public modalSenha: ModalSenhaService
  ) {}

  ngOnInit() {
    this.carregarFiliais();
    this.carregarConfigs();
    this.carregarTiposPagamento();
    this.carregarAdquirentes();
    this.restaurarEstado();
    this.buscarHierarquia();
    this.focarCliente();
    // Listener para receber pré-venda do painel de pendentes
    this.carregarPrevendaListener = (e: CustomEvent) => this.carregarPrevendaNoCheckout(e.detail.vendaId, e.detail.nrCesta);
    window.addEventListener('carregar-prevenda', this.carregarPrevendaListener);
  }

  private carregarFiliais() {
    const usuario = this.auth.usuarioLogado();
    this.filialId.set(parseInt(usuario?.filialId || '1', 10));
    this.http.get<any>(`${this.apiUrl}/filiais`).subscribe({
      next: r => this.filiais.set((r.data ?? []).map((f: any) => ({ id: f.id, nome: f.nomeFilial ?? f.nomeFantasia ?? f.nome ?? `Filial ${f.id}` }))),
      error: () => {
        // Se não tem permissão para listar, criar entrada com a filial do usuário
        const id = this.filialId();
        this.filiais.set([{ id, nome: usuario?.nomeFilial ?? `Filial ${id}` }]);
      }
    });
  }

  private carregarConfigs() {
    this.http.get<any>(`${this.apiUrl}/configuracoes`).subscribe({
      next: r => {
        const map: Record<string, string> = {};
        for (const item of (r.data ?? [])) map[item.chave] = item.valor;
        this.cfgMultiplosVendedores.set(map['caixa.multiplos.vendedores'] === 'true');
        this.cfgDuplicarLinha.set(map['caixa.duplicar.linha'] === 'true');
        this.cfgFocarQuantidade.set(map['caixa.focar.quantidade'] === 'true');
        this.cfgAlterarPrecoPromo.set(map['caixa.alterar.preco.promo'] === 'true');
        this.cfgObrigarEscanear.set(map['caixa.obrigar.escanear'] === 'true');
        this.cfgPromoMultiplas.set((map['caixa.promo.multiplas'] ?? map['venda.promo.multiplas'] ?? 'exibir') as 'exibir' | 'menor');
        this.cfgInformarCesta.set(map['caixa.informar.cesta'] === 'true');
        this.cfgExigirConferencia.set(map['caixa.exigir.conferencia'] !== 'false'); // padrão true
        this.cfgConferenciaSoBarras.set(map['caixa.conferencia.codigo.barras'] === 'true');
        this.cfgSngpcAtivar.set(map['sngpc.ativar'] === 'true');
        const modo = (map['sngpc.vendas.modo'] ?? 'Obrigatorio') as any;
        this.cfgSngpcVendasModo.set(modo === 'NaoLancar' || modo === 'Misto' ? modo : 'Obrigatorio');
        this.configsCarregadas.set(true);
      }
    });
  }

  ngOnDestroy() {
    if (!this.saindo) this.salvarEstado();
    if (this.carregarPrevendaListener) window.removeEventListener('carregar-prevenda', this.carregarPrevendaListener);
  }

  // ── Persistência de estado (múltiplas abas) ────────────────────
  private salvarEstado() {
    this.salvarAbaAtiva();
    const estado = { atendimentos: this.atendimentos(), abaAtivaId: this.abaAtivaId(), nextAbaId: this.nextAbaId };
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify(estado));
  }

  private restaurarEstado() {
    try {
      const json = sessionStorage.getItem(this.STATE_KEY);
      if (!json) { this.novaAba(); return; }
      const estado = JSON.parse(json);
      if (estado.atendimentos?.length > 0) {
        this.atendimentos.set(estado.atendimentos);
        this.nextAbaId = estado.nextAbaId ?? estado.atendimentos.length + 1;
        const ativaId = estado.abaAtivaId ?? estado.atendimentos[0].id;
        this.carregarAba(ativaId);
      } else {
        this.novaAba();
      }
    } catch { this.novaAba(); }
  }

  // ── Abas ──────────────────────────────────────────────────────────
  novaAba() {
    this.salvarAbaAtiva();
    const id = this.nextAbaId++;
    const tipoPadrao = this.tiposPagamento().length > 0 ? this.tiposPagamento()[0].id : null;
    const aba: Atendimento = {
      id, label: `Atendimento ${id}`, preVendaId: null, itens: [],
      clienteId: null, clienteNome: '', colaboradorId: null, colaboradorNome: '', tipoPagamentoId: tipoPadrao
    };
    this.atendimentos.update(abas => [...abas, aba]);
    this.carregarAba(id);
  }

  trocarAba(id: number) {
    if (this.abaAtivaId() === id) return;
    this.salvarAbaAtiva();
    this.carregarAba(id);
  }

  fecharAba(id: number) {
    const abas = this.atendimentos();
    if (abas.length <= 1) return; // não fechar a última
    this.atendimentos.update(a => a.filter(x => x.id !== id));
    if (this.abaAtivaId() === id) {
      const restantes = this.atendimentos();
      this.carregarAba(restantes[restantes.length - 1].id);
    }
  }

  private salvarAbaAtiva() {
    const id = this.abaAtivaId();
    this.atendimentos.update(abas => abas.map(a => a.id === id ? {
      ...a, preVendaId: this.preVendaId(), itens: this.itens(),
      clienteId: this.clienteId(), clienteNome: this.clienteNome(),
      colaboradorId: this.colaboradorId(), colaboradorNome: this.colaboradorNome(),
      tipoPagamentoId: this.tipoPagamentoId(),
      label: this.clienteNome() ? this.clienteNome() : `Atendimento ${a.id}`
    } : a));
  }

  private carregarAba(id: number) {
    const aba = this.atendimentos().find(a => a.id === id);
    if (!aba) return;
    this.abaAtivaId.set(id);
    this.preVendaId.set(aba.preVendaId);
    this.itens.set(aba.itens);
    this.clienteId.set(aba.clienteId);
    this.clienteNome.set(aba.clienteNome);
    this.clienteBusca.set(aba.clienteNome);
    this.colaboradorId.set(aba.colaboradorId);
    this.colaboradorNome.set(aba.colaboradorNome);
    this.colaboradorBusca.set(aba.colaboradorNome);
    this.tipoPagamentoId.set(aba.tipoPagamentoId);
    this.itensSelecionadoIdx.set(null);
    this.produtoBusca.set('');
  }

  // ── Payment types ───────────────────────────────────────────────
  carregarTiposPagamento() {
    this.http.get<any>(`${this.apiUrl}/tipospagamento`).subscribe({
      next: r => {
        const tipos = (r.data ?? []).filter((t: any) => t.ativo).sort((a: any, b: any) => a.ordem - b.ordem);
        this.tiposPagamento.set(tipos);
        // Selecionar o primeiro por padrão (Dinheiro) se nenhum selecionado
        if (!this.tipoPagamentoId() && tipos.length > 0) {
          this.tipoPagamentoId.set(tipos[0].id);
        }
      },
      error: () => this.modal.erro('Erro', 'Erro ao carregar tipos de pagamento.')
    });
  }

  corPagamento(idx: number): string {
    return CORES_PAGAMENTO[idx % CORES_PAGAMENTO.length];
  }

  async selecionarPagamento(id: number) {
    if (this.tipoPagamentoId() === id) return;
    const tipo = this.tiposPagamento().find(t => t.id === id);
    if (!tipo) return;

    // Se modalidade = VendaPrazo (4), validar antes
    if ((tipo as any).modalidade === 4) {
      if (!this.clienteId()) {
        await this.modal.aviso('Cliente Obrigatório', 'Para venda a prazo, é necessário selecionar um cliente.');
        return;
      }
      const ok = await this.validarVendaPrazo(id);
      if (!ok) return;
    } else {
      // Limpando estado de prazo ao trocar para outra condição
      this.tokenLiberacaoCredito = null;
      this.senhaClientePrazo = null;
      this.prazoPermiteParcelada.set(false);
      this.prazoMaxParcelas.set(1);
      this.prazoNumeroParcelas.set(1);
    }

    this.tipoPagamentoId.set(id);
    this.recalcularDescontosTodosItens();
  }

  private async validarVendaPrazo(tipoPagamentoId: number): Promise<boolean> {
    try {
      const r = await firstValueFrom(this.http.post<any>(`${this.apiUrl}/vendas/validar-prazo`, {
        clienteId: this.clienteId(),
        convenioId: this.convenioIdCliente(),
        tipoPagamentoId,
        valorVenda: this.totalLiquido()
      }));
      const v = r.data;

      // 1. Bloqueado
      if (v.clienteBloqueado || v.convenioBloqueado) {
        await this.modal.erro('Bloqueado', v.mensagemBloqueio || 'Cliente ou convênio bloqueado.');
        return false;
      }

      // 2. Tipo pagamento bloqueado
      if (v.tipoPagamentoBloqueado) {
        await this.modal.erro('Condição Bloqueada', v.mensagemTipoBloqueado || 'Condição de pagamento bloqueada.');
        return false;
      }

      // 3. Limite de crédito excedido → pedir liberação supervisor
      if (v.excedeLimite) {
        const perm = await this.modal.permissao('venda', 'prazo-excede-limite');
        if (!perm.confirmado) return false;
        this.tokenLiberacaoCredito = perm.tokenLiberacao ?? null;
      } else {
        this.tokenLiberacaoCredito = null;
      }

      // 4. Bloquear desconto parcelada → zerar descontos manuais
      if (v.bloquearDescontoParcelada) {
        this.itens.update(lista => lista.map(item => {
          const valorBruto = item.precoVenda * item.quantidade;
          const valorPromo = valorBruto * item.percentualPromocao / 100;
          const precoUnit = item.precoVenda * (1 - item.percentualPromocao / 100);
          return {
            ...item,
            percentualDesconto: 0,
            valorDesconto: Math.round(valorPromo * 100) / 100,
            precoUnitario: Math.round(precoUnit * 100) / 100,
            total: Math.round(precoUnit * item.quantidade * 100) / 100
          };
        }));
      }

      // 5. Exige senha do cliente
      if (v.exigeSenha) {
        const senha = await this.modalSenha.pedirSenha(
          'Senha do Cliente',
          'Parâmetro: Vender Somente com Senha'
        );
        if (!senha) return false;
        this.senhaClientePrazo = senha;
      } else {
        this.senhaClientePrazo = null;
      }

      // Guardar info de parcelamento
      this.prazoPermiteParcelada.set(v.permiteParcelada);
      this.prazoMaxParcelas.set(v.maxParcelas || 1);
      this.prazoNumeroParcelas.set(1);

      return true;
    } catch (err: any) {
      const msg = err?.error?.message || 'Erro ao validar venda a prazo.';
      await this.modal.erro('Erro', msg);
      return false;
    }
  }

  private recalcularDescontosTodosItens() {
    const itens = this.itens();
    if (itens.length === 0) return;
    itens.forEach((item, idx) => {
      this.resolverDescontoProduto(item.produtoId, (desc) => {
        this.itens.update(lista => {
          const arr = [...lista];
          const it = { ...arr[idx] };
          it.descontoMaxPermitido = desc.descontoMaxSemSenha;
          it.componenteDesconto = desc.componente ?? undefined;
          // Se desconto atual ultrapassa o novo máximo, ajustar
          if (it.descontoMaxPermitido > 0 && it.percentualDesconto > it.descontoMaxPermitido) {
            it.percentualDesconto = it.descontoMaxPermitido;
            it.precoUnitario = Math.round(it.precoVenda * (1 - it.percentualDesconto / 100) * 100) / 100;
            it.valorDesconto = Math.round(it.precoVenda * it.quantidade * it.percentualDesconto / 100 * 100) / 100;
            it.total = Math.round(it.precoUnitario * it.quantidade * 100) / 100;
          }
          arr[idx] = it;
          return arr;
        });
      });
    });
  }

  // ── Product search ──────────────────────────────────────────────
  private avisoEscanearMostrado = false;

  onProdutoBuscaInput(valor: string) {
    // Obrigatório escanear: bloquear digitação de texto, aceitar apenas números (barcode)
    if (this.cfgObrigarEscanear() && valor.length > 0 && !/^\d+$/.test(valor)) {
      this.produtoBusca.set(valor.replace(/\D/g, ''));
      if (!this.avisoEscanearMostrado) {
        this.avisoEscanearMostrado = true;
        this.modal.aviso('Pesquisa por Código de Barras', 'A pesquisa por texto está desabilitada. Utilize o leitor de código de barras para inserir produtos. Para alterar, acesse Configurações > Venda.');
        setTimeout(() => this.avisoEscanearMostrado = false, 5000);
      }
      return;
    }
    this.produtoBusca.set(valor);
    this.produtoIndice.set(-1);
    if (this.produtoTimer) clearTimeout(this.produtoTimer);
    if (valor.trim().length < 2) {
      this.produtoResultados.set([]);
      this.produtoDropdown.set(false);
      return;
    }
    this.produtoTimer = setTimeout(() => this.buscarProdutos(valor), 300);
  }

  onProdutoAtivosChange(ativo: boolean) {
    this.produtoAtivos.set(ativo);
    const termo = this.produtoBusca();
    if (termo.trim().length >= 2) this.buscarProdutos(termo);
  }

  private buscarProdutos(termo: string) {
    this.http.get<any>(`${this.apiUrl}/produtos/buscar`, {
      params: { termo, filialId: this.filialId().toString(), status: this.produtoAtivos() ? 'ativos' : 'todos' }
    }).subscribe({
      next: r => {
        this.produtoResultados.set(r.data ?? []);
        this.produtoDropdown.set((r.data ?? []).length > 0);
      },
      error: () => this.modal.erro('Erro', 'Erro ao buscar produtos.')
    });
  }

  // ── Cores para múltiplos vendedores ─────────────────────────────
  private readonly CORES_VENDEDOR = ['#1E88E5', '#43A047', '#FB8C00', '#8E24AA', '#E53935', '#00ACC1', '#6D4C41'];
  private vendedorCoresMap = new Map<number, string>();

  corVendedor(colaboradorId?: number): string {
    if (!colaboradorId) return 'inherit';
    const ids = [...new Set(this.itens().map(i => i.colaboradorId).filter(Boolean) as number[])];
    if (ids.length <= 1) return 'inherit';
    if (!this.vendedorCoresMap.has(colaboradorId)) {
      this.vendedorCoresMap.set(colaboradorId, this.CORES_VENDEDOR[this.vendedorCoresMap.size % this.CORES_VENDEDOR.length]);
    }
    return this.vendedorCoresMap.get(colaboradorId)!;
  }

  temMultiplosVendedores(): boolean {
    const ids = new Set(this.itens().map(i => i.colaboradorId).filter(Boolean));
    return ids.size > 1;
  }

  selecionarProduto(p: ProdutoLookup) {
    if (!this.tipoPagamentoId()) {
      this.modal.aviso('Condição de Pagamento', 'Selecione uma condição de pagamento antes de inserir produtos.');
      return;
    }

    // ── Vendedor obrigatório ─────────────────────────────────────
    if (!this.colaboradorId()) {
      this.modal.aviso('Vendedor Obrigatório', 'Informe o vendedor antes de inserir produtos.');
      return;
    }

    // ── Modo conferência: bipar incrementa quantidade ──────────────
    if (this.modoConferencia()) {
      const idxExist = this.itens().findIndex(i => i.produtoId === p.id);
      if (idxExist >= 0) {
        this.itens.update(lista => {
          const arr = [...lista];
          const item = { ...arr[idxExist] };
          item.quantidade += 1;
          item.valorDesconto = Math.round(item.precoVenda * item.quantidade * (item.percentualDesconto + item.percentualPromocao) / 100 * 100) / 100;
          item.total = Math.round(item.precoUnitario * item.quantidade * 100) / 100;
          arr[idxExist] = item;
          return arr;
        });
        this.itensSelecionadoIdx.set(idxExist);
        this.produtoBusca.set('');
        this.produtoResultados.set([]);
        this.produtoDropdown.set(false);
        setTimeout(() => this.inputProdutoRef?.nativeElement?.focus(), 50);
        return;
      }
      // Produto novo (não veio da pré-venda) — adicionar como 'adicionado'
    }

    // ── Múltiplos vendedores: validar se vendedor mudou ──────────
    const vendedorAtualId = this.colaboradorId()!;
    const vendedorAtualNome = this.colaboradorNome() || '';
    if (!this.cfgMultiplosVendedores() && this.itens().length > 0) {
      const vendedorExistente = this.itens().find(i => i.colaboradorId && i.colaboradorId !== vendedorAtualId);
      if (vendedorExistente) {
        this.modal.aviso('Vendedor', 'Não é permitido ter múltiplos vendedores nesta venda. Para habilitar, acesse Configurações > Venda.');
        return;
      }
    }

    // ── Duplicar linha: se desabilitado, incrementar quantidade ──
    if (!this.cfgDuplicarLinha()) {
      const idxExistente = this.itens().findIndex(i => i.produtoId === p.id);
      if (idxExistente >= 0) {
        this.itens.update(lista => {
          const arr = [...lista];
          const item = { ...arr[idxExistente] };
          item.quantidade += 1;
          item.valorDesconto = Math.round(item.precoVenda * item.quantidade * item.percentualDesconto / 100 * 100) / 100;
          item.total = Math.round(item.precoUnitario * item.quantidade * 100) / 100;
          arr[idxExistente] = item;
          return arr;
        });
        this.itensSelecionadoIdx.set(idxExistente);
        this.produtoBusca.set('');
        this.produtoResultados.set([]);
        this.produtoDropdown.set(false);
        if (this.cfgFocarQuantidade()) {
          this.focarCelulaQuantidade(idxExistente);
        } else {
          setTimeout(() => this.inputProdutoRef?.nativeElement?.focus(), 50);
        }
        return;
      }
    }

    const novoItem: PreVendaItem = {
      produtoId: p.id,
      produtoCodigo: p.codigo,
      produtoNome: p.nome,
      fabricante: p.fabricante ?? '',
      precoVenda: p.valorVenda,
      quantidade: 1,
      percentualDesconto: 0,
      percentualPromocao: 0,
      valorDesconto: 0,
      precoUnitario: p.valorVenda,
      total: p.valorVenda,
      estoqueAtual: p.estoqueAtual,
      vendedor: vendedorAtualNome,
      colaboradorId: vendedorAtualId ?? undefined,
      unidade: p.unidade,
      descontos: [],
      classeTerapeutica: (p as any).classeTerapeutica ?? null,
      origemItem: this.modoConferencia() ? 'adicionado' : undefined,
      quantidadePrevenda: 0,
      permitirConferenciaDigitando: (p as any).permitirConferenciaDigitando ?? false
    };
    this.itens.update(lista => [...lista, novoItem]);
    const idx = this.itens().length - 1;
    this.itensSelecionadoIdx.set(idx);
    this.produtoBusca.set('');
    this.produtoResultados.set([]);
    this.produtoDropdown.set(false);

    // Resolver desconto via hierarquia
    this.resolverDescontoProduto(p.id, (desc) => {
      this.itens.update(lista => {
        const arr = [...lista];
        const item = { ...arr[idx], descontos: [...arr[idx].descontos] };
        item.descontoMaxPermitido = desc.descontoMaxSemSenha;
        item.componenteDesconto = desc.componente ?? undefined;
        if (desc.aplicarAutomatico && desc.descontoAplicar > 0) {
          item.percentualDesconto = desc.descontoAplicar;
          item.descontos.push({
            tipo: 1,
            percentual: desc.descontoAplicar,
            origem: desc.componente ?? 'Padrao',
            regra: desc.hierarquiaNome ?? 'Padrão',
            origemId: desc.hierarquiaId
          });
        }
        // Recalcula usando a SOMA de desconto manual + promoção (race-safe)
        this.recalcularItem(item, 'percentualDesconto');
        arr[idx] = item;
        return arr;
      });
    });

    // Buscar promoções ativas para o produto
    this.buscarPromocoesProduto(p.id, idx);

    // ── Focar quantidade ou voltar ao campo produto ──────────────
    if (this.cfgFocarQuantidade()) {
      this.focarCelulaQuantidade(idx);
    } else {
      setTimeout(() => this.inputProdutoRef?.nativeElement?.focus(), 50);
    }
  }

  // ── Promoções ───────────────────────────────────────────────────
  private buscarPromocoesProduto(produtoId: number, itemIdx: number) {
    const params: any = { produtoId, filialId: this.filialId().toString() };
    if (this.tipoPagamentoId()) params.tipoPagamentoId = this.tipoPagamentoId()!.toString();

    this.http.get<any>(`${this.apiUrl}/desconto-engine/promocoes`, { params }).subscribe({
      next: r => {
        const promos: PromocaoProduto[] = r.data ?? [];
        if (promos.length === 0) return;

        const fixas = promos.filter(p => p.tipo === 1);
        const progressivas = promos.filter(p => p.tipo === 2);

        // Promoção fixa
        if (fixas.length === 1) {
          this.aplicarPromocaoFixaUnica(fixas[0], itemIdx);
        } else if (fixas.length > 1) {
          if (this.cfgPromoMultiplas() === 'menor') {
            // Lançar menor preço automaticamente
            const menor = fixas.sort((a, b) => b.percentualPromocao - a.percentualPromocao)[0];
            this.aplicarPromocaoFixaUnica(menor, itemIdx);
          } else {
            // Exibir modal com as promoções para o usuário escolher
            this.promoFixasLista.set(fixas);
            this.promoFixasItemIdx.set(itemIdx);
            this.modalPromoFixas.set(true);
          }
        }

        // Promoção progressiva: abrir modal para o usuário escolher a faixa
        if (progressivas.length > 0) {
          this.promoProgressivaAtual.set(progressivas[0]);
          this.promoProgressivaItemIdx.set(itemIdx);
          this.modalPromoProgressiva.set(true);
        }
      }
    });
  }

  private aplicarPromocaoFixaUnica(promo: PromocaoProduto, itemIdx: number) {
    this.itens.update(lista => {
      const arr = [...lista];
      if (itemIdx >= arr.length) return arr;
      const item = { ...arr[itemIdx], descontos: [...arr[itemIdx].descontos] };
      item.percentualPromocao = promo.percentualPromocao;
      item.temPromocao = true;
      item.descontos.push({
        tipo: 2,
        percentual: promo.percentualPromocao,
        origem: 'PromocaoFixa',
        regra: promo.nome,
        origemId: promo.promocaoId
      });
      // Recalcula usando fórmula consistente (soma desconto + promoção)
      this.recalcularItem(item, 'percentualDesconto');
      arr[itemIdx] = item;
      return arr;
    });
  }

  selecionarPromocaoFixa(promo: PromocaoProduto) {
    this.aplicarPromocaoFixaUnica(promo, this.promoFixasItemIdx());
    this.modalPromoFixas.set(false);
    this.promoFixasLista.set([]);
  }

  fecharModalPromoFixas() {
    this.modalPromoFixas.set(false);
    this.promoFixasLista.set([]);
  }

  aplicarPromocaoProgressiva(faixa: PromocaoFaixa) {
    const promo = this.promoProgressivaAtual();
    const itemIdx = this.promoProgressivaItemIdx();
    if (!promo || itemIdx < 0) return;

    this.itens.update(lista => {
      const arr = [...lista];
      if (itemIdx >= arr.length) return arr;
      const item = { ...arr[itemIdx], descontos: [...arr[itemIdx].descontos] };
      item.quantidade = faixa.quantidade;
      item.percentualPromocao = faixa.percentualDesconto;
      item.temPromocao = true;
      this.recalcularItem(item, 'percentualDesconto');
      item.descontos.push({
        tipo: 2,
        percentual: faixa.percentualDesconto,
        origem: 'PromocaoProgressiva',
        regra: promo.nome,
        origemId: promo.promocaoId
      });
      arr[itemIdx] = item;
      return arr;
    });

    this.modalPromoProgressiva.set(false);
    this.promoProgressivaAtual.set(null);
  }

  fecharModalPromoProgressiva() {
    this.modalPromoProgressiva.set(false);
    this.promoProgressivaAtual.set(null);
  }

  calcularPrecoLiquidoFaixa(faixa: PromocaoFaixa): number {
    const idx = this.promoProgressivaItemIdx();
    const itens = this.itens();
    if (idx < 0 || idx >= itens.length) return 0;
    const item = itens[idx];
    return Math.round(item.precoVenda * (1 - faixa.percentualDesconto / 100) * 100) / 100;
  }

  // ── Carregar pré-venda no checkout (com ou sem conferência) ─────
  carregarPrevendaNoCheckout(vendaId: number, nrCesta?: string) {
    // Esperar configs carregarem se ainda não vieram
    if (!this.configsCarregadas()) {
      setTimeout(() => this.carregarPrevendaNoCheckout(vendaId, nrCesta), 300);
      return;
    }
    this.http.get<any>(`${this.apiUrl}/vendas/${vendaId}`).subscribe({
      next: r => {
        const d = r.data;
        if (!d) return;
        this.preVendaId.set(d.id);
        this.clienteId.set(d.clienteId);
        this.clienteNome.set(d.clienteNome ?? '');
        this.clienteBusca.set(d.clienteNome ?? '');
        this.colaboradorId.set(d.colaboradorId);
        this.colaboradorNome.set(d.colaboradorNome ?? '');
        this.colaboradorBusca.set(d.colaboradorNome ?? '');
        this.tipoPagamentoId.set(d.tipoPagamentoId);
        this.convenioIdCliente.set(d.convenioId);
        this.cestaNumero.set(d.nrCesta ?? nrCesta ?? '');

        const exigirConf = this.cfgExigirConferencia();
        const itens: PreVendaItem[] = (d.itens ?? []).map((i: any) => ({
          produtoId: i.produtoId,
          produtoCodigo: i.produtoCodigo,
          produtoNome: i.produtoNome,
          fabricante: i.fabricante ?? '',
          precoVenda: i.precoVenda,
          quantidade: exigirConf ? 0 : i.quantidade,
          percentualDesconto: i.percentualDesconto,
          percentualPromocao: i.percentualPromocao ?? 0,
          valorDesconto: exigirConf ? 0 : i.valorDesconto,
          precoUnitario: i.precoUnitario,
          total: exigirConf ? 0 : i.total,
          estoqueAtual: i.estoqueAtual ?? 0,
          vendedor: d.colaboradorNome ?? '',
          colaboradorId: d.colaboradorId ?? undefined,
          descontos: (i.descontos ?? []).map((dd: any) => ({
            tipo: dd.tipo, percentual: dd.percentual,
            origem: dd.origem, regra: dd.regra,
            origemId: dd.origemId, liberadoPorId: dd.liberadoPorId
          })),
          temPromocao: (i.percentualPromocao ?? 0) > 0,
          quantidadePrevenda: i.quantidade,
          origemItem: 'prevenda' as const,
          permitirConferenciaDigitando: false
        }));
        this.itens.set(itens);
        this.modoConferencia.set(exigirConf);
        this.buscarHierarquia();
        this.focarProduto();
      }
    });
  }

  // Status de conferência do item
  statusConferencia(item: PreVendaItem): 'abaixo' | 'igual' | 'acima' | 'adicionado' | null {
    if (!this.modoConferencia()) return null;
    if (item.origemItem === 'adicionado') return 'adicionado';
    const prev = item.quantidadePrevenda ?? 0;
    if (item.quantidade < prev) return 'abaixo';
    if (item.quantidade === prev) return 'igual';
    return 'acima';
  }

  // Validação antes de finalizar no modo conferência
  validarConferencia(): boolean {
    if (!this.modoConferencia()) return true;
    const itens = this.itens();
    const abaixo = itens.filter(i => i.origemItem === 'prevenda' && i.quantidade < (i.quantidadePrevenda ?? 0));
    if (abaixo.length > 0) {
      const nomes = abaixo.map(i => `${i.produtoNome} (${i.quantidade}/${i.quantidadePrevenda})`).join('\n');
      this.modal.erro('Conferência Incompleta', `Os seguintes produtos estão com quantidade abaixo da pré-venda:\n\n${nomes}\n\nFinalize a conferência antes de continuar.`);
      return false;
    }
    const acima = itens.filter(i => i.origemItem === 'prevenda' && i.quantidade > (i.quantidadePrevenda ?? 0));
    if (acima.length > 0) {
      // Será tratado com confirmação no finalizar
    }
    return true;
  }

  // Buscar pré-venda por nº da cesta
  buscarPorCesta() {
    const nr = this.cestaNumero().trim();
    if (!nr) return;
    this.http.get<any>(`${this.apiUrl}/vendas`, { params: { filialId: this.filialId().toString() } }).subscribe({
      next: r => {
        const lista = (r.data ?? []).filter((v: any) => v.status === 1 && v.nrCesta === nr);
        if (lista.length === 0) {
          this.modal.aviso('Cesta não encontrada', `Nenhuma pré-venda em aberto encontrada com a cesta "${nr}".`);
        } else {
          this.carregarPrevendaNoCheckout(lista[0].id, nr);
        }
      }
    });
  }

  private focarVendedor() {
    setTimeout(() => this.inputVendedorRef?.nativeElement?.focus(), 50);
  }

  private focarProduto() {
    setTimeout(() => this.inputProdutoRef?.nativeElement?.focus(), 50);
  }

  private focarCelulaQuantidade(idx: number) {
    setTimeout(() => {
      const row = document.querySelector(`.pv-grid tbody tr:nth-child(${idx + 1})`);
      const qtdeCell = row?.querySelector('td[data-campo="quantidade"] input') as HTMLInputElement;
      if (qtdeCell) { qtdeCell.focus(); qtdeCell.select(); }
    }, 100);
  }

  onProdutoKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') { this.produtoDropdown.set(false); return; }
    const lista = this.produtoResultados();
    if (e.key === 'ArrowDown' && lista.length > 0) {
      e.preventDefault();
      this.produtoIndice.update(i => Math.min(i + 1, lista.length - 1));
    } else if (e.key === 'ArrowUp' && lista.length > 0) {
      e.preventDefault();
      this.produtoIndice.update(i => Math.max(i - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (this.produtoTimer) clearTimeout(this.produtoTimer);
      const idx = this.produtoIndice();
      if (idx >= 0 && idx < lista.length) {
        this.selecionarProduto(lista[idx]);
      } else if (lista.length === 1) {
        this.selecionarProduto(lista[0]);
      } else if (this.produtoBusca().trim().length >= 1) {
        // Busca direta — fecha dropdown e busca imediato
        this.produtoDropdown.set(false);
        this.buscarProdutoDireto(this.produtoBusca().trim());
      }
    }
  }

  private buscarProdutoDireto(termo: string) {
    this.http.get<any>(`${this.apiUrl}/produtos/buscar`, {
      params: { termo, filialId: this.filialId().toString(), limit: '2' }
    }).subscribe({
      next: r => {
        const lista = r.data ?? [];
        if (lista.length === 1) {
          this.selecionarProduto(lista[0]);
        } else if (lista.length > 1) {
          this.produtoResultados.set(lista);
          this.produtoDropdown.set(true);
        } else {
          this.modal.aviso('Produto', `Nenhum produto encontrado para "${termo}".`);
        }
      }
    });
  }

  // ── Client search ──────────────────────────────────────────────
  onClienteBuscaInput(valor: string) {
    this.clienteBusca.set(valor);
    this.clienteIndice.set(-1);
    this.clienteExpandidoId.set(null);
    if (this.clienteId()) {
      this.clienteId.set(null);
      this.clienteNome.set('');
      this.convenioIdCliente.set(null);
      this.buscarHierarquia();
    }
    if (this.clienteTimer) clearTimeout(this.clienteTimer);
    if (valor.trim().length < 2) {
      this.clienteResultados.set([]);
      this.clienteDropdown.set(false);
      return;
    }
    this.clienteTimer = setTimeout(() => this.buscarClientes(valor), 300);
  }

  onClienteKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') { this.clienteDropdown.set(false); return; }
    const lista = this.clienteResultados();
    if (e.key === 'ArrowDown' && lista.length > 0) {
      e.preventDefault();
      this.clienteIndice.update(i => Math.min(i + 1, lista.length - 1));
    } else if (e.key === 'ArrowUp' && lista.length > 0) {
      e.preventDefault();
      this.clienteIndice.update(i => Math.max(i - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const idx = this.clienteIndice();
      if (idx >= 0 && idx < lista.length) {
        this.selecionarCliente(lista[idx]);
      } else if (lista.length === 1) {
        this.selecionarCliente(lista[0]);
      } else if (lista.length === 0) {
        this.focarVendedor();
      }
    }
  }

  onClienteAtivosChange(ativo: boolean) {
    this.clienteAtivos.set(ativo);
    const termo = this.clienteBusca();
    if (termo.trim().length >= 2) this.buscarClientes(termo);
  }

  private buscarClientes(termo: string) {
    this.http.get<any>(`${this.apiUrl}/clientes/pesquisar`, {
      params: { termo, status: this.clienteAtivos() ? 'ativos' : 'todos' }
    }).subscribe({
      next: r => {
        const lista = (r.data ?? []).map((c: any) => ({ ...c, convenios: c.convenios ?? [] }));
        this.clienteResultados.set(lista);
        this.clienteDropdown.set(lista.length > 0);
      },
      error: () => {}
    });
  }

  selecionarCliente(c: ClienteLookup) {
    if (c.convenios.length > 1 && this.clienteExpandidoId() !== c.clienteId) {
      this.clienteExpandidoId.set(c.clienteId);
      return;
    }
    this.confirmarCliente(c, c.convenios.length === 1 ? c.convenios[0] : null);
  }

  selecionarConvenio(c: ClienteLookup, conv: ConvenioLookup | null) {
    this.confirmarCliente(c, conv);
  }

  private confirmarCliente(c: ClienteLookup, conv: ConvenioLookup | null) {
    this.clienteId.set(c.clienteId);
    this.clienteNome.set(c.nome);
    this.clienteBusca.set(c.nome);
    this.convenioIdCliente.set(conv?.id ?? null);
    this.clienteResultados.set([]);
    this.clienteDropdown.set(false);
    this.clienteExpandidoId.set(null);
    this.buscarHierarquia();
    this.focarVendedor();
  }

  // ── Hierarquia de desconto ─────────────────────────────────────
  buscarHierarquia() {
    const params: string[] = [];
    if (this.clienteId()) params.push(`clienteId=${this.clienteId()}`);
    if (this.convenioIdCliente()) params.push(`convenioId=${this.convenioIdCliente()}`);
    if (this.colaboradorId()) params.push(`colaboradorId=${this.colaboradorId()}`);
    const url = `${this.apiUrl}/desconto-engine/hierarquia${params.length ? '?' + params.join('&') : ''}`;
    this.http.get<any>(url).subscribe({
      next: r => {
        if (r.data) this.hierarquiaAtiva.set(r.data);
        else this.hierarquiaAtiva.set(null);
      }
    });
  }

  resolverDescontoProduto(produtoId: number, callback: (desc: DescontoResolucao) => void) {
    const params: string[] = [`produtoId=${produtoId}`, `filialId=${this.filialId()}`];
    if (this.clienteId()) params.push(`clienteId=${this.clienteId()}`);
    if (this.convenioIdCliente()) params.push(`convenioId=${this.convenioIdCliente()}`);
    if (this.colaboradorId()) params.push(`colaboradorId=${this.colaboradorId()}`);
    if (this.tipoPagamentoId()) params.push(`tipoPagamentoId=${this.tipoPagamentoId()}`);
    this.http.get<any>(`${this.apiUrl}/desconto-engine/resolver?${params.join('&')}`).subscribe({
      next: r => { if (r.data) callback(r.data); }
    });
  }

  // ── Collaborator search ─────────────────────────────────────────
  onColaboradorBuscaInput(valor: string) {
    this.colaboradorBusca.set(valor);
    this.colaboradorIndice.set(-1);
    if (this.colaboradorId()) {
      this.colaboradorId.set(null);
      this.colaboradorNome.set('');
    }
    if (this.colaboradorTimer) clearTimeout(this.colaboradorTimer);
    if (valor.trim().length < 2) {
      this.colaboradorResultados.set([]);
      this.colaboradorDropdown.set(false);
      return;
    }
    this.colaboradorTimer = setTimeout(() => this.buscarColaboradores(valor), 300);
  }

  onColaboradorKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') { this.colaboradorDropdown.set(false); return; }
    const lista = this.colaboradorResultados();
    if (e.key === 'ArrowDown' && lista.length > 0) {
      e.preventDefault();
      this.colaboradorIndice.update(i => Math.min(i + 1, lista.length - 1));
    } else if (e.key === 'ArrowUp' && lista.length > 0) {
      e.preventDefault();
      this.colaboradorIndice.update(i => Math.max(i - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (this.colaboradorTimer) clearTimeout(this.colaboradorTimer);
      const idx = this.colaboradorIndice();
      if (idx >= 0 && idx < lista.length) {
        this.selecionarColaborador(lista[idx]);
      } else if (lista.length === 1) {
        this.selecionarColaborador(lista[0]);
      } else if (this.colaboradorId()) {
        // Já tem vendedor selecionado, pula pro produto
        this.colaboradorDropdown.set(false);
        this.focarProduto();
      } else if (this.colaboradorBusca().trim().length >= 1) {
        // Busca direta e seleciona se encontrar 1
        this.colaboradorDropdown.set(false);
        this.buscarColaboradorDireto(this.colaboradorBusca().trim());
      }
    }
  }

  private buscarColaboradorDireto(termo: string) {
    this.http.get<any>(`${this.apiUrl}/colaboradores/pesquisar`, {
      params: { termo, limit: '2' }
    }).subscribe({
      next: r => {
        const lista = r.data ?? [];
        if (lista.length === 1) {
          this.selecionarColaborador(lista[0]);
        } else if (lista.length > 1) {
          this.colaboradorResultados.set(lista);
          this.colaboradorDropdown.set(true);
        } else {
          this.modal.aviso('Vendedor', `Nenhum vendedor encontrado para "${termo}".`);
        }
      }
    });
  }

  onColaboradorAtivosChange(ativo: boolean) {
    this.colaboradorAtivos.set(ativo);
    const termo = this.colaboradorBusca();
    if (termo.trim().length >= 2) this.buscarColaboradores(termo);
  }

  private buscarColaboradores(termo: string) {
    this.http.get<any>(`${this.apiUrl}/colaboradores/pesquisar`, {
      params: { termo, status: this.colaboradorAtivos() ? 'ativos' : 'todos' }
    }).subscribe({
      next: r => {
        this.colaboradorResultados.set(r.data ?? []);
        this.colaboradorDropdown.set((r.data ?? []).length > 0);
      },
      error: () => {}
    });
  }

  selecionarColaborador(c: ColaboradorLookup) {
    this.colaboradorId.set(c.id);
    this.colaboradorNome.set(c.nome);
    this.colaboradorBusca.set(c.nome);
    this.colaboradorResultados.set([]);
    this.colaboradorDropdown.set(false);
    this.focarProduto();
  }

  // ── Grid: sort ──────────────────────────────────────────────────
  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string {
    return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '\u25B2' : '\u25BC') : '\u21C5';
  }

  // ── Grid: columns ──────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return PREVENDA_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return PREVENDA_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(PREVENDA_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasStorage();
  }

  // ── Grid: resize ───────────────────────────────────────────────
  iniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    this.resizeState = { campo, startX: e.clientX, startWidth: largura };
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  // ── Opções ─────────────────────────────────────────────────────
  menuOpcoesAberto = signal(false);

  @HostListener('document:keydown', ['$event'])
  onKeydown(e: KeyboardEvent) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
      e.preventDefault();
      this.novaAba();
    }
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    if (!this.resizeState) return;
    const delta = e.clientX - this.resizeState.startX;
    const def = PREVENDA_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── Grid: drag-drop columns ────────────────────────────────────
  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => {
      const arr = [...cols];
      const [moved] = arr.splice(this.dragColIdx!, 1);
      arr.splice(idx, 0, moved);
      this.dragColIdx = idx;
      return arr;
    });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunasStorage(); }

  // ── Grid: cell value ───────────────────────────────────────────
  getCellValue(item: PreVendaItem, campo: string): string {
    if (campo === 'percentualDesconto') {
      const total = item.percentualDesconto + (item.percentualPromocao || 0);
      return this.formatarNumero(total);
    }
    if (campo === 'quantidade') return String(Math.floor(item.quantidade));
    const v = (item as any)[campo];
    if (v === null || v === undefined) return '';
    if (typeof v === 'number') return this.formatarNumero(v);
    return String(v);
  }

  private formatarNumero(v: number): string {
    return v.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  // ── Grid: row selection ────────────────────────────────────────
  selecionarItem(idx: number) {
    this.itensSelecionadoIdx.set(idx);
  }

  // ── Grid: editable cells ───────────────────────────────────────
  isEditavel(campo: string): boolean {
    const def = PREVENDA_COLUNAS.find(c => c.campo === campo);
    return def?.editavel ?? false;
  }

  isCelulaEditavel(item: PreVendaItem, campo: string): boolean {
    // Quando promoção aplicada e config bloqueia alteração
    if (item.temPromocao && !this.cfgAlterarPrecoPromo()) {
      if (['precoVenda', 'percentualDesconto', 'precoUnitario', 'total'].includes(campo)) return false;
    }
    // Modo conferência + somente barras: bloqueia quantidade (exceto produtos com flag)
    if (this.modoConferencia() && this.cfgConferenciaSoBarras() && campo === 'quantidade') {
      if (!item.permitirConferenciaDigitando) return false;
    }
    return true;
  }

  onCellKeydown(e: KeyboardEvent, idx: number, campo: string) {
    if (e.key !== 'Enter') return;
    e.preventDefault();
    // Aplicar o valor atual antes de navegar
    this.onCellEdit(idx, campo, e);
    const ordem = ['quantidade', 'percentualDesconto', 'precoUnitario', 'total'];
    const posAtual = ordem.indexOf(campo);
    if (posAtual >= 0 && posAtual < ordem.length - 1) {
      const prox = ordem[posAtual + 1];
      const row = (e.target as HTMLElement).closest('tr');
      const nextInput = row?.querySelector(`td[data-campo="${prox}"] input`) as HTMLInputElement;
      if (nextInput) { nextInput.focus(); nextInput.select(); }
    } else {
      setTimeout(() => this.inputProdutoRef?.nativeElement?.focus(), 50);
    }
  }

  onCellEdit(idx: number, campo: string, event: Event) {
    const valor = (event.target as HTMLInputElement).value;
    const num = this.parseNumero(valor);
    if (isNaN(num) || num < 0) return;

    this.itens.update(lista => {
      const arr = [...lista];
      const item = { ...arr[idx] };
      (item as any)[campo] = campo === 'quantidade' ? Math.floor(num) : num;
      this.recalcularItem(item, campo);

      // Validar desconto máximo da hierarquia
      if (item.descontoMaxPermitido != null && item.descontoMaxPermitido > 0 && item.percentualDesconto > item.descontoMaxPermitido) {
        item.percentualDesconto = item.descontoMaxPermitido;
        // Recalcular com o desconto limitado
        this.recalcularItem(item, 'percentualDesconto');
        this.modal.aviso('Desconto limitado', `Desconto máximo permitido: ${item.descontoMaxPermitido}%. O valor foi ajustado automaticamente.`);
      }

      arr[idx] = item;
      return arr;
    });
  }

  private parseNumero(valor: string): number {
    const limpo = valor.replace(/\./g, '').replace(',', '.');
    return parseFloat(limpo);
  }

  private recalcularItem(item: PreVendaItem, campoAlterado: string) {
    const r = (v: number) => Math.round(v * 100) / 100;
    // Fórmula consistente (igual o backend): total = bruto - desconto
    const recalcFromPercent = () => {
      const percTotal = (item.percentualDesconto || 0) + (item.percentualPromocao || 0);
      const valorBruto = r(item.precoVenda * item.quantidade);
      item.valorDesconto = r(valorBruto * percTotal / 100);
      item.total = r(valorBruto - item.valorDesconto);
      item.precoUnitario = item.quantidade > 0 ? r(item.total / item.quantidade) : 0;
    };

    switch (campoAlterado) {
      case 'precoVenda':
      case 'quantidade':
      case 'percentualDesconto':
        recalcFromPercent();
        break;

      case 'precoUnitario':
        // Recalcula % desconto (manual) e total a partir do preço unitário
        // Preserva a promoção, ajusta apenas o desconto manual
        {
          const valorBruto = r(item.precoVenda * item.quantidade);
          const totalNovo = r(item.precoUnitario * item.quantidade);
          const descontoTotal = r(valorBruto - totalNovo);
          const valorPromo = r(valorBruto * (item.percentualPromocao || 0) / 100);
          const valorDescManual = Math.max(0, descontoTotal - valorPromo);
          item.percentualDesconto = item.precoVenda > 0 ? r(valorDescManual / valorBruto * 100) : 0;
          item.valorDesconto = descontoTotal;
          item.total = totalNovo;
        }
        break;

      case 'total':
        // Recalcula unitário e % manual a partir do total
        {
          item.precoUnitario = item.quantidade > 0 ? r(item.total / item.quantidade) : 0;
          const valorBruto = r(item.precoVenda * item.quantidade);
          const descontoTotal = r(valorBruto - item.total);
          const valorPromo = r(valorBruto * (item.percentualPromocao || 0) / 100);
          const valorDescManual = Math.max(0, descontoTotal - valorPromo);
          item.percentualDesconto = item.precoVenda > 0 ? r(valorDescManual / valorBruto * 100) : 0;
          item.valorDesconto = descontoTotal;
        }
        break;

      default:
        recalcFromPercent();
        break;
    }
  }

  getEditValue(item: PreVendaItem, campo: string): string {
    if (campo === 'quantidade') return String(Math.floor(item.quantidade));
    const v = (item as any)[campo];
    if (v === null || v === undefined) return '';
    if (typeof v === 'number') return this.formatarNumero(v);
    return String(v);
  }

  // ── Actions ─────────────────────────────────────────────────────
  async limpar() {
    if (this.itens().length === 0) return;
    const resultado = await this.modal.confirmar(
      'Limpar Pre-Venda',
      'Deseja limpar todos os itens e campos da pre-venda atual?',
      'Sim, limpar',
      'Nao'
    );
    if (!resultado.confirmado) return;
    this.resetTudo();
  }

  private resetTudo() {
    this.preVendaId.set(null);
    this.itens.set([]);
    this.itensSelecionadoIdx.set(null);
    this.etapaVenda.set('venda');
    this.sngpcVendaBody = null;
    this.sngpcFinalizarBody = null;
    this.clienteId.set(null);
    this.clienteNome.set('');
    this.clienteBusca.set('');
    this.colaboradorId.set(null);
    this.colaboradorNome.set('');
    this.colaboradorBusca.set('');
    this.cestaNumero.set('');
    this.produtoBusca.set('');
    // Selecionar DINHEIRO (primeiro tipo de pagamento)
    const tipos = this.tiposPagamento();
    this.tipoPagamentoId.set(tipos.length > 0 ? tipos[0].id : null);
    this.salvarEstado();
    this.focarCliente();
  }

  cancelarAlteracao() {
    this.resetTudo();
  }

  editando(): boolean {
    return this.preVendaId() !== null;
  }

  labelTarja(): string {
    if (this.modoConferencia()) return 'Conferência';
    return this.preVendaId() ? 'Finalizando Pré-Venda' : '';
  }

  eliminar() {
    const idx = this.itensSelecionadoIdx();
    if (idx === null) return;
    this.itens.update(lista => {
      const arr = [...lista];
      arr.splice(idx, 1);
      return arr;
    });
    const novoTotal = this.itens().length;
    if (novoTotal === 0) {
      this.itensSelecionadoIdx.set(null);
    } else {
      this.itensSelecionadoIdx.set(Math.min(idx, novoTotal - 1));
    }
  }

  async finalizar() {
    if (this.itens().length === 0) {
      await this.modal.aviso('Sem Itens', 'Adicione pelo menos um produto antes de finalizar.');
      return;
    }
    if (!this.tipoPagamentoId()) {
      await this.modal.aviso('Pagamento', 'Selecione um tipo de pagamento.');
      return;
    }

    // Validar plano de contas no tipo de pagamento
    const tipoPag = this.tiposPagamento().find(t => t.id === this.tipoPagamentoId());
    if (tipoPag && !tipoPag.planoContaId) {
      await this.modal.erro('Plano de Contas', `O tipo de pagamento "${tipoPag.nome}" não possui plano de contas configurado. Acesse o cadastro de Tipos de Pagamento e configure o plano de contas.`);
      return;
    }

    // Validar conferência
    if (this.modoConferencia()) {
      const itens = this.itens();
      const abaixo = itens.filter(i => i.origemItem === 'prevenda' && i.quantidade < (i.quantidadePrevenda ?? 0));
      if (abaixo.length > 0) {
        const nomes = abaixo.map(i => `• ${i.produtoNome} (conferido: ${i.quantidade} / esperado: ${i.quantidadePrevenda})`).join('\n');
        await this.modal.erro('Conferência Incompleta', `Produtos com quantidade abaixo da pré-venda:\n\n${nomes}\n\nFinalize a conferência de todos os produtos.`);
        return;
      }
      const acima = itens.filter(i => i.origemItem === 'prevenda' && i.quantidade > (i.quantidadePrevenda ?? 0));
      if (acima.length > 0) {
        const nomes = acima.map(i => `• ${i.produtoNome} (conferido: ${i.quantidade} / esperado: ${i.quantidadePrevenda})`).join('\n');
        const resultado = await this.modal.confirmar('Quantidade Acima', `Os seguintes produtos têm quantidade acima da pré-venda:\n\n${nomes}\n\nDeseja continuar?`, 'Sim, continuar', 'Cancelar');
        if (!resultado.confirmado) return;
      }
    }

    // Abrir modal de desdobramento de pagamento
    this.abrirModalPagamento();
  }

  abrirModalPagamento() {
    const tipos = this.tiposPagamento();
    this.pagamentoValores.set(tipos.map(t => ({
      tipoPagamentoId: t.id,
      nome: t.nome,
      valor: 0,
      modalidade: (t as any).modalidade ?? 0
    })));
    // Se já tem tipo selecionado, pré-preencher com o total
    const total = this.totalLiquido();
    const selecionado = this.tipoPagamentoId();
    if (selecionado) {
      this.pagamentoValores.update(vals => vals.map(v =>
        v.tipoPagamentoId === selecionado ? { ...v, valor: total } : v
      ));
    }
    this.modalPagamento.set(true);
  }

  pagamentoTotal(): number {
    return this.pagamentoValores().reduce((s, v) => s + v.valor, 0);
  }

  pagamentoTrocoFalta(): { tipo: 'troco' | 'falta' | 'ok'; valor: number } {
    const total = this.totalLiquido();
    const pago = this.pagamentoTotal();
    if (Math.abs(pago - total) < 0.01) return { tipo: 'ok', valor: 0 };
    if (pago > total) return { tipo: 'troco', valor: Math.round((pago - total) * 100) / 100 };
    return { tipo: 'falta', valor: Math.round((total - pago) * 100) / 100 };
  }

  // Verifica troco/falta para campos à vista (modalidade 1 = Venda à Vista)
  dinheiroTrocoFalta(): { tipo: 'troco' | 'falta' | 'ok'; valor: number } | null {
    const dinheiroItem = this.pagamentoValores().find(v => v.modalidade === 1);
    if (!dinheiroItem || dinheiroItem.valor === 0) return null;
    const total = this.totalLiquido();
    const outrosValores = this.pagamentoValores().filter(v => v.modalidade !== 1).reduce((s, v) => s + v.valor, 0);
    const restante = total - outrosValores;
    if (dinheiroItem.valor > restante) return { tipo: 'troco', valor: Math.round((dinheiroItem.valor - restante) * 100) / 100 };
    if (dinheiroItem.valor < restante) return { tipo: 'falta', valor: Math.round((restante - dinheiroItem.valor) * 100) / 100 };
    return { tipo: 'ok', valor: 0 };
  }

  atualizarValorPagamento(tipoPagamentoId: number, valor: string) {
    const num = parseFloat(valor.replace(',', '.')) || 0;
    this.pagamentoValores.update(vals => vals.map(v =>
      v.tipoPagamentoId === tipoPagamentoId ? { ...v, valor: Math.round(num * 100) / 100 } : v
    ));
  }

  confirmarPagamento() {
    const tf = this.pagamentoTrocoFalta();
    if (tf.tipo === 'falta') {
      this.modal.aviso('Valor Insuficiente', `Falta R$ ${tf.valor.toLocaleString('pt-BR', { minimumFractionDigits: 2 })} para completar o pagamento.`);
      return;
    }
    // Se tem pagamento com cartão, abrir modal de dados
    const linhaCartao = this.pagamentoValores().find(p => p.modalidade === 2 && p.valor > 0);
    if (linhaCartao) {
      if (this.adquirentes().length === 0) {
        this.modal.aviso('Sem Adquirentes', 'Nenhum adquirente cadastrado. Cadastre em Outros > Adquirentes antes de usar pagamento com cartão.');
        return;
      }
      this.modalPagamento.set(false);
      this.cartaoTotalAlvo.set(linhaCartao.valor);
      this.cartaoErro.set('');
      // Inicializa com 1 cartão vazio com valor total
      this.cartoes.set([this.novoCartaoItem(linhaCartao.valor)]);
      this.modalCartao.set(true);
      return;
    }
    this.modalPagamento.set(false);
    this.executarFinalizacao();
  }

  private novoCartaoItem(valor: number): CartaoItem {
    return {
      id: Math.random().toString(36).slice(2),
      adquirenteId: null,
      bandeiraId: null,
      modalidade: null,
      modalidadeDescricao: '',
      valor,
      autorizacao: ''
    };
  }

  carregarAdquirentes() {
    this.http.get<any>(`${this.apiUrl}/adquirentes`).subscribe({
      next: r => {
        const ativos = (r.data ?? []).filter((a: any) => a.ativo);
        // Precisa buscar detalhes de cada para ter bandeiras/tarifas
        const ids: number[] = ativos.map((a: any) => a.id);
        if (ids.length === 0) { this.adquirentes.set([]); return; }
        Promise.all(ids.map(id => this.http.get<any>(`${this.apiUrl}/adquirentes/${id}`).toPromise()))
          .then(results => {
            const lista: AdqLookup[] = results.map((r: any) => ({
              id: r.data.id,
              nome: r.data.nome,
              bandeiras: (r.data.bandeiras ?? []).map((b: any) => ({
                id: b.id,
                bandeira: b.bandeira,
                tarifas: (b.tarifas ?? []).map((t: any) => ({
                  id: t.id,
                  modalidade: t.modalidade,
                  modalidadeDescricao: t.modalidadeDescricao,
                  tarifa: t.tarifa,
                  prazoRecebimento: t.prazoRecebimento,
                  contaBancariaId: t.contaBancariaId
                }))
              }))
            }));
            this.adquirentes.set(lista);
          })
          .catch(() => this.adquirentes.set([]));
      },
      error: () => this.adquirentes.set([])
    });
  }

  // ── Helpers da modal de cartão ──────────────────────────────────
  adquirentePorId(id: number | null): AdqLookup | null {
    if (!id) return null;
    return this.adquirentes().find(a => a.id === id) || null;
  }

  bandeirasDoAdquirente(adqId: number | null): AdqBandeira[] {
    const a = this.adquirentePorId(adqId);
    return a?.bandeiras ?? [];
  }

  tarifasDaBandeira(adqId: number | null, bandeiraId: number | null): AdqTarifa[] {
    const bs = this.bandeirasDoAdquirente(adqId);
    const b = bs.find(x => x.id === bandeiraId);
    return b?.tarifas ?? [];
  }

  selecionarAdquirente(cartaoId: string, adqId: number) {
    this.cartoes.update(lista => lista.map(c =>
      c.id === cartaoId ? { ...c, adquirenteId: adqId, bandeiraId: null, modalidade: null, modalidadeDescricao: '' } : c
    ));
  }

  selecionarBandeira(cartaoId: string, bandeiraId: number, adquirenteId: number) {
    this.cartoes.update(lista => lista.map(c =>
      c.id === cartaoId ? { ...c, adquirenteId, bandeiraId, modalidade: null, modalidadeDescricao: '' } : c
    ));
  }

  selecionarModalidade(cartaoId: string, modalidade: number, descricao: string) {
    this.cartoes.update(lista => lista.map(c =>
      c.id === cartaoId ? { ...c, modalidade, modalidadeDescricao: descricao } : c
    ));
  }

  atualizarValorCartao(cartaoId: string, valor: string) {
    const num = parseFloat(valor.replace(',', '.')) || 0;
    this.cartoes.update(lista => lista.map(c =>
      c.id === cartaoId ? { ...c, valor: Math.round(num * 100) / 100 } : c
    ));
  }

  atualizarAutCartao(cartaoId: string, aut: string) {
    this.cartoes.update(lista => lista.map(c =>
      c.id === cartaoId ? { ...c, autorizacao: aut } : c
    ));
  }

  adicionarCartao() {
    const falta = this.cartaoFalta();
    this.cartoes.update(lista => [...lista, this.novoCartaoItem(falta > 0 ? falta : 0)]);
  }

  removerCartao(cartaoId: string) {
    if (this.cartoes().length <= 1) return;
    this.cartoes.update(lista => lista.filter(c => c.id !== cartaoId));
  }

  confirmarCartao() {
    const lista = this.cartoes();
    this.cartaoErro.set('');

    for (const c of lista) {
      if (!c.bandeiraId) { this.cartaoErro.set('Selecione a bandeira em todos os cartões.'); return; }
      if (c.modalidade == null) { this.cartaoErro.set('Selecione a modalidade em todos os cartões.'); return; }
      if (!c.valor || c.valor <= 0) { this.cartaoErro.set('Informe o valor em todos os cartões.'); return; }
      if (!c.autorizacao.trim()) { this.cartaoErro.set('Informe o código de autorização (NSU) em todos os cartões.'); return; }
    }

    if (Math.abs(this.cartaoFalta()) >= 0.01) {
      this.cartaoErro.set(`Total informado diferente do valor: falta R$ ${this.cartaoFalta().toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`);
      return;
    }

    this.modalCartao.set(false);
    this.executarFinalizacao();
  }

  cancelarCartao() {
    this.modalCartao.set(false);
    this.modalPagamento.set(true);
  }

  cancelarPagamento() {
    this.modalPagamento.set(false);
  }

  private executarFinalizacao() {
    this.salvando.set(true);

    const body: any = {
      filialId: this.filialId(),
      caixaId: this.caixaId ?? null,
      clienteId: this.clienteId(),
      colaboradorId: this.colaboradorId(),
      tipoPagamentoId: this.tipoPagamentoId(),
      convenioId: this.convenioIdCliente(),
      nrCesta: this.cestaNumero() || null,
      origem: 2,
      itens: this.itens().map(i => ({
        produtoId: i.produtoId,
        produtoCodigo: i.produtoCodigo,
        produtoNome: i.produtoNome,
        fabricante: i.fabricante,
        precoVenda: i.precoVenda,
        quantidade: i.quantidade,
        percentualDesconto: i.percentualDesconto,
        percentualPromocao: i.percentualPromocao,
        valorDesconto: i.valorDesconto,
        precoUnitario: i.precoUnitario,
        total: i.total,
        descontos: i.descontos
      })),
      pagamentos: this.pagamentoValores().filter(p => p.valor > 0).flatMap((p): any[] => {
        const tf = this.dinheiroTrocoFalta();
        if (p.modalidade === 2) {
          // Cartão: quebra em N linhas (uma por cartão da modal)
          return this.cartoes().filter(c => c.valor > 0).map(c => {
            const bandeiraObj = this.bandeirasDoAdquirente(c.adquirenteId).find(b => b.id === c.bandeiraId);
            const codigoBandeira = bandeiraObj ? (this.bandeiraCodigo[bandeiraObj.bandeira.toUpperCase()] || '99') : '99';
            const cartaoTipo = c.modalidade === 1 ? 1 : 2; // 1=Debito, 2=Credito (parcelado também vai como crédito)
            return {
              tipoPagamentoId: p.tipoPagamentoId,
              valor: c.valor,
              troco: 0,
              trocoPara: null,
              cartaoBandeira: codigoBandeira,
              cartaoTipo,
              cartaoAutorizacao: c.autorizacao,
              cartaoCnpjCredenciadora: null
            };
          });
        }
        return [{
          tipoPagamentoId: p.tipoPagamentoId,
          valor: p.valor,
          troco: (p.modalidade === 1 && tf?.tipo === 'troco') ? tf.valor : 0,
          trocoPara: (p.modalidade === 1 && tf?.tipo === 'troco') ? 'Dinheiro' : null,
          cartaoBandeira: null,
          cartaoTipo: null,
          cartaoAutorizacao: null,
          cartaoCnpjCredenciadora: null
        }];
      })
    };

    // Base do payload de finalização
    const finalizarBody: any = {};
    if (this.senhaClientePrazo) finalizarBody.senhaCliente = this.senhaClientePrazo;
    if (this.tokenLiberacaoCredito) finalizarBody.tokenLiberacaoCredito = this.tokenLiberacaoCredito;
    if (this.prazoNumeroParcelas() > 1) finalizarBody.numeroParcelas = this.prazoNumeroParcelas();

    // ── Se tem controlados E SNGPC ativo E modo ≠ NaoLancar: abre tela inline de receitas ──
    if (this.precisaTelaSngpc()) {
      // Guarda os payloads para usar só quando o usuário clicar Finalizar na tela de receitas.
      // Ainda NÃO salvamos nada no banco.
      this.sngpcVendaBody = body;
      this.sngpcFinalizarBody = finalizarBody;

      // Chama o preview do backend para obter lotes disponíveis.
      const previewPayload = {
        filialId: this.filialId(),
        itens: this.itens().map(i => ({ produtoId: i.produtoId, quantidade: i.quantidade }))
      };
      this.http.post<any>(`${this.apiUrl}/sngpc/vendas/itens-controlados-preview`, previewPayload).subscribe({
        next: (resp: any) => {
          const controlados = resp.data ?? [];
          this.salvando.set(false);
          if (controlados.length === 0) {
            // Fail-safe: nenhum controlado no preview (divergiu do cart) → segue direto
            this.finalizarFluxoNormal(body, finalizarBody);
            return;
          }
          // Usa produtoId como chave temporária (vendaItemId=0 vem zerado)
          const itensComChave = controlados.map((c: any) => ({ ...c, vendaItemId: c.produtoId }));
          this.modalPagamento.set(false);
          this.etapaVenda.set('sngpc');
          // aguarda o @ViewChild ficar disponível (o componente está sempre montado com [hidden])
          setTimeout(() => this.sngpcScreenRef?.atualizarItensExternos(itensComChave), 0);
        },
        error: (e: any) => {
          this.salvando.set(false);
          this.modal.erro('Erro', e?.error?.message || 'Erro ao carregar preview SNGPC.');
        }
      });
      return;
    }

    this.finalizarFluxoNormal(body, finalizarBody);
  }

  /** Fluxo de finalização sem SNGPC (ou modo NaoLancar): salva venda + finaliza. */
  private finalizarFluxoNormal(body: any, finalizarBody: any) {
    this.salvando.set(true);
    const salvar$ = this.preVendaId()
      ? this.http.put<any>(`${this.apiUrl}/vendas/${this.preVendaId()}`, body)
      : this.http.post<any>(`${this.apiUrl}/vendas`, body);

    salvar$.subscribe({
      next: (r: any) => {
        const id = this.preVendaId() ?? r.data?.id;
        if (!id) {
          this.salvando.set(false);
          this.modal.erro('Erro', 'Erro ao salvar venda.');
          return;
        }
        this.executarFinalizarHttp(id, finalizarBody);
      },
      error: (err: any) => {
        this.salvando.set(false);
        const msg = err?.error?.message || 'Erro ao salvar venda.';
        this.modal.erro('Erro', msg);
      }
    });
  }

  private executarFinalizarHttp(id: number, finalizarBody: any) {
    this.salvando.set(true);
    this.http.post<any>(`${this.apiUrl}/vendas/${id}/finalizar`, finalizarBody).subscribe({
          next: () => {
            this.modoConferencia.set(false);
            // Emitir NFC-e
            this.emitindoNfce.set(true);
            this.http.post<any>(`${this.apiUrl}/venda-fiscal/emitir-nfce/${id}`, {}).subscribe({
              next: (nfceRes: any) => {
                this.emitindoNfce.set(false);
                this.salvando.set(false);
                if (nfceRes.success && nfceRes.data?.autorizada) {
                  // Abrir DANFE na modal (resetTudo será chamado ao fechar)
                  const nfceId = nfceRes.data.vendaFiscalId;
                  if (nfceId) {
                    this.abrirDanfe(nfceId);
                  } else {
                    this.resetTudo();
                  }
                } else {
                  const motivo = nfceRes.data?.motivoStatus ?? nfceRes.message ?? 'Erro desconhecido';
                  this.modal.aviso('Venda Finalizada — NFC-e Pendente', `A venda foi finalizada, mas a NFC-e não foi autorizada:\n\n${motivo}\n\nVocê poderá reemitir posteriormente.`);
                  this.resetTudo();
                }
              },
              error: (nfceErr: any) => {
                this.emitindoNfce.set(false);
                this.salvando.set(false);
                const msg = nfceErr?.error?.message || 'Erro ao emitir NFC-e';
                this.modal.aviso('Venda Finalizada — NFC-e com Erro', `A venda foi finalizada com sucesso, mas houve erro na emissão da NFC-e:\n\n${msg}\n\nVocê poderá reemitir posteriormente.`);
                this.resetTudo();
              }
            });
          },
      error: () => {
        this.salvando.set(false);
        this.modal.erro('Erro', 'Erro ao finalizar venda.');
      }
    });
  }

  // ── SNGPC handlers (tela inline do fluxo Avançar) ──────────────
  /** Clicou "Finalizar Venda" dentro da tela SNGPC. Agora sim salva e finaliza. */
  onSngpcConfirmado(ev: { receitas: any[]; lancarDepois: boolean }) {
    const body = this.sngpcVendaBody;
    const finalizarBase = this.sngpcFinalizarBody ?? {};
    if (!body) return;

    this.salvando.set(true);

    const salvar$ = this.preVendaId()
      ? this.http.put<any>(`${this.apiUrl}/vendas/${this.preVendaId()}`, body)
      : this.http.post<any>(`${this.apiUrl}/vendas`, body);

    salvar$.subscribe({
      next: (r: any) => {
        const id = this.preVendaId() ?? r.data?.id;
        const itensSalvos: Array<{ id: number; produtoId: number }> = r.data?.itens ?? [];
        if (!id) {
          this.salvando.set(false);
          this.modal.erro('Erro', 'Erro ao salvar venda.');
          return;
        }
        // Mapeia produtoId (chave temporária) → vendaItemId real
        const mapa = new Map<number, number>();
        for (const it of itensSalvos) mapa.set(it.produtoId, it.id);

        const receitasMapeadas = ev.receitas.map(rec => ({
          ...rec,
          itens: rec.itens.map((it: any) => ({
            ...it,
            vendaItemId: mapa.get(it.vendaItemId) ?? it.vendaItemId
          }))
        }));

        const finalizarBody = {
          ...finalizarBase,
          sngpc: { receitas: receitasMapeadas, lancarDepois: ev.lancarDepois }
        };

        // Limpa estado SNGPC e volta a etapa pro final do fluxo
        this.sngpcVendaBody = null;
        this.sngpcFinalizarBody = null;
        this.etapaVenda.set('venda');

        this.executarFinalizarHttp(id, finalizarBody);
      },
      error: (err: any) => {
        this.salvando.set(false);
        const msg = err?.error?.message || 'Erro ao salvar venda.';
        this.modal.erro('Erro', msg);
      }
    });
  }

  /** Clicou "Voltar" dentro da tela SNGPC — retorna ao cart preservando receitas digitadas. */
  onSngpcVoltar() {
    this.etapaVenda.set('venda');
    // sngpcVendaBody e receitas ficam preservados em memória para o próximo Avançar
  }


  /** Busca os movimentos gerados pela venda e imprime canhotos (exceto dinheiro). */
  private imprimirCanhotosVenda(vendaId: number) {
    this.http.get<any>(`${this.apiUrl}/caixamovimentos/venda/${vendaId}`).subscribe({
      next: r => {
        const movs = ((r.data ?? []) as any[]).filter(m => m.modalidadePagamento !== 1); // não imprime dinheiro
        movs.forEach((m, idx) => {
          setTimeout(() => this.imprimirCanhotoMov(m.id), idx * 400);
        });
      },
      error: () => {}
    });
  }

  private imprimirCanhotoMov(movId: number) {
    this.http.get(`${this.apiUrl}/caixamovimentos/${movId}/canhoto`, { responseType: 'text' }).subscribe({
      next: (html: string) => {
        const win = window.open('', '_blank', 'width=400,height=700');
        if (win) { win.document.open(); win.document.write(html); win.document.close(); }
      },
      error: () => {}
    });
  }

  pendentes() {
    this.pendentesLoading.set(true);
    this.modalPendentes.set(true);
    this.http.get<any>(`${this.apiUrl}/vendas`, { params: { filialId: this.filialId().toString() } }).subscribe({
      next: r => {
        const lista = (r.data ?? []).filter((v: any) => v.status === 1); // Aberta = 1
        this.vendasPendentes.set(lista);
        this.pendentesLoading.set(false);
      },
      error: () => {
        this.pendentesLoading.set(false);
        this.modal.erro('Erro', 'Erro ao carregar vendas pendentes.');
      }
    });
  }

  selecionarPendente(venda: any) {
    this.pendenteSelecionada.set(venda);
    this.pendenteItensLoading.set(true);
    this.http.get<any>(`${this.apiUrl}/vendas/${venda.id}`).subscribe({
      next: r => {
        this.pendenteItens.set(r.data?.itens ?? []);
        this.pendenteItensLoading.set(false);
      },
      error: () => this.pendenteItensLoading.set(false)
    });
  }

  pendenteTotalItens(): number {
    return this.pendenteItens().reduce((s, i) => s + (i.quantidade ?? 0), 0);
  }

  pendenteTotalValor(): number {
    return this.pendenteItens().reduce((s, i) => s + (i.total ?? 0), 0);
  }

  abrirPendente(venda: any) {
    this.modalPendentes.set(false);
    this.http.get<any>(`${this.apiUrl}/vendas/${venda.id}`).subscribe({
      next: r => {
        const d = r.data;
        if (!d) return;
        this.preVendaId.set(d.id);
        this.clienteId.set(d.clienteId);
        this.clienteNome.set(d.clienteNome ?? '');
        this.clienteBusca.set(d.clienteNome ?? '');
        this.colaboradorId.set(d.colaboradorId);
        this.colaboradorNome.set(d.colaboradorNome ?? '');
        this.colaboradorBusca.set(d.colaboradorNome ?? '');
        this.tipoPagamentoId.set(d.tipoPagamentoId);
        this.convenioIdCliente.set(d.convenioId);
        this.cestaNumero.set(d.nrCesta ?? '');
        const itens: PreVendaItem[] = (d.itens ?? []).map((i: any) => ({
          produtoId: i.produtoId,
          produtoCodigo: i.produtoCodigo,
          produtoNome: i.produtoNome,
          fabricante: i.fabricante ?? '',
          precoVenda: i.precoVenda,
          quantidade: i.quantidade,
          percentualDesconto: i.percentualDesconto,
          percentualPromocao: i.percentualPromocao ?? 0,
          valorDesconto: i.valorDesconto,
          precoUnitario: i.precoUnitario,
          total: i.total,
          estoqueAtual: i.estoqueAtual ?? 0,
          vendedor: '',
          descontos: (i.descontos ?? []).map((dd: any) => ({
            tipo: dd.tipo, percentual: dd.percentual,
            origem: dd.origem, regra: dd.regra,
            origemId: dd.origemId, liberadoPorId: dd.liberadoPorId
          })),
          temPromocao: (i.percentualPromocao ?? 0) > 0
        }));
        this.itens.set(itens);
        this.buscarHierarquia();
        this.focarCliente();
      }
    });
  }

  fecharPendentes() {
    this.modalPendentes.set(false);
    this.pendenteSelecionada.set(null);
    this.pendenteItens.set([]);
  }

  // ── Grid pendentes: utilitários ────────────────────────────────
  private carregarColsPend(key: string, defs: ColunaDef[]): ColunaEstado[] {
    try {
      const json = localStorage.getItem(key);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return defs.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return defs.map(c => ({ ...c, visivel: c.padrao }));
  }

  pendOrdenar(colSignal: ReturnType<typeof signal<string>>, dirSignal: ReturnType<typeof signal<'asc' | 'desc'>>, campo: string) {
    if (colSignal() === campo) dirSignal.set(dirSignal() === 'asc' ? 'desc' : 'asc');
    else { colSignal.set(campo); dirSignal.set('asc'); }
  }

  pendSortIcon(colSignal: ReturnType<typeof signal<string>>, dirSignal: ReturnType<typeof signal<'asc' | 'desc'>>, campo: string): string {
    if (colSignal() !== campo) return '⇅';
    return dirSignal() === 'asc' ? '▲' : '▼';
  }

  pendSorted<T>(lista: T[], colSignal: ReturnType<typeof signal<string>>, dirSignal: ReturnType<typeof signal<'asc' | 'desc'>>): T[] {
    const col = colSignal(); const dir = dirSignal();
    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  }

  pendCellValue(item: any, campo: string): string {
    const v = item[campo];
    if (v === null || v === undefined) return '—';
    if (campo === 'criadoEm') return this.formatarData(v);
    if (campo === 'totalLiquido' || campo === 'precoVenda' || campo === 'precoUnitario' || campo === 'total')
      return typeof v === 'number' ? v.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : v;
    if (campo === 'percentualDesconto') {
      const perc = (item.percentualDesconto ?? 0) + (item.percentualPromocao ?? 0);
      return perc.toLocaleString('pt-BR', { minimumFractionDigits: 2 });
    }
    return String(v);
  }

  pendToggleColuna(colsSignal: ReturnType<typeof signal<ColunaEstado[]>>, storageKey: string, campo: string) {
    colsSignal.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    localStorage.setItem(storageKey, JSON.stringify(colsSignal()));
  }

  pendRestaurarCols(colsSignal: ReturnType<typeof signal<ColunaEstado[]>>, storageKey: string, defs: ColunaDef[]) {
    colsSignal.set(defs.map(c => ({ ...c, visivel: c.padrao })));
    localStorage.setItem(storageKey, JSON.stringify(colsSignal()));
  }

  pendIniciarResize(e: MouseEvent, stateRef: 'vendas' | 'itens', campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    const state = { campo, startX: e.clientX, startW: largura };
    if (stateRef === 'vendas') this.pendVendasResizeState = state;
    else this.pendItensResizeState = state;
    document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none';
  }

  @HostListener('document:mousemove', ['$event'])
  onPendResizeMove(e: MouseEvent) {
    const doResize = (state: any, colsSignal: ReturnType<typeof signal<ColunaEstado[]>>, defs: ColunaDef[]) => {
      if (!state) return;
      const delta = e.clientX - state.startX;
      const def = defs.find(c => c.campo === state.campo);
      const min = def?.minLargura ?? 50;
      const novaLargura = Math.max(min, state.startW + delta);
      colsSignal.update(cols => cols.map(c => c.campo === state.campo ? { ...c, largura: novaLargura } : c));
    };
    doResize(this.pendVendasResizeState, this.pendVendasCols, this.pendVendasCols().map(c => ({ ...c })));
    doResize(this.pendItensResizeState, this.pendItensCols, this.pendItensCols().map(c => ({ ...c })));
  }

  @HostListener('document:mouseup')
  onPendResizeEnd() {
    if (this.pendVendasResizeState) {
      localStorage.setItem(this.STORAGE_PEND_VENDAS, JSON.stringify(this.pendVendasCols()));
      this.pendVendasResizeState = null;
    }
    if (this.pendItensResizeState) {
      localStorage.setItem(this.STORAGE_PEND_ITENS, JSON.stringify(this.pendItensCols()));
      this.pendItensResizeState = null;
    }
    document.body.style.cursor = ''; document.body.style.userSelect = '';
  }

  pendDragStart(ref: 'vendas' | 'itens', idx: number) {
    if (ref === 'vendas') this.pendVendasDragIdx = idx; else this.pendItensDragIdx = idx;
  }
  pendDragOver(e: DragEvent, ref: 'vendas' | 'itens', idx: number) {
    e.preventDefault();
    const dragIdx = ref === 'vendas' ? this.pendVendasDragIdx : this.pendItensDragIdx;
    const colsSignal = ref === 'vendas' ? this.pendVendasCols : this.pendItensCols;
    if (dragIdx === null || dragIdx === idx) return;
    colsSignal.update(cols => { const arr = [...cols]; const [m] = arr.splice(dragIdx!, 1); arr.splice(idx, 0, m); return arr; });
    if (ref === 'vendas') this.pendVendasDragIdx = idx; else this.pendItensDragIdx = idx;
  }
  pendDrop(ref: 'vendas' | 'itens') {
    if (ref === 'vendas') { this.pendVendasDragIdx = null; localStorage.setItem(this.STORAGE_PEND_VENDAS, JSON.stringify(this.pendVendasCols())); }
    else { this.pendItensDragIdx = null; localStorage.setItem(this.STORAGE_PEND_ITENS, JSON.stringify(this.pendItensCols())); }
  }

  private focarCliente() {
    setTimeout(() => this.inputClienteRef?.nativeElement?.focus(), 50);
  }

  formatarData(data: string): string {
    if (!data) return '';
    const d = new Date(data);
    return d.toLocaleDateString('pt-BR') + ' ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  }

  opcoes() {
    // Placeholder for future implementation
    this.modal.aviso('Em Desenvolvimento', 'As opcoes adicionais serao implementadas em breve.');
  }

  sairDaTela() {
    // Fechar todas as modais abertas antes de sair
    this.modalPendentes.set(false);
    this.modalPromoFixas.set(false);
    this.modalPromoProgressiva.set(false);
    this.clienteDropdown.set(false);
    this.colaboradorDropdown.set(false);
    this.produtoDropdown.set(false);

    this.salvarAbaAtiva();
    const abas = this.atendimentos();
    const temDados = abas.some(a => a.itens.length > 0 || a.clienteId);
    if (temDados) {
      const msg = abas.length > 1
        ? `Você possui ${abas.length} atendimento(s) aberto(s). Ao sair, todos serão descartados. Deseja continuar?`
        : 'Ao sair, o atendimento atual será descartado. Deseja continuar?';
      setTimeout(() => {
        this.modal.confirmar('Sair da Pré-Venda', msg, 'Sim, sair', 'Não, continuar').then(resultado => {
          if (resultado.confirmado) {
            this.saindo = true;
            sessionStorage.removeItem(this.STATE_KEY);
            this.tabService.fecharTabAtiva();
          }
        });
      }, 100);
    } else {
      this.saindo = true;
      sessionStorage.removeItem(this.STATE_KEY);
      this.tabService.fecharTabAtiva();
    }
  }

  // ── Dropdown close on outside click ─────────────────────────────
  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent) {
    const target = e.target as HTMLElement;
    if (!target.closest('.cg-wrap')) {
      this.produtoDropdown.set(false);
      this.clienteDropdown.set(false);
      this.colaboradorDropdown.set(false);
    }
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      if (this.modalPagamento()) { (e as any).__handled = true; this.cancelarPagamento(); return; }
      if (this.modalPendentes()) { (e as any).__handled = true; this.fecharPendentes(); return; }
      if (this.modalPromoFixas()) { (e as any).__handled = true; this.fecharModalPromoFixas(); return; }
      if (this.modalPromoProgressiva()) { (e as any).__handled = true; this.fecharModalPromoProgressiva(); return; }
    }
    if (e.key === 'Delete' && this.itensSelecionadoIdx() !== null) {
      const target = e.target as HTMLElement;
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.tagName === 'SELECT') return;
      const idx = this.itensSelecionadoIdx()!;
      this.itens.update(lista => lista.filter((_, i) => i !== idx));
      this.itensSelecionadoIdx.set(null);
    }
  }

  // ══ ComboGrid: utilitários de sort e resize ════════════════════
  cgSort(sortSignal: ReturnType<typeof signal<{ col: string; dir: 'asc' | 'desc' } | null>>, col: string) {
    const atual = sortSignal();
    if (atual?.col === col) {
      sortSignal.set(atual.dir === 'asc' ? { col, dir: 'desc' } : null);
    } else {
      sortSignal.set({ col, dir: 'asc' });
    }
  }

  cgSortIcon(sortSignal: ReturnType<typeof signal<{ col: string; dir: 'asc' | 'desc' } | null>>, col: string): string {
    const s = sortSignal();
    if (!s || s.col !== col) return '⇅';
    return s.dir === 'asc' ? '▲' : '▼';
  }

  cgSortedList<T>(lista: T[], sortSignal: ReturnType<typeof signal<{ col: string; dir: 'asc' | 'desc' } | null>>): T[] {
    const s = sortSignal();
    if (!s) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[s.col] ?? '';
      const vb = (b as any)[s.col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number) : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return s.dir === 'asc' ? cmp : -cmp;
    });
  }

  cgResizeStart(e: MouseEvent, th: HTMLElement) {
    e.preventDefault();
    e.stopPropagation();
    this.cgResizeState = { target: th, startX: e.clientX, startW: th.offsetWidth };
  }

  @HostListener('document:mousemove', ['$event'])
  onCgResizeMove(e: MouseEvent) {
    if (!this.cgResizeState) return;
    const diff = e.clientX - this.cgResizeState.startX;
    const novaLargura = Math.max(50, this.cgResizeState.startW + diff);
    this.cgResizeState.target.style.width = novaLargura + 'px';
  }

  @HostListener('document:mouseup')
  onCgResizeEnd() {
    this.cgResizeState = null;
  }
}
