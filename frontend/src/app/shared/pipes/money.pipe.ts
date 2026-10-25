import { Pipe, PipeTransform } from '@angular/core';
import { Money } from '../models/money.model';

export type MoneyDisplay =
  /** "£1,234.56" / "−£43.80" */
  | 'amount'
  /** "+£1,234.56" / "−£43.80" / "£0.00" — for changes. */
  | 'signed'
  /**
   * A figure that is taken away in a waterfall (fees, refunds, payouts). The API sends these as positive amounts:
   * 2831 shows as "−£28.31". A negative one is money coming back (e.g. a fee credit), so −120 shows as "+£1.20".
   */
  | 'deduction';

const formatters = new Map<string, Intl.NumberFormat>();

/**
 * Formats minor units (pence) for display. This is the only place amounts are turned into text, and it does no
 * arithmetic beyond shifting the decimal point for the currency.
 */
export function formatMoney(value: Money, display: MoneyDisplay = 'amount'): string {
  const formatter = formatterFor(value.currency);
  const fractionDigits = formatter.resolvedOptions().maximumFractionDigits ?? 2;
  const major = Math.abs(value.amountMinor) / 10 ** fractionDigits;
  const text = formatter.format(major);

  const effect = signedEffect(value, display);
  if (effect < 0) {
    return `−${text}`;
  }
  return effect > 0 && display !== 'amount' ? `+${text}` : text;
}

/** The direction the amount moves the organiser's money: −1 (out), 0 or +1 (in), as displayed. */
export function signedEffect(value: Money, display: MoneyDisplay): -1 | 0 | 1 {
  const sign = Math.sign(value.amountMinor) as -1 | 0 | 1;
  return display === 'deduction' ? ((-sign || 0) as -1 | 0 | 1) : sign;
}

function formatterFor(currency: string): Intl.NumberFormat {
  let formatter = formatters.get(currency);
  if (!formatter) {
    formatter = new Intl.NumberFormat('en-GB', { style: 'currency', currency });
    formatters.set(currency, formatter);
  }
  return formatter;
}

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: Money | null | undefined, display: MoneyDisplay = 'amount'): string {
    return value ? formatMoney(value, display) : '';
  }
}
