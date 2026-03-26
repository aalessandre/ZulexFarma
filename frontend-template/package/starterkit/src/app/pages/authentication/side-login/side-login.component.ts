import { Component } from '@angular/core';
import { CoreService } from 'src/app/services/core.service';
import { FormGroup, FormControl, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MaterialModule } from '../../../material.module';
import { AppAuthBrandingComponent } from '../../../layouts/full/vertical/sidebar/auth-branding.component';

@Component({
  selector: 'app-side-login',
  imports: [RouterModule, MaterialModule, FormsModule, ReactiveFormsModule, AppAuthBrandingComponent],
  templateUrl: './side-login.component.html'
})
export class AppSideLoginComponent {
  options = this.settings.getOptions();

  constructor(private settings: CoreService, private router: Router) { }

  form = new FormGroup({
    uname: new FormControl('admin', [Validators.required, Validators.minLength(5)]),
    password: new FormControl('12345', [Validators.required]),
  });

  get f() {
    return this.form.controls;
  }

  submit() {
    this.router.navigate(['/starter']);
  }
} 
