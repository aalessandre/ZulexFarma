import { Component, ViewChild } from '@angular/core';
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
import { TablerIconsModule } from 'angular-tabler-icons';
export interface totalrevenueChart {
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
  selector: 'app-total-revenue',
  standalone: true,
  imports: [NgApexchartsModule, MaterialModule, TablerIconsModule],
  templateUrl: './total-revenue.component.html',
})
export class AppTotalRevenueComponent {
  @ViewChild('chart') chart: ChartComponent = Object.create(null);

  public totalrevenueChart!: Partial<totalrevenueChart> | any;

  constructor() {
    this.totalrevenueChart = {
      series: [
        {
          name: '2026',
          data: [800000, 1200000, 1400000, 1300000, 1200000, 1400000, 1300000, 1300000, 1200000],
        },
        {
          name: '2025',
          data: [200000, 400000, 500000, 300000, 400000, 500000, 300000, 300000, 400000],
        },
        {
          name: '2024',
          data: [100000, 200000, 400000, 600000, 200000, 400000, 600000, 600000, 200000],
        },
      ],

      chart: {
        type: 'bar',
        height: 363,
        stacked: true,
        fontFamily: 'inherit',
        toolbar: { show: false },
        zoom: { enabled: false },
      },

      colors: [
        'var(--mat-sys-primary)',
        'var(--mat-sys-secondary)',
        '#F8285A'
      ],

      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '32%',
          borderRadius: 6,
          borderRadiusApplication: 'end',
        },
      },

      grid: {
        show: true,
        borderColor: 'rgba(0,0,0,0.08)',
        strokeDashArray: 4,
        xaxis: { lines: { show: false } },
        yaxis: { lines: { show: true } },
        padding: {
          top: 10,
          right: 10,
          bottom: 0,
          left: 10,
        },
      },

      dataLabels: {
        enabled: false,
      },

      legend: {
        show: false,
        position: 'top',
        horizontalAlign: 'right',
        fontSize: '13px',
        labels: {
          colors: '#6b7280',
        },
        markers: {
          radius: 4,
        },
      },

      xaxis: {
        categories: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sept'],
        axisBorder: { show: false },
        axisTicks: { show: false },
        labels: {
          style: {
            colors: '#9aa0a6',
            fontSize: '12px',
          },
        },
      },

      yaxis: {
        labels: {
          style: {
            colors: '#9aa0a6',
            fontSize: '12px',
          },
          formatter: (val: number) => {
            if (val >= 1000000) return (val / 1000000).toFixed(1) + 'M';
            if (val >= 1000) return (val / 1000).toFixed(0) + 'K';
            return val.toString();
          },
        },
      },

      tooltip: {
        theme: 'dark',
        y: {
          formatter: (val: number) => {
            return '₹ ' + val.toLocaleString();
          },
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
              bar: { columnWidth: '45%' },
            },
            legend: {
              position: 'bottom',
            },
          },
        },
      ],
    };
  }

}
