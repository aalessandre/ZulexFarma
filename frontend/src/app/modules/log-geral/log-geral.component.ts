import { Component, signal, computed, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface LogItem {
  id: number;
  realizadoEm: string;
  nomeUsuario: string;
  loginUsuario: string;
  tela: string;
  acao: string;
  entidade: string | null;
  registroId: string | null;
  valoresAnteriores: string | null;
  valoresNovos: string | null;
  liberadoPor: string | null;
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

const LOG_COLUNAS: ColunaDef[] = [
  { campo: 'realizadoEm', label: 'Data/Hora',    largura: 170, minLargura: 130, padrao: true },
  { campo: 'nomeUsuario', label: 'Usuário',      largura: 180, minLargura: 100, padrao: true },
  { campo: 'tela',        label: 'Tela',         largura: 140, minLargura: 80,  padrao: true },
  { campo: 'acao',        label: 'Ação',         largura: 200, minLargura: 100, padrao: true },
  { campo: 'entidade',    label: 'Entidade',     largura: 130, minLargura: 80,  padrao: true },
  { campo: 'registroId',  label: 'Registro',     largura: 90,  minLargura: 60,  padrao: true },
  { campo: 'liberadoPor', label: 'Liberado Por', largura: 180, minLargura: 100, padrao: false },
];

@Component({
  selector: 'app-log-geral',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './log-geral.component.html',
  styleUrl: './log-geral.component.scss'
})
export class LogGeralComponent implements OnInit {
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_log_geral';

  logs = signal<LogItem[]>([]);
  carregando = signal(false);
  total = signal(0);
  pagina = signal(1);
  tamanhoPagina = signal(50);
  expandidoId = signal<number | null>(null);

  // Filtros
  dataInicio = signal(this.calcularData(-7));
  dataFim = signal(this.calcularData(0));
  filtroTela = signal('');
  filtroAcao = signal('');
  filtroUsuario = signal('');
  filtroLiberacao = signal<string>('');

  // Ordenação
  sortColuna = signal<string>('realizadoEm');
  sortDirecao = signal<'asc' | 'desc'>('desc');

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);

  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;

  totalPaginas = computed(() => Math.ceil(this.total() / this.tamanhoPagina()) || 1);

  // Ordenação client-side
  logsFiltrados = computed(() => {
    const lista = this.logs();
    const col = this.sortColuna();
    const dir = this.sortDirecao();
    if (!col) return lista;

    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private apiUrl = `${environment.apiUrl}/logs`;

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() {
    this.filtrar();
  }

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }

  filtrar() {
    this.pagina.set(1);
    this.carregar();
  }

  carregar() {
    this.carregando.set(true);
    this.expandidoId.set(null);
    let params = new HttpParams()
      .set('dataInicio', this.dataInicio())
      .set('dataFim', this.dataFim())
      .set('pagina', this.pagina().toString())
      .set('tamanhoPagina', this.tamanhoPagina().toString());

    if (this.filtroTela()) params = params.set('tela', this.filtroTela());
    if (this.filtroAcao()) params = params.set('acao', this.filtroAcao());
    if (this.filtroUsuario()) params = params.set('usuario', this.filtroUsuario());
    if (this.filtroLiberacao() === 'sim') params = params.set('liberacaoPorSenha', 'true');
    if (this.filtroLiberacao() === 'nao') params = params.set('liberacaoPorSenha', 'false');

    this.http.get<any>(this.apiUrl, { params }).subscribe({
      next: r => {
        this.logs.set(r.data ?? []);
        this.total.set(r.total ?? 0);
        this.carregando.set(false);
      },
      error: (e: any) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('log-geral', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  irPagina(p: number) {
    if (p < 1 || p > this.totalPaginas()) return;
    this.pagina.set(p);
    this.carregar();
  }

  toggleExpandir(id: number) {
    this.expandidoId.update(v => v === id ? null : id);
  }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  getCellValue(log: LogItem, campo: string): string {
    const v = (log as any)[campo];
    if (v === null || v === undefined || v === '') return '\u2014';
    return String(v);
  }

  acaoCss(acao: string): string {
    if (acao === 'CRIAÇÃO')     return 'badge-criacao';
    if (acao === 'ALTERAÇÃO')   return 'badge-alteracao';
    if (acao === 'EXCLUSÃO')    return 'badge-exclusao';
    if (acao === 'DESATIVAÇÃO') return 'badge-desativacao';
    if (acao === 'LIBERAÇÃO POR SENHA') return 'badge-liberacao';
    // fallback sem acento
    if (acao === 'CRIACAO')     return 'badge-criacao';
    if (acao === 'ALTERACAO')   return 'badge-alteracao';
    if (acao === 'EXCLUSAO')    return 'badge-exclusao';
    if (acao === 'DESATIVACAO') return 'badge-desativacao';
    return '';
  }

  // ── Colunas: resize ───────────────────────────────────────────────

  iniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation();
    e.preventDefault();
    this.resizeState = { campo, startX: e.clientX, startWidth: largura };
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    if (!this.resizeState) return;
    const delta = e.clientX - this.resizeState.startX;
    const def = LOG_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── Colunas: visibilidade ─────────────────────────────────────────

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols =>
      cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c)
    );
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(LOG_COLUNAS.map(d => ({ ...d, visivel: d.padrao })));
    localStorage.removeItem(this.STORAGE_KEY_COLUNAS);
  }

  private carregarColunas(): ColunaEstado[] {
    let salvo: { campo: string; visivel: boolean; largura: number }[] = [];
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) salvo = JSON.parse(json);
    } catch {}

    return LOG_COLUNAS.map(def => {
      const s = salvo.find(x => x.campo === def.campo);
      return {
        ...def,
        visivel: s ? s.visivel : def.padrao,
        largura: s?.largura ?? def.largura
      };
    });
  }

  private salvarColunasStorage() {
    const estado = this.colunas().map(c => ({
      campo: c.campo, visivel: c.visivel, largura: Math.round(c.largura)
    }));
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(estado));
  }

  // ── Detalhe expandido ─────────────────────────────────────────────

  parseJson(val: string | null): Record<string, any> | null {
    if (!val) return null;
    try { return JSON.parse(val); } catch { return null; }
  }

  objectKeys(obj: Record<string, any> | null): string[] {
    return obj ? Object.keys(obj) : [];
  }

  formatarChave(key: string): string {
    return key.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase()).trim();
  }

  formatarValor(val: any): string {
    if (val === null || val === undefined) return '\u2014';
    if (typeof val === 'object') {
      return Object.entries(val).map(([k, v]) => `${this.formatarChave(k)}: ${v}`).join(' | ');
    }
    return String(val);
  }

  /** Verifica se um campo mudou entre valores anteriores e novos */
  campoAlterado(log: LogItem, key: string): boolean {
    const anterior = this.parseJson(log.valoresAnteriores);
    const novo = this.parseJson(log.valoresNovos);
    if (!anterior || !novo) return false;
    const va = anterior[key];
    const vn = novo[key];
    if (va === undefined && vn === undefined) return false;
    return String(va ?? '') !== String(vn ?? '');
  }

  private calcularData(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
