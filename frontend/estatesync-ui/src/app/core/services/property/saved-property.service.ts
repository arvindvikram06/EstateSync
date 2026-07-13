import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse, PagedResponse } from '../../models/api-response.model';
import { PropertySummaryResponse } from '../../models/property.model';

@Injectable({ providedIn: 'root' })
export class SavedPropertyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/SavedProperties`;

  // Saved property ids for the logged-in user — drives heart states on cards.
  readonly savedIds = signal<Set<string>>(new Set());
  private idsLoaded = false;

  isSaved(propertyId: string): boolean {
    return this.savedIds().has(propertyId);
  }

  loadIds(): void {
    this.http.get<ApiResponse<string[]>>(`${this.baseUrl}/ids`).subscribe({
      next: res => {
        if (res.success && res.data) {
          this.savedIds.set(new Set(res.data));
          this.idsLoaded = true;
        }
      },
      error: () => {},
    });
  }

  ensureIdsLoaded(): void {
    if (!this.idsLoaded) this.loadIds();
  }

  // Optimistic toggle: flip the heart immediately, revert if the API call fails.
  toggle(propertyId: string): void {
    const wasSaved = this.isSaved(propertyId);
    this.flip(propertyId, !wasSaved);

    const call = wasSaved
      ? this.http.delete<ApiResponse<object>>(`${this.baseUrl}/${propertyId}`)
      : this.http.post<ApiResponse<object>>(`${this.baseUrl}/${propertyId}`, {});

    call.subscribe({
      error: () => this.flip(propertyId, wasSaved),
    });
  }

  getSaved(page = 1, limit = 12): Observable<PagedResponse<PropertySummaryResponse>> {
    return this.http.get<PagedResponse<PropertySummaryResponse>>(this.baseUrl, {
      params: { page, limit },
    });
  }

  addIds(propertyIds: string[]): void {
    if (propertyIds.length === 0) return;
    this.savedIds.update(ids => {
      const next = new Set(ids);
      for (const id of propertyIds) next.add(id);
      return next;
    });
  }

  clear(): void {
    this.savedIds.set(new Set());
    this.idsLoaded = false;
  }

  private flip(propertyId: string, saved: boolean): void {
    this.savedIds.update(ids => {
      const next = new Set(ids);
      if (saved) next.add(propertyId);
      else next.delete(propertyId);
      return next;
    });
  }
}
