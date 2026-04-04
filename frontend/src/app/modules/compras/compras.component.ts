import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { TabService } from '../../core/services/tab.service';

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
  infoAdicional: string | null;
  fiscal: any;
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
  imports: [CommonModule, FormsModule],
  templateUrl: './compras.component.html',
  styleUrls: ['./compras.component.scss']
})
export class ComprasComponent implements OnInit, OnDestroy {
  private apiUrl = `${environment.apiUrl}/compras`;
  private produtosApiUrl = `${environment.apiUrl}/produtos`;
  private tokenLiberacao: string | null = null;
  private readonly STATE_KEY = 'zulex_compras_state';

  // ── Estado ────────────────────────────────────────────────────
  modo = signal<'lista' | 'detalhe' | 'precificacao'>('lista');
  compras = signal<CompraList[]>([]);
  compraSelecionada = signal<CompraList | null>(null);
  compraDetalhe = signal<CompraDetalhe | null>(null);
  carregando = signal(false);
  importando = signal(false);
  vinculando = signal<number | null>(null);
  erro = signal('');
  busca = signal('');
  filtroStatus = signal<'todos' | 'preentrada' | 'conferencia' | 'finalizada' | 'cancelada'>('todos');

  // ── Ordenacao lista ───────────────────────────────────────────
  sortColuna = signal<string>('id');
  sortDirecao = signal<'asc' | 'desc'>('desc');

  // ── Ordenacao itens ───────────────────────────────────────────
  sortItemColuna = signal<string>('numeroItem');
  sortItemDirecao = signal<'asc' | 'desc'>('asc');

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

  // ── Precificação ──────────────────────────────────────────────
  precificacaoItens = signal<PrecificacaoItem[]>([]);
  precificacaoCarregando = signal(false);
  sortPrecCol = signal<string>('produtoNome');
  sortPrecDir = signal<'asc' | 'desc'>('asc');
  painelColunasPrecAberto = signal(false);

  private readonly PREC_COLUNAS_KEY = 'zulex_prec_colunas';
  precColunas = signal<{ campo: string; label: string; largura: number; visivel: boolean }[]>(this.carregarPrecColunas());

  private precColunasDefault(): { campo: string; label: string; largura: number; visivel: boolean }[] {
    return [
      { campo: 'produtoId', label: 'Cod', largura: 55, visivel: true },
      { campo: 'produtoNome', label: 'Produto', largura: 200, visivel: true },
      { campo: 'fabricanteNome', label: 'Fabricante', largura: 110, visivel: true },
      { campo: 'custoCompraAnterior', label: 'CC Anterior', largura: 85, visivel: true },
      { campo: 'custoCompraAtual', label: 'CC Atual', largura: 85, visivel: true },
      { campo: 'varCustoCompraPercent', label: '% Var CC', largura: 75, visivel: true },
      { campo: 'custoMedioAnterior', label: 'CM Anterior', largura: 85, visivel: true },
      { campo: 'custoMedioAtual', label: 'CM Atual', largura: 85, visivel: true },
      { campo: 'varCustoMedioPercent', label: '% Var CM', largura: 75, visivel: true },
      { campo: 'precoVendaAtual', label: 'Venda Atual', largura: 90, visivel: true },
      { campo: 'sugestaoVendaCustoCompra', label: 'Sug. CC', largura: 85, visivel: true },
      { campo: 'sugestaoVendaCustoMedio', label: 'Sug. CM', largura: 85, visivel: true },
      { campo: 'pmcNota', label: 'PMC Nota', largura: 80, visivel: true },
      { campo: 'pmcAbcFarma', label: 'PMC ABC', largura: 80, visivel: true },
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

  // ── Modal fiscal ──────────────────────────────────────────────
  modalFiscal = signal(false);
  itemFiscal = signal<CompraProduto | null>(null);

  // ── Computed ──────────────────────────────────────────────────
  comprasFiltradas = computed(() => {
    let lista = [...this.compras()];
    const termo = this.busca().toLowerCase().trim();
    const filtro = this.filtroStatus();

    if (filtro !== 'todos') {
      const statusMap: Record<string, number> = {
        preentrada: 1, conferencia: 2, finalizada: 3, cancelada: 4
      };
      lista = lista.filter(c => c.status === statusMap[filtro]);
    }

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

  itensFiltrados = computed(() => {
    const d = this.compraDetalhe();
    if (!d) return [];
    let itens = [...d.produtos];
    const col = this.sortItemColuna();
    const dir = this.sortItemDirecao() === 'asc' ? 1 : -1;
    itens.sort((a: any, b: any) => {
      const va = a[col] ?? '';
      const vb = b[col] ?? '';
      if (typeof va === 'number' && typeof vb === 'number') return (va - vb) * dir;
      return String(va).localeCompare(String(vb)) * dir;
    });
    return itens;
  });

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private modal: ModalService,
    private tabService: TabService
  ) {}

  ngOnInit() {
    this.carregar();
    this.restaurarEstado();
    this.buscaProduto$.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(termo => {
      this.buscaProduto.set(termo);
      this.buscarProdutos();
    });
  }

  ngOnDestroy() {
    this.persistirEstado();
    this.buscaProduto$.complete();
  }

  onBuscaProdutoInput(valor: string) {
    this.buscaProduto.set(valor);
    this.buscaProduto$.next(valor);
  }

  private persistirEstado() {
    const detalhe = this.compraDetalhe();
    if (this.modo() === 'detalhe' && detalhe) {
      sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
        modo: 'detalhe',
        compraId: detalhe.id
      }));
    } else {
      sessionStorage.removeItem(this.STATE_KEY);
    }
  }

  private restaurarEstado() {
    const raw = sessionStorage.getItem(this.STATE_KEY);
    if (!raw) return;
    try {
      const state = JSON.parse(raw);
      if (state.modo === 'detalhe' && state.compraId) {
        this.carregando.set(true);
        this.http.get<any>(`${this.apiUrl}/${state.compraId}`).subscribe({
          next: r => {
            this.compraDetalhe.set(r.data);
            this.modo.set('detalhe');
            this.carregando.set(false);
          },
          error: () => {
            sessionStorage.removeItem(this.STATE_KEY);
            this.carregando.set(false);
          }
        });
      }
    } catch {
      sessionStorage.removeItem(this.STATE_KEY);
    }
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

  ordenarItens(campo: string) {
    if (this.sortItemColuna() === campo) {
      this.sortItemDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortItemColuna.set(campo);
      this.sortItemDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string {
    return this.sortColuna() === campo
      ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅';
  }

  sortItemIcon(campo: string): string {
    return this.sortItemColuna() === campo
      ? (this.sortItemDirecao() === 'asc' ? '▲' : '▼') : '⇅';
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
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.compras.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao carregar compras.');
        this.carregando.set(false);
      }
    });
  }

  selecionar(compra: CompraList) {
    this.compraSelecionada.set(compra);
  }

  abrirDetalhe(compra?: CompraList) {
    const c = compra || this.compraSelecionada();
    if (!c) return;
    this.compraSelecionada.set(c);
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${c.id}`).subscribe({
      next: r => {
        this.compraDetalhe.set(r.data);
        this.modo.set('detalhe');
        this.carregando.set(false);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao carregar detalhe.');
        this.carregando.set(false);
      }
    });
  }

  voltarLista() {
    this.modo.set('lista');
    this.compraDetalhe.set(null);
    this.compraSelecionada.set(null);
    sessionStorage.removeItem(this.STATE_KEY);
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
        this.compraDetalhe.set(r.data);
        this.modo.set('detalhe');
        this.importando.set(false);
        this.carregar();
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao importar XML.');
        this.importando.set(false);
      }
    });
  }

  // ── Atualizar (re-vincular automaticamente) ────────────────────

  atualizarVinculos() {
    const detalhe = this.compraDetalhe();
    if (!detalhe) return;
    this.carregando.set(true);
    this.http.post<any>(`${this.apiUrl}/${detalhe.id}/re-vincular`, {}).subscribe({
      next: r => {
        this.compraDetalhe.set(r.data);
        this.carregando.set(false);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao atualizar.');
        this.carregando.set(false);
      }
    });
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
        // Atualizar o item na modal
        this.itemParaVincular.set(r.data);
        this.vinculando.set(null);
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao desvincular.');
        this.vinculando.set(null);
      }
    });
  }

  cadastrarProduto() {
    const item = this.itemParaVincular();
    const detalhe = this.compraDetalhe();
    if (item && detalhe) {
      sessionStorage.setItem('zulex_preCadastroProduto', JSON.stringify({
        compraId: detalhe.id,
        compraProdutoId: item.id,
        nome: item.descricaoXml,
        codigoBarras: item.codigoBarrasXml,
        ncmXml: item.ncmXml,
        cestXml: item.cestXml,
        codigoProdutoFornecedor: item.codigoProdutoFornecedor,
        descricaoXml: item.descricaoXml,
        fornecedorId: detalhe.fornecedorId,
        fornecedorNome: detalhe.fornecedorNome,
        filialId: detalhe.filialId,
        quantidade: item.quantidade,
        valorUnitario: item.valorUnitario,
        valorStTotal: item.fiscal?.valorSt ?? 0,
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
    }
    this.fecharModalVincular();
    this.tabService.abrirTab({
      id: 'gerenciar-produtos',
      titulo: 'Gerenciar Produtos',
      rota: '/erp/gerenciar-produtos',
      iconKey: 'pill'
    });
  }

  private atualizarItemNoDetalhe(itemAtualizado: CompraProduto) {
    const detalhe = this.compraDetalhe();
    if (!detalhe) return;
    const produtos = detalhe.produtos.map(p =>
      p.id === itemAtualizado.id ? { ...p, ...itemAtualizado } : p
    );
    this.compraDetalhe.set({ ...detalhe, produtos });
  }

  // ── Excluir compra ────────────────────────────────────────────

  async excluir() {
    const detalhe = this.compraDetalhe();
    if (!detalhe) return;
    if (!await this.verificarPermissao('e')) return;

    if (!confirm(`Excluir a NF ${detalhe.numeroNf}? Esta acao nao pode ser desfeita.`)) return;

    const headers = this.headerLiberacao();
    this.http.delete<any>(`${this.apiUrl}/${detalhe.id}`, { headers }).subscribe({
      next: () => {
        this.voltarLista();
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao excluir.');
      }
    });
  }

  // ── Modal fiscal ──────────────────────────────────────────────

  abrirFiscal(item: CompraProduto) {
    this.itemFiscal.set(item);
    this.modalFiscal.set(true);
  }

  fecharFiscal() {
    this.modalFiscal.set(false);
    this.itemFiscal.set(null);
  }

  // ── Helpers ───────────────────────────────────────────────────

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
        this.precificacaoItens.set(r.data?.itens ?? []);
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
  }

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
    const v = item[campo];
    if (v === null || v === undefined) return '--';
    if (campo.startsWith('var')) return (v > 0 ? '+' : '') + v.toFixed(2) + '%';
    if (typeof v === 'number') return v === 0 && (campo.startsWith('pmc')) ? '--' : v.toFixed(2).replace('.', ',');
    return String(v);
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
    return c.totalItens > 0 && c.itensVinculados === c.totalItens;
  }

  toggleNotaSelecionada(id: number) {
    this.notasSelecionadas.update(s => {
      const novo = new Set(s);
      if (novo.has(id)) novo.delete(id); else novo.add(id);
      return novo;
    });
  }

  toggleTodasNotasSelecionadas() {
    const completas = this.comprasFiltradas().filter(c => this.isNotaCompleta(c));
    const selecionadas = this.notasSelecionadas();
    if (completas.length > 0 && completas.every(c => selecionadas.has(c.id))) {
      this.notasSelecionadas.set(new Set());
    } else {
      this.notasSelecionadas.set(new Set(completas.map(c => c.id)));
    }
  }

  isNotaSelecionada(id: number): boolean {
    return this.notasSelecionadas().has(id);
  }

  todasNotasCompletasSelecionadas(): boolean {
    const completas = this.comprasFiltradas().filter(c => this.isNotaCompleta(c));
    return completas.length > 0 && completas.every(c => this.notasSelecionadas().has(c.id));
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

  /** Na lista, se todos vinculados mostra badge verde mesmo sendo PreEntrada */
  statusClassLista(c: CompraList): string {
    if (c.status === 1 && c.totalItens > 0 && c.itensVinculados === c.totalItens) {
      return 'status-vinculado-ok';
    }
    return this.statusClass(c.status);
  }

  statusLabelLista(c: CompraList): string {
    if (c.status === 1 && c.totalItens > 0 && c.itensVinculados === c.totalItens) {
      return 'Pre-Entrada OK';
    }
    return this.statusLabel(c.status);
  }

  totalVinculados(): number {
    return this.compraDetalhe()?.produtos.filter(p => p.vinculado).length ?? 0;
  }

  totalItens(): number {
    return this.compraDetalhe()?.produtos.length ?? 0;
  }

  percentVinculados(): number {
    const total = this.totalItens();
    if (total === 0) return 0;
    return Math.round((this.totalVinculados() / total) * 100);
  }

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('compras', acao)) return true;
    const resultado = await this.modal.permissao('compras', acao);
    if (resultado.tokenLiberacao) this.tokenLiberacao = resultado.tokenLiberacao;
    return resultado.confirmado;
  }

  private headerLiberacao(): { [h: string]: string } {
    if (this.tokenLiberacao) {
      const h: { [h: string]: string } = { 'X-Liberacao': this.tokenLiberacao };
      this.tokenLiberacao = null;
      return h;
    }
    return {};
  }
}
