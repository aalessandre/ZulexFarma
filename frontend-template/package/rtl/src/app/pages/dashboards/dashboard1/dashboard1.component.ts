import { Component } from '@angular/core';
import { AppActivityTimelineComponent } from 'src/app/components/dashboard1/activity-timeline/activity-timeline.component';
import { AppBandwidthUsageComponent } from 'src/app/components/dashboard1/bandwidth-usage/bandwidth-usage.component';
import { AppBlogCardComponent } from 'src/app/components/dashboard1/blog-card/blog-card.component';
import { AppDownloadCountComponent } from 'src/app/components/dashboard1/download-count/download-count.component';
import { AppMyContactsComponent } from 'src/app/components/dashboard1/my-contacts/my-contacts.component';
import { AppNewsletterCampaignComponent } from 'src/app/components/dashboard1/newsletter-campaign/newsletter-campaign.component';
import { AppSalesOurVisitorsComponent } from 'src/app/components/dashboard1/our-visitors/our-visitors.component';
import { AppProfileCardComponent } from 'src/app/components/dashboard1/profile-card/profile-card.component';
import { AppSalesOverviewComponent } from 'src/app/components/dashboard1/sales-overview/sales-overview.component';
import { AppWeatherCardComponent } from 'src/app/components/dashboard1/weather-card/weather-card.component';

@Component({
  selector: 'app-dashboard1',
  standalone: true,
  imports: [
    AppSalesOverviewComponent,
    AppSalesOurVisitorsComponent,
    AppBlogCardComponent,
    AppNewsletterCampaignComponent,
    AppBandwidthUsageComponent,
    AppDownloadCountComponent,
    AppWeatherCardComponent,
    AppProfileCardComponent,
    AppMyContactsComponent,
    AppActivityTimelineComponent,
  ],
  templateUrl: './dashboard1.component.html',
})
export class AppDashboard1Component {
  constructor() { }
}
