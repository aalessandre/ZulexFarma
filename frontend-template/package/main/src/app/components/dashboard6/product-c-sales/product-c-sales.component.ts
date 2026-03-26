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

export interface productcsales {
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
  selector: 'app-product-c-sales',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './product-c-sales.component.html',
})
export class AppProductCSalesComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public productcsales!: Partial<productcsales> | any;

  constructor() {
    this.productcsales = {
      series: [
        {
          name: "",
          data: [4, 5, 2, 10, 9, 12, 4, 9, 4, 5, 3, 10],
        },
      ],
      chart: {
        type: "bar",
        fontFamily: "inherit",
        height: 70,
        toolbar: {
          show: false,
        },
        sparkline: {
          enabled: true,
        },
      },
      colors: ["var(--mat-sys-error)"],
      grid: {
        show: false,
      },
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: "30%",
          borderRadius: 2,
          endingShape: "rounded",
        },
      },
      dataLabels: {
        enabled: false,
      },
      stroke: {
        show: true,
        width: 6,
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
