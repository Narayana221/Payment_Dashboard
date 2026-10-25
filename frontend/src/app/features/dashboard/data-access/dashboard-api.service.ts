import { HttpResourceRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { API_BASE_URL, ORGANISER_ID } from '../../../core/api/api-config';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly organiserId = inject(ORGANISER_ID);

  /**
   * The dashboard request as of an ISO 8601 instant, or as of "now" when `asOf` is undefined.
   * The query string is built with encodeURIComponent: Angular's default parameter encoder leaves "+" as is,
   * which a server reads as a space, so an offset like +01:00 would be rejected.
   */
  dashboardRequest(asOf: string | undefined): HttpResourceRequest {
    const path = `${this.baseUrl}/api/v1/organisers/${encodeURIComponent(this.organiserId)}/dashboard`;
    return { url: asOf === undefined ? path : `${path}?asOf=${encodeURIComponent(asOf)}` };
  }
}
