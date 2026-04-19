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
interface EntregaFaixa {
  id: number;
  filialId: number;
  raioMaxKm: number;
  valor: number;
  ordem: number;
  // UI helpers (não enviados pro backend)
  raioMaxKmStr?: string;
  valorStr?: string;
  dirty?: boolean;
  salvando?: boolean;
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

  // ── Entregas ──────────────────────────────────────────────────
  faixas = signal<EntregaFaixa[]>([]);
  faixasCarregando = signal(false);
  private faixasCarregadas = false;

  private apiUrl = `${environment.apiUrl}/configuracoes`;
  private sefazUrl = `${environment.apiUrl}/sefaz`;
  private entregaFaixasUrl = `${environment.apiUrl}/entrega-faixas`;

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
    if (id === 'entregas' && this.accordionAberto() === 'entregas' && !this.faixasCarregadas) {
      this.carregarFaixas();
    }
  }

  // ── Entregas — CRUD de faixas ────────────────────────────────
  private filialIdAtual(): number {
    const u = this.auth.usuarioLogado();
    return parseInt(u?.filialId || '0', 10);
  }

  carregarFaixas() {
    const filialId = this.filialIdAtual();
    if (filialId <= 0) {
      this.toastr.warning('Usuário sem filial associada.');
      return;
    }
    this.faixasCarregando.set(true);
    this.http.get<any>(`${this.entregaFaixasUrl}?filialId=${filialId}`).subscribe({
      next: r => {
        const lista = (r.data ?? []).map((f: EntregaFaixa) => ({
          ...f,
          raioMaxKmStr: this.formatDecimal(f.raioMaxKm, 3),
          valorStr: this.formatDecimal(f.valor, 2),
          dirty: false,
          salvando: false
        }));
        this.faixas.set(lista);
        this.faixasCarregadas = true;
        this.faixasCarregando.set(false);
      },
      error: () => {
        this.faixasCarregando.set(false);
        this.modal.erro('Erro', 'Erro ao carregar faixas de entrega.');
      }
    });
  }

  adicionarFaixa() {
    const filialId = this.filialIdAtual();
    if (filialId <= 0) return;
    const proximaOrdem = (this.faixas().reduce((m, f) => Math.max(m, f.ordem), 0)) + 1;
    this.faixas.update(lista => [...lista, {
      id: 0,
      filialId,
      raioMaxKm: 0,
      valor: 0,
      ordem: proximaOrdem,
      raioMaxKmStr: '',
      valorStr: '',
      dirty: true,
      salvando: false
    }]);
  }

  atualizarFaixa(idx: number, campo: 'raioMaxKmStr' | 'valorStr', valor: string) {
    this.faixas.update(lista => lista.map((f, i) => i === idx ? { ...f, [campo]: valor, dirty: true } : f));
  }

  private parseDecimal(s: string | undefined): number {
    if (!s) return 0;
    const n = parseFloat(s.replace(/\./g, '').replace(',', '.'));
    return isNaN(n) ? 0 : n;
  }

  private formatDecimal(n: number, casas: number): string {
    return (n ?? 0).toLocaleString('pt-BR', { minimumFractionDigits: casas, maximumFractionDigits: casas });
  }

  async salvarFaixa(idx: number) {
    const faixa = this.faixas()[idx];
    if (!faixa) return;
    const raio = this.parseDecimal(faixa.raioMaxKmStr);
    const valor = this.parseDecimal(faixa.valorStr);
    if (raio <= 0) { this.modal.erro('Erro', 'Raio deve ser maior que zero.'); return; }
    if (valor < 0) { this.modal.erro('Erro', 'Valor não pode ser negativo.'); return; }

    const body = { filialId: faixa.filialId, raioMaxKm: raio, valor, ordem: faixa.ordem };
    this.faixas.update(lista => lista.map((f, i) => i === idx ? { ...f, salvando: true } : f));

    if (faixa.id && faixa.id > 0) {
      this.http.put(`${this.entregaFaixasUrl}/${faixa.id}`, body).subscribe({
        next: () => {
          this.faixas.update(lista => lista.map((f, i) => i === idx
            ? { ...f, raioMaxKm: raio, valor, dirty: false, salvando: false } : f));
          this.toastr.success('Faixa atualizada.');
        },
        error: e => this.erroSalvarFaixa(idx, e)
      });
    } else {
      this.http.post<any>(this.entregaFaixasUrl, body).subscribe({
        next: r => {
          const novoId = r.data?.id ?? 0;
          this.faixas.update(lista => lista.map((f, i) => i === idx
            ? { ...f, id: novoId, raioMaxKm: raio, valor, dirty: false, salvando: false } : f));
          this.toastr.success('Faixa criada.');
        },
        error: e => this.erroSalvarFaixa(idx, e)
      });
    }
  }

  private erroSalvarFaixa(idx: number, err: any) {
    this.faixas.update(lista => lista.map((f, i) => i === idx ? { ...f, salvando: false } : f));
    this.modal.erro('Erro', err?.error?.message ?? 'Erro ao salvar faixa.');
  }

  async excluirFaixa(idx: number) {
    const faixa = this.faixas()[idx];
    if (!faixa) return;
    if (!faixa.id || faixa.id === 0) {
      // Nova que ainda não salvou — só remove da lista
      this.faixas.update(lista => lista.filter((_, i) => i !== idx));
      return;
    }
    const r = await this.modal.confirmar('Excluir faixa', 'Deseja remover esta faixa de entrega?', 'Sim, excluir', 'Cancelar');
    if (!r.confirmado) return;

    this.http.delete(`${this.entregaFaixasUrl}/${faixa.id}`).subscribe({
      next: () => {
        this.faixas.update(lista => lista.filter((_, i) => i !== idx));
        this.toastr.success('Faixa excluída.');
      },
      error: e => this.modal.erro('Erro', e?.error?.message ?? 'Erro ao excluir.')
    });
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
      },
      error: () => this.carregando.set(false)
    });
  }

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
      next: () => {
        this.salvando.set(false);
        this.modal.sucesso('Salvo', 'Configurações salvas com sucesso.');
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
}
