import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Interceptor que adiciona headers anti-cache em TODAS as requisições GET.
 * ERP não pode servir dados em cache — dados fantasma causam erros operacionais.
 */
export const noCacheInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method === 'GET' && req.url.includes('/api/')) {
    const noCacheReq = req.clone({
      setHeaders: {
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache'
      },
      setParams: {
        '_t': Date.now().toString()
      }
    });
    return next(noCacheReq);
  }
  return next(req);
};
