import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalSenhaService } from '../services/modal-senha.service';

@Component({
  selector: 'app-modal-senha',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (svc.visivel()) {
      <div class="ms-overlay" (click)="svc.cancelar()">
        <div class="ms-box" (click)="$event.stopPropagation()">

          <div class="ms-icon">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
            </svg>
          </div>

          <div class="ms-titulo">{{ svc.titulo() }}</div>
          <div class="ms-descricao">{{ svc.descricao() }}</div>

          <div class="ms-campo">
            <label>SENHA</label>
            <input [type]="mostrarSenha ? 'text' : 'password'"
                   [value]="svc.senha()"
                   (input)="svc.senha.set($any($event.target).value)"
                   (keydown.enter)="svc.confirmar()"
                   placeholder="Digite a senha" autocomplete="off" autofocus />
            <button class="ms-toggle" type="button" (click)="mostrarSenha = !mostrarSenha" tabindex="-1">
              @if (mostrarSenha) {
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>
              } @else {
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
              }
            </button>
          </div>

          @if (svc.erro()) {
            <div class="ms-erro">{{ svc.erro() }}</div>
          }

          <div class="ms-botoes">
            <button class="ms-btn ms-btn-cancelar" (click)="svc.cancelar()">Cancelar</button>
            <button class="ms-btn ms-btn-confirmar" (click)="svc.confirmar()">Confirmar</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .ms-overlay {
      position: fixed; inset: 0; z-index: 9999;
      background: rgba(0,0,0,.45); display: flex; align-items: center; justify-content: center;
    }
    .ms-box {
      background: #fff; border-radius: 14px; padding: 28px 32px;
      width: 380px; max-width: 90vw; text-align: center;
      box-shadow: 0 8px 32px rgba(0,0,0,.18);
    }
    .ms-icon { color: #f39c12; margin-bottom: 12px; }
    .ms-titulo { font-size: 18px; font-weight: 700; color: #1a1a2e; margin-bottom: 6px; }
    .ms-descricao { font-size: 13px; color: #7f8c8d; margin-bottom: 18px; line-height: 1.5; }
    .ms-campo {
      position: relative; text-align: left; margin-bottom: 10px;
      label { display: block; font-size: 11px; font-weight: 700; color: #7f8c8d; margin-bottom: 4px; letter-spacing: .5px; }
      input {
        width: 100%; padding: 10px 40px 10px 12px; border: 1px solid #dce1e6; border-radius: 8px;
        font-size: 14px; outline: none; box-sizing: border-box;
        &:focus { border-color: #2196F3; box-shadow: 0 0 0 3px rgba(33,150,243,.12); }
      }
    }
    .ms-toggle {
      position: absolute; right: 8px; top: 28px;
      background: none; border: none; cursor: pointer; color: #95a5a6; padding: 4px;
      &:hover { color: #2c3e50; }
    }
    .ms-erro {
      font-size: 12px; color: #e74c3c; margin-bottom: 10px; text-align: left;
    }
    .ms-botoes {
      display: flex; gap: 10px; margin-top: 18px;
    }
    .ms-btn {
      flex: 1; padding: 10px; border-radius: 8px; font-size: 14px; font-weight: 600;
      cursor: pointer; border: none; transition: all .12s;
    }
    .ms-btn-cancelar {
      background: #f0f0f0; color: #7f8c8d;
      &:hover { background: #e0e0e0; }
    }
    .ms-btn-confirmar {
      background: #2196F3; color: #fff;
      &:hover { background: #1976D2; }
    }
  `]
})
export class ModalSenhaComponent {
  mostrarSenha = false;
  constructor(public svc: ModalSenhaService) {}
}
