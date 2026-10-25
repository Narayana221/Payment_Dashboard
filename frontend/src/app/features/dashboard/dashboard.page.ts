import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { EmptyStateComponent } from '../../shared/components/states/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/states/error-state.component';
import { LoadingStateComponent } from '../../shared/components/states/loading-state.component';
import { DataQualityPanelComponent } from './components/data-quality-panel/data-quality-panel.component';
import { PayoutPositionComponent } from './components/payout-position/payout-position.component';
import { PeriodSummaryComponent } from './components/period-summary/period-summary.component';
import { ReconciliationWaterfallComponent } from './components/reconciliation-waterfall/reconciliation-waterfall.component';
import { DashboardStore } from './data-access/dashboard.store';

/** The organiser payments dashboard: owns the store and the "as of" control, and lays out the three questions. */
@Component({
  selector: 'app-dashboard-page',
  imports: [
    DatePipe,
    LoadingStateComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    PeriodSummaryComponent,
    PayoutPositionComponent,
    ReconciliationWaterfallComponent,
    DataQualityPanelComponent,
  ],
  providers: [DashboardStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
})
export class DashboardPage {
  protected readonly store = inject(DashboardStore);

  /** The nine dots of the decorative grid mark in the top bar. */
  protected readonly markDots = Array.from({ length: 9 });

  /** The "as of" instant in the form a datetime-local input needs, in UTC ("2026-06-20T11:30"). */
  protected readonly asOfInputValue = computed(() => {
    const asOf = this.store.asOf();
    return asOf ? new Date(asOf).toISOString().slice(0, 16) : '';
  });

  /**
   * Applied on submit (Enter or Apply), not on every change: browsers fire `change` while a date is still being typed,
   * which would send half-typed years to the API. The control is labelled UTC, so the value is read as UTC.
   */
  protected applyAsOf(value: string): void {
    if (value) {
      this.store.setAsOf(`${value}:00Z`);
    }
  }

  /**
   * Jumps to the newest record and puts that time back in the box. Done explicitly because if "as of" was already the
   * latest activity, the binding doesn't change, and a half-typed time would otherwise stay in the box.
   */
  protected showLatestActivity(input: HTMLInputElement): void {
    this.store.showLatestActivity();
    input.value = this.asOfInputValue();
  }

  /** The Retry button disappears once retried, so focus moves to the page content rather than being lost. */
  protected retry(main: HTMLElement): void {
    this.store.reload();
    main.focus();
  }
}
