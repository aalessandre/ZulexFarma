import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { FILIAIS_COLUNAS, ColunaDef } from './filiais.columns';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface LogCampo {
  campo: string;
  valorAnterior?: string;
  valorAtual?: string;
}

interface LogEntry {
  id: number;
  realizadoEm: string;
  acao: string;
  nomeUsuario: string;
  campos: LogCampo[];
}

interface Filial {
  id?: number;
  nomeFilial: string;
  razaoSocial: string;
  nomeFantasia: string;
  cnpj: string;
  inscricaoEstadual?: string;
  cep: string;
  rua: string;
  numero: string;
  bairro: string;
  cidade: string;
  uf: string;
  codigoIbgeMunicipio?: string;
  telefone: string;
  email: string;
  aliquotaIcms: number;
  incluirPromoFixa: boolean;
  incluirPromoProgressiva: boolean;
  contaCofreId?: number | null;
  contaCofreNome?: string | null;
  ativo: boolean;
  criadoEm?: string;
}

interface IcmsUfOption {
  id: number;
  uf: string;
  nomeEstado: string;
  aliquotaInterna: number;
}

interface AbaEdicao {
  filial: Filial;
  form: Filial;
  isDirty: boolean;
}

interface ColunaEstado extends ColunaDef {
  visivel: boolean;
}

type Modo = 'lista' | 'form';

@Component({
  selector: 'app-filiais',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './filiais.component.html',
  styleUrl: './filiais.component.scss'
})
export class FiliaisComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_filiais_state';
  modo = signal<Modo>('lista');
  filiais = signal<Filial[]>([]);
  filialSelecionada = signal<Filial | null>(null);
  filialForm = signal<Filial>(this.novaFilial());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  buscandoCep = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('razaoSocial');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  abasEdicao = signal<AbaEdicao[]>([]);
  abaAtivaId = signal<number | null>(null);
  modoEdicao = signal(false);
  isDirty = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});
  icmsUfOptions = signal<IcmsUfOption[]>([]);
  contasBancarias = signal<{ id: number; descricao: string }[]>([]);
  private formOriginal: Filial | null = null;
  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  carregandoLog = signal(false);
  logExpandido = signal<number | null>(null);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal<string>(this.hoje(-30));
  logDataFim    = signal<string>(this.hoje(0));

  // ── Colunas ──────────────────────────────────────────────────────
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_filiais';
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);

  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  private apiUrl = `${environment.apiUrl}/filiais`;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private tokenLiberacao: string | null = null;

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('filiais', acao)) return true;
    const resultado = await this.modal.permissao('filiais', acao);
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

  private primeiroCarregamento = true;

  private readonly TAB_ID = '/erp/filiais';
  private fechamentoConfirmado = false;

  ngOnInit() {
    if (!this.tabService.tabs().find(t => t.id === this.TAB_ID)) {
      this.tabService.abrirTab({ id: this.TAB_ID, titulo: 'Filiais', rota: this.TAB_ID, iconKey: 'building' });
    }
    this.carregar();
    this.http.get<any>(`${environment.apiUrl}/icms-uf`).subscribe({
      next: r => this.icmsUfOptions.set((r.data ?? []).filter((x: any) => x.ativo))
    });
    this.http.get<any>(`${environment.apiUrl}/contasbancarias`).subscribe({
      next: r => this.contasBancarias.set((r.data ?? []).filter((c: any) => c.ativo).map((c: any) => ({ id: c.id, descricao: c.descricao })))
    });
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
    if (e.ctrlKey && e.key === 's' && this.modo() === 'form') {
      e.preventDefault();
      if (this.isDirty()) this.salvar();
    }
    if (e.key === 'Escape' && this.modo() === 'form') {
      e.preventDefault();
      if (this.isDirty()) this.cancelarEdicao();
      else this.fecharForm();
    }
    if (e.key === 'F2' && this.modo() === 'lista') {
      e.preventDefault();
      this.editar();
    }
    if (e.key === 'Enter' && this.modo() === 'lista' && this.filialSelecionada()) {
      const el = e.target as HTMLElement;
      if (el?.tagName === 'INPUT' || el?.tagName === 'SELECT' || el?.tagName === 'TEXTAREA') return;
      e.preventDefault();
      this.editar();
    }
    if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && this.modo() === 'lista') {
      const el = e.target as HTMLElement;
      if (el?.classList?.contains('input-busca')) return;
      e.preventDefault();
      const lista = this.filiaisFiltradas();
      if (lista.length === 0) return;
      const atual = this.filialSelecionada();
      const idx = atual ? lista.findIndex(f => f.id === atual.id) : -1;
      const novoIdx = e.key === 'ArrowDown' ? Math.min(idx + 1, lista.length - 1) : Math.max(idx - 1, 0);
      this.selecionar(lista[novoIdx]);
      setTimeout(() => { const row = document.querySelector('.erp-grid tbody tr.selecionado') as HTMLElement; if (row) row.scrollIntoView({ block: 'nearest' }); });
    }
  }

  campoAlterado(campo: string): boolean {
    if (!this.formOriginal || !this.modoEdicao()) return false;
    const atual = (this.filialForm() as any)[campo];
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
      abas: abas.map(a => ({ filial: a.filial, form: a.form, isDirty: a.isDirty })),
      abaAtivaId: this.abaAtivaId()
    }));
  }

  private restaurarEstado() {
    try {
      const json = sessionStorage.getItem(this.STATE_KEY);
      if (!json) return;
      const state = JSON.parse(json);
      sessionStorage.removeItem(this.STATE_KEY);

      // Formato novo: abas completas com form
      if (state.abas?.length > 0) {
        for (const a of state.abas) {
          if (this.abasEdicao().find(x => x.filial.id === a.filial.id)) continue;
          const novaAba: AbaEdicao = { filial: a.filial, form: { ...a.form }, isDirty: a.isDirty };
          this.abasEdicao.update(tabs => [...tabs, novaAba]);
          if (a.filial.id === state.abaAtivaId) {
            this.filialSelecionada.set(a.filial);
            this.filialForm.set({ ...a.form });
            this.formOriginal = { ...a.form };
            this.isDirty.set(a.isDirty);
            this.abaAtivaId.set(a.filial.id);
            this.modoEdicao.set(a.filial.id !== this.NOVO_ID);
            this.modo.set('form');
          }
        }
        return;
      }

      // Formato legado: só IDs
      if (state.abasIds?.length > 0) {
        for (const id of state.abasIds) {
          const f = this.filiais().find(x => x.id === id);
          if (f) this.restaurarAba(f, id === state.abaAtivaId);
        }
      }
    } catch {}
  }

  private restaurarAba(f: Filial, ativar: boolean) {
    if (this.abasEdicao().find(a => a.filial.id === f.id)) return;
    const novaAba: AbaEdicao = { filial: { ...f }, form: { ...f }, isDirty: false };
    this.abasEdicao.update(tabs => [...tabs, novaAba]);
    if (ativar) this.ativarAba(f.id!);
  }

  // ── Dados ─────────────────────────────────────────────────────────

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.filiais.set(r.data ?? []);
        this.carregando.set(false);
        if (this.primeiroCarregamento) {
          this.primeiroCarregamento = false;
          this.restaurarEstado();
        }
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('filiais', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  filiaisFiltradas = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.filiais().filter(f => {
      if (status === 'ativos'   && !f.ativo) return false;
      if (status === 'inativos' &&  f.ativo) return false;
      if (termo.length < 2) return true;
      const termoDigitos = termo.replace(/\D/g, '');
      return (
        this.normalizar(f.nomeFilial).includes(termo)   ||
        this.normalizar(f.nomeFantasia).includes(termo) ||
        this.normalizar(f.razaoSocial).includes(termo)  ||
        (termoDigitos.length > 0 && f.cnpj.replace(/\D/g, '').includes(termoDigitos)) ||
        this.normalizar(f.cidade).includes(termo)
      );
    });

    if (!col) return lista;

    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean'
        ? (va === vb ? 0 : va ? -1 : 1)
        : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string {
    return (s ?? '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  getCellValue(f: Filial, campo: string): string {
    const v = (f as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Não';
    if (typeof v === 'string') return v.toUpperCase();
    return v ?? '';
  }

  selecionar(f: Filial) { this.filialSelecionada.set(f); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '▲' : '▼') : '⇅'; }

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
    const def = FILIAIS_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  // ── Colunas: visibilidade ─────────────────────────────────────────

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols =>
      cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c)
    );
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(FILIAIS_COLUNAS.map(d => ({ ...d, visivel: d.padrao })));
    localStorage.removeItem(this.STORAGE_KEY_COLUNAS);
  }

  private carregarColunas(): ColunaEstado[] {
    let salvo: { campo: string; visivel: boolean; largura: number }[] = [];
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) salvo = JSON.parse(json);
    } catch {}

    return FILIAIS_COLUNAS.map(def => {
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

  // ── CRUD ──────────────────────────────────────────────────────────

  private readonly NOVO_ID = -1;
  dataHoje = new Date().toLocaleDateString('pt-BR');

  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.salvarEstadoAbaAtiva();
    const jaExiste = this.abasEdicao().find(a => a.filial.id === this.NOVO_ID);
    if (jaExiste) {
      if (jaExiste.isDirty) {
        this.ativarAba(this.NOVO_ID); this.modoEdicao.set(false); return;
      } else {
        // Aba NOVO sem edições — remover e criar nova
        this.abasEdicao.update(tabs => tabs.filter(t => t.filial.id !== this.NOVO_ID));
      }
    }
    const nova = this.novaFilial();
    (nova as any).id = this.NOVO_ID;
    this.filialForm.set(nova);
    this.formOriginal = { ...nova };
    this.erro.set(''); this.errosCampos.set({});
    this.isDirty.set(false); this.modoEdicao.set(false);
    this.abaAtivaId.set(this.NOVO_ID);
    const novaAba: AbaEdicao = { filial: { ...nova, id: this.NOVO_ID, nomeFilial: 'Novo cadastro' } as any, form: { ...nova } as any, isDirty: false };
    this.abasEdicao.update(tabs => [...tabs, novaAba]);
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const f = this.filialSelecionada();
    if (!f?.id) return;

    const jaAberta = this.abasEdicao().find(a => a.filial.id === f.id);
    if (jaAberta) { this.ativarAba(f.id!); return; }

    this.salvarEstadoAbaAtiva();
    const novaAba: AbaEdicao = { filial: { ...f }, form: { ...f }, isDirty: false };
    this.abasEdicao.update(tabs => [...tabs, novaAba]);
    this.abaAtivaId.set(f.id!);
    this.filialForm.set({ ...f });
    this.formOriginal = { ...f };
    this.erro.set('');
    this.errosCampos.set({});
    this.isDirty.set(false);
    this.modoEdicao.set(true);
    this.modo.set('form');
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    this.abaAtivaId.set(id);
    const aba = this.abasEdicao().find(a => a.filial.id === id);
    if (!aba) return;
    this.filialSelecionada.set(aba.filial);
    this.filialForm.set({ ...aba.form });
    this.formOriginal = { ...aba.form };
    this.isDirty.set(aba.isDirty);
    this.errosCampos.set({});
    this.erro.set('');
    this.modoEdicao.set(id !== this.NOVO_ID);
    this.modo.set('form');
  }

  async fecharAba(id: number) {
    const aba = this.abasEdicao().find(a => a.filial.id === id);
    if (aba?.isDirty) {
      const r = await this.modal.confirmar('Fechar aba', `Você tem alterações não salvas. Deseja realmente fechar?`, 'Sim, fechar', 'Não, continuar editando');
      if (!r.confirmado) return;
    }
    const eraAtiva = this.abaAtivaId() === id && this.modo() === 'form';
    this.abasEdicao.update(tabs => tabs.filter(t => t.filial.id !== id));
    if (eraAtiva) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) {
        this.ativarAba(restantes[restantes.length - 1].filial.id!);
      } else {
        this.abaAtivaId.set(null);
        this.modo.set('lista');
      }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (id == null) return;
    const form = this.filialForm();
    const dirty = this.isDirty();
    this.abasEdicao.update(tabs =>
      tabs.map(t => t.filial.id === id ? { ...t, form: { ...form }, isDirty: dirty } : t)
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    if (!this.validar()) return;
    this.erro.set('');
    const f = this.filialForm();
    this.salvando.set(true);

    const headers = this.headerLiberacao();
    const payload = { ...f };
    if (!this.modoEdicao()) delete (payload as any).id;
    const req = this.modoEdicao()
      ? this.http.put<any>(`${this.apiUrl}/${f.id}`, payload, { headers })
      : this.http.post<any>(this.apiUrl, payload, { headers });

    req.subscribe({
      next: (r) => {
        this.salvando.set(false);
        this.carregar();
        if (!this.modoEdicao() && r.data) {
          this.abasEdicao.update(tabs => tabs.filter(t => t.filial.id !== this.NOVO_ID));
          const novaAba: AbaEdicao = { filial: { ...r.data }, form: { ...r.data }, isDirty: false };
          this.abasEdicao.update(tabs => [...tabs, novaAba]);
          this.abaAtivaId.set(r.data.id);
          this.filialSelecionada.set(r.data);
          this.filialForm.set({ ...r.data });
          this.formOriginal = { ...r.data };
          this.modoEdicao.set(true);
        } else {
          const id = this.abaAtivaId();
          if (id != null) {
            this.abasEdicao.update(tabs =>
              tabs.map(t => t.filial.id === id
                ? { ...t, filial: { ...f }, form: { ...f }, isDirty: false } : t)
            );
          }
          this.formOriginal = { ...f };
        }
        this.isDirty.set(false);
        this.errosCampos.set({});
        // Atualiza nome da filial na topbar se for a filial do usuário logado
        const usuario = this.auth.usuarioLogado();
        if (usuario && String(f.id) === String(usuario.filialId) && f.nomeFilial) {
          this.auth.atualizarNomeFilial(f.nomeFilial);
        }
      },
      error: (e) => {
        this.salvando.set(false);
        this.erro.set(e?.error?.message ?? 'Erro ao salvar filial.');
      }
    });
  }

  async cancelarEdicao() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Cancelar edição', 'Você tem alterações não salvas. Deseja realmente cancelar?', 'Sim, cancelar', 'Não, continuar editando');
      if (!r.confirmado) return;
    }
    const id = this.abaAtivaId();
    if (id === this.NOVO_ID) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.filial.id !== this.NOVO_ID));
      this.abaAtivaId.set(null);
      this.modo.set('lista');
      return;
    }
    if (this.formOriginal) this.filialForm.set({ ...this.formOriginal });
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    if (id != null) {
      this.abasEdicao.update(tabs =>
        tabs.map(t => t.filial.id === id ? { ...t, form: { ...this.formOriginal! }, isDirty: false } : t)
      );
    }
  }

  async fecharForm() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Fechar cadastro', 'Você tem alterações não salvas. Deseja realmente fechar?', 'Sim, fechar', 'Não, continuar editando');
      if (!r.confirmado) return;
    }
    const id = this.abaAtivaId();
    if (id != null) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.filial.id !== id));
      this.abaAtivaId.set(null);
    }
    this.modo.set('lista');
  }

  fechar() {
    this.salvarEstadoAbaAtiva();
    this.modo.set('lista');
    this.carregar();
  }

  async excluir() {
    const f = this.filialSelecionada();
    if (!f?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusão',
      `Deseja excluir a filial ${f.nomeFantasia}? O registro será removido permanentemente. Se estiver em uso, será apenas desativado.`,
      'Sim, excluir',
      'Não, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    this.excluindo.set(true);
    const headers = this.headerLiberacao();
    this.http.delete<any>(`${this.apiUrl}/${f.id}`, { headers }).subscribe({
      next: async (r) => {
        this.excluindo.set(false);
        if (f.id) this.abasEdicao.update(tabs => tabs.filter(t => t.filial.id !== f.id));
        this.filialSelecionada.set(null);
        this.abaAtivaId.set(null);
        this.modo.set('lista');
        this.carregar();

        const tipo = r?.resultado ?? 'excluido';
        if (tipo === 'excluido') {
          await this.modal.sucesso('Excluído', 'Registro excluído com sucesso.');
        } else {
          await this.modal.aviso('Desativado', 'O registro está em uso e foi apenas desativado.');
        }
      },
      error: (e) => {
        this.excluindo.set(false);
        this.modal.erro('Erro', e?.error?.message ?? 'Erro ao excluir filial.');
      }
    });
  }

  // ── Log ───────────────────────────────────────────────────────────

  abrirLog() {
    const f = this.filialSelecionada();
    if (!f?.id) return;
    this.logDataInicio.set(this.hoje(-30));
    this.logDataFim.set(this.hoje(0));
    this.modalLog.set(true);
    this.logExpandido.set(null);
    this.filtrarLog();
  }

  filtrarLog() {
    const f = this.filialSelecionada();
    if (!f?.id) return;
    this.carregandoLog.set(true);
    this.logExpandido.set(null);
    this.logSelecionado.set(null);
    const params = `dataInicio=${this.logDataInicio()}&dataFim=${this.logDataFim()}`;
    this.http.get<any>(`${this.apiUrl}/${f.id}/log?${params}`).subscribe({
      next: r => {
        const lista: LogEntry[] = r.data ?? [];
        this.logRegistros.set(lista);
        this.logSelecionado.set(lista.length > 0 ? lista[0] : null);
        this.carregandoLog.set(false);
      },
      error: () => { this.carregandoLog.set(false); }
    });
  }

  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }
  fecharLog() { this.modalLog.set(false); }

  toggleLogRow(id: number) { this.logExpandido.update(v => v === id ? null : id); }

  acaoCss(acao: string): string {
    if (acao === 'CRIAÇÃO')   return 'badge-criacao';
    if (acao === 'ALTERAÇÃO') return 'badge-alteracao';
    if (acao === 'EXCLUSÃO')    return 'badge-exclusao';
    if (acao === 'DESATIVAÇÃO') return 'badge-desativacao';
    return '';
  }

  // ── Formulário ────────────────────────────────────────────────────

  updateForm(campo: keyof Filial, valor: any) {
    if (typeof valor === 'string' && campo !== 'criadoEm') valor = valor.toUpperCase();
    this.filialForm.update(f => ({ ...f, [campo]: valor }));
    this.isDirty.set(true);
    const id = this.abaAtivaId();
    if (id != null) this.abasEdicao.update(tabs => tabs.map(t => t.filial.id === id ? { ...t, isDirty: true } : t));
    if (this.errosCampos()[campo]) {
      this.errosCampos.update(e => { const n = { ...e }; delete n[campo]; return n; });
    }
  }

  onApelidoBlur() {
    const f = this.filialForm();
    if (!f.nomeFilial?.trim() && f.nomeFantasia?.trim()) {
      this.updateForm('nomeFilial', f.nomeFantasia);
    }
  }

  onCnpjInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const mascarado = this.mascaraCnpj(input.value);
    input.value = mascarado;
    this.updateForm('cnpj', mascarado);
  }

  onTelefoneInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const mascarado = this.mascaraTelefone(input.value);
    input.value = mascarado;
    this.updateForm('telefone', mascarado);
  }

  onCepInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const mascarado = this.mascaraCep(input.value);
    input.value = mascarado;
    this.updateForm('cep', mascarado);
    const digits = mascarado.replace(/\D/g, '');
    if (digits.length === 8) this.buscarCep(digits);
  }

  formatarCnpj(valor: string): string {
    if (!valor) return '';
    return this.mascaraCnpj(valor.replace(/\D/g, ''));
  }

  private mascaraCnpj(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 14);
    if (d.length <= 2)  return d;
    if (d.length <= 5)  return `${d.slice(0,2)}.${d.slice(2)}`;
    if (d.length <= 8)  return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5)}`;
    if (d.length <= 12) return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8)}`;
    return `${d.slice(0,2)}.${d.slice(2,5)}.${d.slice(5,8)}/${d.slice(8,12)}-${d.slice(12)}`;
  }

  private mascaraTelefone(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 11);
    if (d.length <= 2)  return d.length ? `(${d}` : '';
    if (d.length <= 6)  return `(${d.slice(0,2)}) ${d.slice(2)}`;
    if (d.length <= 10) return `(${d.slice(0,2)}) ${d.slice(2,6)}-${d.slice(6)}`;
    return `(${d.slice(0,2)}) ${d.slice(2,7)}-${d.slice(7)}`;
  }

  private mascaraCep(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 8);
    if (d.length <= 5) return d;
    return `${d.slice(0,5)}-${d.slice(5)}`;
  }

  buscarCepManual() {
    const digits = (this.filialForm().cep ?? '').replace(/\D/g, '');
    if (digits.length === 8) this.buscarCep(digits);
  }

  private buscarCep(cep: string) {
    this.buscandoCep.set(true);
    this.http.get<any>(`https://viacep.com.br/ws/${cep}/json/`).subscribe({
      next: (r) => {
        this.buscandoCep.set(false);
        if (r.erro) return;
        // Buscar ICMS pela UF retornada
        const uf = r.uf ?? '';
        const icmsUf = this.icmsUfOptions().find(x => x.uf === uf);
        this.filialForm.update(f => ({
          ...f,
          rua:    (r.logradouro ?? f.rua).toUpperCase(),
          bairro: (r.bairro     ?? f.bairro).toUpperCase(),
          cidade: (r.localidade ?? f.cidade).toUpperCase(),
          uf:     (uf || f.uf).toUpperCase(),
          codigoIbgeMunicipio: r.ibge ?? f.codigoIbgeMunicipio,
          aliquotaIcms: icmsUf ? icmsUf.aliquotaInterna : f.aliquotaIcms
        }));
        this.isDirty.set(true);
        const id = this.abaAtivaId();
        if (id != null) this.abasEdicao.update(tabs => tabs.map(t => t.filial.id === id ? { ...t, isDirty: true } : t));
      },
      error: () => this.buscandoCep.set(false)
    });
  }

  // ── Validação ─────────────────────────────────────────────────────

  private validar(): boolean {
    const f = this.filialForm();
    const erros: Record<string, string> = {};
    if (!f.razaoSocial?.trim())  erros['razaoSocial']  = 'Obrigatório';
    if (!f.nomeFantasia?.trim()) erros['nomeFantasia']  = 'Obrigatório';
    if (!f.nomeFilial?.trim())   erros['nomeFilial']    = 'Obrigatório';
    if (!f.cnpj?.trim())         erros['cnpj']          = 'Obrigatório';
    if (!f.cep?.trim())          erros['cep']           = 'Obrigatório';
    if (!f.rua?.trim())          erros['rua']           = 'Obrigatório';
    if (!f.numero?.trim())       erros['numero']        = 'Obrigatório';
    if (!f.bairro?.trim())       erros['bairro']        = 'Obrigatório';
    if (!f.cidade?.trim())       erros['cidade']        = 'Obrigatório';
    if (!f.uf?.trim())           erros['uf']            = 'Obrigatório';
    if (!f.telefone?.trim())     erros['telefone']      = 'Obrigatório';
    if (!f.email?.trim())        erros['email']         = 'Obrigatório';
    this.errosCampos.set(erros);
    if (Object.keys(erros).length > 0) {
      this.erro.set('Preencha todos os campos obrigatórios.');
      return false;
    }
    this.erro.set('');
    return true;
  }

  erroCampo(campo: string): string { return this.errosCampos()[campo] ?? ''; }

  private novaFilial(): Filial {
    return {
      nomeFilial: '', razaoSocial: '', nomeFantasia: '', cnpj: '',
      inscricaoEstadual: '', cep: '', rua: '', numero: '', bairro: '',
      cidade: '', uf: '', telefone: '', email: '', aliquotaIcms: 0,
      incluirPromoFixa: true, incluirPromoProgressiva: true,
      contaCofreId: null, contaCofreNome: null,
      ativo: true
    };
  }

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
