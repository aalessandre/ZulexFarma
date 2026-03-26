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
  selector: 'app-online-revenue',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './online-revenue.component.html',
})
export class OnlineRevenueComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public downloadOptions!: Partial<chartOption> | any;

  constructor() {
    // download
    this.downloadOptions = {

      chart: {
        id: "online-revenue",
        type: "area",
        height: 64,
        sparkline: {
          enabled: true,
        },
        group: "online-revenue",
        fontFamily: "inherit",
        foreColor: "#adb0bb",
      },
      series: [
        {
          name: '',
          color: "#43CED7",
          data: [0, 150, 110, 240, 200, 200, 300, 200],
        },
      ],
      stroke: {
        curve: "straight",
        width: 2,
      },

      fill: {
        type: "gradient",
        gradient: {
          shadeIntensity: 0,
          inverseColors: false,
          opacityFrom: 0.40,
          opacityTo: 0,
          stops: [0, 200],
        },
      },

      markers: {
        size: 0,
      },
      tooltip: {
        theme: "dark",
        fixed: {
          enabled: true,
          position: "right",
        },
        x: {
          show: false,
        },
      },

    };
  }
}
