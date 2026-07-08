import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { PropertyService } from '@core/services/property/property.service';
import { PropertyImageResponse } from '../../../core/models/property.model';

@Component({
  selector: 'app-image-gallery-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative bg-black/80 backdrop-blur-md w-[100vw] h-[100vh] flex flex-col items-center justify-center overflow-hidden" style="border-radius: 0;">
      
      <!-- Main Image -->
      <div class="relative flex items-center justify-center w-full h-full p-12" style="z-index: 10;">
        <img 
          [src]="propSvc.imageUrl(data.images[currentIndex()].image)" 
          alt="Property Image" 
          class="max-w-full max-h-full object-contain transition-opacity duration-300 pointer-events-auto"
        />
      </div>

      <!-- Close Button -->
      <button mat-icon-button mat-dialog-close class="!absolute top-4 right-4 !z-[100] !bg-white/10 !text-white hover:!bg-white/30 !w-12 !h-12 flex items-center justify-center pointer-events-auto">
        <mat-icon class="!text-[24px] !w-[24px] !h-[24px]">close</mat-icon>
      </button>

      <!-- Counter -->
      <div class="absolute top-6 left-6 text-white text-sm font-medium tracking-wider !z-[100] pointer-events-none">
        {{ currentIndex() + 1 }} / {{ data.images.length }}
      </div>

      <!-- Navigation Prev -->
      @if (data.images.length > 1) {
        <button 
          mat-icon-button 
          (click)="prev()" 
          class="!absolute left-4 top-1/2 -translate-y-1/2 !z-[100] !bg-white/10 !text-white hover:!bg-white/30 !w-12 !h-12 flex items-center justify-center pointer-events-auto">
          <mat-icon class="!text-[28px] !w-[28px] !h-[28px]">chevron_left</mat-icon>
        </button>

        <!-- Navigation Next -->
        <button 
          mat-icon-button 
          (click)="next()" 
          class="!absolute right-4 top-1/2 -translate-y-1/2 !z-[100] !bg-white/10 !text-white hover:!bg-white/30 !w-12 !h-12 flex items-center justify-center pointer-events-auto">
          <mat-icon class="!text-[28px] !w-[28px] !h-[28px]">chevron_right</mat-icon>
        </button>
      }
    </div>
  `
})
export class ImageGalleryDialogComponent {
  readonly data = inject(MAT_DIALOG_DATA) as { images: PropertyImageResponse[] };
  readonly dialogRef = inject(MatDialogRef<ImageGalleryDialogComponent>);
  readonly propSvc = inject(PropertyService);
  
  readonly currentIndex = signal(0);

  next(): void {
    if (this.currentIndex() < this.data.images.length - 1) {
      this.currentIndex.update(i => i + 1);
    } else {
      this.currentIndex.set(0); // Wrap around
    }
  }

  prev(): void {
    if (this.currentIndex() > 0) {
      this.currentIndex.update(i => i - 1);
    } else {
      this.currentIndex.set(this.data.images.length - 1); // Wrap around
    }
  }
}
