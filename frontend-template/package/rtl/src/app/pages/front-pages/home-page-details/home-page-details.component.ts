import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { RouterLink } from '@angular/router';
import { ImagesComponent } from '../images/images.component';
import { FooterComponent } from '../footer/footer.component';
import { CardSliderComponent } from '../card-slider/card-slider.component';
import { PricePageComponent } from '../price-page/price-page.component';
import { frameworks, stats } from '../front-end-datatype';

@Component({
  selector: 'app-home-page-details',
  imports: [
    MaterialModule,
    TablerIconsModule,
    CommonModule,
    RouterLink,
    ImagesComponent,
   PricePageComponent ,
    FooterComponent,
    CardSliderComponent
  ],
  templateUrl: './home-page-details.component.html',
  styleUrl: './home-page-details.component.scss',
})
export class HomePageDetailsComponent {
  selectedTab: string = 'team';
  
   hideToolbar:boolean=true;

  frameworks=frameworks;

  stats = stats;

  selectTab(tab: string): void {
    this.selectedTab = tab;
  }


}
