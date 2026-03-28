import { Component, signal, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';

interface Mensagem {
  role: 'user' | 'assistant';
  content: string;
  acao?: string | null;
  timestamp: Date;
}

@Component({
  selector: 'app-cassi',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cassi.component.html',
  styleUrl: './cassi.component.scss'
})
export class CassiComponent implements AfterViewChecked {
  @ViewChild('chatBody') chatBody!: ElementRef;
  @ViewChild('inputRef') inputRef!: ElementRef;

  aberto = signal(false);
  mensagens = signal<Mensagem[]>([]);
  inputTexto = '';
  enviando = signal(false);
  private shouldScroll = false;

  constructor(
    private http: HttpClient,
    private router: Router,
    private tabService: TabService
  ) {}

  ngAfterViewChecked() {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  toggle() {
    this.aberto.update(v => !v);
    if (this.aberto()) {
      if (this.mensagens().length === 0) {
        this.mensagens.set([{
          role: 'assistant',
          content: 'Olá! Sou a **Cassi**, sua assistente virtual. Como posso te ajudar?',
          timestamp: new Date()
        }]);
      }
      setTimeout(() => this.inputRef?.nativeElement?.focus(), 200);
    }
  }

  async enviar() {
    const texto = this.inputTexto.trim();
    if (!texto || this.enviando()) return;

    this.inputTexto = '';
    this.mensagens.update(msgs => [...msgs, { role: 'user', content: texto, timestamp: new Date() }]);
    this.enviando.set(true);
    this.shouldScroll = true;

    const historico = this.mensagens()
      .filter(m => m.role === 'user' || m.role === 'assistant')
      .slice(-10)
      .map(m => ({ role: m.role, content: m.content }));

    this.http.post<any>(`${environment.apiUrl}/assistente/chat`, {
      mensagem: texto,
      historico: historico.slice(0, -1) // exclude current message (already sent as mensagem)
    }).subscribe({
      next: r => {
        this.enviando.set(false);
        const data = r.data;
        this.mensagens.update(msgs => [...msgs, {
          role: 'assistant',
          content: data.mensagem || 'Desculpe, não consegui processar sua pergunta.',
          acao: data.acao,
          timestamp: new Date()
        }]);
        this.shouldScroll = true;

        // Auto-navigate if action returned
        if (data.acao) {
          setTimeout(() => {
            const label = this.getLabel(data.acao);
            this.tabService.abrirTab({ id: data.acao, titulo: label, rota: data.acao, iconKey: 'pill' });
            this.router.navigate([data.acao]);
          }, 800);
        }
      },
      error: () => {
        this.enviando.set(false);
        this.mensagens.update(msgs => [...msgs, {
          role: 'assistant',
          content: 'Desculpe, estou com dificuldade para responder. Verifique sua conexão e tente novamente.',
          timestamp: new Date()
        }]);
        this.shouldScroll = true;
      }
    });
  }

  onKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.enviar();
    }
  }

  executarAcao(acao: string) {
    const label = this.getLabel(acao);
    this.tabService.abrirTab({ id: acao, titulo: label, rota: acao, iconKey: 'pill' });
    this.router.navigate([acao]);
  }

  formatarMensagem(text: string): string {
    // Simple markdown: **bold**
    return text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
  }

  private getLabel(rota: string): string {
    const map: Record<string, string> = {
      '/erp/colaboradores': 'Colaboradores',
      '/erp/fornecedores': 'Fornecedores',
      '/erp/fabricantes': 'Fabricantes',
      '/erp/substancias': 'Substâncias',
      '/erp/gerenciar-produtos': 'Gerenciar Produtos',
      '/erp/filiais': 'Filiais',
      '/erp/grupos': 'Grupo de Usuários',
      '/erp/configuracoes': 'Configurações',
      '/erp/log-geral': 'Log Geral',
      '/erp/sync': 'Sincronização',
      '/erp/sistema': 'Sistema',
    };
    return map[rota] || rota.split('/').pop() || 'Tela';
  }

  private scrollToBottom() {
    try {
      const el = this.chatBody?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
