import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { ToastrService } from 'ngx-toastr';
import { ProdutosComponent } from '../produtos/produtos.component';

interface CompraList {
  id: number;
  codigo: string;
  numeroNf: string;
  serieNf: string;
  fornecedorNome: string;
  fornecedorCnpj: string;
  dataEmissao: string;
  dataEntrada: string;
  valorNota: number;
  status: number;
  totalItens: number;
  itensVinculados: number;
  itensPrecificados: number;
  itensConferidos: number;
  itensConferidosExcedidos: number;
  criadoEm: string;
}

interface CompraProduto {
  id: number;
  numeroItem: number;
  produtoId: number | null;
  produtoNome: string | null;
  produtoCodigoBarras: string | null;
  codigoProdutoFornecedor: string;
  codigoBarrasXml: string;
  descricaoXml: string;
  ncmXml: string;
  cestXml: string;
  cfopXml: string;
  unidadeXml: string;
  quantidade: number;
  valorUnitario: number;
  valorTotal: number;
  valorDesconto: number;
  valorFrete: number;
  valorOutros: number;
  valorItemNota: number;
  lote: string | null;
  dataFabricacao: string | null;
  dataValidade: string | null;
  codigoAnvisa: string | null;
  precoMaximoConsumidor: number | null;
  vinculado: boolean;
  fracao: number;
  qtdeConferida: number;
  qtdeTotal: number;
  infoAdicional: string | null;
  fiscal: any;
}

/** Item da nota + contexto da nota (usado na tela de vinculo em lote, que junta varias notas). */
interface CompraProdutoVinc extends CompraProduto {
  compraId: number;
  compraNumeroNf: string;
  compraSerieNf: string;
  compraFornecedorId: number;
  compraFornecedorNome: string;
  compraFilialId: number;
}

interface CompraDetalhe {
  id: number;
  codigo: string;
  filialId: number;
  fornecedorId: number;
  fornecedorNome: string;
  fornecedorCnpj: string;
  chaveNfe: string;
  numeroNf: string;
  serieNf: string;
  naturezaOperacao: string;
  dataEmissao: string;
  dataEntrada: string;
  valorProdutos: number;
  valorSt: number;
  valorFcpSt: number;
  valorFrete: number;
  valorSeguro: number;
  valorDesconto: number;
  valorIpi: number;
  valorPis: number;
  valorCofins: number;
  valorOutros: number;
  valorNota: number;
  status: number;
  criadoEm: string;
  produtos: CompraProduto[];
}

interface ProdutoBusca {
  id: number;
  nome: string;
  codigoBarras: string;
}

interface PrecificacaoItem {
  produtoId: number; produtoDadosId: number; compraProdutoId: number;
  produtoNome: string; ean: string; fabricanteNome: string;
  custoCompraAnterior: number; custoCompraAtual: number; varCustoCompraPercent: number;
  custoMedioAnterior: number; custoMedioAtual: number; varCustoMedioPercent: number;
  precoVendaAtual: number; sugestaoVendaCustoCompra: number; sugestaoVendaCustoMedio: number;
  novoPrecoVenda: number; pmcNota: number; pmcAbcFarma: number;
  formacaoPreco: string; markup: number; projecaoLucro: number; quantidade: number;
}

@Component({
  selector: 'app-compras',
  standalone: true,
  imports: [CommonModule, FormsModule, ProdutosComponent],
  templateUrl: './compras.component.html',
  styleUrls: ['./compras.component.scss']
})
export class ComprasComponent implements OnInit, OnDestroy {
  private apiUrl = `${environment.apiUrl}/compras`;
  private produtosApiUrl = `${environment.apiUrl}/produtos`;
  private tokenLiberacao: string | null = null;

  // ── Estado ────────────────────────────────────────────────────
  modo = signal<'lista' | 'detalhe' | 'vincular' | 'precificacao' | 'conferencia' | 'conferir-lotes' | 'finalizacao' | 'sefaz'>('lista');
  compras = signal<CompraList[]>([]);
  compraSelecionada = signal<CompraList | null>(null);
  compraDetalhe = signal<CompraDetalhe | null>(null);
  carregando = signal(false);
  importando = signal(false);
  vinculando = signal<number | null>(null);
  erro = signal('');
  busca = signal('');
  filtroStatus = signal<'pendentes' | 'finalizadas'>('pendentes');
  filtroFilialId = signal<number>(0);
  filtroDataInicio = signal<string>(this.dataDefault(-30));
  filtroDataFim = signal<string>(this.dataDefault(0));
  filtroDataTipo = signal<'emissao' | 'finalizacao'>('emissao');
  filiaisDisponiveis = signal<{ id: number; nome: string }[]>([]);

  // ── Ordenacao lista ───────────────────────────────────────────
  sortColuna = signal<string>('id');
  sortDirecao = signal<'asc' | 'desc'>('desc');

  // ── Resize de colunas ─────────────────────────────────────────
  private resizingCol: string | null = null;
  private resizeStartX = 0;
  private resizeStartW = 0;

  // ── Busca de produto (modal vincular) ─────────────────────────
  modalVincular = signal(false);
  itemParaVincular = signal<CompraProduto | null>(null);
  buscaProduto = signal('');
  produtosBusca = signal<ProdutoBusca[]>([]);
  buscandoProduto = signal(false);
  private buscaProduto$ = new Subject<string>();

  // ── Seleção de notas (checkboxes na lista) ─────────────────────
  notasSelecionadas = signal<Set<number>>(new Set());

  // Passo 1 (Vincular): tela combinada com os produtos de TODAS as notas selecionadas.
  vincularItens = signal<CompraProdutoVinc[]>([]);
  vincularCarregando = signal(false);
  vinculandoLote = signal(false); // "Vincular automatico" (re-vincular auto) dentro da tela
  vincularVinculados = computed(() => this.vincularItens().filter(p => p.vinculado).length);
  vincularPendentes = computed(() => this.vincularItens().filter(p => !p.vinculado).length);
  vincularNotasCount = computed(() => new Set(this.vincularItens().map(p => p.compraId)).size);

  // ── Precificação ──────────────────────────────────────────────
  precificacaoItens = signal<PrecificacaoItem[]>([]);
  precificacaoCarregando = signal(false);
  sortPrecCol = signal<string>('produtoNome');
  sortPrecDir = signal<'asc' | 'desc'>('asc');
  painelColunasPrecAberto = signal(false);
  precSelecionados = signal<Set<number>>(new Set());
  precAplicados = signal<Set<number>>(new Set()); // pinta a linha de verde apos aplicar
  precAplicando = signal(false);
  precBaseCalculo = signal<'CUSTO_COMPRA' | 'CUSTO_MEDIO'>('CUSTO_COMPRA');
  precMomentoAplicacao = signal<'AGORA' | 'FINALIZACAO'>('AGORA');

  // Modal precificação
  precModalAberto = signal(false);
  precModalTitulo = signal('');
  precModalMsg = signal('');
  precModalCallback: (() => void) | null = null;

  private readonly PREC_COLUNAS_KEY = 'zulex_prec_colunas';
  precColunas = signal<{ campo: string; label: string; largura: number; visivel: boolean }[]>(this.carregarPrecColunas());

  private precColunasDefault(): { campo: string; label: string; largura: number; visivel: boolean }[] {
    return [
      { campo: 'produtoId', label: 'Cod', largura: 55, visivel: true },
      { campo: 'produtoNome', label: 'Produto', largura: 200, visivel: true },
      { campo: 'fabricanteNome', label: 'Fabricante', largura: 100, visivel: true },
      { campo: 'custoCompraAnterior', label: 'CC Anterior', largura: 82, visivel: true },
      { campo: 'custoCompraAtual', label: 'CC Atual', largura: 82, visivel: true },
      { campo: 'varCustoCompraPercent', label: '% Var CC', largura: 70, visivel: true },
      { campo: 'custoMedioAnterior', label: 'CM Anterior', largura: 82, visivel: true },
      { campo: 'custoMedioAtual', label: 'CM Atual', largura: 82, visivel: true },
      { campo: 'varCustoMedioPercent', label: '% Var CM', largura: 70, visivel: true },
      { campo: 'precoVendaAtual', label: 'Venda Atual', largura: 85, visivel: true },
      { campo: 'novoPrecoVenda', label: 'Sug. Vlr Venda', largura: 95, visivel: true },
      { campo: 'projecaoLucro', label: '% Proj. Lucro', largura: 85, visivel: true },
      { campo: 'markup', label: '% Markup', largura: 80, visivel: true },
      { campo: 'ajustado', label: 'Status', largura: 55, visivel: true },
      { campo: 'pmcNota', label: 'PMC Nota', largura: 75, visivel: true },
      { campo: 'pmcAbcFarma', label: 'PMC ABC', largura: 75, visivel: true },
    ];
  }

  precColunasVisiveis = computed(() => this.precColunas().filter(c => c.visivel));

  precificacaoOrdenada = computed(() => {
    const lista = [...this.precificacaoItens()];
    const col = this.sortPrecCol();
    const dir = this.sortPrecDir() === 'asc' ? 1 : -1;
    lista.sort((a: any, b: any) => {
      const va = a[col] ?? '';
      const vb = b[col] ?? '';
      if (typeof va === 'number') return (va - vb) * dir;
      return String(va).localeCompare(String(vb)) * dir;
    });
    return lista;
  });

  // Drag-and-drop colunas
  private dragColIdx: number | null = null;

  // ── SEFAZ ─────────────────────────────────────────────────────
  sefazNotas = signal<any[]>([]);
  sefazConsultando = signal(false);
  sefazImportando = signal<string | null>(null);
  sefazManifestando = signal<string | null>(null);
  sefazChave = signal('');
  private sefazApiUrl = `${environment.apiUrl}/sefaz`;

  // ── Finalização ───────────────────────────────────────────────
  finDados = signal<any>(null);
  finCarregando = signal(false);
  finAplicando = signal(false);
  finDuplicatasEntregues = signal(false);
  finNotaPaga = signal(false);
  // Desmembrar em grade na finalizacao: distribui a qtde da nota entre os SKUs.
  finDesmembramentos = signal<Record<number, { variacaoId: number; quantidade: number }[]>>({}); // por compraProdutoId
  finDesmembrarItem = signal<any | null>(null); // item (lote) sendo desmembrado no modal
  finDesmembrarSkus = signal<{ variacaoId: number; descricao: string; quantidade: number }[]>([]);
  totalDesmembrarModal = computed(() => this.finDesmembrarSkus().reduce((s, x) => s + (+x.quantidade || 0), 0));

  // ── Conferência ───────────────────────────────────────────────
  confItens = signal<CompraProduto[]>([]);
  confCarregando = signal(false);
  confCompraIds = signal<number[]>([]);

  // ── Conferir Lotes (SNGPC) — tela (mode 'conferir-lotes'), 1+ notas ──
  confLotesCarregando = signal(false);
  confLotesSalvando = signal(false);
  confItemConferindo = signal<number | null>(null); // compraProdutoId sendo gravado
  // Uma "nota" por compra selecionada: { compraId, numeroNf, fornecedorNome,
  // dataEmissao, lotesConferidos, lotesConferidosEm, sngpcOptOut, itens: [...] }.
  confLotesNotas = signal<any[]>([]);

  // ── Computed ──────────────────────────────────────────────────
  comprasFiltradas = computed(() => {
    let lista = [...this.compras()];
    const termo = this.busca().toLowerCase().trim();

    // Filtro local por busca de texto (status/data filtrados no server)
    if (termo) {
      lista = lista.filter(c =>
        c.numeroNf.toLowerCase().includes(termo) ||
        c.fornecedorNome.toLowerCase().includes(termo) ||
        c.fornecedorCnpj.includes(termo)
      );
    }

    // Ordenar
    const col = this.sortColuna();
    const dir = this.sortDirecao() === 'asc' ? 1 : -1;
    lista.sort((a: any, b: any) => {
      const va = a[col] ?? '';
      const vb = b[col] ?? '';
      if (typeof va === 'number' && typeof vb === 'number') return (va - vb) * dir;
      return String(va).localeCompare(String(vb)) * dir;
    });

    return lista;
  });

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private modal: ModalService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.carregar();
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({
      next: r => this.filiaisDisponiveis.set((r.data ?? []).map((f: any) => ({ id: f.id, nome: f.nomeFilial || `Filial ${f.id}` })))
    });
    this.buscaProduto$.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(termo => {
      this.buscaProduto.set(termo);
      this.buscarProdutos();
    });
  }

  ngOnDestroy() {
    // O estado da tela sobrevive ao navegar via RouteReuseStrategy (data:{reuse:true}
    // na rota lancar-compras); a aba so' e' destruida ao ser fechada.
    this.buscaProduto$.complete();
  }

  onBuscaProdutoInput(valor: string) {
    this.buscaProduto.set(valor);
    this.buscaProduto$.next(valor);
  }

  // ── Ordenacao ─────────────────────────────────────────────────

  ordenar(campo: string) {
    if (this.sortColuna() === campo) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(campo);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string {
    return this.sortColuna() === campo
      ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅';
  }

  // ── Resize ────────────────────────────────────────────────────

  iniciarResize(event: MouseEvent, col: string, larguraAtual: number) {
    event.preventDefault();
    event.stopPropagation();
    this.resizingCol = col;
    this.resizeStartX = event.clientX;
    this.resizeStartW = larguraAtual;
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    if (!this.resizingCol) return;
    const diff = event.clientX - this.resizeStartX;
    const novaLargura = Math.max(40, this.resizeStartW + diff);
    const th = document.querySelector(`[data-col="${this.resizingCol}"]`) as HTMLElement;
    if (th) th.style.width = `${novaLargura}px`;
  }

  @HostListener('document:mouseup')
  onMouseUp() {
    this.resizingCol = null;
  }

  // ── CRUD ──────────────────────────────────────────────────────

  carregar() {
    this.carregando.set(true);
    this.erro.set('');
    const params: any = {};
    if (this.filtroFilialId() > 0) params.filialId = this.filtroFilialId();
    if (this.filtroStatus()) params.status = this.filtroStatus();
    // Pendentes: ignora o intervalo de datas (traz tudo que está em aberto).
    if (this.filtroStatus() !== 'pendentes') {
      if (this.filtroDataInicio()) params.dataInicio = this.filtroDataInicio();
      if (this.filtroDataFim()) params.dataFim = this.filtroDataFim();
      params.filtroData = this.filtroDataTipo();
    }
    this.http.get<any>(this.apiUrl, { params }).subscribe({
      next: r => {
        const lista: CompraList[] = r.data ?? [];
        this.compras.set(lista);
        this.podarSelecao(lista);
        this.carregando.set(false);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao carregar compras.');
        this.carregando.set(false);
      }
    });
  }

  /**
   * Poda a selecao pra conter so' notas ainda presentes na lista E selecionaveis.
   * Sem isso, um id "fantasma" (nota finalizada ou fora do filtro atual) fica preso
   * em notasSelecionadas e trava o gate dos passos (todasSelecionadasVinculadas falha
   * porque notas.length !== sel.size) e faz o "Vincular" agir em notas invisiveis.
   */
  private podarSelecao(lista: CompraList[]) {
    const atual = this.notasSelecionadas();
    if (atual.size === 0) return;
    const validos = new Set(lista.filter(c => this.isNotaSelecionavel(c)).map(c => c.id));
    const podado = new Set([...atual].filter(id => validos.has(id)));
    if (podado.size !== atual.size) this.notasSelecionadas.set(podado);
  }

  selecionar(compra: CompraList) {
    this.compraSelecionada.set(compra);
  }

  /** Duplo-clique numa nota: abre a MESMA tela de vinculo do passo "Vincular". */
  abrirDetalhe(compra?: CompraList) {
    const c = compra || this.compraSelecionada();
    if (!c) return;
    this.compraSelecionada.set(c);
    this.abrirVincularLote([c.id]);
  }

  voltarLista() {
    this.modo.set('lista');
    this.compraDetalhe.set(null);
    this.compraSelecionada.set(null);
    this.carregar();
  }

  // ── Importar XML ──────────────────────────────────────────────

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];

    if (!file.name.toLowerCase().endsWith('.xml')) {
      this.erro.set('Selecione um arquivo XML.');
      input.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const xmlConteudo = reader.result as string;
      this.importarXml(xmlConteudo);
      input.value = '';
    };
    reader.readAsText(file);
  }

  private async importarXml(xmlConteudo: string) {
    if (!await this.verificarPermissao('i')) return;

    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);

    this.importando.set(true);
    this.erro.set('');

    const headers = this.headerLiberacao();
    this.http.post<any>(`${this.apiUrl}/importar-xml`, { xmlConteudo, filialId }, { headers }).subscribe({
      next: r => {
        this.importando.set(false);
        this.carregar();
        // Abre a tela de vinculo da nota recem-importada (tela unica).
        this.abrirVincularLote([r.data.id]);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao importar XML.');
        this.importando.set(false);
      }
    });
  }

  /**
   * Passo 1 (Vincular): abre a tela de vinculo com os produtos das notas informadas
   * (ou das selecionadas, se `ids` vier vazio). E' a UNICA tela de vinculo — tambem
   * usada pelo duplo-clique numa nota e pos-importacao de XML. Duplo-clique num item
   * abre o modal de pesquisar/cadastrar. NAO vincula sozinho.
   */
  async abrirVincularLote(ids?: number[]) {
    const notaIds = ids ?? Array.from(this.notasSelecionadas());
    if (notaIds.length === 0 || this.vincularCarregando()) return;
    this.vincularCarregando.set(true);
    this.erro.set('');
    try {
      const { itens, falhas } = await this.carregarItensVincular(notaIds);
      if (itens.length === 0 && falhas > 0) {
        this.erro.set('Nao foi possivel carregar as notas selecionadas.');
      } else {
        this.vincularItens.set(itens);
        this.modo.set('vincular');
        if (falhas > 0) {
          this.toastr.warning(`${falhas} nota(s) nao carregaram e ficaram de fora da tela.`, 'Vinculo', { timeOut: 4000, positionClass: 'toast-top-center' });
        }
      }
    } catch {
      this.erro.set('Erro ao carregar itens para vinculo.');
    } finally {
      this.vincularCarregando.set(false);
    }
  }

  /**
   * Carrega e achata os itens das notas (tagueando cada um com o contexto da nota).
   * Usa allSettled: uma nota que falhe NAO derruba o lote todo (retorna quantas falharam).
   */
  private async carregarItensVincular(ids: number[]): Promise<{ itens: CompraProdutoVinc[]; falhas: number }> {
    const settled = await Promise.allSettled(ids.map(id => this.http.get<any>(`${this.apiUrl}/${id}`).toPromise()));
    const itens: CompraProdutoVinc[] = [];
    let falhas = 0;
    for (const s of settled) {
      if (s.status !== 'fulfilled') { falhas++; continue; }
      const d = s.value?.data;
      if (!d) continue;
      for (const p of (d.produtos ?? [])) {
        itens.push({
          ...p,
          compraId: d.id,
          compraNumeroNf: d.numeroNf,
          compraSerieNf: d.serieNf,
          compraFornecedorId: d.fornecedorId,
          compraFornecedorNome: d.fornecedorNome,
          compraFilialId: d.filialId,
        });
      }
    }
    // Nao vinculados primeiro; depois agrupa por nota e ordem do item.
    itens.sort((a, b) =>
      (a.vinculado === b.vinculado)
        ? ((a.compraId - b.compraId) || (a.numeroItem - b.numeroItem))
        : (a.vinculado ? 1 : -1)
    );
    return { itens, falhas };
  }

  voltarDoVincular() {
    this.modo.set('lista');
    this.vincularItens.set([]);
    this.carregar();
  }

  /**
   * Botao secundario dentro da tela de vinculo: tenta casar automaticamente
   * (re-vincular por codigo de barras/fornecedor, idempotente) as notas da tela
   * e recarrega a lista combinada. Itens sem match seguem pro cadastro manual.
   */
  async vincularAutomatico() {
    const ids = Array.from(new Set(this.vincularItens().map(p => p.compraId)));
    if (ids.length === 0 || this.vinculandoLote()) return;
    this.vinculandoLote.set(true);
    this.vincularCarregando.set(true); // trava o grid (esconde as linhas) enquanto roda
    this.erro.set('');
    let ok = 0, falhas = 0;
    for (const id of ids) {
      try {
        await this.http.post<any>(`${this.apiUrl}/${id}/re-vincular`, {}).toPromise();
        ok++;
      } catch {
        falhas++;
      }
    }
    let recarregados: CompraProdutoVinc[] | null = null;
    try {
      recarregados = (await this.carregarItensVincular(ids)).itens;
    } catch { /* mantem a lista atual se o reload falhar */ }
    this.vinculandoLote.set(false);
    this.vincularCarregando.set(false);
    // Anti-stale: se o usuario saiu da tela (Voltar) durante os awaits, nao repovoa nem avisa.
    if (this.modo() !== 'vincular') return;
    if (recarregados) this.vincularItens.set(recarregados);
    if (falhas > 0) {
      this.toastr.warning(`${ok} nota(s) revinculada(s), ${falhas} com erro. Itens sem correspondencia: cadastre no duplo-clique.`, 'Vinculo automatico', { timeOut: 5000, positionClass: 'toast-top-center' });
    } else {
      this.toastr.success(`Auto-vinculo concluido. Itens sem correspondencia precisam de cadastro manual (duplo-clique no item).`, 'Vinculo automatico', { timeOut: 4000, positionClass: 'toast-top-center' });
    }
  }

  // ── Vincular produto ──────────────────────────────────────────

  abrirModalVincular(item: CompraProduto) {
    this.itemParaVincular.set(item);
    this.buscaProduto.set('');
    this.produtosBusca.set([]);
    this.modalVincular.set(true);

    // Busca silenciosa por código de barras — se encontrar, vincula sem mostrar nada
    if (!item.vinculado && item.codigoBarrasXml && item.codigoBarrasXml.length >= 3) {
      const url = `${this.produtosApiUrl}?busca=${encodeURIComponent(item.codigoBarrasXml)}`;
      this.http.get<any>(url).subscribe({
        next: r => {
          const produtos: any[] = r.data ?? [];
          const match = produtos.find((p: any) => p.codigoBarras === item.codigoBarrasXml);
          if (match) {
            this.selecionarProduto({ id: match.id, nome: match.nome, codigoBarras: match.codigoBarras });
          }
        }
      });
    }
  }

  fecharModalVincular() {
    this.modalVincular.set(false);
    this.itemParaVincular.set(null);
    this.produtosBusca.set([]);
  }

  buscarProdutos() {
    const termo = this.buscaProduto().trim();
    if (!termo || termo.length < 3) {
      this.erro.set('Digite ao menos 3 caracteres para buscar.');
      return;
    }

    this.buscandoProduto.set(true);
    this.erro.set('');
    const url = `${this.produtosApiUrl}?busca=${encodeURIComponent(termo)}`;
    this.http.get<any>(url).subscribe({
      next: r => {
        const produtos: any[] = r.data ?? [];
        this.produtosBusca.set(
          produtos.slice(0, 30).map((p: any) => ({
            id: p.id,
            nome: p.nome,
            codigoBarras: p.codigoBarras
          }))
        );
        this.buscandoProduto.set(false);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao buscar produtos.');
        this.buscandoProduto.set(false);
      }
    });
  }

  async selecionarProduto(produto: ProdutoBusca) {
    const item = this.itemParaVincular();
    if (!item) return;
    if (!await this.verificarPermissao('a')) return;

    this.vinculando.set(item.id);
    const headers = this.headerLiberacao();
    this.http.post<any>(`${this.apiUrl}/vincular`,
      { compraProdutoId: item.id, produtoId: produto.id }, { headers }
    ).subscribe({
      next: r => {
        this.atualizarItemNoDetalhe(r.data);
        this.fecharModalVincular();
        this.vinculando.set(null);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao vincular.');
        this.vinculando.set(null);
      }
    });
  }

  async desvincular(item: CompraProduto) {
    if (!confirm(`Desvincular "${item.produtoNome}" deste item?`)) return;
    if (!await this.verificarPermissao('a')) return;

    this.vinculando.set(item.id);
    const headers = this.headerLiberacao();
    this.http.post<any>(`${this.apiUrl}/desvincular/${item.id}`, {}, { headers }).subscribe({
      next: r => {
        this.atualizarItemNoDetalhe(r.data);
        this.vinculando.set(null);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao desvincular.');
        this.vinculando.set(null);
      }
    });
  }

  async desvincularDaModal(item: CompraProduto) {
    if (!confirm(`Desvincular "${item.produtoNome}" deste item?`)) return;
    if (!await this.verificarPermissao('a')) return;

    this.vinculando.set(item.id);
    const headers = this.headerLiberacao();
    this.http.post<any>(`${this.apiUrl}/desvincular/${item.id}`, {}, { headers }).subscribe({
      next: r => {
        this.atualizarItemNoDetalhe(r.data);
        // Atualizar o item na modal preservando o contexto da nota (compra*) — que so'
        // existe no item da tela de vinculo em lote e nao vem no r.data do backend.
        this.itemParaVincular.set({ ...item, ...r.data });
        this.vinculando.set(null);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao desvincular.');
        this.vinculando.set(null);
      }
    });
  }

  async cadastrarProduto() {
    // Vincular o produto recém-criado exige 'compras/a' (igual ao vínculo manual).
    // Checa ANTES de abrir o cadastro: evita criar produto órfão sem poder vincular
    // e captura o token de liberação do supervisor pro vínculo automático (headerLiberacao).
    if (!await this.verificarPermissao('a')) return;
    const item = this.itemParaVincular();
    const detalhe = this.compraDetalhe();
    const itemVinc = item as (CompraProdutoVinc | null);
    // Contexto da nota: do detalhe (modo 'detalhe') OU do proprio item (modo 'vincular'
    // em lote, onde compraDetalhe e' null e cada item carrega o contexto da sua nota).
    const ctx = (itemVinc && itemVinc.compraId != null)
      ? { compraId: itemVinc.compraId, fornecedorId: itemVinc.compraFornecedorId, fornecedorNome: itemVinc.compraFornecedorNome, filialId: itemVinc.compraFilialId }
      : (detalhe
          ? { compraId: detalhe.id, fornecedorId: detalhe.fornecedorId, fornecedorNome: detalhe.fornecedorNome, filialId: detalhe.filialId }
          : null);
    if (item && ctx) {
      sessionStorage.setItem('zulex_preCadastroProduto', JSON.stringify({
        compraId: ctx.compraId,
        compraProdutoId: item.id,
        nome: item.descricaoXml,
        codigoBarras: item.codigoBarrasXml,
        ncmXml: item.ncmXml,
        cestXml: item.cestXml,
        codigoProdutoFornecedor: item.codigoProdutoFornecedor,
        descricaoXml: item.descricaoXml,
        fornecedorId: ctx.fornecedorId,
        fornecedorNome: ctx.fornecedorNome,
        filialId: ctx.filialId,
        quantidade: item.quantidade,
        valorUnitario: item.valorUnitario,
        valorStTotal: item.fiscal?.valorSt ?? 0,
        valorIpiTotal: item.fiscal?.valorIpi ?? 0,
        valorOutrosTotal: item.valorOutros ?? 0,
        valorFreteTotal: item.valorFrete ?? 0,
        pmc: item.precoMaximoConsumidor ?? 0,
        origemMercadoria: item.fiscal?.origemMercadoria,
        cstIcms: item.fiscal?.cstIcms,
        aliquotaIcms: item.fiscal?.aliquotaIcms ?? 0,
        cstPis: item.fiscal?.cstPis,
        aliquotaPis: item.fiscal?.aliquotaPis ?? 0,
        cstCofins: item.fiscal?.cstCofins,
        aliquotaCofins: item.fiscal?.aliquotaCofins ?? 0,
        codigoAnvisa: item.codigoAnvisa,
      }));
      // Guarda o item pra vincular automaticamente quando o produto for salvo.
      this.cadastroItemId.set(item.id);
    }
    this.fecharModalVincular();
    // Abre o cadastro de produto como MODAL sobre a entrada de nota (o pré-cadastro
    // vai via sessionStorage; ao salvar, o modal emite e a gente vincula sozinho).
    this.modalCadastroProduto.set(true);
  }

  // ── Cadastro de produto em modal (entrada de nota) ─────────────────
  modalCadastroProduto = signal(false);
  cadastroItemId = signal<number | null>(null);

  fecharModalCadastroProduto() {
    this.modalCadastroProduto.set(false);
    this.cadastroItemId.set(null);
    sessionStorage.removeItem('zulex_preCadastroProduto');
  }

  /** O produto acabou de ser criado no modal → vincula no item da nota e fecha. */
  onProdutoCadastrado(produto: { id: number; nome: string }) {
    const compraProdutoId = this.cadastroItemId();
    this.modalCadastroProduto.set(false);
    this.cadastroItemId.set(null);
    if (!compraProdutoId) return;
    this.vinculando.set(compraProdutoId);
    const headers = this.headerLiberacao();
    this.http.post<any>(`${this.apiUrl}/vincular`,
      { compraProdutoId, produtoId: produto.id }, { headers }
    ).subscribe({
      next: r => { this.atualizarItemNoDetalhe(r.data); this.vinculando.set(null); },
      error: e => { this.erro.set(e?.error?.message || 'Produto criado, mas houve erro ao vincular.'); this.vinculando.set(null); }
    });
  }

  private atualizarItemNoDetalhe(itemAtualizado: CompraProduto) {
    const detalhe = this.compraDetalhe();
    if (detalhe) {
      const produtos = detalhe.produtos.map(p =>
        p.id === itemAtualizado.id ? { ...p, ...itemAtualizado } : p
      );
      this.compraDetalhe.set({ ...detalhe, produtos });
    }
    // Tela de vinculo em lote: atualiza o item na lista combinada (id e' PK global).
    // Preserva o contexto da nota (spread do item existente primeiro).
    const vinc = this.vincularItens();
    if (vinc.length > 0 && vinc.some(p => p.id === itemAtualizado.id)) {
      this.vincularItens.set(vinc.map(p =>
        p.id === itemAtualizado.id ? { ...p, ...itemAtualizado } : p
      ));
    }
  }

  /** Edita a fracao (fator de conversao) do item na tela de vinculo; afeta o estoque na finalizacao. */
  atualizarFracao(item: CompraProduto, fracao: number) {
    if (fracao < 1 || fracao === (item.fracao || 1)) return;
    this.http.post<any>(`${this.apiUrl}/atualizar-fracao/${item.id}`, { fracao }).subscribe({
      next: r => { this.atualizarItemNoDetalhe(r.data); }
    });
  }

  // ── Finalização ────────────────────────────────────────────────

  abrirFinalizacao() {
    const selecionadas = this.notasSelecionadas();
    if (selecionadas.size !== 1) {
      this.toastr.warning('Selecione apenas uma nota para finalizar.', 'Atenção', { timeOut: 3000, positionClass: 'toast-top-center' });
      return;
    }
    const compraId = Array.from(selecionadas)[0];
    this.finCarregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${compraId}/dados-finalizacao`).subscribe({
      next: r => {
        this.finDados.set(r.data);
        this.finDuplicatasEntregues.set(false);
        this.finNotaPaga.set(false);
        this.finDesmembramentos.set({});
        this.finDesmembrarItem.set(null);
        this.modo.set('finalizacao');
        this.finCarregando.set(false);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao carregar dados.');
        this.finCarregando.set(false);
      }
    });
  }

  voltarDaFinalizacao() {
    this.modo.set('lista');
    this.finDados.set(null);
    this.carregar();
  }

  /** Adiciona uma linha de duplicata (o usuario pode lancar mais de uma). */
  adicionarDuplicata() {
    this.finDados.update((d: any) => {
      if (!d) return d;
      const duplicatas = d.duplicatas ?? [];
      const nova = { numero: String(duplicatas.length + 1).padStart(3, '0'), vencimento: '', descontos: 0, encargos: 0, valor: 0 };
      return { ...d, duplicatas: [...duplicatas, nova] };
    });
  }

  removerDuplicata(i: number) {
    this.finDados.update((d: any) => {
      if (!d) return d;
      return { ...d, duplicatas: (d.duplicatas ?? []).filter((_: any, idx: number) => idx !== i) };
    });
  }

  // ── Desmembrar em grade (finalizacao) ──────────────────────────
  /** Itens da nota que sao produto de grade (podem ser desmembrados). */
  itensGrade(): any[] {
    return (this.finDados()?.lotes ?? []).filter((l: any) => l.controlaGrade && (l.variacoes?.length ?? 0) > 0);
  }

  totalDesmembradoItem(compraProdutoId: number): number {
    return (this.finDesmembramentos()[compraProdutoId] ?? []).reduce((s, x) => s + (+x.quantidade || 0), 0);
  }

  abrirDesmembrar(item: any) {
    const salvos = new Map((this.finDesmembramentos()[item.compraProdutoId] ?? []).map(s => [s.variacaoId, s.quantidade]));
    this.finDesmembrarSkus.set((item.variacoes ?? []).map((v: any) => ({
      variacaoId: v.id, descricao: v.descricao, quantidade: salvos.get(v.id) ?? 0
    })));
    this.finDesmembrarItem.set(item);
  }

  fecharDesmembrar() { this.finDesmembrarItem.set(null); }

  setDesmembrarSkuQtd(i: number, valor: any) {
    this.finDesmembrarSkus.update(sk => sk.map((s, idx) => idx === i ? { ...s, quantidade: Math.max(0, +valor || 0) } : s));
  }

  salvarDesmembrar() {
    const item = this.finDesmembrarItem();
    if (!item) return;
    if (this.totalDesmembrarModal() > item.qtdeEstoque) {
      this.toastr.warning(`A soma (${this.totalDesmembrarModal()}) excede a quantidade da nota (${item.qtdeEstoque}).`, 'Desmembrar', { timeOut: 3500, positionClass: 'toast-top-center' });
      return;
    }
    const skus = this.finDesmembrarSkus().filter(s => s.quantidade > 0).map(s => ({ variacaoId: s.variacaoId, quantidade: s.quantidade }));
    this.finDesmembramentos.update(m => ({ ...m, [item.compraProdutoId]: skus }));
    this.finDesmembrarItem.set(null);
  }

  finalizar() {
    const dados = this.finDados();
    if (!dados) return;
    const usuario = this.auth.usuarioLogado();

    this.abrirPrecModal('Finalizar Nota',
      `Confirma a finalizacao da NF ${dados.numeroNf}? O estoque sera atualizado e os precos aplicados.`, () => {
      this.finAplicando.set(true);
      this.http.post<any>(`${this.apiUrl}/finalizar`, {
        compraId: dados.compraId,
        duplicatasEntregues: this.finDuplicatasEntregues(),
        notaPaga: this.finNotaPaga(),
        nomeUsuario: usuario?.nome || '',
        duplicatas: dados.duplicatas,
        lotes: dados.lotes.map((l: any) => ({
          compraProdutoId: l.compraProdutoId,
          lote: l.lote,
          dataFabricacao: l.dataFabricacao,
          dataValidade: l.dataValidade
        })),
        desmembramentos: Object.entries(this.finDesmembramentos())
          .filter(([, skus]) => skus.length > 0)
          .map(([compraProdutoId, skus]) => ({ compraProdutoId: +compraProdutoId, skus }))
      }).subscribe({
        next: r => {
          this.finAplicando.set(false);
          const d = r.data;
          this.abrirPrecModal('Nota Finalizada',
            `${d.produtosAtualizados} produto(s) atualizado(s).\n${d.precosAplicados} preco(s) aplicado(s).\nEstoque adicionado: ${d.estoqueAdicionado}`, null);
          this.voltarDaFinalizacao();
        },
        error: e => {
          this.erro.set(e?.error?.message || 'Erro ao finalizar.');
          this.finAplicando.set(false);
        }
      });
    });
  }

  // ── SEFAZ ──────────────────────────────────────────────────────

  buscarSefaz() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.sefazConsultando.set(true);
    this.erro.set('');
    this.modo.set('sefaz');

    // Buscar novas do SEFAZ e depois carregar cache completo
    this.http.post<any>(`${this.sefazApiUrl}/consultar-nfe`, { filialId }).subscribe({
      next: r => {
        if (r.data?.mensagem) this.toastr.info(r.data.mensagem, 'SEFAZ', { timeOut: 4000, positionClass: 'toast-top-center' });
        this.carregarCacheSefaz();
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao consultar SEFAZ.');
        this.sefazConsultando.set(false);
        // Mesmo com erro, mostrar o cache
        this.carregarCacheSefaz();
      }
    });
  }

  carregarCacheSefaz() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.http.get<any>(`${this.sefazApiUrl}/notas/${filialId}`).subscribe({
      next: r => {
        this.sefazNotas.set(r.data ?? []);
        this.sefazConsultando.set(false);
        this.modo.set('sefaz');
      },
      error: () => this.sefazConsultando.set(false)
    });
  }

  importarNotaSefaz(nota: any) {
    if (!nota.temXml) {
      this.toastr.warning('XML completo nao disponivel. Manifeste "Ciencia" primeiro.', 'Atenção', { timeOut: 4000, positionClass: 'toast-top-center' });
      return;
    }
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.sefazImportando.set(nota.chaveNfe);

    // Buscar XML completo por chave do cache/SEFAZ, depois importar
    this.http.post<any>(`${this.sefazApiUrl}/consultar-chave`, { filialId, chaveNfe: nota.chaveNfe }).subscribe({
      next: r => {
        const xml = r.data?.notas?.[0]?.xmlCompleto;
        if (!xml) {
          this.toastr.error('XML nao disponivel.', 'Erro', { timeOut: 3000, positionClass: 'toast-top-center' });
          this.sefazImportando.set(null);
          return;
        }
        this.http.post<any>(`${this.apiUrl}/importar-xml`, { xmlConteudo: xml, filialId }).subscribe({
          next: () => {
            this.toastr.success(`NF ${nota.numeroNf || ''} importada!`, 'OK', { timeOut: 3000, positionClass: 'toast-top-center' });
            this.sefazImportando.set(null);
            this.carregarCacheSefaz();
          },
          error: e => {
            this.toastr.error(e?.error?.message || 'Erro ao importar.', 'Erro', { timeOut: 4000, positionClass: 'toast-top-center' });
            this.sefazImportando.set(null);
          }
        });
      },
      error: e => {
        this.toastr.error(e?.error?.message || 'Erro ao buscar XML.', 'Erro', { timeOut: 4000, positionClass: 'toast-top-center' });
        this.sefazImportando.set(null);
      }
    });
  }

  buscarPorChave() {
    const chave = this.sefazChave().trim();
    if (chave.length !== 44) {
      this.toastr.warning('A chave da NF-e deve ter 44 digitos.', 'Atenção', { timeOut: 3000, positionClass: 'toast-top-center' });
      return;
    }

    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.sefazConsultando.set(true);
    this.erro.set('');
    this.modo.set('sefaz');

    this.http.post<any>(`${this.sefazApiUrl}/consultar-chave`, { filialId, chaveNfe: chave }).subscribe({
      next: r => {
        this.sefazNotas.set(r.data?.notas ?? []);
        this.sefazConsultando.set(false);
        if (r.data?.notas?.length > 0) {
          this.toastr.success('Nota encontrada!', 'SEFAZ', { timeOut: 3000, positionClass: 'toast-top-center' });
        } else {
          this.toastr.warning(r.data?.mensagem || 'Nota nao encontrada.', 'SEFAZ', { timeOut: 4000, positionClass: 'toast-top-center' });
        }
        this.sefazChave.set('');
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao consultar SEFAZ.');
        this.sefazConsultando.set(false);
      }
    });
  }

  manifestar(nota: any, tipoEvento: number) {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    const tipoNome = tipoEvento === 210210 ? 'Ciencia' : tipoEvento === 210220 ? 'Desconhecimento' : 'Nao Realizada';

    let justificativa: string | null = null;
    if (tipoEvento === 210240) {
      justificativa = prompt('Informe a justificativa (min 15 caracteres):');
      if (!justificativa || justificativa.length < 15) {
        this.toastr.warning('Justificativa deve ter no minimo 15 caracteres.', 'Atenção', { timeOut: 3000, positionClass: 'toast-top-center' });
        return;
      }
    }

    this.sefazManifestando.set(nota.chaveNfe);
    this.http.post<any>(`${this.sefazApiUrl}/manifestar`, {
      filialId, chaveNfe: nota.chaveNfe, tipoEvento, justificativa
    }).subscribe({
      next: () => {
        this.toastr.success(`${tipoNome} registrada com sucesso!`, 'SEFAZ', { timeOut: 3000, positionClass: 'toast-top-center' });
        this.sefazManifestando.set(null);
        this.carregarCacheSefaz();
      },
      error: e => {
        this.toastr.error(e?.error?.message || 'Erro ao manifestar.', 'Erro', { timeOut: 4000, positionClass: 'toast-top-center' });
        this.sefazManifestando.set(null);
      }
    });
  }

  voltarDoSefaz() {
    this.modo.set('lista');
    this.sefazNotas.set([]);
    this.carregar();
  }

  // ── Conferência ────────────────────────────────────────────────

  abrirConferencia() {
    const selecionadas = this.notasSelecionadas();
    if (selecionadas.size === 0) return;
    const compraIds = Array.from(selecionadas);
    this.confCompraIds.set(compraIds);
    this.confCarregando.set(true);

    // Carregar todos os itens vinculados das notas selecionadas
    const requests = compraIds.map(id => this.http.get<any>(`${this.apiUrl}/${id}`));
    Promise.all(requests.map(r => r.toPromise())).then(responses => {
      const todosItens: CompraProduto[] = [];
      for (const resp of responses) {
        const produtos = resp?.data?.produtos ?? [];
        todosItens.push(...produtos.filter((p: any) => p.vinculado));
      }
      this.confItens.set(todosItens);
      this.modo.set('conferencia');
      this.confCarregando.set(false);
    }).catch(() => {
      this.erro.set('Erro ao carregar itens para conferência.');
      this.confCarregando.set(false);
    });
  }

  voltarDaConferencia() {
    this.modo.set('lista');
    this.confItens.set([]);
    this.carregar();
  }

  onBipar(input: HTMLInputElement) {
    let raw = input.value.trim();
    if (!raw) return;

    // Parse multiplicador: "5*7896509970349" ou "7896509970349"
    let qtde = 1;
    if (raw.includes('*')) {
      const parts = raw.split('*');
      qtde = parseInt(parts[0], 10) || 1;
      raw = parts[1]?.trim() || '';
    }
    if (!raw) return;

    input.value = '';
    input.focus();

    this.http.post<any>(`${this.apiUrl}/bipar`, {
      barras: raw,
      quantidade: qtde,
      compraIds: this.confCompraIds()
    }).subscribe({
      next: r => {
        const d = r.data;
        if (!d.encontrado) {
          this.toastr.error(d.mensagem || 'Produto não encontrado.', 'Erro', { timeOut: 3000, positionClass: 'toast-top-center' });
        } else if (!d.pertenceNota) {
          this.toastr.warning(d.mensagem, 'Atenção', { timeOut: 4000, positionClass: 'toast-top-center' });
        } else {
          // Atualizar o item na lista local
          this.confItens.update(itens => itens.map(i =>
            i.id === d.compraProdutoId ? { ...i, qtdeConferida: d.qtdeConferida, qtdeTotal: d.qtdeTotal } : i
          ));
          if (d.qtdeConferida === d.qtdeTotal) {
            this.toastr.success(`${d.produtoNome} - Conferido!`, 'OK', { timeOut: 2000, positionClass: 'toast-top-center' });
          } else if (d.qtdeConferida > d.qtdeTotal) {
            this.toastr.warning(`${d.produtoNome} - Quantidade excedida!`, 'Atenção', { timeOut: 3000, positionClass: 'toast-top-center' });
          }
        }
      },
      error: e => this.toastr.error(e?.error?.message || 'Erro ao bipar.', 'Erro', { timeOut: 3000, positionClass: 'toast-top-center' })
    });
  }

  onQtdeConfInput(item: CompraProduto, qtde: number) {
    // Atualizar localmente para feedback visual imediato (sem salvar no banco)
    this.confItens.update(itens => itens.map(i =>
      i.id === item.id ? { ...i, qtdeConferida: qtde } : i
    ));
  }

  atualizarQtdeConf(item: CompraProduto, qtde: number) {
    this.http.post<any>(`${this.apiUrl}/atualizar-qtde-conf/${item.id}`, { qtdeConferida: qtde }).subscribe({
      next: r => this.confItens.update(itens => itens.map(i =>
        i.id === r.data.id ? { ...i, qtdeConferida: r.data.qtdeConferida, qtdeTotal: r.data.qtdeTotal } : i
      ))
    });
  }

  confStatusClass(item: CompraProduto): string {
    if (!item.vinculado) return '';
    const fracao = item.fracao > 0 ? item.fracao : 1;
    const total = item.qtdeTotal > 0 ? item.qtdeTotal : item.quantidade * fracao;
    if (item.qtdeConferida >= total && item.qtdeConferida > 0) return item.qtdeConferida > total ? 'conf-excedeu' : 'conf-ok';
    if (item.qtdeConferida > 0) return 'conf-parcial';
    return 'conf-pendente';
  }

  confTotalConferidos(): number { return this.confItens().filter(i => this.confStatusClass(i) === 'conf-ok').length; }
  confTotalPendentes(): number { return this.confItens().filter(i => this.confStatusClass(i) === 'conf-pendente' || this.confStatusClass(i) === 'conf-parcial').length; }

  // ── Excluir compra ────────────────────────────────────────────

  async excluir() {
    const detalhe = this.compraDetalhe();
    const selecionada = this.compraSelecionada();
    const compra = detalhe || selecionada;
    if (!compra) {
      this.toastr.warning('Selecione uma nota para excluir.', 'Atenção', { timeOut: 3000, positionClass: 'toast-top-center' });
      return;
    }
    if (!await this.verificarPermissao('e')) return;

    const msg = (compra as any).status === 3
      ? `Excluir NF ${compra.numeroNf}? A nota esta FINALIZADA — estoque sera revertido!`
      : `Excluir NF ${compra.numeroNf}? Esta acao nao pode ser desfeita.`;

    this.abrirPrecModal('Excluir Nota', msg, () => {
      const headers = this.headerLiberacao();
      this.http.delete<any>(`${this.apiUrl}/${compra.id}`, { headers }).subscribe({
        next: () => {
          this.toastr.success('Nota excluida com sucesso.', 'OK', { timeOut: 3000, positionClass: 'toast-top-center' });
          if (detalhe) this.voltarLista(); else this.carregar();
        },
        error: e => this.erro.set(e?.error?.message || 'Erro ao excluir.')
      });
    });
  }

  // ── Precificação ações ─────────────────────────────────────────

  abrirPrecificacao() {
    const selecionadas = this.notasSelecionadas();
    if (selecionadas.size === 0) return;

    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);

    this.precificacaoCarregando.set(true);
    this.erro.set('');

    this.http.post<any>(`${this.apiUrl}/precificacao`, {
      filialId,
      compraIds: Array.from(selecionadas)
    }).subscribe({
      next: r => {
        const itens = r.data?.itens ?? [];
        this.precificacaoItens.set(itens);
        this.precSelecionados.set(new Set());
        // Verde persistente: itens que ja tiveram preco aplicado (flag do backend).
        this.precAplicados.set(new Set(itens.filter((i: any) => i.precificacaoAplicada).map((i: any) => i.produtoDadosId)));
        this.modo.set('precificacao');
        this.precificacaoCarregando.set(false);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao gerar precificação.');
        this.precificacaoCarregando.set(false);
      }
    });
  }

  voltarDaPrecificacao() {
    this.modo.set('lista');
    this.precificacaoItens.set([]);
    this.precSelecionados.set(new Set());
    this.precAplicados.set(new Set());
  }

  // Edição inline na precificação
  isEditableCol(campo: string): boolean {
    return ['novoPrecoVenda', 'projecaoLucro', 'markup'].includes(campo);
  }

  onPrecCellBlur(item: PrecificacaoItem, campo: string, input: HTMLInputElement) {
    // Parse pt-BR format (virgula decimal) or plain number
    const raw = input.value.replace(/\./g, '').replace(',', '.');
    const valor = parseFloat(raw);
    if (isNaN(valor)) return;
    // So' processa (e auto-seleciona) se o valor MUDOU de fato — blur sem edicao
    // (Tab/click so' pra revisar) nao deve re-selecionar/reaplicar a linha.
    const atual = Number((item as any)[campo]);
    if (!isNaN(atual) && Math.abs(atual - valor) < 0.005) return;
    this.onPrecCellChange(item, campo, valor);
  }

  onPrecCellChange(item: PrecificacaoItem, campo: string, valor: number) {
    // Custo base: preferência conforme seleção, fallback para o outro se 0
    let custoBase = this.precBaseCalculo() === 'CUSTO_MEDIO' ? item.custoMedioAtual : item.custoCompraAtual;
    if (custoBase <= 0) custoBase = item.custoCompraAtual || item.custoMedioAtual || item.custoCompraAnterior || item.custoMedioAnterior;
    if (custoBase <= 0) return;

    let venda = item.novoPrecoVenda;
    let mk = item.markup;
    let proj = item.projecaoLucro;

    if (campo === 'novoPrecoVenda') {
      venda = valor;
      mk = custoBase > 0 ? Math.round(((valor - custoBase) / custoBase) * 100 * 100) / 100 : 0;
      proj = valor > 0 ? Math.min(Math.round(((valor - custoBase) / valor) * 100 * 100) / 100, 99) : 0;
    } else if (campo === 'markup') {
      mk = valor;
      venda = Math.round(custoBase * (1 + valor / 100) * 100) / 100;
      proj = venda > 0 ? Math.min(Math.round(((venda - custoBase) / venda) * 100 * 100) / 100, 99) : 0;
    } else if (campo === 'projecaoLucro') {
      proj = Math.min(valor, 99);
      venda = valor < 100 ? Math.round(custoBase / (1 - valor / 100) * 100) / 100 : venda;
      mk = custoBase > 0 ? Math.round(((venda - custoBase) / custoBase) * 100 * 100) / 100 : 0;
    }

    // Criar objeto novo para forçar reatividade do Angular
    this.precificacaoItens.update(itens => itens.map(i =>
      i.produtoDadosId === item.produtoDadosId
        ? { ...i, novoPrecoVenda: venda, markup: mk, projecaoLucro: proj }
        : i
    ));
    // Editou um preco -> marca o checkbox (indica alteracao a aplicar) e tira o verde
    // de "aplicado" (o valor mudou, nao reflete mais o que ja foi aplicado).
    this.precSelecionados.update(s => { const n = new Set(s); n.add(item.produtoDadosId); return n; });
    this.precAplicados.update(s => {
      if (!s.has(item.produtoDadosId)) return s;
      const n = new Set(s); n.delete(item.produtoDadosId); return n;
    });
  }

  recalcularTodosSugestao() {
    const base = this.precBaseCalculo();
    this.precificacaoItens.update(itens => itens.map(i => {
      let custoBase = base === 'CUSTO_MEDIO' ? i.custoMedioAtual : i.custoCompraAtual;
      if (custoBase <= 0) custoBase = i.custoCompraAtual || i.custoMedioAtual || i.custoCompraAnterior || i.custoMedioAnterior;
      if (custoBase <= 0) return i;

      let venda = i.novoPrecoVenda;
      if (i.projecaoLucro > 0 && i.projecaoLucro < 100) {
        venda = Math.round(custoBase / (1 - i.projecaoLucro / 100) * 100) / 100;
      } else if (i.markup > 0) {
        venda = Math.round(custoBase * (1 + i.markup / 100) * 100) / 100;
      }
      const mk = custoBase > 0 && venda > 0 ? Math.round(((venda - custoBase) / custoBase) * 100 * 100) / 100 : i.markup;
      const proj = venda > 0 && custoBase > 0 ? Math.min(Math.round(((venda - custoBase) / venda) * 100 * 100) / 100, 99) : i.projecaoLucro;
      return { ...i, novoPrecoVenda: venda, markup: mk, projecaoLucro: proj };
    }));
  }

  acatarPmc(tipo: 'NOTA' | 'ABC') {
    const base = this.precBaseCalculo();
    const sel = new Set<number>();
    this.precificacaoItens.update(itens => itens.map(i => {
      const pmc = tipo === 'NOTA' ? i.pmcNota : i.pmcAbcFarma;
      if (pmc <= 0) return i;
      sel.add(i.produtoDadosId);
      let custoBase = base === 'CUSTO_MEDIO' ? i.custoMedioAtual : i.custoCompraAtual;
      if (custoBase <= 0) custoBase = i.custoCompraAtual || i.custoMedioAtual || i.custoCompraAnterior || i.custoMedioAnterior;
      const mk = custoBase > 0 ? Math.round(((pmc - custoBase) / custoBase) * 100 * 100) / 100 : 0;
      const proj = pmc > 0 ? Math.min(Math.round(((pmc - custoBase) / pmc) * 100 * 100) / 100, 99) : 0;
      return { ...i, novoPrecoVenda: pmc, markup: mk, projecaoLucro: proj };
    }));
    this.precSelecionados.set(sel);
  }

  // Seleção precificação
  togglePrecItem(id: number) {
    this.precSelecionados.update(s => { const n = new Set(s); if (n.has(id)) n.delete(id); else n.add(id); return n; });
  }
  togglePrecTodos() {
    const itens = this.precificacaoItens();
    if (this.precSelecionados().size === itens.length) this.precSelecionados.set(new Set());
    else this.precSelecionados.set(new Set(itens.map(i => i.produtoDadosId)));
  }
  isPrecSelecionado(id: number): boolean { return this.precSelecionados().has(id); }
  todosPrecSelecionados(): boolean { return this.precificacaoItens().length > 0 && this.precSelecionados().size === this.precificacaoItens().length; }

  selecionarPrecPor(tipo: string) {
    if (!tipo) return;
    const itens = this.precificacaoItens();
    let filtrados: PrecificacaoItem[];
    switch (tipo) {
      case 'TODAS': filtrados = itens; break;
      case 'AUMENTO': filtrados = itens.filter(i => i.varCustoCompraPercent > 0 || i.varCustoMedioPercent > 0); break;
      case 'REDUCAO': filtrados = itens.filter(i => i.varCustoCompraPercent < 0 || i.varCustoMedioPercent < 0); break;
      case 'PMC_NOTA': filtrados = itens.filter(i => i.pmcNota > 0); break;
      case 'PMC_ABC': filtrados = itens.filter(i => i.pmcAbcFarma > 0); break;
      default: return;
    }
    this.precSelecionados.set(new Set(filtrados.map(i => i.produtoDadosId)));
  }

  // Aplicar precificação
  aplicarPrecificacao() {
    const selecionados = this.precSelecionados();
    if (selecionados.size === 0) {
      this.abrirPrecModal('Nenhum produto selecionado', 'Selecione os produtos que deseja atualizar usando os checkboxes.', null);
      return;
    }

    const momento = this.precMomentoAplicacao();
    const msgMomento = momento === 'AGORA'
      ? 'Os precos serao atualizados AGORA nos produtos.'
      : 'Os precos serao salvos e aplicados na FINALIZACAO da nota.';

    this.abrirPrecModal('Aplicar Ajuste de Precos',
      `${selecionados.size} produto(s) selecionado(s). ${msgMomento}`, () => {
      const usuario = this.auth.usuarioLogado();
      const filialId = parseInt(usuario?.filialId || '1', 10);
      const itens = this.precificacaoItens().filter(i => selecionados.has(i.produtoDadosId));
      this.precAplicando.set(true);

      if (momento === 'AGORA') {
        this.http.post<any>(`${this.apiUrl}/aplicar-precificacao`, {
          filialId,
          nomeUsuario: usuario?.nome || '',
          itens: itens.map(i => ({
            produtoDadosId: i.produtoDadosId,
            produtoId: i.produtoId,
            compraProdutoId: i.compraProdutoId,
            novoPrecoVenda: i.novoPrecoVenda,
            novoMarkup: i.markup,
            novaProjecaoLucro: i.projecaoLucro,
            novoCustoCompra: i.custoCompraAtual,
            novoCustoMedio: i.custoMedioAtual,
            novoPmc: i.pmcNota > 0 ? i.pmcNota : i.pmcAbcFarma
          }))
        }).subscribe({
          next: r => {
            this.precAplicando.set(false);
            // Marcar itens aplicados
            const idsAplicados = new Set(itens.map(i => i.produtoDadosId));
            this.precificacaoItens.update(all => all.map(i =>
              idsAplicados.has(i.produtoDadosId) ? { ...i, precoVendaAtual: i.novoPrecoVenda } : i
            ));
            this.precAplicados.update(s => new Set([...s, ...idsAplicados]));
            this.precSelecionados.set(new Set());
            this.abrirPrecModal('Ajuste Aplicado',
              `${r.data?.alterados || 0} produto(s) atualizado(s) com sucesso.`, null);
          },
          error: e => { this.erro.set(e?.error?.message || 'Erro ao aplicar.'); this.precAplicando.set(false); }
        });
      } else {
        // Salvar sugestões para aplicar na finalização
        this.http.post<any>(`${this.apiUrl}/salvar-sugestoes`, {
          itens: itens.map(i => ({
            compraProdutoId: i.compraProdutoId,
            sugestaoVenda: i.novoPrecoVenda,
            sugestaoMarkup: i.markup,
            sugestaoProjecao: i.projecaoLucro,
            sugestaoCustoMedio: i.custoMedioAtual
          }))
        }).subscribe({
          next: r => {
            this.precAplicando.set(false);
            // FINALIZACAO nao "aplica" agora (fica pendente ate finalizar), entao nao pinta verde.
            this.precSelecionados.set(new Set());
            this.abrirPrecModal('Sugestoes Salvas',
              `${r.data?.salvos || 0} sugestao(oes) salva(s). Serao aplicadas na finalizacao da nota.`, null);
          },
          error: e => { this.erro.set(e?.error?.message || 'Erro ao salvar.'); this.precAplicando.set(false); }
        });
      }
    });
  }

  // Modal precificação
  abrirPrecModal(titulo: string, msg: string, cb: (() => void) | null) {
    this.precModalTitulo.set(titulo);
    this.precModalMsg.set(msg);
    this.precModalCallback = cb;
    this.precModalAberto.set(true);
  }
  confirmarPrecModal() { this.precModalAberto.set(false); if (this.precModalCallback) this.precModalCallback(); }
  fecharPrecModal() { this.precModalAberto.set(false); }

  ordenarPrec(campo: string) {
    if (this.sortPrecCol() === campo) this.sortPrecDir.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortPrecCol.set(campo); this.sortPrecDir.set('asc'); }
  }

  sortPrecIcon(campo: string): string {
    return this.sortPrecCol() === campo ? (this.sortPrecDir() === 'asc' ? '▲' : '▼') : '⇅';
  }

  // ── Precificação colunas ───────────────────────────────────────

  togglePrecColuna(campo: string) {
    this.precColunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarPrecColunas();
  }

  restaurarPrecColunasPadrao() {
    this.precColunas.set(this.precColunasDefault());
    localStorage.removeItem(this.PREC_COLUNAS_KEY);
  }

  private carregarPrecColunas() {
    try {
      const json = localStorage.getItem('zulex_prec_colunas');
      if (json) {
        const salvo = JSON.parse(json);
        const defaults = this.precColunasDefault();
        return defaults.map(d => {
          const s = salvo.find((x: any) => x.campo === d.campo);
          return s ? { ...d, visivel: s.visivel, largura: s.largura ?? d.largura } : d;
        });
      }
    } catch {}
    return this.precColunasDefault();
  }

  private salvarPrecColunas() {
    localStorage.setItem(this.PREC_COLUNAS_KEY, JSON.stringify(
      this.precColunas().map(c => ({ campo: c.campo, visivel: c.visivel, largura: c.largura }))
    ));
  }

  getPrecCellValue(item: any, campo: string): string {
    if (campo === 'ajustado') {
      // Status = "aplicado": ✓ para itens cujo preco ja foi aplicado (sincronizado com o verde).
      return this.precAplicados().has(item.produtoDadosId) ? '✓' : '';
    }
    const v = item[campo];
    if (v === null || v === undefined) return '--';
    if (campo.startsWith('var')) return (v > 0 ? '+' : '') + v.toFixed(2) + '%';
    if (typeof v === 'number') return v === 0 && (campo.startsWith('pmc')) ? '--' : v.toFixed(2).replace('.', ',');
    return String(v);
  }

  getAjustadoClass(item: any): string {
    return this.precAplicados().has(item.produtoDadosId) ? 'ajustado-ok' : '';
  }

  isVarColumn(campo: string): boolean { return campo.startsWith('var'); }
  getVarClass(item: any, campo: string): string {
    if (!this.isVarColumn(campo)) return '';
    const v = item[campo] ?? 0;
    return v > 0 ? 'var-subiu' : v < 0 ? 'var-desceu' : '';
  }

  isValorColumn(campo: string): boolean {
    return ['custoCompraAnterior','custoCompraAtual','custoMedioAnterior','custoMedioAtual',
      'precoVendaAtual','sugestaoVendaCustoCompra','sugestaoVendaCustoMedio','pmcNota','pmcAbcFarma'].includes(campo);
  }

  // Drag-and-drop
  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.precColunas.update(cols => {
      const arr = [...cols];
      const [moved] = arr.splice(this.dragColIdx!, 1);
      arr.splice(idx, 0, moved);
      this.dragColIdx = idx;
      return arr;
    });
  }
  onDropCol() { this.dragColIdx = null; this.salvarPrecColunas(); }

  // ── Checkboxes de seleção (notas na lista) ─────────────────────

  isNotaCompleta(c: CompraList): boolean {
    return c.totalItens > 0 && c.itensVinculados === c.totalItens && c.status !== 3; // 3 = Finalizada
  }

  /** Selecionavel no fluxo (vinculo em lote/etapas): tem itens e nao esta finalizada/cancelada. */
  isNotaSelecionavel(c: CompraList): boolean {
    return c.totalItens > 0 && c.status !== 3 && c.status !== 4; // 3=Finalizada, 4=Cancelada
  }

  toggleNotaSelecionada(id: number) {
    this.notasSelecionadas.update(s => {
      const novo = new Set(s);
      if (novo.has(id)) novo.delete(id); else novo.add(id);
      return novo;
    });
  }

  toggleTodasNotasSelecionadas() {
    const selecionaveis = this.comprasFiltradas().filter(c => this.isNotaSelecionavel(c));
    const selecionadas = this.notasSelecionadas();
    if (selecionaveis.length > 0 && selecionaveis.every(c => selecionadas.has(c.id))) {
      this.notasSelecionadas.set(new Set());
    } else {
      this.notasSelecionadas.set(new Set(selecionaveis.map(c => c.id)));
    }
  }

  isNotaSelecionada(id: number): boolean {
    return this.notasSelecionadas().has(id);
  }

  todasNotasSelecionaveisSelecionadas(): boolean {
    const selecionaveis = this.comprasFiltradas().filter(c => this.isNotaSelecionavel(c));
    return selecionaveis.length > 0 && selecionaveis.every(c => this.notasSelecionadas().has(c.id));
  }

  /**
   * Todas as notas selecionadas estao 100% vinculadas (gate dos passos 2-5).
   * Le da lista completa (compras()) pois uma selecionada pode estar fora do filtro.
   */
  todasSelecionadasVinculadas(): boolean {
    const sel = this.notasSelecionadas();
    if (sel.size === 0) return false;
    const notas = this.compras().filter(c => sel.has(c.id));
    return notas.length === sel.size && notas.every(c => this.isNotaCompleta(c));
  }

  qtdeNotasSelecionadas(): number {
    return this.notasSelecionadas().size;
  }

  // ── Helpers ───────────────────────────────────────────────────

  statusLabel(status: number): string {
    const map: Record<number, string> = {
      1: 'Pre-Entrada', 2: 'Conferencia', 3: 'Finalizada', 4: 'Cancelada'
    };
    return map[status] || 'Desconhecido';
  }

  statusClass(status: number): string {
    const map: Record<number, string> = {
      1: 'status-preentrada', 2: 'status-conferencia', 3: 'status-finalizada', 4: 'status-cancelada'
    };
    return map[status] || '';
  }

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('compras', acao)) return true;
    const resultado = await this.modal.permissao('compras', acao);
    if (resultado.tokenLiberacao) this.tokenLiberacao = resultado.tokenLiberacao;
    return resultado.confirmado;
  }

  private dataDefault(offset: number): string {
    const d = new Date(); d.setDate(d.getDate() + offset);
    return d.toISOString().slice(0, 10);
  }

  private headerLiberacao(): { [h: string]: string } {
    if (this.tokenLiberacao) {
      const h: { [h: string]: string } = { 'X-Liberacao': this.tokenLiberacao };
      this.tokenLiberacao = null;
      return h;
    }
    return {};
  }

  // ══ Conferir Lotes (SNGPC + rastreabilidade) ═══════════════════
  /**
   * Passo 4 (Conferir Lotes): abre a TELA (mode 'conferir-lotes') com uma secao por
   * nota selecionada (cada uma tem seu toggle SNGPC e sua tabela de itens/lotes).
   * Carrega N notas via allSettled — uma que falhe nao derruba o lote todo.
   */
  async abrirConferirLotes() {
    const ids = Array.from(this.notasSelecionadas());
    if (ids.length === 0 || this.confLotesCarregando()) return;
    this.confLotesCarregando.set(true);
    this.erro.set('');
    try {
      const settled = await Promise.allSettled(
        ids.map(id => this.http.get<any>(`${this.apiUrl}/${id}/conferencia-lotes`).toPromise())
      );
      const notas: any[] = [];
      let falhas = 0;
      for (const s of settled) {
        if (s.status !== 'fulfilled') { falhas++; continue; }
        const d = s.value?.data;
        if (!d) continue;
        notas.push({ ...d, sngpcOptOut: d.sngpcOptOut === true });
      }
      if (notas.length === 0) {
        this.modal.erro('Erro', 'Nao foi possivel carregar a conferencia de lotes das notas selecionadas.');
      } else {
        this.confLotesNotas.set(notas);
        this.modo.set('conferir-lotes');
        if (falhas > 0) {
          this.toastr.warning(`${falhas} nota(s) nao carregaram e ficaram de fora da tela.`, 'Conferir lotes', { timeOut: 4000, positionClass: 'toast-top-center' });
        }
      }
    } catch {
      this.modal.erro('Erro', 'Erro ao carregar conferencia de lotes.');
    } finally {
      this.confLotesCarregando.set(false);
    }
  }

  voltarDoConferirLotes() {
    this.modo.set('lista');
    this.confLotesNotas.set([]);
    this.carregar();
  }

  /** Muta uma nota (por indice) na lista de conferencia. */
  private atualizarNotaLotes(notaIdx: number, mut: (nota: any) => any) {
    this.confLotesNotas.update((notas: any[]) => {
      if (!notas[notaIdx]) return notas;
      const novo = [...notas];
      novo[notaIdx] = mut(novo[notaIdx]);
      return novo;
    });
  }

  setSngpcOptOut(notaIdx: number, valor: boolean) {
    this.atualizarNotaLotes(notaIdx, nota => ({ ...nota, sngpcOptOut: valor }));
  }

  /**
   * Marca/desmarca UM item como conferido, gravando na hora (lotes daquele item + flag),
   * pra o usuario parar no meio e retomar sabendo o que ja conferiu.
   */
  async toggleItemConferido(notaIdx: number, item: any) {
    const nota = this.confLotesNotas()[notaIdx];
    if (!nota || this.confItemConferindo() === item.compraProdutoId) return;
    const novoConferido = !item.loteConferido;

    // Ao MARCAR, exige que todo lote tenha numero (mesma regra do salvar).
    if (novoConferido) {
      for (const l of item.lotes) {
        if (!l.numeroLote || !l.numeroLote.trim()) {
          this.modal.aviso('Lote invalido', `O item "${item.descricao}" tem um lote sem numero.`);
          return;
        }
      }
    }

    this.confItemConferindo.set(item.compraProdutoId);
    const body = {
      conferido: novoConferido,
      registroMs: item.registroMs,
      lotes: item.lotes.map((l: any) => ({
        id: l.id,
        numeroLote: l.numeroLote,
        dataFabricacao: l.dataFabricacao,
        dataValidade: l.dataValidade,
        quantidade: l.quantidade,
        registroMs: l.registroMs
      }))
    };
    try {
      await this.http.post<any>(`${this.apiUrl}/${nota.compraId}/item-conferido/${item.compraProdutoId}`, body).toPromise();
      this.atualizarNotaLotes(notaIdx, n => ({
        ...n,
        itens: n.itens.map((i: any) =>
          i.compraProdutoId === item.compraProdutoId ? { ...i, loteConferido: novoConferido } : i)
      }));
    } catch (e: any) {
      this.modal.erro('Erro', e?.error?.message || 'Erro ao marcar item conferido.');
    } finally {
      this.confItemConferindo.set(null);
    }
  }

  atualizarLoteCampo(notaIdx: number, itemIdx: number, loteIdx: number, campo: string, valor: any) {
    this.atualizarNotaLotes(notaIdx, nota => {
      const item = nota.itens[itemIdx];
      const lote = { ...item.lotes[loteIdx], [campo]: valor };
      const novoLotes = [...item.lotes];
      novoLotes[loteIdx] = lote;
      const novosItens = [...nota.itens];
      // Editou um lote -> o item deixa de estar "conferido" (o botao volta pra "Conferir"
      // pra reconfirmar/gravar): evita a edicao se perder com o item marcado como salvo.
      novosItens[itemIdx] = { ...item, lotes: novoLotes, loteConferido: false };
      // A nota tambem deixa de estar "conferida" (senao o cabecalho 'Ja conferida' mascara a edicao).
      return { ...nota, itens: novosItens, lotesConferidos: false, lotesConferidosEm: null };
    });
  }

  /** "Confirmar Todos": salva a conferencia de cada nota e marca TODOS os itens como conferido. */
  async salvarConferirLotes() {
    const notas = this.confLotesNotas();
    if (notas.length === 0 || this.confLotesSalvando()) return;

    // Validacao: todo lote precisa de numero.
    for (const nota of notas) {
      for (const item of nota.itens) {
        for (const lote of item.lotes) {
          if (!lote.numeroLote || !lote.numeroLote.trim()) {
            this.modal.aviso('Lote invalido', `NF ${nota.numeroNf}: o item "${item.descricao}" tem um lote sem numero.`);
            return;
          }
        }
      }
    }

    this.confLotesSalvando.set(true);
    let ok = 0, falhas = 0;
    for (const nota of notas) {
      const body = {
        sngpcOptOut: nota.sngpcOptOut === true,
        itens: nota.itens.map((i: any) => ({
          compraProdutoId: i.compraProdutoId,
          registroMs: i.registroMs,
          lotes: i.lotes.map((l: any) => ({
            id: l.id,
            numeroLote: l.numeroLote,
            dataFabricacao: l.dataFabricacao,
            dataValidade: l.dataValidade,
            quantidade: l.quantidade,
            registroMs: l.registroMs
          }))
        }))
      };
      try {
        await this.http.post<any>(`${this.apiUrl}/${nota.compraId}/conferencia-lotes`, body).toPromise();
        ok++;
      } catch {
        falhas++;
      }
    }
    this.confLotesSalvando.set(false);
    if (falhas === 0) {
      // Marca todos os itens como conferido (o backend ja gravou) e FICA na tela mostrando o resultado.
      const agora = new Date().toISOString();
      this.confLotesNotas.update(ns => ns.map(n => ({
        ...n,
        lotesConferidos: true,
        lotesConferidosEm: agora,
        itens: n.itens.map((i: any) => ({ ...i, loteConferido: true }))
      })));
      this.toastr.success(`Conferencia salva — ${ok} nota(s), todos os itens conferidos.`);
    } else {
      this.modal.erro('Erro', `${ok} nota(s) salva(s), ${falhas} com erro. Revise e tente novamente.`);
    }
  }
}
