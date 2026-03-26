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

export interface chartOption {
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
  axisBorder: | any;
  marker: ApexMarkers | any;
}

@Component({
  selector: 'app-download-count',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './download-count.component.html',
})
export class AppDownloadCountComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public downloadOptions!: Partial<chartOption> | any;

  constructor() {
    // download
    this.downloadOptions = {

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
        foreColor: "#adb0bb",
        toolbar: {
          show: false,
        },
        sparkline: {
          enabled: true,
        },
      },
      colors: ["rgba(255, 255, 255, 0.7)"],
      grid: {
        show: false,
      },
      plotOptions: {
        bar: {
          horizontal: false,
          startingShape: "flat",
          endingShape: "flat",
          columnWidth: "60%",
          barHeight: "100%",
          borderRadius: 2,
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
