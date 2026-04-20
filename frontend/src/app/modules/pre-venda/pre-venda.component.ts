import { Component, signal, computed, OnInit, OnDestroy, HostListener, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { ModalSenhaService } from '../../core/services/modal-senha.service';
import { ModalSenhaComponent } from '../../core/components/modal-senha.component';
import { ModalEntregaService, EntregaResultado } from '../../core/services/modal-entrega.service';
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
  // Farmácia Popular
  qtdePorDia?: number;
  precoFpNormal?: number;
  precoFpBolsaFamilia?: number;
  codigoBarras?: string;
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
  precoFp?: number;
  precoFpBolsaFamilia?: number;
  participaFarmaciaPopular?: boolean;
  codigoBarras?: string;
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
  modoPbm?: 'FP' | 'EPHARMA' | 'FUNCIONAL' | 'VIDALINK' | null;
  // ── Farmácia Popular ────────────────────────────────────────────
  prescritorId?: number | null;
  prescritorNome?: string;
  prescritorTipo?: string;
  crmMedico?: string;
  ufCrm?: string;
  numeroReceita?: string;
  dataReceita?: string;
  bolsaFamilia?: boolean;
}

const PREVENDA_COLUNAS: ColunaDef[] = [
  { campo: 'produtoCodigo',      label: 'CÓDIGO',       largura: 80,  minLargura: 60,  padrao: true, tipo: 'texto' },
  { campo: 'produtoNome',        label: 'PRODUTO',      largura: 280, minLargura: 150, padrao: true, tipo: 'texto' },
  { campo: 'fabricante',         label: 'FABRICANTE',   largura: 130, minLargura: 80,  padrao: true, tipo: 'texto' },
  { campo: 'precoVenda',         label: 'PREÇO VENDA',  largura: 100, minLargura: 70,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'quantidade',         label: 'QTDE',         largura: 70,  minLargura: 50,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'qtdePorDia',         label: 'QTDE/DIA',     largura: 80,  minLargura: 60,  padrao: false, tipo: 'numero', editavel: true },
  { campo: 'percentualDesconto', label: '%DESC',        largura: 80,  minLargura: 60,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'precoUnitario',      label: 'PREÇO UNIT',   largura: 100, minLargura: 70,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'total',              label: 'TOTAL',        largura: 100, minLargura: 70,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'vendedor',           label: 'VENDEDOR',     largura: 120, minLargura: 80,  padrao: true, tipo: 'texto' },
];

const CORES_PAGAMENTO = ['#2196F3', '#4CAF50', '#FF9800', '#9C27B0', '#F44336', '#009688', '#795548'];

@Component({
  selector: 'app-pre-venda',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalSenhaComponent],
  templateUrl: './pre-venda.component.html',
  styleUrl: './pre-venda.component.scss'
})
export class PreVendaComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_prevenda_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_prevenda_itens';
  private readonly apiUrl = environment.apiUrl;

  @ViewChild('inputCliente') inputClienteRef!: ElementRef<HTMLInputElement>;
  @ViewChild('inputVendedor') inputVendedorRef!: ElementRef<HTMLInputElement>;
  @ViewChild('inputProduto') inputProdutoRef!: ElementRef<HTMLInputElement>;
  private saindo = false;

  // ── Configurações de venda ──────────────────────────────────────
  cfgMultiplosVendedores = signal(false);
  cfgDuplicarLinha = signal(false);
  cfgFocarQuantidade = signal(false);
  cfgAlterarPrecoPromo = signal(false);
  cfgObrigarEscanear = signal(false);
  cfgPromoMultiplas = signal<'exibir' | 'menor'>('exibir');
  cfgInformarCesta = signal(false);

  // ── Modal cesta ─────────────────────────────────────────────────
  modalCesta = signal(false);
  cestaNumero = signal('');

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
  promoProgressivaAtual = signal<PromocaoProduto | null>(null);
  promoProgressivaItemIdx = signal(-1);
  modalPromoFixas = signal(false);
  promoFixasLista = signal<PromocaoProduto[]>([]);
  promoFixasItemIdx = signal(-1);

  // ── Abas de atendimento ─────────────────────────────────────────
  atendimentos = signal<Atendimento[]>([]);
  abaAtivaId = signal(1);
  private nextAbaId = 1;

  // ── Pre-venda state (aba ativa) ─────────────────────────────────
  preVendaId = signal<number | null>(null);
  itens = signal<PreVendaItem[]>([]);
  itensSelecionadoIdx = signal<number | null>(null);

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

  // ── Farmácia Popular (só usado quando a aba ativa é FP) ─────────
  readonly UF_OPCOES = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO'];
  fpPrescritorId = signal<number | null>(null);
  fpPrescritorNome = signal('');
  fpPrescritorTipo = signal('CRM');
  fpCrmMedico = signal('');
  fpUfCrm = signal('');
  fpPrescritorBusca = signal('');
  fpPrescritorResultados = signal<{ id: number; nome: string; tipoConselho: string; numeroConselho: string; uf: string }[]>([]);
  fpPrescritorDropdown = signal(false);
  fpPrescritorIndice = signal(-1);
  private fpPrescritorTimer: any = null;
  fpNumeroReceita = signal('');
  fpDataReceita = signal('');
  fpBolsaFamilia = signal(false);

  // ── Venda a prazo ───────────────────────────────────────────────
  private tokenLiberacaoCredito: string | null = null;
  private senhaClientePrazo: string | null = null;
  prazoPermiteParcelada = signal(false);
  prazoMaxParcelas = signal(1);

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
    public modalSenha: ModalSenhaService,
    private modalEntrega: ModalEntregaService
  ) {}

  // ── Entrega ─────────────────────────────────────────────────────
  ehEntrega = signal(false);
  private dadosEntrega: EntregaResultado | null = null;

  toggleEntrega(checked: boolean) {
    this.ehEntrega.set(checked);
    if (!checked) this.dadosEntrega = null;
  }

  ngOnInit() {
    this.carregarFiliais();
    this.carregarConfigs();
    this.carregarTiposPagamento();
    this.restaurarEstado();
    this.buscarHierarquia();
    this.focarCliente();
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
        this.cfgMultiplosVendedores.set(map['venda.multiplos.vendedores'] === 'true');
        this.cfgDuplicarLinha.set(map['venda.duplicar.linha'] === 'true');
        this.cfgFocarQuantidade.set(map['venda.focar.quantidade'] === 'true');
        this.cfgAlterarPrecoPromo.set(map['venda.alterar.preco.promo'] === 'true');
        this.cfgObrigarEscanear.set(map['venda.obrigar.escanear'] === 'true');
        this.cfgPromoMultiplas.set((map['venda.promo.multiplas'] ?? 'exibir') as 'exibir' | 'menor');
        this.cfgInformarCesta.set(map['caixa.informar.cesta'] === 'true');
      }
    });
  }

  ngOnDestroy() {
    if (!this.saindo) this.salvarEstado();
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
    const sufixoFP = this.ehAbaFP() ? ' — Farmácia Popular' : '';
    this.atendimentos.update(abas => abas.map(a => a.id === id ? {
      ...a, preVendaId: this.preVendaId(), itens: this.itens(),
      clienteId: this.clienteId(), clienteNome: this.clienteNome(),
      colaboradorId: this.colaboradorId(), colaboradorNome: this.colaboradorNome(),
      tipoPagamentoId: this.tipoPagamentoId(),
      label: this.clienteNome() ? this.clienteNome() + sufixoFP : `Atendimento ${a.id}${sufixoFP}`,
      prescritorId: this.fpPrescritorId(), prescritorNome: this.fpPrescritorNome(), prescritorTipo: this.fpPrescritorTipo(),
      crmMedico: this.fpCrmMedico(), ufCrm: this.fpUfCrm(),
      numeroReceita: this.fpNumeroReceita(), dataReceita: this.fpDataReceita(),
      bolsaFamilia: this.fpBolsaFamilia()
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
    this.fpPrescritorId.set(aba.prescritorId ?? null);
    this.fpPrescritorNome.set(aba.prescritorNome ?? '');
    this.fpPrescritorTipo.set(aba.prescritorTipo ?? 'CRM');
    this.fpCrmMedico.set(aba.crmMedico ?? '');
    this.fpUfCrm.set(aba.ufCrm ?? '');
    this.fpPrescritorBusca.set('');
    this.fpPrescritorResultados.set([]);
    this.fpPrescritorDropdown.set(false);
    this.fpNumeroReceita.set(aba.numeroReceita ?? '');
    this.fpDataReceita.set(aba.dataReceita ?? '');
    this.fpBolsaFamilia.set(aba.bolsaFamilia ?? false);
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
      this.tokenLiberacaoCredito = null;
      this.senhaClientePrazo = null;
      this.prazoPermiteParcelada.set(false);
      this.prazoMaxParcelas.set(1);
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

      if (v.clienteBloqueado || v.convenioBloqueado) {
        await this.modal.erro('Bloqueado', v.mensagemBloqueio || 'Cliente ou convênio bloqueado.');
        return false;
      }

      if (v.tipoPagamentoBloqueado) {
        await this.modal.erro('Condição Bloqueada', v.mensagemTipoBloqueado || 'Condição de pagamento bloqueada.');
        return false;
      }

      if (v.excedeLimite) {
        const perm = await this.modal.permissao('venda', 'prazo-excede-limite');
        if (!perm.confirmado) return false;
        this.tokenLiberacaoCredito = perm.tokenLiberacao ?? null;
      } else {
        this.tokenLiberacaoCredito = null;
      }

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

      this.prazoPermiteParcelada.set(v.permiteParcelada);
      this.prazoMaxParcelas.set(v.maxParcelas || 1);

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

    // ── Farmácia Popular: só produtos com ParticipaFP=true ───────
    if (this.ehAbaFP() && !p.participaFarmaciaPopular) {
      this.modal.aviso('Farmácia Popular', `O produto "${p.nome}" não participa do programa Farmácia Popular.`);
      return;
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

    // Preço FP: usa PrecoFpBolsaFamilia (se checkbox marcado) ou PrecoFp; senão valorVenda.
    const precoFpEfetivo = this.ehAbaFP()
      ? (this.abaAtual()?.bolsaFamilia && p.precoFpBolsaFamilia != null
          ? p.precoFpBolsaFamilia
          : (p.precoFp ?? p.valorVenda))
      : p.valorVenda;
    const novoItem: PreVendaItem = {
      produtoId: p.id,
      produtoCodigo: p.codigo,
      produtoNome: p.nome,
      fabricante: p.fabricante ?? '',
      precoVenda: precoFpEfetivo,
      precoFpNormal: p.precoFp,
      precoFpBolsaFamilia: p.precoFpBolsaFamilia,
      codigoBarras: p.codigoBarras,
      quantidade: 1,
      percentualDesconto: 0,
      percentualPromocao: 0,
      valorDesconto: 0,
      precoUnitario: precoFpEfetivo,
      total: precoFpEfetivo,
      estoqueAtual: p.estoqueAtual,
      vendedor: vendedorAtualNome,
      colaboradorId: vendedorAtualId ?? undefined,
      unidade: p.unidade,
      descontos: []
    };
    this.itens.update(lista => [...lista, novoItem]);
    const idx = this.itens().length - 1;
    this.itensSelecionadoIdx.set(idx);
    this.produtoBusca.set('');
    this.produtoResultados.set([]);
    this.produtoDropdown.set(false);

    // Resolver desconto via hierarquia (backend já faz o loop e PARA no primeiro que casa).
    // Regra: desconto nunca soma com promoção — respeita a ordem da hierarquia.
    this.resolverDescontoProduto(p.id, (desc) => {
      this.itens.update(lista => {
        const arr = [...lista];
        const item = { ...arr[idx], descontos: [...arr[idx].descontos] };
        item.descontoMaxPermitido = desc.descontoMaxSemSenha;
        item.componenteDesconto = desc.componente ?? undefined;
        if (desc.aplicarAutomatico && desc.descontoAplicar > 0) {
          item.percentualDesconto = desc.descontoAplicar;
          item.precoUnitario = Math.round(item.precoVenda * (1 - desc.descontoAplicar / 100) * 100) / 100;
          item.valorDesconto = Math.round(item.precoVenda * item.quantidade * desc.descontoAplicar / 100 * 100) / 100;
          item.total = Math.round(item.precoUnitario * item.quantidade * 100) / 100;
          item.descontos.push({
            tipo: 1,
            percentual: desc.descontoAplicar,
            origem: desc.componente ?? 'Padrao',
            regra: desc.hierarquiaNome ?? 'Padrão',
            origemId: desc.hierarquiaId
          });
        }
        arr[idx] = item;
        return arr;
      });

      // Só buscar promoções separadamente se a hierarquia NÃO casou via componente de promoção.
      // Se casou via PromoFixa/PromoProgressiva, o resolver já trouxe o desconto certo.
      const componentePromocao = desc.componente === 'Promoção Fixa' || desc.componente === 'Promoção Progressiva';
      if (!componentePromocao) return;

      // Progressiva precisa modal pra escolher faixa (o resolver não retorna as faixas).
      if (desc.componente === 'Promoção Progressiva') {
        this.buscarPromocoesProduto(p.id, idx);
      }
    });

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
      const percTotal = item.percentualDesconto + item.percentualPromocao;
      item.precoUnitario = Math.round(item.precoVenda * (1 - percTotal / 100) * 100) / 100;
      item.valorDesconto = Math.round(item.precoVenda * item.quantidade * percTotal / 100 * 100) / 100;
      item.total = Math.round(item.precoUnitario * item.quantidade * 100) / 100;
      item.descontos.push({
        tipo: 2,
        percentual: promo.percentualPromocao,
        origem: 'PromocaoFixa',
        regra: promo.nome,
        origemId: promo.promocaoId
      });
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
      const percTotal = item.percentualDesconto + item.percentualPromocao;
      item.precoUnitario = Math.round(item.precoVenda * (1 - percTotal / 100) * 100) / 100;
      item.valorDesconto = Math.round(item.precoVenda * item.quantidade * percTotal / 100 * 100) / 100;
      item.total = Math.round(item.precoUnitario * item.quantidade * 100) / 100;
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
  menuPbmsAberto = signal(false);

  modoPbmAtual = computed<Atendimento['modoPbm']>(() => {
    const id = this.abaAtivaId();
    const aba = this.atendimentos().find(a => a.id === id);
    return aba?.modoPbm ?? null;
  });

  ehAbaFP = computed(() => this.modoPbmAtual() === 'FP');

  abaAtual = computed<Atendimento | null>(() => {
    const id = this.abaAtivaId();
    return this.atendimentos().find(a => a.id === id) ?? null;
  });

  onBolsaFamiliaChange(novoValor: boolean) {
    this.fpBolsaFamilia.set(novoValor);
    if (this.itens().length === 0) return;
    this.itens.update(lista => lista.map(item => {
      const novoPreco = novoValor && item.precoFpBolsaFamilia != null
        ? item.precoFpBolsaFamilia
        : (item.precoFpNormal ?? item.precoVenda);
      const clone = { ...item, precoVenda: novoPreco };
      this.recalcularItem(clone, 'precoVenda');
      return clone;
    }));
  }

  // ── Lookup de Prescritor (topo FP) ─────────────────────────────
  onPrescritorInput(valor: string) {
    this.fpPrescritorBusca.set(valor);
    if (this.fpPrescritorTimer) clearTimeout(this.fpPrescritorTimer);
    if (valor.trim().length < 2) {
      this.fpPrescritorResultados.set([]);
      this.fpPrescritorDropdown.set(false);
      return;
    }
    this.fpPrescritorTimer = setTimeout(() => this.buscarPrescritores(valor), 250);
  }

  private buscarPrescritores(termo: string) {
    this.http.get<any>(`${this.apiUrl}/prescritores/buscar`, { params: { termo } }).subscribe({
      next: r => {
        this.fpPrescritorResultados.set(r.data ?? []);
        this.fpPrescritorDropdown.set((r.data ?? []).length > 0);
        this.fpPrescritorIndice.set(-1);
      },
      error: () => { this.fpPrescritorResultados.set([]); this.fpPrescritorDropdown.set(false); }
    });
  }

  selecionarPrescritor(p: { id: number; nome: string; tipoConselho: string; numeroConselho: string; uf: string }) {
    this.fpPrescritorId.set(p.id);
    this.fpPrescritorNome.set(p.nome);
    this.fpPrescritorTipo.set(p.tipoConselho || 'CRM');
    this.fpCrmMedico.set(p.numeroConselho);
    this.fpUfCrm.set(p.uf);
    this.fpPrescritorBusca.set('');
    this.fpPrescritorResultados.set([]);
    this.fpPrescritorDropdown.set(false);
  }

  limparPrescritor() {
    this.fpPrescritorId.set(null);
    this.fpPrescritorNome.set('');
    this.fpPrescritorTipo.set('CRM');
    this.fpCrmMedico.set('');
    this.fpUfCrm.set('');
    this.fpPrescritorBusca.set('');
    this.fpPrescritorResultados.set([]);
    this.fpPrescritorDropdown.set(false);
  }

  onPrescritorBlur() { setTimeout(() => this.fpPrescritorDropdown.set(false), 200); }
  onPrescritorFocus() { if (this.fpPrescritorResultados().length > 0) this.fpPrescritorDropdown.set(true); }

  onPrescritorKeydown(e: KeyboardEvent) {
    const lista = this.fpPrescritorResultados();
    if (!this.fpPrescritorDropdown() || lista.length === 0) return;
    if (e.key === 'ArrowDown') { e.preventDefault(); this.fpPrescritorIndice.set(Math.min(this.fpPrescritorIndice() + 1, lista.length - 1)); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); this.fpPrescritorIndice.set(Math.max(this.fpPrescritorIndice() - 1, 0)); }
    else if (e.key === 'Enter') {
      e.preventDefault();
      const idx = this.fpPrescritorIndice();
      if (idx >= 0 && idx < lista.length) this.selecionarPrescritor(lista[idx]);
      else if (lista.length === 1) this.selecionarPrescritor(lista[0]);
    } else if (e.key === 'Escape') {
      this.fpPrescritorDropdown.set(false);
    }
  }

  abrirAbaPbm(pbm: 'FP' | 'EPHARMA' | 'FUNCIONAL' | 'VIDALINK') {
    if (pbm !== 'FP') {
      this.modal.aviso('Em breve', 'Este PBM ainda não está disponível. Por enquanto apenas Farmácia Popular.');
      return;
    }
    this.salvarAbaAtiva();
    const id = this.nextAbaId++;
    const tipoPadrao = this.tiposPagamento().length > 0 ? this.tiposPagamento()[0].id : null;
    const aba: Atendimento = {
      id, label: `Atendimento ${id} — Farmácia Popular`, preVendaId: null, itens: [],
      clienteId: null, clienteNome: '', colaboradorId: null, colaboradorNome: '',
      tipoPagamentoId: tipoPadrao, modoPbm: pbm
    };
    this.atendimentos.update(abas => [...abas, aba]);
    this.carregarAba(id);
    this.menuPbmsAberto.set(false);
  }

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
    if (campo === 'qtdePorDia') return item.qtdePorDia != null ? String(Math.floor(item.qtdePorDia)) : '';
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
    return true;
  }

  onCellKeydown(e: KeyboardEvent, idx: number, campo: string) {
    if (e.key !== 'Enter') return;
    e.preventDefault();
    const ordem = ['quantidade', 'percentualDesconto', 'precoUnitario', 'total'];
    const posAtual = ordem.indexOf(campo);
    if (posAtual >= 0 && posAtual < ordem.length - 1) {
      // Próxima coluna na mesma linha
      const prox = ordem[posAtual + 1];
      const row = (e.target as HTMLElement).closest('tr');
      const nextInput = row?.querySelector(`td[data-campo="${prox}"] input`) as HTMLInputElement;
      if (nextInput) { nextInput.focus(); nextInput.select(); }
    } else {
      // Última coluna ou não encontrado — volta ao campo produto
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
      const inteiro = campo === 'quantidade' || campo === 'qtdePorDia';
      (item as any)[campo] = inteiro ? Math.floor(num) : num;
      if (campo !== 'qtdePorDia') this.recalcularItem(item, campo);

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

    switch (campoAlterado) {
      case 'precoVenda':
        // Recalcula unitário e total mantendo o % desconto
        item.precoUnitario = r(item.precoVenda * (1 - item.percentualDesconto / 100));
        item.valorDesconto = r(item.precoVenda * item.quantidade * item.percentualDesconto / 100);
        item.total = r(item.precoUnitario * item.quantidade);
        break;

      case 'quantidade':
        // Mantém % desconto, recalcula unitário e total
        item.precoUnitario = r(item.precoVenda * (1 - item.percentualDesconto / 100));
        item.valorDesconto = r(item.precoVenda * item.quantidade * item.percentualDesconto / 100);
        item.total = r(item.precoUnitario * item.quantidade);
        break;

      case 'percentualDesconto':
        // Recalcula unitário e total a partir do %
        item.precoUnitario = r(item.precoVenda * (1 - item.percentualDesconto / 100));
        item.valorDesconto = r(item.precoVenda * item.quantidade * item.percentualDesconto / 100);
        item.total = r(item.precoUnitario * item.quantidade);
        break;

      case 'precoUnitario':
        // Recalcula % desconto e total a partir do preço unitário
        item.percentualDesconto = item.precoVenda > 0 ? r((item.precoVenda - item.precoUnitario) / item.precoVenda * 100) : 0;
        item.valorDesconto = r((item.precoVenda - item.precoUnitario) * item.quantidade);
        item.total = r(item.precoUnitario * item.quantidade);
        break;

      case 'total':
        // Recalcula unitário e % a partir do total
        item.precoUnitario = item.quantidade > 0 ? r(item.total / item.quantidade) : 0;
        item.percentualDesconto = item.precoVenda > 0 ? r((item.precoVenda - item.precoUnitario) / item.precoVenda * 100) : 0;
        item.valorDesconto = r((item.precoVenda - item.precoUnitario) * item.quantidade);
        break;

      default:
        // Fallback padrão
        item.precoUnitario = r(item.precoVenda * (1 - item.percentualDesconto / 100));
        item.valorDesconto = r(item.precoVenda * item.quantidade * item.percentualDesconto / 100);
        item.total = r(item.precoUnitario * item.quantidade);
        break;
    }
  }

  getEditValue(item: PreVendaItem, campo: string): string {
    if (campo === 'quantidade') return String(Math.floor(item.quantidade));
    if (campo === 'qtdePorDia') return item.qtdePorDia != null ? String(Math.floor(item.qtdePorDia)) : '';
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

    // Se é entrega, exige cliente e abre modal de configuração
    if (this.ehEntrega()) {
      if (!this.clienteId()) {
        await this.modal.aviso('Cliente obrigatório', 'Para venda com entrega, informe o cliente antes de finalizar.');
        return;
      }
      const resultado = await this.modalEntrega.abrir(this.clienteId()!, this.filialId());
      if (!resultado) return; // cancelou
      this.dadosEntrega = resultado;
    } else {
      this.dadosEntrega = null;
    }

    // Informar cesta (se habilitado)
    if (this.cfgInformarCesta()) {
      if (!this.cestaNumero()) {
        this.modalCesta.set(true);
        return;
      }
    }

    this.executarFinalizacao();
  }

  confirmarCesta() {
    const nr = this.cestaNumero().trim();
    if (this.cfgInformarCesta() && !nr) {
      this.modal.aviso('Cesta Obrigatória', 'Informe o número da cesta para finalizar a pré-venda.');
      return;
    }
    this.modalCesta.set(false);
    this.executarFinalizacao();
  }

  cancelarCesta() {
    this.modalCesta.set(false);
    this.focarCliente();
  }

  private executarFinalizacao() {
    this.salvando.set(true);

    const body: any = {
      filialId: this.filialId(),
      clienteId: this.clienteId(),
      colaboradorId: this.colaboradorId(),
      tipoPagamentoId: this.tipoPagamentoId(),
      convenioId: this.convenioIdCliente(),
      nrCesta: this.cestaNumero() || null,
      origem: 1,
      entregaSolicitada: !!this.dadosEntrega,
      entregaEnderecoId: this.dadosEntrega?.enderecoEntregaId ?? null,
      entregaObservacao: this.dadosEntrega?.observacao ?? null,
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
      farmaciaPopular: this.ehAbaFP() ? {
        prescritorId: this.fpPrescritorId(),
        crmMedico: this.fpCrmMedico(),
        ufCrm: this.fpUfCrm(),
        dtEmissaoReceita: this.fpDataReceita() || null,
        nuReceita: this.fpNumeroReceita() || null,
        bolsaFamilia: this.fpBolsaFamilia(),
        itens: this.itens().map(i => ({
          produtoId: i.produtoId,
          codigoBarraEAN: i.codigoBarras || '',
          qtPrescrita: i.qtdePorDia ?? 1,
          qtSolicitada: i.quantidade,
          vlPrecoVenda: i.precoVenda
        }))
      } : null
    };

    const salvar$ = this.preVendaId()
      ? this.http.put<any>(`${this.apiUrl}/vendas/${this.preVendaId()}`, body)
      : this.http.post<any>(`${this.apiUrl}/vendas`, body);

    salvar$.subscribe({
      next: (r: any) => {
        this.salvando.set(false);
        const id = this.preVendaId() ?? r.data?.id;
        if (!id) {
          this.modal.erro('Erro', 'Erro ao salvar pré-venda.');
          return;
        }
        this.preVendaId.set(id);
        // Entrega: campos já persistidos na Venda; Entrega propriamente dita é criada no caixa ao finalizar
        this.dadosEntrega = null;
        this.ehEntrega.set(false);
        this.modal.sucesso('Salvo', 'Pré-venda salva com sucesso.');
        this.resetTudo();
      },
      error: (err: any) => {
        this.salvando.set(false);
        const msg = err?.error?.message || 'Erro ao salvar pré-venda.';
        // Se erro de cesta duplicada, reabrir modal para informar outra
        if (msg.includes('cesta')) {
          this.modal.aviso('Cesta em Uso', msg);
          this.cestaNumero.set('');
          this.modalCesta.set(true);
        } else {
          this.modal.erro('Erro', msg);
        }
      }
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
    this.modalCesta.set(false);
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
      if (this.modalPendentes()) { (e as any).__handled = true; this.fecharPendentes(); return; }
      if (this.modalCesta()) { (e as any).__handled = true; this.cancelarCesta(); return; }
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
