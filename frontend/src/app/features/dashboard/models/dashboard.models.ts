import { DataQualityIssue } from '../../../shared/models/data-quality.model';
import { Money } from '../../../shared/models/money.model';

// The dashboard API contract. Each interface mirrors a DTO in
// backend/src/Payments.Application/Features/Dashboard/Dtos, field for field (camelCase in JSON).
// Keep the two in step: the backend's Api tests pin the JSON names, and the fixtures in ../testing use these types.

/** Mirrors DashboardResponseDto. */
export interface DashboardResponse {
  readonly organiserId: string;
  /** The instant every figure is calculated at (ISO 8601, UTC). */
  readonly asOf: string;
  /** When the newest record in the source was created; null with no records. */
  readonly latestActivityAt: string | null;
  /** One per currency; amounts are never summed across currencies. */
  readonly summaries: readonly CurrencySummary[];
  readonly dataQuality: readonly DataQualityIssue[];
}

/** Mirrors CurrencySummaryDto. PaidOut + InTransit + StillToCome + PayoutDiscrepancy = AllTime.NetEarned. */
export interface CurrencySummary {
  readonly currency: string;
  readonly allTime: SalesBreakdown;
  readonly periods: readonly PeriodBreakdown[];
  readonly paidOut: Money;
  readonly inTransit: Money;
  readonly stillToCome: Money;
  readonly payoutDiscrepancy: Money;
  readonly stillToComeByDate: readonly AvailabilityBucket[];
  readonly payouts: readonly PayoutLine[];
}

/** Mirrors SalesBreakdownDto. Gross − Stripe − TT − Other − Refunds + Unexplained = NetEarned. */
export interface SalesBreakdown {
  readonly grossSales: Money;
  readonly stripeFees: Money;
  readonly ticketTailorFeesCharged: Money;
  readonly ticketTailorFeesReturned: Money;
  readonly ticketTailorFees: Money;
  readonly otherFees: Money;
  readonly refundsIssued: Money;
  readonly unexplainedDifference: Money;
  readonly netEarned: Money;
  readonly salesCount: number;
  readonly refundCount: number;
}

/** Mirrors PeriodBreakdownDto. The window is (windowStart, windowEnd]. */
export interface PeriodBreakdown {
  /** "day", "week" or "month". */
  readonly period: string;
  readonly windowStart: string;
  readonly windowEnd: string;
  readonly breakdown: SalesBreakdown;
}

/** Mirrors AvailabilityBucketDto. Availability only, never a predicted payout date. */
export interface AvailabilityBucket {
  /** ISO date, e.g. "2026-06-16". */
  readonly availableOn: string;
  /** Can be negative (e.g. a refund of a sale that was already paid out). */
  readonly amount: Money;
  /** True when the date has passed and the money is waiting for the next payout. */
  readonly isAvailable: boolean;
}

/** Mirrors PayoutLineDto. */
export interface PayoutLine {
  readonly id: string;
  readonly amount: Money;
  /** ISO date. */
  readonly arrivalDate: string;
  readonly hasArrived: boolean;
}

/** The periods the API reports, plus everything up to "as of". */
export type PeriodKey = 'day' | 'week' | 'month' | 'allTime';

export const PERIOD_LABELS: Record<PeriodKey, string> = {
  day: 'Last 24 hours',
  week: 'Last 7 days',
  month: 'Last 30 days',
  allTime: 'All time',
};
