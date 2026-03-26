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
  ApexResponsive,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { MaterialModule } from 'src/app/material.module';

export interface downloadcountChart {
  series: ApexAxisChartSeries | any;
  chart: ApexChart | any;
  dataLabels: ApexDataLabels | any;
  plotOptions: ApexPlotOptions | any;
  responsive: ApexResponsive[] | any;
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
  selector: 'app-download-count-chart',
  standalone: true,
  imports: [NgApexchartsModule, TablerIconsModule, MaterialModule],
  templateUrl: './download-count-chart.component.html',
})
export class AppDownloadCountChartComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public downloadcountChart!: Partial<downloadcountChart> | any;

  constructor() {
    this.downloadcountChart = {
      series: [
        {
          name: "Premium",
          data: [5, 4, 3, 7, 5, 10, 3],
        },
        {
          name: "Free",
          data: [3, 2, 9, 5, 4, 6, 4],
        },
      ],

      chart: {
        type: "bar",
        height: 315,
        fontFamily: "inherit",
        toolbar: { show: false },
      },

      colors: [
        "var(--mat-sys-primary)",
        "var(--mat-sys-secondary)"
      ],

      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: "42%",
          borderRadius: 6,
          borderRadiusApplication: "end",
        },
      },

      dataLabels: {
        enabled: false,
      },

      stroke: {
        show: true,
        width: 2,
        colors: ["transparent"],
      },

      grid: {
        show: true,
        borderColor: "rgba(0,0,0,0.08)",
        strokeDashArray: 4,
        xaxis: { lines: { show: false } },
        yaxis: { lines: { show: true } },
        padding: {
          left: 10,
          right: 10,
        },
      },

      xaxis: {
        categories: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
        axisBorder: { show: false },
        axisTicks: { show: false },
        labels: {
          style: {
            colors: "#9aa0a6",
            fontSize: "12px",
          },
        },
      },

      yaxis: {
        labels: {
          style: {
            colors: "#9aa0a6",
            fontSize: "12px",
          },
        },
      },

      legend: {
        show: false,
        position: "top",
        horizontalAlign: "right",
        fontSize: "13px",
      },

      tooltip: {
        theme: "dark",
        y: {
          formatter: (val: number) => `${val} downloads`,
        },
      },

      fill: {
        opacity: 1,
      },

      responsive: [
        {
          breakpoint: 768,
          options: {
            plotOptions: {
              bar: {
                columnWidth: "55%",
              },
            },
            legend: {
              position: "bottom",
            },
          },
        },
      ],
    };
  }

}
