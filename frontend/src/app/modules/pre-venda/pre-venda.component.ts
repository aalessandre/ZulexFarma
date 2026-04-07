import { Component, signal, computed, OnInit, OnDestroy, HostListener, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface PreVendaItem {
  produtoId: number;
  produtoCodigo: string;
  produtoNome: string;
  fabricante?: string;
  precoVenda: number;
  quantidade: number;
  percentualDesconto: number;
  valorDesconto: number;
  precoUnitario: number;
  total: number;
  estoqueAtual?: number;
  unidade?: string;
}

interface TipoPagBtn {
  id: number;
  nome: string;
}

interface ClienteLookup {
  id: number;
  nome: string;
  cpfCnpj?: string;
}

interface ColaboradorLookup {
  id: number;
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
  { campo: 'produtoCodigo',     label: 'Codigo',          largura: 80,  minLargura: 60,  padrao: true, tipo: 'texto' },
  { campo: 'produtoNome',       label: 'Nome do Produto', largura: 280, minLargura: 150, padrao: true, tipo: 'texto' },
  { campo: 'fabricante',        label: 'Fabricante',      largura: 130, minLargura: 80,  padrao: true, tipo: 'texto' },
  { campo: 'precoVenda',        label: 'Preco Venda',     largura: 100, minLargura: 70,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'quantidade',        label: 'Qtde',            largura: 70,  minLargura: 50,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'percentualDesconto', label: 'Desconto',       largura: 80,  minLargura: 60,  padrao: true, tipo: 'numero', editavel: true },
  { campo: 'precoUnitario',     label: 'Preco Unitario',  largura: 100, minLargura: 70,  padrao: true, tipo: 'numero' },
  { campo: 'total',             label: 'Total',           largura: 100, minLargura: 70,  padrao: true, tipo: 'numero' },
];

const CORES_PAGAMENTO = ['#2196F3', '#4CAF50', '#FF9800', '#9C27B0', '#F44336', '#009688', '#795548'];

@Component({
  selector: 'app-pre-venda',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pre-venda.component.html',
  styleUrl: './pre-venda.component.scss'
})
export class PreVendaComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_prevenda_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_prevenda_itens';
  private readonly apiUrl = environment.apiUrl;

  @ViewChild('inputProduto') inputProdutoRef!: ElementRef<HTMLInputElement>;
  private saindo = false;

  // ── Abas de atendimento ─────────────────────────────────────────
  atendimentos = signal<Atendimento[]>([]);
  abaAtivaId = signal(1);
  private nextAbaId = 1;

  // ── Pre-venda state (aba ativa) ─────────────────────────────────
  preVendaId = signal<number | null>(null);
  itens = signal<PreVendaItem[]>([]);
  itensSelecionadoIdx = signal<number | null>(null);

  // ── Client ──────────────────────────────────────────────────────
  clienteId = signal<number | null>(null);
  clienteNome = signal('');
  clienteBusca = signal('');
  clienteResultados = signal<ClienteLookup[]>([]);
  clienteDropdown = signal(false);
  private clienteTimer: any = null;

  // ── Collaborator ────────────────────────────────────────────────
  colaboradorId = signal<number | null>(null);
  colaboradorNome = signal('');
  colaboradorBusca = signal('');
  colaboradorResultados = signal<ColaboradorLookup[]>([]);
  colaboradorDropdown = signal(false);
  private colaboradorTimer: any = null;

  // ── Payment type ────────────────────────────────────────────────
  tipoPagamentoId = signal<number | null>(null);
  tiposPagamento = signal<TipoPagBtn[]>([]);

  // ── Product search ──────────────────────────────────────────────
  produtoBusca = signal('');
  produtoResultados = signal<ProdutoLookup[]>([]);
  produtoDropdown = signal(false);
  private produtoTimer: any = null;

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
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.carregarTiposPagamento();
    this.restaurarEstado();
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
    const aba: Atendimento = {
      id, label: `Atendimento ${id}`, preVendaId: null, itens: [],
      clienteId: null, clienteNome: '', colaboradorId: null, colaboradorNome: '', tipoPagamentoId: null
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
      next: r => this.tiposPagamento.set(r.data ?? []),
      error: () => this.modal.erro('Erro', 'Erro ao carregar tipos de pagamento.')
    });
  }

  corPagamento(idx: number): string {
    return CORES_PAGAMENTO[idx % CORES_PAGAMENTO.length];
  }

  selecionarPagamento(id: number) {
    this.tipoPagamentoId.set(this.tipoPagamentoId() === id ? null : id);
  }

  // ── Product search ──────────────────────────────────────────────
  onProdutoBuscaInput(valor: string) {
    this.produtoBusca.set(valor);
    if (this.produtoTimer) clearTimeout(this.produtoTimer);
    if (valor.trim().length < 2) {
      this.produtoResultados.set([]);
      this.produtoDropdown.set(false);
      return;
    }
    this.produtoTimer = setTimeout(() => this.buscarProdutos(valor), 300);
  }

  private buscarProdutos(termo: string) {
    this.http.get<any>(`${this.apiUrl}/produtos/buscar`, {
      params: { termo, filialId: '1' }
    }).subscribe({
      next: r => {
        this.produtoResultados.set(r.data ?? []);
        this.produtoDropdown.set((r.data ?? []).length > 0);
      },
      error: () => this.modal.erro('Erro', 'Erro ao buscar produtos.')
    });
  }

  selecionarProduto(p: ProdutoLookup) {
    const novoItem: PreVendaItem = {
      produtoId: p.id,
      produtoCodigo: p.codigo,
      produtoNome: p.nome,
      fabricante: p.fabricante ?? '',
      precoVenda: p.valorVenda,
      quantidade: 1,
      percentualDesconto: 0,
      valorDesconto: 0,
      precoUnitario: p.valorVenda,
      total: p.valorVenda,
      estoqueAtual: p.estoqueAtual,
      unidade: p.unidade
    };
    this.itens.update(lista => [...lista, novoItem]);
    this.itensSelecionadoIdx.set(this.itens().length - 1);
    this.produtoBusca.set('');
    this.produtoResultados.set([]);
    this.produtoDropdown.set(false);
    setTimeout(() => this.inputProdutoRef?.nativeElement?.focus(), 50);
  }

  onProdutoKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      this.produtoDropdown.set(false);
    } else if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      // future: navigate dropdown
    } else if (e.key === 'Enter') {
      const resultados = this.produtoResultados();
      if (resultados.length === 1) {
        this.selecionarProduto(resultados[0]);
      }
    }
  }

  // ── Client search ──────────────────────────────────────────────
  onClienteBuscaInput(valor: string) {
    this.clienteBusca.set(valor);
    this.clienteId.set(null);
    this.clienteNome.set('');
    if (this.clienteTimer) clearTimeout(this.clienteTimer);
    if (valor.trim().length < 2) {
      this.clienteResultados.set([]);
      this.clienteDropdown.set(false);
      return;
    }
    this.clienteTimer = setTimeout(() => this.buscarClientes(valor), 300);
  }

  private buscarClientes(termo: string) {
    this.http.get<any>(`${this.apiUrl}/pessoas/pesquisar`, {
      params: { termo }
    }).subscribe({
      next: r => {
        this.clienteResultados.set(r.data ?? []);
        this.clienteDropdown.set((r.data ?? []).length > 0);
      },
      error: () => {}
    });
  }

  selecionarCliente(c: ClienteLookup) {
    this.clienteId.set(c.id);
    this.clienteNome.set(c.nome);
    this.clienteBusca.set(c.nome);
    this.clienteResultados.set([]);
    this.clienteDropdown.set(false);
  }

  // ── Collaborator search ─────────────────────────────────────────
  onColaboradorBuscaInput(valor: string) {
    this.colaboradorBusca.set(valor);
    this.colaboradorId.set(null);
    this.colaboradorNome.set('');
    if (this.colaboradorTimer) clearTimeout(this.colaboradorTimer);
    if (valor.trim().length < 2) {
      this.colaboradorResultados.set([]);
      this.colaboradorDropdown.set(false);
      return;
    }
    this.colaboradorTimer = setTimeout(() => this.buscarColaboradores(valor), 300);
  }

  private buscarColaboradores(termo: string) {
    this.http.get<any>(`${this.apiUrl}/colaboradores`, {
      params: { busca: termo }
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

  onCellEdit(idx: number, campo: string, event: Event) {
    const valor = (event.target as HTMLInputElement).value;
    const num = this.parseNumero(valor);
    if (isNaN(num) || num < 0) return;

    this.itens.update(lista => {
      const arr = [...lista];
      const item = { ...arr[idx] };
      (item as any)[campo] = num;
      this.recalcularItem(item);
      arr[idx] = item;
      return arr;
    });
  }

  private parseNumero(valor: string): number {
    const limpo = valor.replace(/\./g, '').replace(',', '.');
    return parseFloat(limpo);
  }

  private recalcularItem(item: PreVendaItem) {
    item.valorDesconto = item.precoVenda * item.quantidade * (item.percentualDesconto / 100);
    item.precoUnitario = item.precoVenda * (1 - item.percentualDesconto / 100);
    item.total = item.precoUnitario * item.quantidade;
  }

  getEditValue(item: PreVendaItem, campo: string): string {
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
    this.tipoPagamentoId.set(null);
    this.produtoBusca.set('');
    this.salvarEstado();
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

    this.salvando.set(true);

    const body = {
      clienteId: this.clienteId(),
      colaboradorId: this.colaboradorId(),
      tipoPagamentoId: this.tipoPagamentoId(),
      itens: this.itens().map(i => ({
        produtoId: i.produtoId,
        quantidade: i.quantidade,
        precoVenda: i.precoVenda,
        percentualDesconto: i.percentualDesconto,
      }))
    };

    const salvar$ = this.preVendaId()
      ? this.http.put<any>(`${this.apiUrl}/prevendas/${this.preVendaId()}`, body)
      : this.http.post<any>(`${this.apiUrl}/prevendas`, body);

    salvar$.subscribe({
      next: (r: any) => {
        const id = this.preVendaId() ?? r.data?.id;
        if (!id) {
          this.salvando.set(false);
          this.modal.erro('Erro', 'Erro ao salvar pre-venda.');
          return;
        }
        this.http.post<any>(`${this.apiUrl}/prevendas/${id}/finalizar`, {}).subscribe({
          next: () => {
            this.salvando.set(false);
            this.modal.aviso('Sucesso', 'Pre-venda finalizada com sucesso!');
            this.resetTudo();
          },
          error: () => {
            this.salvando.set(false);
            this.modal.erro('Erro', 'Erro ao finalizar pre-venda.');
          }
        });
      },
      error: () => {
        this.salvando.set(false);
        this.modal.erro('Erro', 'Erro ao salvar pre-venda.');
      }
    });
  }

  pendentes() {
    // Placeholder for future implementation
    this.modal.aviso('Em Desenvolvimento', 'A funcionalidade de pre-vendas pendentes sera implementada em breve.');
  }

  opcoes() {
    // Placeholder for future implementation
    this.modal.aviso('Em Desenvolvimento', 'As opcoes adicionais serao implementadas em breve.');
  }

  async sairDaTela() {
    const abas = this.atendimentos();
    const abasComItens = abas.filter(a => a.itens.length > 0 || a.clienteId);
    if (abasComItens.length > 0) {
      const msg = abas.length > 1
        ? `Você possui ${abas.length} atendimento(s) aberto(s). Ao sair, todos serão descartados. Deseja continuar?`
        : 'Ao sair, o atendimento atual será descartado. Deseja continuar?';
      const resultado = await this.modal.confirmar('Sair da Pré-Venda', msg, 'Sim, sair', 'Não, continuar');
      if (!resultado.confirmado) return;
    }
    this.saindo = true;
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  // ── Dropdown close on outside click ─────────────────────────────
  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent) {
    const target = e.target as HTMLElement;
    if (!target.closest('.pv-autocomplete')) {
      this.produtoDropdown.set(false);
      this.clienteDropdown.set(false);
      this.colaboradorDropdown.set(false);
    }
  }
}
