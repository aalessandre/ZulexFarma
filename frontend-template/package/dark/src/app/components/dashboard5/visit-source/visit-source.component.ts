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

export interface visitsourceChart {
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

interface month {
  value: string;
  viewValue: string;
}

@Component({
  selector: 'app-visit-source',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './visit-source.component.html',
})
export class AppVisitSourceComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public visitsourceChart!: Partial<visitsourceChart> | any;

  months: month[] = [
    { value: 'jan', viewValue: 'January' },
    { value: 'feb', viewValue: 'February' },
    { value: 'mar', viewValue: 'March' },
    { value: 'apr', viewValue: 'April' },
  ];

  constructor() {
    this.visitsourceChart = {

      labels: ["Desktop", "Tablet", "Mobile", "Other"],
      series: [100, 40, 80, 90],
      chart: {
        type: "donut",
        height: 200,
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
            size: "85%",
            labels: {
              show: true,
              name: {
                show: false,
                offsetY: 7,
              },
              value: {
                show: true,
                fontSize: "20px",
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
      colors: ["var(--mat-sys-primary)", "#F8285A", "var(--mat-sys-secondary)", "#dadada"],
      tooltip: {
        show: true,
        fillSeriesColor: false,
      },
      legend: {
        show: false,
      },
      responsive: [
        {
          breakpoint: 480,
          options: {
            chart: {
              width: 200,
            },
          },
        },
      ],

    };
  }
}
