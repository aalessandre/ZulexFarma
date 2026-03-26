import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-reviews',
  standalone: true,
  imports:[TablerIconsModule, MaterialModule],
  templateUrl: './reviews.component.html',
})
export class AppReviewsComponent {
  constructor() {}
}
