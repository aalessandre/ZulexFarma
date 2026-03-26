import { Component, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AuthService } from '../../core/services/auth.service';
import { TabService } from '../../core/services/tab.service';
import { ErpSettingsService } from '../../core/services/erp-settings.service';
import { environment } from '../../../environments/environment';

const ICONS: Record<string, string> = {
  cart:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 0 1-8 0"/></svg>`,
  handshake: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="m20.42 4.58-7.65 7.65M3.58 4.58l7.65 7.65M12 12l-1.77 1.77a3 3 0 0 1-4.24 0L3.58 11.3"/><path d="m20.42 4.58-2.83-2.83a2 2 0 0 0-2.83 0L12 4.58"/></svg>`,
  box:       `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>`,
  wallet:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="5" width="20" height="14" rx="2"/><line x1="2" y1="10" x2="22" y2="10"/></svg>`,
  user:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>`,
  cash:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/></svg>`,
  cart2:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/></svg>`,
  dollar:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>`,
  users:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>`,
  pill:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="m10.5 20.5 10-10a4.95 4.95 0 1 0-7-7l-10 10a4.95 4.95 0 1 0 7 7Z"/><line x1="8.5" y1="8.5" x2="15.5" y2="15.5"/></svg>`,
  truck:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="1" y="3" width="15" height="13"/><polygon points="16 8 20 8 23 11 23 16 16 16 16 8"/><circle cx="5.5" cy="18.5" r="2.5"/><circle cx="18.5" cy="18.5" r="2.5"/></svg>`,
  chart:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>`,
  log:       `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>`,
  wrench:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>`,
  gear:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>`,
  building:  `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 22V12h6v10"/><path d="M9 7h.01M12 7h.01M15 7h.01M9 12h.01M15 12h.01"/></svg>`,
  lock:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>`,
};

export interface TileItem {
  label: string;
  sigla: string;
  iconKey: string;
  rota: string;
  tamanho?: 'normal' | 'largo';
}

export interface BlocoTiles {
  nome: string;
  cor: string;
  tiles: TileItem[];
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  usuario = computed(() => this.authService.usuarioLogado());

  blocos: BlocoTiles[] = [
    {
      nome: 'Movimento',
      cor: '#00acc1',
      tiles: [
        { label: 'Vendas',           sigla: 'VE', iconKey: 'cart',      rota: '/erp/vendas' },
        { label: 'Empréstimo',       sigla: 'EP', iconKey: 'handshake', rota: '/erp/emprestimo' },
        { label: 'Mov. de Estoque',  sigla: 'ME', iconKey: 'box',       rota: '/erp/movimentacao-estoque' },
        { label: 'Carteira Digital', sigla: 'CD', iconKey: 'wallet',    rota: '/erp/carteira' },
        { label: 'Conta do Cliente', sigla: 'CC', iconKey: 'user',      rota: '/erp/conta-cliente' },
        { label: 'Caixa',            sigla: 'CX', iconKey: 'cash',      rota: '/erp/caixa' },
        { label: 'Compras',          sigla: 'CP', iconKey: 'cart2',     rota: '/erp/compras' },
        { label: 'Financeiro',       sigla: 'FN', iconKey: 'dollar',    rota: '/erp/financeiro' },
      ]
    },
    {
      nome: 'Cadastros',
      cor: '#e65100',
      tiles: [
        { label: 'Clientes',         sigla: 'CL', iconKey: 'users',     rota: '/erp/clientes' },
        { label: 'Colaboradores',    sigla: 'CO', iconKey: 'user',      rota: '/erp/colaboradores' },
        { label: 'Produtos',         sigla: 'PR', iconKey: 'pill',      rota: '/erp/produtos' },
        { label: 'Fornecedores',     sigla: 'FO', iconKey: 'truck',     rota: '/erp/fornecedores' },
      ]
    },
    {
      nome: 'Relatórios',
      cor: '#6a1b9a',
      tiles: [
        { label: 'Análise de Vendas',   sigla: 'AV', iconKey: 'chart', rota: '/erp/rel/vendas' },
        { label: 'Análise de Produtos', sigla: 'AP', iconKey: 'chart', rota: '/erp/rel/produtos' },
        { label: 'Log de Auditoria',    sigla: 'LA', iconKey: 'log',   rota: '/erp/log-geral' },
      ]
    },
    {
      nome: 'Manutenção',
      cor: '#f9a825',
      tiles: [
        { label: 'Manutenção',        sigla: 'MT', iconKey: 'wrench',   rota: '/erp/manutencao' },
        { label: 'Sincronização',     sigla: 'SI', iconKey: 'wrench',   rota: '/erp/sync' },
        { label: 'Grupo de Usuários', sigla: 'GU', iconKey: 'lock',     rota: '/erp/grupos' },
        { label: 'Usuários',          sigla: 'US', iconKey: 'users',    rota: '/erp/usuarios' },
        { label: 'Filiais',           sigla: 'FL', iconKey: 'building', rota: '/erp/filiais' },
        { label: 'Sistema',           sigla: 'ST', iconKey: 'gear',     rota: '/erp/sistema' },
        { label: 'Configurações',     sigla: 'CF', iconKey: 'gear',     rota: '/erp/configuracoes' },
      ]
    }
  ];

  menuUsuarioAberto = signal(false);
  modalSenhaAberta = signal(false);
  senhaAtual = signal('');
  novaSenha = signal('');
  confirmarSenha = signal('');
  erroSenha = signal('');
  sucessoSenha = signal('');
  salvandoSenha = signal(false);

  constructor(
    private authService: AuthService,
    public tabService: TabService,
    public settings: ErpSettingsService,
    private router: Router,
    private sanitizer: DomSanitizer,
    private http: HttpClient
  ) {}

  getIcon(key: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(ICONS[key] ?? ICONS['gear']);
  }

  navegar(tile: TileItem) {
    this.tabService.abrirTab({
      id: tile.rota,
      titulo: tile.label,
      rota: tile.rota,
      iconKey: tile.iconKey,
    });
  }

  logout() {
    this.authService.logout();
  }

  toggleMenuUsuario() {
    this.menuUsuarioAberto.update(v => !v);
  }

  abrirAlterarSenha() {
    this.menuUsuarioAberto.set(false);
    this.senhaAtual.set('');
    this.novaSenha.set('');
    this.confirmarSenha.set('');
    this.erroSenha.set('');
    this.sucessoSenha.set('');
    this.modalSenhaAberta.set(true);
  }

  fecharModalSenha() {
    this.modalSenhaAberta.set(false);
  }

  alterarSenha() {
    this.erroSenha.set('');
    this.sucessoSenha.set('');

    if (!this.senhaAtual()) {
      this.erroSenha.set('Informe a senha atual.');
      return;
    }
    if (!this.novaSenha() || this.novaSenha().length < 4 || this.novaSenha().length > 12) {
      this.erroSenha.set('A nova senha deve ter entre 4 e 12 caracteres.');
      return;
    }
    if (this.novaSenha() !== this.confirmarSenha()) {
      this.erroSenha.set('A nova senha e a confirmação não coincidem.');
      return;
    }

    this.salvandoSenha.set(true);
    this.http.post<any>(`${environment.apiUrl}/auth/alterar-senha`, {
      senhaAtual: this.senhaAtual(),
      novaSenha: this.novaSenha()
    }).subscribe({
      next: r => {
        this.salvandoSenha.set(false);
        if (r.success) {
          this.sucessoSenha.set('Senha alterada com sucesso!');
          setTimeout(() => this.fecharModalSenha(), 2000);
        } else {
          this.erroSenha.set(r.message || 'Erro ao alterar senha.');
        }
      },
      error: () => {
        this.salvandoSenha.set(false);
        this.erroSenha.set('Erro ao comunicar com o servidor.');
      }
    });
  }
}
