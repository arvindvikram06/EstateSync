import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { PageSpinnerComponent } from '../../../../shared/components/page-spinner/page-spinner.component';
import { FinancialStore, FinancialStep, FinancialDocumentType } from '../../../../core/stores/financial.store';

type DocSlot = { type: FinancialDocumentType; label: string; hint: string; accept: string };

const DOC_SLOTS_BY_TYPE: Record<string, DocSlot[]> = {
  Salaried: [
    { type: 'SalarySlip',       label: 'Salary Slip',       hint: 'Latest salary slip',           accept: 'image/*,application/pdf' },
    { type: 'EmploymentLetter', label: 'Employment Letter',  hint: 'Offer letter or HR letter',    accept: 'image/*,application/pdf' },
    { type: 'BankStatement',    label: 'Bank Statement',     hint: 'Last 3 months bank statement', accept: 'image/*,application/pdf' },
  ],
  SelfEmployed: [
    { type: 'IncomeTaxReturn',  label: 'Income Tax Return',  hint: 'Latest ITR filing',            accept: 'image/*,application/pdf' },
    { type: 'BankStatement',    label: 'Bank Statement',     hint: 'Last 3 months bank statement', accept: 'image/*,application/pdf' },
  ],
  BusinessOwner: [
    { type: 'IncomeTaxReturn',  label: 'Income Tax Return',  hint: 'Latest ITR filing',            accept: 'image/*,application/pdf' },
    { type: 'BankStatement',    label: 'Bank Statement',     hint: 'Last 3 months bank statement', accept: 'image/*,application/pdf' },
  ],
  Student: [
    { type: 'BankStatement',    label: 'Bank Statement',     hint: 'Last 3 months bank statement', accept: 'image/*,application/pdf' },
  ],
  Other: [
    { type: 'BankStatement',    label: 'Bank Statement',     hint: 'Last 3 months bank statement', accept: 'image/*,application/pdf' },
  ],
};

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

  readonly finStepperLabels = ['Start', 'Financial Docs', 'Income Details', 'Review'];

  readonly finDetailsForm = new FormGroup({
    employmentType: new FormControl('Salaried', { nonNullable: true, validators: [Validators.required] }),
    employerName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(250)] }),
    declaredMonthlyIncome: new FormControl<number>(0, { nonNullable: true, validators: [Validators.required, Validators.min(1)] })
  });

  private readonly employmentTypeValue = toSignal(
    this.finDetailsForm.controls.employmentType.valueChanges,
    { initialValue: 'Salaried' }
  );

  readonly requiredDocSlots = computed(() =>
    DOC_SLOTS_BY_TYPE[this.employmentTypeValue()] ?? DOC_SLOTS_BY_TYPE['Other']
  );

  readonly allFinDocsUploaded = computed(() =>
    this.requiredDocSlots().every(slot => !!this.finStore.uploadedDocs()[slot.type])
  );

  readonly uploadedFinDocCount = computed(() =>
    this.requiredDocSlots().filter(slot => !!this.finStore.uploadedDocs()[slot.type]).length
  );

  startFin(): void {
    this.finDetailsForm.reset({ employmentType: 'Salaried' });
    this.finStore.startProfile();
  }

  onFinFileSelected(event: Event, docType: FinancialDocumentType): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.finStore.uploadDocument({ file, docType });
  }

  proceedToFinDetails(): void {
    if (!this.allFinDocsUploaded()) return;
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
      declaredMonthlyIncome: val.declaredMonthlyIncome
    });
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  getFinStepIndex(s: FinancialStep): number {
    const map: Record<string, number> = { intro: 1, documents: 2, details: 3, done: 4 };
    return map[s] ?? 0;
  }
}
