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
  telefone: string;
  email: string;
  aliquotaIcms: number;
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

  ngOnInit() {
    this.carregar();
    this.http.get<any>(`${environment.apiUrl}/icms-uf`).subscribe({
      next: r => this.icmsUfOptions.set((r.data ?? []).filter((x: any) => x.ativo))
    });
  }

  ngOnDestroy() { sessionStorage.removeItem(this.STATE_KEY); }

  sairDaTela() {
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abasIds: abas.map(a => a.filial.id),
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

  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.salvarEstadoAbaAtiva();
    const nova = this.novaFilial();
    this.filialForm.set(nova);
    this.formOriginal = { ...nova };
    this.erro.set('');
    this.errosCampos.set({});
    this.isDirty.set(false);
    this.modoEdicao.set(false);
    this.abaAtivaId.set(null);
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
    this.formOriginal = { ...aba.filial };
    this.isDirty.set(aba.isDirty);
    this.errosCampos.set({});
    this.erro.set('');
    this.modoEdicao.set(true);
    this.modo.set('form');
  }

  fecharAba(id: number) {
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
    const req = this.modoEdicao()
      ? this.http.put<any>(`${this.apiUrl}/${f.id}`, f, { headers })
      : this.http.post<any>(this.apiUrl, f, { headers });

    req.subscribe({
      next: (r) => {
        this.salvando.set(false);
        this.carregar();
        if (!this.modoEdicao() && r.data) {
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
      },
      error: (e) => {
        this.salvando.set(false);
        this.erro.set(e?.error?.message ?? 'Erro ao salvar filial.');
      }
    });
  }

  cancelarEdicao() {
    if (this.formOriginal) this.filialForm.set({ ...this.formOriginal });
    this.isDirty.set(false);
    this.erro.set('');
    this.errosCampos.set({});
    const id = this.abaAtivaId();
    if (id != null) {
      this.abasEdicao.update(tabs =>
        tabs.map(t => t.filial.id === id ? { ...t, form: { ...this.formOriginal! }, isDirty: false } : t)
      );
    }
  }

  fecharForm() {
    const id = this.abaAtivaId();
    if (this.modoEdicao() && id != null) {
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
    this.filialForm.update(f => ({ ...f, [campo]: valor }));
    this.isDirty.set(true);
    if (this.errosCampos()[campo]) {
      this.errosCampos.update(e => { const n = { ...e }; delete n[campo]; return n; });
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

  private buscarCep(cep: string) {
    this.buscandoCep.set(true);
    this.http.get<any>(`https://viacep.com.br/ws/${cep}/json/`).subscribe({
      next: (r) => {
        this.buscandoCep.set(false);
        if (r.erro) return;
        this.filialForm.update(f => ({
          ...f,
          rua:    r.logradouro ?? f.rua,
          bairro: r.bairro     ?? f.bairro,
          cidade: r.localidade ?? f.cidade,
          uf:     r.uf         ?? f.uf
        }));
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
      cidade: '', uf: '', telefone: '', email: '', aliquotaIcms: 0, ativo: true
    };
  }

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
