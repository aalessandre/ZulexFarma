import { Component, HostListener, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalService } from '../services/modal.service';

@Component({
  selector: 'app-modal-global',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (modal.visivel()) {
      <div class="modal-overlay" (click)="modal.fechar()" (keydown)="onKeydown($event)" tabindex="0" #modalOverlay>
        <div class="modal-box" [class]="'modal-' + modal.config().tipo" (click)="$event.stopPropagation()">

          <!-- Ícone -->
          <div class="modal-icon" [class]="'icon-' + modal.config().tipo">
            @switch (modal.config().tipo) {
              @case ('sucesso') {
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="10"/><polyline points="9 12 12 15 16 10"/>
                </svg>
              }
              @case ('erro') {
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>
                </svg>
              }
              @case ('confirmacao') {
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
              }
              @case ('permissao') {
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              }
              @default {
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
              }
            }
          </div>

          <!-- Título e Mensagem -->
          <div class="modal-titulo">{{ modal.config().titulo }}</div>
          <div class="modal-mensagem">{{ modal.config().mensagem }}</div>

          <!-- Campos de liberação por senha -->
          @if (modal.config().tipo === 'permissao') {
            <div class="modal-liberacao">
              <div class="modal-lib-campo">
                <label>LOGIN DO SUPERVISOR</label>
                <input type="text" [value]="modal.loginSupervisor()"
                       (input)="modal.loginSupervisor.set($any($event.target).value)"
                       placeholder="Login" autocomplete="off" />
              </div>
              <div class="modal-lib-campo">
                <label>SENHA</label>
                <input type="password" [value]="modal.senhaSupervisor()"
                       (input)="modal.senhaSupervisor.set($any($event.target).value)"
                       placeholder="Senha" autocomplete="off"
                       (keydown.enter)="modal.liberarPorSenha()" />
              </div>
              @if (modal.erroLiberacao()) {
                <div class="modal-lib-erro">{{ modal.erroLiberacao() }}</div>
              }
            </div>
          }

          <!-- Botões -->
          <div class="modal-acoes">
            @switch (modal.config().tipo) {
              @case ('aviso') {
                <button class="modal-btn modal-btn-ok" (click)="modal.fechar(true)">OK</button>
              }
              @case ('sucesso') {
                <button class="modal-btn modal-btn-ok" (click)="modal.fechar(true)">OK</button>
              }
              @case ('erro') {
                <button class="modal-btn modal-btn-ok" (click)="modal.fechar(true)">OK</button>
              }
              @case ('confirmacao') {
                <button class="modal-btn modal-btn-cancelar" [class.modal-btn-foco]="focoBotao() === 0" (click)="modal.fechar(false)">
                  {{ modal.config().textoBotaoCancelar || 'Não, cancelar' }}
                </button>
                <button class="modal-btn modal-btn-confirmar" [class.modal-btn-foco]="focoBotao() === 1" (click)="modal.confirmarAcao()">
                  {{ modal.config().textoBotaoConfirmar || 'Sim, confirmar' }}
                </button>
              }
              @case ('permissao') {
                <button class="modal-btn modal-btn-cancelar" (click)="modal.fechar(false)">Cancelar</button>
                <button class="modal-btn modal-btn-liberar" [disabled]="modal.liberando()" (click)="modal.liberarPorSenha()">
                  {{ modal.liberando() ? 'Verificando...' : 'Liberar' }}
                </button>
              }
            }
          </div>

        </div>
      </div>
    }
  `,
  styles: [`
    .modal-overlay {
      position: fixed; inset: 0;
      background: rgba(0,0,0,0.45);
      display: flex; align-items: center; justify-content: center;
      z-index: 10000;
      backdrop-filter: blur(2px);
      animation: fadeIn 0.15s ease-out;
      outline: none;
    }

    .modal-box {
      background: var(--erp-surface, #fff);
      border-radius: 16px;
      padding: 36px 40px 32px;
      width: 440px;
      max-width: 92vw;
      box-shadow: 0 20px 60px rgba(0,0,0,0.2);
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      animation: scaleIn 0.18s ease-out;
    }

    .modal-icon {
      width: 72px; height: 72px;
      border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      margin-bottom: 18px;
    }

    .icon-aviso      { background: #fff3e0; color: #e65100; }
    .icon-sucesso    { background: #e8f5e9; color: #2e7d32; }
    .icon-erro       { background: #fde8e8; color: #c62828; }
    .icon-confirmacao { background: #e3f2fd; color: #1565c0; }
    .icon-permissao  { background: #fff3e0; color: #e65100; }

    .modal-titulo {
      font-size: 20px; font-weight: 700;
      color: var(--erp-text, #2c3e50);
      margin-bottom: 8px;
    }

    .modal-mensagem {
      font-size: 15px;
      color: var(--erp-text-muted, #6a7888);
      line-height: 1.6;
      margin-bottom: 24px;
      white-space: pre-line;
    }

    /* Liberação por senha */
    .modal-liberacao {
      width: 100%;
      display: flex;
      flex-direction: column;
      gap: 12px;
      margin-bottom: 20px;
      text-align: left;
    }

    .modal-lib-campo {
      display: flex;
      flex-direction: column;
      gap: 4px;

      label {
        font-size: 12px; font-weight: 700;
        color: var(--erp-text-muted, #6a7888);
        letter-spacing: 0.7px;
      }

      input {
        height: 40px; padding: 0 12px;
        border: 1px solid var(--erp-border-input, #d0d7e2);
        border-radius: 8px;
        font-size: 15px;
        color: var(--erp-text, #2c3e50);
        outline: none;
        background: var(--erp-input-bg, #fafbfc);

        &:focus { border-color: var(--erp-blue, #2c5fad); }
      }
    }

    .modal-lib-erro {
      font-size: 13px;
      color: #c62828;
      background: #fde8e8;
      padding: 8px 12px;
      border-radius: 6px;
      border: 1px solid #ef9a9a;
    }

    /* Botões */
    .modal-acoes {
      display: flex;
      gap: 10px;
      width: 100%;
    }

    .modal-btn {
      flex: 1;
      height: 44px;
      border: none;
      border-radius: 10px;
      font-size: 15px;
      font-weight: 600;
      cursor: pointer;
      transition: filter 0.15s, transform 0.08s;

      &:hover:not(:disabled) { filter: brightness(1.06); }
      &:active:not(:disabled) { transform: scale(0.98); }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }

    .modal-btn-ok        { background: var(--erp-blue, #2c5fad); color: #fff; }
    .modal-btn-cancelar  { background: #f5f6f8; color: var(--erp-text, #2c3e50); border: 1px solid #e0e4eb; }
    .modal-btn-confirmar { background: #e74c3c; color: #fff; }
    .modal-btn-liberar   { background: #e65100; color: #fff; }
    .modal-btn-foco      { outline: 3px solid var(--erp-blue, #2c5fad); outline-offset: 2px; }

    @keyframes fadeIn {
      from { opacity: 0; }
      to   { opacity: 1; }
    }

    @keyframes scaleIn {
      from { opacity: 0; transform: scale(0.92) translateY(-8px); }
      to   { opacity: 1; transform: scale(1) translateY(0); }
    }
  `]
})
export class ModalGlobalComponent implements AfterViewChecked {
  focoBotao = signal(0);
  @ViewChild('modalOverlay') modalOverlay?: ElementRef<HTMLDivElement>;

  constructor(public modal: ModalService) {}

  ngAfterViewChecked() {
    if (this.modal.visivel() && this.modalOverlay) {
      this.modalOverlay.nativeElement.focus();
    }
  }

  onKeydown(e: KeyboardEvent) {
    if (!this.modal.visivel()) return;
    const tipo = this.modal.config().tipo;

    if (e.key === 'Escape') {
      e.preventDefault();
      e.stopPropagation();
      this.modal.fechar(false);
      this.focoBotao.set(0);
      return;
    }

    if (e.key === 'Enter') {
      e.preventDefault();
      e.stopPropagation();
      if (tipo === 'confirmacao') {
        if (this.focoBotao() === 1) this.modal.confirmarAcao();
        else this.modal.fechar(false);
      } else if (tipo === 'permissao') {
        this.modal.liberarPorSenha();
      } else {
        this.modal.fechar(true);
      }
      this.focoBotao.set(0);
      return;
    }

    if ((e.key === 'ArrowLeft' || e.key === 'ArrowRight') && tipo === 'confirmacao') {
      e.preventDefault();
      e.stopPropagation();
      this.focoBotao.update(v => v === 0 ? 1 : 0);
    }
  }
}
