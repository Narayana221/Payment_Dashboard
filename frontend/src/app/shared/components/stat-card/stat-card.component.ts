import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Money } from '../../models/money.model';
import { MoneyComponent } from '../money/money.component';

export type StatCardVariant = 'default' | 'accent' | 'light';

/**
 * One labelled figure as a dashboard tile. Renders its own description list, so label and value stay paired for
 * screen readers. `accent` and `light` tiles pick out the figures that answer the organiser's questions.
 */
@Component({
  selector: 'app-stat-card',
  imports: [MoneyComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stat-card.component.html',
  styleUrl: './stat-card.component.scss',
})
export class StatCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<Money>();
  readonly hint = input<string>();
  readonly variant = input<StatCardVariant>('default');
  /** Neutral for figures where a negative isn't a loss (e.g. a fee credit). Accent and light tiles are always neutral. */
  readonly tone = input<'auto' | 'neutral'>('auto');
}
