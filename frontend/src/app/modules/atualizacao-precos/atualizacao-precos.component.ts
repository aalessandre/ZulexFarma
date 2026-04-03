import { Component, OnInit, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { TabService } from '../../core/services/tab.service';

interface GrupoPrincipal { id: number; nome: string; }
interface PreviewItem {
  produtoId: number; produtoDadosId: number; produtoNome: string; ean: string;
  grupoPrincipalNome: string; valorVendaAtual: number; valorVendaNovo: number;
  pmcAtual: number; pmcNovo: number; variacaoPercent: number;
}
interface Historico {
  id: number; tipo: string; dataExecucao: string; nomeUsuario: string;
  totalProdutos: number; totalAlterados: number; status: string;
}

@Component({
  selector: 'app-atualizacao-precos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './atualizacao-precos.component.html',
  styleUrls: ['./atualizacao-precos.component.scss']
})
export class AtualizacaoPrecosComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/atualizacao-precos`;

  // ── Base info ─────────────────────────────────────────────────
  baseTotal = signal(0);
  baseUltimaAtualizacao = signal<string | null>(null);
  uploadando = signal(false);

  // ── Filtros ───────────────────────────────────────────────────
  origem = signal<'ABCFARMA'>('ABCFARMA');
  fonte = signal<'LOCAL'>('LOCAL');
  modo = signal<'AMBOS' | 'AUMENTAR' | 'REDUZIR'>('AMBOS');
  acao = signal<'LISTA' | 'AUTOMATICO'>('LISTA');
  reajustarPromocoes = signal(false);
  reajustarOfertas = signal(false);
  gruposPrincipais = signal<GrupoPrincipal[]>([]);
  gruposSelecionados = signal<number[]>([]);

  // ── Ordenação preview ──────────────────────────────────────────
  sortColuna = signal<string>('produtoNome');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  private resizingCol: string | null = null;
  private resizeStartX = 0;
  private resizeStartW = 0;

  // ── Processamento ─────────────────────────────────────────────
  processando = signal(false);
  resultado = signal<PreviewItem[]>([]);
  totalAnalisados = signal(0);
  totalAlterados = signal(0);
  atualizacaoId = signal<number | null>(null);
  mostrarResultado = signal(false);

  // ── Histórico ─────────────────────────────────────────────────
  historico = signal<Historico[]>([]);
  revertendo = signal<number | null>(null);

  // ── Seleção no preview ─────────────────────────────────────────
  itensSelecionados = signal<Set<number>>(new Set());

  // ── Modal ─────────────────────────────────────────────────────
  modalConfirm = signal(false);
  modalTitulo = signal('');
  modalMensagem = signal('');
  modalCallback: (() => void) | null = null;

  // ── Geral ─────────────────────────────────────────────────────
  erro = signal('');
  mensagem = signal('');

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private tabService: TabService
  ) {}

  ngOnInit() {
    this.carregarInfoBase();
    this.carregarGrupos();
    this.carregarHistorico();
  }

  private carregarInfoBase() {
    this.http.get<any>(`${this.apiUrl}/info-base`).subscribe({
      next: r => {
        this.baseTotal.set(r.data?.totalRegistros ?? 0);
        this.baseUltimaAtualizacao.set(r.data?.ultimaAtualizacao);
      }
    });
  }

  private carregarGrupos() {
    this.http.get<any>(`${environment.apiUrl}/grupos-principais`).subscribe({
      next: r => this.gruposPrincipais.set((r.data ?? []).filter((g: any) => g.ativo))
    });
  }

  private carregarHistorico() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.http.get<any>(`${this.apiUrl}/historico/${filialId}`).subscribe({
      next: r => this.historico.set(r.data ?? [])
    });
  }

  // ── Upload base ABCFarma ──────────────────────────────────────
  uploadProgresso = signal('');

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];

    const tamanhoMB = (file.size / 1024 / 1024).toFixed(1);
    this.uploadando.set(true);
    this.erro.set('');
    this.mensagem.set('');
    this.uploadProgresso.set(`Lendo arquivo (${tamanhoMB} MB)...`);

    const reader = new FileReader();
    reader.onload = () => {
      this.uploadProgresso.set('Enviando para o servidor...');
      const conteudo = reader.result as string;
      this.http.post<any>(`${this.apiUrl}/upload-base`, { conteudoJson: conteudo }).subscribe({
        next: r => {
          const d = r.data;
          this.mensagem.set(`Base atualizada com sucesso! ${d.inseridos} produtos carregados.`);
          this.uploadando.set(false);
          this.uploadProgresso.set('');
          this.carregarInfoBase();
          input.value = '';
        },
        error: e => {
          const msg = e?.error?.message || 'Erro ao carregar base. Verifique se o arquivo e um JSON valido do ABCFarma.';
          this.erro.set(msg);
          this.uploadando.set(false);
          this.uploadProgresso.set('');
          input.value = '';
        }
      });
    };
    reader.readAsText(file);
  }

  // ── Processar atualização ─────────────────────────────────────

  processar() {
    if (this.baseTotal() === 0) {
      this.erro.set('Base ABCFarma vazia. Carregue o arquivo primeiro.');
      return;
    }

    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);

    this.processando.set(true);
    this.erro.set('');
    this.mensagem.set('');
    this.resultado.set([]);
    this.itensSelecionados.set(new Set());

    this.http.post<any>(`${this.apiUrl}/processar`, {
      filialId,
      modo: this.modo(),
      gruposPrincipaisIds: this.gruposSelecionados(),
      reajustarPromocoes: this.reajustarPromocoes(),
      reajustarOfertas: this.reajustarOfertas(),
      acao: this.acao(),
      nomeUsuario: usuario?.nome || ''
    }).subscribe({
      next: r => {
        const d = r.data;
        this.resultado.set(d.itens ?? []);
        this.totalAnalisados.set(d.totalProdutos);
        this.totalAlterados.set(d.totalAlterados);
        this.atualizacaoId.set(d.atualizacaoPrecoId);
        this.mostrarResultado.set(true);
        this.processando.set(false);

        if (this.acao() === 'AUTOMATICO' && d.totalAlterados > 0) {
          this.mensagem.set(`Atualização aplicada: ${d.totalAlterados} produtos alterados de ${d.totalProdutos} analisados.`);
          this.carregarHistorico();
        }
      },
      error: e => {
        this.erro.set(e?.error?.message || 'Erro ao processar.');
        this.processando.set(false);
      }
    });
  }

  // ── Reverter ──────────────────────────────────────────────────

  pedirReverter(id: number) {
    this.abrirModal('Reverter Atualizacao', 'Os precos voltarao aos valores anteriores. Deseja continuar?', () => {
      this.revertendo.set(id);
      this.http.post<any>(`${this.apiUrl}/reverter/${id}`, {}).subscribe({
        next: () => {
          this.mensagem.set('Atualizacao revertida com sucesso.');
          this.revertendo.set(null);
          this.carregarHistorico();
        },
        error: e => {
          this.erro.set(e?.error?.message || 'Erro ao reverter.');
          this.revertendo.set(null);
        }
      });
    });
  }

  // ── Aplicar do preview ────────────────────────────────────────

  aplicarDoPreview() {
    const selecionados = this.itensSelecionados();
    if (selecionados.size === 0) {
      this.abrirModal('Nenhum produto selecionado', 'Selecione os produtos que deseja atualizar usando os checkboxes da lista.', null);
      return;
    }

    this.abrirModal('Aplicar Atualizacao',
      `Deseja aplicar a atualizacao de precos para ${selecionados.size} produto(s) selecionado(s)?`, () => {
      const usuario = this.auth.usuarioLogado();
      const filialId = parseInt(usuario?.filialId || '1', 10);

      this.processando.set(true);
      this.http.post<any>(`${this.apiUrl}/processar`, {
        filialId,
        modo: this.modo(),
        gruposPrincipaisIds: this.gruposSelecionados(),
        reajustarPromocoes: this.reajustarPromocoes(),
        reajustarOfertas: this.reajustarOfertas(),
        acao: 'AUTOMATICO',
        nomeUsuario: usuario?.nome || '',
        produtoDadosIds: Array.from(selecionados)
      }).subscribe({
        next: r => {
          this.mensagem.set(`Atualizacao aplicada: ${r.data.totalAlterados} produtos alterados.`);
          this.processando.set(false);
          this.mostrarResultado.set(false);
          this.itensSelecionados.set(new Set());
          this.carregarHistorico();
        },
        error: e => {
          this.erro.set(e?.error?.message || 'Erro ao aplicar.');
          this.processando.set(false);
        }
      });
    });
  }

  // ── Seleção de itens no preview ───────────────────────────────

  toggleItemSelecionado(id: number) {
    this.itensSelecionados.update(s => {
      const novo = new Set(s);
      if (novo.has(id)) novo.delete(id); else novo.add(id);
      return novo;
    });
  }

  toggleTodosSelecionados() {
    const itens = this.resultado();
    if (this.itensSelecionados().size === itens.length) {
      this.itensSelecionados.set(new Set());
    } else {
      this.itensSelecionados.set(new Set(itens.map(i => i.produtoDadosId)));
    }
  }

  isItemSelecionado(id: number): boolean { return this.itensSelecionados().has(id); }
  todosSelecionados(): boolean { return this.resultado().length > 0 && this.itensSelecionados().size === this.resultado().length; }

  // ── Modal genérica ────────────────────────────────────────────

  abrirModal(titulo: string, mensagem: string, callback: (() => void) | null) {
    this.modalTitulo.set(titulo);
    this.modalMensagem.set(mensagem);
    this.modalCallback = callback;
    this.modalConfirm.set(true);
  }

  confirmarModal() {
    this.modalConfirm.set(false);
    if (this.modalCallback) this.modalCallback();
  }

  fecharModal() { this.modalConfirm.set(false); }

  // ── Helpers ───────────────────────────────────────────────────

  toggleGrupo(id: number) {
    this.gruposSelecionados.update(g => {
      const idx = g.indexOf(id);
      return idx >= 0 ? g.filter(x => x !== id) : [...g, id];
    });
  }

  isGrupoSelecionado(id: number): boolean {
    return this.gruposSelecionados().includes(id);
  }

  fecharResultado() {
    this.mostrarResultado.set(false);
    this.resultado.set([]);
  }

  // ── Ordenação ─────────────────────────────────────────────────

  resultadoOrdenado = computed(() => {
    const lista = [...this.resultado()];
    const col = this.sortColuna();
    const dir = this.sortDirecao() === 'asc' ? 1 : -1;
    lista.sort((a: any, b: any) => {
      const va = a[col] ?? '';
      const vb = b[col] ?? '';
      if (typeof va === 'number') return (va - vb) * dir;
      return String(va).localeCompare(String(vb)) * dir;
    });
    return lista;
  });

  ordenar(campo: string) {
    if (this.sortColuna() === campo) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortColuna.set(campo); this.sortDirecao.set('asc'); }
  }

  sortIcon(campo: string): string {
    return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅';
  }

  iniciarResize(event: MouseEvent, col: string, largura: number) {
    event.preventDefault(); event.stopPropagation();
    this.resizingCol = col; this.resizeStartX = event.clientX; this.resizeStartW = largura;
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    if (!this.resizingCol) return;
    const diff = event.clientX - this.resizeStartX;
    const th = document.querySelector(`[data-col="${this.resizingCol}"]`) as HTMLElement;
    if (th) th.style.width = `${Math.max(40, this.resizeStartW + diff)}px`;
  }

  @HostListener('document:mouseup')
  onMouseUp() { this.resizingCol = null; }

  sairDaTela() { this.tabService.fecharTabAtiva(); }
}
