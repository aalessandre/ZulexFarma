import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

// table 1
export interface projectsData {
  id: number;
  imagePath: string;
  uname: string;
  post: string;
  productName: string;
  budget: number;
  priority: string;
}

interface month {
  value: string;
  viewValue: string;
}

const PROJECT_DATA: projectsData[] = [
  {
    id: 1,
    imagePath: 'assets/images/profile/user-2.jpg',
    uname: 'Sunil Joshi',
    productName: 'Digital Agency',
    post: 'web designer',
    budget: 3.9,
    priority: 'low',
  },
  {
    id: 2,
    imagePath: 'assets/images/profile/user-4.jpg',
    uname: 'Andrew Liock',
    productName: 'Real Homes',
    post: 'Project Manager',
    budget: 23.9,
    priority: 'medium',
  },
  {
    id: 3,
    imagePath: 'assets/images/profile/user-5.jpg',
    uname: 'Biaca George',
    productName: 'MedicalPro Theme',
    post: 'Developer',
    budget: 12.9,
    priority: 'high',
  },
  {
    id: 4,
    imagePath: 'assets/images/profile/user-6.jpg',
    uname: 'Nirav Joshi',
    productName: 'Elite Admin',
    post: 'Frontend Eng',
    budget: 2.6,
    priority: 'Very High',
  },
];

@Component({
  selector: 'app-project-data',
  standalone: true,
  imports:[CommonModule, MaterialModule, TablerIconsModule],
  templateUrl: './project-data.component.html',
})
export class AppProjectDataComponent {

  months: month[] = [
    { value: 'jan', viewValue: 'January' },
    { value: 'feb', viewValue: 'February' },
    { value: 'mar', viewValue: 'March' },
    { value: 'apr', viewValue: 'April' },
  ];


   // table 1
  displayedColumns2: string[] = ['client', 'name', 'priority', 'budget'];
  dataSource2 = PROJECT_DATA;
  
  constructor() {}
}
