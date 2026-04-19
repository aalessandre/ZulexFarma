import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

type StatusEntrega = 'Pendente' | 'EmPreparacao' | 'SaiuParaEntrega' | 'Entregue' | 'Cancelada' | 'Devolvida';

interface EntregaLista {
  id: number;
  vendaId: number;
  clienteId: number;
  clienteNome: string;
  clienteTelefone: string;
  entregadorId?: number | null;
  entregadorNome?: string | null;
  status: StatusEntrega;
  statusNome: string;
  valorEntrega: number;
  distanciaKm: number;
  bairro: string;
  cidade: string;
  uf: string;
  dataPedido: string;
  dataSaida?: string | null;
  dataEntrega?: string | null;
  tokenRastreamento: string;
}

interface ColaboradorOpcao { id: number; nome: string; cargo?: string; ativo: boolean; }

const STATUS_LABELS: Record<string, string> = {
  Pendente: 'Pendente',
  EmPreparacao: 'Em Preparação',
  SaiuParaEntrega: 'Em rota',
  Entregue: 'Entregue',
  Cancelada: 'Cancelada',
  Devolvida: 'Devolvida'
};

const STATUS_CORES: Record<string, string> = {
  Pendente: '#e67e22',
  EmPreparacao: '#3498db',
  SaiuParaEntrega: '#2980b9',
  Entregue: '#27ae60',
  Cancelada: '#95a5a6',
  Devolvida: '#c0392b'
};

@Component({
  selector: 'app-entregas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './entregas.component.html',
  styleUrl: './entregas.component.scss'
})
export class EntregasComponent implements OnInit {
  private apiUrl = environment.apiUrl;

  // Estado
  entregas = signal<EntregaLista[]>([]);
  carregando = signal(false);

  // Filtros
  filtroStatus = signal<'ativas' | 'todas' | StatusEntrega>('ativas');
  filtroData = signal<'hoje' | '7dias' | '30dias' | 'todas'>('hoje');
  busca = signal('');

  // Modais
  modalDespachar = signal<EntregaLista | null>(null);
  entregadoresDespachar = signal<ColaboradorOpcao[]>([]);
  entregadorIdDespachar = signal<number | null>(null);
  carregandoEntregadores = signal(false);

  modalBaixar = signal<EntregaLista | null>(null);
  caixaAbertoId = signal<number | null>(null);
  baixarObservacao = signal('');

  modalCancelar = signal<EntregaLista | null>(null);
  cancelarObservacao = signal('');

  modalDetalhe = signal<any | null>(null);

  // Listas filtradas
  entregasFiltradas = computed(() => {
    const f = this.filtroStatus();
    const busca = this.busca().toLowerCase().trim();
    let lista = this.entregas();

    if (f === 'ativas') {
      lista = lista.filter(e => e.status === 'Pendente' || e.status === 'EmPreparacao' || e.status === 'SaiuParaEntrega');
    } else if (f !== 'todas') {
      lista = lista.filter(e => e.status === f);
    }

    if (busca) {
      lista = lista.filter(e =>
        e.clienteNome.toLowerCase().includes(busca) ||
        e.bairro.toLowerCase().includes(busca) ||
        e.cidade.toLowerCase().includes(busca) ||
        (e.entregadorNome?.toLowerCase().includes(busca)) ||
        String(e.vendaId).includes(busca)
      );
    }
    return lista;
  });

  totais = computed(() => {
    const list = this.entregas();
    return {
      pendentes: list.filter(e => e.status === 'Pendente' || e.status === 'EmPreparacao').length,
      emRota: list.filter(e => e.status === 'SaiuParaEntrega').length,
      entregues: list.filter(e => e.status === 'Entregue').length,
      canceladas: list.filter(e => e.status === 'Cancelada' || e.status === 'Devolvida').length
    };
  });

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() {
    this.verificarCaixaAberto();
    this.carregar();
  }

  verificarCaixaAberto() {
    this.http.get<any>(`${this.apiUrl}/caixas/aberto`).subscribe({
      next: r => { if (r.data?.id) this.caixaAbertoId.set(r.data.id); },
      error: () => {}
    });
  }

  carregar() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.carregando.set(true);

    let params: any = { filialId: filialId.toString() };
    const hoje = new Date();
    const inicio = new Date(hoje);
    if (this.filtroData() === 'hoje') inicio.setHours(0, 0, 0, 0);
    else if (this.filtroData() === '7dias') { inicio.setDate(hoje.getDate() - 7); inicio.setHours(0, 0, 0, 0); }
    else if (this.filtroData() === '30dias') { inicio.setDate(hoje.getDate() - 30); inicio.setHours(0, 0, 0, 0); }

    if (this.filtroData() !== 'todas') params.dataInicio = inicio.toISOString();

    this.http.get<any>(`${this.apiUrl}/entregas`, { params }).subscribe({
      next: r => {
        this.entregas.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.modal.erro('Entregas', 'Erro ao carregar entregas.');
      }
    });
  }

  // ── Ações ─────────────────────────────────────────────────────
  abrirDespachar(e: EntregaLista) {
    this.modalDespachar.set(e);
    this.entregadorIdDespachar.set(e.entregadorId ?? null);
    if (this.entregadoresDespachar().length === 0) this.carregarEntregadores();
  }

  private carregarEntregadores() {
    this.carregandoEntregadores.set(true);
    this.http.get<any>(`${this.apiUrl}/colaboradores`).subscribe({
      next: r => {
        const lista: ColaboradorOpcao[] = (r.data ?? []).filter((c: ColaboradorOpcao) => c.ativo);
        this.entregadoresDespachar.set(lista);
        this.carregandoEntregadores.set(false);
      },
      error: () => this.carregandoEntregadores.set(false)
    });
  }

  confirmarDespachar() {
    const e = this.modalDespachar();
    const entregadorId = this.entregadorIdDespachar();
    if (!e || !entregadorId) {
      this.modal.aviso('Entregador', 'Selecione o entregador.');
      return;
    }
    this.http.post<any>(`${this.apiUrl}/entregas/${e.id}/atribuir-entregador`, { entregadorId }).subscribe({
      next: () => {
        this.http.post<any>(`${this.apiUrl}/entregas/${e.id}/status`, { novoStatus: 3, observacao: 'Despachada' }).subscribe({
          next: () => {
            this.modalDespachar.set(null);
            this.modal.sucesso('Despachada', 'Entrega despachada com sucesso.');
            this.carregar();
          },
          error: (err: any) => this.modal.erro('Despachar', err?.error?.message || 'Erro ao mudar status.')
        });
      },
      error: (err: any) => this.modal.erro('Despachar', err?.error?.message || 'Erro ao atribuir entregador.')
    });
  }

  cancelarDespachar() { this.modalDespachar.set(null); }

  abrirBaixar(e: EntregaLista) {
    if (!this.caixaAbertoId()) {
      this.modal.aviso('Caixa fechado', 'Abra um caixa antes de baixar entregas (o recebimento é contabilizado no caixa aberto).');
      return;
    }
    this.modalBaixar.set(e);
    this.baixarObservacao.set('');
  }

  confirmarBaixar() {
    const e = this.modalBaixar();
    if (!e) return;
    const body = {
      caixaAtualId: this.caixaAbertoId(),
      observacao: this.baixarObservacao().trim() || null
    };
    this.http.post<any>(`${this.apiUrl}/entregas/${e.id}/baixar`, body).subscribe({
      next: () => {
        this.modalBaixar.set(null);
        this.modal.sucesso('Baixada', 'Entrega baixada com sucesso.');
        this.carregar();
      },
      error: (err: any) => this.modal.erro('Baixar', err?.error?.message || 'Erro ao baixar entrega.')
    });
  }

  cancelarBaixar() { this.modalBaixar.set(null); }

  abrirCancelar(e: EntregaLista) {
    this.modalCancelar.set(e);
    this.cancelarObservacao.set('');
  }

  confirmarCancelar() {
    const e = this.modalCancelar();
    if (!e) return;
    const body = { novoStatus: 5, observacao: this.cancelarObservacao().trim() || null };
    this.http.post<any>(`${this.apiUrl}/entregas/${e.id}/cancelar`, body).subscribe({
      next: () => {
        this.modalCancelar.set(null);
        this.modal.sucesso('Cancelada', 'Entrega cancelada.');
        this.carregar();
      },
      error: (err: any) => this.modal.erro('Cancelar', err?.error?.message || 'Erro ao cancelar entrega.')
    });
  }

  cancelarCancelar() { this.modalCancelar.set(null); }

  abrirDetalhe(e: EntregaLista) {
    this.http.get<any>(`${this.apiUrl}/entregas/${e.id}`).subscribe({
      next: r => this.modalDetalhe.set(r.data),
      error: () => this.modal.erro('Detalhes', 'Erro ao carregar detalhes da entrega.')
    });
  }

  fecharDetalhe() { this.modalDetalhe.set(null); }

  copiarLinkRastreio(token: string) {
    const url = `${window.location.origin}/rastreio/${token}`;
    navigator.clipboard.writeText(url).then(
      () => this.modal.sucesso('Link copiado', url),
      () => this.modal.erro('Copiar', 'Falha ao copiar link.')
    );
  }

  // ── Helpers ───────────────────────────────────────────────────
  podeDespachar(e: EntregaLista): boolean { return e.status === 'Pendente' || e.status === 'EmPreparacao'; }
  podeBaixar(e: EntregaLista): boolean { return e.status === 'SaiuParaEntrega'; }
  podeCancelar(e: EntregaLista): boolean { return e.status === 'Pendente' || e.status === 'EmPreparacao'; }

  labelStatus(s: string | null | undefined): string { return s ? (STATUS_LABELS[s] ?? s) : ''; }
  corStatus(s: string | null | undefined): string { return s ? (STATUS_CORES[s] ?? '#888') : '#888'; }
}
