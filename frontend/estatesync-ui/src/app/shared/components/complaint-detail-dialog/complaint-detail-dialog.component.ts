import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ComplaintService } from '@core/services/complaint/complaint.service';
import { ComplaintResponse } from '../../../core/models/complaint.model';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import { formatDate } from '../../../core/utils/format.utils';

export interface ComplaintDetailDialogData {
  complaint: ComplaintResponse;
}

@Component({
  selector: 'app-complaint-detail-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7 min-w-[440px] max-w-[560px]">

      <!-- Header -->
      <div class="flex items-start justify-between gap-3 mb-1">
        <h2 class="text-[18px] font-bold text-es-text leading-snug">{{ c.title }}</h2>
        <div class="flex items-center gap-1.5 shrink-0">
          <app-status-badge [status]="c.status" />
          <app-status-badge [status]="c.priority" />
        </div>
      </div>

      <!-- Meta row -->
      <div class="flex items-center gap-2 flex-wrap mb-5 es-text-xs">
        @if (c.propertyTitle) {
          <span class="flex items-center gap-1">
            <mat-icon class="!text-[14px] !w-[14px] !h-[14px]">home</mat-icon>
            {{ c.propertyTitle }}
          </span>
          <span class="text-es-faint">·</span>
        }
        <span class="es-chip-primary text-[11px] px-2.5 py-[3px] rounded-[20px] font-semibold">{{ c.complaintType }}</span>
        <span class="text-es-faint">·</span>
        <span>Raised {{ formatDate(c.createdAt) }}</span>
        @if (c.updatedAt && c.updatedAt !== c.createdAt) {
          <span class="text-es-faint">·</span>
          <span>Updated {{ formatDate(c.updatedAt) }}</span>
        }
      </div>

      <!-- Full description -->
      <div class="es-tile mb-4">
        <div class="es-label-upper mb-1.5">Issue</div>
        <p class="text-[13px] text-es-soft leading-[1.7] whitespace-pre-wrap">{{ c.description }}</p>
      </div>

      <!-- Review remarks -->
      @if (c.reviewRemarks) {
        <div class="es-info-banner es-info-banner--info mb-4">
          <mat-icon class="!text-[16px] !w-[16px] !h-[16px] mt-px shrink-0">forum</mat-icon>
          <div>
            <div class="text-[12px] font-semibold mb-0.5">Remarks</div>
            <div class="text-[13px]">{{ c.reviewRemarks }}</div>
          </div>
        </div>
      }

      <!-- Photos -->
      @if (c.images.length) {
        <div class="mb-2">
          <div class="es-label-upper mb-2">Photos ({{ c.images.length }})</div>
          <div class="grid grid-cols-3 gap-2">
            @for (img of c.images; track img.complaintImageId) {
              <img [src]="svc.fileUrl(img.imageUrl)" alt="Complaint photo" loading="lazy"
                class="w-full aspect-square object-cover rounded-xl border border-solid border-es-border-light cursor-pointer hover:opacity-90 transition-opacity"
                (click)="openImage(img.imageUrl)" />
            }
          </div>
        </div>
      }

      <div class="flex justify-end mt-5">
        <button mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>Close</button>
      </div>
    </div>
  `
})
export class ComplaintDetailDialogComponent {
  private readonly data: ComplaintDetailDialogData = inject(MAT_DIALOG_DATA);
  readonly svc = inject(ComplaintService);

  readonly c = this.data.complaint;
  readonly formatDate = formatDate;

  openImage(url: string): void {
    window.open(this.svc.fileUrl(url), '_blank');
  }
}
