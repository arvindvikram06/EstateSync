import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ComplaintService } from '@core/services/complaint/complaint.service';
import { ComplaintResponse } from '../../../core/models/complaint.model';

export interface ComplaintDetailDialogData {
  complaint: ComplaintResponse;
  viewer?: 'owner' | 'tenant' | 'admin';
}

@Component({
  selector: 'app-complaint-detail-dialog',
  standalone: true,
  imports: [DatePipe, FormsModule, MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="complaint-dialog">
      <header>
        <div class="header-icon"><mat-icon>build_circle</mat-icon></div>
        <div class="header-copy">
          <span>{{ c().propertyTitle }}</span>
          <h2>{{ c().title }}</h2>
          <div class="meta">
            <b class="priority priority-{{ c().priority.toLowerCase() }}">{{ c().priority }}</b>
            <b class="status">{{ statusLabel(c().status) }}</b>
            <small>Raised {{ c().createdAt | date:'MMM d, y' }}</small>
          </div>
        </div>
        <button class="icon-close" (click)="close()"><mat-icon>close</mat-icon></button>
      </header>

      <div class="dialog-body">
        <main>
          <section class="issue-box">
            <span class="section-label">Reported issue</span>
            <p>{{ c().description }}</p>
            @if (c().images.length) {
              <div class="photos">
                @for (img of c().images; track img.complaintImageId) {
                  <img [src]="svc.fileUrl(img.imageUrl)" alt="Complaint evidence" (click)="openImage(img.imageUrl)" />
                }
              </div>
            }
          </section>

          @if (c().resolutionSummary) {
            <section class="resolution-box">
              <mat-icon>task_alt</mat-icon>
              <div><span class="section-label">Owner’s resolution</span><p>{{ c().resolutionSummary }}</p></div>
            </section>
          }
          @if (c().reopenReason) {
            <section class="reopen-box">
              <mat-icon>replay</mat-icon>
              <div><span class="section-label">Latest reopen reason</span><p>{{ c().reopenReason }}</p></div>
            </section>
          }

          <section class="timeline-section">
            <div class="timeline-heading">
              <div><span class="section-label">Activity history</span><h3>What happened</h3></div>
              @if (c().reopenCount > 0) { <b>{{ c().reopenCount }} reopen{{ c().reopenCount === 1 ? '' : 's' }}</b> }
            </div>
            <div class="timeline">
              @for (activity of c().activities; track activity.activityId; let last = $last) {
                <div class="timeline-item" [class.last]="last">
                  <span class="timeline-dot"><mat-icon>{{ activityIcon(activity.activityType) }}</mat-icon></span>
                  <div>
                    <strong>{{ activityLabel(activity.activityType, activity.newStatus) }}</strong>
                    <small>{{ activity.actorName }} · {{ activity.actorRole }} · {{ activity.createdAt | date:'MMM d, h:mm a' }}</small>
                    @if (activity.summary && activity.activityType !== 'Created') { <p>{{ activity.summary }}</p> }
                  </div>
                </div>
              }
            </div>
          </section>
        </main>

        <aside>
          <span class="section-label">Next step</span>
          <h3>{{ nextStepTitle() }}</h3>
          <p>{{ nextStepDescription() }}</p>

          @if (editorMode()) {
            <label>{{ editorMode() === 'resolution' ? 'Resolution summary' : 'Why is the issue not fixed?' }}</label>
            <textarea [ngModel]="remarks()" (ngModelChange)="remarks.set($event)" maxlength="1000" rows="5"
              [placeholder]="editorMode() === 'resolution' ? 'Explain what was fixed and any instructions for the tenant…' : 'Describe what is still wrong or what happened again…'"></textarea>
            <div class="editor-actions">
              <button mat-flat-button color="primary" [disabled]="!remarks().trim() || saving()"
                (click)="submitEditor()">
                @if (saving()) { <mat-spinner diameter="16" /> }
                {{ editorMode() === 'resolution' ? 'Send for confirmation' : 'Reopen complaint' }}
              </button>
              <button mat-button (click)="editorMode.set(null)">Cancel</button>
            </div>
          } @else {
            <div class="workflow-actions">
              @if (viewer === 'owner' && (c().status === 'Open' || c().status === 'Reopened')) {
                <button mat-flat-button color="primary" (click)="changeStatus('InProgress')" [disabled]="saving()">
                  <mat-icon>construction</mat-icon> Start work
                </button>
              }
              @if (viewer === 'owner' && c().status === 'InProgress') {
                <button mat-flat-button color="primary" (click)="editorMode.set('resolution')" [disabled]="saving()">
                  <mat-icon>task_alt</mat-icon> Propose resolution
                </button>
              }
              @if (viewer === 'tenant' && c().status === 'ResolutionProposed') {
                <button mat-flat-button color="primary" (click)="changeStatus('Resolved')" [disabled]="saving()">
                  <mat-icon>check_circle</mat-icon> Confirm fixed
                </button>
                <button mat-stroked-button color="warn" (click)="editorMode.set('reopen')" [disabled]="saving()">
                  <mat-icon>replay</mat-icon> Still not fixed
                </button>
              }
              @if (viewer === 'tenant' && c().status === 'Resolved') {
                <button mat-stroked-button color="warn" (click)="editorMode.set('reopen')" [disabled]="saving()">Reopen</button>
                <button mat-flat-button color="primary" (click)="changeStatus('Closed')" [disabled]="saving()">Close complaint</button>
              }
              @if (saving()) { <mat-spinner diameter="20" /> }
            </div>
          }
        </aside>
      </div>
    </div>
  `,
  styles: [`
    .complaint-dialog{width:min(820px,94vw);max-height:86vh;overflow:auto;color:#172033;background:#fff}.complaint-dialog header{position:sticky;top:0;z-index:2;display:flex;align-items:flex-start;gap:12px;padding:18px 20px;border-bottom:1px solid #eceef2;background:#fff}.header-icon{display:grid;width:38px;height:38px;border-radius:12px;color:var(--mat-sys-primary);background:#ffe9ed;place-items:center}.header-icon mat-icon{width:19px;height:19px;font-size:19px}.header-copy{min-width:0;flex:1}.header-copy>span,.section-label{display:block;color:#8a92a1;font-size:9px;font-weight:800;letter-spacing:.11em;text-transform:uppercase}.header-copy h2{margin:2px 0 6px;font-size:18px}.meta{display:flex;align-items:center;gap:7px}.meta b{padding:4px 8px;border-radius:20px;font-size:8px}.meta small{color:#8a92a1;font-size:9px}.priority{color:#9a6700;background:#fef3c7}.priority-high,.priority-critical{color:#b91c1c;background:#fee2e2}.status{color:#9f1239;background:#ffe4e9}.icon-close{display:grid;width:31px;height:31px;border:0;border-radius:9px;color:#737b8a;background:#f6f7f9;place-items:center;cursor:pointer}.dialog-body{display:grid;grid-template-columns:minmax(0,1fr) 285px}.dialog-body main{min-width:0;padding:19px 20px}.dialog-body aside{position:sticky;top:111px;align-self:start;box-sizing:border-box;min-height:340px;padding:20px;border-left:1px solid #eceef2;background:#fafbfc}.issue-box{padding:14px;border:1px solid #e7e9ee;border-radius:12px;background:#fff}.issue-box>p,.resolution-box p,.reopen-box p{margin:6px 0 0;color:#596274;font-size:11px;line-height:1.6;white-space:pre-wrap}.photos{display:flex;gap:7px;margin-top:11px}.photos img{width:62px;height:62px;border-radius:9px;object-fit:cover;cursor:pointer}.resolution-box,.reopen-box{display:flex;gap:10px;margin-top:10px;padding:12px;border-radius:11px;color:#047857;background:#ecfdf5}.resolution-box mat-icon,.reopen-box mat-icon{font-size:18px}.reopen-box{color:#b45309;background:#fffbeb}.timeline-section{margin-top:18px}.timeline-heading{display:flex;align-items:flex-end;justify-content:space-between}.timeline-heading h3,.dialog-body aside h3{margin:3px 0 0;font-size:14px}.timeline-heading>b{padding:4px 7px;border-radius:20px;color:#b31332;background:#ffe4e9;font-size:8px}.timeline{margin-top:13px}.timeline-item{position:relative;display:grid;grid-template-columns:28px 1fr;gap:9px;padding-bottom:14px}.timeline-item:not(.last)::before{position:absolute;top:25px;bottom:0;left:13px;width:1px;background:#e3e5ea;content:''}.timeline-dot{z-index:1;display:grid;width:27px;height:27px;border-radius:9px;color:var(--mat-sys-primary);background:#fff0f3;place-items:center}.timeline-dot mat-icon{width:14px;height:14px;font-size:14px}.timeline-item strong,.timeline-item small{display:block}.timeline-item strong{font-size:10px}.timeline-item small{margin-top:2px;color:#8b92a0;font-size:8px}.timeline-item p{margin:5px 0 0;padding:7px 9px;border-radius:7px;color:#636c7c;background:#f6f7f9;font-size:9px}.dialog-body aside>p{margin-bottom:0;color:#788192;font-size:10px;line-height:1.55}.workflow-actions{display:flex;align-items:stretch;gap:8px;margin-top:16px;flex-direction:column}.workflow-actions button{width:100%;white-space:nowrap}.dialog-body aside label{display:block;margin:15px 0 6px;color:#60697a;font-size:9px;font-weight:800}.dialog-body aside textarea{box-sizing:border-box;width:100%;min-height:104px;padding:10px;border:1px solid #dde0e6;border-radius:10px;font:inherit;font-size:10px;line-height:1.5;resize:vertical;outline:none}.dialog-body aside textarea:focus{border-color:var(--mat-sys-primary);box-shadow:0 0 0 3px rgb(255 56 92 / .08)}.editor-actions{display:flex;align-items:stretch;gap:4px;margin-top:10px;flex-direction:column}.editor-actions button{width:100%;white-space:nowrap}.editor-actions button:first-child{min-height:38px}.editor-actions mat-spinner{display:inline-block;margin-right:6px}@media(max-width:700px){.complaint-dialog{max-height:92vh}.dialog-body{grid-template-columns:1fr}.dialog-body aside{position:static;min-height:0;border-top:1px solid #eceef2;border-left:0}.complaint-dialog header{padding:16px}}
  `]
})
export class ComplaintDetailDialogComponent {
  private readonly data: ComplaintDetailDialogData = inject(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ComplaintDetailDialogComponent>);
  readonly svc = inject(ComplaintService);
  readonly c = signal(this.data.complaint);
  readonly viewer = this.data.viewer ?? 'tenant';
  readonly saving = signal(false);
  readonly editorMode = signal<'resolution' | 'reopen' | null>(null);
  readonly remarks = signal('');

  changeStatus(status: ComplaintResponse['status'], remarks?: string): void {
    this.saving.set(true);
    this.svc.updateComplaintStatus(this.c().complaintId, { status, reviewRemarks: remarks ?? null }).subscribe({
      next: res => {
        this.c.set(res.data);
        this.saving.set(false);
        this.editorMode.set(null);
        this.remarks.set('');
      },
      error: () => this.saving.set(false)
    });
  }

  submitEditor(): void {
    this.changeStatus(this.editorMode() === 'resolution' ? 'ResolutionProposed' : 'Reopened', this.remarks().trim());
  }

  nextStepTitle(): string {
    const status = this.c().status;
    if (this.viewer === 'owner') {
      if (status === 'Open' || status === 'Reopened') return 'Acknowledge the issue';
      if (status === 'InProgress') return 'Finish the work';
      if (status === 'ResolutionProposed') return 'Waiting for tenant';
    } else {
      if (status === 'ResolutionProposed') return 'Review the owner’s fix';
      if (status === 'Resolved') return 'Issue resolved';
      if (status === 'Open' || status === 'InProgress' || status === 'Reopened') return 'Waiting for owner';
    }
    return 'No action required';
  }

  nextStepDescription(): string {
    const status = this.c().status;
    if (this.viewer === 'owner' && status === 'InProgress') return 'When the issue is fixed, explain the work and ask the tenant to confirm.';
    if (this.viewer === 'tenant' && status === 'ResolutionProposed') return 'Confirm the repair only if the problem is actually fixed.';
    if (status === 'Open' || status === 'Reopened') return 'The owner needs to acknowledge this complaint and begin work.';
    if (status === 'ResolutionProposed') return 'The tenant must confirm the resolution or reopen the complaint.';
    return 'The complete activity history is preserved for reference.';
  }

  statusLabel(status: ComplaintResponse['status']): string {
    return status === 'InProgress' ? 'In progress' :
      status === 'ResolutionProposed' ? 'Waiting for tenant' : status;
  }

  activityLabel(type: string, status: string | null): string {
    if (type === 'Created') return 'Complaint submitted';
    if (type === 'ResolutionProposed') return 'Resolution proposed';
    if (type === 'ResolutionConfirmed') return 'Tenant confirmed the fix';
    if (type === 'Reopened') return 'Complaint reopened';
    return status ? `Status changed to ${this.statusLabel(status as ComplaintResponse['status'])}` : 'Complaint updated';
  }

  activityIcon(type: string): string {
    if (type === 'Created') return 'add';
    if (type === 'ResolutionProposed') return 'task_alt';
    if (type === 'ResolutionConfirmed') return 'verified';
    if (type === 'Reopened') return 'replay';
    return 'sync';
  }

  openImage(url: string): void {
    window.open(this.svc.fileUrl(url), '_blank');
  }

  close(): void {
    this.ref.close(this.c());
  }
}
