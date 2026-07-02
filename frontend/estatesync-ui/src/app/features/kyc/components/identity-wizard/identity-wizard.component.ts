import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { PageSpinnerComponent } from '../../../../shared/components/page-spinner/page-spinner.component';
import { KycDocumentType } from '../../../../core/models/kyc.model';
import { KycStore, KycStep } from '../../../../core/stores/kyc.store';

interface DocSlot {
  type: KycDocumentType;
  label: string;
  hint: string;
  accept: string;
}

const DOC_SLOTS: DocSlot[] = [
  { type: 'AadhaarFront', label: 'Aadhaar Card — Front', hint: 'Clear photo/scan of the front side', accept: 'image/*,application/pdf' },
  { type: 'AadhaarBack',  label: 'Aadhaar Card — Back',  hint: 'Clear photo/scan of the back side',  accept: 'image/*,application/pdf' },
  { type: 'PanCard',      label: 'PAN Card',              hint: 'Clear photo/scan of your PAN card',  accept: 'image/*,application/pdf' },
  { type: 'AddressProof', label: 'Address Proof',         hint: 'Utility bill, bank statement or passport (last 3 months)', accept: 'image/*,application/pdf' }
];

@Component({
  selector: 'app-identity-wizard',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule,
    StatusBadgeComponent, PageSpinnerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './identity-wizard.component.html',
  styleUrl: '../../kyc-wizard.component.css'
})
export class IdentityWizardComponent {
  readonly store = inject(KycStore);
  private readonly router = inject(Router);

  readonly DOC_SLOTS = DOC_SLOTS;
  readonly stepperLabels = ['Start', 'Documents', 'Details', 'Review'];

  readonly detailsForm = new FormGroup({
    aadhaarNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^\d{12}$/)]
    }),
    panNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Z]{5}[0-9]{4}[A-Z]{1}$/)]
    })
  });

  get aadhaarCtrl() { return this.detailsForm.controls.aadhaarNumber; }
  get panCtrl()     { return this.detailsForm.controls.panNumber; }

  readonly allDocsUploaded = computed(() =>
    DOC_SLOTS.every(slot => !!this.store.uploadedDocs()[slot.type])
  );

  readonly uploadedDocCount = computed(() =>
    DOC_SLOTS.filter(slot => !!this.store.uploadedDocs()[slot.type]).length
  );

  startKyc(): void {
    this.detailsForm.reset();
    this.store.startKyc();
  }

  onFileSelected(event: Event, docType: KycDocumentType): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.store.uploadDocument({ file, docType });
  }

  proceedToDetails(): void {
    if (!this.allDocsUploaded()) return;
    this.store.setStep('details');
  }

  submitKyc(): void {
    if (this.detailsForm.invalid) {
      this.detailsForm.markAllAsTouched();
      return;
    }
    this.store.submitKyc(this.detailsForm.getRawValue());
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  getStepIndex(s: KycStep): number {
    const map: Record<string, number> = { intro: 1, documents: 2, details: 3, done: 4 };
    return map[s] ?? 0;
  }
}
