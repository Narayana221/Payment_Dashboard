import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MoneyComponent } from '../../../../shared/components/money/money.component';
import { StatCardComponent } from '../../../../shared/components/stat-card/stat-card.component';
import { CurrencySummary } from '../../models/dashboard.models';
import { AvailabilityTableComponent } from '../availability-table/availability-table.component';

/** Q2: how much have I actually been paid, and when does the rest become available? */
@Component({
  selector: 'app-payout-position',
  imports: [StatCardComponent, AvailabilityTableComponent, MoneyComponent, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './payout-position.component.html',
  styleUrl: './payout-position.component.scss',
})
export class PayoutPositionComponent {
  readonly summary = input.required<CurrencySummary>();
}
