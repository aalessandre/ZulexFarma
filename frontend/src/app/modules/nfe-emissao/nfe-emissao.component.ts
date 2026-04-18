import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface NaturezaOp {
  id: number;
  descricao: string;
  natOp: string;
  tipoNf: number;
  finalidadeNfe: number;
  identificadorDestino: number;
  relacionarDocumentoFiscal?: boolean;
}

interface PessoaResumo {
  id: number;
  nome: string;
  cpfCnpj: string;
}

interface ProdutoResumo {
  id: number;
  codigo: string;
  codigoBarras: string;
  nome: string;
  ncm: string;
  cest?: string;
  cfop?: string;
  origemMercadoria: string;
  cstIcms?: string;
  csosn?: string;
  cstPis: string;
  cstCofins: string;
  cstIpi?: string;
  valorVenda: number;
}

interface NfeItemForm {
  produtoId: number;
  produtoNome?: string;
  codigoProduto: string;
  codigoBarras: string;
  descricaoProduto: string;
  ncm: string;
  cest?: string;
  cfop: string;
  quantidade: number;
  valorUnitario: number;
  valorTotal: number;
  origemMercadoria: string;
  cstIcms?: string;
  csosn?: string;
  cstPis: string;
  cstCofins: string;
  cstIpi?: string;
}

interface NfeParcelaForm {
  numeroParcela: string;
  dataVencimento: string;
  valor: number;
}

interface NfeForm {
  id?: number;
  filialId: number;
  naturezaOperacaoId: number;
  destinatarioPessoaId?: number;
  natOp: string;
  tipoNf: number;
  finalidadeNfe: number;
  identificadorDestino: number;
  modFrete: number;
  chaveNfeReferenciada?: string;
  observacao?: string;
  itens: NfeItemForm[];
  parcelas: NfeParcelaForm[];
}

@Component({
  selector: 'app-nfe-emissao',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './nfe-emissao.component.html',
  styleUrl: './nfe-emissao.component.scss'
})
export class NfeEmissaoComponent implements OnInit, OnDestroy {

  // ── Form state ─────────────────────────────────────────────────────
  form = signal<NfeForm>(this.novoForm());
  salvando = signal(false);
  emitindo = signal(false);
  carregando = signal(false);
  erro = signal('');
  errosCampos = signal<Record<string, string>>({});
  isDirty = signal(false);
  modoEdicao = signal(false);

  // ── Lookups ────────────────────────────────────────────────────────
  filiais = signal<Array<{ id: number; nome: string }>>([]);
  naturezas = signal<NaturezaOp[]>([]);
  naturezaSelecionada = signal<NaturezaOp | null>(null);

  // Destinatario lookup
  destinatarioBusca = signal('');
  destinatarioResultados = signal<PessoaResumo[]>([]);
  destinatarioSelecionado = signal<PessoaResumo | null>(null);
  destinatarioAberto = signal(false);
  private destinatarioTimer: any = null;

  // Produto lookup
  produtoBusca = signal('');
  produtoResultados = signal<ProdutoResumo[]>([]);
  produtoAberto = signal(false);
  private produtoTimer: any = null;

  // Item being added
  novoItemQtd = signal('1');
  novoItemVlrUnit = signal('');

  // Totais
  totalProdutos = computed(() => this.form().itens.reduce((s, i) => s + i.valorTotal, 0));
  totalNota = computed(() => this.totalProdutos());

  private apiUrl = `${environment.apiUrl}/venda-fiscal`;
  private tokenLiberacao: string | null = null;
  private nfeId: number | null = null;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService,
    private route: ActivatedRoute
  ) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('nfe', acao)) return true;
    const resultado = await this.modal.permissao('nfe', acao);
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

  private readonly TAB_ID = '/erp/nfe-emissao';

  ngOnInit() {
    this.carregarFiliais();
    this.carregarNaturezas();

    // Check if editing existing NF-e
    const params = new URLSearchParams(window.location.search);
    const idParam = params.get('id') || this.route.snapshot.queryParamMap.get('id');
    if (idParam) {
      this.nfeId = +idParam;
      this.carregarNfe(this.nfeId);
      this.modoEdicao.set(true);
    }

    if (!this.tabService.tabs().find(t => t.id === this.TAB_ID && !idParam) &&
        !this.tabService.tabs().find(t => t.id === `${this.TAB_ID}?id=${idParam}`)) {
      this.tabService.abrirTab({
        id: idParam ? `${this.TAB_ID}?id=${idParam}` : this.TAB_ID,
        titulo: idParam ? 'Editar NF-e' : 'Emitir NF-e',
        rota: idParam ? `${this.TAB_ID}?id=${idParam}` : this.TAB_ID,
        iconKey: 'fiscal',
      });
    }
  }

  ngOnDestroy() {
    if (this.destinatarioTimer) clearTimeout(this.destinatarioTimer);
    if (this.produtoTimer) clearTimeout(this.produtoTimer);
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(e: KeyboardEvent) {
    if (this.modal.visivel()) return;
    if (e.ctrlKey && e.key === 's') {
      e.preventDefault();
      if (this.isDirty()) this.salvarRascunho();
    }
  }

  // ── Load data ──────────────────────────────────────────────────────
  private carregarNaturezas() {
    this.http.get<any>(`${environment.apiUrl}/natureza-operacao`).subscribe({
      next: r => this.naturezas.set(r.data ?? []),
      error: () => {}
    });
  }

  private carregarFiliais() {
    const usuario = this.auth.usuarioLogado();
    const filialIdUsuario = parseInt(usuario?.filialId || '0', 10);
    // Default pra filial do usuário logado enquanto a lista carrega
    if (filialIdUsuario > 0 && !this.form().filialId) {
      this.form.update(f => ({ ...f, filialId: filialIdUsuario }));
    }
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({
      next: r => {
        const lista = (r.data ?? []).map((f: any) => ({
          id: f.id,
          nome: f.nomeFilial ?? f.nomeFantasia ?? f.nome ?? `Filial ${f.id}`
        }));
        this.filiais.set(lista);
        // Se ainda não tinha filial no form e o usuário tem só uma, seleciona-a
        if (!this.form().filialId && lista.length === 1) {
          this.form.update(f => ({ ...f, filialId: lista[0].id }));
        }
      },
      error: () => {
        // Sem permissão pra listar — usa só a filial do usuário
        if (filialIdUsuario > 0) {
          this.filiais.set([{ id: filialIdUsuario, nome: usuario?.nomeFilial ?? `Filial ${filialIdUsuario}` }]);
        }
      }
    });
  }

  private carregarNfe(id: number) {
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${id}`).subscribe({
      next: r => {
        const d = r.data;
        this.form.set({
          id: d.id,
          filialId: d.filialId,
          naturezaOperacaoId: d.naturezaOperacaoId,
          destinatarioPessoaId: d.destinatarioPessoaId,
          natOp: d.natOp ?? '',
          tipoNf: d.tipoNf ?? 1,
          finalidadeNfe: d.finalidadeNfe ?? 1,
          identificadorDestino: d.identificadorDestino ?? 1,
          modFrete: d.modFrete ?? 9,
          chaveNfeReferenciada: d.chaveNfeReferenciada ?? '',
          observacao: d.observacao ?? '',
          itens: (d.itens ?? []).map((i: any) => ({
            produtoId: i.produtoId,
            produtoNome: i.descricaoProduto,
            codigoProduto: i.codigoProduto ?? '',
            codigoBarras: i.codigoBarras ?? '',
            descricaoProduto: i.descricaoProduto ?? '',
            ncm: i.ncm ?? '',
            cest: i.cest,
            cfop: i.cfop ?? '',
            quantidade: i.quantidade ?? 0,
            valorUnitario: i.valorUnitario ?? 0,
            valorTotal: i.valorTotal ?? 0,
            origemMercadoria: i.origemMercadoria ?? '0',
            cstIcms: i.cstIcms,
            csosn: i.csosn,
            cstPis: i.cstPis ?? '',
            cstCofins: i.cstCofins ?? '',
            cstIpi: i.cstIpi,
          })),
          parcelas: (d.parcelas ?? []).map((p: any) => ({
            numeroParcela: p.numeroParcela ?? '',
            dataVencimento: p.dataVencimento ?? '',
            valor: p.valor ?? 0,
          })),
        });
        if (d.destinatarioPessoaId && d.destinatarioNome) {
          this.destinatarioSelecionado.set({
            id: d.destinatarioPessoaId,
            nome: d.destinatarioNome,
            cpfCnpj: d.destinatarioCpfCnpj ?? '',
          });
          this.destinatarioBusca.set(d.destinatarioNome);
        }
        if (d.naturezaOperacaoId) {
          const nat = this.naturezas().find(n => n.id === d.naturezaOperacaoId);
          if (nat) this.naturezaSelecionada.set(nat);
        }
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.modal.erro('Erro', 'Erro ao carregar NF-e.');
      }
    });
  }

  // ── Natureza ───────────────────────────────────────────────────────
  onNaturezaChange(natId: string) {
    const id = +natId;
    const nat = this.naturezas().find(n => n.id === id);
    if (!nat) return;
    this.naturezaSelecionada.set(nat);
    this.form.update(f => ({
      ...f,
      naturezaOperacaoId: nat.id,
      natOp: nat.natOp,
      tipoNf: nat.tipoNf,
      finalidadeNfe: nat.finalidadeNfe,
      identificadorDestino: nat.identificadorDestino,
    }));
    this.isDirty.set(true);
  }

  // ── Destinatario lookup ────────────────────────────────────────────
  onDestinatarioBusca(termo: string) {
    this.destinatarioBusca.set(termo);
    this.destinatarioSelecionado.set(null);
    this.form.update(f => ({ ...f, destinatarioPessoaId: undefined }));
    this.isDirty.set(true);

    if (this.destinatarioTimer) clearTimeout(this.destinatarioTimer);
    if (termo.length < 2) {
      this.destinatarioResultados.set([]);
      this.destinatarioAberto.set(false);
      return;
    }
    this.destinatarioTimer = setTimeout(() => {
      this.http.get<any>(`${environment.apiUrl}/pessoas/pesquisar?termo=${encodeURIComponent(termo)}`).subscribe({
        next: r => {
          this.destinatarioResultados.set(r.data ?? []);
          this.destinatarioAberto.set(true);
        },
        error: () => {}
      });
    }, 300);
  }

  selecionarDestinatario(p: PessoaResumo) {
    this.destinatarioSelecionado.set(p);
    this.destinatarioBusca.set(`${p.cpfCnpj} - ${p.nome}`);
    this.destinatarioAberto.set(false);
    this.form.update(f => ({ ...f, destinatarioPessoaId: p.id }));
    this.isDirty.set(true);
  }

  fecharDestinatarioDropdown() {
    setTimeout(() => this.destinatarioAberto.set(false), 200);
  }

  // ── Produto lookup ─────────────────────────────────────────────────
  onProdutoBusca(termo: string) {
    this.produtoBusca.set(termo);
    if (this.produtoTimer) clearTimeout(this.produtoTimer);
    if (termo.length < 2) {
      this.produtoResultados.set([]);
      this.produtoAberto.set(false);
      return;
    }
    this.produtoTimer = setTimeout(() => {
      this.http.get<any>(`${environment.apiUrl}/produtos?busca=${encodeURIComponent(termo)}&limit=10`).subscribe({
        next: r => {
          this.produtoResultados.set(r.data ?? []);
          this.produtoAberto.set(true);
        },
        error: () => {}
      });
    }, 300);
  }

  selecionarProduto(p: ProdutoResumo) {
    this.produtoBusca.set(`${p.codigo} - ${p.nome}`);
    this.produtoAberto.set(false);
    this.novoItemVlrUnit.set(String(p.valorVenda ?? 0));
    this.novoItemQtd.set('1');

    // Store selected product for adicionarItem
    (this as any)._produtoSelecionado = p;
  }

  fecharProdutoDropdown() {
    setTimeout(() => this.produtoAberto.set(false), 200);
  }

  adicionarItem() {
    const p = (this as any)._produtoSelecionado as ProdutoResumo | undefined;
    if (!p) return;

    const qtd = parseFloat(this.novoItemQtd().replace(',', '.')) || 0;
    const vlrUnit = parseFloat(this.novoItemVlrUnit().replace(',', '.')) || 0;
    if (qtd <= 0 || vlrUnit <= 0) return;

    const item: NfeItemForm = {
      produtoId: p.id,
      produtoNome: p.nome,
      codigoProduto: p.codigo,
      codigoBarras: p.codigoBarras ?? '',
      descricaoProduto: p.nome,
      ncm: p.ncm ?? '',
      cest: p.cest,
      cfop: p.cfop ?? '',
      quantidade: qtd,
      valorUnitario: vlrUnit,
      valorTotal: +(qtd * vlrUnit).toFixed(2),
      origemMercadoria: p.origemMercadoria ?? '0',
      cstIcms: p.cstIcms,
      csosn: p.csosn,
      cstPis: p.cstPis ?? '',
      cstCofins: p.cstCofins ?? '',
      cstIpi: p.cstIpi,
    };

    this.form.update(f => ({ ...f, itens: [...f.itens, item] }));
    this.isDirty.set(true);

    // Clear
    this.produtoBusca.set('');
    this.novoItemQtd.set('1');
    this.novoItemVlrUnit.set('');
    (this as any)._produtoSelecionado = null;
  }

  removerItem(idx: number) {
    this.form.update(f => ({ ...f, itens: f.itens.filter((_, i) => i !== idx) }));
    this.isDirty.set(true);
  }

  // ── Form helpers ───────────────────────────────────────────────────
  upd(campo: keyof NfeForm, v: any) {
    this.form.update(f => ({ ...f, [campo]: v }));
    this.isDirty.set(true);
  }

  erroCampo(campo: string): string {
    return this.errosCampos()[campo] ?? '';
  }

  formatMoney(valor: number): string {
    return 'R$ ' + valor.toFixed(2).replace('.', ',');
  }

  // ── Save / Emit ───────────────────────────────────────────────────
  async salvarRascunho() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;

    const erros: Record<string, string> = {};
    const f = this.form();
    if (!f.filialId) erros['filialId'] = 'Filial e obrigatoria.';
    if (!f.naturezaOperacaoId) erros['naturezaOperacaoId'] = 'Natureza de operacao e obrigatoria.';
    if (!f.destinatarioPessoaId) erros['destinatarioPessoaId'] = 'Destinatario e obrigatorio.';
    if (Object.keys(erros).length) {
      this.errosCampos.set(erros);
      return;
    }
    this.errosCampos.set({});
    this.salvando.set(true);
    this.erro.set('');

    const headers = this.headerLiberacao();
    const body = this.buildBody();

    const req$ = f.id
      ? this.http.put(`${this.apiUrl}/rascunho-nfe/${f.id}`, body, { headers })
      : this.http.post<any>(`${this.apiUrl}/rascunho-nfe`, body, { headers });

    req$.subscribe({
      next: (r: any) => {
        this.salvando.set(false);
        this.isDirty.set(false);
        const id = f.id ?? r.data?.id;
        if (id && !f.id) {
          this.form.update(fm => ({ ...fm, id }));
          this.nfeId = id;
          this.modoEdicao.set(true);
        }
      },
      error: (err) => {
        this.salvando.set(false);
        this.erro.set(err.error?.message ?? 'Erro ao salvar NF-e.');
      }
    });
  }

  async emitirNfe() {
    const f = this.form();
    if (!f.id) {
      this.modal.aviso('Salve primeiro', 'E necessario salvar o rascunho antes de emitir.');
      return;
    }
    if (f.itens.length === 0) {
      this.modal.aviso('Sem itens', 'Adicione pelo menos um item antes de emitir.');
      return;
    }

    const resultado = await this.modal.confirmar(
      'Emitir NF-e',
      'Deseja enviar esta NF-e para a SEFAZ? Apos autorizada, nao sera possivel alterar.',
      'Sim, emitir',
      'Nao, cancelar'
    );
    if (!resultado.confirmado) return;

    this.emitindo.set(true);
    this.erro.set('');

    this.http.post<any>(`${this.apiUrl}/emitir-nfe/${f.id}`, {}).subscribe({
      next: (r: any) => {
        this.emitindo.set(false);
        this.modal.aviso('NF-e Emitida', r.message ?? 'NF-e emitida com sucesso.');
        this.fechar();
      },
      error: (err) => {
        this.emitindo.set(false);
        this.erro.set(err.error?.message ?? 'Erro ao emitir NF-e.');
      }
    });
  }

  private buildBody(): any {
    const f = this.form();
    return {
      filialId: f.filialId,
      naturezaOperacaoId: f.naturezaOperacaoId,
      destinatarioPessoaId: f.destinatarioPessoaId,
      natOp: f.natOp,
      tipoNf: f.tipoNf,
      finalidadeNfe: f.finalidadeNfe,
      identificadorDestino: f.identificadorDestino,
      modFrete: f.modFrete,
      chaveNfeReferenciada: f.chaveNfeReferenciada || undefined,
      observacao: f.observacao || undefined,
      itens: f.itens.map(i => ({
        produtoId: i.produtoId,
        codigoProduto: i.codigoProduto,
        codigoBarras: i.codigoBarras,
        descricaoProduto: i.descricaoProduto,
        ncm: i.ncm,
        cest: i.cest,
        cfop: i.cfop,
        quantidade: i.quantidade,
        valorUnitario: i.valorUnitario,
        valorTotal: i.valorTotal,
        origemMercadoria: i.origemMercadoria,
        cstIcms: i.cstIcms,
        csosn: i.csosn,
        cstPis: i.cstPis,
        cstCofins: i.cstCofins,
        cstIpi: i.cstIpi,
      })),
      parcelas: f.parcelas.map(p => ({
        numeroParcela: p.numeroParcela,
        dataVencimento: p.dataVencimento,
        valor: p.valor,
      })),
    };
  }

  async fechar() {
    if (this.isDirty()) {
      const r = await this.modal.confirmar('Fechar', 'Voce tem alteracoes nao salvas. Deseja realmente fechar?', 'Sim, fechar', 'Nao, continuar');
      if (!r.confirmado) return;
    }
    this.tabService.fecharTabAtiva();
  }

  async sairDaTela() {
    await this.fechar();
  }

  // ── Utils ──────────────────────────────────────────────────────────
  private novoForm(): NfeForm {
    return {
      filialId: 0,
      naturezaOperacaoId: 0,
      destinatarioPessoaId: undefined,
      natOp: '',
      tipoNf: 1,
      finalidadeNfe: 1,
      identificadorDestino: 1,
      modFrete: 9,
      chaveNfeReferenciada: '',
      observacao: '',
      itens: [],
      parcelas: [],
    };
  }
}
