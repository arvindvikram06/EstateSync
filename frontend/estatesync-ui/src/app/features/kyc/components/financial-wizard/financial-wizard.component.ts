import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { PageSpinnerComponent } from '../../../../shared/components/page-spinner/page-spinner.component';
import { FinancialStore, FinancialStep } from '../../../../core/stores/financial.store';

const FIN_DOC_SLOTS = [
  { type: 'SalarySlip', label: 'Salary Slip', hint: 'Latest salary slip or income proof', accept: 'image/*,application/pdf' },
  { type: 'BankStatement', label: 'Bank Statement', hint: 'Last 3 months bank statement', accept: 'image/*,application/pdf' }
];

@Component({
  selector: 'app-financial-wizard',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule,
    StatusBadgeComponent, PageSpinnerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './financial-wizard.component.html',
  styleUrl: '../../kyc-wizard.component.css'
})
export class FinancialWizardComponent {
  readonly finStore = inject(FinancialStore);
  private readonly router = inject(Router);

  readonly FIN_DOC_SLOTS = FIN_DOC_SLOTS;
  readonly finStepperLabels = ['Start', 'Financial Docs', 'Income Details', 'Review'];

  readonly finDetailsForm = new FormGroup({
    employmentType: new FormControl('Salaried', { nonNullable: true, validators: [Validators.required] }),
    employerName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(250)] }),
    declaredMonthlyIncome: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(1)] })
  });

  get allFinDocsUploaded(): boolean {
    const docs = this.finStore.uploadedDocs();
    return FIN_DOC_SLOTS.every(slot => !!docs[slot.type]);
  }

  get uploadedFinDocCount(): number {
    return Object.keys(this.finStore.uploadedDocs()).filter(k => !!this.finStore.uploadedDocs()[k]).length;
  }

  startFin(): void {
    this.finDetailsForm.reset({ employmentType: 'Salaried' });
    this.finStore.startProfile();
  }

  onFinFileSelected(event: Event, docType: string): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.finStore.uploadDocument({ file, docType });
  }

  proceedToFinDetails(): void {
    if (!this.allFinDocsUploaded) return;
    this.finStore.setStep('details');
  }

  submitFin(): void {
    if (this.finDetailsForm.invalid) {
      this.finDetailsForm.markAllAsTouched();
      return;
    }
    const val = this.finDetailsForm.getRawValue();
    this.finStore.submitProfile({
      employmentType: val.employmentType,
      employerName: val.employerName,
      declaredMonthlyIncome: val.declaredMonthlyIncome ?? 0
    });
  }

  triggerFileInput(docType: string): void {
    (document.getElementById('file-' + docType) as HTMLInputElement)?.click();
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  getFinStepIndex(s: FinancialStep): number {
    const map: Record<string, number> = { intro: 1, documents: 2, details: 3, done: 4 };
    return map[s] ?? 0;
  }
}
