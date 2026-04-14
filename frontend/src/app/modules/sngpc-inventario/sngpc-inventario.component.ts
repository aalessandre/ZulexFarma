import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface Filial { id: number; nomeFilial: string; }
interface ProdutoLookup { id: number; texto: string; nome: string; classeTerapeutica: string | null; }
interface InventarioItem {
  id?: number;
  produtoId: number;
  produtoNome?: string;
  produtoCodigoBarras?: string;
  classeTerapeutica?: string | null;
  numeroLote: string;
  dataFabricacao: string | null;
  dataValidade: string | null;
  quantidade: number;
  registroMs?: string | null;
  observacao?: string | null;
}
interface InventarioList {
  id: number;
  filialId: number;
  filialNome?: string;
  dataInventario: string;
  descricao?: string;
  status: number;
  statusNome: string;
  dataFinalizacao?: string;
  totalItens: number;
  quantidadeTotal: number;
}
interface InventarioDetalhe {
  id: number;
  filialId: number;
  dataInventario: string;
  descricao?: string;
  status: number;
  dataFinalizacao?: string;
  observacao?: string;
  itens: InventarioItem[];
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'id',               label: 'ID',            largura: 70,  minLargura: 50,  padrao: true },
  { campo: 'dataInventario',   label: 'Data',          largura: 110, minLargura: 90,  padrao: true },
  { campo: 'filialNome',       label: 'Filial',        largura: 180, minLargura: 120, padrao: true },
  { campo: 'descricao',        label: 'Descrição',     largura: 280, minLargura: 150, padrao: true },
  { campo: 'totalItens',       label: 'Itens',         largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'quantidadeTotal',  label: 'Qtde Total',    largura: 110, minLargura: 80,  padrao: true },
  { campo: 'statusNome',       label: 'Status',        largura: 110, minLargura: 90,  padrao: true },
  { campo: 'dataFinalizacao',  label: 'Finalizado em', largura: 150, minLargura: 120, padrao: false },
];

type Modo = 'lista' | 'form';

@Component({
  selector: 'app-sngpc-inventario',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sngpc-inventario.component.html',
  styleUrl: './sngpc-inventario.component.scss'
})
export class SngpcInventarioComponent implements OnInit, OnDestroy {
  private api = `${environment.apiUrl}/sngpc/inventarios`;
  private produtosApi = `${environment.apiUrl}/produtos`;
  private filiaisApi = `${environment.apiUrl}/filiais`;
  private readonly STORAGE_KEY = 'zulex_colunas_sngpc_inventario';

  modo = signal<Modo>('lista');
  filiais = signal<Filial[]>([]);
  inventarios = signal<InventarioList[]>([]);
  carregando = signal(false);
  salvando = signal(false);

  // Form
  editId = signal<number | null>(null);
  filialId = signal<number>(0);
  dataInventario = signal<string>(new Date().toISOString().slice(0, 10));
  descricao = signal<string>('');
  observacao = signal<string>('');
  itens = signal<InventarioItem[]>([]);

  // Produto lookup
  produtoBusca = signal<string>('');
  produtoResultados = signal<ProdutoLookup[]>([]);
  produtoBuscaAberto = signal(false);

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal<string>('dataInventario');
  sortDirecao = signal<'asc' | 'desc'>('desc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  inventariosOrdenados = computed(() => {
    const col = this.sortColuna(); const dir = this.sortDirecao();
    const lista = [...this.inventarios()];
    if (!col) return lista;
    return lista.sort((a, b) => {
      const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() { this.carregarFiliais(); this.carregar(); }
  ngOnDestroy() {}

  carregarFiliais() {
    this.http.get<any>(this.filiaisApi).subscribe({
      next: r => {
        const lista: Filial[] = r.data ?? [];
        this.filiais.set(lista);
        if (lista.length > 0 && this.filialId() === 0) this.filialId.set(lista[0].id);
      }
    });
  }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.api).subscribe({
      next: r => { this.inventarios.set(r.data ?? []); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  // ── Columns ──────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }
  private salvarColunasStorage() { localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.colunas())); }
  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }
  restaurarPadrao() { this.colunas.set(COLUNAS.map(c => ({ ...c, visivel: c.padrao }))); this.salvarColunasStorage(); }
  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    else { this.sortColuna.set(coluna); this.sortDirecao.set('asc'); }
  }
  sortIcon(campo: string): string {
    return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅';
  }
  iniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    this.resizeState = { campo, startX: e.clientX, startWidth: largura };
    document.body.style.cursor = 'col-resize'; document.body.style.userSelect = 'none';
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
      document.body.style.cursor = ''; document.body.style.userSelect = '';
    }
  }
  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => { const arr = [...cols]; const [m] = arr.splice(this.dragColIdx!, 1); arr.splice(idx, 0, m); this.dragColIdx = idx; return arr; });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunasStorage(); }

  getCellValue(i: InventarioList, campo: string): string {
    switch (campo) {
      case 'dataInventario':   return i.dataInventario ? new Date(i.dataInventario).toLocaleDateString('pt-BR') : '—';
      case 'dataFinalizacao':  return i.dataFinalizacao ? new Date(i.dataFinalizacao).toLocaleString('pt-BR') : '—';
      case 'quantidadeTotal':  return i.quantidadeTotal.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 3 });
      case 'descricao':        return i.descricao || '—';
      default:                 return (i as any)[campo]?.toString() ?? '—';
    }
  }

  // ── Form ────────────────────────────────────────────────────────
  novo() {
    this.editId.set(null);
    this.dataInventario.set(new Date().toISOString().slice(0, 10));
    this.descricao.set(''); this.observacao.set('');
    this.itens.set([]);
    this.modo.set('form');
  }

  async editar(inv: InventarioList) {
    if (inv.status !== 1) {
      await this.modal.aviso('Não editável', 'Inventário já finalizado não pode ser editado.');
      return;
    }
    this.http.get<any>(`${this.api}/${inv.id}`).subscribe({
      next: r => {
        const d: InventarioDetalhe = r.data;
        this.editId.set(d.id);
        this.filialId.set(d.filialId);
        this.dataInventario.set(d.dataInventario.substring(0, 10));
        this.descricao.set(d.descricao || '');
        this.observacao.set(d.observacao || '');
        this.itens.set(d.itens.map(i => ({
          ...i,
          dataFabricacao: i.dataFabricacao ? i.dataFabricacao.substring(0, 10) : null,
          dataValidade: i.dataValidade ? i.dataValidade.substring(0, 10) : null
        })));
        this.modo.set('form');
      }
    });
  }

  voltar() { this.modo.set('lista'); this.carregar(); }

  onProdutoInput(valor: string) {
    this.produtoBusca.set(valor);
    if (valor.length < 2) { this.produtoResultados.set([]); return; }
    this.http.get<any>(`${this.produtosApi}/pesquisar?termo=${encodeURIComponent(valor)}`).subscribe({
      next: r => {
        this.produtoResultados.set((r.data ?? []).slice(0, 20).map((p: any) => ({
          id: p.id,
          texto: `${p.nome}${p.codigoBarras ? ' — ' + p.codigoBarras : ''}`,
          nome: p.nome,
          classeTerapeutica: p.classeTerapeutica ?? null
        })));
        this.produtoBuscaAberto.set(true);
      }
    });
  }

  selecionarProduto(p: ProdutoLookup) {
    this.itens.update(arr => [...arr, {
      produtoId: p.id, produtoNome: p.nome, classeTerapeutica: p.classeTerapeutica,
      numeroLote: '', dataFabricacao: null, dataValidade: null, quantidade: 0,
      registroMs: null, observacao: null
    }]);
    this.produtoBusca.set(''); this.produtoResultados.set([]); this.produtoBuscaAberto.set(false);
  }

  removerItem(idx: number) { this.itens.update(arr => arr.filter((_, i) => i !== idx)); }
  atualizarItem(idx: number, campo: keyof InventarioItem, valor: any) {
    this.itens.update(arr => { const copy = [...arr]; copy[idx] = { ...copy[idx], [campo]: valor }; return copy; });
  }

  salvar() {
    if (this.filialId() <= 0) { this.modal.aviso('Filial', 'Selecione a filial.'); return; }
    if (this.itens().length === 0) { this.modal.aviso('Itens', 'Adicione ao menos um produto.'); return; }
    for (const it of this.itens()) {
      if (!it.numeroLote.trim()) { this.modal.aviso('Lote', `Informe o lote do produto "${it.produtoNome}".`); return; }
      if (it.quantidade <= 0) { this.modal.aviso('Quantidade', `Informe a quantidade do produto "${it.produtoNome}".`); return; }
    }

    this.salvando.set(true);
    const body = {
      filialId: this.filialId(),
      dataInventario: this.dataInventario(),
      descricao: this.descricao() || null,
      observacao: this.observacao() || null,
      itens: this.itens()
    };
    const req = this.editId()
      ? this.http.put<any>(`${this.api}/${this.editId()}`, body)
      : this.http.post<any>(this.api, body);
    req.subscribe({
      next: () => { this.salvando.set(false); this.voltar(); },
      error: (e: any) => {
        this.salvando.set(false);
        this.modal.erro('Erro', e?.error?.message || 'Erro ao salvar inventário.');
      }
    });
  }

  async finalizar(inv: InventarioList) {
    const r = await this.modal.confirmar(
      'Finalizar inventário?',
      `Finalizar o inventário de ${inv.dataInventario.substring(0, 10)} aplicará os saldos aos lotes do estoque. Esta ação é irreversível.`,
      'Sim, finalizar', 'Cancelar'
    );
    if (!r.confirmado) return;
    this.http.post<any>(`${this.api}/${inv.id}/finalizar`, {}).subscribe({
      next: r => {
        this.modal.sucesso('OK', `Inventário finalizado. ${r.lotesCriados} lotes aplicados ao estoque.`);
        this.carregar();
      },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao finalizar.')
    });
  }

  async excluir(inv: InventarioList) {
    if (inv.status !== 1) { this.modal.aviso('Não pode', 'Só é possível excluir inventário em rascunho.'); return; }
    const r = await this.modal.confirmar('Excluir inventário', `Excluir o inventário de ${inv.dataInventario.substring(0, 10)}?`, 'Sim', 'Não');
    if (!r.confirmado) return;
    this.http.delete(`${this.api}/${inv.id}`).subscribe({
      next: () => this.carregar(),
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao excluir.')
    });
  }
}
