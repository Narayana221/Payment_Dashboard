import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardPage } from './dashboard.page';
import { dashboardResponse } from './testing/dashboard.fixtures';
import { flushSignals } from './testing/flush';

describe('DashboardPage', () => {
  let fixture: ComponentFixture<DashboardPage>;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(DashboardPage);
    await render();
  });

  afterEach(() => http.verify());

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  async function render(): Promise<void> {
    await flushSignals();
    fixture.detectChanges();
  }

  async function respondWith(...responses: ReturnType<typeof dashboardResponse>[]): Promise<void> {
    for (const response of responses) {
      http.expectOne(() => true).flush(response);
      await render();
    }
  }

  it('shows a loading state until the figures arrive', () => {
    expect(text()).toContain('Loading your payments');
    http.expectOne(() => true);
  });

  it('shows the three questions answered with the API’s figures', async () => {
    await respondWith(dashboardResponse(), dashboardResponse());

    expect(text()).toContain("What you've made");
    expect(text()).toContain('£988.64');
    expect(text()).toContain("What you've been paid");
    expect(text()).toContain('£445.36');
    expect(text()).toContain('£543.28');
    expect(text()).toContain('Why your bank balance differs from your sales');
    expect(text()).toContain('−£43.80');
  });

  it('shows an error with a retry that asks again', async () => {
    http.expectOne(() => true).flush(null, { status: 0, statusText: 'Unknown Error' });
    await render();

    expect(text()).toContain("couldn't reach the payments service");
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('app-error-state button')!
      .click();
    await render();

    http.expectOne(() => true);
  });

  it('shows an empty state for an account with no activity', async () => {
    await respondWith(
      dashboardResponse({ latestActivityAt: null, summaries: [], dataQuality: [] }),
    );

    expect(text()).toContain('No sales or payouts yet');
  });

  it('labels the "as of" control so the organiser knows what "last 7 days" is measured from', async () => {
    await respondWith(dashboardResponse(), dashboardResponse());

    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('#as-of')!;
    const label = (fixture.nativeElement as HTMLElement).querySelector('label[for="as-of"]');
    expect(label?.textContent).toContain('UTC');
    expect(input.value).toBe('2026-06-20T11:30');
  });
  it('applies a typed "as of" only on submit, read as UTC', async () => {
    await respondWith(dashboardResponse(), dashboardResponse());
    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector<HTMLInputElement>('#as-of')!;

    input.value = '2026-06-17T12:00';
    input.dispatchEvent(new Event('change'));
    await render();
    http.expectNone(() => true); // typing alone doesn't fetch

    element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await render();
    http.expectOne('/api/v1/organisers/acct_demo/dashboard?asOf=2026-06-17T12%3A00%3A00Z');
  });

  it('puts the latest activity time back in the box, discarding a half-typed one', async () => {
    await respondWith(dashboardResponse(), dashboardResponse());
    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector<HTMLInputElement>('#as-of')!;

    input.value = '2026-06-15T09:00'; // typed, never applied
    const latest = Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
      (b) => b.textContent?.trim() === 'Latest activity',
    )!;
    latest.click();
    await render();

    expect(input.value).toBe('2026-06-20T11:30');
    http.expectNone(() => true); // already at the latest activity: nothing to fetch
  });
});
