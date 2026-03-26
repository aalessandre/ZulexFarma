import { Injectable, signal, effect } from '@angular/core';

export type FonteEscala = 'normal' | 'grande' | 'enorme' | 'extra';
export type Tema = 'claro' | 'escuro';

@Injectable({ providedIn: 'root' })
export class ErpSettingsService {
  tema  = signal<Tema>('claro');
  fonte = signal<FonteEscala>('normal');
  cor   = signal<string>('');

  readonly coresPreset = [
    { nome: 'Azul',    valor: '#2c5fad' },
    { nome: 'Verde',   valor: '#1a7a40' },
    { nome: 'Roxo',    valor: '#6a1b9a' },
    { nome: 'Laranja', valor: '#e65100' },
    { nome: 'Teal',    valor: '#00838f' },
    { nome: 'Vinho',   valor: '#880e4f' },
  ];

  private readonly KEY = 'zulex_settings';

  constructor() {
    try {
      const s = JSON.parse(localStorage.getItem(this.KEY) ?? '{}');
      if (s.tema)  this.tema.set(s.tema);
      if (s.fonte) this.fonte.set(s.fonte);
      if (s.cor)   this.cor.set(s.cor);
    } catch {}

    this.aplicar(this.tema(), this.fonte(), this.cor());

    effect(() => {
      const tema  = this.tema();
      const fonte = this.fonte();
      const cor   = this.cor();
      this.aplicar(tema, fonte, cor);
      localStorage.setItem(this.KEY, JSON.stringify({ tema, fonte, cor }));
    });
  }

  private aplicar(tema: Tema, fonte: FonteEscala, cor: string) {
    const el = document.documentElement;
    el.setAttribute('data-tema',  tema);
    el.setAttribute('data-fonte', fonte);

    if (cor) {
      el.style.setProperty('--erp-accent', cor);
      el.style.setProperty('--erp-blue', cor);
      el.style.setProperty('--erp-accent-light', cor + '26');
      // Derivar sidebar do accent (versão escura)
      el.style.setProperty('--erp-sidebar-bg', this.escurecer(cor, 0.6));
      el.style.setProperty('--erp-primary', this.escurecer(cor, 0.5));
      el.style.setProperty('--erp-navy-mid', this.escurecer(cor, 0.45));
    } else {
      el.style.removeProperty('--erp-accent');
      el.style.removeProperty('--erp-blue');
      el.style.removeProperty('--erp-accent-light');
      el.style.removeProperty('--erp-sidebar-bg');
      el.style.removeProperty('--erp-primary');
      el.style.removeProperty('--erp-navy-mid');
    }
  }

  private escurecer(hex: string, fator: number): string {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    const f = 1 - fator;
    return `#${Math.round(r * f).toString(16).padStart(2, '0')}${Math.round(g * f).toString(16).padStart(2, '0')}${Math.round(b * f).toString(16).padStart(2, '0')}`;
  }
}
