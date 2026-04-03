import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { FORNECEDORES_COLUNAS, ColunaDef } from './fornecedores.columns';
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

interface Fornecedor {
  id?: number; tipo: string; nome: string; razaoSocial?: string; cpfCnpj: string;
  inscricaoEstadual?: string; rg?: string;
  email?: string; telefone?: string; cidade?: string; uf?: string;
  ativo: boolean; criadoEm?: string;
}

interface FornecedorDetalhe extends Fornecedor {
  dataNascimento?: string;
  observacao?: string;
  enderecos: Endereco[];
  contatos: Contato[];
}

interface AbaEdicao { fornecedor: Fornecedor; form: FornecedorDetalhe; isDirty: boolean; }
interface ColunaEstado extends ColunaDef { visivel: boolean; }

type Modo = 'lista' | 'form';
type AbaForm = 'dados' | 'endereco' | 'contato';

@Component({
  selector: 'app-fornecedores',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './fornecedores.component.html',
  styleUrl: './fornecedores.component.scss'
})
export class FornecedoresComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_fornecedores_state';
  modo = signal<Modo>('lista');
  fornecedores = signal<Fornecedor[]>([]);
  fornecedorSelecionado = signal<Fornecedor | null>(null);
  fornecedorForm = signal<FornecedorDetalhe>(this.novoFornecedor());
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
  private formOriginal: FornecedorDetalhe | null = null;

  abaFormAtiva = signal<AbaForm>('dados');

  // Modais
  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  carregandoLog = signal(false);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal<string>(this.hoje(-30));
  logDataFim    = signal<string>(this.hoje(0));

  // Colunas
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_fornecedores';
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;

  private apiUrl = `${environment.apiUrl}/fornecedores`;

  tiposContato = ['TELEFONE', 'CELULAR', 'EMAIL', 'WHATSAPP', 'OUTRO'];
  tiposEndereco = ['PRINCIPAL', 'ENTREGA', 'COBRANCA', 'OUTRO'];

  private tokenLiberacao: string | null = null;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('fornecedores', acao)) return true;
    const resultado = await this.modal.permissao('fornecedores', acao);
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
      abasIds: abas.map(a => a.fornecedor.id),
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
          const f = this.fornecedores().find(x => x.id === id);
          if (f) this.restaurarAba(f, id === state.abaAtivaId);
        }
      }
    } catch {}
  }

  private restaurarAba(f: any, ativar: boolean) {
    this.http.get<any>(`${this.apiUrl}/${f.id}`).subscribe({
      next: r => {
        if (this.abasEdicao().find(a => a.fornecedor.id === f.id)) return;
        const detalhe: FornecedorDetalhe = {
          ...f,
          enderecos: r.data.enderecos ?? [],
          contatos: r.data.contatos ?? [],
          observacao: r.data.observacao,
          dataNascimento: r.data.dataNascimento,
          razaoSocial: r.data.razaoSocial,
          inscricaoEstadual: r.data.inscricaoEstadual,
          rg: r.data.rg
        };
        const novaAba: AbaEdicao = { fornecedor: { ...f }, form: this.clonarDetalhe(detalhe), isDirty: false };
        this.abasEdicao.update(tabs => [...tabs, novaAba]);
        if (ativar) this.ativarAba(f.id!);
      },
      error: () => { /* silently ignore - tab just won't restore */ }
    });
  }

  private primeiroCarregamento = true;

  // ── Dados ─────────────────────────────────────────────────────────
  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.fornecedores.set(r.data ?? []);
        this.carregando.set(false);
        if (this.primeiroCarregamento) {
          this.primeiroCarregamento = false;
          this.restaurarEstado();
        }
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('fornecedores', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  fornecedoresFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.fornecedores().filter(f => {
      if (status === 'ativos'   && !f.ativo) return false;
      if (status === 'inativos' &&  f.ativo) return false;
      if (termo.length < 3) return true;
      const termoDigitos = termo.replace(/\D/g, '');
      return (
        this.normalizar(f.nome).includes(termo) ||
        this.normalizar(f.razaoSocial ?? '').includes(termo) ||
        (termoDigitos.length > 0 && f.cpfCnpj.replace(/\D/g, '').includes(termoDigitos)) ||
        this.normalizar(f.cidade ?? '').includes(termo)
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

  getCellValue(f: Fornecedor, campo: string): string {
    const v = (f as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'Nao';
    if (campo === 'tipo') return v === 'F' ? 'PF' : 'PJ';
    return v ?? '';
  }

  selecionar(f: Fornecedor) { this.fornecedorSelecionado.set(f); }
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
    const def = FORNECEDORES_COLUNAS.find(c => c.campo === this.resizeState!.campo);
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
    this.colunas.set(FORNECEDORES_COLUNAS.map(d => ({ ...d, visivel: d.padrao })));
    localStorage.removeItem(this.STORAGE_KEY_COLUNAS);
  }

  private carregarColunas(): ColunaEstado[] {
    let salvo: { campo: string; visivel: boolean; largura: number }[] = [];
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) salvo = JSON.parse(json);
    } catch {}
    return FORNECEDORES_COLUNAS.map(def => {
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
    const novo = this.novoFornecedor();
    this.fornecedorForm.set(novo);
    this.formOriginal = { ...novo, enderecos: [...novo.enderecos.map(e => ({ ...e }))], contatos: [...novo.contatos.map(c => ({ ...c }))] };
    this.erro.set(''); this.errosCampos.set({});
    this.isDirty.set(false); this.modoEdicao.set(false); this.pessoaEncontrada.set(null);
    this.abaAtivaId.set(null); this.abaFormAtiva.set('dados');
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const f = this.fornecedorSelecionado();
    if (!f?.id) return;
    const jaAberta = this.abasEdicao().find(a => a.fornecedor.id === f.id);
    if (jaAberta) { this.ativarAba(f.id!); return; }

    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${f.id}`).subscribe({
      next: r => {
        this.carregando.set(false);
        const detalhe: FornecedorDetalhe = {
          ...f,
          enderecos: r.data.enderecos ?? [],
          contatos: r.data.contatos ?? [],
          observacao: r.data.observacao,
          dataNascimento: r.data.dataNascimento,
          razaoSocial: r.data.razaoSocial,
          inscricaoEstadual: r.data.inscricaoEstadual,
          rg: r.data.rg
        };
        this.salvarEstadoAbaAtiva();
        const novaAba: AbaEdicao = { fornecedor: { ...f }, form: this.clonarDetalhe(detalhe), isDirty: false };
        this.abasEdicao.update(tabs => [...tabs, novaAba]);
        this.abaAtivaId.set(f.id!);
        this.fornecedorForm.set(this.clonarDetalhe(detalhe));
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
    const aba = this.abasEdicao().find(a => a.fornecedor.id === id);
    if (!aba) return;
    this.fornecedorSelecionado.set(aba.fornecedor);
    this.fornecedorForm.set(this.clonarDetalhe(aba.form));
    this.formOriginal = this.clonarDetalhe(aba.form);
    this.isDirty.set(aba.isDirty);
    this.errosCampos.set({}); this.erro.set('');
    this.modoEdicao.set(true); this.modo.set('form');
  }

  fecharAba(id: number) {
    const eraAtiva = this.abaAtivaId() === id && this.modo() === 'form';
    this.abasEdicao.update(tabs => tabs.filter(t => t.fornecedor.id !== id));
    if (eraAtiva) {
      const restantes = this.abasEdicao();
      if (restantes.length > 0) this.ativarAba(restantes[restantes.length - 1].fornecedor.id!);
      else { this.abaAtivaId.set(null); this.modo.set('lista'); }
    }
  }

  private salvarEstadoAbaAtiva() {
    const id = this.abaAtivaId();
    if (id == null) return;
    const form = this.fornecedorForm();
    const dirty = this.isDirty();
    this.abasEdicao.update(tabs =>
      tabs.map(t => t.fornecedor.id === id ? { ...t, form: this.clonarDetalhe(form), isDirty: dirty } : t)
    );
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;
    if (!this.validar()) return;
    this.erro.set('');
    const f = this.fornecedorForm();
    this.salvando.set(true);

    const payload: any = {
      tipo: f.tipo, nome: f.nome, razaoSocial: f.razaoSocial,
      cpfCnpj: f.cpfCnpj, inscricaoEstadual: f.inscricaoEstadual,
      rg: f.rg, dataNascimento: f.dataNascimento || null,
      observacao: f.observacao, ativo: f.ativo,
      enderecos: f.enderecos, contatos: f.contatos
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
          this.http.get<any>(`${this.apiUrl}/${r.data.id}`).subscribe({
            next: det => {
              const detalhe: FornecedorDetalhe = {
                ...r.data,
                enderecos: det.data.enderecos ?? [],
                contatos: det.data.contatos ?? [],
                observacao: det.data.observacao,
                razaoSocial: det.data.razaoSocial,
                inscricaoEstadual: det.data.inscricaoEstadual,
                rg: det.data.rg,
                dataNascimento: det.data.dataNascimento
              };
              const novaAba: AbaEdicao = { fornecedor: { ...r.data }, form: this.clonarDetalhe(detalhe), isDirty: false };
              this.abasEdicao.update(tabs => [...tabs, novaAba]);
              this.abaAtivaId.set(r.data.id);
              this.fornecedorSelecionado.set(r.data);
              this.fornecedorForm.set(this.clonarDetalhe(detalhe));
              this.formOriginal = this.clonarDetalhe(detalhe);
              this.modoEdicao.set(true);
              this.isDirty.set(false); this.errosCampos.set({});
            }
          });
        } else {
          const id = this.abaAtivaId();
          if (id != null) {
            this.http.get<any>(`${this.apiUrl}/${id}`).subscribe({
              next: det => {
                const detalhe: FornecedorDetalhe = {
                  ...f, id,
                  enderecos: det.data.enderecos ?? [],
                  contatos: det.data.contatos ?? [],
                  observacao: det.data.observacao,
                  razaoSocial: det.data.razaoSocial,
                  inscricaoEstadual: det.data.inscricaoEstadual,
                  rg: det.data.rg,
                  dataNascimento: det.data.dataNascimento
                };
                this.abasEdicao.update(tabs =>
                  tabs.map(t => t.fornecedor.id === id
                    ? { ...t, fornecedor: { ...f }, form: this.clonarDetalhe(detalhe), isDirty: false } : t)
                );
                this.fornecedorForm.set(this.clonarDetalhe(detalhe));
                this.formOriginal = this.clonarDetalhe(detalhe);
              }
            });
          }
          this.isDirty.set(false); this.errosCampos.set({});
        }
      },
      error: (e) => {
        this.salvando.set(false);
        this.erro.set(e?.error?.message ?? 'Erro ao salvar fornecedor.');
      }
    });
  }

  cancelarEdicao() {
    if (this.formOriginal) this.fornecedorForm.set(this.clonarDetalhe(this.formOriginal));
    this.isDirty.set(false); this.erro.set(''); this.errosCampos.set({});
    const id = this.abaAtivaId();
    if (id != null) {
      this.abasEdicao.update(tabs =>
        tabs.map(t => t.fornecedor.id === id ? { ...t, form: this.clonarDetalhe(this.formOriginal!), isDirty: false } : t)
      );
    }
  }

  fecharForm() {
    const id = this.abaAtivaId();
    if (this.modoEdicao() && id != null) {
      this.abasEdicao.update(tabs => tabs.filter(t => t.fornecedor.id !== id));
      this.abaAtivaId.set(null);
    }
    this.modo.set('lista');
  }

  fechar() { this.salvarEstadoAbaAtiva(); this.modo.set('lista'); this.carregar(); }

  // ── Excluir ───────────────────────────────────────────────────────
  async excluir() {
    const f = this.fornecedorSelecionado();
    if (!f?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclusao',
      `Deseja excluir o fornecedor ${f.nome}? O registro sera removido permanentemente. Se estiver em uso, sera apenas desativado.`,
      'Sim, excluir',
      'Nao, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${f.id}`, { headers }).subscribe({
      next: async (r) => {
        this.excluindo.set(false);
        if (f.id) this.abasEdicao.update(tabs => tabs.filter(t => t.fornecedor.id !== f.id));
        this.fornecedorSelecionado.set(null);
        this.abaAtivaId.set(null);
        this.modo.set('lista');
        this.carregar();
        const tipo = r?.resultado ?? 'excluido';
        if (tipo === 'excluido') {
          await this.modal.sucesso('Excluido', 'Registro excluido com sucesso.');
        } else {
          await this.modal.aviso('Desativado', 'O registro esta em uso e foi apenas desativado.');
        }
      },
      error: (e) => {
        this.excluindo.set(false);
        this.modal.erro('Erro', e?.error?.message ?? 'Erro ao excluir fornecedor.');
      }
    });
  }

  // ── Log ───────────────────────────────────────────────────────────
  abrirLog() {
    const f = this.fornecedorSelecionado();
    if (!f?.id) return;
    this.logDataInicio.set(this.hoje(-30));
    this.logDataFim.set(this.hoje(0));
    this.modalLog.set(true);
    this.filtrarLog();
  }

  filtrarLog() {
    const f = this.fornecedorSelecionado();
    if (!f?.id) return;
    this.carregandoLog.set(true);
    this.logSelecionado.set(null);
    const params = `dataInicio=${this.logDataInicio()}&dataFim=${this.logDataFim()}`;
    this.http.get<any>(`${this.apiUrl}/${f.id}/log?${params}`).subscribe({
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
    if (acao === 'CRIACAO')     return 'badge-criacao';
    if (acao === 'ALTERACAO')   return 'badge-alteracao';
    if (acao === 'EXCLUSAO')    return 'badge-exclusao';
    if (acao === 'DESATIVACAO') return 'badge-desativacao';
    return '';
  }

  // ── Formulario ────────────────────────────────────────────────────
  updateForm(campo: string, valor: any) {
    this.fornecedorForm.update(f => ({ ...f, [campo]: valor }));
    this.isDirty.set(true);
    if (this.errosCampos()[campo]) {
      this.errosCampos.update(e => { const n = { ...e }; delete n[campo]; return n; });
    }
  }

  onTipoChange(tipo: string) {
    this.fornecedorForm.update(f => ({
      ...f,
      tipo,
      cpfCnpj: '',
      razaoSocial: tipo === 'F' ? undefined : f.razaoSocial,
      inscricaoEstadual: tipo === 'F' ? undefined : f.inscricaoEstadual,
      rg: tipo === 'J' ? undefined : f.rg,
      dataNascimento: tipo === 'J' ? undefined : f.dataNascimento
    }));
    this.isDirty.set(true);
  }

  // ── Enderecos ─────────────────────────────────────────────────────
  adicionarEndereco() {
    this.fornecedorForm.update(f => ({
      ...f,
      enderecos: [...f.enderecos, { tipo: 'PRINCIPAL', cep: '', rua: '', numero: '', bairro: '', cidade: '', uf: '', principal: f.enderecos.length === 0 }]
    }));
    this.isDirty.set(true);
  }

  removerEndereco(idx: number) {
    this.fornecedorForm.update(f => ({
      ...f,
      enderecos: f.enderecos.filter((_, i) => i !== idx)
    }));
    this.isDirty.set(true);
  }

  updateEndereco(idx: number, campo: string, valor: any) {
    this.fornecedorForm.update(f => ({
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
        this.fornecedorForm.update(f => ({
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
    this.fornecedorForm.update(f => ({
      ...f,
      contatos: [...f.contatos, { tipo: 'CELULAR', valor: '', principal: f.contatos.length === 0 }]
    }));
    this.isDirty.set(true);
  }

  removerContato(idx: number) {
    this.fornecedorForm.update(f => ({
      ...f,
      contatos: f.contatos.filter((_, i) => i !== idx)
    }));
    this.isDirty.set(true);
  }

  updateContato(idx: number, campo: string, valor: any) {
    this.fornecedorForm.update(f => ({
      ...f,
      contatos: f.contatos.map((c, i) => i === idx ? { ...c, [campo]: valor } : c)
    }));
    this.isDirty.set(true);
  }

  onContatoValorInput(event: Event, idx: number) {
    const input = event.target as HTMLInputElement;
    const tipo = this.fornecedorForm().contatos[idx].tipo;
    let mascarado = input.value;
    if (tipo === 'TELEFONE' || tipo === 'CELULAR' || tipo === 'WHATSAPP') {
      mascarado = this.mascaraTelefone(input.value);
    }
    input.value = mascarado;
    this.updateContato(idx, 'valor', mascarado);
  }

  // ── Busca CNPJ ───────────────────────────────────────────────────
  buscandoCnpj = signal(false);
  pessoaEncontrada = signal<any>(null);

  onCpfCnpjBlur() {
    if (this.modoEdicao()) return;

    const cpfCnpj = this.fornecedorForm().cpfCnpj;
    const digits = cpfCnpj.replace(/\D/g, '');
    if (digits.length !== 11 && digits.length !== 14) {
      this.pessoaEncontrada.set(null);
      return;
    }

    this.http.get<any>(`${environment.apiUrl}/pessoas/buscar-cpfcnpj?valor=${cpfCnpj}`).subscribe({
      next: r => {
        const p = r?.data;
        if (!p) {
          this.pessoaEncontrada.set(null);
          return;
        }

        if (p.temFornecedor) {
          this.erro.set('Este CPF/CNPJ já possui um fornecedor cadastrado.');
          this.pessoaEncontrada.set(null);
          return;
        }

        this.pessoaEncontrada.set(p);
        this.fornecedorForm.update(f => ({
          ...f,
          nome: p.nome || f.nome,
          razaoSocial: p.razaoSocial || f.razaoSocial,
          inscricaoEstadual: p.inscricaoEstadual || f.inscricaoEstadual,
          rg: p.rg || f.rg,
          dataNascimento: p.dataNascimento ? p.dataNascimento.slice(0, 10) : f.dataNascimento,
          observacao: p.observacao || f.observacao
        }));

        const form = this.fornecedorForm();
        const noEnderecos = form.enderecos.length <= 1 && !form.enderecos[0]?.cep;
        if (p.enderecos?.length > 0 && noEnderecos) {
          this.fornecedorForm.update(f => ({
            ...f,
            enderecos: p.enderecos.map((e: any) => ({
              id: e.id, tipo: e.tipo, cep: e.cep, rua: e.rua, numero: e.numero,
              complemento: e.complemento, bairro: e.bairro, cidade: e.cidade, uf: e.uf, principal: e.principal
            }))
          }));
        }

        if (p.contatos?.length > 0 && form.contatos.length === 0) {
          this.fornecedorForm.update(f => ({
            ...f,
            contatos: p.contatos.map((c: any) => ({
              id: c.id, tipo: c.tipo, valor: c.valor, descricao: c.descricao, principal: c.principal
            }))
          }));
        }
      },
      error: () => this.pessoaEncontrada.set(null)
    });
  }

  onCpfCnpjInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const tipo = this.fornecedorForm().tipo;
    const mascarado = tipo === 'F' ? this.mascaraCpf(input.value) : this.mascaraCnpj(input.value);
    input.value = mascarado;
    this.updateForm('cpfCnpj', mascarado);

    // Auto-busca CNPJ quando completo (14 dígitos)
    if (tipo === 'J') {
      const digits = mascarado.replace(/\D/g, '');
      if (digits.length === 14) this.buscarCnpj(digits);
    }
  }

  private buscarCnpj(cnpj: string) {
    this.buscandoCnpj.set(true);

    // Serviço 1: BrasilAPI
    this.http.get<any>(`https://brasilapi.com.br/api/cnpj/v1/${cnpj}`).subscribe({
      next: r => {
        this.buscandoCnpj.set(false);
        if (r.razao_social) {
          this.aplicarDadosCnpj(r.razao_social, r.nome_fantasia, r.cep, r.logradouro,
            r.numero, r.complemento, r.bairro, r.municipio, r.uf);
        }
      },
      error: () => {
        // Fallback: ReceitaWS
        this.http.get<any>(`https://receitaws.com.br/v1/cnpj/${cnpj}`).subscribe({
          next: r => {
            this.buscandoCnpj.set(false);
            if (r.nome && r.status !== 'ERROR') {
              this.aplicarDadosCnpj(r.nome, r.fantasia, r.cep, r.logradouro,
                r.numero, r.complemento, r.bairro, r.municipio, r.uf);
            }
          },
          error: () => this.buscandoCnpj.set(false)
        });
      }
    });
  }

  private aplicarDadosCnpj(razaoSocial: string, nomeFantasia: string, cep: string,
    rua: string, numero: string, complemento: string, bairro: string, cidade: string, uf: string) {
    this.fornecedorForm.update(f => ({
      ...f,
      razaoSocial: razaoSocial?.toUpperCase() || f.razaoSocial,
      nome: nomeFantasia?.toUpperCase() || razaoSocial?.toUpperCase() || f.nome,
    }));
    this.isDirty.set(true);

    // Preencher endereço se houver dados e endereço vazio
    if (cep && this.fornecedorForm().enderecos.length > 0) {
      const end = this.fornecedorForm().enderecos[0];
      if (!end.cep || !end.rua) {
        this.fornecedorForm.update(f => ({
          ...f,
          enderecos: f.enderecos.map((e, i) => i === 0 ? {
            ...e,
            cep: this.mascaraCep(cep || ''),
            rua: rua?.toUpperCase() || e.rua,
            numero: numero || e.numero,
            complemento: complemento?.toUpperCase() || e.complemento,
            bairro: bairro?.toUpperCase() || e.bairro,
            cidade: cidade?.toUpperCase() || e.cidade,
            uf: uf?.toUpperCase() || e.uf,
          } : e)
        }));
      }
    }
  }

  private mascaraCpf(v: string): string {
    const d = v.replace(/\D/g, '').slice(0, 11);
    if (d.length <= 3)  return d;
    if (d.length <= 6)  return `${d.slice(0,3)}.${d.slice(3)}`;
    if (d.length <= 9)  return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6)}`;
    return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6,9)}-${d.slice(9)}`;
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

  // ── Validacao ─────────────────────────────────────────────────────
  private validar(): boolean {
    const f = this.fornecedorForm();
    const erros: Record<string, string> = {};

    // Campos obrigatórios da aba Dados
    if (!f.nome?.trim()) erros['nome'] = 'Obrigatório';
    if (!f.cpfCnpj?.trim()) erros['cpfCnpj'] = 'Obrigatório';
    if (f.tipo === 'J' && !f.razaoSocial?.trim()) erros['razaoSocial'] = 'Obrigatório';

    // Endereço NÃO é obrigatório para fornecedores
    // Mas se preencheu parcialmente, valida os campos preenchidos
    for (let i = 0; i < f.enderecos.length; i++) {
      const e = f.enderecos[i];
      const temAlgo = e.cep || e.rua || e.numero || e.bairro || e.cidade || e.uf;
      if (temAlgo) {
        if (!e.cep?.trim())    erros[`end_cep_${i}`]    = 'Obrigatório';
        if (!e.rua?.trim())    erros[`end_rua_${i}`]    = 'Obrigatório';
        if (!e.numero?.trim()) erros[`end_numero_${i}`] = 'Obrigatório';
        if (!e.bairro?.trim()) erros[`end_bairro_${i}`] = 'Obrigatório';
        if (!e.cidade?.trim()) erros[`end_cidade_${i}`] = 'Obrigatório';
        if (!e.uf?.trim())     erros[`end_uf_${i}`]     = 'Obrigatório';
      }
    }

    this.errosCampos.set(erros);
    if (Object.keys(erros).length > 0) {
      this.erro.set('Preencha todos os campos obrigatórios.');
      // Priorizar: primeiro volta para aba Dados se tem erros lá
      const temErroDados = erros['nome'] || erros['cpfCnpj'] || erros['razaoSocial'];
      const temErroEndereco = Object.keys(erros).some(k => k.startsWith('end_'));
      if (temErroDados) {
        this.abaFormAtiva.set('dados');
      } else if (temErroEndereco) {
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
  private novoFornecedor(): FornecedorDetalhe {
    return {
      tipo: 'J', nome: '', razaoSocial: '', cpfCnpj: '', inscricaoEstadual: '',
      rg: '', dataNascimento: '', observacao: '', ativo: true,
      enderecos: [{ tipo: 'PRINCIPAL', cep: '', rua: '', numero: '', bairro: '', cidade: '', uf: '', principal: true }],
      contatos: []
    };
  }

  private clonarDetalhe(d: FornecedorDetalhe): FornecedorDetalhe {
    return {
      ...d,
      enderecos: d.enderecos.map(e => ({ ...e })),
      contatos: d.contatos.map(c => ({ ...c }))
    };
  }

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
