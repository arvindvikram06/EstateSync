import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export interface NavItem {
  id: string;
  label: string;
  icon: string;
  badge?: number;
}

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.css'
})
export class DashboardLayoutComponent {
  readonly title     = input<string>('');
  readonly subtitle  = input<string>('');
  readonly navItems  = input<NavItem[]>([]);
  readonly activeTab = input<string>('');
  readonly tabChange = output<string>();
}
