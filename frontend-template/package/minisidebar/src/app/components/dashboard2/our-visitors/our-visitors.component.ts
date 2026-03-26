import { Component, ViewChild } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import {
  ApexChart,
  ChartComponent,
  ApexDataLabels,
  ApexPlotOptions,
  ApexLegend,
  ApexTooltip,
  ApexNonAxisChartSeries,
  ApexResponsive,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { MaterialModule } from 'src/app/material.module';

export interface ourvisitorChart {
  series: ApexNonAxisChartSeries | any;
  chart: ApexChart | any;
  responsive: ApexResponsive[] | any;
  labels: any;
  tooltip: ApexTooltip | any;
  legend: ApexLegend | any;
  colors: string[] | any;
  stroke: any;
  dataLabels: ApexDataLabels | any;
  plotOptions: ApexPlotOptions | any;
}

@Component({
  selector: 'app-our-visitors',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './our-visitors.component.html',
})
export class AppSalesOurVisitorsComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public ourvisitorChart!: Partial<ourvisitorChart> | any;

  constructor() {
    this.ourvisitorChart = {

      series: [50, 40, 30, 10],
      labels: ["Mobile", "Tablet", "Other", "Desktop"],
      chart: {
        type: "donut",
        height: 215,
        fontFamily: "inherit",
      },
      dataLabels: {
        enabled: false,
      },
      stroke: {
        width: 0,
      },
      plotOptions: {
        pie: {
          expandOnClick: true,
          donut: {
            size: "83",
            labels: {
              show: true,
              name: {
                show: true,
                offsetY: 7,
              },
              value: {
                show: false,
              },
              total: {
                show: true,
                color: "#a1aab2",
                fontSize: "13px",
                label: "Our Visitor",
              },
            },
          },
        },
      },
      colors: ['var(--mat-sys-primary)', 'var(--mat-sys-secondary)', '#eceff180', '#725AF2'],
      tooltip: {
        show: true,
        fillSeriesColor: false,
      },
      legend: {
        show: false,
      },
      responsive: [
        {
          breakpoint: 1025,
          options: {
            chart: {
              height: 270,
            },
          },
        },
        {
          breakpoint: 426,
          options: {
            chart: {
              height: 250,
            },
          },
        },
      ],
    };
  }
}
