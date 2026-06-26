import { Component, ChangeDetectionStrategy, inject, input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { KycStore } from '../../../../core/stores/kyc.store';
import { FinancialStore } from '../../../../core/stores/financial.store';

@Component({
  selector: 'app-verifications-tab',
  standalone: true,
  imports: [RouterModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './verifications-tab.component.html'
})
export class VerificationsTabComponent {
  readonly kycStore  = inject(KycStore);
  readonly finStore  = inject(FinancialStore);
}
