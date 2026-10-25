import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MoneyComponent } from '../../../../shared/components/money/money.component';
import { Money } from '../../../../shared/models/money.model';
import { MoneyDisplay, formatMoney } from '../../../../shared/pipes/money.pipe';
import { CurrencySummary } from '../../models/dashboard.models';

type RowKind = 'start' | 'deduction' | 'adjustment' | 'total';

interface WaterfallRow {
  readonly label: string;
  readonly detail?: string;
  readonly amount: Money;
  readonly kind: RowKind;
  /** Bar length as a share of gross sales (presentation only; never shown as a figure). */
  readonly barPercent: number;
}

const DISPLAY: Record<RowKind, MoneyDisplay> = {
  start: 'amount',
  deduction: 'deduction',
  adjustment: 'signed',
  total: 'amount',
};

/**
 * Q3: why is the number in my bank different from my sales? Each row is a figure the API already calculated,
 * in the order they connect. The API guarantees the rows add up (D-5); this component only lays them out.
 * Rows for data problems (other fees, unexplained difference, payout discrepancy) appear only when non-zero.
 */
@Component({
  selector: 'app-reconciliation-waterfall',
  imports: [MoneyComponent, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './reconciliation-waterfall.component.html',
  styleUrl: './reconciliation-waterfall.component.scss',
})
export class ReconciliationWaterfallComponent {
  readonly summary = input.required<CurrencySummary>();
  /** The instant the figures are as of (ISO 8601), shown so the all-time scope is explicit. */
  readonly asOf = input<string>();

  protected readonly display = DISPLAY;

  protected readonly rows = computed<WaterfallRow[]>(() => {
    const s = this.summary();
    const all = s.allTime;
    const scale = Math.abs(all.grossSales.amountMinor);
    const row = (label: string, amount: Money, kind: RowKind, detail?: string): WaterfallRow => ({
      label,
      amount,
      kind,
      detail,
      barPercent: scale === 0 ? 0 : Math.min(100, (Math.abs(amount.amountMinor) / scale) * 100),
    });
    const nonZero = (amount: Money) => amount.amountMinor !== 0;

    return [
      row('Gross sales', all.grossSales, 'start', 'What ticket buyers paid'),
      row(
        'Stripe fees',
        all.stripeFees,
        'deduction',
        'Payment processing; not returned on refunds',
      ),
      row(
        'Ticket Tailor fees',
        all.ticketTailorFees,
        'deduction',
        `${formatMoney(all.ticketTailorFeesCharged)} charged, ${formatMoney(all.ticketTailorFeesReturned)} returned on refunds`,
      ),
      ...(nonZero(all.otherFees)
        ? [
            row(
              'Other fees',
              all.otherFees,
              'deduction',
              'Fees of a type we don’t recognise — see data notes',
            ),
          ]
        : []),
      row('Refunds issued', all.refundsIssued, 'deduction', 'Money returned to ticket buyers'),
      ...(nonZero(all.unexplainedDifference)
        ? [
            row(
              'Unexplained difference',
              all.unexplainedDifference,
              'adjustment',
              'Records whose totals don’t add up — see data notes',
            ),
          ]
        : []),
      row('Net earned', all.netEarned, 'total'),
      row('Paid into your bank', s.paidOut, 'deduction'),
      ...(nonZero(s.inTransit) ? [row('On its way to your bank', s.inTransit, 'deduction')] : []),
      ...(nonZero(s.payoutDiscrepancy)
        ? [
            row(
              'Payout discrepancy',
              s.payoutDiscrepancy,
              // StillToCome = NetEarned − PaidOut − InTransit − PayoutDiscrepancy (D-5), so it is taken away here.
              'deduction',
              'Payouts that don’t match their records — see data notes',
            ),
          ]
        : []),
      row('Still to come', s.stillToCome, 'total', 'Earned but not yet paid out'),
    ];
  });

  protected readonly explanation = computed(() => {
    const s = this.summary();
    return (
      `Your sales of ${formatMoney(s.allTime.grossSales)} became ${formatMoney(s.allTime.netEarned)} after fees and refunds. ` +
      `${formatMoney(s.paidOut)} of that has reached your bank` +
      (s.inTransit.amountMinor !== 0 ? `, ${formatMoney(s.inTransit)} is on its way` : '') +
      ` and ${formatMoney(s.stillToCome)} is still to come.` +
      // Without this the sentence wouldn't add up when payouts and their records disagree (D-5).
      (s.payoutDiscrepancy.amountMinor !== 0
        ? ` A further ${formatMoney(s.payoutDiscrepancy, 'deduction')} is a payout discrepancy; see the data notes.`
        : '')
    );
  });
}
