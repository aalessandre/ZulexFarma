import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-ad-expense',
  standalone: true,
  imports:[TablerIconsModule, MaterialModule],
  templateUrl: './ad-expense.component.html',
})
export class AppAdExpenseComponent {
  constructor() {}
}
