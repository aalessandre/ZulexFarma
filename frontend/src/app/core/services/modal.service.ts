import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export type ModalTipo = 'aviso' | 'confirmacao' | 'sucesso' | 'erro' | 'permissao';

export interface ModalConfig {
  tipo: ModalTipo;
  titulo: string;
  mensagem: string;
  // Para confirmação
  textoBotaoConfirmar?: string;
  textoBotaoCancelar?: string;
  // Para permissão (liberação por senha)
  tela?: string;
  acao?: string;
  entidade?: string;
  registroId?: string;
}

export interface ModalResultado {
  confirmado: boolean;
  liberado?: boolean;
  supervisorNome?: string;
  tokenLiberacao?: string;
}

@Injectable({ providedIn: 'root' })
export class ModalService {
  // Estado da modal
  visivel = signal(false);
  config = signal<ModalConfig>({ tipo: 'aviso', titulo: '', mensagem: '' });

  // Campos de liberação
  loginSupervisor = signal('');
  senhaSupervisor = signal('');
  erroLiberacao = signal('');
  liberando = signal(false);

  private resolver: ((resultado: ModalResultado) => void) | null = null;

  constructor(private http: HttpClient) {}

  /** Mostra aviso simples (só OK) */
  aviso(titulo: string, mensagem: string): Promise<ModalResultado> {
    return this.abrir({ tipo: 'aviso', titulo, mensagem });
  }

  /** Mostra mensagem de sucesso */
  sucesso(titulo: string, mensagem: string): Promise<ModalResultado> {
    return this.abrir({ tipo: 'sucesso', titulo, mensagem });
  }

  /** Mostra mensagem de erro */
  erro(titulo: string, mensagem: string): Promise<ModalResultado> {
    return this.abrir({ tipo: 'erro', titulo, mensagem });
  }

  /** Mostra confirmação (Sim/Não) */
  confirmar(titulo: string, mensagem: string, textoBotaoConfirmar = 'Sim, confirmar', textoBotaoCancelar = 'Não, cancelar'): Promise<ModalResultado> {
    return this.abrir({ tipo: 'confirmacao', titulo, mensagem, textoBotaoConfirmar, textoBotaoCancelar });
  }

  /** Mostra modal de permissão negada com opção de liberação por senha */
  permissao(tela: string, acao: string, entidade?: string, registroId?: string): Promise<ModalResultado> {
    const nomeTela = tela.charAt(0).toUpperCase() + tela.slice(1);
    const nomeAcao = acao === 'c' ? 'Consultar' : acao === 'i' ? 'Incluir' : acao === 'a' ? 'Alterar' : 'Excluir';
    return this.abrir({
      tipo: 'permissao',
      titulo: 'Permissão Necessária',
      mensagem: `Você não tem permissão para esta ação.\n\nTela: ${nomeTela}\nFunção: ${nomeAcao}\n\nSolicite a liberação de um supervisor.`,
      tela, acao, entidade, registroId
    });
  }

  /** Fecha a modal com resultado */
  fechar(confirmado = false) {
    this.visivel.set(false);
    this.loginSupervisor.set('');
    this.senhaSupervisor.set('');
    this.erroLiberacao.set('');
    if (this.resolver) {
      this.resolver({ confirmado });
      this.resolver = null;
    }
  }

  /** Confirma a ação */
  confirmarAcao() {
    this.visivel.set(false);
    if (this.resolver) {
      this.resolver({ confirmado: true });
      this.resolver = null;
    }
  }

  /** Tenta liberar por senha de supervisor */
  liberarPorSenha() {
    const cfg = this.config();
    if (!cfg.tela || !cfg.acao) return;

    const login = this.loginSupervisor();
    const senha = this.senhaSupervisor();

    if (!login || !senha) {
      this.erroLiberacao.set('Informe o login e a senha do supervisor.');
      return;
    }

    this.liberando.set(true);
    this.erroLiberacao.set('');

    this.http.post<any>(`${environment.apiUrl}/auth/liberar`, {
      login, senha,
      tela: cfg.tela,
      acao: cfg.acao,
      entidade: cfg.entidade ?? null,
      registroId: cfg.registroId ?? null
    }).subscribe({
      next: r => {
        this.liberando.set(false);
        if (r.success) {
          this.visivel.set(false);
          this.loginSupervisor.set('');
          this.senhaSupervisor.set('');
          if (this.resolver) {
            this.resolver({ confirmado: true, liberado: true, supervisorNome: r.supervisorNome, tokenLiberacao: r.tokenLiberacao });
            this.resolver = null;
          }
        } else {
          this.erroLiberacao.set(r.message || 'Erro na liberação.');
        }
      },
      error: () => {
        this.liberando.set(false);
        this.erroLiberacao.set('Erro ao comunicar com o servidor.');
      }
    });
  }

  private abrir(config: ModalConfig): Promise<ModalResultado> {
    this.config.set(config);
    this.loginSupervisor.set('');
    this.senhaSupervisor.set('');
    this.erroLiberacao.set('');
    this.liberando.set(false);
    this.visivel.set(true);
    return new Promise(resolve => { this.resolver = resolve; });
  }
}
