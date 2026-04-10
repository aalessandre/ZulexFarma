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
  totalLiquido: number;
  totalItens: number;
  criadoEm: string;
  status: number;
}

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

  // ── Vendas realizadas ──────────────────────────────────────────
  vendasRealizadas = signal<VendaRealizada[]>([]);
  vendasLoading = signal(false);

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.carregarFiliais();
    this.verificarCaixaAberto();
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
  carregarVendasRealizadas() {
    if (!this.caixaAberto()) return;
    this.vendasLoading.set(true);
    this.http.get<any>(`${this.apiUrl}/vendas`, { params: { caixaId: this.caixaAberto()!.id.toString(), status: 'finalizada' } }).subscribe({
      next: r => { this.vendasRealizadas.set(r.data ?? []); this.vendasLoading.set(false); },
      error: () => this.vendasLoading.set(false)
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
  onPendResizeMove(e: MouseEvent) {
    if (!this.pendResizeState) return;
    const delta = e.clientX - this.pendResizeState.startX;
    const def = PEND_COLUNAS.find(c => c.campo === this.pendResizeState!.campo);
    const min = def?.minLargura ?? 50;
    const novaLargura = Math.max(min, this.pendResizeState.startW + delta);
    this.pendColunas.update(cols => cols.map(c => c.campo === this.pendResizeState!.campo ? { ...c, largura: novaLargura } : c));
  }

  @HostListener('document:mouseup')
  onPendResizeEnd() {
    if (this.pendResizeState) { this.salvarColunasPend(); this.pendResizeState = null; document.body.style.cursor = ''; document.body.style.userSelect = ''; }
  }

  pendDragStart(idx: number) { this.pendDragIdx = idx; }
  pendDragOver(e: DragEvent, idx: number) {
    e.preventDefault();
    if (this.pendDragIdx === null || this.pendDragIdx === idx) return;
    this.pendColunas.update(cols => { const arr = [...cols]; const [m] = arr.splice(this.pendDragIdx!, 1); arr.splice(idx, 0, m); this.pendDragIdx = idx; return arr; });
  }
  pendDrop() { this.pendDragIdx = null; this.salvarColunasPend(); }
}
