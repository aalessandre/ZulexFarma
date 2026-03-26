
import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

// table 1
export interface projectsData {
  id: number;
  color: string;
  icon: string;
  uname: string;
  quantity: string;
  post: string;
  price: string;
}

interface month {
  value: string;
  viewValue: string;
}

const PROJECT_DATA: projectsData[] = [
  {
    id: 1,
    color: 'secondary',
    icon: 'shopping-cart',
    uname: 'Apple iPhone 6 Space Grey, 16 GB',
    post: 'Product id : MI5457',
    quantity: '357',
    price: '$435',
  },
  {
    id: 2,
    color: 'success',
    icon: 'shopping-cart',
    uname: 'Fossil Marshall For Men Black watch',
    post: 'Product id : MI5457',
    quantity: '357',
    price: '$435',
  },
  {
    id: 3,
    color: 'error',
    icon: 'shopping-cart',
    uname: 'Sony Bravia 80cm - 32 HD Ready LED TV',
    post: 'Product id : MI5457',
    quantity: '357',
    price: '$435',
  },
  {
    id: 4,
    color: 'primary',
    icon: 'shopping-cart',
    uname: 'Panasonic P75 Champagne Gold, 8 GB',
    post: 'Product id : MI5457',
    quantity: '357',
    price: '$435',
  },
];

@Component({
  selector: 'app-products-availability',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './products-availability.component.html',
})
export class AppProductsAvailabilityComponent {
  months: month[] = [
    { value: 'ele', viewValue: 'Electronics' },
    { value: 'kit', viewValue: 'Kitchen' },
    { value: 'cro', viewValue: 'Crocory' },
    { value: 'woo', viewValue: 'Wooden' },
  ];

  // table 1
  displayedColumns2: string[] = ['product', 'description', 'quantity', 'price'];
  dataSource2 = PROJECT_DATA;

  constructor() {}
}
