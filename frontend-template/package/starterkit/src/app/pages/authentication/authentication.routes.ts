import { Routes } from '@angular/router';

import { AppErrorComponent } from './error/error.component';
import { AppSideLoginComponent } from './side-login/side-login.component';
import { AppSideRegisterComponent } from './side-register/side-register.component';

export const AuthenticationRoutes: Routes = [
  {
    path: '',
    children: [
      {
        title: 'Error-404',
        path: 'error',
        component: AppErrorComponent,
      },
      {
        title: 'Login',
        path: 'login',
        component: AppSideLoginComponent,
      },
      {
        title: 'Register',
        path: 'register',
        component: AppSideRegisterComponent,
      },
    ],
  },
];
