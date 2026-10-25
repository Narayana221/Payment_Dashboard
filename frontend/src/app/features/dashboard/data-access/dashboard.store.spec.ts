import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { flushSignals } from '../testing/flush';
import { currencySummary, dashboardResponse, gbp } from '../testing/dashboard.fixtures';
import { DashboardStore } from './dashboard.store';

const DASHBOARD = '/api/v1/organisers/acct_demo/dashboard';

describe('DashboardStore', () => {
  let http: HttpTestingController;
  let store: DashboardStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), DashboardStore],
    });
    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(DashboardStore);
  });

  afterEach(() => http.verify());

  const settle = flushSignals;

  it('defaults "as of" to the latest activity, then shows figures at that instant', async () => {
    await settle();
    http.expectOne(DASHBOARD).flush(dashboardResponse({ asOf: '2026-10-22T12:00:00+00:00' }));
    await settle();

    // The "now" response is not shown; the store asks again at the latest activity.
    expect(store.dashboard()).toBeUndefined();
    expect(store.isLoading()).toBe(true);
    http
      .expectOne(`${DASHBOARD}?asOf=2026-06-20T11%3A30%3A00%2B00%3A00`)
      .flush(dashboardResponse());
    await settle();

    expect(store.isLoading()).toBe(false);
    expect(store.asOf()).toBe('2026-06-20T11:30:00+00:00');
    expect(store.summary()?.paidOut).toEqual(gbp(44_536));
  });

  it('keeps an "as of" the organiser chose instead of defaulting it', async () => {
    store.setAsOf('2026-06-17T12:00:00Z');
    await settle();

    http.expectOne(`${DASHBOARD}?asOf=2026-06-17T12%3A00%3A00Z`).flush(dashboardResponse());
    await settle();

    expect(store.asOf()).toBe('2026-06-17T12:00:00Z');
    expect(store.isLoading()).toBe(false);
  });

  it('shows an empty account as is, without a second request', async () => {
    await settle();
    http
      .expectOne(DASHBOARD)
      .flush(dashboardResponse({ latestActivityAt: null, summaries: [], dataQuality: [] }));
    await settle();

    expect(store.isLoading()).toBe(false);
    expect(store.summary()).toBeUndefined();
  });

  it('selects the API’s own breakdown for the chosen period', async () => {
    store.setAsOf('2026-06-21T00:00:00Z');
    await settle();
    http.expectOne(() => true).flush(dashboardResponse());
    await settle();

    store.setPeriod('day');

    expect(store.breakdown()).toBe(
      store.summary()?.periods.find((p) => p.period === 'day')?.breakdown,
    );
    expect(store.breakdown()?.netEarned).toEqual(gbp(-4_380));
  });

  it('lets the organiser switch currency', async () => {
    store.setAsOf('2026-06-21T00:00:00Z');
    await settle();
    http
      .expectOne(() => true)
      .flush(
        dashboardResponse({ summaries: [currencySummary(), currencySummary({ currency: 'EUR' })] }),
      );
    await settle();

    store.selectCurrency('EUR');

    expect(store.currencies()).toEqual(['GBP', 'EUR']);
    expect(store.summary()?.currency).toBe('EUR');
  });

  it('turns a failure into an organiser-facing message', async () => {
    store.setAsOf('2026-06-21T00:00:00Z');
    await settle();
    http.expectOne(() => true).flush(null, { status: 500, statusText: 'Server Error' });
    await settle();

    expect(store.error()?.message).toContain('Something went wrong');
  });

  it('recovers when the first "now" request fails: retry, then default as usual', async () => {
    await settle();
    http.expectOne(DASHBOARD).flush(null, { status: 0, statusText: 'Unknown Error' });
    await settle();
    expect(store.error()).toBeDefined();

    store.reload();
    await settle();
    // While the retry is in flight the old error is gone and the page shows loading.
    expect(store.error()).toBeUndefined();
    expect(store.isLoading()).toBe(true);
    http.expectOne(DASHBOARD).flush(dashboardResponse());
    await settle();
    http
      .expectOne(`${DASHBOARD}?asOf=2026-06-20T11%3A30%3A00%2B00%3A00`)
      .flush(dashboardResponse());
    await settle();

    expect(store.error()).toBeUndefined();
    expect(store.summary()?.allTime.netEarned).toEqual(gbp(98_864));
  });

  it('never lets a late "now" response override an "as of" the organiser chose meanwhile', async () => {
    await settle();
    http.expectOne(DASHBOARD); // the "now" request is in flight…

    store.setAsOf('2026-06-17T12:00:00Z'); // …when the organiser picks a time
    await settle();
    http.expectOne(`${DASHBOARD}?asOf=2026-06-17T12%3A00%3A00Z`).flush(dashboardResponse());
    await settle();

    expect(store.asOf()).toBe('2026-06-17T12:00:00Z');
  });

  it('keeps the figures on screen while a new "as of" loads', async () => {
    store.setAsOf('2026-06-21T00:00:00Z');
    await settle();
    http.expectOne(() => true).flush(dashboardResponse());
    await settle();

    store.setAsOf('2026-06-17T12:00:00Z');
    await settle();

    expect(store.isUpdating()).toBe(true);
    expect(store.isLoading()).toBe(false);
    expect(store.summary()?.paidOut).toEqual(gbp(44_536));
    http.expectOne(() => true).flush(dashboardResponse());
    await settle();
    expect(store.isUpdating()).toBe(false);
  });

  it('reports an error rather than loading for ever when a success has no body', async () => {
    store.setAsOf('2026-06-21T00:00:00Z');
    await settle();
    http.expectOne(() => true).flush(null);
    await settle();

    expect(store.error()?.message).toContain('returned no data');
    expect(store.isLoading()).toBe(false);
  });
});
