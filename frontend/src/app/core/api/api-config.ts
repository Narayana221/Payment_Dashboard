import { InjectionToken } from '@angular/core';

/** Prefix for API calls. Empty means same origin: `ng serve` proxies /api to the backend (proxy.conf.json). */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '',
});

/**
 * The organiser whose dashboard is shown. Fixed for this demo; in production it would come from the
 * signed-in session, and the API would take it from the authenticated principal rather than the URL.
 */
export const ORGANISER_ID = new InjectionToken<string>('ORGANISER_ID', {
  providedIn: 'root',
  factory: () => 'acct_demo',
});
