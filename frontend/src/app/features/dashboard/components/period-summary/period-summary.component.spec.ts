import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PeriodKey, SalesBreakdown } from '../../models/dashboard.models';
import { gbp, salesBreakdown } from '../../testing/dashboard.fixtures';
import { PeriodSummaryComponent } from './period-summary.component';

describe('PeriodSummaryComponent', () => {
  let fixture: ComponentFixture<PeriodSummaryComponent>;
  const element = () => fixture.nativeElement as HTMLElement;

  async function render(breakdown: SalesBreakdown, period: PeriodKey = 'allTime'): Promise<void> {
    fixture = TestBed.createComponent(PeriodSummaryComponent);
    fixture.componentRef.setInput('breakdown', breakdown);
    fixture.componentRef.setInput('period', period);
    await fixture.whenStable();
  }

  it('marks the selected period with aria-pressed and emits a new choice', async () => {
    await render(salesBreakdown(), 'week');
    const chosen: PeriodKey[] = [];
    fixture.componentInstance.periodChange.subscribe((p) => chosen.push(p));

    const buttons = Array.from(
      element().querySelectorAll<HTMLButtonElement>('[role=group] button'),
    );
    expect(buttons.map((b) => b.getAttribute('aria-pressed'))).toEqual([
      'false',
      'true',
      'false',
      'false',
    ]);

    buttons[0].click();
    expect(chosen).toEqual(['day']);
  });

  it('pairs each label with its figure in a description list', async () => {
    await render(salesBreakdown());

    const netEarned = Array.from(element().querySelectorAll('dl')).find((dl) =>
      dl.querySelector('dt')?.textContent?.includes('Net earned'),
    );
    expect(netEarned?.querySelector('dd')?.textContent).toContain('£988.64');
  });

  it('explains a Ticket Tailor fee credit with the charged/returned split, without marking it as a loss', async () => {
    // Last 24 hours as of 20 June: only a refund, so the fee came back.
    await render(
      salesBreakdown({
        ticketTailorFeesCharged: gbp(0),
        ticketTailorFeesReturned: gbp(120),
        ticketTailorFees: gbp(-120),
      }),
      'day',
    );

    const card = Array.from(element().querySelectorAll('dl')).find((dl) =>
      dl.querySelector('dt')?.textContent?.includes('Ticket Tailor fees'),
    )!;
    expect(card.textContent).toContain('£0.00 charged, £1.20 returned on refunds');
    expect(card.querySelector('.money--negative')).toBeNull();
  });
});
