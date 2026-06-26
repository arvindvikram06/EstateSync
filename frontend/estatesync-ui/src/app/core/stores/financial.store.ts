import { inject } from '@angular/core';
import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { catchError, of, tap, pipe, switchMap, exhaustMap, concatMap } from 'rxjs';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { FinancialProfileService } from '../services/financial-profile.service';
import { TenantFinancialProfileResponse } from '../models/financial-profile.model';

export const REQUIRED_FINANCIAL_DOCS = ['SalarySlip', 'BankStatement'] as const;
export type FinancialDocumentType = typeof REQUIRED_FINANCIAL_DOCS[number];

export type FinancialStep = 'loading' | 'intro' | 'documents' | 'details' | 'done' | 'pending' | 'approved' | 'rejected';

interface FinancialState {
  step: FinancialStep;
  profileId: string | null;
  profile: TenantFinancialProfileResponse | null;
  uploadedDocs: Record<string, any>;
  uploadingDoc: string | null;
  isSubmitting: boolean;
}

const initialState: FinancialState = {
  step: 'loading',
  profileId: null,
  profile: null,
  uploadedDocs: {},
  uploadingDoc: null,
  isSubmitting: false,
};

export const FinancialStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, financialService = inject(FinancialProfileService)) => ({
    
    setStep(step: FinancialStep) {
      patchState(store, { step });
    },

    loadExisting: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { step: 'loading' })),
        switchMap(() => financialService.getMyProfile().pipe(
          tap((res) => {
            const list = res.data ?? [];
            if (list.length === 0) {
              patchState(store, { step: 'intro' });
              return;
            }
            const latest = list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];
            
            const docs: Record<string, any> = {};
            latest.documents?.forEach((d: any) => { docs[d.documentType] = d; });
            
            let step: FinancialStep = 'documents';
            // Need SalarySlip and BankStatement as mandatory, for example
            const allUploaded = REQUIRED_FINANCIAL_DOCS.every(type => !!docs[type]);
            
            switch (latest.status) {
              case 'Draft': step = allUploaded ? 'details' : 'documents'; break;
              case 'Pending': step = 'pending'; break;
              case 'Approved': step = 'approved'; break;
              case 'Rejected': step = 'rejected'; break;
            }

            patchState(store, { profile: latest, profileId: latest.profileId, uploadedDocs: docs, step });
          }),
          catchError(() => {
            patchState(store, { step: 'intro' });
            return of(null);
          })
        ))
      )
    ),

    startProfile: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { step: 'loading', profileId: null, profile: null, uploadedDocs: {} })),
        exhaustMap(() => financialService.createDraft().pipe(
          tap((res) => patchState(store, { profileId: res.data.profileId, step: 'documents' })),
          catchError(() => {
            patchState(store, { step: 'intro' });
            return of(null);
          })
        ))
      )
    ),

    uploadDocument: rxMethod<{file: File, docType: string}>(
      pipe(
        tap(({docType}) => patchState(store, { uploadingDoc: docType })),
        concatMap(({file, docType}) => financialService.uploadDocument(store.profileId()!, file, docType).pipe(
          tap((res) => patchState(store, (state) => ({
            uploadedDocs: { ...state.uploadedDocs, [docType]: res.data },
            uploadingDoc: null
          }))),
          catchError(() => {
            patchState(store, { uploadingDoc: null });
            return of(null);
          })
        ))
      )
    ),

    submitProfile: rxMethod<{employmentType: string, employerName: string, declaredMonthlyIncome: number}>(
      pipe(
        tap(() => patchState(store, { isSubmitting: true })),
        exhaustMap((details) => financialService.submit(store.profileId()!, details).pipe(
          tap(() => patchState(store, { isSubmitting: false, step: 'done' })),
          catchError(() => {
            patchState(store, { isSubmitting: false });
            return of(null);
          })
        ))
      )
    ),

    reset() {
      patchState(store, initialState);
    }

  }))
);
