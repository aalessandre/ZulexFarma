import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface ContaReceber {
  id: number;
  descricao: string;
  clienteNome: string;
  tipoPagamentoNome: string;
  modalidade: string;
  valor: number;
  valorLiquido: number;
  tarifa: number;
  numParcela: number;
  totalParcelas: number;
  dataEmissao: string;
  dataVencimento: string;
  dataRecebimento: string | null;
  valorRecebido: number | null;
  status: string;
  nsu: string;
  bandeiraNome: string;
  adquirenteNome: string;
}

interface ColunaDef {
  campo: string;
  label: string;
  largura: number;
  minLargura: number;
  padrao: boolean;
}

interface ColunaEstado extends ColunaDef {
  visivel: boolean;
}

const CONTAS_RECEBER_COLUNAS: ColunaDef[] = [
  { campo: 'id',                label: 'Codigo',        largura: 70,  minLargura: 50,  padrao: true },
  { campo: 'descricao',         label: 'Descricao',     largura: 200, minLargura: 100, padrao: true },
  { campo: 'clienteNome',       label: 'Cliente',        largura: 180, minLargura: 100, padrao: true },
  { campo: 'tipoPagamentoNome', label: 'Tipo Pagamento', largura: 120, minLargura: 80,  padrao: true },
  { campo: 'modalidade',        label: 'Modalidade',     largura: 110, minLargura: 80,  padrao: false },
  { campo: 'valor',             label: 'Valor',          largura: 100, minLargura: 70,  padrao: true },
  { campo: 'valorLiquido',      label: 'Valor Liquido',  largura: 110, minLargura: 70,  padrao: true },
  { campo: 'tarifa',  label: 'Tarifa %',       largura: 80,  minLargura: 60,  padrao: false },
  { campo: 'numParcela',        label: 'Parcela',        largura: 70,  minLargura: 50,  padrao: true },
  { campo: 'dataEmissao',       label: 'Data Emissao',   largura: 110, minLargura: 80,  padrao: true },
  { campo: 'dataVencimento',    label: 'Vencimento',     largura: 110, minLargura: 80,  padrao: true },
  { campo: 'dataRecebimento',   label: 'Recebimento',    largura: 110, minLargura: 80,  padrao: true },
  { campo: 'valorRecebido',     label: 'Valor Recebido', largura: 110, minLargura: 70,  padrao: true },
  { campo: 'status',            label: 'Status',         largura: 100, minLargura: 70,  padrao: true },
  { campo: 'nsu',               label: 'NSU',            largura: 100, minLargura: 60,  padrao: false },
  { campo: 'bandeiraNome',      label: 'Bandeira',       largura: 100, minLargura: 70,  padrao: false },
  { campo: 'adquirenteNome',    label: 'Adquirente',     largura: 120, minLargura: 80,  padrao: false },
];

@Component({
  selector: 'app-contas-receber',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contas-receber.component.html',
  styleUrl: './contas-receber.component.scss'
})
export class ContasReceberComponent implements OnInit, OnDestroy {
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_contas_receber';

  contas = signal<ContaReceber[]>([]);
  contaSelecionada = signal<ContaReceber | null>(null);
  carregando = signal(false);
  processando = signal(false);

  // Filtros
  busca = signal('');
  filtroStatus = signal('');
  filtroTipoPagamento = signal('');
  filtroDataInicio = signal('');
  filtroDataFim = signal('');

  // Sort
  sortColuna = signal<string>('dataVencimento');
  sortDirecao = signal<'asc' | 'desc'>('desc');

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  // Modal Receber
  modalReceber = signal(false);
  modalValorRecebido = signal('');
  modalContaBancaria = signal('');

  private apiUrl = `${environment.apiUrl}/contasreceber`;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() { this.carregar(); }
  ngOnDestroy() {}

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  // ── Data ───────────────────────────────────────────────────────────
  carregar() {
    this.carregando.set(true);
    const params: string[] = [];
    const usuario = this.auth.usuarioLogado();
    const filialId = usuario?.filialId;
    if (filialId) params.push(`filialId=${filialId}`);
    if (this.filtroStatus()) params.push(`status=${this.filtroStatus()}`);
    if (this.filtroTipoPagamento()) params.push(`tipoPagamento=${this.filtroTipoPagamento()}`);
    if (this.filtroDataInicio()) params.push(`dataInicio=${this.filtroDataInicio()}`);
    if (this.filtroDataFim()) params.push(`dataFim=${this.filtroDataFim()}`);
    if (this.busca()) params.push(`busca=${encodeURIComponent(this.busca())}`);

    const url = params.length ? `${this.apiUrl}?${params.join('&')}` : this.apiUrl;

    this.http.get<any>(url).subscribe({
      next: r => {
        this.contas.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.modal.erro('Erro', 'Erro ao carregar contas a receber.');
      }
    });
  }

  contasFiltradas = computed(() => {
    const col = this.sortColuna();
    const dir = this.sortDirecao();
    const lista = this.contas();

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

  selecionar(c: ContaReceber) { this.contaSelecionada.set(c); }

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

  getCellValue(conta: ContaReceber, campo: string): string {
    const v = (conta as any)[campo];
    if (v === null || v === undefined) return '';

    if (campo === 'valor' || campo === 'valorLiquido' || campo === 'valorRecebido') {
      return this.formatarMoeda(v);
    }
    if (campo === 'tarifa') {
      return typeof v === 'number' ? v.toFixed(2) + '%' : '';
    }
    if (campo === 'numParcela') {
      return `${conta.numParcela}/${conta.totalParcelas}`;
    }
    if (campo === 'dataEmissao' || campo === 'dataVencimento' || campo === 'dataRecebimento') {
      return this.formatarData(v);
    }
    return String(v);
  }

  isValorColumn(campo: string): boolean {
    return campo === 'valor' || campo === 'valorLiquido' || campo === 'valorRecebido' || campo === 'tarifa';
  }

  statusClass(status: string, vencimento?: string): string {
    if (status === 'recebida') return 'badge-recebida';
    if (status === 'cancelada') return 'badge-cancelada';
    if (status === 'aberta' && vencimento && new Date(vencimento) < new Date()) return 'badge-vencida';
    if (status === 'aberta') return 'badge-aberta';
    if (status === 'vencida') return 'badge-vencida';
    return '';
  }

  statusLabel(status: string, vencimento?: string): string {
    if (status === 'aberta' && vencimento && new Date(vencimento) < new Date()) return 'Vencida';
    const map: Record<string, string> = {
      'aberta': 'Aberta',
      'recebida': 'Recebida',
      'cancelada': 'Cancelada',
      'vencida': 'Vencida'
    };
    return map[status] ?? status;
  }

  // ── Columns ────────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return CONTAS_RECEBER_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return CONTAS_RECEBER_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(CONTAS_RECEBER_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasStorage();
  }

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
    const def = CONTAS_RECEBER_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── Actions ────────────────────────────────────────────────────────
  abrirModalReceber() {
    const c = this.contaSelecionada();
    if (!c || c.status !== 'aberta') return;
    this.modalValorRecebido.set(this.formatarMoeda(c.valor));
    this.modalContaBancaria.set('');
    this.modalReceber.set(true);
  }

  fecharModalReceber() {
    this.modalReceber.set(false);
  }

  confirmarRecebimento() {
    const c = this.contaSelecionada();
    if (!c) return;

    const valorStr = this.modalValorRecebido().replace(/[^\d,.-]/g, '').replace(',', '.');
    const valor = parseFloat(valorStr);
    if (isNaN(valor) || valor <= 0) {
      this.modal.erro('Erro', 'Informe um valor recebido valido.');
      return;
    }

    this.processando.set(true);
    this.http.post<any>(`${this.apiUrl}/${c.id}/receber`, {
      valorRecebido: valor,
      contaBancaria: this.modalContaBancaria()
    }).subscribe({
      next: () => {
        this.processando.set(false);
        this.modalReceber.set(false);
        this.contaSelecionada.set(null);
        this.carregar();
      },
      error: () => {
        this.processando.set(false);
        this.modal.erro('Erro', 'Erro ao registrar recebimento.');
      }
    });
  }

  async cancelarConta() {
    const c = this.contaSelecionada();
    if (!c || c.status !== 'aberta') return;

    const resultado = await this.modal.confirmar(
      'Confirmar Cancelamento',
      `Deseja cancelar a conta "${c.descricao}" no valor de ${this.formatarMoeda(c.valor)}?`,
      'Sim, cancelar',
      'Nao, manter'
    );
    if (!resultado.confirmado) return;

    this.processando.set(true);
    this.http.post<any>(`${this.apiUrl}/${c.id}/cancelar`, {}).subscribe({
      next: () => {
        this.processando.set(false);
        this.contaSelecionada.set(null);
        this.carregar();
      },
      error: () => {
        this.processando.set(false);
        this.modal.erro('Erro', 'Erro ao cancelar conta.');
      }
    });
  }

  // ── Utils ──────────────────────────────────────────────────────────
  private formatarMoeda(v: any): string {
    if (v === null || v === undefined) return '';
    const num = typeof v === 'number' ? v : parseFloat(v);
    if (isNaN(num)) return '';
    return num.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  private formatarData(v: string): string {
    if (!v) return '';
    try {
      const d = new Date(v);
      return d.toLocaleDateString('pt-BR');
    } catch { return v; }
  }

  podeCancelar(): boolean {
    const c = this.contaSelecionada();
    return !!c && c.status === 'aberta';
  }

  podeReceber(): boolean {
    const c = this.contaSelecionada();
    return !!c && c.status === 'aberta';
  }
}
