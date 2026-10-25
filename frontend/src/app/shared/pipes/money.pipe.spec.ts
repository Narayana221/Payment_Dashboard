import { formatMoney, MoneyPipe } from './money.pipe';

describe('formatMoney', () => {
  const gbp = (amountMinor: number) => ({ amountMinor, currency: 'GBP' });

  it('formats pence as pounds with thousands separators', () => {
    expect(formatMoney(gbp(116_250))).toBe('£1,162.50');
    expect(formatMoney(gbp(5))).toBe('£0.05');
    expect(formatMoney(gbp(0))).toBe('£0.00');
  });

  it('shows negatives with a minus sign, not only colour', () => {
    expect(formatMoney(gbp(-4_380))).toBe('−£43.80');
  });

  it('marks increases in signed mode and leaves zero unsigned', () => {
    expect(formatMoney(gbp(70), 'signed')).toBe('+£0.70');
    expect(formatMoney(gbp(-70), 'signed')).toBe('−£0.70');
    expect(formatMoney(gbp(0), 'signed')).toBe('£0.00');
  });

  it('shows a deduction (sent as a positive amount) with a minus sign', () => {
    expect(formatMoney(gbp(2_831), 'deduction')).toBe('−£28.31');
    expect(formatMoney(gbp(0), 'deduction')).toBe('£0.00');
  });

  it('shows a negative deduction as money coming back, not going out', () => {
    // e.g. a Ticket Tailor fee credit: more fee returned on refunds than charged in the period.
    expect(formatMoney(gbp(-120), 'deduction')).toBe('+£1.20');
  });

  it('uses the currency’s own number of decimal places', () => {
    expect(formatMoney({ amountMinor: 1_500, currency: 'EUR' })).toBe('€15.00');
    expect(formatMoney({ amountMinor: 1_500, currency: 'JPY' })).toBe('JP¥1,500');
  });
});

describe('MoneyPipe', () => {
  it('renders nothing for a missing value', () => {
    expect(new MoneyPipe().transform(undefined)).toBe('');
  });
});
