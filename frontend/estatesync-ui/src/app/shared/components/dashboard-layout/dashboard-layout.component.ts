import { Component, ChangeDetectionStrategy, input, output, signal, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

export interface NavItem {
  id: string;
  label: string;
  icon: string;
  badge?: number;
  route?: string;
}

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [MatIconModule, MatButtonModule],
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
  @Input() topOffset = 64;

  // Open by default on desktop, closed on mobile
  readonly sidebarOpen = signal(
    typeof window !== 'undefined' && window.innerWidth >= 768
  );

  toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }

  onNavClick(id: string): void {
    this.tabChange.emit(id);
    // On mobile the sidebar is an overlay — close after selecting
    if (typeof window !== 'undefined' && window.innerWidth < 768) {
      this.sidebarOpen.set(false);
    }
  }
}
