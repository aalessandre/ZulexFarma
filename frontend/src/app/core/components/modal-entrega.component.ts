import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalEntregaService } from '../services/modal-entrega.service';

@Component({
  selector: 'app-modal-entrega',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (svc.visivel()) {
      <div class="me-overlay" (click)="svc.cancelar()">
        <div class="me-box" (click)="$event.stopPropagation()">

          <div class="me-titulo">Configurar Entrega</div>

          @if (svc.carregandoEnderecos()) {
            <div class="me-loading">Carregando endereços...</div>
          } @else if (svc.enderecos().length === 0) {
            <div class="me-erro-bloco">{{ svc.erro() || 'Cliente sem endereços.' }}</div>
            <div class="me-botoes">
              <button class="me-btn me-btn-cancelar" (click)="svc.cancelar()">Fechar</button>
            </div>
          } @else {
            <div class="me-campo">
              <label>ENDEREÇO DE ENTREGA</label>
              <select [value]="svc.enderecoSelecionadoId() ?? ''"
                      (change)="svc.onEnderecoChange(+$any($event.target).value)">
                @for (end of svc.enderecos(); track end.id) {
                  <option [value]="end.id">
                    {{ end.principal ? '★ ' : '' }}{{ end.tipo }} — {{ end.rua }}, {{ end.numero }}{{ end.complemento ? ' - ' + end.complemento : '' }}, {{ end.bairro }}, {{ end.cidade }}/{{ end.uf }}
                  </option>
                }
              </select>
            </div>

            @if (svc.calculando()) {
              <div class="me-loading">Calculando distância...</div>
            } @else if (svc.preview(); as prev) {
              <div class="me-preview">
                <div class="me-preview-linha">
                  <span class="me-label">Distância:</span>
                  <span class="me-valor">{{ prev.distanciaKm | number:'1.2-2' }} km</span>
                </div>
                <div class="me-preview-linha me-preview-valor">
                  <span class="me-label">Valor da entrega:</span>
                  <span class="me-valor">R$ {{ prev.valorEntrega | number:'1.2-2' }}</span>
                </div>
              </div>
            }

            @if (svc.erro()) {
              <div class="me-erro-bloco">{{ svc.erro() }}</div>
            }

            <div class="me-campo">
              <label>OBSERVAÇÃO</label>
              <textarea rows="2" [value]="svc.observacao()"
                        (input)="svc.observacao.set($any($event.target).value)"
                        placeholder="Ponto de referência, complemento, horário preferido..."></textarea>
            </div>

            <div class="me-botoes">
              <button class="me-btn me-btn-cancelar" (click)="svc.cancelar()">Cancelar</button>
              <button class="me-btn me-btn-confirmar"
                      [disabled]="!svc.preview() || svc.calculando()"
                      (click)="svc.confirmar()">Confirmar Entrega</button>
            </div>
          }

        </div>
      </div>
    }
  `,
  styles: [`
    .me-overlay { position: fixed; inset: 0; z-index: 9998; background: rgba(0,0,0,.45);
                  display: flex; align-items: center; justify-content: center; }
    .me-box { background: #fff; border-radius: 14px; padding: 24px 28px;
              width: 480px; max-width: 92vw; box-shadow: 0 8px 32px rgba(0,0,0,.18); }
    .me-titulo { font-size: 18px; font-weight: 600; color: #2c3e50; margin-bottom: 16px; text-align: center; }
    .me-loading { padding: 16px; text-align: center; color: #888; font-size: 13px; }
    .me-campo { margin-bottom: 14px; }
    .me-campo label { display: block; font-size: 10px; color: #666; font-weight: 600;
                      letter-spacing: .5px; margin-bottom: 4px; }
    .me-campo select, .me-campo textarea {
      width: 100%; padding: 8px 10px; font-size: 13px;
      border: 1px solid #d0d7de; border-radius: 6px; background: #fff;
      font-family: inherit;
    }
    .me-campo textarea { resize: vertical; min-height: 50px; }
    .me-preview { background: #f0f7ff; border: 1px solid #c9dcf4; border-radius: 8px;
                  padding: 12px 16px; margin-bottom: 14px; }
    .me-preview-linha { display: flex; justify-content: space-between; padding: 3px 0; font-size: 13px; }
    .me-preview-valor { border-top: 1px solid #c9dcf4; margin-top: 4px; padding-top: 8px;
                        font-weight: 600; font-size: 16px; color: #1e3a5f; }
    .me-label { color: #555; }
    .me-valor { color: #1e3a5f; font-weight: 600; }
    .me-erro-bloco { background: #fdecea; border: 1px solid #f5c6c1; border-radius: 6px;
                     padding: 10px 14px; font-size: 12px; color: #a94442; margin-bottom: 14px; }
    .me-botoes { display: flex; gap: 8px; justify-content: flex-end; margin-top: 8px; }
    .me-btn { padding: 8px 18px; border-radius: 6px; font-size: 13px; font-weight: 600;
              cursor: pointer; border: none; }
    .me-btn:disabled { opacity: .5; cursor: not-allowed; }
    .me-btn-cancelar { background: #e4e7eb; color: #333; }
    .me-btn-confirmar { background: #27ae60; color: #fff; }
    .me-btn-confirmar:hover:not(:disabled) { background: #229954; }
  `]
})
export class ModalEntregaComponent {
  constructor(public svc: ModalEntregaService) {}
}
