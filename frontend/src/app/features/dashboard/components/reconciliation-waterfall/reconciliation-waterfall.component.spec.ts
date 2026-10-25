import { ComponentFixture, TestBed } from '@angular/core/testing';
import { currencySummary, gbp, salesBreakdown } from '../../testing/dashboard.fixtures';
import { CurrencySummary } from '../../models/dashboard.models';
import { ReconciliationWaterfallComponent } from './reconciliation-waterfall.component';

describe('ReconciliationWaterfallComponent', () => {
  let fixture: ComponentFixture<ReconciliationWaterfallComponent>;

  async function render(summary: CurrencySummary): Promise<Map<string, string>> {
    fixture = TestBed.createComponent(ReconciliationWaterfallComponent);
    fixture.componentRef.setInput('summary', summary);
    await fixture.whenStable();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    return new Map(
      Array.from(rows).map((row) => [
        row.querySelector('.step-name')!.textContent!.trim(),
        row.querySelector('.amount')!.textContent!.trim(),
      ]),
    );
  }

  it('lays out the API’s figures from gross sales to still to come, without recalculating any', async () => {
    const rows = await render(currencySummary());

    expect([...rows.entries()]).toEqual([
      ['Gross sales', '£1,162.50'],
      ['Stripe fees', '−£28.31'],
      ['Ticket Tailor fees', '−£38.05'],
      ['Refunds issued', '−£107.50'],
      ['Net earned', '£988.64'],
      ['Paid into your bank', '−£445.36'],
      ['Still to come', '£543.28'],
    ]);
  });

  it('shows the Ticket Tailor fee split as reported', async () => {
    await render(currencySummary());

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      '£41.25 charged, £3.20 returned on refunds',
    );
  });

  it('adds the data-problem rows only when they are not zero', async () => {
    const rows = await render(
      currencySummary({
        allTime: salesBreakdown({ otherFees: gbp(10), unexplainedDifference: gbp(-70) }),
        inTransit: gbp(1_000),
        payoutDiscrepancy: gbp(-30),
      }),
    );

    expect(rows.get('Other fees')).toBe('−£0.10');
    expect(rows.get('Unexplained difference')).toBe('−£0.70');
    expect(rows.get('On its way to your bank')).toBe('−£10.00');
    // Taken away on the way from net earned to still to come (D-5), so a negative one adds back.
    expect(rows.get('Payout discrepancy')).toBe('+£0.30');
  });

  it('reads top to bottom when a payout falls short of its records', async () => {
    // po_0001 for 44,000 against 44,536 of covered nets: the API reports a discrepancy of +536.
    const rows = await render(
      currencySummary({ paidOut: gbp(44_000), payoutDiscrepancy: gbp(536) }),
    );

    // £988.64 − £440.00 − £5.36 = £543.28
    expect(rows.get('Net earned')).toBe('£988.64');
    expect(rows.get('Paid into your bank')).toBe('−£440.00');
    expect(rows.get('Payout discrepancy')).toBe('−£5.36');
    expect(rows.get('Still to come')).toBe('£543.28');
    // The sentence above the table accounts for the same gap.
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('.explanation')?.textContent,
    ).toContain(
      '£440.00 of that has reached your bank and £543.28 is still to come. A further −£5.36 is a payout discrepancy',
    );
  });

  it('leaves the discrepancy out of the sentence when there is none', async () => {
    await render(currencySummary());

    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('discrepancy');
  });

  it('shows a fee credit as money coming back', async () => {
    const rows = await render(
      currencySummary({ allTime: salesBreakdown({ ticketTailorFees: gbp(-120) }) }),
    );

    expect(rows.get('Ticket Tailor fees')).toBe('+£1.20');
  });

  it('says the waterfall covers all time up to "as of"', async () => {
    fixture = TestBed.createComponent(ReconciliationWaterfallComponent);
    fixture.componentRef.setInput('summary', currencySummary());
    fixture.componentRef.setInput('asOf', '2026-06-20T11:30:00+00:00');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent?.replace(/\s+/g, ' ')).toContain(
      'All time, up to Sat 20 Jun 2026, 11:30 UTC',
    );
  });
});
