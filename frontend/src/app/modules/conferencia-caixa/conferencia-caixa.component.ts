import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface CaixaOption {
  id: number;
  codigo?: string;
  colaboradorNome: string;
  dataAbertura: string;
  dataFechamento?: string;
  status: number;
  modeloFechamento?: string;
}

interface MovimentoItem {
  id: number;
  codigo?: string;
  tipo: number;
  tipoDescricao: string;
  dataMovimento: string;
  valor: number;
  tipoPagamentoNome?: string;
  descricao: string;
  observacao?: string;
  statusConferencia: number;
  statusConferenciaDescricao: string;
  usuarioNome?: string;
}

interface FormaPagamento {
  tipoPagamentoId?: number;
  tipoPagamentoNome: string;
  modalidade?: number;
  valorDeclarado: number;
  valorSistema: number;
  diferenca: number;
  qtdeMovimentos: number;
  qtdeConferidos: number;
  movimentos: MovimentoItem[];
}

interface ConferenciaData {
  caixaId: number;
  codigo?: string;
  modeloFechamento?: string;
  dataAbertura: string;
  dataFechamento?: string;
  colaboradorNome: string;
  valorAbertura: number;
  status: number;
  formasPagamento: FormaPagamento[];
}

@Component({
  selector: 'app-conferencia-caixa',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './conferencia-caixa.component.html',
  styleUrl: './conferencia-caixa.component.scss'
})
export class ConferenciaCaixaComponent implements OnInit {
  private apiUrl = environment.apiUrl;

  etapa = signal<'selecao' | 'conferencia'>('selecao');
  caixas = signal<CaixaOption[]>([]);
  caixaSelecionadoId = signal<number | null>(null);
  conferencia = signal<ConferenciaData | null>(null);
  carregando = signal(false);
  salvando = signal(false);

  formaExpandidaId = signal<number | null>(null);
  selecionados = signal<Set<number>>(new Set());
  observacao = signal('');

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private modal: ModalService
  ) {}

  ngOnInit() { this.carregarCaixas(); }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregarCaixas() {
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/caixas?status=fechado`).subscribe({
      next: r => {
        this.caixas.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  selecionarCaixa(id: number) { this.caixaSelecionadoId.set(id); }

  avancar() {
    const id = this.caixaSelecionadoId();
    if (!id) { this.modal.aviso('Seleção', 'Selecione um caixa para conferir.'); return; }
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/caixas/${id}/conferencia`).subscribe({
      next: r => {
        this.conferencia.set(r.data);
        // Pré-marca os já conferidos
        const set = new Set<number>();
        (r.data.formasPagamento ?? []).forEach((f: FormaPagamento) => {
          f.movimentos.forEach(m => { if (m.statusConferencia === 3) set.add(m.id); });
        });
        this.selecionados.set(set);
        this.etapa.set('conferencia');
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.modal.erro('Erro', 'Erro ao carregar conferência.');
      }
    });
  }

  voltar() {
    this.etapa.set('selecao');
    this.conferencia.set(null);
    this.formaExpandidaId.set(null);
    this.selecionados.set(new Set());
  }

  toggleForma(id: number | undefined) {
    if (!id) return;
    this.formaExpandidaId.update(v => v === id ? null : id);
  }

  toggleMovimento(id: number) {
    this.selecionados.update(s => {
      const ns = new Set(s);
      if (ns.has(id)) ns.delete(id); else ns.add(id);
      return ns;
    });
  }

  isSelecionado(id: number): boolean { return this.selecionados().has(id); }

  totalSelecionadoForma(forma: FormaPagamento): number {
    return forma.movimentos
      .filter(m => this.selecionados().has(m.id))
      .reduce((s, m) => s + this.valorAjustado(m), 0);
  }

  private valorAjustado(m: MovimentoItem): number {
    // Sangria e Pagamento são saídas
    if (m.tipo === 4 || m.tipo === 7) return -m.valor;
    return m.valor;
  }

  corDiferenca(d: number): string {
    if (Math.abs(d) < 0.01) return '#2e7d32';
    return '#c62828';
  }

  corStatus(status: number): string {
    if (status === 3) return '#2e7d32';
    if (status === 2) return '#f39c12';
    return '#90a4ae';
  }

  nomeStatus(status: number): string {
    if (status === 3) return 'Conferido';
    if (status === 2) return 'Pendente Conferente';
    return 'Pendente';
  }

  confirmarConferencia() {
    const id = this.conferencia()?.caixaId;
    if (!id) return;
    const body = {
      movimentoIdsConferidos: Array.from(this.selecionados()),
      observacao: this.observacao() || null
    };
    this.salvando.set(true);
    this.http.post<any>(`${this.apiUrl}/caixas/${id}/conferir`, body).subscribe({
      next: () => {
        this.salvando.set(false);
        this.modal.sucesso('Conferência', 'Caixa conferido com sucesso.');
        this.voltar();
        this.carregarCaixas();
      },
      error: (err: any) => {
        this.salvando.set(false);
        this.modal.erro('Erro', err?.error?.message || 'Erro ao confirmar conferência.');
      }
    });
  }
}
