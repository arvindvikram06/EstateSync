import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef, ViewChild, ElementRef, effect
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
import { LeaseService } from '@core/services/leasing/lease.service';
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
export class LeaseSignComponent implements OnInit {
  @ViewChild('signatureCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly svc        = inject(LeaseService);
  private readonly fb         = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  private pad?: SignaturePad;
  private leaseId!: string;

  readonly lease      = signal<LeaseDetailResponse | null>(null);
  readonly loading    = signal(true);
  readonly submitting = signal(false);
  readonly padEmpty   = signal(true);
  readonly formValid  = signal(false);
  readonly error      = signal<string | null>(null);

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  readonly form = this.fb.nonNullable.group({
    signatureText: ['', [Validators.required, Validators.minLength(2)]],
  });

  constructor() {
    // Track form validity as a signal so the computed below stays reactive
    this.form.statusChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(status => {
      this.formValid.set(status === 'VALID');
    });

    // Init signature pad after lease loads and the canvas element is in the DOM
    effect(() => {
      if (!this.lease() || this.loading()) return;
      // setTimeout (macrotask) guarantees Angular CD + layout have fully completed
      setTimeout(() => {
        if (!this.canvasRef?.nativeElement || this.pad) return;
        const canvas = this.canvasRef.nativeElement;
        canvas.width  = canvas.clientWidth || canvas.offsetWidth;
        canvas.height = canvas.clientHeight || canvas.offsetHeight;
        this.pad = new SignaturePad(canvas, { penColor: '#222222' });
        const onEndStroke = () => this.padEmpty.set(this.pad!.isEmpty());
        this.pad.addEventListener('endStroke', onEndStroke);
        this.destroyRef.onDestroy(() => this.pad?.removeEventListener('endStroke', onEndStroke));
      }, 0);
    });
  }

  ngOnInit(): void {
    this.leaseId = this.route.snapshot.paramMap.get('id')!;
    this.svc.getLeaseDetail(this.leaseId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.lease.set(res.data); this.loading.set(false); },
      error: () => { this.error.set('Could not load lease.'); this.loading.set(false); }
    });
  }

  clearPad(): void {
    this.pad?.clear();
    this.padEmpty.set(true);
  }

  readonly canSubmit = computed(() => this.formValid() && !this.padEmpty());

  submit(): void {
    if (!this.canSubmit() || this.submitting() || !this.pad) return;
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
        this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to sign lease. Please try again.');
      }
    });
  }
}
