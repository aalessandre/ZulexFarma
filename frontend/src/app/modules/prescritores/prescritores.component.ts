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

interface Prescritor {
  id?: number;
  codigo?: string;
  nome: string;
  tipoConselho: string;
  numeroConselho: string;
  uf: string;
  cpf?: string | null;
  especialidade?: string | null;
  telefone?: string | null;
  ativo: boolean;
  criadoEm?: string;
}

interface AbaEdicao {
  prescritor: Prescritor;
  form: Prescritor;
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

const PRESCRITORES_COLUNAS: ColunaDef[] = [
  { campo: 'codigo',         label: 'Código',        largura: 80,  minLargura: 60,  padrao: true },
  { campo: 'nome',           label: 'Nome',          largura: 260, minLargura: 120, padrao: true },
  { campo: 'tipoConselho',   label: 'Conselho',      largura: 90,  minLargura: 70,  padrao: true },
  { campo: 'numeroConselho', label: 'Nº',            largura: 100, minLargura: 60,  padrao: true },
  { campo: 'uf',             label: 'UF',            largura: 60,  minLargura: 40,  padrao: true },
  { campo: 'especialidade',  label: 'Especialidade', largura: 180, minLargura: 100, padrao: false },
  { campo: 'telefone',       label: 'Telefone',      largura: 130, minLargura: 90,  padrao: false },
  { campo: 'ativo',          label: 'Ativo',         largura: 60,  minLargura: 50,  padrao: true },
];

const TIPOS_CONSELHO = ['CRM', 'CRO', 'CRMV', 'CRF', 'CRP', 'CRN'];

@Component({
  selector: 'app-prescritores',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './prescritores.component.html',
  styleUrl: './prescritores.component.scss'
})
export class PrescritoresComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_prescritores_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_prescritores';

  tiposConselho = TIPOS_CONSELHO;
  modo = signal<Modo>('lista');
  prescritores = signal<Prescritor[]>([]);
  prescritorSelecionado = signal<Prescritor | null>(null);
  prescritorForm = signal<Prescritor>(this.novoPrescritor());
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
  private formOriginal: Prescritor | null = null;

  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal('');
  logDataFim = signal('');
  carregandoLog = signal(false);

  private apiUrl = `${environment.apiUrl}/prescritores`;
  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('prescritores', acao)) return true;
    const resultado = await this.modal.permissao('prescritores', acao);
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

  private readonly TAB_ID = '/erp/prescritores';
  private fechamentoConfirmado = false;

  ngOnInit() {
    if (!this.tabService.tabs().find(t => t.id === this.TAB_ID)) {
      this.tabService.abrirTab({ id: this.TAB_ID, titulo: 'Prescritores', rota: this.TAB_ID, iconKey: 'box' });
    }
    this.carregar();
    window.addEventListener('beforeunload', this.onBeforeUnload);
    this.tabService.registrarBeforeClose(this.TAB_ID, async () => {
      if (this.isDirty()) {
        const r = await this.modal.confirmar('Fechar tela', 'Você tem alterações não salvas. Deseja realmente fechar?', 'Sim, fechar', 'Não, continuar editando');
        if (!r.confirmado) return false;
      }
      this.fechamentoConfirmado = true;
      this.abasEdicao.set([]);
      sessionStorage.removeItem(this.STATE_KEY);
      return true;
    });
  }

  ngOnDestroy() {
    window.removeEventListener('beforeunload', this.onBeforeUnload);
    this.tabService.removerBeforeClose(this.TAB_ID);
    if (!this.fechamentoConfirmado) this.persistirEstado();
  }

  private onBeforeUnload = (e: BeforeUnloadEvent) => {
    this.persistirEstado();
    if (this.isDirty()) e.preventDefault();
  };

  @HostListener('document:keydown', ['$event'])
  onKeydown(e: KeyboardEvent) {
    if (this.modal.visivel()) return;
    if (e.ctrlKey && e.key === 's' && this.modo() === 'form') { e.preventDefault(); if (this.isDirty()) this.salvar(); }
    if (e.key === 'Escape' && this.modo() === 'form') { e.preventDefault(); if (this.isDirty()) this.cancelarEdicao(); else this.fecharForm(); }
    if (e.key === 'F2' && this.modo() === 'lista') { e.preventDefault(); this.editar(); }
    if (e.key === 'Enter' && this.modo() === 'lista' && this.prescritorSelecionado()) {
      const el = e.target as HTMLElement;
      if (el?.tagName === 'INPUT' || el?.tagName === 'SELECT' || el?.tagName === 'TEXTAREA') return;
      e.preventDefault(); this.editar();
    }
    if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && this.modo() === 'lista') {
      const el = e.target as HTMLElement;
      if (el?.classList?.contains('input-busca')) return;
      e.preventDefault();
      const lista = this.prescritoresFiltrados();
      if (lista.length === 0) return;
      const atual = this.prescritorSelecionado();
      const idx = atual ? lista.findIndex(f => f.id === atual.id) : -1;
      const novoIdx = e.key === 'ArrowDown' ? Math.min(idx + 1, lista.length - 1) : Math.max(idx - 1, 0);
      this.selecionar(lista[novoIdx]);
      setTimeout(() => { const row = document.querySelector('.erp-grid tbody tr.selecionado') as HTMLElement; if (row) row.scrollIntoView({ block: 'nearest' }); });
    }
  }

  campoAlterado(campo: string): boolean {
    if (!this.formOriginal || !this.modoEdicao()) return false;
    const atual = (this.prescritorForm() as any)[campo];
    const original = (this.formOriginal as any)[campo];
    return (atual ?? '') !== (original ?? '');
  }

  async sairDaTela() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Sair da tela', 'Você tem alterações não salvas. Deseja realmente sair?', 'Sim, sair', 'Não, continuar editando');
      if (!r.confirmado) return;
    }
    this.fechamentoConfirmado = true;
    this.abasEdicao.set([]);
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abas: abas.map(a => ({ prescritor: a.prescritor, form: a.form, isDirty: a.isDirty })),
      abaAtivaId: this.abaAtivaId()
    }));
  }

  private restaurarEstado() {
    try {
      const json = sessionStorage.getItem(this.STATE_KEY);
      if (!json) return;
      const state = JSON.parse(json);
      sessionStorage.removeItem(this.STATE_KEY);

      if (state.abas?.length > 0) {
        for (const a of state.abas) {
          if (this.abasEdicao().find(x => x.prescritor.id === a.prescritor.id)) continue;
          const novaAba: AbaEdicao = { prescritor: a.prescritor, form: this.clonar(a.form), isDirty: a.isDirty };
          this.abasEdicao.update(tabs => [...tabs, novaAba]);
          if (a.prescritor.id === state.abaAtivaId) {
            this.prescritorSelecionado.set(a.prescritor);
            this.prescritorForm.set(this.clonar(a.form));
            this.formOriginal = this.clonar(a.form);
            this.isDirty.set(a.isDirty);
            this.abaAtivaId.set(a.prescritor.id);
            this.modoEdicao.set(a.prescritor.id !== this.NOVO_ID);
            this.modo.set('form');
          }
        }
      }
    } catch {}
  }

  private primeiroCarregamento = true;

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.prescritores.set(r.data ?? []);
        this.carregando.set(false);
        if (this.primeiroCarregamento) {
          this.primeiroCarregamento = false;
          this.restaurarEstado();
        }
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('prescritores', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  prescritoresFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.prescritores().filter(p => {
      if (status === 'ativos'   && !p.ativo) return false;
      if (status === 'inativos' &&  p.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(p.nome).includes(termo)
          || this.normalizar(p.numeroConselho).includes(termo);
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

  private normalizar(s: string | null | undefined): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(p: Prescritor, campo: string): string {
    const v = (p as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Não';
    return v ?? '';
  }

  selecionar(p: Prescritor) { this.prescritorSelecionado.set(p); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }

  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return PRESCRITORES_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return PRESCRITORES_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(PRESCRITORES_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
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
    const def = PRESCRITORES_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  private readonly NOVO_ID = -1;
  dataHoje = new Date().toLocaleDateString('pt-BR');

  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.salvarEstadoAbaAtiva();
    const jaExiste = this.abasEdicao().find(a => a.prescritor.id === this.NOVO_ID);
    if (jaExiste) {
      if (jaExiste.isDirty) { this.ativarAba(this.NOVO_ID); this.modoEdicao.set(false); return; }
      else { this.abasEdicao.update(tabs => tabs.filter(t => t.prescritor.id !== this.NOVO_ID)); }
    }
    const novo = this.novoPrescritor();
    (novo as any).id = this.NOVO_ID;
    this.prescritorForm.set(novo);
    this.formOriginal = this.clonar(novo);
    this.modoEdicao.set(false);
    this.isDirty.set(false);
    this.erro.set(''); this.errosCampos.set({});
    this.abaAtivaId.set(this.NOVO_ID);
    const novaAba: AbaEdicao = {
      prescritor: { id: this.NOVO_ID, nome: 'Novo cadastro', tipoConselho: 'CRM', numeroConselho: '', uf: '', ativo: true },
      form: this.clonar(novo),
      isDirty: false
    };
    this.abasEdicao.update(tabs => [...tabs, novaAba]);
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const p = this.prescritorSelecionado();
    if (!p?.id) return;

    const jaAberta = this.abasEdicao().find(a => a.prescritor.id === p.id);
    if (jaAberta) { this.ativarAba(p.id); return; }

    const aba: AbaEdicao = { prescritor: { ...p }, form: this.clonar(p), isDirty: false };
    this.abasEdicao.update(abas => [...abas, aba]);
    this.abaAtivaId.set(p.id!);
    this.prescritorForm.set(this.clonar(p));
    this.formOriginal = this.clonar(p);
    this.modoEdicao.set(true);
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    this.modo.set('form');
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    const aba = this.abasEdicao().find(a => a.prescritor.id === id);
    if (!aba) return;
    this.abaAtivaId.set(id);
    this.prescritorForm.set(this.clonar(aba.form));
    this.formOriginal = this.clonar(aba.form);
    this.isDirty.set(aba.isDirty);
    this.modoEdicao.set(id !== this.NOVO_ID);
    this.erro.set(''); this.errosCampos.set({});
    this.modo.set('form');
  }

  async fecharAba(id: number) {
    const aba = this.abasEdicao().find(a => a.prescritor.id === id);
    if (aba?.isDirty) {
      const r = await this.modal.confirmar('Fechar aba', 'Você tem alterações não salvas. Deseja realmente fechar?', 'Sim, fechar', 'Não, continuar editando');
      if (!r.confirmado) return;
    }
    this.abasEdicao.update(abas => abas.filter(a => a.prescritor.id !== id));
    if (this.abaAtivaId() === id) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) this.ativarAba(restantes[restantes.length - 1].prescritor.id!);
      else { this.modo.set('lista'); this.abaAtivaId.set(null); }
    }
  }

  async fecharForm() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Fechar cadastro', 'Você tem alterações não salvas. Deseja realmente fechar?', 'Sim, fechar', 'Não, continuar editando');
      if (!r.confirmado) return;
    }
    const id = this.abaAtivaId();
    if (id != null) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.prescritor.id !== id));
      this.abaAtivaId.set(null);
    } else {
      this.modo.set('lista');
    }
  }

  async cancelarEdicao() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Cancelar edição', 'Você tem alterações não salvas. Deseja realmente cancelar?', 'Sim, cancelar', 'Não, continuar editando');
      if (!r.confirmado) return;
    }
    const id = this.abaAtivaId();
    if (id === this.NOVO_ID) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.prescritor.id !== this.NOVO_ID));
      this.abaAtivaId.set(null); this.modo.set('lista'); return;
    }
    if (this.formOriginal) {
      this.prescritorForm.set(this.clonar(this.formOriginal));
      this.isDirty.set(false);
      if (id) {
        this.abasEdicao.update(abas =>
          abas.map(a => a.prescritor.id === id ? { ...a, isDirty: false } : a)
        );
      }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (!id || this.modo() !== 'form') return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.prescritor.id === id
        ? { ...a, form: this.clonar(this.prescritorForm()), isDirty: this.isDirty() }
        : a
      )
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    const erros: Record<string, string> = {};
    const p = this.prescritorForm();
    if (!p.nome?.trim()) erros['nome'] = 'Nome é obrigatório.';
    if (!p.tipoConselho?.trim()) erros['tipoConselho'] = 'Conselho é obrigatório.';
    if (!p.numeroConselho?.trim()) erros['numeroConselho'] = 'Número é obrigatório.';
    if (!p.uf?.trim() || p.uf.trim().length !== 2) erros['uf'] = 'UF inválida.';
    if (Object.keys(erros).length) { this.errosCampos.set(erros); return; }
    this.errosCampos.set({});
    this.salvando.set(true);

    const headers = this.headerLiberacao();
    const body: any = {
      nome: p.nome,
      tipoConselho: p.tipoConselho,
      numeroConselho: p.numeroConselho,
      uf: p.uf,
      cpf: p.cpf ?? null,
      especialidade: p.especialidade ?? null,
      telefone: p.telefone ?? null,
      ativo: p.ativo
    };

    const salvarDados$ = this.modoEdicao()
      ? this.http.put(`${this.apiUrl}/${p.id}`, body, { headers })
      : this.http.post<any>(this.apiUrl, body, { headers });

    salvarDados$.subscribe({
      next: (r: any) => {
        const prescId = this.modoEdicao() ? p.id! : r.data?.id;
        this.finalizarSalvar(prescId);
      },
      error: (e: any) => {
        this.erro.set(e?.error?.message || 'Erro ao salvar prescritor.');
        this.salvando.set(false);
      }
    });
  }

  private finalizarSalvar(id: number) {
    this.salvando.set(false);
    this.isDirty.set(false);
    if (this.modoEdicao()) this.fecharAba(id);
    this.carregar();
    this.modo.set('lista');
  }

  async excluir() {
    const p = this.prescritorSelecionado();
    if (!p?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusão',
      `Deseja excluir o prescritor ${p.nome}? O registro será removido permanentemente. Se estiver em uso, será apenas desativado.`,
      'Sim, excluir',
      'Não, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${p.id}`, { headers }).subscribe({
      next: async (r: any) => {
        this.excluindo.set(false);
        this.prescritorSelecionado.set(null);
        this.fecharAba(p.id!);
        this.carregar();
        if (r.resultado === 'desativado') {
          await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.');
        }
      },
      error: () => {
        this.excluindo.set(false);
        this.modal.erro('Erro', 'Erro ao excluir prescritor.');
      }
    });
  }

  private readonly camposNaoUpper = new Set(['cpf', 'telefone', 'ativo', 'criadoEm']);

  upd(campo: keyof Prescritor, v: any) {
    if (typeof v === 'string' && !this.camposNaoUpper.has(campo as string)) v = v.toUpperCase();
    this.prescritorForm.update(f => ({ ...f, [campo]: v }));
    this.isDirty.set(true);
    this.atualizarDirtyAba();
  }

  erroCampo(campo: string): string { return this.errosCampos()[campo] ?? ''; }

  private atualizarDirtyAba() {
    const id = this.abaAtivaId();
    if (!id) return;
    this.abasEdicao.update(abas =>
      abas.map(a => a.prescritor.id === id ? { ...a, isDirty: true } : a)
    );
  }

  abrirLog() {
    const p = this.prescritorSelecionado();
    if (!p?.id) return;
    this.modalLog.set(true);
    this.logRegistros.set([]);
    this.logSelecionado.set(null);
    this.filtrarLog();
  }
  fecharLog() { this.modalLog.set(false); }

  filtrarLog() {
    const p = this.prescritorSelecionado();
    if (!p?.id) return;
    this.carregandoLog.set(true);
    let url = `${this.apiUrl}/${p.id}/log`;
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
      'CRIAÇÃO': 'log-badge badge-criacao',
      'ALTERAÇÃO': 'log-badge badge-alteracao',
      'EXCLUSÃO': 'log-badge badge-exclusao',
      'DESATIVAÇÃO': 'log-badge badge-desativacao'
    };
    return map[acao] ?? 'log-badge';
  }

  private novoPrescritor(): Prescritor {
    return { nome: '', tipoConselho: 'CRM', numeroConselho: '', uf: '', cpf: null, especialidade: null, telefone: null, ativo: true };
  }

  private clonar<T>(obj: T): T { return JSON.parse(JSON.stringify(obj)); }
}
