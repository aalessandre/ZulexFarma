import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';
import { SngpcModalComponent } from '../caixa-venda/sngpc-modal/sngpc-modal.component';

interface VendaPendente {
  vendaId: number;
  codigo?: string;
  dataFinalizacao?: string;
  clienteNome?: string;
  qtdeItensControlados: number;
  qtdeTotal: number;
}

@Component({
  selector: 'app-sngpc-pendentes',
  standalone: true,
  imports: [CommonModule, FormsModule, SngpcModalComponent],
  templateUrl: './sngpc-pendentes.component.html',
  styleUrl: './sngpc-pendentes.component.scss'
})
export class SngpcPendentesComponent implements OnInit {
  private api = `${environment.apiUrl}/sngpc/vendas`;

  pendentes = signal<VendaPendente[]>([]);
  carregando = signal(false);

  sngpcModalAberta = signal(false);
  sngpcVendaId = signal<number | null>(null);

  constructor(
    private http: HttpClient,
    private tab: TabService,
    private modal: ModalService
  ) {}

  ngOnInit() { this.carregar(); }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.api}/pendentes`).subscribe({
      next: r => { this.pendentes.set(r.data ?? []); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  abrirModal(v: VendaPendente) {
    this.sngpcVendaId.set(v.vendaId);
    this.sngpcModalAberta.set(true);
  }

  onConfirmado(ev: { receitas: any[]; lancarDepois: boolean }) {
    const id = this.sngpcVendaId();
    if (!id) return;
    if (ev.lancarDepois) { this.sngpcModalAberta.set(false); return; }
    this.http.post(`${environment.apiUrl}/sngpc/vendas/${id}/receitas`, ev.receitas).subscribe({
      next: () => {
        this.modal.sucesso('OK', 'Receitas registradas.');
        this.sngpcModalAberta.set(false);
        this.sngpcVendaId.set(null);
        this.carregar();
      },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao registrar receitas.')
    });
  }

  onCancelado() {
    this.sngpcModalAberta.set(false);
    this.sngpcVendaId.set(null);
  }

  sairDaTela() { this.tab.fecharTabAtiva(); }
}
