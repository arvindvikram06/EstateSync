import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { PageSpinnerComponent } from '../../shared/components/page-spinner/page-spinner.component';
import { KycStore } from '../../core/stores/kyc.store';
import { IdentityWizardComponent } from './components/identity-wizard/identity-wizard.component';

@Component({
  selector: 'app-kyc-wizard',
  standalone: true,
  imports: [
    PageSpinnerComponent,
    IdentityWizardComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './kyc-wizard.component.html',
  styleUrl: './kyc-wizard.component.css'
})
export class KycWizardComponent implements OnInit {
  readonly store = inject(KycStore);

  ngOnInit(): void {
    this.store.loadExisting();
  }
}
