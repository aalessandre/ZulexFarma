import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';
import { AuthService } from '../../core/services/auth.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';
import { ToastrService } from 'ngx-toastr';

interface ConfigItem { chave: string; valor: string; descricao?: string; }
interface PcLookup { id: number; descricao: string; codigoHierarquico: string; }
interface CertificadoInfo {
  id: number; filialId: number; cnpj: string; razaoSocial: string;
  validade: string; emissor: string; valido: boolean; diasParaVencer: number;
}
@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './configuracoes.component.html',
  styleUrl: './configuracoes.component.scss'
})
export class ConfiguracoesComponent implements OnInit {
  carregando = signal(false);
  salvando = signal(false);
  configs = signal<Record<string, string>>({});
  accordionAberto = signal<string>('geral');

  // Parâmetros já implementados (fonte verde na tela)
  readonly implementados = new Set([
    'venda.multiplos.vendedores', 'caixa.multiplos.vendedores',
    'venda.duplicar.linha', 'caixa.duplicar.linha',
    'venda.focar.quantidade', 'caixa.focar.quantidade',
    'venda.alterar.preco.promo', 'caixa.alterar.preco.promo',
    'venda.obrigar.escanear', 'caixa.obrigar.escanear',
    'caixa.informar.cesta',
    'venda.promo.multiplas',
  ]);
  certificado = signal<CertificadoInfo | null>(null);
  uploadandoCert = signal(false);

  // ── Farmácia Popular ──
  testandoFp = signal(false);
  ultimoTesteFp = signal<string>('');

  // ── Self-Checkout ──
  scAtivo = signal(true);
  scErpOrigem = signal<number>(1); // 1 = Inovafarma
  scHost = signal('');
  scBanco = signal('');
  scUsuario = signal('');
  scSenha = signal('');
  scTemSenha = signal(false);
  scFilialErp = signal('');
  scCodigoNatureza = signal<number | null>(null);
  scNaturezas = signal<{ codigo: number; nome: string }[]>([]);
  carregandoNaturezas = signal(false);
  scConfigId = signal<number | null>(null);
  scTerminais = signal<{ id: number; numero: number; apelido?: string | null; ativo: boolean }[]>([]);
  scNovoTerminalNumero = signal<number>(0);
  scNovoTerminalApelido = signal('');
  testandoSc = signal(false);
  ultimoTesteSc = signal('');
  salvandoSc = signal(false);
  private scCarregado = false;
  private scUrl = `${environment.apiUrl}/configuracoes/self-checkout`;
  private scActionsUrl = `${environment.apiUrl}/self-checkout`;
  fpPlanoContaBusca = signal('');
  fpPlanoContaResultados = signal<{id:number; descricao:string; codigoHierarquico:string}[]>([]);
  fpPlanoContaDropdown = signal(false);
  fpPlanoContaIndice = signal(-1);
  fpPlanoContaSelecionado = signal<{id:number; descricao:string; codigoHierarquico:string} | null>(null);
  private fpPlanoContaTimer: any = null;

  private apiUrl = `${environment.apiUrl}/configuracoes`;
  private sefazUrl = `${environment.apiUrl}/sefaz`;
  private farmaciaPopularUrl = `${environment.apiUrl}/farmacia-popular`;

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private modal: ModalService,
    private auth: AuthService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.carregar();
    this.carregarCertificado();
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  toggleAccordion(id: string) {
    this.accordionAberto.set(this.accordionAberto() === id ? '' : id);
  }

  getBool(chave: string, padrao = false): boolean {
    return (this.configs()[chave] ?? (padrao ? 'true' : 'false')) === 'true';
  }

  setBool(chave: string, valor: boolean) {
    this.setConfig(chave, valor ? 'true' : 'false');
  }

  formatarIntervalo(segundos: number): string {
    if (segundos < 60) return `${segundos}s`;
    const min = Math.floor(segundos / 60);
    const sec = segundos % 60;
    return sec > 0 ? `${min}min ${sec}s` : `${min}min`;
  }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        const map: Record<string, string> = {};
        for (const item of (r.data ?? [])) map[item.chave] = item.valor;
        this.configs.set(map);
        this.carregando.set(false);
        this.carregarNomesPc();
        this.carregarFpPlanoContaSelecionado();
      },
      error: () => this.carregando.set(false)
    });
  }

  // ── Lookup Plano de Contas (Farmácia Popular) ─────────────────
  private carregarFpPlanoContaSelecionado() {
    const id = this.getConfig('pbm.fp.plano.conta.id', '');
    if (!id) { this.fpPlanoContaSelecionado.set(null); return; }
    this.http.get<any>(`${environment.apiUrl}/planoscontas/${id}/resumo`).subscribe({
      next: r => { if (r?.data) this.fpPlanoContaSelecionado.set(r.data); },
      error: () => this.fpPlanoContaSelecionado.set(null)
    });
  }

  onFpPlanoContaInput(valor: string) {
    this.fpPlanoContaBusca.set(valor);
    if (this.fpPlanoContaTimer) clearTimeout(this.fpPlanoContaTimer);
    if (valor.trim().length < 2) {
      this.fpPlanoContaResultados.set([]);
      this.fpPlanoContaDropdown.set(false);
      return;
    }
    this.fpPlanoContaTimer = setTimeout(() => {
      this.http.get<any>(`${environment.apiUrl}/planoscontas/pesquisar`, { params: { termo: valor } }).subscribe({
        next: r => {
          this.fpPlanoContaResultados.set(r.data ?? []);
          this.fpPlanoContaDropdown.set((r.data ?? []).length > 0);
          this.fpPlanoContaIndice.set(-1);
        },
        error: () => { this.fpPlanoContaResultados.set([]); this.fpPlanoContaDropdown.set(false); }
      });
    }, 250);
  }

  selecionarFpPlanoConta(pc: {id:number; descricao:string; codigoHierarquico:string}) {
    this.fpPlanoContaSelecionado.set(pc);
    this.setConfig('pbm.fp.plano.conta.id', String(pc.id));
    this.fpPlanoContaBusca.set('');
    this.fpPlanoContaResultados.set([]);
    this.fpPlanoContaDropdown.set(false);
  }

  limparFpPlanoConta() {
    this.fpPlanoContaSelecionado.set(null);
    this.setConfig('pbm.fp.plano.conta.id', '');
    this.fpPlanoContaBusca.set('');
  }

  onFpPlanoContaBlur() { setTimeout(() => this.fpPlanoContaDropdown.set(false), 200); }
  onFpPlanoContaFocus() { if (this.fpPlanoContaResultados().length > 0) this.fpPlanoContaDropdown.set(true); }

  onFpPlanoContaKeydown(e: KeyboardEvent) {
    const lista = this.fpPlanoContaResultados();
    if (!this.fpPlanoContaDropdown() || lista.length === 0) return;
    if (e.key === 'ArrowDown') { e.preventDefault(); this.fpPlanoContaIndice.set(Math.min(this.fpPlanoContaIndice() + 1, lista.length - 1)); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); this.fpPlanoContaIndice.set(Math.max(this.fpPlanoContaIndice() - 1, 0)); }
    else if (e.key === 'Enter') {
      e.preventDefault();
      const idx = this.fpPlanoContaIndice();
      if (idx >= 0 && idx < lista.length) this.selecionarFpPlanoConta(lista[idx]);
      else if (lista.length === 1) this.selecionarFpPlanoConta(lista[0]);
    } else if (e.key === 'Escape') {
      this.fpPlanoContaDropdown.set(false);
    }
  }

  // ── Lookup Vendedor padrão (config venda.vendedor.padrao.*) ─────────
  vendPadraoBusca = signal('');
  vendPadraoResultados = signal<{ id: number; nome: string; codigo?: string }[]>([]);
  vendPadraoDropdown = signal(false);
  private vendPadraoTimer: any = null;

  onVendPadraoInput(valor: string) {
    this.vendPadraoBusca.set(valor);
    if (this.vendPadraoTimer) clearTimeout(this.vendPadraoTimer);
    if (valor.trim().length < 2) { this.vendPadraoResultados.set([]); this.vendPadraoDropdown.set(false); return; }
    this.vendPadraoTimer = setTimeout(() => {
      this.http.get<any>(`${environment.apiUrl}/colaboradores/pesquisar`, { params: { termo: valor, status: 'ativos' } }).subscribe({
        next: r => { this.vendPadraoResultados.set(r.data ?? []); this.vendPadraoDropdown.set((r.data ?? []).length > 0); },
        error: () => { this.vendPadraoResultados.set([]); this.vendPadraoDropdown.set(false); }
      });
    }, 250);
  }

  selecionarVendPadrao(c: { id: number; nome: string }) {
    this.setConfig('venda.vendedor.padrao.id', String(c.id));
    this.setConfig('venda.vendedor.padrao.nome', c.nome);
    this.vendPadraoBusca.set('');
    this.vendPadraoResultados.set([]);
    this.vendPadraoDropdown.set(false);
  }

  limparVendPadrao() {
    this.setConfig('venda.vendedor.padrao.id', '');
    this.setConfig('venda.vendedor.padrao.nome', '');
    this.vendPadraoBusca.set('');
  }

  onVendPadraoBlur() { setTimeout(() => this.vendPadraoDropdown.set(false), 200); }

  getConfig(chave: string, padrao = ''): string {
    return this.configs()[chave] ?? padrao;
  }

  setConfig(chave: string, valor: string) {
    this.configs.update(c => ({ ...c, [chave]: valor }));
  }

  /**
   * Gera um CSRT fake (36 chars alfanuméricos) e ID=1 pra uso em HOMOLOGAÇÃO.
   * Em produção o código deve ser cadastrado no portal SEFAZ-UF pelo desenvolvedor.
   */
  gerarCsrtHomologacao() {
    const charset = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    let csrt = '';
    const crypto = window.crypto;
    if (crypto?.getRandomValues) {
      const bytes = new Uint8Array(36);
      crypto.getRandomValues(bytes);
      for (let i = 0; i < 36; i++) csrt += charset[bytes[i] % charset.length];
    } else {
      for (let i = 0; i < 36; i++) csrt += charset[Math.floor(Math.random() * charset.length)];
    }
    this.setConfig('fiscal.csrt.id', '01');
    this.setConfig('fiscal.csrt.codigo', csrt);
    this.toastr.info('CSRT fake gerado. Válido apenas em homologação — não esqueça de Salvar.');
  }

  async salvar() {
    this.salvando.set(true);
    const items: ConfigItem[] = Object.entries(this.configs()).map(([chave, valor]) => ({ chave, valor }));
    this.http.put(this.apiUrl, items).subscribe({
      next: async () => {
        const erroSc = await this.salvarSelfCheckoutSeAplicavel();
        this.salvando.set(false);
        if (erroSc) this.modal.erro('Self-Checkout', erroSc);
        else this.modal.sucesso('Salvo', 'Configurações salvas com sucesso.');
      },
      error: () => {
        this.salvando.set(false);
        this.modal.erro('Erro', 'Erro ao salvar configurações.');
      }
    });
  }

  // ── Plano de Contas Padrão ──────────────────────────────────────
  readonly pcChaves = [
    { chave: 'pc.compra_mercadorias', label: 'Compra de Mercadorias' },
    { chave: 'pc.venda_vista', label: 'Venda à Vista' },
    { chave: 'pc.venda_prazo', label: 'Venda a Prazo' },
    { chave: 'pc.venda_cartao', label: 'Venda Cartão' },
    { chave: 'pc.transferencia_mercadoria', label: 'Transferência de Mercadoria' },
    { chave: 'pc.desconto_obtido', label: 'Desconto Obtido' },
    { chave: 'pc.desconto_concedido', label: 'Desconto Concedido' },
    { chave: 'pc.juros_pagos', label: 'Juros/Multa Pagos' },
    { chave: 'pc.juros_recebidos', label: 'Juros/Multa Recebidos' },
    { chave: 'pc.despesa_bancaria', label: 'Despesas Bancárias' },
    { chave: 'pc.frete_compra', label: 'Frete sobre Compras' },
    { chave: 'pc.devolucao_compra', label: 'Devolução de Compra' },
    { chave: 'pc.devolucao_venda', label: 'Devolução de Venda' },
    { chave: 'pc.bonificacao_fornecedor', label: 'Bonificação de Fornecedor' },
  ];

  // Armazena nomes resolvidos para exibir nos inputs
  pcNomes = signal<Record<string, string>>({});
  pcBuscaAtiva = signal('');
  pcResultados = signal<PcLookup[]>([]);
  pcDropdown = signal(false);
  private pcTimer: any = null;

  carregarNomesPc() {
    // Para cada chave que tem valor (id), buscar o nome
    const configs = this.configs();
    for (const item of this.pcChaves) {
      const id = configs[item.chave];
      if (id) {
        // Busca individual por ID via listagem
        this.http.get<any>(`${environment.apiUrl}/planoscontas/pesquisar?termo=${id}`).subscribe({
          next: r => {
            const encontrado = (r.data ?? []).find((p: any) => p.id === Number(id));
            if (encontrado) {
              this.pcNomes.update(n => ({ ...n, [item.chave]: `${encontrado.codigoHierarquico} - ${encontrado.descricao}` }));
            }
          }
        });
      }
    }
  }

  getPcNome(chave: string): string {
    return this.pcNomes()[chave] ?? '';
  }

  onPcBuscaInput(chave: string, valor: string) {
    this.pcBuscaAtiva.set(chave);
    this.pcNomes.update(n => ({ ...n, [chave]: valor }));
    if (this.pcTimer) clearTimeout(this.pcTimer);
    if (valor.trim().length < 2) { this.pcResultados.set([]); this.pcDropdown.set(false); return; }
    this.pcTimer = setTimeout(() => {
      this.http.get<any>(`${environment.apiUrl}/planoscontas/pesquisar?termo=${encodeURIComponent(valor.trim())}`).subscribe({
        next: r => { this.pcResultados.set(r.data ?? []); this.pcDropdown.set((r.data ?? []).length > 0); }
      });
    }, 300);
  }

  onPcBuscaBlur() { setTimeout(() => this.pcDropdown.set(false), 200); }

  selecionarPcConfig(chave: string, pc: PcLookup) {
    this.setConfig(chave, String(pc.id));
    this.pcNomes.update(n => ({ ...n, [chave]: `${pc.codigoHierarquico} - ${pc.descricao}` }));
    this.pcDropdown.set(false);
  }

  limparPcConfig(chave: string) {
    this.setConfig(chave, '');
    this.pcNomes.update(n => ({ ...n, [chave]: '' }));
  }

  // ── Certificado Digital ────────────────────────────────────────

  carregarCertificado() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.http.get<any>(`${this.sefazUrl}/certificado/${filialId}`).subscribe({
      next: r => this.certificado.set(r.data),
      error: () => this.certificado.set(null)
    });
  }

  onCertificadoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    const senha = prompt('Digite a senha do certificado:');
    if (!senha) { input.value = ''; return; }

    this.uploadandoCert.set(true);
    const reader = new FileReader();
    reader.onload = () => {
      const base64 = (reader.result as string).split(',')[1];
      const usuario = this.auth.usuarioLogado();
      const filialId = parseInt(usuario?.filialId || '1', 10);

      this.http.post<any>(`${this.sefazUrl}/certificado/upload`, {
        filialId, pfxBase64: base64, senha
      }).subscribe({
        next: r => {
          this.certificado.set(r.data);
          this.toastr.success('Certificado carregado com sucesso!', 'OK', { timeOut: 3000, positionClass: 'toast-top-center' });
          this.uploadandoCert.set(false);
          input.value = '';
        },
        error: e => {
          this.modal.erro('Erro', e?.error?.message || 'Erro ao carregar certificado.');
          this.uploadandoCert.set(false);
          input.value = '';
        }
      });
    };
    reader.readAsDataURL(file);
  }

  // ── Farmácia Popular — testar conexão ────────────────────────────
  testarConexaoFp() {
    const caminho = this.getConfig('pbm.fp.caminho.gbasmsb', '');
    if (!caminho?.trim()) {
      this.modal.erro('Farmácia Popular', 'Informe o caminho do gbasmsb.exe antes de testar.');
      return;
    }
    this.testandoFp.set(true);
    // Persiste todas as configs atuais antes do teste (backend lê do banco).
    const items: ConfigItem[] = Object.entries(this.configs()).map(([chave, valor]) => ({ chave, valor }));
    this.http.put(this.apiUrl, items).subscribe({
      next: () => {
        this.http.post<any>(`${this.farmaciaPopularUrl}/testar-conexao`, {}).subscribe({
          next: r => {
            this.testandoFp.set(false);
            const msg = r.message ?? '';
            this.ultimoTesteFp.set(`${new Date().toLocaleString('pt-BR')} — ${msg}`);
            if (r.success) this.modal.sucesso('Teste OK', msg);
            else this.modal.erro('Teste falhou', msg);
          },
          error: e => {
            this.testandoFp.set(false);
            this.modal.erro('Teste falhou', e?.error?.message ?? 'Erro ao testar conexão.');
          }
        });
      },
      error: () => {
        this.testandoFp.set(false);
        this.modal.erro('Erro', 'Não foi possível salvar configurações antes do teste.');
      }
    });
  }

  // ── Self-Checkout ────────────────────────────────────────────────
  private get filialIdAtual(): number {
    return parseInt(this.auth.usuarioLogado()?.filialId || '1', 10);
  }

  onAbrirSelfCheckout() {
    this.toggleAccordion('self-checkout');
    if (this.accordionAberto() === 'self-checkout' && !this.scCarregado) {
      this.carregarSelfCheckout();
    }
  }

  carregarSelfCheckout() {
    const filialId = this.filialIdAtual;
    this.http.get<any>(`${this.scUrl}/${filialId}`).subscribe({
      next: r => {
        const cfg = r?.data;
        if (cfg) {
          this.scConfigId.set(cfg.id);
          this.scAtivo.set(!!cfg.ativo);
          this.scErpOrigem.set(cfg.erpOrigem ?? 1);
          this.scHost.set(cfg.hostBanco ?? '');
          this.scBanco.set(cfg.nomeBanco ?? '');
          this.scUsuario.set(cfg.usuarioBanco ?? '');
          this.scTemSenha.set(!!cfg.temSenhaCadastrada);
          this.scSenha.set('');
          this.scFilialErp.set(cfg.filialErpOrigem ?? '');
          this.scCodigoNatureza.set(cfg.codigoNaturezaOperacaoNfce ?? null);
        }
        this.scCarregado = true;
        this.carregarTerminais();
        // Pre-carrega naturezas se a config já tem ID (config persistida).
        if (this.scConfigId()) this.carregarNaturezas();
      },
      error: () => { this.scCarregado = true; }
    });
  }

  carregarTerminais() {
    const filialId = this.filialIdAtual;
    this.http.get<any>(`${this.scUrl}/${filialId}/terminais`).subscribe({
      next: r => this.scTerminais.set(r?.data ?? []),
      error: () => this.scTerminais.set([])
    });
  }

  /**
   * Salva a configuração do Self-Checkout junto com o Salvar geral, mas só se
   * o usuário interagiu com o accordion (carregou pelo menos uma vez) e há
   * dados mínimos preenchidos. Retorna null em sucesso ou mensagem de erro.
   */
  private salvarSelfCheckoutSeAplicavel(): Promise<string | null> {
    if (!this.scCarregado) return Promise.resolve(null);

    const semDadosBasicos = !this.scHost().trim() || !this.scBanco().trim()
      || !this.scUsuario().trim() || !this.scFilialErp().trim();

    // Sem registro persistido ainda: só tenta criar se o usuário preencheu o
    // mínimo + senha. Senão, ignora silenciosamente (usuário ainda não terminou).
    if (this.scConfigId() === null) {
      if (semDadosBasicos) return Promise.resolve(null);
      if (!this.scSenha()) return Promise.resolve(
        'Preencha a senha do banco para criar a configuração do Self-Checkout.');
    }

    const filialId = this.filialIdAtual;
    const form = {
      erpOrigem: this.scErpOrigem(),
      hostBanco: this.scHost(),
      nomeBanco: this.scBanco(),
      usuarioBanco: this.scUsuario(),
      senhaBanco: this.scSenha() || null,
      filialErpOrigem: this.scFilialErp(),
      codigoNaturezaOperacaoNfce: this.scCodigoNatureza(),
      ativo: this.scAtivo()
    };

    return new Promise(resolve => {
      this.http.put<any>(`${this.scUrl}/${filialId}`, form).subscribe({
        next: r => {
          this.scConfigId.set(r?.data?.id ?? null);
          this.scTemSenha.set(!!r?.data?.temSenhaCadastrada);
          this.scSenha.set('');
          resolve(null);
        },
        error: e => resolve(e?.error?.message ?? 'Erro ao salvar configuração do Self-Checkout.')
      });
    });
  }

  testarConexaoSelfCheckout() {
    if (!this.scHost() || !this.scBanco() || !this.scUsuario() || !this.scFilialErp()) {
      this.modal.erro('Self-Checkout', 'Preencha host, banco, usuário e filial antes de testar.');
      return;
    }
    if (!this.scSenha() && !this.scTemSenha()) {
      this.modal.erro('Self-Checkout', 'Informe a senha do banco para testar.');
      return;
    }
    this.testandoSc.set(true);
    // Senha em texto puro para o teste — não persiste se o usuário não clicar Salvar.
    // Se o campo de senha estiver vazio e há senha cadastrada, salva primeiro pra usar a config persistida.
    const usaPersistida = !this.scSenha() && this.scTemSenha();
    const exec = () => {
      const body = {
        erpOrigem: this.scErpOrigem(),
        hostBanco: this.scHost(),
        nomeBanco: this.scBanco(),
        usuarioBanco: this.scUsuario(),
        senhaBanco: this.scSenha(),
        filialErpOrigem: this.scFilialErp()
      };
      this.http.post<any>(`${this.scActionsUrl}/testar-conexao`, body).subscribe({
        next: r => {
          this.testandoSc.set(false);
          const data = r?.data;
          const ts = new Date().toLocaleString('pt-BR');
          if (data?.ok) {
            this.ultimoTesteSc.set(`${ts} — ${data.mensagem} (${data.totalProdutos ?? 0} produtos)`);
            this.modal.sucesso('Teste OK', data.mensagem);
          } else {
            this.ultimoTesteSc.set(`${ts} — ${data?.mensagem ?? 'falha'}`);
            this.modal.erro('Teste falhou', data?.mensagem ?? 'falha');
          }
        },
        error: e => {
          this.testandoSc.set(false);
          this.modal.erro('Teste falhou', e?.error?.message ?? 'Erro ao testar conexão.');
        }
      });
    };

    if (usaPersistida) {
      this.toastr.info('Para testar com a senha já cadastrada, clique em Salvar antes — ou digite a senha no campo.', 'Atenção',
        { timeOut: 6000, positionClass: 'toast-top-center' });
      this.testandoSc.set(false);
      return;
    }
    exec();
  }

  criarTerminal() {
    const numero = this.scNovoTerminalNumero();
    if (!numero || numero <= 0) {
      this.modal.erro('Terminal', 'Informe um número válido.');
      return;
    }
    const filialId = this.filialIdAtual;
    const form = { numero, apelido: this.scNovoTerminalApelido() || null, ativo: true };
    this.http.post<any>(`${this.scUrl}/${filialId}/terminais`, form).subscribe({
      next: () => {
        this.scNovoTerminalNumero.set(0);
        this.scNovoTerminalApelido.set('');
        this.carregarTerminais();
      },
      error: e => this.modal.erro('Terminal', e?.error?.message ?? 'Erro ao criar terminal.')
    });
  }

  carregarNaturezas() {
    const filialId = this.filialIdAtual;
    this.carregandoNaturezas.set(true);
    this.http.get<any>(`${this.scActionsUrl}/filial/${filialId}/naturezas-operacao`).subscribe({
      next: r => {
        this.scNaturezas.set(r?.data ?? []);
        this.carregandoNaturezas.set(false);
      },
      error: () => {
        this.scNaturezas.set([]);
        this.carregandoNaturezas.set(false);
      }
    });
  }

  onScNaturezaChange(valor: string) {
    const num = parseInt(valor, 10);
    this.scCodigoNatureza.set(isNaN(num) ? null : num);
  }

  removerTerminal(id: number) {
    if (!confirm('Remover este terminal?')) return;
    this.http.delete<any>(`${this.scUrl}/terminais/${id}`).subscribe({
      next: () => this.carregarTerminais(),
      error: e => this.modal.erro('Terminal', e?.error?.message ?? 'Erro ao remover terminal.')
    });
  }
}
