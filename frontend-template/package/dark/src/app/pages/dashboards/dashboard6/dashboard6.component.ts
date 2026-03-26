import { Component } from '@angular/core';
import { AppDownloadCountComponent } from 'src/app/components/dashboard1/download-count/download-count.component';
import { AppSalesOverviewComponent } from 'src/app/components/dashboard1/sales-overview/sales-overview.component';
import { AppWeatherCardComponent } from 'src/app/components/dashboard1/weather-card/weather-card.component';
import { AppDiscountComponent } from 'src/app/components/dashboard6/discount/discount.component';
import { AppEarningComponent } from 'src/app/components/dashboard6/earning/earning.component';
import { AppMonthlyWinnerComponent } from 'src/app/components/dashboard6/monthly-winner/monthly-winner.component';
import { AppNewItemsComponent } from 'src/app/components/dashboard6/new-items/new-items.component';
import { AppProductASalesComponent } from 'src/app/components/dashboard6/product-a-sales/product-a-sales.component';
import { AppProductBSalesComponent } from 'src/app/components/dashboard6/product-b-sales/product-b-sales.component';
import { AppProductCSalesComponent } from 'src/app/components/dashboard6/product-c-sales/product-c-sales.component';
import { AppProductOverviewComponent } from 'src/app/components/dashboard6/product-overview/product-overview.component';
import { AppProductSalesComponent } from 'src/app/components/dashboard6/product-sales/product-sales.component';


@Component({
  selector: 'app-dashboard6',
  standalone: true,
  imports: [
    AppProductASalesComponent,
    AppProductBSalesComponent,
    AppProductCSalesComponent,
    AppSalesOverviewComponent,
    AppProductSalesComponent,
    AppDownloadCountComponent,
    AppWeatherCardComponent,
    AppProductOverviewComponent,
    AppEarningComponent,
    AppDiscountComponent,
    AppMonthlyWinnerComponent,
    AppNewItemsComponent
  ],
  templateUrl: './dashboard6.component.html',
})
export class AppDashboard6Component {
  constructor() { }
}
