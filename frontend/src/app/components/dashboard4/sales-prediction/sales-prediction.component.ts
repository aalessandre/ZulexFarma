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
  ApexGrid,
  ApexFill,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { MaterialModule } from 'src/app/material.module';

export interface predictionChart {
  series: ApexNonAxisChartSeries | any;
  chart: ApexChart | any;
  responsive: ApexResponsive[] | any;
  labels: any;
  tooltip: ApexTooltip | any;
  legend: ApexLegend | any;
  grid: ApexGrid | any;
  fill: ApexFill | any;
  colors: string[] | any;
  stroke: any;
  dataLabels: ApexDataLabels | any;
  plotOptions: ApexPlotOptions | any;
}

@Component({
  selector: 'app-sales-prediction',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './sales-prediction.component.html',
})
export class AppSalesPredictionComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public predictionChart!: Partial<predictionChart> | any;

  constructor() {
    this.predictionChart = {
      chart: {
        height: 150,
        type: "radialBar",
        fontFamily: "inherit",
        sparkline: {
          enabled: true,
        },
      },
      series: [100],
      colors: ["var(--mat-sys-primary)"],
      plotOptions: {
        radialBar: {
          startAngle: -135,
          endAngle: 135,
          track: {
            background: "#f2f4f8",
            startAngle: -135,
            endAngle: 135,
          },
          hollow: {
            size: "30%",
            background: "transparent",
          },
          dataLabels: {
            show: true,
            name: {
              show: false,
            },
            value: {
              show: false,
            },
            total: {
              show: true,
              fontSize: "20px",
              color: "#000",
              label: "91.4 %",
            },
          },
        },
      },
      fill: {
        type: "solid",
      },
      stroke: {
        lineCap: "butt",
      },
      tooltip: {
        enabled: true,
        fillSeriesColor: false,
        theme: "dark",
      },
      labels: ["Sales Prediction "],
    };
  }
}
