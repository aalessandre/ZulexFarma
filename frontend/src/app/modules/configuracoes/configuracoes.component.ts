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
  private apiUrl = `${environment.apiUrl}/configuracoes`;
  private sefazUrl = `${environment.apiUrl}/sefaz`;

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
