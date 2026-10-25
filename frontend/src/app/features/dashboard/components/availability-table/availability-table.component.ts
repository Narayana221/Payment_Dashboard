import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MoneyComponent } from '../../../../shared/components/money/money.component';
import { AvailabilityBucket } from '../../models/dashboard.models';

/**
 * Q2: when does the rest become available? Availability only — there is no payout schedule in the data,
 * so this never claims when money will be paid.
 */
@Component({
  selector: 'app-availability-table',
  imports: [DatePipe, MoneyComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './availability-table.component.html',
  styleUrl: './availability-table.component.scss',
})
export class AvailabilityTableComponent {
  readonly buckets = input.required<readonly AvailabilityBucket[]>();
}
