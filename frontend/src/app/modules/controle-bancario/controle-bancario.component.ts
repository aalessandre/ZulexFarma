import { Component, signal, computed, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface ContaOption {
  id: number;
  descricao: string;
  tipoConta: number;
  ativo: boolean;
}

interface SaldoInfo {
  contaBancariaId: number;
  contaBancariaNome: string;
  tipoConta: number;
  ehCofre: boolean;
  saldoInicial: number;
  dataSaldoInicial?: string;
  saldoAtual: number;
  totalEntradasPeriodo: number;
  totalSaidasPeriodo: number;
  saldoPeriodo: number;
}

interface Movimento {
  id: number;
  dataMovimento: string;
  tipo: number;
  tipoDescricao: string;
  valor: number;
  descricao: string;
  caixaId?: number;
  caixaCodigo?: string;
  caixaMovimentoId?: number;
  caixaMovimentoCodigo?: string;
  caixaMovimentoTipo?: number;
  usuarioNome?: string;
  manual: boolean;
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'dataMovimento',      label: 'Data/Hora',   largura: 140, minLargura: 100, padrao: true },
  { campo: 'tipoDescricao',      label: 'Tipo',        largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'descricao',          label: 'Descrição',   largura: 320, minLargura: 160, padrao: true },
  { campo: 'caixaCodigo',        label: 'Caixa',       largura: 100, minLargura: 70,  padrao: false },
  { campo: 'caixaMovimentoCodigo', label: 'Mov. Origem', largura: 140, minLargura: 90,  padrao: false },
  { campo: 'usuarioNome',        label: 'Usuário',     largura: 160, minLargura: 100, padrao: true },
  { campo: 'entrada',            label: 'Entrada',     largura: 110, minLargura: 80,  padrao: true },
  { campo: 'saida',              label: 'Saída',       largura: 110, minLargura: 80,  padrao: true },
];

@Component({
  selector: 'app-controle-bancario',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './controle-bancario.component.html',
  styleUrl: './controle-bancario.component.scss'
})
export class ControleBancarioComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/contasbancarias`;
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_controle_bancario';

  contas = signal<ContaOption[]>([]);
  contaSelecionadaId = signal<number | null>(null);
  saldo = signal<SaldoInfo | null>(null);
  movimentos = signal<Movimento[]>([]);
  carregando = signal(false);

  // Filtros
  dataInicio = signal<string>(this.hoje(-30));
  dataFim = signal<string>(this.hoje(0));

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal('dataMovimento');
  sortDirecao = signal<'asc' | 'desc'>('desc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  // Modal ajuste manual
  modalAjuste = signal(false);
  ajusteTipo = signal(1);
  ajusteValor = signal('');
  ajusteDescricao = signal('');
  ajusteObservacao = signal('');

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.carregarContas();
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  private carregarContas() {
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        const lista: ContaOption[] = (r.data ?? []).filter((c: any) => c.ativo);
        this.contas.set(lista);

        // Detecta se veio pela rota /cofre — seleciona a conta cofre da filial
        const ehRotaCofre = window.location.href.includes('/cofre');
        if (ehRotaCofre) {
          this.selecionarContaCofre();
        } else if (lista.length > 0) {
          this.selecionarConta(lista[0].id);
        }
      }
    });
  }

  private selecionarContaCofre() {
    // Pega a filial do usuário logado e busca a ContaCofreId
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({
      next: r => {
        const filial = (r.data ?? []).find((f: any) => f.id === filialId);
        if (filial?.contaCofreId) {
          this.selecionarConta(filial.contaCofreId);
        } else if (this.contas().length > 0) {
          this.modal.aviso('Cofre', 'Nenhuma conta cofre configurada para esta filial.');
          this.selecionarConta(this.contas()[0].id);
        }
      }
    });
  }

  selecionarConta(id: number) {
    this.contaSelecionadaId.set(id);
    this.carregarSaldo();
    this.carregarExtrato();
  }

  carregarSaldo() {
    const id = this.contaSelecionadaId();
    if (!id) return;
    const params: string[] = [];
    if (this.dataInicio()) params.push(`dataInicio=${this.dataInicio()}`);
    if (this.dataFim()) params.push(`dataFim=${this.dataFim()}`);
    const qs = params.length ? '?' + params.join('&') : '';
    this.http.get<any>(`${this.apiUrl}/${id}/saldo${qs}`).subscribe({
      next: r => this.saldo.set(r.data)
    });
  }

  carregarExtrato() {
    const id = this.contaSelecionadaId();
    if (!id) return;
    this.carregando.set(true);
    const params: string[] = [];
    if (this.dataInicio()) params.push(`dataInicio=${this.dataInicio()}`);
    if (this.dataFim()) params.push(`dataFim=${this.dataFim()}`);
    const qs = params.length ? '?' + params.join('&') : '';
    this.http.get<any>(`${this.apiUrl}/${id}/extrato${qs}`).subscribe({
      next: r => {
        this.movimentos.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  aplicarFiltro() {
    this.carregarSaldo();
    this.carregarExtrato();
  }

  // ── Ajuste manual ──────────────────────────────────────────────
  abrirAjuste() {
    if (!this.contaSelecionadaId()) {
      this.modal.aviso('Seleção', 'Selecione uma conta bancária primeiro.');
      return;
    }
    this.ajusteTipo.set(1);
    this.ajusteValor.set('');
    this.ajusteDescricao.set('');
    this.ajusteObservacao.set('');
    this.modalAjuste.set(true);
  }

  cancelarAjuste() { this.modalAjuste.set(false); }

  confirmarAjuste() {
    const valor = parseFloat(this.ajusteValor().replace(',', '.')) || 0;
    if (valor <= 0) { this.modal.aviso('Valor inválido', 'Informe um valor maior que zero.'); return; }
    if (!this.ajusteDescricao().trim()) { this.modal.aviso('Descrição', 'Informe a descrição/motivo.'); return; }

    const id = this.contaSelecionadaId();
    this.http.post<any>(`${this.apiUrl}/${id}/ajuste-manual`, {
      tipo: this.ajusteTipo(),
      valor,
      descricao: this.ajusteDescricao(),
      observacao: this.ajusteObservacao() || null
    }).subscribe({
      next: () => {
        this.modalAjuste.set(false);
        this.carregarSaldo();
        this.carregarExtrato();
        this.modal.sucesso('Ajuste registrado', 'Movimento manual registrado com sucesso.');
      },
      error: (err: any) => this.modal.erro('Erro', err?.error?.message || 'Erro ao registrar ajuste.')
    });
  }

  // ── Grid helpers ────────────────────────────────────────────────
  getCellValue(m: Movimento, campo: string): string {
    if (campo === 'dataMovimento') return new Date(m.dataMovimento).toLocaleString('pt-BR');
    if (campo === 'entrada') return m.tipo === 1 ? m.valor.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    if (campo === 'saida') return m.tipo === 2 ? m.valor.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    if (campo === 'caixaCodigo') return m.caixaCodigo ?? '—';
    if (campo === 'caixaMovimentoCodigo') return m.caixaMovimentoCodigo ?? '—';
    if (campo === 'usuarioNome') return m.usuarioNome ?? '—';
    const v = (m as any)[campo];
    return v?.toString() ?? '';
  }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string {
    return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅';
  }

  movimentosOrdenados = computed(() => {
    const lista = [...this.movimentos()];
    const col = this.sortColuna();
    const dir = this.sortDirecao();
    return lista.sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number)
                 : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private carregarColunas(): ColunaEstado[] {
    let salvo: { campo: string; visivel: boolean; largura: number }[] = [];
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) salvo = JSON.parse(json);
    } catch {}
    return COLUNAS.map(def => {
      const s = salvo.find(x => x.campo === def.campo);
      return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
    });
  }

  private salvarColunasStorage() {
    const estado = this.colunas().map(c => ({ campo: c.campo, visivel: c.visivel, largura: Math.round(c.largura) }));
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(estado));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(COLUNAS.map(d => ({ ...d, visivel: d.padrao })));
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
    const def = COLUNAS.find(c => c.campo === this.resizeState!.campo);
    const min = def?.minLargura ?? 50;
    const novaLargura = Math.max(min, this.resizeState.startWidth + delta);
    this.colunas.update(cols => cols.map(c => c.campo === this.resizeState!.campo ? { ...c, largura: novaLargura } : c));
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

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }

  ehCofre(contaId: number): boolean {
    return this.saldo()?.contaBancariaId === contaId && this.saldo()?.ehCofre === true;
  }
}
