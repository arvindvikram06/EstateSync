import {
  Component, ChangeDetectionStrategy, computed, input, output
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NgApexchartsModule } from 'ng-apexcharts';
import type { ApexChart, ApexNonAxisChartSeries, ApexAxisChartSeries, ApexDataLabels,
  ApexPlotOptions, ApexLegend, ApexFill, ApexTooltip, ApexXAxis, ApexStroke } from 'ng-apexcharts';
import { AdminDashboardStats } from '../../../../core/models/admin.model';
import { formatCurrency } from '../../../../core/utils/format.utils';

@Component({
  selector: 'app-admin-overview-tab',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, MatProgressSpinnerModule, NgApexchartsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-overview-tab.component.html',
})
export class AdminOverviewTabComponent {
  readonly tabSwitch = output<string>();

  // Stats are fetched once by the parent dashboard and shared with this tab.
  readonly stats   = input.required<AdminDashboardStats | null>();
  readonly loading = input(false);

  // Computed chart data
  readonly propertyDonut    = computed(() => this.buildDonut(this.stats()?.propertyStatusBreakdown ?? {}));
  readonly leaseDonut       = computed(() => this.buildDonut(this.stats()?.leaseStatusBreakdown ?? {}));
  readonly billStatusDonut  = computed(() => this.buildDonut(this.stats()?.billStatusBreakdown ?? {}));
  readonly revenueTypeDonut = computed(() => this.buildDonut(this.stats()?.revenueByBillType ?? {}));
  readonly payoutDonut      = computed(() => this.buildDonut(this.stats()?.payoutStatusBreakdown ?? {}));
  readonly usersArea        = computed(() => this.buildArea(this.stats()?.newUsersTimeSeries ?? [], 'New Users'));
  readonly paymentsArea     = computed(() => this.buildArea(this.stats()?.paymentsTimeSeries ?? [], 'Revenue (₹)'));

  readonly donutColors = ['#6750A4','#059669','#D97706','#DC2626','#0284C7','#7C3AED','#0891B2'];

  readonly donutChartBase: ApexChart = {
    type: 'donut', height: 200,
    toolbar: { show: false },
    fontFamily: 'Inter, Roboto, system-ui',
  };

  readonly areaChart: ApexChart = {
    type: 'area', height: 220,
    toolbar: { show: false },
    fontFamily: 'Inter, Roboto, system-ui',
    zoom: { enabled: false },
  };

  readonly paymentsTooltip: ApexTooltip = {
    theme: 'light',
    y: { formatter: (val: number) => '₹' + val.toLocaleString('en-IN') },
  };

  readonly revenueTooltip: ApexTooltip = {
    theme: 'light',
    y: { formatter: (val: number) => '₹' + Math.round(val).toLocaleString('en-IN') },
  };

  readonly revenuePlotOptions: ApexPlotOptions = {
    pie: {
      donut: {
        size: '60%',
        labels: {
          show: true,
          total: {
            show: true,
            label: 'Total',
            fontSize: '13px',
            formatter: (w: { globals: { seriesTotals: number[] } }) => {
              const sum = w.globals.seriesTotals.reduce((a, b) => a + b, 0);
              return '₹' + Math.round(sum).toLocaleString('en-IN');
            },
          },
        },
      },
    },
  };

  private buildDonut(breakdown: Record<string, number>): { series: ApexNonAxisChartSeries; labels: string[] } {
    const entries = Object.entries(breakdown).filter(([, v]) => v > 0);
    return {
      series: entries.map(([, v]) => v),
      labels: entries.map(([k]) => k),
    };
  }

  private buildArea(points: { date: string; count: number; amount: number }[], name: string): {
    series: ApexAxisChartSeries; categories: string[]
  } {
    return {
      series: [{ name, data: points.map(p => name.includes('₹') ? p.amount : p.count) }],
      categories: points.map(p => p.date.slice(5)), // MM-DD
    };
  }

  readonly formatCurrency = formatCurrency;
}
