import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DataQualityIssue } from '../../../../shared/models/data-quality.model';

/**
 * What the organiser should know about the source data. Issues that took records out of the figures are shown
 * open and first; informational ones (e.g. a duplicate delivery counted once) are collapsed underneath.
 */
@Component({
  selector: 'app-data-quality-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-quality-panel.component.html',
  styleUrl: './data-quality-panel.component.scss',
})
export class DataQualityPanelComponent {
  readonly issues = input.required<readonly DataQualityIssue[]>();

  protected readonly excluded = computed(() => this.issues().filter((i) => i.excludedFromTotals));
  protected readonly informational = computed(() =>
    this.issues().filter((i) => !i.excludedFromTotals),
  );
  protected readonly excludedSummary = computed(() => {
    const count = this.excluded().length;
    return `${count} problem${count === 1 ? '' : 's'} with the source data.`;
  });
  protected readonly informationalSummary = computed(() => {
    const count = this.informational().length;
    return `${count} note${count === 1 ? '' : 's'} that don’t change your figures`;
  });
}
