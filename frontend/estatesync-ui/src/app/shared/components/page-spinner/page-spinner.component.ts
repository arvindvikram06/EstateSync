import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-page-spinner',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './page-spinner.component.html',
  styleUrl: './page-spinner.component.css'
})
export class PageSpinnerComponent {
  readonly diameter = input<number>(48);
}
