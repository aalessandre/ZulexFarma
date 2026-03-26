import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-monthly-winner',
  standalone: true,
  imports:[MaterialModule, TablerIconsModule],
  templateUrl: './monthly-winner.component.html',
})
export class AppMonthlyWinnerComponent {
  constructor() {}
}
