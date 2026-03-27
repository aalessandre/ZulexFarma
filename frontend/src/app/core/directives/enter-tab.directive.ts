import { Directive, HostListener } from '@angular/core';

@Directive({
  selector: 'input, select, textarea',
  standalone: true
})
export class EnterTabDirective {
  @HostListener('keydown.enter', ['$event'])
  onEnter(event: Event) {
    const kbEvent = event as KeyboardEvent;
    kbEvent.preventDefault();
    const target = kbEvent.target as HTMLElement;
    const form = target.closest('form, .form-wrapper, .form-grid, .tela-main');
    if (!form) return;

    const focusable = Array.from(form.querySelectorAll<HTMLElement>(
      'input:not([disabled]):not([type="hidden"]):not([type="checkbox"]), select:not([disabled]), textarea:not([disabled])'
    )).filter(el => el.offsetParent !== null);

    const index = focusable.indexOf(target);
    if (index >= 0 && index < focusable.length - 1) {
      focusable[index + 1].focus();
    }
  }
}
