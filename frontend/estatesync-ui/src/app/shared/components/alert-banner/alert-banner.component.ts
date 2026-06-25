import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

export type AlertType = 'error' | 'warning' | 'success' | 'info';

const ICON_MAP: Record<AlertType, string> = {
  error:   'error_outline',
  warning: 'warning_amber',
  success: 'check_circle_outline',
  info:    'info_outline',
};

@Component({
  selector: 'app-alert-banner',
  standalone: true,
  imports: [NgClass, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './alert-banner.component.html',
  styleUrl: './alert-banner.component.css'
})
export class AlertBannerComponent {
  readonly message = input.required<string>();
  readonly type    = input<AlertType>('error');

  get icon(): string { return ICON_MAP[this.type()]; }
}
