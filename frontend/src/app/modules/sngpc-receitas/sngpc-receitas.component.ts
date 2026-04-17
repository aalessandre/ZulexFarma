import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';
import { AuthService } from '../../core/services/auth.service';
import { SngpcModalComponent } from '../caixa-venda/sngpc-modal/sngpc-modal.component';

interface VendaSngpc {
  vendaId: number | null;
  receitaId: number | null;
  codigo?: string;
  dataFinalizacao?: string;
  clienteNome?: string;
  qtdeItensControlados: number;
  qtdeTotal: number;
  status: 'Pendente' | 'Lançada' | 'Manual';
  qtdeReceitas: number;
}

interface DetalheItem {
  produtoNome: string;
  classeTerapeutica?: string;
  quantidade: number;
  numeroLote?: string;
  dataValidade?: string;
  origem: string;
}

type Filtro = 'todas' | 'pendentes' | 'lancadas' | 'manuais';

@Component({
  selector: 'app-sngpc-receitas',
  standalone: true,
  imports: [CommonModule, FormsModule, SngpcModalComponent],
  templateUrl: './sngpc-receitas.component.html',
  styleUrl: './sngpc-receitas.component.scss'
})
export class SngpcReceitasComponent implements OnInit {
  private api = `${environment.apiUrl}/sngpc/vendas`;

  vendas = signal<VendaSngpc[]>([]);
  carregando = signal(false);
  filtro = signal<Filtro>('todas');
  dataInicio = signal<string>(this.hoje(-30));
  dataFim = signal<string>(this.hoje(0));

  totais = computed(() => {
    const lista = this.vendas();
    return {
      total: lista.length,
      pendentes: lista.filter(v => v.status === 'Pendente').length,
      lancadas: lista.filter(v => v.status === 'Lançada').length,
      manuais:  lista.filter(v => v.status === 'Manual').length
    };
  });

  sngpcModalAberta = signal(false);
  sngpcVendaId = signal<number | null>(null);
  sngpcModoManual = signal(false);
  filialId = signal<number>(0);

  // Expansão inline
  linhaExpandida = signal<string | null>(null); // chave = v.vendaId ?? ('m-' + receitaId)
  detalheItens = signal<DetalheItem[]>([]);
  detalheLoading = signal(false);

  constructor(
    private http: HttpClient,
    private tab: TabService,
    private modal: ModalService,
    private auth: AuthService
  ) {
    const u = this.auth.usuarioLogado();
    this.filialId.set(parseInt(u?.filialId || '1', 10));
  }

  ngOnInit() { this.carregar(); }

  carregar() {
    this.carregando.set(true);
    const params: string[] = [`filtro=${this.filtro()}`];
    if (this.dataInicio()) params.push(`dataInicio=${this.dataInicio()}`);
    if (this.dataFim()) params.push(`dataFim=${this.dataFim()}`);
    const qs = '?' + params.join('&');
    this.http.get<any>(`${this.api}/receitas${qs}`).subscribe({
      next: r => { this.vendas.set(r.data ?? []); this.carregando.set(false); },
      error: () => this.carregando.set(false)
    });
  }

  trocarFiltro(f: Filtro) {
    this.filtro.set(f);
    this.carregar();
  }

  async lancarReceitas(v: VendaSngpc) {
    if (v.status === 'Lançada') {
      await this.modal.aviso('Já lançada', 'Esta venda já tem receitas registradas.');
      return;
    }
    if (v.vendaId == null) return;
    this.sngpcModoManual.set(false);
    this.sngpcVendaId.set(v.vendaId);
    this.sngpcModalAberta.set(true);
  }

  abrirReceitaManual() {
    this.sngpcModoManual.set(true);
    this.sngpcVendaId.set(null);
    this.sngpcModalAberta.set(true);
  }

  onConfirmado(ev: { receitas: any[]; lancarDepois: boolean }) {
    if (ev.lancarDepois) { this.sngpcModalAberta.set(false); return; }

    // Modo manual: usa o primeiro (único) item de receitas
    if (this.sngpcModoManual()) {
      if (ev.receitas.length === 0) return;
      const payload = { filialId: this.filialId(), receita: ev.receitas[0] };
      this.http.post(`${environment.apiUrl}/sngpc/vendas/receita-manual`, payload).subscribe({
        next: () => {
          this.modal.sucesso('OK', 'Receita manual registrada.');
          this.fecharModal();
          this.carregar();
        },
        error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao registrar receita manual.')
      });
      return;
    }

    // Modo venda: lançamento retroativo em venda existente
    const id = this.sngpcVendaId();
    if (!id) return;
    this.http.post(`${environment.apiUrl}/sngpc/vendas/${id}/receitas`, ev.receitas).subscribe({
      next: () => {
        this.modal.sucesso('OK', 'Receitas registradas.');
        this.fecharModal();
        this.carregar();
      },
      error: (e: any) => this.modal.erro('Erro', e?.error?.message || 'Erro ao registrar receitas.')
    });
  }

  onCancelado() { this.fecharModal(); }

  private fecharModal() {
    this.sngpcModalAberta.set(false);
    this.sngpcVendaId.set(null);
    this.sngpcModoManual.set(false);
  }

  // Expansão inline — busca detalhes ao expandir
  chaveLinha(v: VendaSngpc): string {
    return v.vendaId !== null ? `v-${v.vendaId}` : `m-${v.receitaId}`;
  }

  toggleExpandir(v: VendaSngpc) {
    const chave = this.chaveLinha(v);
    if (this.linhaExpandida() === chave) {
      this.linhaExpandida.set(null);
      this.detalheItens.set([]);
      return;
    }
    this.linhaExpandida.set(chave);
    this.detalheItens.set([]);
    this.detalheLoading.set(true);
    const params: string[] = [];
    if (v.vendaId !== null) params.push(`vendaId=${v.vendaId}`);
    if (v.receitaId !== null) params.push(`receitaId=${v.receitaId}`);
    this.http.get<any>(`${this.api}/detalhes?${params.join('&')}`).subscribe({
      next: r => { this.detalheItens.set(r.data ?? []); this.detalheLoading.set(false); },
      error: () => this.detalheLoading.set(false)
    });
  }

  sairDaTela() { this.tab.fecharTabAtiva(); }

  private hoje(offsetDias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDias);
    return d.toISOString().slice(0, 10);
  }
}
