import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Money } from '../../models/money.model';
import { MoneyDisplay, MoneyPipe, signedEffect } from '../../pipes/money.pipe';

/**
 * An amount with its direction made visible in text (− / +), not just colour. Money going out is red unless
 * `tone` is neutral (e.g. a fee card, where a negative fee is a credit, not a loss).
 */
@Component({
  selector: 'app-money',
  imports: [MoneyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './money.component.html',
  styleUrl: './money.component.scss',
})
export class MoneyComponent {
  readonly value = input.required<Money>();
  readonly display = input<MoneyDisplay>('amount');
  readonly tone = input<'auto' | 'neutral'>('auto');

  protected readonly isNegative = computed(
    () => this.tone() === 'auto' && signedEffect(this.value(), this.display()) < 0,
  );
}
