import { Component } from '@angular/core';
import { AppMyContactsTwoComponent } from 'src/app/components/dashboard3/my-contacts-two/my-contacts-two.component';
import { AppBrowserStatsComponent } from 'src/app/components/dashboard4/browser-stats/browser-stats.component';
import { AppCountryVisitsComponent } from 'src/app/components/dashboard4/country-visit/country-visit.component';
import { AppRecentChatComponent } from 'src/app/components/dashboard4/recent-chat/recent-chat.component';
import { AppSalesDifferenceComponent } from 'src/app/components/dashboard4/sales-difference/sales-difference.component';
import { AppSalesPredictionComponent } from 'src/app/components/dashboard4/sales-prediction/sales-prediction.component';
import { AppSparklineCardsComponent } from 'src/app/components/dashboard4/sparkline-cards/sparkline-cards.component';
import { AppTotalRevenueComponent } from 'src/app/components/dashboard4/total-revenue/total-revenue.component';
import { AppFullcalendarComponent } from '../../apps/fullcalendar/fullcalendar.component';

@Component({
  selector: 'app-dashboard4',
  standalone: true,
  imports: [
    AppSparklineCardsComponent,
    AppCountryVisitsComponent,
    AppBrowserStatsComponent,
    AppTotalRevenueComponent,
    AppSalesPredictionComponent,
    AppSalesDifferenceComponent,
    AppMyContactsTwoComponent,
    AppRecentChatComponent,
    AppFullcalendarComponent
  ],
  templateUrl: './dashboard4.component.html',
})
export class AppDashboard4Component {
  constructor() {}
}
