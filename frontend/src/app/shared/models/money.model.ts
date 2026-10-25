/**
 * An amount in the currency's smallest unit (pence for GBP), exactly as the API sends it.
 * Mirrors `MoneyDto` in backend/src/Payments.Application/Shared/Dtos.
 */
export interface Money {
  readonly amountMinor: number;
  readonly currency: string;
}
