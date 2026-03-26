import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-discount',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './discount.component.html',
})
export class AppDiscountComponent {
  constructor() {}
}
