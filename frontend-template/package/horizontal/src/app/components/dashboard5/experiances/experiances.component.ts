import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

// table 1
export interface projectsData {
  id: number;
  color: string;
  image: string;
  uname: string;
  post: string;
  value: number;
}

const PROJECT_DATA: projectsData[] = [
  {
    id: 1,
    color: 'primary',
    image: 'assets/images/browser/photoshop.jpg',
    uname: 'Photoshop',
    post: 'This is a sample text',
    value: 65
  },
  {
    id: 2,
    color: 'secondary',
    image: 'assets/images/browser/sketch.jpg',
    uname: 'Sketch',
    post: 'This is a sample text',
    value: 45
  },
];

@Component({
  selector: 'app-experiances',
  standalone: true,
  imports:[TablerIconsModule, MaterialModule],
  templateUrl: './experiances.component.html',
})
export class AppExperiancesComponent {

  // table 1
  displayedColumns2: string[] = ['product', 'description', 'quantity'];
  dataSource2 = PROJECT_DATA;

  constructor() {}
}
