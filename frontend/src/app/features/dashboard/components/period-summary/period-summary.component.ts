import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { StatCardComponent } from '../../../../shared/components/stat-card/stat-card.component';
import { formatMoney } from '../../../../shared/pipes/money.pipe';
import { PERIOD_LABELS, PeriodKey, SalesBreakdown } from '../../models/dashboard.models';

/** Q1: how much have I made? Shows one period's breakdown, exactly as the API reported it. */
@Component({
  selector: 'app-period-summary',
  imports: [StatCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './period-summary.component.html',
  styleUrl: './period-summary.component.scss',
})
export class PeriodSummaryComponent {
  readonly breakdown = input.required<SalesBreakdown>();
  readonly period = input.required<PeriodKey>();
  readonly periodChange = output<PeriodKey>();

  protected readonly periodKeys: readonly PeriodKey[] = ['day', 'week', 'month', 'allTime'];
  protected readonly labels = PERIOD_LABELS;
  protected readonly salesHint = computed(() => countOf(this.breakdown().salesCount, 'sale'));
  protected readonly refundHint = computed(() => countOf(this.breakdown().refundCount, 'refund'));
  /** Explains the net figure, which is negative when more fee came back on refunds than was charged. */
  protected readonly ticketTailorHint = computed(
    () =>
      `${formatMoney(this.breakdown().ticketTailorFeesCharged)} charged, ${formatMoney(this.breakdown().ticketTailorFeesReturned)} returned on refunds`,
  );
}

function countOf(count: number, noun: string): string {
  return `${count} ${noun}${count === 1 ? '' : 's'}`;
}
