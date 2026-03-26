
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { FooterComponent } from '../footer/footer.component';
import { PricePageComponent } from '../price-page/price-page.component';

@Component({
  selector: 'app-pricing',
  imports: [MaterialModule, TablerIconsModule, RouterLink, FooterComponent, PricePageComponent],
  templateUrl: './pricing.component.html',
})
export class PricingComponent {


}
