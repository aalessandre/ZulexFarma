import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface EnderecoClienteOpcao {
  id: number;
  tipo: string;
  cep: string;
  rua: string;
  numero: string;
  complemento?: string;
  bairro: string;
  cidade: string;
  uf: string;
  latitude?: number | null;
  longitude?: number | null;
  principal: boolean;
}

export interface EntregaPreview {
  distanciaKm: number;
  valorEntrega: number;
  entregaFaixaId: number;
  bairro: string;
  cidade: string;
  latitude: number;
  longitude: number;
}

export interface EntregaResultado {
  enderecoEntregaId: number;
  valorEntrega: number;
  distanciaKm: number;
  observacao?: string;
  vendaRecebida: boolean;
  despacharAgora: boolean;
  entregadorId?: number;
}

interface ColaboradorOpcao {
  id: number;
  nome: string;
  apelido?: string;
  cargo?: string;
  ativo: boolean;
}

@Injectable({ providedIn: 'root' })
export class ModalEntregaService {
  visivel = signal(false);
  carregandoEnderecos = signal(false);
  carregandoEntregadores = signal(false);
  calculando = signal(false);
  enderecos = signal<EnderecoClienteOpcao[]>([]);
  enderecoSelecionadoId = signal<number | null>(null);
  preview = signal<EntregaPreview | null>(null);
  observacao = signal('');
  erro = signal('');

  // Checkboxes + entregador (somente no caixa)
  mostrarOpcoesCaixa = signal(false);
  vendaRecebida = signal(false);
  despacharAgora = signal(false);
  entregadores = signal<ColaboradorOpcao[]>([]);
  entregadorId = signal<number | null>(null);

  private clienteId = 0;
  private filialId = 0;
  private resolver: ((r: EntregaResultado | null) => void) | null = null;

  constructor(private http: HttpClient) {}

  /** Abre o modal. Retorna promise que resolve com os dados ao confirmar, ou null ao cancelar. */
  async abrir(clienteId: number, filialId: number, preset?: { enderecoId?: number | null; observacao?: string; mostrarOpcoesCaixa?: boolean }): Promise<EntregaResultado | null> {
    this.clienteId = clienteId;
    this.filialId = filialId;
    this.enderecoSelecionadoId.set(preset?.enderecoId ?? null);
    this.preview.set(null);
    this.observacao.set(preset?.observacao ?? '');
    this.erro.set('');
    this.enderecos.set([]);
    this.mostrarOpcoesCaixa.set(preset?.mostrarOpcoesCaixa ?? false);
    this.vendaRecebida.set(false);
    this.despacharAgora.set(false);
    this.entregadorId.set(null);
    this.entregadores.set([]);
    this.visivel.set(true);
    await this.carregarEnderecos();
    if (this.mostrarOpcoesCaixa()) this.carregarEntregadores();
    return new Promise(resolve => { this.resolver = resolve; });
  }

  async carregarEntregadores() {
    if (this.entregadores().length > 0) return;
    this.carregandoEntregadores.set(true);
    try {
      const r: any = await this.http.get(`${environment.apiUrl}/colaboradores`).toPromise();
      const lista: ColaboradorOpcao[] = (r?.data ?? []).filter((c: ColaboradorOpcao) => c.ativo);
      this.entregadores.set(lista);
    } catch { /* silencioso — select mostra "sem opções" */ }
    finally { this.carregandoEntregadores.set(false); }
  }

  toggleDespacharAgora(checked: boolean) {
    this.despacharAgora.set(checked);
    if (!checked) this.entregadorId.set(null);
  }

  private async carregarEnderecos() {
    this.carregandoEnderecos.set(true);
    try {
      const r: any = await this.http.get(`${environment.apiUrl}/clientes/${this.clienteId}`).toPromise();
      const lista: EnderecoClienteOpcao[] = r?.data?.enderecos ?? [];
      this.enderecos.set(lista);
      if (lista.length === 0) {
        this.erro.set('Cliente não tem endereço cadastrado. Cadastre um endereço com coordenadas antes de usar entrega.');
        return;
      }
      const principal = lista.find(e => e.principal) ?? lista[0];
      this.enderecoSelecionadoId.set(principal.id);
      await this.recalcular();
    } catch {
      this.erro.set('Erro ao carregar endereços do cliente.');
    } finally {
      this.carregandoEnderecos.set(false);
    }
  }

  async onEnderecoChange(id: number) {
    this.enderecoSelecionadoId.set(id);
    await this.recalcular();
  }

  async recalcular() {
    const id = this.enderecoSelecionadoId();
    if (!id) { this.preview.set(null); return; }
    this.calculando.set(true);
    this.erro.set('');
    try {
      const r: any = await this.http.get(
        `${environment.apiUrl}/entregas/calcular?filialId=${this.filialId}&enderecoId=${id}`
      ).toPromise();
      this.preview.set(r?.data ?? null);
    } catch (e: any) {
      this.preview.set(null);
      this.erro.set(e?.error?.message ?? 'Erro ao calcular entrega.');
    } finally {
      this.calculando.set(false);
    }
  }

  confirmar() {
    const id = this.enderecoSelecionadoId();
    const prev = this.preview();
    if (!id || !prev) {
      this.erro.set('Selecione um endereço válido e aguarde o cálculo.');
      return;
    }
    if (this.despacharAgora() && !this.entregadorId()) {
      this.erro.set('Selecione um entregador para despachar agora.');
      return;
    }
    this.visivel.set(false);
    this.resolver?.({
      enderecoEntregaId: id,
      valorEntrega: prev.valorEntrega,
      distanciaKm: prev.distanciaKm,
      observacao: this.observacao().trim() || undefined,
      vendaRecebida: this.vendaRecebida(),
      despacharAgora: this.despacharAgora(),
      entregadorId: this.despacharAgora() ? this.entregadorId() ?? undefined : undefined
    });
    this.resolver = null;
  }

  cancelar() {
    this.visivel.set(false);
    this.resolver?.(null);
    this.resolver = null;
  }
}
