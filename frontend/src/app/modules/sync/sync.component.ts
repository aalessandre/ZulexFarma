import { Component, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface FilialItem { id: number; nomeFantasia: string; }
interface SyncStatus {
  tabela: string;
  ultimaVersaoEnviada: number;
  ultimaVersaoRecebida: number;
  ultimoSync: string | null;
  status: string;
  pendentesEnvio: number;
}

@Component({
  selector: 'app-sync',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sync.component.html',
  styleUrl: './sync.component.scss'
})
export class SyncComponent implements OnInit, OnDestroy {
  filiais = signal<FilialItem[]>([]);
  filialSelecionada = signal<number>(0);
  statusList = signal<SyncStatus[]>([]);
  carregando = signal(false);
  sincronizando = signal(false);
  private refreshInterval: any = null;

  private apiUrl = `${environment.apiUrl}/sync`;

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() {
    this.carregarFiliais();
  }

  ngOnDestroy() {
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregarFiliais() {
    this.http.get<any>(`${environment.apiUrl}/filiais`).subscribe({
      next: r => {
        this.filiais.set(r.data ?? []);
        if (this.filiais().length > 0 && !this.filialSelecionada()) {
          this.filialSelecionada.set(this.filiais()[0].id);
          this.carregarStatus();
        }
      }
    });
  }

  onFilialChange(id: number) {
    this.filialSelecionada.set(id);
    this.carregarStatus();
  }

  carregarStatus() {
    const filialId = this.filialSelecionada();
    if (!filialId) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/status/${filialId}`).subscribe({
      next: r => {
        this.statusList.set(r.data ?? []);
        this.carregando.set(false);
        this.iniciarAutoRefresh();
      },
      error: () => this.carregando.set(false)
    });
  }

  private iniciarAutoRefresh() {
    if (this.refreshInterval) clearInterval(this.refreshInterval);
    this.refreshInterval = setInterval(() => this.carregarStatus(), 30000);
  }

  async executarSync() {
    const filialId = this.filialSelecionada();
    if (!filialId) return;
    const r = await this.modal.confirmar('Sincronizar', `Executar sincronização completa para esta filial?`, 'Sim, sincronizar', 'Cancelar');
    if (!r.confirmado) return;

    this.sincronizando.set(true);
    this.http.post<any>(`${this.apiUrl}/executar/${filialId}`, {}).subscribe({
      next: async res => {
        this.sincronizando.set(false);
        this.carregarStatus();
        await this.modal.sucesso('Sincronizado', 'Sincronização executada com sucesso.');
      },
      error: async () => {
        this.sincronizando.set(false);
        await this.modal.erro('Erro', 'Erro ao executar sincronização.');
      }
    });
  }

  statusCss(status: string): string {
    if (status === 'OK') return 'status-ok';
    if (status === 'ERRO') return 'status-erro';
    if (status === 'LOCAL') return 'status-local';
    return 'status-pendente';
  }

  totalPendentes = computed(() => this.statusList().reduce((sum, s) => sum + s.pendentesEnvio, 0));
}
