import { Component, ViewChild } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import {
  ApexChart,
  ChartComponent,
  ApexDataLabels,
  ApexLegend,
  ApexTooltip,
  ApexPlotOptions,
  ApexAxisChartSeries,
  ApexXAxis,
  ApexYAxis,
  ApexGrid,
  ApexFill,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { MaterialModule } from 'src/app/material.module';

export interface newsletterchartOptions {
  series: ApexAxisChartSeries | any;
  chart: ApexChart | any;
  xaxis: ApexXAxis | any;
  yaxis: ApexYAxis | any;
  stroke: any | any;
  plotOptions: ApexPlotOptions | any;
  tooltip: ApexTooltip | any;
  dataLabels: ApexDataLabels | any;
  legend: ApexLegend | any;
  colors: string[] | any;
  markers: any;
  grid: ApexGrid | any;
  fill: ApexFill | any;
}

@Component({
  selector: 'app-newsletter-campaign',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './newsletter-campaign.component.html',
})
export class AppNewsletterCampaignComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public newsletterchartOptions!: Partial<newsletterchartOptions> | any;

  constructor() {
    this.newsletterchartOptions = {
      series: [
        {
          name: 'Open Rate',
          data: [
            65, 80, 80, 60, 60, 45, 45, 80, 80, 70, 70, 90, 90, 80, 80, 80, 60,
            60, 50,
          ],
        },
        {
          name: 'Recurring Payments',
          data: [
            90, 110, 110, 95, 95, 85, 85, 95, 95, 115, 115, 100, 100, 115, 115,
            95, 95, 85, 85,
          ],
        },
      ],
      chart: {
        fontFamily: 'inherit',
        type: 'area',
        height: 300,
        offsetX: -15,
        toolbar: { show: !1 },
      },
      plotOptions: {},
      legend: { show: !1 },
      dataLabels: { enabled: !1 },
      fill: { type: 'solid', opacity: 0.07, colors: ['#1B84FF', '#43CED7'] },
      stroke: {
        curve: 'smooth',
        show: !0,
        width: 2,
        colors: ['#1B84FF', '#43CED7'],
      },
      xaxis: {
        categories: [
          '',
          '8 AM',
          '81 AM',
          '9 AM',
          '10 AM',
          '11 AM',
          '12 PM',
          '13 PM',
          '14 PM',
          '15 PM',
          '16 PM',
          '17 PM',
          '18 PM',
          '18:20 PM',
          '18:20 PM',
          '19 PM',
          '20 PM',
          '21 PM',
          '',
        ],
        axisBorder: { show: !1 },
        axisTicks: { show: !1 },
        tickAmount: 6,
        labels: {
          rotate: 0,
          rotateAlways: !0,
          style: { fontSize: '12px', colors: '#a1aab2' },
        },
        crosshairs: {
          position: 'front',
          stroke: { color: ['#1B84FF', '#43CED7'], width: 1, dashArray: 3 },
        },
      },
      yaxis: {
        max: 120,
        min: 30,
        tickAmount: 6,
        labels: { style: { fontSize: '12px', colors: '#a1aab2' } },
      },
      states: {
        normal: { filter: { type: 'none', value: 0 } },
        hover: { filter: { type: 'none', value: 0 } },
        active: {
          allowMultipleDataPointsSelection: !1,
          filter: { type: 'none', value: 0 },
        },
      },
      tooltip: {
        theme: 'dark',
      },
      colors: ['#1B84FF', '#43CED7'],
      grid: {
        borderColor: 'var(--bs-border-color)',
        strokeDashArray: 4,
        yaxis: { lines: { show: !0 } },
      },
      markers: { strokeColor: ['#1B84FF', '#43CED7'], strokeWidth: 3 },
    };
  }
}
