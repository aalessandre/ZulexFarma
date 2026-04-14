import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';

interface SangriaPendente {
  id: number;
  codigo?: string;
  dataMovimento: string;
  valor: number;
  descricao: string;
  observacao?: string;
  usuarioNome?: string;
}

@Component({
  selector: 'app-sangrias-pendentes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sangrias-pendentes.component.html',
  styleUrl: './sangrias-pendentes.component.scss'
})
export class SangriasPendentesComponent implements OnInit {
  private apiUrl = environment.apiUrl;

  sangrias = signal<SangriaPendente[]>([]);
  carregando = signal(false);
  scanInput = signal('');

  constructor(
    private http: HttpClient,
    private tabService: TabService,
    private auth: AuthService,
    private modal: ModalService
  ) {}

  ngOnInit() { this.carregar(); }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregar() {
    const usuario = this.auth.usuarioLogado();
    const filialId = parseInt(usuario?.filialId || '1', 10);
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/caixamovimentos/sangrias-pendentes?filialId=${filialId}`).subscribe({
      next: r => {
        this.sangrias.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  imprimirCanhoto(id: number) {
    this.http.get(`${this.apiUrl}/caixamovimentos/${id}/canhoto`, { responseType: 'text' }).subscribe({
      next: (html: string) => {
        const win = window.open('', '_blank', 'width=400,height=700');
        if (win) { win.document.open(); win.document.write(html); win.document.close(); }
      },
      error: () => this.modal.erro('Canhoto', 'Erro ao gerar canhoto.')
    });
  }

  confirmar(s: SangriaPendente) {
    this.http.post<any>(`${this.apiUrl}/caixamovimentos/${s.id}/confirmar-sangria`, {}).subscribe({
      next: () => {
        this.modal.sucesso('Sangria confirmada', 'O valor foi registrado na Conta Cofre.');
        this.carregar();
      },
      error: (err: any) => this.modal.erro('Erro', err?.error?.message || 'Erro ao confirmar sangria.')
    });
  }

  bipar() {
    const codigo = this.scanInput().trim();
    if (!codigo) return;
    // Busca a sangria pelo código e confirma
    const s = this.sangrias().find(x => x.codigo === codigo);
    if (!s) {
      this.modal.aviso('Canhoto não encontrado', 'Nenhuma sangria pendente com este código.');
      this.scanInput.set('');
      return;
    }
    this.confirmar(s);
    this.scanInput.set('');
  }
}
