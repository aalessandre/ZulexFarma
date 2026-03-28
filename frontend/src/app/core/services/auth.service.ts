import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, UsuarioLogado } from '../models/auth.model';
import { TabService } from './tab.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'zulex_token';
  private readonly USER_KEY = 'zulex_usuario';

  private permissoes: Record<string, string> = {};

  usuarioLogado = signal<UsuarioLogado | null>(this.carregarUsuarioStorage());

  // Session timers
  private sessaoMaxima = 0;
  private inatividade = 0;
  private timerSessao: any = null;
  private timerInatividade: any = null;
  private timerAviso: any = null;
  avisoSessao = signal('');

  constructor(private http: HttpClient, private router: Router, private tabService: TabService) {
    const token = this.getToken();
    if (token) {
      this.extrairPermissoes(token);
      this.extrairSessao(token);
      this.iniciarTimers();
    }
  }

  login(request: LoginRequest): Observable<{ success: boolean; data: LoginResponse }> {
    return this.http.post<{ success: boolean; data: LoginResponse }>(
      `${environment.apiUrl}/auth/login`, request
    ).pipe(
      tap(response => {
        if (response.success) {
          // Limpar estado anterior (abas abertas, forms em edição)
          sessionStorage.clear();

          const usuario: UsuarioLogado = {
            ...response.data,
            expiracao: new Date(response.data.expiracao)
          };
          localStorage.setItem(this.TOKEN_KEY, response.data.token);
          localStorage.setItem(this.USER_KEY, JSON.stringify(usuario));
          this.usuarioLogado.set(usuario);
          this.extrairPermissoes(response.data.token);
          this.extrairSessao(response.data.token);
          this.iniciarTimers();
        }
      })
    );
  }

  temPermissao(tela: string, acao: string): boolean {
    const usuario = this.usuarioLogado();
    if (!usuario) return false;
    if (usuario.isAdministrador) return true;
    const acoes = this.permissoes[tela];
    return acoes ? acoes.includes(acao) : false;
  }

  private extrairPermissoes(token: string) {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.permissoes = payload.permissoes ? JSON.parse(payload.permissoes) : {};
    } catch {
      this.permissoes = {};
    }
  }

  private extrairSessao(token: string) {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.sessaoMaxima = parseInt(payload.sessaoMaxima || '0');
      this.inatividade = parseInt(payload.inatividade || '0');
    } catch {
      this.sessaoMaxima = 0;
      this.inatividade = 0;
    }
  }

  iniciarTimers() {
    this.pararTimers();

    // Timer de sessão máxima
    if (this.sessaoMaxima > 0) {
      const ms = this.sessaoMaxima * 60 * 1000;
      const avisoMs = Math.max(0, ms - 5 * 60 * 1000); // aviso 5 min antes

      this.timerAviso = setTimeout(() => {
        this.avisoSessao.set('Sua sessão expira em 5 minutos.');
      }, avisoMs);

      this.timerSessao = setTimeout(() => {
        this.logout();
      }, ms);
    }

    // Timer de inatividade
    if (this.inatividade > 0) {
      this.resetarInatividade();
      document.addEventListener('mousemove', this.onAtividade);
      document.addEventListener('keydown', this.onAtividade);
      document.addEventListener('click', this.onAtividade);
    }
  }

  private onAtividade = () => {
    this.resetarInatividade();
  };

  private resetarInatividade() {
    if (this.timerInatividade) clearTimeout(this.timerInatividade);
    this.timerInatividade = setTimeout(() => {
      this.logout();
    }, this.inatividade * 60 * 1000);
  }

  pararTimers() {
    if (this.timerSessao) clearTimeout(this.timerSessao);
    if (this.timerInatividade) clearTimeout(this.timerInatividade);
    if (this.timerAviso) clearTimeout(this.timerAviso);
    document.removeEventListener('mousemove', this.onAtividade);
    document.removeEventListener('keydown', this.onAtividade);
    document.removeEventListener('click', this.onAtividade);
  }

  logout(): void {
    this.pararTimers();
    this.usuarioLogado.set(null);
    this.permissoes = {};
    this.avisoSessao.set('');
    this.tabService.fecharTodas();
    localStorage.clear();
    sessionStorage.clear();
    this.router.navigate(['/authentication/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAutenticado(): boolean {
    const usuario = this.usuarioLogado();
    if (!usuario) return false;
    return new Date() < new Date(usuario.expiracao);
  }

  private carregarUsuarioStorage(): UsuarioLogado | null {
    const json = localStorage.getItem(this.USER_KEY);
    return json ? JSON.parse(json) : null;
  }
}
