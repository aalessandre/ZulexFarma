import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface TodoItem {
  id: number;
  titulo: string;
  descricao?: string;
  tipo?: string;
  prioridade?: string;
  status: string;
  modulo?: string;
  criadoPor?: string;
  atribuidoPara?: string;
  dataLimite?: string;
  dataConclusao?: string;
  criadoEm?: string;
}

const TIPOS = ['Bug', 'Melhoria', 'Ideia', 'Tarefa'];
const PRIORIDADES = ['Baixa', 'Media', 'Alta', 'Urgente'];
const MODULOS = ['Geral', 'Dashboard', 'Produtos', 'Compras', 'Financeiro', 'Contas a Pagar', 'Contas a Receber', 'Estoque', 'Vendas', 'Caixa', 'Fiscal', 'Sync', 'Configurações', 'Relatórios', 'Cadastros'];

@Component({
  selector: 'app-todo-board',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './todo-board.component.html',
  styleUrl: './todo-board.component.scss'
})
export class TodoBoardComponent implements OnInit {
  items = signal<TodoItem[]>([]);
  carregando = signal(false);
  modalAberto = signal(false);
  editandoId = signal<number | null>(null);

  // Form
  fTitulo = signal('');
  fDescricao = signal('');
  fTipo = signal('Tarefa');
  fPrioridade = signal('Media');
  fModulo = signal('Geral');
  fAtribuido = signal('');
  fDataLimite = signal('');

  // Filtros
  filtroTipo = signal('');
  filtroPrioridade = signal('');
  filtroModulo = signal('');

  tipos = TIPOS;
  prioridades = PRIORIDADES;
  modulos = MODULOS;

  abertos = computed(() => this.filtrar(this.items().filter(i => i.status === 'aberto')));
  emAndamento = computed(() => this.filtrar(this.items().filter(i => i.status === 'em_andamento')));
  concluidos = computed(() => this.filtrar(this.items().filter(i => i.status === 'concluido')));

  totalAbertos = computed(() => this.abertos().length);
  totalAndamento = computed(() => this.emAndamento().length);
  totalConcluidos = computed(() => this.concluidos().length);

  private apiUrl = `${environment.apiUrl}/todoboard`;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() { this.carregar(); }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => { this.items.set(r.data ?? []); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  private filtrar(lista: TodoItem[]): TodoItem[] {
    const tipo = this.filtroTipo();
    const prio = this.filtroPrioridade();
    const modulo = this.filtroModulo();
    return lista.filter(i => {
      if (tipo && i.tipo !== tipo) return false;
      if (prio && i.prioridade !== prio) return false;
      if (modulo && i.modulo !== modulo) return false;
      return true;
    });
  }

  // ── Modal ─────────────────────────────────────────────────────────
  abrirNovo() {
    this.editandoId.set(null);
    this.fTitulo.set(''); this.fDescricao.set(''); this.fTipo.set('Tarefa');
    this.fPrioridade.set('Media'); this.fModulo.set('Geral');
    this.fAtribuido.set(''); this.fDataLimite.set('');
    this.modalAberto.set(true);
  }

  abrirEditar(item: TodoItem) {
    this.editandoId.set(item.id);
    this.fTitulo.set(item.titulo); this.fDescricao.set(item.descricao ?? '');
    this.fTipo.set(item.tipo ?? 'Tarefa'); this.fPrioridade.set(item.prioridade ?? 'Media');
    this.fModulo.set(item.modulo ?? 'Geral'); this.fAtribuido.set(item.atribuidoPara ?? '');
    this.fDataLimite.set(item.dataLimite ?? '');
    this.modalAberto.set(true);
  }

  fecharModal() { this.modalAberto.set(false); }

  salvar() {
    if (!this.fTitulo().trim()) return;
    const usuario = this.auth.usuarioLogado();
    const body = {
      titulo: this.fTitulo(), descricao: this.fDescricao(), tipo: this.fTipo(),
      prioridade: this.fPrioridade(), modulo: this.fModulo(),
      atribuidoPara: this.fAtribuido() || null, dataLimite: this.fDataLimite() || null,
      criadoPor: usuario?.nome || usuario?.login || '',
      status: this.editandoId() ? undefined : 'aberto'
    };

    const op$ = this.editandoId()
      ? this.http.put(`${this.apiUrl}/${this.editandoId()}`, body)
      : this.http.post(this.apiUrl, body);

    op$.subscribe({
      next: () => { this.fecharModal(); this.carregar(); },
      error: () => this.modal.erro('Erro', 'Erro ao salvar.')
    });
  }

  // ── Ações rápidas ─────────────────────────────────────────────────
  moverPara(id: number, status: string) {
    this.http.put(`${this.apiUrl}/${id}/status`, { status }).subscribe({
      next: () => this.carregar()
    });
  }

  async excluir(item: TodoItem) {
    const r = await this.modal.confirmar('Excluir', `Excluir "${item.titulo}"?`, 'Sim', 'Não');
    if (!r.confirmado) return;
    this.http.delete(`${this.apiUrl}/${item.id}`).subscribe({ next: () => this.carregar() });
  }

  // ── Helpers visuais ───────────────────────────────────────────────
  tipoCor(tipo?: string): string {
    switch (tipo) {
      case 'Bug': return '#e74c3c';
      case 'Melhoria': return '#2980b9';
      case 'Ideia': return '#8e44ad';
      case 'Tarefa': return '#27ae60';
      default: return '#7f8c8d';
    }
  }

  prioCor(prio?: string): string {
    switch (prio) {
      case 'Urgente': return '#e74c3c';
      case 'Alta': return '#e67e22';
      case 'Media': return '#f39c12';
      case 'Baixa': return '#95a5a6';
      default: return '#95a5a6';
    }
  }

  formatarData(d?: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('pt-BR');
  }
}
