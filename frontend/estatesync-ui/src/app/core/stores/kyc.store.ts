import { inject } from '@angular/core';
import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { KycService } from '@core/services/verification/kyc.service';
import { KycVerificationResponse, DocumentResponse, KycDocumentType, REQUIRED_KYC_DOCS } from '../models/kyc.model';
import { catchError, map, of, tap, pipe, switchMap, exhaustMap, concatMap } from 'rxjs';
import { rxMethod } from '@ngrx/signals/rxjs-interop';

export type KycStep = 'loading' | 'intro' | 'documents' | 'details' | 'done' | 'pending' | 'approved' | 'rejected';

interface KycState {
  step: KycStep;
  kycId: string | null;
  kyc: KycVerificationResponse | null;
  uploadedDocs: Record<string, DocumentResponse>;
  uploadingDoc: KycDocumentType | null;
  isSubmitting: boolean;
}

const initialState: KycState = {
  step: 'loading',
  kycId: null,
  kyc: null,
  uploadedDocs: {},
  uploadingDoc: null,
  isSubmitting: false,
};

export const KycStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, kycService = inject(KycService)) => ({
    
    setStep(step: KycStep) {
      patchState(store, { step });
    },

    loadExisting: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { step: 'loading' })),
        switchMap(() => kycService.getMyKyc().pipe(
          tap((res) => {
            const list = res.data ?? [];
            if (list.length === 0) {
              patchState(store, { step: 'intro' });
              return;
            }
            const latest = list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];
            
            const docs: Record<string, DocumentResponse> = {};
            latest.documents.forEach((d: DocumentResponse) => { docs[d.documentType] = d; });
            
            let step: KycStep = 'documents';
            const allUploaded = REQUIRED_KYC_DOCS.every(type => !!docs[type]);
            
            switch (latest.status) {
              case 'Draft': step = allUploaded ? 'details' : 'documents'; break;
              case 'Pending': step = 'pending'; break;
              case 'Approved': step = 'approved'; break;
              case 'Rejected': step = 'rejected'; break;
            }

            patchState(store, { kyc: latest, kycId: latest.kycVerificationId, uploadedDocs: docs, step });
          }),
          catchError(() => {
            patchState(store, { step: 'intro' });
            return of(null);
          })
        ))
      )
    ),

    startKyc: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { step: 'loading', kycId: null, kyc: null, uploadedDocs: {} })),
        exhaustMap(() => kycService.createDraft().pipe(
          tap((res) => patchState(store, { kycId: res.data.kycVerificationId, step: 'documents' })),
          catchError(() => {
            patchState(store, { step: 'intro' });
            return of(null);
          })
        ))
      )
    ),

    uploadDocument: rxMethod<{file: File, docType: KycDocumentType}>(
      pipe(
        tap(({docType}) => patchState(store, { uploadingDoc: docType })),
        concatMap(({file, docType}) => kycService.uploadDocument(store.kycId()!, file, docType).pipe(
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

    submitKyc: rxMethod<{aadhaarNumber: string, panNumber: string}>(
      pipe(
        tap(() => patchState(store, { isSubmitting: true })),
        exhaustMap((details) => kycService.submit(store.kycId()!, details).pipe(
          tap(() => patchState(store, {
        isSubmitting: false,
        step: 'done',
        kyc: store.kyc() ? { ...store.kyc()!, status: 'Pending' } : null,
      })),
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
