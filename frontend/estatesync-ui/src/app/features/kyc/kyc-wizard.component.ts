import { Component, ChangeDetectionStrategy, inject, OnInit, effect } from '@angular/core';
import { PageSpinnerComponent } from '../../shared/components/page-spinner/page-spinner.component';
import { KycStore } from '../../core/stores/kyc.store';
import { FinancialStore } from '../../core/stores/financial.store';
import { IdentityWizardComponent } from './components/identity-wizard/identity-wizard.component';
import { FinancialWizardComponent } from './components/financial-wizard/financial-wizard.component';

@Component({
  selector: 'app-kyc-wizard',
  standalone: true,
  imports: [
    PageSpinnerComponent,
    IdentityWizardComponent,
    FinancialWizardComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './kyc-wizard.component.html',
  styleUrl: './kyc-wizard.component.css'
})
export class KycWizardComponent implements OnInit {
  readonly store = inject(KycStore);
  readonly finStore = inject(FinancialStore);

  constructor() {
    effect(() => {
      if (this.store.step() === 'approved' && this.finStore.step() === 'loading' && !this.finStore.profileId()) {
        this.finStore.loadExisting();
      }
    });
  }

  ngOnInit(): void {
    this.store.loadExisting();
  }
}
