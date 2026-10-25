import { ChangeDetectionStrategy, Component } from '@angular/core';
import { DashboardPage } from './features/dashboard/dashboard.page';

@Component({
  selector: 'app-root',
  imports: [DashboardPage],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
})
export class App {}
