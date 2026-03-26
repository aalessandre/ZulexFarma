import { Component } from '@angular/core';
import { CoreService } from 'src/app/services/core.service';
import {
  FormGroup,
  FormControl,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MaterialModule } from '../../../material.module';
import { AppAuthBrandingComponent } from 'src/app/layouts/full/vertical/sidebar/auth-branding.component';

@Component({
  selector: 'app-boxed-register',
  imports: [
    RouterModule,
    MaterialModule,
    FormsModule,
    AppAuthBrandingComponent,
    ReactiveFormsModule,
  ],
  templateUrl: './boxed-register.component.html',
})
export class AppBoxedRegisterComponent {
  options = this.settings.getOptions();

  constructor(private settings: CoreService, private router: Router) { }

  form = new FormGroup({
    uname: new FormControl('admin', [Validators.required, Validators.minLength(5)]),
    email: new FormControl('admin@gmail.com', [Validators.required]),
    password: new FormControl('admin@123', [Validators.required]),
  });

  get f() {
    return this.form.controls;
  }

  submit() {
    this.router.navigate(['/dashboards/dashboard1']);
  }
}
