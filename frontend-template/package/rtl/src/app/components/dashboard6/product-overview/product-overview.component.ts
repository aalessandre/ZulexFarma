import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

// table 1
export interface projectsData {
  id: number;
  customer: string;
  imagePath: string;
  quantity: string;
  date: string;
  priority: string;
}

interface month {
  value: string;
  viewValue: string;
}

const PROJECT_DATA: projectsData[] = [
  {
    id: 1,
    customer: 'Steave Jobs',
    imagePath: 'assets/images/gallery/chair.png',
    quantity: '20',
    date: '10-7-2026',
    priority: 'paid',
  },
  {
    id: 2,
    customer: 'Varun Dhavan',
    imagePath: 'assets/images/gallery/chair2.png',
    quantity: '25',
    date: '09-7-2026',
    priority: 'pending',
  },
  {
    id: 3,
    customer: 'Ritesh Desh',
    imagePath: 'assets/images/gallery/chair3.png',
    quantity: '12',
    date: '08-7-2026',
    priority: 'paid',
  },
  {
    id: 4,
    customer: 'Hrithik',
    imagePath: 'assets/images/gallery/chair4.png',
    quantity: '18',
    date: '02-7-2026',
    priority: 'failed',
  },
];

@Component({
  selector: 'app-product-overview',
  standalone: true,
  imports: [CommonModule, MaterialModule, TablerIconsModule],
  templateUrl: './product-overview.component.html',
})
export class AppProductOverviewComponent {

  months: month[] = [
    { value: 'mar', viewValue: 'January' },
    { value: 'apr', viewValue: 'February' },
    { value: 'june', viewValue: 'March' },
    { value: 'june', viewValue: 'April' },
  ];


  // table 1
  displayedColumns2: string[] = ['customer', 'photo', 'quantity', 'date', 'status', 'actions'];
  dataSource2 = PROJECT_DATA;

  constructor() { }
}
