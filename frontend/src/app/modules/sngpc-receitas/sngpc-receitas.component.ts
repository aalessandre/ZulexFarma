import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface Receita {
  id: number;
  filialId: number;
  vendaId?: number;
  medicoNome: string;
  medicoCrm?: string;
  pacienteNome: string;
  numeroReceita?: string;
  dataEmissao: string;
  tipoReceita?: string;
  totalItens: number;
}

interface ReceitaItem {
  id?: number;
  produtoId: number;
  produtoNome?: string;
  quantidade: number;
  posologia?: string | null;
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'id',             label: 'ID',         largura: 70,  minLargura: 50,  padrao: true },
  { campo: 'dataEmissao',    label: 'Data',       largura: 110, minLargura: 90,  padrao: true },
  { campo: 'tipoReceita',    label: 'Tipo',       largura: 120, minLargura: 90,  padrao: true },
  { campo: 'medicoNome',     label: 'Médico',     largura: 220, minLargura: 140, padrao: true },
  { campo: 'medicoCrm',      label: 'CRM',        largura: 90,  minLargura: 70,  padrao: true },
  { campo: 'pacienteNome',   label: 'Paciente',   largura: 220, minLargura: 140, padrao: true },
  { campo: 'numeroReceita',  label: 'Nº Receita', largura: 130, minLargura: 90,  padrao: true },
  { campo: 'totalItens',     label: 'Itens',      largura: 70,  minLargura: 50,  padrao: true },
];

@Component({
  selector: 'app-sngpc-receitas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sngpc-receitas.component.html',
  styleUrl: './sngpc-receitas.component.scss'
})
export class SngpcReceitasComponent implements OnInit, OnDestroy {
  private api = `${environment.apiUrl}/sngpc/receitas`;
  private produtosApi = `${environment.apiUrl}/produtos`;
  private readonly STORAGE_KEY = 'zulex_colunas_sngpc_receitas';

  receitas = signal<Receita[]>([]);
  carregando = signal(false);

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal<string>('dataEmissao');
  sortDirecao = signal<'asc' | 'desc'>('desc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  receitasOrdenadas = computed(() => {
    const col = this.sortColuna(); const dir = this.sortDirecao();
    const lista = [...this.receitas()];
    if (!col) return lista;
    return lista.sort((a, b) => {
      const va = (a as any)[col] ?? ''; const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'number' ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  // Modal
  modalAberto = signal(false);
  editId = signal<number | null>(null);
  filialId = signal(1);
  medicoNome = signal(''); medicoCrm = signal(''); medicoUf = signal(''); medicoCpf = signal('');
  pacienteNome = signal(''); pacienteCpf = signal(''); pacienteEndereco = signal('');
  pacienteCidade = signal(''); pacienteUf = signal('');
  numeroReceita = signal('');
  dataEmissao = signal(new Date().toISOString().slice(0, 10));
  tipoReceita = signal<string>('Amarela');
  observacao = signal('');
  itens = signal<ReceitaItem[]>([]);

  produtoBusca = signal('');
  produtoResultados = signal<any[]>([]);

  readonly tiposReceita = ['Amarela', 'Azul', 'Azul B2', 'Branca', 'Especial'];

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() { this.carregar(); }
  ngOnDestroy() {}

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.api).subscribe({
      next: r => { this.receitas.set(r.data ?? []); this.carregando.set(false); },
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

  getCellValue(r: Receita, campo: string): string {
    switch (campo) {
      case 'dataEmissao': return r.dataEmissao ? new Date(r.dataEmissao).toLocaleDateString('pt-BR') : '—';
      default:            return (r as any)[campo]?.toString() ?? '—';
    }
  }

  // ── Modal CRUD ──────────────────────────────────────────────────
  novaReceita() {
    this.editId.set(null);
    this.medicoNome.set(''); this.medicoCrm.set(''); this.medicoUf.set(''); this.medicoCpf.set('');
    this.pacienteNome.set(''); this.pacienteCpf.set(''); this.pacienteEndereco.set('');
    this.pacienteCidade.set(''); this.pacienteUf.set('');
    this.numeroReceita.set('');
    this.dataEmissao.set(new Date().toISOString().slice(0, 10));
    this.tipoReceita.set('Amarela');
    this.observacao.set('');
    this.itens.set([]);
    this.modalAberto.set(true);
  }

  editar(r: Receita) {
    this.http.get<any>(`${this.api}/${r.id}`).subscribe({
      next: resp => {
        const d = resp.data;
        this.editId.set(d.id);
        this.medicoNome.set(d.medicoNome); this.medicoCrm.set(d.medicoCrm || '');
        this.medicoUf.set(d.medicoUf || ''); this.medicoCpf.set(d.medicoCpf || '');
        this.pacienteNome.set(d.pacienteNome); this.pacienteCpf.set(d.pacienteCpf || '');
        this.pacienteEndereco.set(d.pacienteEndereco || '');
        this.pacienteCidade.set(d.pacienteCidade || ''); this.pacienteUf.set(d.pacienteUf || '');
        this.numeroReceita.set(d.numeroReceita || '');
        this.dataEmissao.set(d.dataEmissao.substring(0, 10));
        this.tipoReceita.set(d.tipoReceita || 'Amarela');
        this.observacao.set(d.observacao || '');
        this.itens.set(d.itens || []);
        this.modalAberto.set(true);
      }
    });
  }
  fecharModal() { this.modalAberto.set(false); }

  onProdutoInput(v: string) {
    this.produtoBusca.set(v);
    if (v.length < 2) { this.produtoResultados.set([]); return; }
    this.http.get<any>(`${this.produtosApi}/pesquisar?termo=${encodeURIComponent(v)}`).subscribe({
      next: r => this.produtoResultados.set((r.data ?? []).slice(0, 15))
    });
  }
  selecionarProduto(p: any) {
    this.itens.update(a => [...a, { produtoId: p.id, produtoNome: p.nome, quantidade: 1, posologia: null }]);
    this.produtoBusca.set(''); this.produtoResultados.set([]);
  }
  removerItem(idx: number) { this.itens.update(a => a.filter((_, i) => i !== idx)); }
  atualizarItem(idx: number, campo: keyof ReceitaItem, valor: any) {
    this.itens.update(a => { const c = [...a]; c[idx] = { ...c[idx], [campo]: valor }; return c; });
  }

  salvar() {
    if (!this.medicoNome().trim()) { this.modal.aviso('Médico', 'Nome do médico obrigatório.'); return; }
    if (!this.pacienteNome().trim()) { this.modal.aviso('Paciente', 'Nome do paciente obrigatório.'); return; }
    if (this.itens().length === 0) { this.modal.aviso('Itens', 'Adicione ao menos um produto.'); return; }

    const body = {
      filialId: this.filialId(),
      medicoNome: this.medicoNome(), medicoCrm: this.medicoCrm() || null,
      medicoUf: this.medicoUf() || null, medicoCpf: this.medicoCpf() || null,
      pacienteNome: this.pacienteNome(), pacienteCpf: this.pacienteCpf() || null,
      pacienteEndereco: this.pacienteEndereco() || null,
      pacienteCidade: this.pacienteCidade() || null, pacienteUf: this.pacienteUf() || null,
      numeroReceita: this.numeroReceita() || null,
      dataEmissao: this.dataEmissao(),
      tipoReceita: this.tipoReceita(),
      observacao: this.observacao() || null,
      itens: this.itens()
    };
    const req = this.editId()
      ? this.http.put<any>(`${this.api}/${this.editId()}`, body)
      : this.http.post<any>(this.api, body);
    req.subscribe({
      next: () => { this.modalAberto.set(false); this.carregar(); },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao salvar receita.')
    });
  }

  async excluir(r: Receita) {
    const c = await this.modal.confirmar('Excluir receita', `Excluir a receita #${r.id}?`, 'Sim', 'Não');
    if (!c.confirmado) return;
    this.http.delete(`${this.api}/${r.id}`).subscribe({
      next: () => this.carregar(),
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao excluir.')
    });
  }
}
