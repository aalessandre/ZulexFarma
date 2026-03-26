import { Component, ViewChild } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import {
  ApexChart,
  ChartComponent,
  ApexDataLabels,
  ApexLegend,
  ApexStroke,
  ApexTooltip,
  ApexAxisChartSeries,
  ApexXAxis,
  ApexYAxis,
  ApexGrid,
  ApexPlotOptions,
  ApexFill,
  ApexMarkers,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { MaterialModule } from 'src/app/material.module';

export interface inexpuchartOptions {
  series: ApexAxisChartSeries | any;
  chart: ApexChart | any;
  dataLabels: ApexDataLabels | any;
  plotOptions: ApexPlotOptions | any;
  yaxis: ApexYAxis | any;
  xaxis: ApexXAxis | any;
  fill: ApexFill | any;
  tooltip: ApexTooltip | any;
  stroke: ApexStroke | any;
  legend: ApexLegend | any;
  grid: ApexGrid | any;
  colors: string[] | any;
  marker: ApexMarkers | any;
}

@Component({
  selector: 'app-bandwidth-usage',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './bandwidth-usage.component.html',
})
export class AppBandwidthUsageComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public inexpuchartOptions!: Partial<inexpuchartOptions> | any;

  constructor() {
    this.inexpuchartOptions = {

      series: [
        {
          name: "",
          labels: ["0", "4", "8", "12", "16", "20", "24", "30"],
          data: [0, 8, 12, 10, 6, 8, 15, 23],
        },
      ],
      chart: {
        height: 50,
        type: "line",
        foreColor: "#adb0bb",
        toolbar: {
          show: false,
        },
        sparkline: {
          enabled: true,
        },
      },
      colors: ["#fff"],
      fill: {
        type: "solid",
        opacity: 1,
        colors: ["#fff"],
      },
      grid: {
        show: false,
      },
      stroke: {
        curve: "smooth",
        lineCap: "square",
        colors: ["#fff"],
        width: 2,
      },
      markers: {
        size: 0,
        colors: ["#fff"],
        strokeColors: "transparent",
        shape: "square",
        hover: {
          size: 7,
        },
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
      tooltip: {
        theme: "dark",
        style: {
          fontSize: "10px",
          fontFamily: "inherit",
        },
        x: {
          show: false,
        },
        y: {
          formatter: undefined,
        },
        marker: {
          show: true,
        },
        followCursor: true,
      },
    };
  }
}
