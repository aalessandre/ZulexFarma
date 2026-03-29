import { CanDeactivateFn } from '@angular/router';

export interface HasDirtyForm {
  isDirty: () => boolean;
}

/**
 * Guard para prevenir navegação quando há alterações não salvas.
 * Pronto para uso futuro quando integrado às rotas.
 */
export const dirtyFormGuard: CanDeactivateFn<HasDirtyForm> = (component) => {
  if (component?.isDirty && component.isDirty()) {
    return confirm('Você tem alterações não salvas. Deseja sair?');
  }
  return true;
};
