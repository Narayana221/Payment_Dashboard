import { TestBed } from '@angular/core/testing';
import { DataQualityIssue } from '../../../../shared/models/data-quality.model';
import { DataQualityPanelComponent } from './data-quality-panel.component';

describe('DataQualityPanelComponent', () => {
  const issue = (type: string, excludedFromTotals: boolean, id: string): DataQualityIssue => ({
    type,
    message: `${type} on ${id}`,
    recordIds: [id],
    excludedFromTotals,
  });

  async function render(issues: DataQualityIssue[]): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(DataQualityPanelComponent);
    fixture.componentRef.setInput('issues', issues);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders nothing when the data is clean', async () => {
    expect((await render([])).querySelector('section')).toBeNull();
  });

  it('shows problems that change the figures openly, and tucks informational notes away', async () => {
    const element = await render([
      issue('orphanRefund', true, 're_1'),
      issue('duplicateRecord', false, 'ch_1'),
      issue('duplicateRecord', false, 'ch_2'),
    ]);

    expect(element.textContent).toContain('1 problem with the source data.');
    expect(element.querySelector('details')?.textContent).toContain(
      '2 notes that don’t change your figures',
    );
    expect(element.querySelector('details')?.open).toBe(false);
  });

  it('copes with two issues that have the same message', async () => {
    const same = issue('duplicateRecord', false, 'ch_1');
    const element = await render([same, { ...same, recordIds: ['ch_2'] }]);

    expect(element.querySelectorAll('li').length).toBe(2);
  });
});
