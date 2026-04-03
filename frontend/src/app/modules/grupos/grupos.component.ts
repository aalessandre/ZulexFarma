import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { GRUPOS_COLUNAS, ColunaDef } from './grupos.columns';
import { TELAS_SISTEMA, getBlocos } from './telas-sistema';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface Grupo {
  id?: number;
  nome: string;
  descricao?: string;
  totalUsuarios?: number;
  ativo: boolean;
  criadoEm?: string;
}

interface Permissao {
  bloco: number;
  codigoTela: string;
  nomeTela: string;
  podeConsultar: boolean;
  podeIncluir: boolean;
  podeAlterar: boolean;
  podeExcluir: boolean;
}

interface GrupoDetalhe extends Grupo {
  permissoes: Permissao[];
}

interface AbaEdicao {
  grupo: Grupo;
  form: GrupoDetalhe;
  isDirty: boolean;
}

interface ColunaEstado extends ColunaDef {
  visivel: boolean;
}

type Modo = 'lista' | 'form';

@Component({
  selector: 'app-grupos',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './grupos.component.html',
  styleUrl: './grupos.component.scss'
})
export class GruposComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_grupos_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_grupos';

  modo = signal<Modo>('lista');
  grupos = signal<Grupo[]>([]);
  grupoSelecionado = signal<Grupo | null>(null);
  grupoForm = signal<GrupoDetalhe>(this.novoGrupo());
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
  private formOriginal: GrupoDetalhe | null = null;

  // TreeView
  buscaPermissao = signal('');
  nosAbertos = signal<Set<string>>(new Set());


  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;

  // Permissoes
  blocos = getBlocos();
  // blocos expandidos por padrão na edição

  private apiUrl = `${environment.apiUrl}/grupos`;

  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('grupos', acao)) return true;
    const resultado = await this.modal.permissao('grupos', acao);
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

  ngOnInit() { this.carregar(); }
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
      abasIds: abas.map(a => a.grupo.id),
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
          const g = this.grupos().find(x => x.id === id);
          if (g) this.restaurarAba(g, id === state.abaAtivaId);
        }
      }
    } catch {}
  }

  private restaurarAba(g: Grupo, ativar: boolean) {
    if (this.abasEdicao().find(a => a.grupo.id === g.id)) return;
    const detalhe: GrupoDetalhe = { ...g, permissoes: this.criarPermissoesVazias() };
    const aba: AbaEdicao = { grupo: { ...g }, form: this.clonar(detalhe), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    this.carregarPermissoes(g.id!);
    if (ativar) this.ativarAba(g.id!);
  }

  // ── Data ───────────────────────────────────────────────────────────
  private primeiroCarregamento = true;

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.grupos.set(r.data ?? []);
        this.carregando.set(false);
        if (this.primeiroCarregamento) {
          this.primeiroCarregamento = false;
          this.restaurarEstado();
        }
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('grupos', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  gruposFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.grupos().filter(g => {
      if (status === 'ativos'   && !g.ativo) return false;
      if (status === 'inativos' &&  g.ativo) return false;
      if (termo.length < 2) return true;
      return (
        this.normalizar(g.nome).includes(termo) ||
        this.normalizar(g.descricao ?? '').includes(termo)
      );
    });

    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean'
        ? (va === vb ? 0 : va ? -1 : 1)
        : typeof va === 'number'
          ? va - (vb as number)
          : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(g: Grupo, campo: string): string {
    const v = (g as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'N\u00e3o';
    return v ?? '';
  }

  selecionar(g: Grupo) { this.grupoSelecionado.set(g); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  // ── Columns ────────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return GRUPOS_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return GRUPOS_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(GRUPOS_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
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
    const def = GRUPOS_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── CRUD ───────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.grupoForm.set(this.novoGrupo());
    this.formOriginal = this.clonar(this.novoGrupo());
    this.modoEdicao.set(false);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});

    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const g = this.grupoSelecionado();
    if (!g?.id) return;

    // Check if tab already open
    const jaAberta = this.abasEdicao().find(a => a.grupo.id === g.id);
    if (jaAberta) {
      this.ativarAba(g.id);
      return;
    }

    const detalhe: GrupoDetalhe = { ...g, permissoes: this.criarPermissoesVazias() };
    const aba: AbaEdicao = { grupo: { ...g }, form: this.clonar(detalhe), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    this.abaAtivaId.set(g.id!);
    this.grupoForm.set(this.clonar(detalhe));
    this.formOriginal = this.clonar(detalhe);
    this.modoEdicao.set(true);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});

    this.modo.set('form');

    // Load permissions from API
    this.carregarPermissoes(g.id!);
  }

  private carregarPermissoes(grupoId: number) {
    this.http.get<any>(`${this.apiUrl}/${grupoId}/permissoes`).subscribe({
      next: r => {
        const permsApi: Permissao[] = r.data ?? [];
        const merged = this.mergePermissoes(permsApi);
        this.grupoForm.update(f => ({ ...f, permissoes: merged }));
        this.formOriginal = this.clonar(this.grupoForm());
        // Update tab
        this.abasEdicao.update(abas =>
          abas.map(a => a.grupo.id === grupoId ? { ...a, form: this.clonar(this.grupoForm()) } : a)
        );
      },
      error: () => { /* silently ignore - permissions will remain empty defaults */ }
    });
  }

  private mergePermissoes(permsApi: Permissao[]): Permissao[] {
    return TELAS_SISTEMA.map(t => {
      const existing = permsApi.find(p => p.codigoTela === t.codigo && p.bloco === t.bloco);
      return existing
        ? { ...existing }
        : { bloco: t.bloco, codigoTela: t.codigo, nomeTela: t.nome, podeConsultar: false, podeIncluir: false, podeAlterar: false, podeExcluir: false };
    });
  }

  private criarPermissoesVazias(): Permissao[] {
    return TELAS_SISTEMA.map(t => ({
      bloco: t.bloco,
      codigoTela: t.codigo,
      nomeTela: t.nome,
      podeConsultar: false,
      podeIncluir: false,
      podeAlterar: false,
      podeExcluir: false
    }));
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    const aba = this.abasEdicao().find(a => a.grupo.id === id);
    if (!aba) return;
    this.abaAtivaId.set(id);
    this.grupoForm.set(this.clonar(aba.form));
    this.formOriginal = this.clonar(aba.form);
    this.isDirty.set(aba.isDirty);
    this.modoEdicao.set(true);
    this.erro.set('');
    this.errosCampos.set({});

    this.modo.set('form');
  }

  fecharAba(id: number) {
    this.abasEdicao.update(abas => abas.filter(a => a.grupo.id !== id));
    if (this.abaAtivaId() === id) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) {
        this.ativarAba(restantes[restantes.length - 1].grupo.id!);
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

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (!id || this.modo() !== 'form') return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.grupo.id === id
        ? { ...a, form: this.clonar(this.grupoForm()), isDirty: this.isDirty() }
        : a
      )
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    // Validate
    const erros: Record<string, string> = {};
    const f = this.grupoForm();
    if (!f.nome.trim()) erros['nome'] = 'Nome \u00e9 obrigat\u00f3rio.';
    if (Object.keys(erros).length) {
      this.errosCampos.set(erros);
  
      return;
    }
    this.errosCampos.set({});
    this.salvando.set(true);

    const headers = this.headerLiberacao();
    const body: any = { nome: f.nome, descricao: f.descricao, ativo: f.ativo };

    const salvarDados$ = this.modoEdicao()
      ? this.http.put(`${this.apiUrl}/${f.id}`, body, { headers })
      : this.http.post<any>(this.apiUrl, body, { headers });

    salvarDados$.subscribe({
      next: (r: any) => {
        const grupoId = this.modoEdicao() ? f.id! : r.data?.id;

        // Save permissions if we have a grupoId
        if (grupoId) {
          const permissoes = f.permissoes.map(p => ({
            bloco: p.bloco,
            codigoTela: p.codigoTela,
            nomeTela: p.nomeTela,
            podeConsultar: p.podeConsultar,
            podeIncluir: p.podeIncluir,
            podeAlterar: p.podeAlterar,
            podeExcluir: p.podeExcluir
          }));

          this.http.put(`${this.apiUrl}/${grupoId}/permissoes`, { permissoes }).subscribe({
            next: () => {
              this.finalizarSalvar(grupoId);
            },
            error: () => {
              this.erro.set('Grupo salvo, mas houve erro ao salvar permiss\u00f5es.');
              this.salvando.set(false);
            }
          });
        } else {
          this.finalizarSalvar(grupoId);
        }
      },
      error: () => {
        this.erro.set('Erro ao salvar grupo.');
        this.salvando.set(false);
      }
    });
  }

  private finalizarSalvar(grupoId: number) {
    this.salvando.set(false);
    this.isDirty.set(false);
    if (this.modoEdicao()) {
      this.fecharAba(grupoId);
    }
    this.carregar();
    this.modo.set('lista');
  }

  async excluir() {
    const g = this.grupoSelecionado();
    if (!g?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusão',
      `Deseja excluir o grupo ${g.nome}? O registro será removido permanentemente. Se estiver em uso, será apenas desativado.`,
      'Sim, excluir',
      'Não, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete(`${this.apiUrl}/${g.id}`, { headers }).subscribe({
      next: async () => {
        this.excluindo.set(false);
        this.grupoSelecionado.set(null);
        this.fecharAba(g.id!);
        this.carregar();
        await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.');
      },
      error: () => {
        this.excluindo.set(false);
        this.modal.erro('Erro', 'Erro ao excluir grupo.');
      }
    });
  }

  // ── Form helpers ───────────────────────────────────────────────────
  upd(campo: keyof Grupo, v: any) {
    this.grupoForm.update(f => ({ ...f, [campo]: v }));
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  erroCampo(campo: string): string {
    return this.errosCampos()[campo] ?? '';
  }

  // ── TreeView Permissões ────────────────────────────────────────────
  toggleNo(key: string) {
    this.nosAbertos.update(s => {
      const ns = new Set(s);
      if (ns.has(key)) ns.delete(key); else ns.add(key);
      return ns;
    });
  }

  isNoAberto(key: string): boolean {
    return this.nosAbertos().has(key);
  }

  getPermissoesBloco(bloco: number): Permissao[] {
    const termo = this.normalizar(this.buscaPermissao());
    return this.grupoForm().permissoes.filter(p => {
      if (p.bloco !== bloco) return false;
      if (termo.length < 2) return true;
      return this.normalizar(p.nomeTela).includes(termo) ||
             this.normalizar(p.codigoTela).includes(termo);
    });
  }

  blocoTemResultados(bloco: number): boolean {
    return this.getPermissoesBloco(bloco).length > 0;
  }

  temAlgumaPermissao(p: Permissao): boolean {
    return p.podeConsultar || p.podeIncluir || p.podeAlterar || p.podeExcluir;
  }

  contarPermissoesBloco(bloco: number): number {
    return this.grupoForm().permissoes.filter(p => p.bloco === bloco && this.temAlgumaPermissao(p)).length;
  }

  totalTelasBloco(bloco: number): number {
    return this.grupoForm().permissoes.filter(p => p.bloco === bloco).length;
  }

  togglePermissao(codigo: string, campo: 'podeConsultar' | 'podeIncluir' | 'podeAlterar' | 'podeExcluir') {
    this.grupoForm.update(f => ({
      ...f,
      permissoes: f.permissoes.map(p =>
        p.codigoTela === codigo ? { ...p, [campo]: !p[campo] } : p
      )
    }));
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  marcarTodosBloco(bloco: number, valor: boolean) {
    this.grupoForm.update(f => ({
      ...f,
      permissoes: f.permissoes.map(p =>
        p.bloco === bloco
          ? { ...p, podeConsultar: valor, podeIncluir: valor, podeAlterar: valor, podeExcluir: valor }
          : p
      )
    }));
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  marcarTodosTela(codigo: string, valor: boolean) {
    this.grupoForm.update(f => ({
      ...f,
      permissoes: f.permissoes.map(p =>
        p.codigoTela === codigo
          ? { ...p, podeConsultar: valor, podeIncluir: valor, podeAlterar: valor, podeExcluir: valor }
          : p
      )
    }));
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  expandirTodos() {
    const keys = new Set<string>();
    for (const b of this.blocos) keys.add(`bloco_${b.bloco}`);
    for (const p of this.grupoForm().permissoes) keys.add(`tela_${p.codigoTela}`);
    this.nosAbertos.set(keys);
  }

  recolherTodos() {
    this.nosAbertos.set(new Set());
  }

  private atualizarDirtyAba() {
    const id = this.abaAtivaId();
    if (!id) return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.grupo.id === id ? { ...a, isDirty: true } : a)
    );
  }

  // ── Utils ──────────────────────────────────────────────────────────
  private novoGrupo(): GrupoDetalhe {
    return { nome: '', descricao: '', ativo: true, permissoes: this.criarPermissoesVazias() };
  }

  private clonar<T>(obj: T): T {
    return JSON.parse(JSON.stringify(obj));
  }
}
