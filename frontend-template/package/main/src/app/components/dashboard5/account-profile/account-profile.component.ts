import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-account-profile',
  standalone: true,
  imports:[TablerIconsModule, MaterialModule],
  templateUrl: './account-profile.component.html',
})
export class AppAccountProfileComponent {
  constructor() {}
}
