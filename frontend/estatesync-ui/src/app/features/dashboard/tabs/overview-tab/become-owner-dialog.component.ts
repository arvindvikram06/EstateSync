import {
  Component, ChangeDetectionStrategy, inject, signal, computed, DestroyRef
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { switchMap, finalize } from 'rxjs/operators';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRadioModule } from '@angular/material/radio';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from '@core/services/admin/user.service';
import { AuthService } from '@core/services/auth/auth.service';
import { BecomeOwnerRequest, OwnerType, CompanyType } from '../../../../core/models/auth.model';

@Component({
  selector: 'app-become-owner-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatDialogModule, MatButtonModule, MatIconModule, MatRadioModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
  template: `
    <div class="bod-shell">
      <!-- Step indicator -->
      <div class="bod-steps">
        <div class="bod-step" [class.bod-step--active]="step() === 1" [class.bod-step--done]="step() > 1">
          <div class="bod-step__circle">
            @if (step() > 1) { <mat-icon class="bod-check">check</mat-icon> } @else { 1 }
          </div>
          <span class="bod-step__label">Overview</span>
        </div>
        <div class="bod-step__line" [class.bod-step__line--done]="step() > 1"></div>
        <div class="bod-step" [class.bod-step--active]="step() === 2">
          <div class="bod-step__circle">2</div>
          <span class="bod-step__label">Your Details</span>
        </div>
      </div>

      <!-- Step 1: Feature overview -->
      @if (step() === 1) {
        <div class="bod-content">
          <div class="bod-hero">
            <div class="bod-hero__icon">
              <mat-icon style="font-size:32px;width:32px;height:32px;color:var(--mat-sys-primary)">home_work</mat-icon>
            </div>
            <h2 class="bod-hero__title">Become a Property Owner</h2>
            <p class="bod-hero__sub">Everything you need to list and manage properties — all in one place.</p>
          </div>

          <div class="bod-features">
            <div class="bod-feature">
              <div class="bod-feature__icon"><mat-icon>add_home</mat-icon></div>
              <div>
                <div class="bod-feature__title">List Properties</div>
                <div class="bod-feature__desc">Create and publish rental listings with photos and amenities</div>
              </div>
            </div>
            <div class="bod-feature">
              <div class="bod-feature__icon"><mat-icon>people</mat-icon></div>
              <div>
                <div class="bod-feature__title">Review Applications</div>
                <div class="bod-feature__desc">Browse verified tenant profiles and approve applications</div>
              </div>
            </div>
            <div class="bod-feature">
              <div class="bod-feature__icon"><mat-icon>article</mat-icon></div>
              <div>
                <div class="bod-feature__title">Digital Leases</div>
                <div class="bod-feature__desc">Generate, e-sign, and store lease agreements online</div>
              </div>
            </div>
            <div class="bod-feature">
              <div class="bod-feature__icon"><mat-icon>account_balance</mat-icon></div>
              <div>
                <div class="bod-feature__title">Track Payments</div>
                <div class="bod-feature__desc">Receive rent via Stripe and monitor settlement history</div>
              </div>
            </div>
          </div>
        </div>

        <div class="bod-actions">
          <button mat-stroked-button (click)="close()">Cancel</button>
          <button mat-flat-button color="primary" class="!rounded-full !font-semibold !px-8" (click)="goToStep2()">
            Get Started <mat-icon iconPositionEnd>arrow_forward</mat-icon>
          </button>
        </div>
      }

      <!-- Step 2: Owner form -->
      @if (step() === 2) {
        <div class="bod-content">
          <div class="bod-hero bod-hero--sm">
            <h2 class="bod-hero__title">Owner Details</h2>
            <p class="bod-hero__sub">Tell us how you'll be listing properties</p>
          </div>

          <form [formGroup]="ownerForm" class="bod-form">
            <div class="bod-label">Owner Type</div>
            <mat-radio-group formControlName="ownerType" class="bod-radio-group">
              <mat-radio-button value="Individual">
                <div class="bod-radio__inner">
                  <mat-icon>person</mat-icon>
                  <span>Individual</span>
                </div>
              </mat-radio-button>
              <mat-radio-button value="Company">
                <div class="bod-radio__inner">
                  <mat-icon>business</mat-icon>
                  <span>Company / Organisation</span>
                </div>
              </mat-radio-button>
            </mat-radio-group>

            @if (isCompany()) {
              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Company Name</mat-label>
                <mat-icon matPrefix>business</mat-icon>
                <input matInput formControlName="companyName" placeholder="e.g. Prestige Properties Pvt Ltd" />
                @if (ownerForm.get('companyName')?.invalid && ownerForm.get('companyName')?.touched) {
                  <mat-error>Company name is required</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Company Type</mat-label>
                <mat-icon matPrefix>account_balance</mat-icon>
                <mat-select formControlName="companyType">
                  @for (ct of companyTypes; track ct) {
                    <mat-option [value]="ct">{{ ct }}</mat-option>
                  }
                </mat-select>
                @if (ownerForm.get('companyType')?.invalid && ownerForm.get('companyType')?.touched) {
                  <mat-error>Company type is required</mat-error>
                }
              </mat-form-field>

              <div class="grid grid-cols-2 gap-3">
                <mat-form-field appearance="outline" class="w-full">
                  <mat-label>CIN <span style="font-weight:400;color:#aaa">(optional)</span></mat-label>
                  <mat-icon matPrefix>tag</mat-icon>
                  <input matInput formControlName="cin" placeholder="U12345MH2020PTC000000" style="text-transform:uppercase" />
                </mat-form-field>
                <mat-form-field appearance="outline" class="w-full">
                  <mat-label>GSTIN <span style="font-weight:400;color:#aaa">(optional)</span></mat-label>
                  <mat-icon matPrefix>receipt</mat-icon>
                  <input matInput formControlName="gstin" placeholder="27AAPFU0939F1ZV" style="text-transform:uppercase;letter-spacing:1px;font-family:monospace" />
                </mat-form-field>
              </div>
            }

            @if (error()) {
              <div style="font-size:12px;color:#DC2626;padding:8px 12px;background:#FEF2F2;border-radius:8px;border:1px solid #FECACA">
                {{ error() }}
              </div>
            }
          </form>
        </div>

        <div class="bod-actions">
          <button mat-stroked-button (click)="step.set(1)" [disabled]="loading()">
            <mat-icon>arrow_back</mat-icon> Back
          </button>
          <button mat-flat-button color="primary" class="!rounded-full !font-semibold !px-8"
            [disabled]="loading()" (click)="submit()">
            @if (loading()) {
              <mat-spinner diameter="16" class="inline-block !mr-2" />Activating…
            } @else {
              Activate Owner Account
            }
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .bod-shell {
      display: flex;
      flex-direction: column;
      gap: 0;
    }

    .bod-steps {
      display: flex;
      align-items: center;
      gap: 0;
      padding: 24px 28px 20px;
      border-bottom: 1px solid #F0F0F0;
    }

    .bod-step {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .bod-step__circle {
      width: 28px;
      height: 28px;
      border-radius: 50%;
      background: #E5E7EB;
      color: #6B7280;
      font-size: 13px;
      font-weight: 600;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: background 0.2s, color 0.2s;
    }

    .bod-step--active .bod-step__circle {
      background: var(--mat-sys-primary);
      color: white;
    }

    .bod-step--done .bod-step__circle {
      background: var(--mat-sys-primary);
      color: white;
    }

    .bod-check {
      font-size: 16px !important;
      width: 16px !important;
      height: 16px !important;
    }

    .bod-step__label {
      font-size: 13px;
      font-weight: 500;
      color: #6B7280;
    }

    .bod-step--active .bod-step__label,
    .bod-step--done .bod-step__label {
      color: var(--mat-sys-primary);
      font-weight: 600;
    }

    .bod-step__line {
      flex: 1;
      height: 2px;
      background: #E5E7EB;
      margin: 0 12px;
      border-radius: 1px;
      transition: background 0.2s;
    }

    .bod-step__line--done {
      background: var(--mat-sys-primary);
    }

    .bod-content {
      padding: 28px 28px 16px;
      flex: 1;
    }

    .bod-hero {
      text-align: center;
      margin-bottom: 28px;
    }

    .bod-hero--sm {
      margin-bottom: 20px;
    }

    .bod-hero__icon {
      width: 64px;
      height: 64px;
      border-radius: 16px;
      background: color-mix(in srgb, var(--mat-sys-primary) 10%, white);
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 16px;
    }

    .bod-hero__title {
      font-size: 20px;
      font-weight: 700;
      color: #222;
      margin: 0 0 6px;
    }

    .bod-hero__sub {
      font-size: 13px;
      color: #717171;
      margin: 0;
    }

    .bod-features {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    .bod-feature {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      background: #FAFAFA;
      border: 1px solid #EBEBEB;
      border-radius: 12px;
      padding: 14px;
    }

    .bod-feature__icon {
      width: 36px;
      height: 36px;
      border-radius: 8px;
      background: color-mix(in srgb, var(--mat-sys-primary) 10%, white);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      color: var(--mat-sys-primary);
    }

    .bod-feature__title {
      font-size: 13px;
      font-weight: 600;
      color: #222;
      margin-bottom: 3px;
    }

    .bod-feature__desc {
      font-size: 12px;
      color: #717171;
      line-height: 1.4;
    }

    .bod-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .bod-label {
      font-size: 12px;
      font-weight: 600;
      color: #717171;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .bod-radio-group {
      display: flex;
      gap: 12px;
    }

    .bod-radio__inner {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 14px;
    }

    .bod-actions {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 12px;
      padding: 16px 28px 24px;
      border-top: 1px solid #F0F0F0;
    }
  `]
})
export class BecomeOwnerDialogComponent {
  private readonly dialogRef  = inject(MatDialogRef<BecomeOwnerDialogComponent>);
  private readonly router     = inject(Router);
  private readonly fb         = inject(FormBuilder);
  private readonly userService  = inject(UserService);
  private readonly authService  = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly step    = signal<1 | 2>(1);
  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);

  readonly companyTypes: CompanyType[] = ['PvtLtd', 'LLP', 'Proprietorship', 'Partnership', 'Other'];

  readonly ownerForm = this.fb.nonNullable.group({
    ownerType:   this.fb.nonNullable.control<OwnerType>('Individual'),
    companyName: this.fb.nonNullable.control(''),
    companyType: this.fb.nonNullable.control<CompanyType | ''>(''),
    cin:         this.fb.nonNullable.control(''),
    gstin:       this.fb.nonNullable.control(''),
  });

  private readonly ownerTypeValue = toSignal(
    this.ownerForm.get('ownerType')!.valueChanges.pipe(startWith('Individual' as OwnerType)),
    { initialValue: 'Individual' as OwnerType }
  );
  readonly isCompany = computed(() => this.ownerTypeValue() === 'Company');

  constructor() {
    this.ownerForm.get('ownerType')!.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(type => {
        const companyName = this.ownerForm.get('companyName')!;
        const companyType = this.ownerForm.get('companyType')!;
        if (type === 'Company') {
          companyName.setValidators([Validators.required, Validators.maxLength(300)]);
          companyType.setValidators([Validators.required]);
        } else {
          companyName.clearValidators();
          companyType.clearValidators();
        }
        companyName.updateValueAndValidity();
        companyType.updateValueAndValidity();
      });
  }

  goToStep2(): void {
    this.step.set(2);
  }

  close(): void {
    this.dialogRef.close();
  }

  submit(): void {
    if (this.ownerForm.invalid) {
      this.ownerForm.markAllAsTouched();
      return;
    }

    const raw = this.ownerForm.getRawValue();
    const request: BecomeOwnerRequest = {
      ownerType:   raw.ownerType,
      companyName: raw.ownerType === 'Company' ? raw.companyName || undefined : undefined,
      companyType: raw.ownerType === 'Company' && raw.companyType ? raw.companyType as CompanyType : undefined,
      cin:         raw.ownerType === 'Company' ? raw.cin || undefined : undefined,
      gstin:       raw.ownerType === 'Company' ? raw.gstin || undefined : undefined,
    };

    this.loading.set(true);
    this.error.set(null);
    this.userService.becomeOwner(request).pipe(
      switchMap(() => this.authService.refreshToken()),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: () => {
        this.dialogRef.close('success');
        this.router.navigate(['/owner/dashboard']);
      },
      error: (err) => this.error.set(err?.error?.message ?? 'Something went wrong. Please try again.'),
    });
  }
}
