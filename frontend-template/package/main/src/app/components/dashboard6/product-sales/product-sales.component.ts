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

export interface productsalesChart {
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
  selector: 'app-product-sales',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './product-sales.component.html',
})
export class AppProductSalesComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public productsalesChart!: Partial<productsalesChart> | any;

  constructor() {
    this.productsalesChart = {

      series: [50, 40, 30, 10],
      labels: ["Mobile", "Tablet", "Other", "Desktop"],
      chart: {
        type: "donut",
        height: 210,
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
                label: "Yearly Sales",
              },
            },
          },
        },
      },
      colors: ["var(--mat-sys-primary)", "var(--mat-sys-secondary)", "#eceff1", "#6610f2"],
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
