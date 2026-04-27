import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TerminalKiosk {
  id: number;
  filialId: number;
  numero: number;
  apelido?: string | null;
  ativo: boolean;
}

export interface ProdutoKiosk {
  codigoExterno: string;
  codigoBarras: string | null;
  nome: string;
  ncm?: string | null;
  unidade?: string | null;
  precoCheio: number;
  precoFinal: number;
  emPromocao: boolean;
  estoqueAtual?: number | null;
}

interface ApiResponse<T> { success: boolean; data?: T; message?: string; }

export type FormaPagamentoKiosk = 'PIX' | 'CARTAO';
// Backend usa enum int: PIX=1, CARTAO=2
const FORMA_PAGAMENTO_INT: Record<FormaPagamentoKiosk, number> = { PIX: 1, CARTAO: 2 };

export type StatusVendaKiosk =
  | 'AguardandoFormaPagamento'
  | 'AguardandoAtendente'
  | 'NfceAutorizada'
  | 'Cancelada'
  | 'Erro';

const STATUS_MAP: Record<number, StatusVendaKiosk> = {
  1: 'AguardandoFormaPagamento',
  2: 'AguardandoAtendente',
  3: 'NfceAutorizada',
  4: 'Cancelada',
  5: 'Erro'
};

export interface VendaKioskItemResult {
  vendaItemId: number;
  codigoExterno: string;
  nome: string;
  precoUnitario: number;
  quantidade: number;
  total: number;
  emPromocao: boolean;
}

export interface IniciarVendaKioskResult {
  vendaId: number;
  totalLiquido: number;
  totalItens: number;
  itens: VendaKioskItemResult[];
}

export interface StatusVendaKioskResult {
  vendaId: number;
  status: StatusVendaKiosk;
  chaveAcesso?: string;
  numeroNfce?: number;
  serieNfce?: number;
  mensagem?: string;
}

@Injectable({ providedIn: 'root' })
export class KioskApiService {
  private base = `${environment.apiUrl}/self-checkout`;

  constructor(private http: HttpClient) {}

  async listarTerminais(filialId: number): Promise<TerminalKiosk[]> {
    const r = await firstValueFrom(
      this.http.get<ApiResponse<TerminalKiosk[]>>(`${this.base}/filial/${filialId}/terminais`)
    );
    return r?.data ?? [];
  }

  async buscarPorEan(filialId: number, ean: string): Promise<ProdutoKiosk | null> {
    try {
      const r = await firstValueFrom(
        this.http.get<ApiResponse<ProdutoKiosk>>(`${this.base}/filial/${filialId}/produto/ean/${encodeURIComponent(ean)}`)
      );
      return r?.data ?? null;
    } catch (e: any) {
      if (e?.status === 404) return null;
      throw e;
    }
  }

  async buscarPorNome(filialId: number, termo: string, top = 20): Promise<ProdutoKiosk[]> {
    if (!termo?.trim()) return [];
    const r = await firstValueFrom(
      this.http.get<ApiResponse<ProdutoKiosk[]>>(`${this.base}/filial/${filialId}/produto/busca`, {
        params: { q: termo.trim(), top }
      })
    );
    return r?.data ?? [];
  }

  async iniciarVenda(filialId: number, terminalId: number, itens: { codigoExterno: string; quantidade: number }[]): Promise<IniciarVendaKioskResult> {
    const r = await firstValueFrom(
      this.http.post<ApiResponse<IniciarVendaKioskResult>>(`${this.base}/filial/${filialId}/venda`, { terminalId, itens })
    );
    return r!.data!;
  }

  async registrarPagamento(vendaId: number, forma: FormaPagamentoKiosk): Promise<void> {
    await firstValueFrom(
      this.http.post<ApiResponse<void>>(`${this.base}/venda/${vendaId}/pagamento`, {
        formaPagamento: FORMA_PAGAMENTO_INT[forma]
      })
    );
  }

  async cancelarVenda(vendaId: number, motivo?: string): Promise<void> {
    await firstValueFrom(
      this.http.post<ApiResponse<void>>(`${this.base}/venda/${vendaId}/cancelar`, { motivo })
    );
  }

  async statusVenda(vendaId: number): Promise<StatusVendaKioskResult | null> {
    try {
      const r = await firstValueFrom(
        this.http.get<ApiResponse<{ vendaId: number; status: number | string; chaveAcesso?: string; numeroNfce?: number; serieNfce?: number; mensagem?: string }>>(
          `${this.base}/venda/${vendaId}/status-kiosk`
        )
      );
      const raw = r?.data;
      if (!raw) return null;
      // Backend pode serializar enum como número (default) ou string. Suporta ambos.
      const statusNorm = typeof raw.status === 'number'
        ? STATUS_MAP[raw.status]
        : (raw.status as StatusVendaKiosk);
      return { ...raw, status: statusNorm };
    } catch (e: any) {
      if (e?.status === 404) return null;
      throw e;
    }
  }
}
