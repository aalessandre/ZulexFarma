import { Component } from '@angular/core';
import { AppBandwidthUsageComponent } from 'src/app/components/dashboard1/bandwidth-usage/bandwidth-usage.component';
import { AppBlogCardComponent } from 'src/app/components/dashboard1/blog-card/blog-card.component';
import { AppDownloadCountComponent } from 'src/app/components/dashboard1/download-count/download-count.component';
import { AppNewsletterCampaignComponent } from 'src/app/components/dashboard1/newsletter-campaign/newsletter-campaign.component';
import { AppProfileCardComponent } from 'src/app/components/dashboard1/profile-card/profile-card.component';
import { AppCurrentVisitsComponent } from 'src/app/components/dashboard2/current-visits/current-visits.component';
import { AppSalesOurVisitorsComponent } from 'src/app/components/dashboard2/our-visitors/our-visitors.component';
import { AppProjectDataComponent } from 'src/app/components/dashboard2/project-data/project-data.component';
import { RecentcommentComponent } from 'src/app/components/dashboard2/recent-comments/recent-comments.component';
import { AppRevenueCardsComponent } from 'src/app/components/dashboard2/revenue-cards/revenue-cards.component';
import { AppTodoListComponent } from 'src/app/components/dashboard2/todo-list/todo-list.component';

@Component({
  selector: 'app-dashboard2',
  standalone: true,
  imports: [AppRevenueCardsComponent,
    AppBandwidthUsageComponent,
    AppDownloadCountComponent,
    AppSalesOurVisitorsComponent,
    AppCurrentVisitsComponent,
    AppBlogCardComponent,
    AppNewsletterCampaignComponent,
    AppProjectDataComponent,
    AppProjectDataComponent,
    AppProfileCardComponent,
    RecentcommentComponent,
    AppTodoListComponent],
  templateUrl: './dashboard2.component.html',
})
export class AppDashboard2Component {
  constructor() { }
}
