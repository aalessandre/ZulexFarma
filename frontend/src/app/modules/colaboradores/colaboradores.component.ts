import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { COLABORADORES_COLUNAS, ColunaDef } from './colaboradores.columns';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface LogCampo { campo: string; valorAnterior?: string; valorAtual?: string; }
interface LogEntry { id: number; realizadoEm: string; acao: string; nomeUsuario: string; campos: LogCampo[]; }

interface Contato { id?: number; tipo: string; valor: string; descricao?: string; principal: boolean; }
interface Endereco {
  id?: number; tipo: string; cep: string; rua: string; numero: string;
  complemento?: string; bairro: string; cidade: string; uf: string; principal: boolean;
}

interface Colaborador {
  id?: number; nome: string; cpf: string; rg?: string; dataNascimento?: string;
  cargo?: string; dataAdmissao?: string; salario?: number;
  email?: string; telefone?: string; cidade?: string; uf?: string;
  observacao?: string; ativo: boolean; criadoEm?: string;
}

interface FilialGrupoItem { filialId: number; grupoUsuarioId: number; nomeFilial?: string; nomeGrupo?: string; }

interface Acesso {
  usuarioId?: number;
  login: string;
  senha?: string;
  isAdministrador: boolean;
  sessaoMaximaMinutos: number;
  inatividadeMinutos: number;
  filialPadraoId?: number;
  filialGrupos: FilialGrupoItem[];
}

interface FilialOption { id: number; nomeFantasia: string; }
interface GrupoOption { id: number; nome: string; }

interface ColaboradorDetalhe extends Colaborador {
  enderecos: Endereco[];
  contatos: Contato[];
  acesso?: Acesso | null;
}

interface AbaEdicao { colaborador: Colaborador; form: ColaboradorDetalhe; isDirty: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

type Modo = 'lista' | 'form';
type AbaForm = 'dados' | 'endereco' | 'contato' | 'acesso';

@Component({
  selector: 'app-colaboradores',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './colaboradores.component.html',
  styleUrl: './colaboradores.component.scss'
})
export class ColaboradoresComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_colaboradores_state';
  modo = signal<Modo>('lista');
  colaboradores = signal<Colaborador[]>([]);
  colaboradorSelecionado = signal<Colaborador | null>(null);
  colaboradorForm = signal<ColaboradorDetalhe>(this.novoColaborador());
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  buscandoCep = signal(false);
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
  private formOriginal: ColaboradorDetalhe | null = null;

  abaFormAtiva = signal<AbaForm>('dados');

  // Modais
  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  carregandoLog = signal(false);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal<string>(this.hoje(-30));
  logDataFim    = signal<string>(this.hoje(0));

  // Colunas
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_colaboradores';
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;

  private apiUrl = `${environment.apiUrl}/colaboradores`;

  // Acesso
  filiais = signal<FilialOption[]>([]);
  grupos = signal<GrupoOption[]>([]);
  acessoHabilitado = signal(false);
  multiSelectAberto = signal<number | null>(null);

  tiposContato = ['TELEFONE', 'CELULAR', 'EMAIL', 'WHATSAPP', 'OUTRO'];
  tiposEndereco = ['PRINCIPAL', 'ENTREGA', 'COBRANÇA', 'OUTRO'];

  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('colaboradores', acao)) return true;
    const resultado = await this.modal.permissao('colaboradores', acao);
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

  ngOnInit() {
    this.carregar();
    this.carregarFiliais();
    this.carregarGrupos();
  }

  ngOnDestroy() { this.persistirEstado(); }

  sairDaTela() {
    this.persistirEstado();
    this.tabService.fecharTabAtiva();
  }

  private persistirEstado() {
    this.salvarEstadoAbaAtiva();
    const abas = this.abasEdicao();
    if (abas.length === 0) { sessionStorage.removeItem(this.STATE_KEY); return; }
    sessionStorage.setItem(this.STATE_KEY, JSON.stringify({
      abasIds: abas.map(a => a.colaborador.id),
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
          const c = this.colaboradores().find(x => x.id === id);
          if (c) this.restaurarAba(c, id === state.abaAtivaId);
        }
      }
    } catch {}
  }

  private restaurarAba(c: any, ativar: boolean) {
    this.http.get<any>(`${this.apiUrl}/${c.id}`).subscribe({
      next: r => {
        if (this.abasEdicao().find(a => a.colaborador.id === c.id)) return;
        const detalhe: ColaboradorDetalhe = {
          ...c,
          enderecos: r.data.enderecos ?? [],
          contatos: r.data.contatos ?? [],
          observacao: r.data.observacao,
          cargo: r.data.cargo,
          dataAdmissao: r.data.dataAdmissao,
          salario: r.data.salario,
          rg: r.data.rg,
          dataNascimento: r.data.dataNascimento,
          acesso: r.data.acesso ?? null
        };
        const novaAba: AbaEdicao = { colaborador: { ...c }, form: this.clonarDetalhe(detalhe), isDirty: false };
        this.abasEdicao.update(tabs => [...tabs, novaAba]);
        if (ativar) this.ativarAba(c.id!);
      },
      error: () => { /* silently ignore - tab just won't restore */ }
    });
  }

  private carregarFiliais() {
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({
      next: r => this.filiais.set((r.data ?? []).filter((f: any) => f.ativo))
    });
  }

  private carregarGrupos() {
    this.http.get<any>(`${environment.apiUrl}/grupos`).subscribe({
      next: r => this.grupos.set((r.data ?? []).filter((g: any) => g.ativo))
    });
  }

  private primeiroCarregamento = true;

  // ── Dados ─────────────────────────────────────────────────────────
  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.colaboradores.set(r.data ?? []);
        this.carregando.set(false);
        if (this.primeiroCarregamento) {
          this.primeiroCarregamento = false;
          this.restaurarEstado();
        }
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('colaboradores', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  colaboradoresFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.colaboradores().filter(c => {
      if (status === 'ativos'   && !c.ativo) return false;
      if (status === 'inativos' &&  c.ativo) return false;
      if (termo.length < 3) return true;
      const termoDigitos = termo.replace(/\D/g, '');
      return (
        this.normalizar(c.nome).includes(termo) ||
        (termoDigitos.length > 0 && c.cpf.replace(/\D/g, '').includes(termoDigitos)) ||
        this.normalizar(c.cargo ?? '').includes(termo) ||
        this.normalizar(c.cidade ?? '').includes(termo) ||
        this.normalizar(c.email ?? '').includes(termo)
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
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(c: Colaborador, campo: string): string {
    const v = (c as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Não';
    if (campo === 'dataNascimento' && v) return new Date(v).toLocaleDateString('pt-BR');
    if (campo === 'salario' && v != null) return Number(v).toLocaleString('pt-BR', { minimumFractionDigits: 2 });
    return v ?? '';
  }

  selecionar(c: Colaborador) { this.colaboradorSelecionado.set(c); }
  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  // ── Colunas: resize ───────────────────────────────────────────────
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
    const def = COLABORADORES_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(COLABORADORES_COLUNAS.map(d => ({ ...d, visivel: d.padrao })));
    localStorage.removeItem(this.STORAGE_KEY_COLUNAS);
  }

  private carregarColunas(): ColunaEstado[] {
    let salvo: { campo: string; visivel: boolean; largura: number }[] = [];
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) salvo = JSON.parse(json);
    } catch {}
    return COLABORADORES_COLUNAS.map(def => {
      const s = salvo.find(x => x.campo === def.campo);
      return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
    });
  }

  private salvarColunasStorage() {
    const estado = this.colunas().map(c => ({ campo: c.campo, visivel: c.visivel, largura: Math.round(c.largura) }));
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(estado));
  }

  // ── CRUD ──────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.salvarEstadoAbaAtiva();
    const novo = this.novoColaborador();
    this.colaboradorForm.set(novo);
    this.formOriginal = { ...novo, enderecos: [...novo.enderecos.map(e => ({ ...e }))], contatos: [...novo.contatos.map(c => ({ ...c }))] };
    this.erro.set(''); this.errosCampos.set({});
    this.isDirty.set(false); this.modoEdicao.set(false);
    this.abaAtivaId.set(null); this.abaFormAtiva.set('dados');
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const c = this.colaboradorSelecionado();
    if (!c?.id) return;
    const jaAberta = this.abasEdicao().find(a => a.colaborador.id === c.id);
    if (jaAberta) { this.ativarAba(c.id!); return; }

    // Carregar detalhe do backend
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${c.id}`).subscribe({
      next: r => {
        this.carregando.set(false);
        // Re-check duplicata (outra chamada async pode ter adicionado enquanto esperava o HTTP)
        if (this.abasEdicao().find(a => a.colaborador.id === c.id)) { this.ativarAba(c.id!); return; }
        const detalhe: ColaboradorDetalhe = {
          ...c,
          enderecos: r.data.enderecos ?? [],
          contatos: r.data.contatos ?? [],
          observacao: r.data.observacao,
          cargo: r.data.cargo,
          dataAdmissao: r.data.dataAdmissao,
          salario: r.data.salario,
          rg: r.data.rg,
          dataNascimento: r.data.dataNascimento,
          acesso: r.data.acesso ?? null
        };
        this.acessoHabilitado.set(!!detalhe.acesso);
        this.salvarEstadoAbaAtiva();
        const novaAba: AbaEdicao = { colaborador: { ...c }, form: this.clonarDetalhe(detalhe), isDirty: false };
        this.abasEdicao.update(tabs => [...tabs, novaAba]);
        this.abaAtivaId.set(c.id!);
        this.colaboradorForm.set(this.clonarDetalhe(detalhe));
        this.formOriginal = this.clonarDetalhe(detalhe);
        this.erro.set(''); this.errosCampos.set({});
        this.isDirty.set(false); this.modoEdicao.set(true);
        this.abaFormAtiva.set('dados');
        this.modo.set('form');
      },
      error: () => this.carregando.set(false)
    });
  }

  ativarAba(id: number) {
    this.salvarEstadoAbaAtiva();
    this.abaAtivaId.set(id);
    const aba = this.abasEdicao().find(a => a.colaborador.id === id);
    if (!aba) return;
    this.colaboradorSelecionado.set(aba.colaborador);
    this.colaboradorForm.set(this.clonarDetalhe(aba.form));
    this.formOriginal = this.clonarDetalhe(aba.form);
    this.acessoHabilitado.set(!!aba.form.acesso);
    this.isDirty.set(aba.isDirty);
    this.errosCampos.set({}); this.erro.set('');
    this.modoEdicao.set(true); this.modo.set('form');
  }

  fecharAba(id: number) {
    const eraAtiva = this.abaAtivaId() === id && this.modo() === 'form';
    this.abasEdicao.update(tabs => tabs.filter(t => t.colaborador.id !== id));
    if (eraAtiva) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) this.ativarAba(restantes[restantes.length - 1].colaborador.id!);
      else { this.abaAtivaId.set(null); this.modo.set('lista'); }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (id == null) return;
    const form = this.colaboradorForm();
    const dirty = this.isDirty();
    this.abasEdicao.update(tabs =>
      tabs.map(t => t.colaborador.id === id ? { ...t, form: this.clonarDetalhe(form), isDirty: dirty } : t)
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    if (!this.validar()) return;
    this.erro.set('');
    const f = this.colaboradorForm();
    this.salvando.set(true);

    const acesso = this.acessoHabilitado() && f.acesso?.login
      ? { login: f.acesso.login, senha: f.acesso.senha || null,
          isAdministrador: f.acesso.isAdministrador,
          sessaoMaximaMinutos: f.acesso.sessaoMaximaMinutos || 0,
          inatividadeMinutos: f.acesso.inatividadeMinutos || 0,
          filialPadraoId: f.acesso.filialPadraoId || 0,
          filialGrupos: (f.acesso.filialGrupos || []).map(fg => ({ filialId: fg.filialId, grupoUsuarioId: fg.grupoUsuarioId })) }
      : null;

    const payload: any = {
      nome: f.nome, cpf: f.cpf, rg: f.rg,
      dataNascimento: f.dataNascimento || null,
      cargo: f.cargo, dataAdmissao: f.dataAdmissao || null,
      salario: f.salario, observacao: f.observacao, ativo: f.ativo,
      enderecos: f.enderecos, contatos: f.contatos,
      acesso
    };

    const headers = this.headerLiberacao();
    const req = this.modoEdicao()
      ? this.http.put<any>(`${this.apiUrl}/${f.id}`, payload, { headers })
      : this.http.post<any>(this.apiUrl, payload, { headers });

    req.subscribe({
      next: (r) => {
        this.salvando.set(false);
        this.carregar();
        if (!this.modoEdicao() && r.data) {
          // Recarregar detalhe do novo registro
          this.http.get<any>(`${this.apiUrl}/${r.data.id}`).subscribe({
            next: det => {
              const detalhe: ColaboradorDetalhe = {
                ...r.data,
                enderecos: det.data.enderecos ?? [],
                contatos: det.data.contatos ?? [],
                observacao: det.data.observacao,
                cargo: det.data.cargo,
                dataAdmissao: det.data.dataAdmissao,
                salario: det.data.salario
              };
              const novaAba: AbaEdicao = { colaborador: { ...r.data }, form: this.clonarDetalhe(detalhe), isDirty: false };
              this.abasEdicao.update(tabs => [...tabs, novaAba]);
              this.abaAtivaId.set(r.data.id);
              this.colaboradorSelecionado.set(r.data);
              this.colaboradorForm.set(this.clonarDetalhe(detalhe));
              this.formOriginal = this.clonarDetalhe(detalhe);
              this.modoEdicao.set(true);
              this.isDirty.set(false); this.errosCampos.set({});
            }
          });
        } else {
          const id = this.abaAtivaId();
          if (id != null) {
            // Recarregar detalhe atualizado
            this.http.get<any>(`${this.apiUrl}/${id}`).subscribe({
              next: det => {
                const detalhe: ColaboradorDetalhe = {
                  ...f, id,
                  enderecos: det.data.enderecos ?? [],
                  contatos: det.data.contatos ?? [],
                  observacao: det.data.observacao,
                  cargo: det.data.cargo,
                  dataAdmissao: det.data.dataAdmissao,
                  salario: det.data.salario
                };
                this.abasEdicao.update(tabs =>
                  tabs.map(t => t.colaborador.id === id
                    ? { ...t, colaborador: { ...f }, form: this.clonarDetalhe(detalhe), isDirty: false } : t)
                );
                this.colaboradorForm.set(this.clonarDetalhe(detalhe));
                this.formOriginal = this.clonarDetalhe(detalhe);
              }
            });
          }
          this.isDirty.set(false); this.errosCampos.set({});
        }
      },
      error: (e) => {
        this.salvando.set(false);
        this.erro.set(e?.error?.message ?? 'Erro ao salvar colaborador.');
      }
    });
  }

  cancelarEdicao() {
    if (this.formOriginal) this.colaboradorForm.set(this.clonarDetalhe(this.formOriginal));
    this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({});
    const id = this.abaAtivaId();
    if (id != null) {
      this.abasEdicao.update(tabs =>
        tabs.map(t => t.colaborador.id === id ? { ...t, form: this.clonarDetalhe(this.formOriginal!), isDirty: false } : t)
      );
    }
  }

  fecharForm() {
    const id = this.abaAtivaId();
    if (this.modoEdicao() && id != null) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.colaborador.id !== id));
      this.abaAtivaId.set(null);
    }
    this.modo.set('lista');
  }

  fechar() { this.salvarEstadoAbaAtiva(); this.modo.set('lista'); this.carregar(); }

  // ── Excluir ───────────────────────────────────────────────────────
  async excluir() {
    const c = this.colaboradorSelecionado();
    if (!c?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusão',
      `Deseja excluir o colaborador ${c.nome}? O registro será removido permanentemente. Se estiver em uso, será apenas desativado.`,
      'Sim, excluir',
      'Não, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${c.id}`, { headers }).subscribe({
      next: async (r) => {
        this.excluindo.set(false);
        if (c.id) this.abasEdicao.update(tabs => tabs.filter(t => t.colaborador.id !== c.id));
        this.colaboradorSelecionado.set(null);
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
        this.modal.erro('Erro', e?.error?.message ?? 'Erro ao excluir colaborador.');
      }
    });
  }

  // ── Log ───────────────────────────────────────────────────────────
  abrirLog() {
    const c = this.colaboradorSelecionado();
    if (!c?.id) return;
    this.logDataInicio.set(this.hoje(-30));
    this.logDataFim.set(this.hoje(0));
    this.modalLog.set(true);
    this.filtrarLog();
  }

  filtrarLog() {
    const c = this.colaboradorSelecionado();
    if (!c?.id) return;
    this.carregandoLog.set(true);
    this.logSelecionado.set(null);
    const params = `dataInicio=${this.logDataInicio()}&dataFim=${this.logDataFim()}`;
    this.http.get<any>(`${this.apiUrl}/${c.id}/log?${params}`).subscribe({
      next: r => {
        const lista: LogEntry[] = r.data ?? [];
        this.logRegistros.set(lista);
        this.logSelecionado.set(lista.length > 0 ? lista[0] : null);
        this.carregandoLog.set(false);
      },
      error: () => this.carregandoLog.set(false)
    });
  }

  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }
  fecharLog() { this.modalLog.set(false); }

  acaoCss(acao: string): string {
    if (acao === 'CRIAÇÃO')     return 'badge-criacao';
    if (acao === 'ALTERAÇÃO')   return 'badge-alteracao';
    if (acao === 'EXCLUSÃO')    return 'badge-exclusao';
    if (acao === 'DESATIVAÇÃO') return 'badge-desativacao';
    return '';
  }

  // ── Formulário ────────────────────────────────────────────────────
  updateForm(campo: string, valor: any) {
    this.colaboradorForm.update(f => ({ ...f, [campo]: valor }));
    this.isDirty.set(true);
    if (this.errosCampos()[campo]) {
      this.errosCampos.update(e => { const n = { ...e }; delete n[campo]; return n; });
    }
  }

  // ── Endereços ─────────────────────────────────────────────────────
  adicionarEndereco() {
    this.colaboradorForm.update(f => ({
      ...f,
      enderecos: [...f.enderecos, { tipo: 'PRINCIPAL', cep: '', rua: '', numero: '', bairro: '', cidade: '', uf: '', principal: f.enderecos.length === 0 }]
    }));
    this.isDirty.set(true);
  }

  removerEndereco(idx: number) {
    this.colaboradorForm.update(f => ({
      ...f,
      enderecos: f.enderecos.filter((_, i) => i !== idx)
    }));
    this.isDirty.set(true);
  }

  updateEndereco(idx: number, campo: string, valor: any) {
    this.colaboradorForm.update(f => ({
      ...f,
      enderecos: f.enderecos.map((e, i) => i === idx ? { ...e, [campo]: valor } : e)
    }));
    this.isDirty.set(true);
  }

  onCepEnderecoInput(event: Event, idx: number) {
    const input = event.target as HTMLInputElement;
    const mascarado = this.mascaraCep(input.value);
    input.value = mascarado;
    this.updateEndereco(idx, 'cep', mascarado);
    const digits = mascarado.replace(/\D/g, '');
    if (digits.length === 8) this.buscarCepEndereco(digits, idx);
  }

  private buscarCepEndereco(cep: string, idx: number) {
    this.buscandoCep.set(true);
    this.http.get<any>(`https://viacep.com.br/ws/${cep}/json/`).subscribe({
      next: (r) => {
        this.buscandoCep.set(false);
        if (r.erro) return;
        this.colaboradorForm.update(f => ({
          ...f,
          enderecos: f.enderecos.map((e, i) => i === idx ? {
            ...e,
            rua: r.logradouro ?? e.rua,
            bairro: r.bairro ?? e.bairro,
            cidade: r.localidade ?? e.cidade,
            uf: r.uf ?? e.uf
          } : e)
        }));
      },
      error: () => this.buscandoCep.set(false)
    });
  }

  // ── Contatos ──────────────────────────────────────────────────────
  adicionarContato() {
    this.colaboradorForm.update(f => ({
      ...f,
      contatos: [...f.contatos, { tipo: 'CELULAR', valor: '', principal: f.contatos.length === 0 }]
    }));
    this.isDirty.set(true);
  }

  removerContato(idx: number) {
    this.colaboradorForm.update(f => ({
      ...f,
      contatos: f.contatos.filter((_, i) => i !== idx)
    }));
    this.isDirty.set(true);
  }

  updateContato(idx: number, campo: string, valor: any) {
    this.colaboradorForm.update(f => ({
      ...f,
      contatos: f.contatos.map((c, i) => i === idx ? { ...c, [campo]: valor } : c)
    }));
    this.isDirty.set(true);
  }

  onContatoValorInput(event: Event, idx: number) {
    const input = event.target as HTMLInputElement;
    const tipo = this.colaboradorForm().contatos[idx].tipo;
    let mascarado = input.value;
    if (tipo === 'TELEFONE' || tipo === 'CELULAR' || tipo === 'WHATSAPP') {
      mascarado = this.mascaraTelefone(input.value);
    }
    input.value = mascarado;
    this.updateContato(idx, 'valor', mascarado);
  }

  // ── Acesso ────────────────────────────────────────────────────────
  toggleAcesso(habilitado: boolean) {
    this.acessoHabilitado.set(habilitado);
    if (habilitado && !this.colaboradorForm().acesso) {
      this.colaboradorForm.update(f => ({
        ...f,
        acesso: { login: '', senha: '', isAdministrador: false, sessaoMaximaMinutos: 0, inatividadeMinutos: 0, filialPadraoId: 0, filialGrupos: [] }
      }));
    }
    this.isDirty.set(true);
  }

  updateAcesso(campo: string, valor: any) {
    this.colaboradorForm.update(f => ({
      ...f,
      acesso: f.acesso ? { ...f.acesso, [campo]: valor } : null
    }));
    this.isDirty.set(true);
  }

  getGruposFilial(filialId: number): FilialGrupoItem[] {
    return (this.colaboradorForm().acesso?.filialGrupos || []).filter(x => x.filialId === filialId);
  }

  temGrupoFilial(filialId: number, grupoId: number): boolean {
    return (this.colaboradorForm().acesso?.filialGrupos || []).some(x => x.filialId === filialId && x.grupoUsuarioId === grupoId);
  }

  toggleMultiselect(filialId: number, event?: MouseEvent) {
    this.multiSelectAberto.update(v => v === filialId ? null : filialId);
  }

  devemAbrirCima(filialId: number): boolean {
    const row = document.querySelector(`.acesso-grid tbody tr:has(.multiselect-wrap [class*="display"]:focus-within), .acesso-grid tbody tr:nth-child(${this.filiais().findIndex(f => f.id === filialId) + 1})`);
    if (!row) return false;
    const rect = row.getBoundingClientRect();
    const espacoAbaixo = window.innerHeight - rect.bottom;
    return espacoAbaixo < 220;
  }

  getGrupoNome(grupoId: number): string {
    return this.grupos().find(g => g.id === grupoId)?.nome ?? '';
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (this.multiSelectAberto() !== null) {
      const target = event.target as HTMLElement;
      if (!target.closest('.multiselect-wrap')) {
        this.multiSelectAberto.set(null);
      }
    }
  }

  toggleGrupoFilial(filialId: number, grupoId: number, checked: boolean) {
    this.colaboradorForm.update(f => {
      if (!f.acesso) return f;
      let fgs = [...(f.acesso.filialGrupos || [])];
      if (checked) {
        if (!fgs.some(x => x.filialId === filialId && x.grupoUsuarioId === grupoId)) {
          fgs.push({ filialId, grupoUsuarioId: grupoId });
        }
      } else {
        fgs = fgs.filter(x => !(x.filialId === filialId && x.grupoUsuarioId === grupoId));
      }
      return { ...f, acesso: { ...f.acesso, filialGrupos: fgs } };
    });
    this.isDirty.set(true);
  }

  // ── Máscaras ──────────────────────────────────────────────────────
  onCpfInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const mascarado = this.mascaraCpf(input.value);
    input.value = mascarado;
    this.updateForm('cpf', mascarado);
  }

  private mascaraCpf(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 11);
    if (d.length <= 3)  return d;
    if (d.length <= 6)  return `${d.slice(0,3)}.${d.slice(3)}`;
    if (d.length <= 9)  return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6)}`;
    return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6,9)}-${d.slice(9)}`;
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

  onSalarioInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '');
    const num = parseInt(digits || '0', 10) / 100;
    this.updateForm('salario', num);
    input.value = num.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatarSalario(valor?: number): string {
    if (valor == null) return '';
    return valor.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  // ── Validação ─────────────────────────────────────────────────────
  private validar(): boolean {
    const f = this.colaboradorForm();
    const erros: Record<string, string> = {};
    if (!f.nome?.trim())   erros['nome'] = 'Obrigatório';
    if (!f.cpf?.trim())    erros['cpf']  = 'Obrigatório';
    if (f.enderecos.length === 0) erros['enderecos'] = 'Informe pelo menos um endereço';
    for (let i = 0; i < f.enderecos.length; i++) {
      const e = f.enderecos[i];
      if (!e.cep?.trim())    erros[`end_cep_${i}`]    = 'Obrigatório';
      if (!e.rua?.trim())    erros[`end_rua_${i}`]    = 'Obrigatório';
      if (!e.numero?.trim()) erros[`end_numero_${i}`] = 'Obrigatório';
      if (!e.bairro?.trim()) erros[`end_bairro_${i}`] = 'Obrigatório';
      if (!e.cidade?.trim()) erros[`end_cidade_${i}`] = 'Obrigatório';
      if (!e.uf?.trim())     erros[`end_uf_${i}`]     = 'Obrigatório';
    }
    this.errosCampos.set(erros);
    if (Object.keys(erros).length > 0) {
      this.erro.set('Preencha todos os campos obrigatórios.');
      if (erros['enderecos'] || Object.keys(erros).some(k => k.startsWith('end_'))) {
        this.abaFormAtiva.set('endereco');
      }
      return false;
    }
    this.erro.set('');
    return true;
  }

  erroCampo(campo: string): string { return this.errosCampos()[campo] ?? ''; }
  hasEnderecoErrors(): boolean { return Object.keys(this.errosCampos()).some(k => k.startsWith('end_')); }

  // ── Helpers ───────────────────────────────────────────────────────
  private novoColaborador(): ColaboradorDetalhe {
    return {
      nome: '', cpf: '', rg: '', dataNascimento: '', cargo: '', dataAdmissao: '',
      salario: undefined, observacao: '', ativo: true,
      enderecos: [{ tipo: 'PRINCIPAL', cep: '', rua: '', numero: '', bairro: '', cidade: '', uf: '', principal: true }],
      contatos: [],
      acesso: null
    };
  }

  private clonarDetalhe(d: ColaboradorDetalhe): ColaboradorDetalhe {
    return {
      ...d,
      enderecos: d.enderecos.map(e => ({ ...e })),
      contatos: d.contatos.map(c => ({ ...c })),
      acesso: d.acesso ? { ...d.acesso, filialGrupos: (d.acesso.filialGrupos || []).map(fg => ({ ...fg })) } : null
    };
  }

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
