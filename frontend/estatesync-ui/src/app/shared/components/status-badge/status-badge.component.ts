import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';

const STATUS_CLASS: Record<string, string> = {
  pending:              'es-badge--pending',
  pendingapproval:      'es-badge--pending',
  pendingadminapproval: 'es-badge--pending',
  inprogress:           'es-badge--inprogress',
  underreview:          'es-badge--pending',
  active:               'es-badge--active',
  approved:             'es-badge--approved',
  paid:                 'es-badge--approved',
  resolved:             'es-badge--resolved',
  verified:             'es-badge--approved',
  rejected:             'es-badge--rejected',
  voided:               'es-badge--rejected',
  failed:               'es-badge--failed',
  partialsigned:        'es-badge--partial',
  open:                 'es-badge--open',
  draft:                'es-badge--draft',
  expired:              'es-badge--draft',
  terminated:           'es-badge--draft',
  withdrawn:            'es-badge--draft',
  closed:               'es-badge--closed',
  notstarted:           'es-badge--draft',
  availablesoon:        'es-badge--partial',
  leased:               'es-badge--partial',
  low:                  'es-badge--approved',
  medium:               'es-badge--pending',
  high:                 'es-badge--inprogress',
  critical:             'es-badge--rejected',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [MatChipsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.css'
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();

  get badgeClass(): string {
    const key = (this.status() ?? '').toLowerCase().replace(/\s+/g, '');
    return STATUS_CLASS[key] ?? 'es-badge--default';
  }
}
