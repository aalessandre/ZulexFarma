import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

interface topcards {
  id: number;
  icon: string;
  color: string;
  title: string;
  subtitle: string;
}

@Component({
  selector: 'app-revenue-cards',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './revenue-cards.component.html',
})
export class AppRevenueCardsComponent {
  topcards: topcards[] = [
    {
      id: 1,
      color: 'primary',
      icon: 'credit-card',
      title: '$3249',
      subtitle: 'Total Revenue',
    },
    {
      id: 2,
      color: 'secondary',
      icon: 'users',
      title: '$2376',
      subtitle: 'Online Revenue',
    },
    {
      id: 3,
      color: 'error',
      icon: 'calendar',
      title: '$1795',
      subtitle: 'Offline Products',
    },
    {
      id: 4,
      color: 'warning',
      icon: 'settings',
      title: '$687',
      subtitle: 'Ad. Expense',
    },
  ];

  constructor() {}
}
