import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { TabService } from '../../core/services/tab.service';

interface PagamentoPendente {
  vendaId: number;
  terminalId: number;
  terminalNumero: number;
  terminalApelido?: string | null;
  totalLiquido: number;
  totalItens: number;
  formaPagamento: string;
  criadoEm: string;
}

interface ApiResp<T> { success: boolean; data?: T; message?: string; }

@Component({
  selector: 'app-self-checkout-pendentes',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './self-checkout-pendentes.component.html',
  styleUrl: './self-checkout-pendentes.component.scss'
})
export class SelfCheckoutPendentesComponent implements OnInit, OnDestroy {
  pendentes = signal<PagamentoPendente[]>([]);
  carregando = signal(false);
  acaoEmAndamento = signal<number | null>(null);
  ultimaAtualizacao = signal<Date | null>(null);

  private filialId = 1;
  private pollingTimer: any = null;
  private base = `${environment.apiUrl}/self-checkout`;

  constructor(
    private http: HttpClient,
    private auth: AuthService,
    private modal: ModalService,
    private tabService: TabService
  ) {}

  ngOnInit(): void {
    this.filialId = parseInt(this.auth.usuarioLogado()?.filialId || '1', 10);
    this.carregar();
    this.pollingTimer = setInterval(() => this.carregar(true), 2000);
  }

  ngOnDestroy(): void {
    if (this.pollingTimer) clearInterval(this.pollingTimer);
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  async carregar(silencioso = false) {
    if (!silencioso) this.carregando.set(true);
    try {
      const r = await firstValueFrom(
        this.http.get<ApiResp<PagamentoPendente[]>>(`${this.base}/filial/${this.filialId}/pagamentos-pendentes`)
      );
      this.pendentes.set(r?.data ?? []);
      this.ultimaAtualizacao.set(new Date());
    } catch {
      // mantém última lista boa
    } finally {
      if (!silencioso) this.carregando.set(false);
    }
  }

  async confirmar(p: PagamentoPendente) {
    if (this.acaoEmAndamento() !== null) return;
    this.acaoEmAndamento.set(p.vendaId);
    try {
      const r = await firstValueFrom(
        this.http.post<ApiResp<{ nfceAutorizada: boolean; numeroNfce?: number; serieNfce?: number; mensagem?: string }>>(
          `${this.base}/venda/${p.vendaId}/confirmar`, {}
        )
      );
      const data = r?.data;
      if (data?.nfceAutorizada) {
        this.modal.sucesso('NFC-e autorizada', `Série ${data.serieNfce} · Nº ${data.numeroNfce}`);
      } else {
        this.modal.erro('NFC-e rejeitada', data?.mensagem ?? 'Falha desconhecida.');
      }
    } catch (e: any) {
      this.modal.erro('Erro', e?.error?.message ?? 'Falha ao confirmar pagamento.');
    } finally {
      this.acaoEmAndamento.set(null);
      this.carregar(true);
    }
  }

  async recusar(p: PagamentoPendente) {
    if (this.acaoEmAndamento() !== null) return;
    if (!confirm(`Recusar a venda #${p.vendaId} do terminal ${p.terminalNumero}?`)) return;

    this.acaoEmAndamento.set(p.vendaId);
    try {
      await firstValueFrom(
        this.http.post<ApiResp<void>>(`${this.base}/venda/${p.vendaId}/cancelar`,
          { motivo: 'Pagamento recusado pelo atendente' })
      );
    } catch (e: any) {
      this.modal.erro('Erro', e?.error?.message ?? 'Falha ao recusar.');
    } finally {
      this.acaoEmAndamento.set(null);
      this.carregar(true);
    }
  }

  formatarHora(iso: string): string {
    try { return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit', second: '2-digit' }); }
    catch { return iso; }
  }
}
