import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NgTemplateOutlet } from '@angular/common';

@Component({
  selector: 'app-loading-button',
  standalone: true,
  imports: [MatButtonModule, MatProgressSpinnerModule, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './loading-button.component.html',
  styleUrl: './loading-button.component.css'
})
export class LoadingButtonComponent {
  readonly loading  = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly type     = input<'button' | 'submit' | 'reset'>('button');
  readonly color    = input<'primary' | 'accent' | 'warn'>('primary');
  readonly variant  = input<'raised' | 'stroked' | 'flat'>('raised');
}
