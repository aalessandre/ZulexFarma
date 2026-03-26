import { Component } from '@angular/core';
import { AppActivityTimelineComponent } from 'src/app/components/dashboard1/activity-timeline/activity-timeline.component';
import { AppBandwidthUsageComponent } from 'src/app/components/dashboard1/bandwidth-usage/bandwidth-usage.component';
import { AppDownloadCountComponent } from 'src/app/components/dashboard1/download-count/download-count.component';
import { AppAccountProfileComponent } from 'src/app/components/dashboard5/account-profile/account-profile.component';
import { AppDownloadCountChartComponent } from 'src/app/components/dashboard5/download-count-chart/download-count-chart.component';
import { AppExperiancesComponent } from 'src/app/components/dashboard5/experiances/experiances.component';
import { AppNotificationComponent } from 'src/app/components/dashboard5/notification/notification.component';
import { AppProductCardComponent } from 'src/app/components/dashboard5/product-card/product-card.component';
import { AppProductsAvailabilityComponent } from 'src/app/components/dashboard5/products-availability/products-availability.component';
import { AppProfileCardTwoComponent } from 'src/app/components/dashboard5/profile-card-two/profile-card-two.component';
import { AppReviewsComponent } from 'src/app/components/dashboard5/reviews/reviews.component';
import { AppSalesAmplePixelComponent } from 'src/app/components/dashboard5/sales-ample-pixel/sales-ample-pixel.component';
import { AppVisitSourceComponent } from 'src/app/components/dashboard5/visit-source/visit-source.component';

@Component({
  selector: 'app-dashboard5',
  standalone: true,
  imports: [
    AppProductCardComponent,
    AppSalesAmplePixelComponent,
    AppBandwidthUsageComponent,
    AppDownloadCountComponent,
    AppDownloadCountChartComponent,
    AppProductsAvailabilityComponent,
    AppNotificationComponent,
    AppReviewsComponent,
    AppVisitSourceComponent,
    AppExperiancesComponent,
    AppProfileCardTwoComponent,
    AppActivityTimelineComponent,
    AppAccountProfileComponent
  ],
  templateUrl: './dashboard5.component.html',
})
export class AppDashboard5Component {
  constructor() {}
}
