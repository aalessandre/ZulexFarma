import { Component } from '@angular/core';
import { AppBandwidthUsageComponent } from 'src/app/components/dashboard1/bandwidth-usage/bandwidth-usage.component';
import { AppDownloadCountComponent } from 'src/app/components/dashboard1/download-count/download-count.component';
import { AppNewsletterCampaignComponent } from 'src/app/components/dashboard1/newsletter-campaign/newsletter-campaign.component';
import { AppProfileCardComponent } from 'src/app/components/dashboard1/profile-card/profile-card.component';
import { AppProjectDataComponent } from 'src/app/components/dashboard2/project-data/project-data.component';
import { AppAdExpenseComponent } from 'src/app/components/dashboard3/ad-expense/ad-expense.component';
import { AppCurrentVisitsTwoComponent } from 'src/app/components/dashboard3/current-visits-two/current-visits-two.component';
import { AppDesignMeetingsComponent } from 'src/app/components/dashboard3/design-meetings/design-meetings.component';
import { AppHighlightsComponent } from 'src/app/components/dashboard3/highlights/highlights.component';
import { AppMaterialProComponent } from 'src/app/components/dashboard3/material-pro/material-pro.component';
import { AppMyContactsTwoComponent } from 'src/app/components/dashboard3/my-contacts-two/my-contacts-two.component';
import { OnlineRevenueComponent } from 'src/app/components/dashboard3/online-revenue/online-revenue.component';
import { AppUpgradePlanComponent } from 'src/app/components/dashboard3/upgrade-plan/upgrade-plan.component';
import { AppNotificationComponent } from 'src/app/components/dashboard5/notification/notification.component';

@Component({
  selector: 'app-dashboard3',
  standalone: true,
  imports: [
    OnlineRevenueComponent,
    AppAdExpenseComponent,
    AppUpgradePlanComponent,
    AppMaterialProComponent,
    AppHighlightsComponent,
    AppDesignMeetingsComponent,
    AppNewsletterCampaignComponent,
    AppMyContactsTwoComponent,
    AppCurrentVisitsTwoComponent,
    AppProjectDataComponent,
    AppBandwidthUsageComponent,
    AppDownloadCountComponent,
    AppProfileCardComponent,
    AppNotificationComponent,
  ],
  templateUrl: './dashboard3.component.html',
})
export class AppDashboard3Component {
  constructor() {}
}
