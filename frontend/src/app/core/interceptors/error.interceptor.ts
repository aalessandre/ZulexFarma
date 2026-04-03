import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ModalService } from '../services/modal.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const modal = inject(ModalService);

  return next(req).pipe(
    catchError(error => {
      // Backend offline ou erro de rede (status 0 ou sem resposta)
      if (error.status === 0) {
        modal.erro(
          'Erro de Conexão',
          'Não foi possível se comunicar com o servidor. Verifique se o sistema está em execução e tente novamente. Se o problema persistir, entre em contato com o suporte.'
        );
      }

      return throwError(() => error);
    })
  );
};
