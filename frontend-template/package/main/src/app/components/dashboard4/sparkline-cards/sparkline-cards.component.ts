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

export interface sparkchart {
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
  selector: 'app-sparkline-cards',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './sparkline-cards.component.html',
})
export class AppSparklineCardsComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public spark1Chart!: Partial<sparkchart> | any;
  public spark2Chart!: Partial<sparkchart> | any;
  public spark3Chart!: Partial<sparkchart> | any;
  public spark4Chart!: Partial<sparkchart> | any;

  constructor() {
    // 1
    this.spark1Chart = {
      series: [
        {
          name: "",
          data: [4, 5, 2, 10, 9, 12, 4, 9],
        },
      ],
      chart: {
        type: "bar",
        height: 70,
        toolbar: {
          show: false,
        },
        sparkline: {
          enabled: true,
        },
      },
      colors: ["var(--mat-sys-secondary)"],
      grid: {
        show: false,
      },
      plotOptions: {
        bar: {
          horizontal: false,
          startingShape: "flat",
          endingShape: "flat",
          columnWidth: "65%",
          barHeight: "100%",
          borderRadius: 4,
        },
      },
      dataLabels: {
        enabled: false,
      },
      stroke: {
        show: true,
        width: 4,
        colors: ["transparent"],
      },
      xaxis: {
        axisBorder: {
          show: false,
        },
        axisTicks: {
          show: false,
        },
        labels: {
          show: false,
        },
      },
      yaxis: {
        labels: {
          show: false,
        },
      },
      axisBorder: {
        show: false,
      },
      fill: {
        opacity: 1,
      },
      tooltip: {
        theme: "dark",
        style: {
          fontSize: "12px",
          fontFamily: "inherit",
        },
        x: {
          show: false,
        },
        y: {
          formatter: undefined,
        },
      },
    };
    // 2
    this.spark2Chart = {
      series: [
        {
          name: "",
          data: [4, 5, 2, 10, 9, 12, 4, 9],
        },
      ],
      chart: {
        type: "bar",
        height: 70,
        toolbar: {
          show: false,
        },
        sparkline: {
          enabled: true,
        },
      },
      colors: ["#6610f2"],
      grid: {
        show: false,
      },
      plotOptions: {
        bar: {
          horizontal: false,
          startingShape: "flat",
          endingShape: "flat",
          columnWidth: "65%",
          barHeight: "100%",
          borderRadius: 4,
        },
      },
      dataLabels: {
        enabled: false,
      },
      stroke: {
        show: true,
        width: 4,
        colors: ["transparent"],
      },
      xaxis: {
        axisBorder: {
          show: false,
        },
        axisTicks: {
          show: false,
        },
        labels: {
          show: false,
        },
      },
      yaxis: {
        labels: {
          show: false,
        },
      },
      axisBorder: {
        show: false,
      },
      fill: {
        opacity: 1,
      },
      tooltip: {
        theme: "dark",
        style: {
          fontSize: "12px",
          fontFamily: "inherit",
        },
        x: {
          show: false,
        },
        y: {
          formatter: undefined,
        },
      },
    };
    // 3
    this.spark3Chart = {
      series: [
        {
          name: "",
          data: [4, 5, 2, 10, 9, 12, 4, 9],
        },
      ],
      chart: {
        type: "bar",
        height: 70,
        toolbar: {
          show: false,
        },
        sparkline: {
          enabled: true,
        },
      },
      colors: ["var(--mat-sys-primary)"],
      grid: {
        show: false,
      },
      plotOptions: {
        bar: {
          horizontal: false,
          startingShape: "flat",
          endingShape: "flat",
          columnWidth: "65%",
          barHeight: "100%",
          borderRadius: 4,
        },
      },
      dataLabels: {
        enabled: false,
      },
      stroke: {
        show: true,
        width: 4,
        colors: ["transparent"],
      },
      xaxis: {
        axisBorder: {
          show: false,
        },
        axisTicks: {
          show: false,
        },
        labels: {
          show: false,
        },
      },
      yaxis: {
        labels: {
          show: false,
        },
      },
      axisBorder: {
        show: false,
      },
      fill: {
        opacity: 1,
      },
      tooltip: {
        theme: "dark",
        style: {
          fontSize: "12px",
          fontFamily: "inherit",
        },
        x: {
          show: false,
        },
        y: {
          formatter: undefined,
        },
      },
    };
    // 4
    this.spark4Chart = {
      series: [
        {
          name: "",
          data: [4, 5, 2, 10, 9, 12, 4, 9],
        },
      ],
      chart: {
        type: "bar",
        height: 70,
        toolbar: {
          show: false,
        },
        sparkline: {
          enabled: true,
        },
      },
      colors: ["#F8285A"],
      grid: {
        show: false,
      },
      plotOptions: {
        bar: {
          horizontal: false,
          startingShape: "flat",
          endingShape: "flat",
          columnWidth: "65%",
          barHeight: "100%",
          borderRadius: 4,
        },
      },
      dataLabels: {
        enabled: false,
      },
      stroke: {
        show: true,
        width: 4,
        colors: ["transparent"],
      },
      xaxis: {
        axisBorder: {
          show: false,
        },
        axisTicks: {
          show: false,
        },
        labels: {
          show: false,
        },
      },
      yaxis: {
        labels: {
          show: false,
        },
      },
      axisBorder: {
        show: false,
      },
      fill: {
        opacity: 1,
      },
      tooltip: {
        theme: "dark",
        style: {
          fontSize: "12px",
          fontFamily: "inherit",
        },
        x: {
          show: false,
        },
        y: {
          formatter: undefined,
        },
      },
    };
  }
}
