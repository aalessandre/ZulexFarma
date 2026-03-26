import { Component, signal } from '@angular/core';
import { CoreService } from 'src/app/services/core.service';
import { FormGroup, FormControl, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-side-login',
  standalone: true,
  imports: [RouterModule, FormsModule, ReactiveFormsModule, CommonModule],
  templateUrl: './side-login.component.html',
  styleUrl: './side-login.component.scss'
})
export class AppSideLoginComponent {
  options = this.settings.getOptions();

  carregando = signal(false);
  erroLogin = signal('');
  nomeFilial = 'ZULEX FARMA';
  filialSelecionada = '001';

  form = new FormGroup({
    uname:    new FormControl('', [Validators.required]),
    password: new FormControl('', [Validators.required]),
    turno:    new FormControl('tarde'),
  });

  get f() { return this.form.controls; }

  constructor(
    private settings: CoreService,
    private router: Router,
    private authService: AuthService
  ) {}

  submit() {
    if (this.form.invalid) return;

    this.carregando.set(true);
    this.erroLogin.set('');

    this.authService.login({
      login: this.f['uname'].value!,
      senha: this.f['password'].value!
    }).subscribe({
      next: () => {
        this.carregando.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.carregando.set(false);
        this.erroLogin.set(
          err.status === 401
            ? 'Login ou senha inválidos.'
            : 'Erro ao conectar. Tente novamente.'
        );
      }
    });
  }
}
