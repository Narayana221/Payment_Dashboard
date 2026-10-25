import { httpResource } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { ApiError, toApiError } from '../../../core/api/api-error';
import {
  CurrencySummary,
  DashboardResponse,
  PeriodKey,
  SalesBreakdown,
} from '../models/dashboard.models';
import { DashboardApiService } from './dashboard-api.service';

/**
 * Dashboard state for one page. Selection only: every figure shown comes from the API response as is.
 *
 * "As of" (A7): the first request asks for "now"; if the organiser hasn't picked a time, the view then moves to
 * the response's latestActivityAt, so a quiet account doesn't open on an empty "last 24 hours".
 */
@Injectable()
export class DashboardStore {
  private readonly api = inject(DashboardApiService);

  private readonly requestedAsOf = signal<string | undefined>(undefined);
  private readonly selectedPeriod = signal<PeriodKey>('allTime');
  private readonly chosenCurrency = signal<string | undefined>(undefined);
  /** False until "as of" has been defaulted from the first response or chosen by the organiser. */
  private readonly defaulted = signal(false);

  private readonly resource = httpResource<DashboardResponse>(() =>
    this.api.dashboardRequest(this.requestedAsOf()),
  );

  /** The instant being viewed (ISO 8601, UTC). Undefined until defaulted or chosen: the server then uses "now". */
  readonly asOf = this.requestedAsOf.asReadonly();
  readonly period = this.selectedPeriod.asReadonly();

  readonly error = computed<ApiError | undefined>(() => {
    // While a retry is in flight the previous failure no longer applies, so the page shows loading instead.
    if (this.resource.isLoading()) {
      return undefined;
    }
    const error = this.resource.error();
    if (error) {
      return toApiError(error);
    }
    // A successful response without a body would otherwise leave the page loading for ever.
    return this.resource.status() === 'resolved' && !this.resource.value()
      ? { status: 200, message: 'The payments service returned no data. Please try again.' }
      : undefined;
  });

  /** The last response at the defaulted or chosen "as of". Never the "now" response used to find latestActivityAt. */
  private readonly loaded = signal<DashboardResponse | undefined>(undefined);

  /**
   * The dashboard being shown. It keeps the previous response while a new one loads, so changing "as of" updates the
   * figures in place instead of blanking the page.
   */
  readonly dashboard = this.loaded.asReadonly();

  /** Nothing to show yet (first load). */
  readonly isLoading = computed(() => this.dashboard() === undefined && !this.error());
  /** Figures are on screen and newer ones are on their way. */
  readonly isUpdating = computed(() => this.dashboard() !== undefined && this.resource.isLoading());

  readonly currencies = computed(() => this.dashboard()?.summaries.map((s) => s.currency) ?? []);
  readonly summary = computed<CurrencySummary | undefined>(() => {
    const summaries = this.dashboard()?.summaries ?? [];
    return summaries.find((s) => s.currency === this.chosenCurrency()) ?? summaries[0];
  });
  readonly breakdown = computed<SalesBreakdown | undefined>(() => {
    const summary = this.summary();
    const period = this.selectedPeriod();
    if (!summary) {
      return undefined;
    }
    return period === 'allTime'
      ? summary.allTime
      : summary.periods.find((p) => p.period === period)?.breakdown;
  });
  readonly latestActivityAt = computed(() => this.dashboard()?.latestActivityAt ?? undefined);
  readonly dataQuality = computed(() => this.dashboard()?.dataQuality ?? []);

  constructor() {
    effect(() => {
      const response = this.resource.hasValue() ? this.resource.value() : undefined;
      if (!response || untracked(this.defaulted)) {
        return;
      }
      this.defaulted.set(true);
      if (untracked(this.requestedAsOf) === undefined && response.latestActivityAt) {
        this.requestedAsOf.set(response.latestActivityAt);
      }
    });

    // Kept by an effect rather than derived, so the last figures survive a reload whether or not anything read them.
    effect(() => {
      if (this.defaulted() && !this.resource.isLoading() && this.resource.hasValue()) {
        const response = this.resource.value();
        if (response) {
          this.loaded.set(response);
        }
      }
    });
  }

  setAsOf(isoInstant: string): void {
    this.defaulted.set(true);
    this.requestedAsOf.set(isoInstant);
  }

  showLatestActivity(): void {
    const latest = this.latestActivityAt();
    if (latest) {
      this.setAsOf(latest);
    }
  }

  setPeriod(period: PeriodKey): void {
    this.selectedPeriod.set(period);
  }

  selectCurrency(currency: string): void {
    this.chosenCurrency.set(currency);
  }

  reload(): void {
    this.resource.reload();
  }
}
