import { TestBed } from '@angular/core/testing';
import { AvailabilityBucket } from '../../models/dashboard.models';
import { gbp } from '../../testing/dashboard.fixtures';
import { AvailabilityTableComponent } from './availability-table.component';

describe('AvailabilityTableComponent', () => {
  async function render(buckets: AvailabilityBucket[]): Promise<string> {
    const fixture = TestBed.createComponent(AvailabilityTableComponent);
    fixture.componentRef.setInput('buckets', buckets);
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('reports availability, never a predicted payout date', async () => {
    const text = await render([
      { availableOn: '2026-06-16', amount: gbp(7_962), isAvailable: true },
      { availableOn: '2026-06-25', amount: gbp(1_000), isAvailable: false },
    ]);

    expect(text).toContain('Available now');
    expect(text).toContain('Available from Thu 25 Jun 2026');
    expect(text.toLowerCase()).not.toContain('paid on');
    expect(text.toLowerCase()).not.toContain('will be paid');
  });

  it('explains a negative amount instead of relying on colour', async () => {
    const text = await render([
      { availableOn: '2026-06-20', amount: gbp(-4_380), isAvailable: true },
    ]);

    expect(text).toContain('−£43.80');
    expect(text).toContain(
      'Refunds of sales that were already paid out are taken from future funds.',
    );
  });

  it('says so when nothing is still to come', async () => {
    expect(await render([])).toContain('everything you');
  });
});
