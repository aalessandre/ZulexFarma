import { Directive, ElementRef, HostListener, Input, Output, EventEmitter, OnInit } from '@angular/core';

/**
 * Diretiva para campos de valor monetário (R$).
 * - Aceita apenas números, vírgula e ponto
 * - Formata com 2 casas decimais no blur
 * - Emite valor numérico (float) no event valueChange
 *
 * Uso: <input currencyInput [currencyValue]="campo" (currencyChange)="update($event)" />
 */
@Directive({
  selector: '[currencyInput]',
  standalone: true
})
export class CurrencyInputDirective implements OnInit {
  @Input() currencyValue: number | null = 0;
  @Output() currencyChange = new EventEmitter<number>();

  private el: HTMLInputElement;

  constructor(private ref: ElementRef) {
    this.el = ref.nativeElement;
  }

  ngOnInit() {
    this.formatDisplay();
  }

  ngOnChanges() {
    // Só atualizar display se o input não estiver focado (evita conflito com digitação)
    if (document.activeElement !== this.el) {
      this.formatDisplay();
    }
  }

  @HostListener('keypress', ['$event'])
  onKeyPress(e: KeyboardEvent) {
    const allowed = /[0-9.,]/;
    if (!allowed.test(e.key)) {
      e.preventDefault();
      return;
    }
    // Só permite um separador decimal
    const val = this.el.value;
    if ((e.key === ',' || e.key === '.') && (val.includes(',') || val.includes('.'))) {
      e.preventDefault();
    }
  }

  @HostListener('input')
  onInput() {
    const raw = this.el.value.replace(',', '.');
    const num = parseFloat(raw);
    if (!isNaN(num)) {
      this.currencyChange.emit(num);
    } else if (this.el.value === '' || this.el.value === '0') {
      this.currencyChange.emit(0);
    }
  }

  @HostListener('focus')
  onFocus() {
    // Ao focar, mostrar valor limpo para edição
    const v = this.currencyValue ?? 0;
    if (v === 0) {
      this.el.value = '';
    } else {
      this.el.value = v.toString().replace('.', ',');
    }
    // Selecionar tudo para facilitar edição
    setTimeout(() => this.el.select(), 0);
  }

  @HostListener('blur')
  onBlur() {
    this.formatDisplay();
  }

  private formatDisplay() {
    const v = this.currencyValue ?? 0;
    this.el.value = v.toFixed(2).replace('.', ',');
  }
}
