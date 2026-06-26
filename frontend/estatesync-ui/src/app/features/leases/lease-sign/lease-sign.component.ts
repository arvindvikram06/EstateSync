import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, AfterViewInit, DestroyRef, ViewChild, ElementRef
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import SignaturePad from 'signature_pad';
import { LeaseService } from '../../../core/services/lease.service';
import { LeaseDetailResponse } from '../../../core/models/lease.model';
import { LoadingButtonComponent } from '../../../shared/components/loading-button/loading-button.component';
import { formatDate, formatCurrency } from '../../../core/utils/format.utils';

@Component({
  selector: 'app-lease-sign',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './lease-sign.component.html',
  styleUrl: './lease-sign.component.css'
})
export class LeaseSignComponent implements OnInit, AfterViewInit {
  @ViewChild('signatureCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly svc        = inject(LeaseService);
  private readonly fb         = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  private pad!: SignaturePad;
  private leaseId!: string;

  readonly lease      = signal<LeaseDetailResponse | null>(null);
  readonly loading    = signal(true);
  readonly submitting = signal(false);
  readonly padEmpty   = signal(true);
  readonly error      = signal<string | null>(null);

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  readonly form = this.fb.nonNullable.group({
    signatureText: ['', [Validators.required, Validators.minLength(2)]],
  });

  ngOnInit(): void {
    this.leaseId = this.route.snapshot.paramMap.get('id')!;
    this.svc.getLeaseDetail(this.leaseId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.lease.set(res.data); this.loading.set(false); },
      error: () => { this.error.set('Could not load lease.'); this.loading.set(false); }
    });
  }

  ngAfterViewInit(): void {
    // Wait until lease is loaded before initializing pad (canvas may not be in DOM yet)
    const initPad = () => {
      if (!this.canvasRef) { setTimeout(initPad, 100); return; }
      const canvas = this.canvasRef.nativeElement;
      canvas.width  = canvas.clientWidth;
      canvas.height = canvas.clientHeight;
      this.pad = new SignaturePad(canvas, { penColor: '#222222' });
      this.pad.addEventListener('endStroke', () => this.padEmpty.set(this.pad.isEmpty()));
    };
    setTimeout(initPad, 300);
  }

  clearPad(): void {
    this.pad?.clear();
    this.padEmpty.set(true);
  }

  get canSubmit(): boolean {
    return this.form.valid && !this.padEmpty();
  }

  submit(): void {
    if (!this.canSubmit || this.submitting()) return;
    const dataUrl = this.pad.toDataURL('image/png');
    const base64  = dataUrl.replace('data:image/png;base64,', '');
    this.submitting.set(true);
    this.error.set(null);
    this.svc.signLease(this.leaseId, {
      signatureText: this.form.getRawValue().signatureText,
      signatureImageBase64: base64,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/leases', this.leaseId]);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error?.message ?? 'Failed to sign lease. Please try again.');
      }
    });
  }
}
