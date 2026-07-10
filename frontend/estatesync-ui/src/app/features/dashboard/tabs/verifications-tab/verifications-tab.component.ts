import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { KycStore } from '../../../../core/stores/kyc.store';

@Component({
  selector: 'app-verifications-tab',
  standalone: true,
  imports: [RouterModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './verifications-tab.component.html'
})
export class VerificationsTabComponent {
  readonly kycStore = inject(KycStore);
}
