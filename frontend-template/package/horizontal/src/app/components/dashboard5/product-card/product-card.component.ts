import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

interface productcard {
  id: number;
  color: string;
  title: string;
  value: number;
  subtitle: string;
}

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './product-card.component.html',
})
export class AppProductCardComponent {
  productcard: productcard[] = [
    {
      id: 1,
      color: 'mat-primary',
      title: '86%',
      subtitle: 'Total Product',
      value: 85
    },
    {
      id: 2,
      color: 'mat-secondary',
      title: '40%',
      subtitle: 'Pending Product',
      value: 40
    },
    {
      id: 3,
      color: 'mat-error',
      title: '56%',
      subtitle: 'Product A',
      value: 56
    },
    {
      id: 4,
      color: 'mat-warning',
      title: '26%',
      subtitle: 'Product B',
      value: 26
    },
  ];

  constructor() {}
}
