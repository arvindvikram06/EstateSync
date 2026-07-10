import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { SlicePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { forkJoin } from 'rxjs';
import * as L from 'leaflet';
import { AdminService } from '@core/services/admin/admin.service';
import { PropertyService } from '@core/services/property/property.service';
import { PropertyDetailResponse, DocumentResponse } from '../../../core/models/property.model';
import { KycVerificationResponse, KycDocumentSummary } from '../../../core/models/admin.model';

@Component({
  selector: 'app-doc-preview-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div style="display:flex;align-items:center;justify-content:space-between;padding:16px 20px 12px">
      <div>
        <div class="text-[15px] font-bold text-es-text">{{ data.label }}</div>
        <div style="font-size:12px;color:#AAAAAA;margin-top:2px">{{ data.fileName }}</div>
      </div>
      <div style="display:flex;gap:8px;align-items:center">
        <a [href]="data.url" target="_blank" rel="noopener" mat-stroked-button class="!rounded-full !text-xs !h-8"
          style="display:inline-flex;align-items:center;gap:4px;text-decoration:none;color:#6750A4;border-color:#6750A4">
          <mat-icon class="!text-[14px] !w-[14px] !h-[14px]">open_in_new</mat-icon>Open full
        </a>
        <button mat-icon-button mat-dialog-close>
          <mat-icon style="font-size:20px;width:20px;height:20px;color:#717171">close</mat-icon>
        </button>
      </div>
    </div>
    <mat-dialog-content style="padding:0 20px 20px;margin:0">
      @if (data.isImage) {
        <img [src]="data.url" [alt]="data.label"
          style="width:100%;max-height:72vh;object-fit:contain;border-radius:8px;border:1px solid #EBEBEB;display:block" />
      } @else {
        <iframe [src]="data.safeUrl" style="width:100%;height:72vh;border:1px solid #EBEBEB;border-radius:8px;display:block" title="Document preview"></iframe>
      }
    </mat-dialog-content>
  `,
})
export class DocPreviewDialogComponent {
  readonly data = inject<{ url: string; safeUrl: SafeResourceUrl; label: string; fileName: string; isImage: boolean }>(MAT_DIALOG_DATA);
}

const DOC_LABELS: Record<string, string> = {
  AadhaarFront:           'Aadhaar Card (Front)',
  AadhaarBack:            'Aadhaar Card (Back)',
  PanCard:                'PAN Card',
  AddressProof:           'Address Proof',
  PropertyDeed:           'Property Deed',
  PropertyTaxReceipt:     'Property Tax Receipt',
  OwnershipCertificate:   'Ownership Certificate',
};

const KYC_DOC_ORDER = ['AadhaarFront', 'AadhaarBack', 'PanCard', 'AddressProof'];

export interface AdminReviewDialogData {
  title: string;
  subtitle: string;
  entityId: string;
  type: 'kyc' | 'property' | 'lease';
  readOnly?: boolean;
  currentStatus?: string;
  currentRemarks?: string;
  kycItem?: KycVerificationResponse;
}

@Component({
  selector: 'app-admin-review-dialog',
  standalone: true,
  imports: [
    SlicePipe, ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatProgressSpinnerModule, MatIconModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div mat-dialog-title class="text-[18px] font-bold text-es-text">{{ data.title }}</div>

    <mat-dialog-content
      [style.minWidth]="data.type === 'property' ? '780px' : (data.kycItem ? '580px' : '400px')"
      style="padding-top:16px;max-width:800px">

      <div class="flex flex-col gap-4">

        <!-- ── Property full detail ─────────────────────────────── -->
        @if (data.type === 'property') {
          @if (propertyLoading()) {
            <div class="flex items-center gap-3 py-8 text-sm text-es-muted">
              <mat-spinner diameter="24" /><span>Loading property details…</span>
            </div>
          } @else if (property()) {
            <!-- Image gallery -->
            @if (property()!.images.length > 0) {
              <div>
                <img [src]="activeImage()" alt="{{ property()!.title }}"
                  style="width:100%;height:280px;object-fit:cover;border-radius:12px;border:1px solid #EBEBEB;display:block" />
                @if (property()!.images.length > 1) {
                  <div class="flex gap-2 mt-2 overflow-x-auto pb-1">
                    @for (img of property()!.images; track img.imageId) {
                      <button type="button" (click)="activeImage.set(imgSrc(img.image))"
                        style="flex-shrink:0;padding:0;border:2px solid transparent;border-radius:8px;background:none;cursor:pointer;transition:border-color .15s"
                        [style.borderColor]="activeImage() === imgSrc(img.image) ? 'var(--mat-sys-primary)' : '#EBEBEB'">
                        <img [src]="imgSrc(img.image)" alt=""
                          style="width:64px;height:48px;object-fit:cover;border-radius:6px;display:block" />
                      </button>
                    }
                  </div>
                }
              </div>
            }

            <!-- Owner + basic info -->
            <div class="es-tile">
              <div class="es-label-upper mb-3">Owner Details</div>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px">
                <div>
                  <div class="es-text-hint mb-0.5">Owner Name</div>
                  <div class="es-text-strong">{{ property()!.ownerName }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">KYC Verified</div>
                  <div class="es-text-strong es-text-success">Must be verified to list</div>
                </div>
              </div>
            </div>

            <!-- Property specs -->
            <div class="es-tile">
              <div class="es-label-upper mb-3">Property Details</div>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px">
                <div>
                  <div class="es-text-hint mb-0.5">Title</div>
                  <div class="es-text-strong">{{ property()!.title }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">Type</div>
                  <div class="es-text-strong">{{ property()!.propertyType }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">City</div>
                  <div class="es-text-body">{{ property()!.city }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">State</div>
                  <div class="es-text-body">{{ property()!.state }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">Pincode</div>
                  <div style="font-size:13px;color:#444;font-family:'Roboto Mono',monospace;letter-spacing:1px">{{ property()!.pincode }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">Address</div>
                  <div class="es-text-body">{{ property()!.addressLine }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">Bedrooms</div>
                  <div class="es-text-strong">{{ property()!.bedrooms }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">Bathrooms</div>
                  <div class="es-text-strong">{{ property()!.bathrooms }}</div>
                </div>
                @if (property()!.landArea) {
                  <div>
                    <div class="es-text-hint mb-0.5">Land Area</div>
                    <div class="es-text-body">{{ property()!.landArea }} sq.ft</div>
                  </div>
                }
                @if (property()!.buildupArea) {
                  <div>
                    <div class="es-text-hint mb-0.5">Buildup Area</div>
                    <div class="es-text-body">{{ property()!.buildupArea }} sq.ft</div>
                  </div>
                }
                <div>
                  <div class="es-text-hint mb-0.5">Monthly Rent</div>
                  <div style="font-size:15px;font-weight:700;color:var(--mat-sys-primary)">₹{{ property()!.monthlyRent.toLocaleString('en-IN') }}</div>
                </div>
                <div>
                  <div class="es-text-hint mb-0.5">Security Deposit</div>
                  <div class="es-detail-value">₹{{ property()!.securityDeposit.toLocaleString('en-IN') }}</div>
                </div>
              </div>
            </div>

            <!-- Map -->
            <div style="background:#F7F7F7;border:1px solid #EBEBEB;border-radius:12px;overflow:hidden">
              <div style="font-size:11px;font-weight:600;color:#717171;text-transform:uppercase;letter-spacing:.5px;padding:14px 16px 10px">
                Location Map{{ !property()!.latitude ? ' — coordinates not set' : '' }}
              </div>
              <div #mapEl style="height:220px;width:100%"></div>
            </div>

            <!-- Description -->
            @if (property()!.description) {
              <div class="es-tile">
                <div style="font-size:11px;font-weight:600;color:#717171;text-transform:uppercase;letter-spacing:.5px;margin-bottom:8px">Description</div>
                <p style="font-size:13px;color:#444;line-height:1.65;margin:0;white-space:pre-wrap">{{ property()!.description }}</p>
              </div>
            }

            <!-- Amenities -->
            @if (property()!.amenities.length > 0) {
              <div class="es-tile">
                <div style="font-size:11px;font-weight:600;color:#717171;text-transform:uppercase;letter-spacing:.5px;margin-bottom:10px">Amenities ({{ property()!.amenities.length }})</div>
                <div class="flex flex-wrap gap-2">
                  @for (a of property()!.amenities; track a) {
                    <span style="display:inline-flex;align-items:center;gap:4px;font-size:12px;font-weight:500;color:#444;background:white;border:1px solid #DDDDDD;border-radius:20px;padding:3px 10px">
                      <mat-icon style="font-size:13px;width:13px;height:13px;color:#6750A4">check_circle</mat-icon>
                      {{ a }}
                    </span>
                  }
                </div>
              </div>
            }

            <!-- Property documents -->
            @if (propertyDocs().length > 0) {
              <div class="es-tile">
                <div class="es-label-upper mb-3">Ownership Documents ({{ propertyDocs().length }})</div>
                <div class="flex flex-col gap-2">
                  @for (doc of propertyDocs(); track doc.documentId) {
                    <div style="display:flex;align-items:center;justify-content:space-between;background:white;border:1px solid #EBEBEB;border-radius:8px;padding:10px 12px">
                      <div class="flex items-center gap-2 min-w-0">
                        <mat-icon style="font-size:18px;width:18px;height:18px;color:#6750A4;flex-shrink:0">description</mat-icon>
                        <div class="min-w-0">
                          <div style="font-size:13px;font-weight:500;color:#222">{{ docLabel(doc.documentType) }}</div>
                          <div style="font-size:11px;color:#AAAAAA;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:300px">{{ doc.originalFileName }}</div>
                        </div>
                      </div>
                      <button type="button" (click)="viewPropertyDoc(doc)"
                        style="display:inline-flex;align-items:center;gap:4px;font-size:12px;font-weight:600;color:#6750A4;background:none;border:none;cursor:pointer;white-space:nowrap;flex-shrink:0;margin-left:8px;padding:4px 0">
                        <mat-icon class="!text-[14px] !w-[14px] !h-[14px]">{{ isDocImage(doc) ? 'image' : 'open_in_new' }}</mat-icon>
                        View
                      </button>
                    </div>
                  }
                </div>
              </div>
            }
          }
        }

        <!-- ── KYC identity + documents ─────────────────────────── -->
        @if (data.kycItem) {
          <div class="es-tile">
            <div class="es-label-upper mb-3">Identity Details</div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
              <div>
                <div class="es-text-hint mb-0.5">Full Name</div>
                <div class="es-text-strong">{{ data.kycItem.fullName }}</div>
              </div>
              <div>
                <div class="es-text-hint mb-0.5">Email</div>
                <div class="es-text-body">{{ data.kycItem.email }}</div>
              </div>
              <div>
                <div class="es-text-hint mb-0.5">Aadhaar Number</div>
                <div style="font-size:13px;font-weight:600;color:#222;font-family:'Roboto Mono',monospace;letter-spacing:1px">
                  {{ data.kycItem.aadhaarNumber || '—' }}
                </div>
              </div>
              <div>
                <div class="es-text-hint mb-0.5">PAN Number</div>
                <div style="font-size:13px;font-weight:600;color:#222;font-family:'Roboto Mono',monospace;letter-spacing:1px">
                  {{ data.kycItem.panNumber || '—' }}
                </div>
              </div>
              <div>
                <div class="es-text-hint mb-0.5">Submitted</div>
                <div class="es-text-body">{{ data.kycItem.createdAt | slice:0:10 }}</div>
              </div>
              @if (data.kycItem.reviewedAt) {
                <div>
                  <div class="es-text-hint mb-0.5">Reviewed</div>
                  <div class="es-text-body">{{ data.kycItem.reviewedAt | slice:0:10 }}</div>
                </div>
              }
            </div>
          </div>

          @if (data.kycItem.documents.length > 0) {
            <div class="es-tile">
              <div class="es-label-upper mb-3">
                Submitted Documents ({{ data.kycItem.documents.length }})
              </div>
              <div class="flex flex-col gap-2">
                @for (doc of orderedDocs(data.kycItem.documents); track doc.documentId) {
                  <div style="display:flex;align-items:center;justify-content:space-between;background:white;border:1px solid #EBEBEB;border-radius:8px;padding:10px 12px">
                    <div class="flex items-center gap-2 min-w-0">
                      <mat-icon style="font-size:18px;width:18px;height:18px;color:#6750A4;flex-shrink:0">description</mat-icon>
                      <div class="min-w-0">
                        <div style="font-size:13px;font-weight:500;color:#222">{{ docLabel(doc.documentType) }}</div>
                        <div style="font-size:11px;color:#AAAAAA;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:260px">{{ doc.originalFileName }}</div>
                      </div>
                    </div>
                    <button type="button" (click)="viewDoc(doc)"
                      style="display:inline-flex;align-items:center;gap:4px;font-size:12px;font-weight:600;color:#6750A4;background:none;border:none;cursor:pointer;white-space:nowrap;flex-shrink:0;margin-left:8px;padding:4px 0">
                      <mat-icon class="!text-[14px] !w-[14px] !h-[14px]">{{ isImage(doc) ? 'image' : 'open_in_new' }}</mat-icon>
                      View
                    </button>
                  </div>
                }
              </div>
            </div>
          } @else {
            <div style="font-size:13px;color:#AAAAAA;font-style:italic">No documents uploaded.</div>
          }
        }

        <!-- ── Decision (read-only or editable) ─────────────────── -->
        @if (data.readOnly) {
          <div class="es-tile">
            <div style="font-size:11px;font-weight:600;color:#717171;text-transform:uppercase;letter-spacing:.5px;margin-bottom:8px">Decision</div>
            <div class="flex items-center gap-2">
              @if (data.currentStatus === 'Approved') {
                <mat-icon style="color:#059669;font-size:20px;width:20px;height:20px">check_circle</mat-icon>
                <span style="font-size:15px;font-weight:600;color:#059669">Approved</span>
              } @else if (data.currentStatus === 'Rejected') {
                <mat-icon style="color:#DC2626;font-size:20px;width:20px;height:20px">cancel</mat-icon>
                <span style="font-size:15px;font-weight:600;color:#DC2626">Rejected</span>
              } @else {
                <span style="font-size:15px;font-weight:600;color:#717171">{{ data.currentStatus }}</span>
              }
            </div>
            @if (data.currentRemarks) {
              <div style="font-size:13px;color:#444;margin-top:8px;line-height:1.5">{{ data.currentRemarks }}</div>
            }
          </div>
        } @else {
          <div style="border-top:1px solid #EBEBEB;padding-top:16px">
            <div class="es-label-upper mb-3">Review Decision</div>
            <form [formGroup]="form" class="flex flex-col gap-3">

              @if (data.type === 'property') {
                <mat-form-field appearance="outline" class="w-full">
                  <mat-label>Decision</mat-label>
                  <mat-icon matPrefix class="mr-2">gavel</mat-icon>
                  <mat-select formControlName="status">
                    <mat-option value="Active">Approve — make Active</mat-option>
                    <mat-option value="Rejected">Reject</mat-option>
                  </mat-select>
                </mat-form-field>
              } @else if (data.type === 'lease') {
                <div class="es-text-sm">Choose whether to activate this lease or void it.</div>
                <div class="flex gap-3">
                  <button mat-stroked-button type="button" (click)="setApprove(true)"
                    [style.border-color]="approveVal() === true ? '#059669' : ''"
                    [style.color]="approveVal() === true ? '#059669' : ''"
                    class="!rounded-full flex-1">
                    <mat-icon>check_circle</mat-icon> Approve
                  </button>
                  <button mat-stroked-button type="button" (click)="setApprove(false)"
                    [style.border-color]="approveVal() === false ? '#DC2626' : ''"
                    [style.color]="approveVal() === false ? '#DC2626' : ''"
                    class="!rounded-full flex-1">
                    <mat-icon>cancel</mat-icon> Reject
                  </button>
                </div>
              } @else {
                <mat-form-field appearance="outline" class="w-full">
                  <mat-label>Decision</mat-label>
                  <mat-icon matPrefix class="mr-2">gavel</mat-icon>
                  <mat-select formControlName="status">
                    <mat-option value="Approved">Approve</mat-option>
                    <mat-option value="Rejected">Reject</mat-option>
                  </mat-select>
                </mat-form-field>
              }

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Remarks</mat-label>
                <mat-icon matPrefix class="mr-2">comment</mat-icon>
                <textarea matInput formControlName="remarks" rows="3" placeholder="Optional notes or reason…"></textarea>
              </mat-form-field>

              @if (error()) {
                <div class="es-text-xs es-text-danger">{{ error() }}</div>
              }
            </form>
          </div>
        }

      </div>

    </mat-dialog-content>

    <mat-dialog-actions align="end" style="padding:16px 24px;gap:8px">
      @if (data.readOnly) {
        <button mat-flat-button color="primary" class="!rounded-full !font-semibold" mat-dialog-close>Close</button>
      } @else {
        <button mat-stroked-button class="!rounded-full" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" class="!rounded-full !font-semibold"
          [disabled]="submitting() || propertyLoading() || (data.type !== 'lease' && form.invalid) || (data.type === 'lease' && approveVal() === null)"
          (click)="submit()">
          @if (submitting()) {
            <mat-spinner diameter="16" class="inline-block !mr-2" />
          }
          Submit Decision
        </button>
      }
    </mat-dialog-actions>
  `,
})
export class AdminReviewDialogComponent implements OnInit, OnDestroy {
  readonly data         = inject<AdminReviewDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef     = inject(MatDialogRef<AdminReviewDialogComponent>);
  private readonly adminService  = inject(AdminService);
  private readonly propService   = inject(PropertyService);
  private readonly dialog        = inject(MatDialog);
  private readonly sanitizer     = inject(DomSanitizer);
  private readonly destroyRef    = inject(DestroyRef);

  @ViewChild('mapEl') mapElRef!: ElementRef<HTMLDivElement>;

  readonly submitting      = signal(false);
  readonly error           = signal<string | null>(null);
  readonly approveVal      = signal<boolean | null>(null);
  readonly propertyLoading = signal(false);
  readonly property        = signal<PropertyDetailResponse | null>(null);
  readonly propertyDocs    = signal<DocumentResponse[]>([]);
  readonly activeImage     = signal<string>('');

  private map?: L.Map;
  private mapInitTimer?: ReturnType<typeof setTimeout>;
  private mapResizeTimer?: ReturnType<typeof setTimeout>;

  readonly form = new FormGroup({
    status:  new FormControl('', Validators.required),
    remarks: new FormControl(''),
  });

  ngOnInit(): void {
    if (this.data.type === 'property') {
      this.propertyLoading.set(true);
      forkJoin({
        detail: this.propService.getProperty(this.data.entityId),
        docs:   this.propService.getDocuments(this.data.entityId),
      }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: ({ detail, docs }) => {
          this.property.set(detail.data);
          this.propertyDocs.set(docs.data ?? []);
          const primary = detail.data.images.find(i => i.isPrimary) ?? detail.data.images[0];
          if (primary) this.activeImage.set(this.imgSrc(primary.image));
          this.propertyLoading.set(false);
          this.mapInitTimer = setTimeout(() => this.initMap(), 50);
        },
        error: () => this.propertyLoading.set(false),
      });
    }
  }

  ngOnDestroy(): void {
    clearTimeout(this.mapInitTimer);
    clearTimeout(this.mapResizeTimer);
    this.map?.remove();
    this.map = undefined;
  }

  private initMap(): void {
    if (!this.mapElRef?.nativeElement || this.map) return;
    const p = this.property();
    if (!p) return;

    const lat = p.latitude ? Number(p.latitude) : 13.0827;
    const lng = p.longitude ? Number(p.longitude) : 80.2707;
    const hasCoords = !!p.latitude && !!p.longitude;

    this.map = L.map(this.mapElRef.nativeElement, {
      zoomControl: true,
      scrollWheelZoom: false,
    }).setView([lat, lng], hasCoords ? 14 : 7);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    if (hasCoords) {
      const icon = L.divIcon({
        className: '',
        html: `<div style="width:18px;height:18px;background:var(--mat-sys-primary);border:2px solid white;border-radius:50%;box-shadow:0 2px 6px rgba(0,0,0,0.3)"></div>`,
        iconSize: [18, 18],
        iconAnchor: [9, 9],
      });
      L.marker([lat, lng], { icon }).addTo(this.map);
    }

    // Force tile reload after dialog finishes animating in
    this.mapResizeTimer = setTimeout(() => this.map?.invalidateSize(), 150);
  }

  imgSrc(path: string): string {
    return this.propService.imageUrl(path);
  }

  setApprove(v: boolean): void { this.approveVal.set(v); }

  docLabel(type: string): string { return DOC_LABELS[type] ?? type; }

  orderedDocs(docs: KycVerificationResponse['documents']): typeof docs {
    return KYC_DOC_ORDER.map(t => docs.find(d => d.documentType === t)).filter(Boolean) as typeof docs;
  }

  isImage(doc: KycDocumentSummary): boolean {
    return this._mimeIsImage(doc.mimeType, doc.originalFileName);
  }

  isDocImage(doc: DocumentResponse): boolean {
    return this._mimeIsImage(doc.mimeType, doc.originalFileName);
  }

  private _mimeIsImage(mime: string | null, name: string): boolean {
    if (mime?.toLowerCase().startsWith('image/')) return true;
    const ext = name.split('.').pop()?.toLowerCase() ?? '';
    return ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp'].includes(ext);
  }

  viewDoc(doc: KycDocumentSummary): void {
    this.dialog.open(DocPreviewDialogComponent, {
      data: {
        url:      doc.documentUrl,
        safeUrl:  this.sanitizer.bypassSecurityTrustResourceUrl(doc.documentUrl),
        label:    this.docLabel(doc.documentType),
        fileName: doc.originalFileName,
        isImage:  this.isImage(doc),
      },
      width: '780px', maxWidth: '92vw', panelClass: 'rounded-dialog',
    });
  }

  viewPropertyDoc(doc: DocumentResponse): void {
    const url = this.propService.fileUrl(doc.documentUrl);
    this.dialog.open(DocPreviewDialogComponent, {
      data: {
        url,
        safeUrl:  this.sanitizer.bypassSecurityTrustResourceUrl(url),
        label:    this.docLabel(doc.documentType),
        fileName: doc.originalFileName,
        isImage:  this.isDocImage(doc),
      },
      width: '780px', maxWidth: '92vw', panelClass: 'rounded-dialog',
    });
  }

  submit(): void {
    this.submitting.set(true);
    this.error.set(null);

    let obs$;
    if (this.data.type === 'kyc') {
      obs$ = this.adminService.reviewKyc(this.data.entityId, {
        status: this.form.value.status as 'Approved' | 'Rejected',
        reviewRemarks: this.form.value.remarks ?? '',
      });
    } else if (this.data.type === 'property') {
      obs$ = this.adminService.approveProperty(this.data.entityId, {
        status: this.form.value.status as 'Active' | 'Rejected',
        approveRemarks: this.form.value.remarks ?? '',
      });
    } else {
      obs$ = this.adminService.approveLease(this.data.entityId, {
        approve: this.approveVal()!,
        remarks: this.form.value.remarks ?? '',
      });
    }

    obs$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.submitting.set(false); this.dialogRef.close(true); },
      error: (e) => { this.submitting.set(false); this.error.set(e?.error?.message ?? 'Something went wrong.'); },
    });
  }
}
