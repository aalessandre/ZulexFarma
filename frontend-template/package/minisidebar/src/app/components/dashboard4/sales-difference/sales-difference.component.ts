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

export interface differenceChart {
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
  selector: 'app-sales-difference',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './sales-difference.component.html',
})
export class AppSalesDifferenceComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public differenceChart!: Partial<differenceChart> | any;

  constructor() {
    this.differenceChart = {
      series: [25, 10],
      labels: ["A ", "B "],
      chart: {
        type: "donut",
        height: 100,
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
            size: "60%",
            labels: {
              show: false,
              name: {
                show: true,
                offsetY: 7,
              },
              value: {
                show: false,
              },
              total: {
                show: false,
                color: "#a1aab2",
                fontSize: "13px",
                label: "Visits",
              },
            },
          },
        },
      },
      colors: ["var(--mat-sys-secondary)", "#f2f4f8"],
      tooltip: {
        show: true,
        fillSeriesColor: false,
      },
      legend: {
        show: false,
      },
      responsive: [
        {
          breakpoint: 426,
          options: {
            chart: {
              offsetX: -35,
              width: 200,
            },
          },
        },
      ],
    };
  }
}
