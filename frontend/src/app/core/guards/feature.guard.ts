import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Bloqueia rotas que exigem uma feature de ramo (ex.: SNGPC só em filial Farmácia).
 * A rota declara `data: { feature: 'sngpc' }`. Sem a feature → volta pro dashboard.
 */
export const featureGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const feature = route.data?.['feature'] as string | undefined;
  if (!feature || auth.temFeature(feature)) return true;

  router.navigate(['/erp']);
  return false;
};
