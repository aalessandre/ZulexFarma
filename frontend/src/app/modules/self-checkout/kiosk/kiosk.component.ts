import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { FormaPagamentoKiosk, KioskApiService, ProdutoKiosk, StatusVendaKioskResult, TerminalKiosk } from '../services/kiosk-api.service';

type KioskEstado =
  | 'selecionarTerminal'
  | 'atrator' | 'carrinho' | 'busca' | 'naoLocalizado'
  | 'aguardandoAtendente'
  | 'pagamento' | 'aguardandoConfirmacao' | 'cupom' | 'erroEmissao';

interface ItemCarrinho {
  produto: ProdutoKiosk;
  quantidade: number;
}

@Component({
  selector: 'app-kiosk',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './kiosk.component.html',
  styleUrl: './kiosk.component.scss'
})
export class KioskComponent implements OnInit, OnDestroy {
  @ViewChild('inputEan') inputEan?: ElementRef<HTMLInputElement>;

  estado = signal<KioskEstado>('selecionarTerminal');
  terminais = signal<TerminalKiosk[]>([]);
  carregandoTerminais = signal(false);
  erroTerminais = signal('');
  carrinho = signal<ItemCarrinho[]>([]);
  inputBuffer = signal('');
  termoBusca = signal('');
  resultadosBusca = signal<ProdutoKiosk[]>([]);
  buscando = signal(false);
  ultimoEanNaoLocalizado = signal('');
  erroApi = signal('');

  total = computed(() =>
    this.carrinho().reduce((s, i) => s + i.produto.precoFinal * i.quantidade, 0));

  qtdItens = computed(() =>
    this.carrinho().reduce((s, i) => s + i.quantidade, 0));

  private filialId = 1;
  private terminalId: number | null = null;

  // ── Ciclo de venda ────────────────────────────────────────────
  vendaId = signal<number | null>(null);
  finalizandoVenda = signal(false);
  ultimoStatus = signal<StatusVendaKioskResult | null>(null);
  private pollingTimer: any = null;

  // Layout do teclado virtual touch (busca por nome)
  readonly tecladoLinhas: string[][] = [
    ['Q','W','E','R','T','Y','U','I','O','P'],
    ['A','S','D','F','G','H','J','K','L'],
    ['Z','X','C','V','B','N','M']
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private auth: AuthService,
    private api: KioskApiService
  ) {}

  ngOnInit(): void {
    this.filialId = parseInt(this.auth.usuarioLogado()?.filialId || '1', 10);

    document.documentElement.classList.add('kiosk-mode');
    document.body.classList.add('kiosk-mode');

    // Resolve terminalId em ordem: query param (?terminalId=) > localStorage > tela de seleção.
    const tParam = this.route.snapshot.queryParamMap.get('terminalId');
    if (tParam) {
      const id = parseInt(tParam, 10);
      if (!isNaN(id)) {
        this.adotarTerminal(id, /*persistir*/ true);
        return;
      }
    }
    const armazenado = this.lerTerminalArmazenado();
    if (armazenado) {
      this.adotarTerminal(armazenado, /*persistir*/ false);
      return;
    }
    this.estado.set('selecionarTerminal');
    this.carregarTerminais();
  }

  private get storageKey() { return `kiosk:terminalId:filial-${this.filialId}`; }

  private lerTerminalArmazenado(): number | null {
    try {
      const v = localStorage.getItem(this.storageKey);
      const n = v ? parseInt(v, 10) : NaN;
      return isNaN(n) ? null : n;
    } catch { return null; }
  }

  private gravarTerminal(id: number) {
    try { localStorage.setItem(this.storageKey, String(id)); } catch { /* best effort */ }
  }

  private adotarTerminal(id: number, persistir: boolean) {
    this.terminalId = id;
    if (persistir) this.gravarTerminal(id);
    this.estado.set('atrator');
  }

  async carregarTerminais() {
    this.carregandoTerminais.set(true);
    this.erroTerminais.set('');
    try {
      const lista = await this.api.listarTerminais(this.filialId);
      this.terminais.set(lista);
      if (lista.length === 0) {
        this.erroTerminais.set('Nenhum terminal cadastrado nesta filial. Configure em Configurações → Self-Checkout.');
      }
    } catch (e: any) {
      this.erroTerminais.set(e?.error?.message ?? 'Falha ao listar terminais.');
    } finally {
      this.carregandoTerminais.set(false);
    }
  }

  selecionarTerminal(t: TerminalKiosk) {
    this.adotarTerminal(t.id, /*persistir*/ true);
  }

  trocarTerminal() {
    try { localStorage.removeItem(this.storageKey); } catch { /* ignore */ }
    this.terminalId = null;
    this.estado.set('selecionarTerminal');
    this.carregarTerminais();
  }

  ngOnDestroy(): void {
    this.pararPolling();
    document.documentElement.classList.remove('kiosk-mode');
    document.body.classList.remove('kiosk-mode');
  }

  // ── Navegação de estado ────────────────────────────────────────
  iniciarSessao() {
    this.estado.set('carrinho');
    this.carrinho.set([]);
    this.inputBuffer.set('');
    this.erroApi.set('');
    setTimeout(() => this.focarLeitor(), 0);
  }

  voltarAoAtrator() {
    this.estado.set('atrator');
    this.carrinho.set([]);
    this.inputBuffer.set('');
    this.termoBusca.set('');
    this.resultadosBusca.set([]);
    this.ultimoEanNaoLocalizado.set('');
    this.erroApi.set('');
  }

  abrirBusca() {
    this.estado.set('busca');
    this.termoBusca.set('');
    this.resultadosBusca.set([]);
  }

  fecharBusca() {
    this.estado.set('carrinho');
    setTimeout(() => this.focarLeitor(), 0);
  }

  fecharNaoLocalizado() {
    this.estado.set('carrinho');
    setTimeout(() => this.focarLeitor(), 0);
  }

  chamarAtendente() {
    this.estado.set('aguardandoAtendente');
    // Fatia 6 grava o chamado no backend; aqui só mostra a tela.
  }

  fecharChamado() {
    this.estado.set(this.carrinho().length > 0 ? 'carrinho' : 'atrator');
  }

  // ── Leitor / EAN ──────────────────────────────────────────────
  onInputChange(valor: string) {
    this.inputBuffer.set(valor);
  }

  async onInputEnter() {
    const valor = this.inputBuffer().trim();
    if (!valor) return;
    this.inputBuffer.set('');
    await this.adicionarPorEan(valor);
  }

  private async adicionarPorEan(ean: string) {
    try {
      const produto = await this.api.buscarPorEan(this.filialId, ean);
      if (!produto) {
        this.ultimoEanNaoLocalizado.set(ean);
        this.estado.set('naoLocalizado');
        return;
      }
      this.adicionarAoCarrinho(produto);
    } catch (e: any) {
      this.erroApi.set('Falha ao consultar o ERP. Chame um atendente.');
      this.estado.set('aguardandoAtendente');
    } finally {
      setTimeout(() => this.focarLeitor(), 0);
    }
  }

  // ── Carrinho ──────────────────────────────────────────────────
  adicionarAoCarrinho(produto: ProdutoKiosk) {
    this.carrinho.update(list => {
      const idx = list.findIndex(i => i.produto.codigoExterno === produto.codigoExterno);
      if (idx >= 0) {
        return list.map((i, k) => k === idx ? { ...i, quantidade: i.quantidade + 1 } : i);
      }
      return [...list, { produto, quantidade: 1 }];
    });
  }

  alterarQuantidade(codigoExterno: string, delta: number) {
    this.carrinho.update(list => list
      .map(i => i.produto.codigoExterno === codigoExterno
        ? { ...i, quantidade: Math.max(0, i.quantidade + delta) }
        : i)
      .filter(i => i.quantidade > 0));
  }

  removerItem(codigoExterno: string) {
    this.carrinho.update(list => list.filter(i => i.produto.codigoExterno !== codigoExterno));
  }

  async finalizarCompra() {
    if (this.carrinho().length === 0 || this.finalizandoVenda()) return;
    if (!this.terminalId) {
      this.trocarTerminal();
      return;
    }

    this.finalizandoVenda.set(true);
    try {
      const itens = this.carrinho().map(i => ({
        codigoExterno: i.produto.codigoExterno,
        quantidade: i.quantidade
      }));
      const resultado = await this.api.iniciarVenda(this.filialId, this.terminalId, itens);
      this.vendaId.set(resultado.vendaId);
      this.estado.set('pagamento');
    } catch (e: any) {
      this.erroApi.set(e?.error?.message ?? 'Falha ao iniciar venda. Chame um atendente.');
      this.estado.set('aguardandoAtendente');
    } finally {
      this.finalizandoVenda.set(false);
    }
  }

  // ── Pagamento ────────────────────────────────────────────────
  async escolherPagamento(forma: FormaPagamentoKiosk) {
    const id = this.vendaId();
    if (!id) return;
    try {
      await this.api.registrarPagamento(id, forma);
      this.estado.set('aguardandoConfirmacao');
      this.iniciarPolling();
    } catch (e: any) {
      this.erroApi.set(e?.error?.message ?? 'Falha ao registrar pagamento.');
      this.estado.set('aguardandoAtendente');
    }
  }

  async cancelarVenda() {
    const id = this.vendaId();
    if (!id) {
      this.voltarAoAtrator();
      return;
    }
    try { await this.api.cancelarVenda(id, 'Cancelada pelo cliente no kiosk'); }
    catch { /* ignora; volta ao atrator de qualquer forma */ }
    this.pararPolling();
    this.vendaId.set(null);
    this.voltarAoAtrator();
  }

  // ── Polling de status ────────────────────────────────────────
  private iniciarPolling() {
    this.pararPolling();
    this.pollingTimer = setInterval(() => this.consultarStatus(), 2000);
    // Consulta imediata
    this.consultarStatus();
  }

  private pararPolling() {
    if (this.pollingTimer) { clearInterval(this.pollingTimer); this.pollingTimer = null; }
  }

  private async consultarStatus() {
    const id = this.vendaId();
    if (!id) { this.pararPolling(); return; }
    try {
      const st = await this.api.statusVenda(id);
      if (!st) return;
      this.ultimoStatus.set(st);

      switch (st.status) {
        case 'NfceAutorizada':
          this.pararPolling();
          this.estado.set('cupom');
          break;
        case 'Cancelada':
          this.pararPolling();
          this.erroApi.set(st.mensagem ?? 'Venda cancelada pelo atendente.');
          this.estado.set('aguardandoAtendente');
          break;
        case 'Erro':
          this.pararPolling();
          this.erroApi.set(st.mensagem ?? 'Falha na emissão da NFC-e.');
          this.estado.set('erroEmissao');
          break;
        // 'AguardandoFormaPagamento' / 'AguardandoAtendente': segue polling
      }
    } catch { /* mantém polling */ }
  }

  finalizarCupom() {
    this.pararPolling();
    this.vendaId.set(null);
    this.ultimoStatus.set(null);
    this.voltarAoAtrator();
  }

  // ── Busca por nome ────────────────────────────────────────────
  digitarTeclado(letra: string) {
    this.termoBusca.update(t => (t + letra).slice(0, 30));
    this.executarBusca();
  }

  apagarUltimo() {
    this.termoBusca.update(t => t.slice(0, -1));
    if (this.termoBusca().length === 0) this.resultadosBusca.set([]);
    else this.executarBusca();
  }

  espaco() {
    this.termoBusca.update(t => t + ' ');
    this.executarBusca();
  }

  private buscaTimer: any = null;
  private executarBusca() {
    if (this.buscaTimer) clearTimeout(this.buscaTimer);
    const termo = this.termoBusca().trim();
    if (termo.length < 2) { this.resultadosBusca.set([]); return; }
    this.buscando.set(true);
    this.buscaTimer = setTimeout(async () => {
      try {
        const res = await this.api.buscarPorNome(this.filialId, termo, 20);
        this.resultadosBusca.set(res);
      } catch {
        this.resultadosBusca.set([]);
      } finally {
        this.buscando.set(false);
      }
    }, 300);
  }

  selecionarResultado(produto: ProdutoKiosk) {
    this.adicionarAoCarrinho(produto);
    this.fecharBusca();
  }

  // ── Foco persistente ──────────────────────────────────────────
  focarLeitor() { this.inputEan?.nativeElement.focus(); }

  @HostListener('document:click', ['$event'])
  onDocClick(ev: MouseEvent) {
    if (this.estado() !== 'carrinho') return;
    const alvo = ev.target as HTMLElement;
    if (alvo?.closest('button, input, [data-no-refocus]')) return;
    setTimeout(() => this.focarLeitor(), 0);
  }

  // ── Saída do modo kiosk (manutenção) ──────────────────────────
  // Combinação secreta: 5 toques no canto inferior direito + senha (Fatia 4 deixa simples).
  cantoToqueCount = 0;
  private cantoTimer: any = null;
  onCantoToque() {
    this.cantoToqueCount++;
    if (this.cantoTimer) clearTimeout(this.cantoTimer);
    this.cantoTimer = setTimeout(() => this.cantoToqueCount = 0, 3000);
    if (this.cantoToqueCount >= 5) {
      this.cantoToqueCount = 0;
      this.router.navigate(['/erp']);
    }
  }
}
