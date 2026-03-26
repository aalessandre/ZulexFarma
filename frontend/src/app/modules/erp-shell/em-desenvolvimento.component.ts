import { Component } from '@angular/core';

@Component({
  standalone: true,
  template: `
    <div style="display:flex;flex-direction:column;align-items:center;justify-content:center;
                height:100%;color:#8a9bb0;font-family:'Segoe UI',sans-serif;gap:12px">
      <svg width="56" height="56" viewBox="0 0 24 24" fill="none" stroke="#c0ccd8" stroke-width="1.5">
        <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/>
      </svg>
      <p style="font-size:15px;font-weight:600;margin:0">Módulo em desenvolvimento</p>
      <p style="font-size:13px;margin:0;opacity:0.7">Esta tela será implementada em breve.</p>
    </div>
  `,
})
export class EmDesenvolvimentoComponent {}
