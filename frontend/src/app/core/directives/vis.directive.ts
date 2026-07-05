import { Directive, Input, TemplateRef, ViewContainerRef, effect, signal } from '@angular/core';
import { VisibilidadeService } from '../services/visibilidade.service';

/**
 * Gate de visibilidade por ramo: `<div class="prod-field" *vis="'produto.campo.x'">`.
 * Mostra o elemento só se `VisibilidadeService.mostra(id)` for true (override do
 * configurador ?? default por feature). Reativo aos overrides/usuário (signals).
 * Ver docs/specs/configurador-ramo-visibilidade.md.
 */
@Directive({ selector: '[vis]', standalone: true })
export class VisDirective {
  private id = signal<string>('');
  private mostrando = false;

  @Input({ required: true }) set vis(v: string) { this.id.set(v); }

  constructor(tpl: TemplateRef<any>, vcr: ViewContainerRef, visSvc: VisibilidadeService) {
    effect(() => {
      const show = visSvc.mostra(this.id());   // lê signals (overrides/usuário) → reativo
      if (show === this.mostrando) return;
      this.mostrando = show;
      vcr.clear();
      if (show) vcr.createEmbeddedView(tpl);
    });
  }
}
