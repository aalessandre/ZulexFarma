import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface Mapa {
  id: number;
  filialId: number;
  competenciaMes: number;
  competenciaAno: number;
  status: number;
  statusNome: string;
  dataGeracao?: string;
  dataEnvio?: string;
  protocoloAnvisa?: string;
  totalEntradas: number;
  totalSaidas: number;
  totalReceitas: number;
  totalPerdas: number;
}

interface ColunaDef { campo: string; label: string; largura: number; minLargura: number; padrao: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

const COLUNAS: ColunaDef[] = [
  { campo: 'competencia',     label: 'Competência', largura: 140, minLargura: 100, padrao: true },
  { campo: 'statusNome',      label: 'Status',      largura: 120, minLargura: 90,  padrao: true },
  { campo: 'totalEntradas',   label: 'Entradas',    largura: 100, minLargura: 70,  padrao: true },
  { campo: 'totalSaidas',     label: 'Saídas',      largura: 100, minLargura: 70,  padrao: true },
  { campo: 'totalReceitas',   label: 'Receitas',    largura: 100, minLargura: 70,  padrao: true },
  { campo: 'totalPerdas',     label: 'Perdas',      largura: 100, minLargura: 70,  padrao: true },
  { campo: 'dataGeracao',     label: 'Gerado em',   largura: 150, minLargura: 120, padrao: true },
  { campo: 'dataEnvio',       label: 'Enviado em',  largura: 150, minLargura: 120, padrao: true },
  { campo: 'protocoloAnvisa', label: 'Protocolo',   largura: 160, minLargura: 100, padrao: false },
];

const MESES = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];

@Component({
  selector: 'app-sngpc-mapas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sngpc-mapas.component.html',
  styleUrl: './sngpc-mapas.component.scss'
})
export class SngpcMapasComponent implements OnInit, OnDestroy {
  private api = `${environment.apiUrl}/sngpc/mapas`;
  private filiaisApi = `${environment.apiUrl}/filiais`;
  private readonly STORAGE_KEY = 'zulex_colunas_sngpc_mapas';

  mapas = signal<Mapa[]>([]);
  carregando = signal(false);
  anoFiltro = signal(new Date().getFullYear());

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  sortColuna = signal<string>('competencia');
  sortDirecao = signal<'asc' | 'desc'>('desc');
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  mapasOrdenados = computed(() => {
    const col = this.sortColuna(); const dir = this.sortDirecao();
    const lista = [...this.mapas()];
    if (!col) return lista;
    return lista.sort((a, b) => {
      let va: any; let vb: any;
      if (col === 'competencia') {
        va = a.competenciaAno * 100 + a.competenciaMes;
        vb = b.competenciaAno * 100 + b.competenciaMes;
      } else {
        va = (a as any)[col] ?? ''; vb = (b as any)[col] ?? '';
      }
      const cmp = typeof va === 'number' ? va - (vb as number)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  // Modal gerar
  modalAberto = signal(false);
  filiais = signal<any[]>([]);
  filialId = signal<number>(1);
  mes = signal(new Date().getMonth() + 1);
  ano = signal(new Date().getFullYear());

  readonly meses = MESES;
  readonly anosDisponiveis = this.gerarAnos();

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() { this.carregarFiliais(); this.carregar(); }
  ngOnDestroy() {}

  gerarAnos(): number[] {
    const atual = new Date().getFullYear();
    return [atual - 2, atual - 1, atual, atual + 1];
  }
  mesNome(m: number): string { return MESES[m - 1] || ''; }

  carregarFiliais() {
    this.http.get<any>(this.filiaisApi).subscribe({
      next: r => {
        const lista = r.data ?? [];
        this.filiais.set(lista);
        if (lista.length > 0) this.filialId.set(lista[0].id);
      }
    });
  }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.api}?ano=${this.anoFiltro()}`).subscribe({
      next: r => { this.mapas.set(r.data ?? []); this.carregando.set(false); },
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

  getCellValue(m: Mapa, campo: string): string {
    switch (campo) {
      case 'competencia': return `${this.mesNome(m.competenciaMes)}/${m.competenciaAno}`;
      case 'dataGeracao': return m.dataGeracao ? new Date(m.dataGeracao).toLocaleString('pt-BR') : '—';
      case 'dataEnvio':   return m.dataEnvio ? new Date(m.dataEnvio).toLocaleString('pt-BR') : '—';
      default:            return (m as any)[campo]?.toString() ?? '—';
    }
  }

  abrirGerar() {
    this.mes.set(new Date().getMonth() + 1);
    this.ano.set(new Date().getFullYear());
    this.modalAberto.set(true);
  }
  fecharModal() { this.modalAberto.set(false); }

  gerar() {
    this.http.post<any>(`${this.api}/gerar`, {
      filialId: this.filialId(), mes: this.mes(), ano: this.ano()
    }).subscribe({
      next: () => { this.modalAberto.set(false); this.carregar(); this.modal.sucesso('OK', 'Mapa gerado.'); },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao gerar mapa.')
    });
  }

  baixar(m: Mapa) { window.open(`${this.api}/${m.id}/xml`, '_blank'); }

  async enviar(m: Mapa) {
    const r = await this.modal.confirmar('Enviar à Anvisa',
      `Marcar o mapa ${m.competenciaMes}/${m.competenciaAno} como enviado?\n\n⚠️ Atenção: o envio real ao webservice Anvisa ainda não foi implementado — esta ação só marca o status no sistema.`,
      'Sim, marcar enviado', 'Cancelar');
    if (!r.confirmado) return;
    const protocolo = prompt('Cole o protocolo Anvisa (opcional):');
    this.http.post<any>(`${this.api}/${m.id}/enviar`, { protocolo }).subscribe({
      next: () => this.carregar(),
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao marcar enviado.')
    });
  }

  async excluir(m: Mapa) {
    const r = await this.modal.confirmar('Excluir mapa', `Excluir mapa ${m.competenciaMes}/${m.competenciaAno}?`, 'Sim', 'Não');
    if (!r.confirmado) return;
    this.http.delete(`${this.api}/${m.id}`).subscribe({
      next: () => this.carregar(),
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao excluir.')
    });
  }
}
