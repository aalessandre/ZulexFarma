import { Routes } from '@angular/router';

import { AppBoxedForgotPasswordComponent } from './boxed-forgot-password/boxed-forgot-password.component';
import { AppBoxedLoginComponent } from './boxed-login/boxed-login.component';
import { AppBoxedRegisterComponent } from './boxed-register/boxed-register.component';
import { AppBoxedTwoStepsComponent } from './boxed-two-steps/boxed-two-steps.component';
import { AppErrorComponent } from './error/error.component';
import { AppMaintenanceComponent } from './maintenance/maintenance.component';
import { AppSideForgotPasswordComponent } from './side-forgot-password/side-forgot-password.component';
import { AppSideLoginComponent } from './side-login/side-login.component';
import { AppSideRegisterComponent } from './side-register/side-register.component';
import { AppSideTwoStepsComponent } from './side-two-steps/side-two-steps.component';

export const AuthenticationRoutes: Routes = [
  {
    path: '',
    children: [
      {
        title: 'Boxed Forgot Authentication',
        path: 'boxed-forgot-pwd',
        component: AppBoxedForgotPasswordComponent,
      },
      {
        title: 'Boxed Login Authentication',
        path: 'boxed-login',
        component: AppBoxedLoginComponent,
      },
      {
        title: 'Boxed Register Authentication',
        path: 'boxed-register',
        component: AppBoxedRegisterComponent,
      },
      {
        title: 'Boxed Two Steps Authentication',
        path: 'boxed-two-steps',
        component: AppBoxedTwoStepsComponent,
      },
      {
        title: 'Error-404',
        path: 'error',
        component: AppErrorComponent,
      },
      {
        title: 'Maintenance',
        path: 'maintenance',
        component: AppMaintenanceComponent,
      },
      {
        title: 'Side Forgot Authentication',
        path: 'side-forgot-pwd',
        component: AppSideForgotPasswordComponent,
      },
      {
        title: 'Side Login Authentication',
        path: 'login',
        component: AppSideLoginComponent,
      },
      {
        title: 'Side Register Authentication',
        path: 'side-register',
        component: AppSideRegisterComponent,
      },
      {
        title: 'Side Two Steps Authentication',
        path: 'side-two-steps',
        component: AppSideTwoStepsComponent,
      },
    ],
  },
];
