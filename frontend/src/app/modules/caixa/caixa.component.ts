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
    this.modalAbertura.set(true);
  }

  confirmarAberturaCaixa() {
    this.modalAbertura.set(false);
    this.caixaAbertoLoading.set(true);
    this.http.post<any>(`${this.apiUrl}/caixas/abrir`, { valorAbertura: 0 }).subscribe({
      next: r => {
        this.caixaAberto.set(r.data);
        this.caixaAbertoLoading.set(false);
        this.modal.sucesso('Caixa Aberto', 'O caixa foi aberto com sucesso.');
      },
      error: (err: any) => {
        this.caixaAbertoLoading.set(false);
        this.modal.erro('Erro', err?.error?.message || 'Erro ao abrir caixa.');
      }
    });
  }

  cancelarAbertura() {
    this.modalAbertura.set(false);
  }

  async fecharCaixa() {
    if (!this.caixaAberto()) return;
    const resultado = await this.modal.confirmar('Fechar Caixa', 'Deseja fechar o caixa? Não será possível realizar novas vendas até reabrir.', 'Fechar', 'Cancelar');
    if (!resultado.confirmado) return;
    this.http.post<any>(`${this.apiUrl}/caixas/${this.caixaAberto()!.id}/fechar`, {}).subscribe({
      next: () => {
        this.caixaAberto.set(null);
        this.modal.sucesso('Caixa Fechado', 'O caixa foi fechado com sucesso.');
      },
      error: () => this.modal.erro('Erro', 'Erro ao fechar caixa.')
    });
  }

  // ── Sidebar ────────────────────────────────────────────────────
  selecionarPainel(id: PainelAtivo) {
    this.painelAtivo.set(id);
    if (id === 'pendentes') this.carregarPendentes();
    if (id === 'vendas') this.carregarVendasRealizadas();
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

  // ── Operações (placeholder) ────────────────────────────────────
  sangria() { this.modal.aviso('Em Desenvolvimento', 'A funcionalidade de sangria será implementada em breve.'); }
  suprimento() { this.modal.aviso('Em Desenvolvimento', 'A funcionalidade de suprimento será implementada em breve.'); }
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
