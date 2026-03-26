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

export interface salesamplepixelchartOptions {
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
  selector: 'app-sales-ample-pixel',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './sales-ample-pixel.component.html',
})
export class AppSalesAmplePixelComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public salesamplepixelchartOptions!: Partial<salesamplepixelchartOptions> | any;

  constructor() {
    this.salesamplepixelchartOptions = {
      series: [
        {
          name: "Growth ",
          data: [0, 1, 1, 10, 24, 6, 12, 4, 21, 15, 44, 24, 28, 4, 10, 21, 5, 47],
        },
        {
          name: "Loss ",
          data: [
            0, 4, 3, 24, 9, 10, 18, 15, 44, 17, 19, 26, 31, 8, 37, 10, 24, 51,
          ],
        },
      ],
      chart: {
        height: 350,
        type: "area",
        stacked: false,
        fontFamily: "inherit",
        zoom: {
          enabled: false,
        },
        toolbar: {
          show: false,
        },
      },
      colors: ["rgba(38, 198, 218, 0.7)", "rgba(38, 198, 218, 0.1)"],
      dataLabels: {
        enabled: false,
      },
      stroke: {
        show: false,
      },
      markers: {
        size: 2,
        strokeColors: "transparent",
        colors: "#26c6da",
      },
      fill: {
        type: "solid",
        colors: ["rgba(38, 198, 218, 0.7)", "rgba(38, 198, 218, 0.4)"],
        opacity: 1,
      },
      grid: {
        strokeDashArray: 3,
        borderColor: "rgba(0,0,0,0.2)",
      },
      xaxis: {
        axisBorder: {
          show: false,
        },
        axisTicks: {
          show: false,
        },
        categories: [
          "0",
          "4",
          "8",
          "12",
          "16",
          "20",
          "24",
          "30",
          "16",
          "20",
          "24",
          "30",
          "34",
          "38",
          "42",
          "46",
          "50",
          "54",
        ],
        labels: {
          style: {
            colors: "#a1aab2",
          },
        },
      },
      yaxis: {
        labels: {
          style: {
            colors: "#a1aab2",
          },
        },
      },
      legend: {
        show: false,
      },
      tooltip: {
        theme: "dark",
        marker: {
          show: true,
        },
      },
    };
  }
}
