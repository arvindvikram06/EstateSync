import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { NgFor } from '@angular/common';

@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  imports: [NgFor],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (type === 'table') {
      <div class="sk-table-wrap">
        @for (r of rowArr; track $index) {
          <div class="sk-row animate-pulse">
            <div class="sk-cell" style="width:28%"><div class="sk-block" style="height:14px"></div></div>
            <div class="sk-cell" style="width:18%"><div class="sk-block" style="height:14px"></div></div>
            <div class="sk-cell" style="width:14%"><div class="sk-block sk-badge"></div></div>
            <div class="sk-cell" style="width:12%"><div class="sk-block" style="height:14px"></div></div>
            <div class="sk-cell" style="width:18%"><div class="sk-block" style="height:14px;width:70%"></div></div>
            <div class="sk-cell sk-cell--right" style="width:10%"><div class="sk-block sk-btn"></div></div>
          </div>
        }
      </div>
    } @else {
      <div class="flex flex-col gap-3">
        @for (r of rowArr; track $index) {
          <div class="sk-card animate-pulse">
            <div class="sk-avatar"></div>
            <div class="flex-1 flex flex-col gap-2">
              <div class="sk-block" style="height:14px;width:55%"></div>
              <div class="sk-block" style="height:12px;width:35%"></div>
            </div>
            <div class="sk-block sk-badge"></div>
          </div>
        }
      </div>
    }
  `,
  styles: [`
    .sk-table-wrap {
      background: white;
      border: 1px solid #EBEBEB;
      border-radius: 16px;
      overflow: hidden;
      box-shadow: 0 2px 12px rgba(0,0,0,0.06);
    }
    .sk-row {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 16px;
      border-bottom: 1px solid #F3F4F6;
    }
    .sk-row:last-child { border-bottom: none; }
    .sk-cell { padding: 0 4px; }
    .sk-cell--right { display: flex; justify-content: flex-end; }
    .sk-block {
      background: #E5E7EB;
      border-radius: 6px;
      width: 100%;
    }
    .sk-badge { height: 22px; width: 64px; border-radius: 20px; }
    .sk-btn  { height: 28px; width: 64px; border-radius: 20px; }

    .sk-card {
      background: white;
      border: 1px solid #EBEBEB;
      border-radius: 12px;
      padding: 18px 20px;
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .sk-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #E5E7EB;
      flex-shrink: 0;
    }
  `],
})
export class SkeletonLoaderComponent {
  @Input() type: 'table' | 'card' = 'card';
  @Input() rows = 5;

  get rowArr(): number[] {
    return Array.from({ length: this.rows });
  }
}
