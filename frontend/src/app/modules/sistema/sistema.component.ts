import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { ModalService } from '../../core/services/modal.service';

interface SistemaInfo {
  versao: string;
  build: string;
  dotnet: string;
  os: string;
  maquina: string;
  processadores: number;
  memoriaAtual: number;
  uptime: string;
  syncHabilitado: boolean;
  atualizacaoHabilitada: boolean;
}

interface AtualizacaoStatus {
  atualizacaoDisponivel: boolean;
  versaoDisponivel: string | null;
  versaoAtual: string;
  descricao: string | null;
  ultimaVerificacao: string | null;
}

@Component({
  selector: 'app-sistema',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sistema.component.html',
  styleUrl: './sistema.component.scss'
})
export class SistemaComponent implements OnInit {
  info = signal<SistemaInfo | null>(null);
  atualizacao = signal<AtualizacaoStatus | null>(null);
  carregando = signal(false);
  verificando = signal(false);

  private apiUrl = `${environment.apiUrl}/sistema`;

  constructor(private http: HttpClient, private tabService: TabService, private modal: ModalService) {}

  ngOnInit() {
    this.carregar();
  }

  sairDaTela() { this.tabService.fecharTabAtiva(); }

  carregar() {
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/info`).subscribe({
      next: r => {
        this.info.set(r);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });

    this.http.get<any>(`${this.apiUrl}/atualizacao/status`).subscribe({
      next: r => this.atualizacao.set(r)
    });
  }

  verificarAtualizacao() {
    this.verificando.set(true);
    this.http.get<any>(`${this.apiUrl}/atualizacao/verificar`).subscribe({
      next: async r => {
        this.verificando.set(false);
        if (r.atualizado) {
          await this.modal.sucesso('Sistema Atualizado', `Você está na versão mais recente (${r.versaoAtual}).`);
        } else {
          await this.modal.aviso('Atualização Disponível',
            `Versão atual: ${r.versaoAtual}\nVersão disponível: ${r.versaoDisponivel}\n\n${r.descricao || 'Nova versão disponível para download.'}`);
        }
        this.carregar();
      },
      error: async () => {
        this.verificando.set(false);
        await this.modal.erro('Erro', 'Não foi possível verificar atualizações.');
      }
    });
  }
}
