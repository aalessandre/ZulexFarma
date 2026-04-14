import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface LogCampo { campo: string; valorAnterior?: string; valorAtual?: string; }
interface LogEntry { id: number; realizadoEm: string; acao: string; nomeUsuario: string; campos: LogCampo[]; }

interface Tarifa {
  id?: number;
  modalidade: string;
  tarifaPercentual: string;
  prazoRecebimentoDias: string;
  contaBancariaId: string;
}

interface Bandeira {
  id?: number;
  nome: string;
  tarifas: Tarifa[];
  expandida: boolean;
}

interface Adquirente {
  id?: number;
  nome: string;
  ativo: boolean;
  criadoEm?: string;
  bandeiras: Bandeira[];
}

interface AbaEdicao {
  adquirente: Adquirente;
  form: Adquirente;
  isDirty: boolean;
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

type Modo = 'lista' | 'form';

const ADQUIRENTES_COLUNAS: ColunaDef[] = [
  { campo: 'nome', label: 'Nome', largura: 220, minLargura: 120, padrao: true },
  { campo: 'totalBandeiras', label: 'Total Bandeiras', largura: 130, minLargura: 80, padrao: true },
  { campo: 'ativo', label: 'Ativo', largura: 70, minLargura: 50, padrao: true },
  { campo: 'criadoEm', label: 'Data Cadastro', largura: 150, minLargura: 100, padrao: true },
];

const MODALIDADES: { label: string; valor: number }[] = [
  { label: 'Debito', valor: 1 },
  { label: 'Credito', valor: 2 },
  { label: 'Parcelado 2x', valor: 3 },
  { label: 'Parcelado 3x', valor: 4 },
  { label: 'Parcelado 4x', valor: 5 },
  { label: 'Parcelado 5x', valor: 6 },
  { label: 'Parcelado 6x', valor: 7 },
  { label: 'Parcelado 7x', valor: 8 },
  { label: 'Parcelado 8x', valor: 9 },
  { label: 'Parcelado 9x', valor: 10 },
  { label: 'Parcelado 10x', valor: 11 },
  { label: 'Parcelado 11x', valor: 12 },
  { label: 'Parcelado 12x', valor: 13 },
];

const BANDEIRAS_SUGESTOES = ['Visa', 'Mastercard', 'Elo', 'Amex', 'Hipercard', 'Diners'];

@Component({
  selector: 'app-adquirentes',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './adquirentes.component.html',
  styleUrl: './adquirentes.component.scss'
})
export class AdquirentesComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_adquirentes_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_adquirentes';

  readonly modalidades = MODALIDADES;
  readonly bandeirasSugestoes = BANDEIRAS_SUGESTOES;

  modo = signal<Modo>('lista');
  adquirentes = signal<Adquirente[]>([]);
  adquirenteSelecionado = signal<Adquirente | null>(null);
  adquirenteForm = signal<Adquirente>(this.novoAdquirente());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('nome');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  abasEdicao = signal<AbaEdicao[]>([]);
  abaAtivaId = signal<number | null>(null);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});
  private formOriginal: Adquirente | null = null;

  // Nova bandeira
  novaBandeiraNome = signal('');
  mostrarSugestoes = signal(false);

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  // Log
  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal('');
  logDataFim = signal('');
  carregandoLog = signal(false);

  // Contas bancárias lookup
  contasBancarias = signal<{ id: number; nome: string }[]>([]);

  private apiUrl = `${environment.apiUrl}/adquirentes`;
  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('adquirentes', acao)) return true;
    const resultado = await this.modal.permissao('adquirentes', acao);
    if (resultado.tokenLiberacao) this.tokenLiberacao = resultado.tokenLiberacao;
    return resultado.confirmado;
  }

  private headerLiberacao(): { [h: string]: string } {
    if (this.tokenLiberacao) {
      const h = { 'X-Liberacao': this.tokenLiberacao };
      this.tokenLiberacao = null;
      return h;
    }
    return {};
  }

  ngOnInit() { this.carregar(); this.carregarContasBancarias(); }
  ngOnDestroy() { sessionStorage.removeItem(this.STATE_KEY); }

  sairDaTela() {
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  // ── State persistence ─────────────────────────────────────────────
  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abasIds: abas.map(a => a.adquirente.id),
      abaAtivaId: this.abaAtivaId()
    }));
  }

  private restaurarEstado() {
    try {
      const json = sessionStorage.getItem(this.STATE_KEY);
      if (!json) return;
      const state = JSON.parse(json);
      sessionStorage.removeItem(this.STATE_KEY);
      if (state.abasIds?.length > 0) {
        for (const id of state.abasIds) {
          const a = this.adquirentes().find(x => x.id === id);
          if (a) this.restaurarAba(a, id === state.abaAtivaId);
        }
      }
    } catch {}
  }

  private restaurarAba(a: Adquirente, ativar: boolean) {
    if (this.abasEdicao().find(ab => ab.adquirente.id === a.id)) return;
    const aba: AbaEdicao = { adquirente: { ...a }, form: this.clonar(a), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    if (ativar) this.ativarAba(a.id!);
  }

  // ── Data ───────────────────────────────────────────────────────────
  private primeiroCarregamento = true;

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        const dados = (r.data ?? []).map((a: any) => ({
          ...a,
          bandeiras: (a.bandeiras ?? []).map((b: any) => ({ ...b, nome: b.bandeira ?? b.nome ?? '', expandida: false }))
        }));
        this.adquirentes.set(dados);
        this.carregando.set(false);
        if (this.primeiroCarregamento) {
          this.primeiroCarregamento = false;
          this.restaurarEstado();
        }
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('adquirentes', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  private carregarContasBancarias() {
    this.http.get<any>(`${environment.apiUrl}/contasbancarias`).subscribe({
      next: r => this.contasBancarias.set((r.data ?? []).filter((c: any) => c.ativo).map((c: any) => ({ id: c.id, nome: c.nome || c.descricao || `Conta #${c.id}` }))),
      error: () => {}
    });
  }

  adquirentesFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.adquirentes().filter(a => {
      if (status === 'ativos'   && !a.ativo) return false;
      if (status === 'inativos' &&  a.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(a.nome).includes(termo);
    });

    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = this.getSortValue(a, col);
      const vb = this.getSortValue(b, col);
      const cmp = typeof va === 'boolean'
        ? (va === vb ? 0 : va ? -1 : 1)
        : typeof va === 'number'
          ? va - (vb as number)
          : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private getSortValue(a: Adquirente, campo: string): any {
    if (campo === 'totalBandeiras') return a.bandeiras?.length ?? 0;
    return (a as any)[campo] ?? '';
  }

  private normalizar(s: string): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(a: Adquirente, campo: string): string {
    if (campo === 'totalBandeiras') return String(a.bandeiras?.length ?? 0);
    if (campo === 'criadoEm') {
      if (!a.criadoEm) return '';
      try {
        const d = new Date(a.criadoEm);
        return d.toLocaleDateString('pt-BR') + ' ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
      } catch { return a.criadoEm; }
    }
    const v = (a as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Nao';
    return v ?? '';
  }

  selecionar(a: Adquirente) { this.adquirenteSelecionado.set(a); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '\u25B2' : '\u25BC') : '\u21C5'; }

  // ── Columns ────────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return ADQUIRENTES_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return ADQUIRENTES_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(ADQUIRENTES_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
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
    const def = ADQUIRENTES_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── CRUD ───────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.adquirenteForm.set(this.novoAdquirente());
    this.formOriginal = this.clonar(this.novoAdquirente());
    this.modoEdicao.set(false);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    this.novaBandeiraNome.set('');
    this.mostrarSugestoes.set(false);
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const a = this.adquirenteSelecionado();
    if (!a?.id) return;

    const jaAberta = this.abasEdicao().find(ab => ab.adquirente.id === a.id);
    if (jaAberta) {
      this.ativarAba(a.id);
      return;
    }

    // Fetch detail to get full bandeiras/tarifas
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${a.id}`).subscribe({
      next: r => {
        this.carregando.set(false);
        const detail: Adquirente = {
          ...r.data,
          bandeiras: (r.data.bandeiras ?? []).map((b: any) => ({
            id: b.id,
            nome: b.bandeira ?? b.nome ?? '',
            expandida: false,
            tarifas: (b.tarifas ?? []).map((t: any) => ({
              id: t.id,
              modalidade: String(t.modalidade ?? 1),
              tarifaPercentual: String(t.tarifa ?? t.tarifaPercentual ?? ''),
              prazoRecebimentoDias: String(t.prazoRecebimento ?? t.prazoRecebimentoDias ?? ''),
              contaBancariaId: t.contaBancariaId ? String(t.contaBancariaId) : ''
            }))
          }))
        };
        const aba: AbaEdicao = { adquirente: { ...detail }, form: this.clonar(detail), isDirty: false };
        this.abasEdicao.update(abas => [...abas, aba]);
        this.abaAtivaId.set(detail.id!);
        this.adquirenteForm.set(this.clonar(detail));
        this.formOriginal = this.clonar(detail);
        this.modoEdicao.set(true);
        this.isDirty.set(false);
        this.erro.set('');
        this.errosCampos.set({});
        this.novaBandeiraNome.set('');
        this.mostrarSugestoes.set(false);
        this.modo.set('form');
      },
      error: () => {
        this.carregando.set(false);
        this.modal.erro('Erro', 'Erro ao carregar detalhes do adquirente.');
      }
    });
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    const aba = this.abasEdicao().find(a => a.adquirente.id === id);
    if (!aba) return;
    this.abaAtivaId.set(id);
    this.adquirenteForm.set(this.clonar(aba.form));
    this.formOriginal = this.clonar(aba.form);
    this.isDirty.set(aba.isDirty);
    this.modoEdicao.set(true);
    this.erro.set('');
    this.errosCampos.set({});
    this.novaBandeiraNome.set('');
    this.mostrarSugestoes.set(false);
    this.modo.set('form');
  }

  fecharAba(id: number) {
    this.abasEdicao.update(abas => abas.filter(a => a.adquirente.id !== id));
    if (this.abaAtivaId() === id) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) {
        this.ativarAba(restantes[restantes.length - 1].adquirente.id!);
      } else {
        this.modo.set('lista');
        this.abaAtivaId.set(null);
      }
    }
  }

  fechar() {
    this.modo.set('lista');
    this.carregar();
  }

  fecharForm() {
    if (this.modoEdicao()) {
      const id = this.abaAtivaId();
      if (id) this.fecharAba(id);
      else this.modo.set('lista');
    } else {
      this.modo.set('lista');
    }
  }

  cancelarEdicao() {
    if (this.formOriginal) {
      this.adquirenteForm.set(this.clonar(this.formOriginal));
      this.isDirty.set(false);
      const id = this.abaAtivaId();
      if (id) {
        this.abasEdicao.update(abas =>
          abas.map(a => a.adquirente.id === id ? { ...a, isDirty: false } : a)
        );
      }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (!id || this.modo() !== 'form') return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.adquirente.id === id
        ? { ...a, form: this.clonar(this.adquirenteForm()), isDirty: this.isDirty() }
        : a
      )
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const erros: Record<string, string> = {};
    const a = this.adquirenteForm();
    if (!a.nome.trim()) erros['nome'] = 'Nome e obrigatorio.';
    if (Object.keys(erros).length) {
      this.errosCampos.set(erros);
      return;
    }
    this.errosCampos.set({});
    this.salvando.set(true);

    const headers = this.headerLiberacao();
    const body: any = {
      nome: a.nome,
      ativo: a.ativo,
      bandeiras: a.bandeiras.map(b => ({
        id: b.id,
        bandeira: b.nome,
        tarifas: b.tarifas.map(t => ({
          id: t.id,
          modalidade: +t.modalidade || 1,
          tarifa: t.tarifaPercentual ? parseFloat(String(t.tarifaPercentual).replace(',', '.')) : 0,
          prazoRecebimento: t.prazoRecebimentoDias ? parseInt(String(t.prazoRecebimentoDias), 10) : 0,
          contaBancariaId: t.contaBancariaId ? parseInt(t.contaBancariaId, 10) : null,
        }))
      }))
    };

    const salvarDados$ = this.modoEdicao()
      ? this.http.put(`${this.apiUrl}/${a.id}`, body, { headers })
      : this.http.post<any>(this.apiUrl, body, { headers });

    salvarDados$.subscribe({
      next: (r: any) => {
        const adquirenteId = this.modoEdicao() ? a.id! : r.data?.id;
        this.finalizarSalvar(adquirenteId);
      },
      error: (err: any) => {
        this.salvando.set(false);
        const msg = err?.error?.message || 'Erro ao salvar adquirente.';
        this.modal.erro('Erro ao Salvar', msg);
      }
    });
  }

  private finalizarSalvar(adquirenteId: number) {
    this.salvando.set(false);
    this.isDirty.set(false);
    if (this.modoEdicao()) {
      this.fecharAba(adquirenteId);
    }
    this.carregar();
    this.modo.set('lista');
  }

  async excluir() {
    const a = this.adquirenteSelecionado();
    if (!a?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusao',
      `Deseja excluir o adquirente ${a.nome}? O registro sera removido permanentemente. Se estiver em uso, sera apenas desativado.`,
      'Sim, excluir',
      'Nao, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${a.id}`, { headers }).subscribe({
      next: async (r: any) => {
        this.excluindo.set(false);
        this.adquirenteSelecionado.set(null);
        this.fecharAba(a.id!);
        this.carregar();
        if (r.resultado === 'desativado') {
          await this.modal.aviso('Desativado', 'O registro esta em uso e foi apenas desativado.');
        }
      },
      error: () => {
        this.excluindo.set(false);
        this.modal.erro('Erro', 'Erro ao excluir adquirente.');
      }
    });
  }

  // ── Form helpers ───────────────────────────────────────────────────
  upd(campo: keyof Adquirente, v: any) {
    this.adquirenteForm.update(a => ({ ...a, [campo]: v }));
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  erroCampo(campo: string): string {
    return this.errosCampos()[campo] ?? '';
  }

  private atualizarDirtyAba() {
    const id = this.abaAtivaId();
    if (!id) return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.adquirente.id === id ? { ...a, isDirty: true } : a)
    );
  }

  // ── Bandeiras ──────────────────────────────────────────────────────
  toggleBandeira(idx: number) {
    this.adquirenteForm.update(a => {
      const bandeiras = [...a.bandeiras];
      bandeiras[idx] = { ...bandeiras[idx], expandida: !bandeiras[idx].expandida };
      return { ...a, bandeiras };
    });
  }

  adicionarBandeira() {
    const nome = this.novaBandeiraNome().trim();
    if (!nome) return;
    const novaBandeira: Bandeira = { nome, tarifas: [], expandida: true };
    this.adquirenteForm.update(a => ({ ...a, bandeiras: [...a.bandeiras, novaBandeira] }));
    this.novaBandeiraNome.set('');
    this.mostrarSugestoes.set(false);
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  adicionarBandeiraSugestao(nome: string) {
    const jaExiste = this.adquirenteForm().bandeiras.some(b => this.normalizar(b.nome) === this.normalizar(nome));
    if (jaExiste) return;
    const novaBandeira: Bandeira = { nome, tarifas: [], expandida: true };
    this.adquirenteForm.update(a => ({ ...a, bandeiras: [...a.bandeiras, novaBandeira] }));
    this.mostrarSugestoes.set(false);
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  removerBandeira(idx: number) {
    this.adquirenteForm.update(a => {
      const bandeiras = [...a.bandeiras];
      bandeiras.splice(idx, 1);
      return { ...a, bandeiras };
    });
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  sugestoesDisponiveis(): string[] {
    const existentes = this.adquirenteForm().bandeiras.map(b => this.normalizar(b.nome));
    return BANDEIRAS_SUGESTOES.filter(s => !existentes.includes(this.normalizar(s)));
  }

  // ── Tarifas ────────────────────────────────────────────────────────
  adicionarTarifa(bandeiraIdx: number) {
    const novaTarifa: Tarifa = { modalidade: '1', tarifaPercentual: '', prazoRecebimentoDias: '', contaBancariaId: '' };
    this.adquirenteForm.update(a => {
      const bandeiras = [...a.bandeiras];
      bandeiras[bandeiraIdx] = {
        ...bandeiras[bandeiraIdx],
        tarifas: [...bandeiras[bandeiraIdx].tarifas, novaTarifa]
      };
      return { ...a, bandeiras };
    });
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  removerTarifa(bandeiraIdx: number, tarifaIdx: number) {
    this.adquirenteForm.update(a => {
      const bandeiras = [...a.bandeiras];
      const tarifas = [...bandeiras[bandeiraIdx].tarifas];
      tarifas.splice(tarifaIdx, 1);
      bandeiras[bandeiraIdx] = { ...bandeiras[bandeiraIdx], tarifas };
      return { ...a, bandeiras };
    });
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  updTarifa(bandeiraIdx: number, tarifaIdx: number, campo: keyof Tarifa, valor: any) {
    this.adquirenteForm.update(a => {
      const bandeiras = [...a.bandeiras];
      const tarifas = [...bandeiras[bandeiraIdx].tarifas];
      tarifas[tarifaIdx] = { ...tarifas[tarifaIdx], [campo]: valor };
      bandeiras[bandeiraIdx] = { ...bandeiras[bandeiraIdx], tarifas };
      return { ...a, bandeiras };
    });
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  // ── Log ────────────────────────────────────────────────────────────
  abrirLog() {
    const a = this.adquirenteSelecionado();
    if (!a?.id) return;
    this.modalLog.set(true);
    this.logRegistros.set([]);
    this.logSelecionado.set(null);
    this.filtrarLog();
  }

  fecharLog() { this.modalLog.set(false); }

  filtrarLog() {
    const a = this.adquirenteSelecionado();
    if (!a?.id) return;
    this.carregandoLog.set(true);
    let url = `${this.apiUrl}/${a.id}/log`;
    const params: string[] = [];
    if (this.logDataInicio()) params.push(`dataInicio=${this.logDataInicio()}`);
    if (this.logDataFim()) params.push(`dataFim=${this.logDataFim()}`);
    if (params.length) url += '?' + params.join('&');

    this.http.get<any>(url).subscribe({
      next: r => {
        this.logRegistros.set(r.data ?? []);
        this.carregandoLog.set(false);
        if (r.data?.length > 0) this.selecionarLogEntry(r.data[0]);
      },
      error: () => this.carregandoLog.set(false)
    });
  }

  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }

  acaoCss(acao: string): string {
    const map: Record<string, string> = {
      'CRIACAO': 'log-badge badge-criacao',
      'ALTERACAO': 'log-badge badge-alteracao',
      'EXCLUSAO': 'log-badge badge-exclusao',
      'DESATIVACAO': 'log-badge badge-desativacao'
    };
    return map[acao] ?? 'log-badge';
  }

  // ── Utils ──────────────────────────────────────────────────────────
  private novoAdquirente(): Adquirente {
    return { nome: '', ativo: true, bandeiras: [] };
  }

  private clonar<T>(obj: T): T {
    return JSON.parse(JSON.stringify(obj));
  }
}
