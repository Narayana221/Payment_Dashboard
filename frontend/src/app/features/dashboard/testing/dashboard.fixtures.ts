import { Money } from '../../../shared/models/money.model';
import { CurrencySummary, DashboardResponse, SalesBreakdown } from '../models/dashboard.models';

/** Test data shaped like the API's response for records.json as of 2026-06-21 (the golden figures, in pence). */
export const gbp = (amountMinor: number): Money => ({ amountMinor, currency: 'GBP' });

export function salesBreakdown(overrides: Partial<SalesBreakdown> = {}): SalesBreakdown {
  return {
    grossSales: gbp(116_250),
    stripeFees: gbp(2_831),
    ticketTailorFeesCharged: gbp(4_125),
    ticketTailorFeesReturned: gbp(320),
    ticketTailorFees: gbp(3_805),
    otherFees: gbp(0),
    refundsIssued: gbp(10_750),
    unexplainedDifference: gbp(0),
    netEarned: gbp(98_864),
    salesCount: 60,
    refundCount: 4,
    ...overrides,
  };
}

export function currencySummary(overrides: Partial<CurrencySummary> = {}): CurrencySummary {
  const asOf = '2026-06-21T00:00:00+00:00';
  return {
    currency: 'GBP',
    allTime: salesBreakdown(),
    periods: [
      {
        period: 'day',
        windowStart: '2026-06-20T00:00:00+00:00',
        windowEnd: asOf,
        breakdown: salesBreakdown({
          grossSales: gbp(0),
          stripeFees: gbp(0),
          ticketTailorFeesCharged: gbp(0),
          ticketTailorFeesReturned: gbp(120),
          ticketTailorFees: gbp(-120),
          refundsIssued: gbp(4_500),
          netEarned: gbp(-4_380),
          salesCount: 0,
          refundCount: 1,
        }),
      },
      {
        period: 'week',
        windowStart: '2026-06-14T00:00:00+00:00',
        windowEnd: asOf,
        breakdown: salesBreakdown({
          grossSales: gbp(60_350),
          netEarned: gbp(46_366),
          salesCount: 30,
        }),
      },
      {
        period: 'month',
        windowStart: '2026-05-22T00:00:00+00:00',
        windowEnd: asOf,
        breakdown: salesBreakdown(),
      },
    ],
    paidOut: gbp(44_536),
    inTransit: gbp(0),
    stillToCome: gbp(54_328),
    payoutDiscrepancy: gbp(0),
    stillToComeByDate: [
      { availableOn: '2026-06-16', amount: gbp(7_962), isAvailable: true },
      { availableOn: '2026-06-17', amount: gbp(50_746), isAvailable: true },
      { availableOn: '2026-06-20', amount: gbp(-4_380), isAvailable: true },
    ],
    payouts: [{ id: 'po_0001', amount: gbp(44_536), arrivalDate: '2026-06-18', hasArrived: true }],
    ...overrides,
  };
}

export function dashboardResponse(overrides: Partial<DashboardResponse> = {}): DashboardResponse {
  return {
    organiserId: 'acct_demo',
    asOf: '2026-06-21T00:00:00+00:00',
    latestActivityAt: '2026-06-20T11:30:00+00:00',
    summaries: [currencySummary()],
    dataQuality: [
      {
        type: 'duplicateRecord',
        message: 'Record ch_0005 was delivered 2 times with identical content; it is counted once.',
        recordIds: ['ch_0005'],
        excludedFromTotals: false,
      },
    ],
    ...overrides,
  };
}
