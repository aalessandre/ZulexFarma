import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { ImagesComponent } from '../images/images.component';
import { CardSliderComponent } from '../card-slider/card-slider.component';
import { PricePageComponent } from '../price-page/price-page.component';
import { FooterComponent } from '../footer/footer.component';
import { records, support } from '../front-end-datatype';
import { RouterLink } from '@angular/router';
@Component({
  selector: 'app-about-us',
  imports: [MaterialModule,
    TablerIconsModule, CommonModule, ImagesComponent, CardSliderComponent, PricePageComponent, FooterComponent, RouterLink],
  templateUrl: './about-us.component.html',
})
export class AboutUsComponent {

  hideToolbar: boolean = false;

  support = support;

  records = records;
}
