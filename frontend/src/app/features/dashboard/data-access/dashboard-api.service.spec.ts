import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../../core/api/api-config';
import { DashboardApiService } from './dashboard-api.service';

describe('DashboardApiService', () => {
  function service(baseUrl = ''): DashboardApiService {
    TestBed.configureTestingModule({ providers: [{ provide: API_BASE_URL, useValue: baseUrl }] });
    return TestBed.inject(DashboardApiService);
  }

  it('asks for "now" when no instant is given', () => {
    expect(service().dashboardRequest(undefined).url).toBe(
      '/api/v1/organisers/acct_demo/dashboard',
    );
  });

  it('encodes the offset so "+" is not read as a space by the server', () => {
    const request = service().dashboardRequest('2026-06-21T01:00:00+01:00');

    expect(request.url).toBe(
      '/api/v1/organisers/acct_demo/dashboard?asOf=2026-06-21T01%3A00%3A00%2B01%3A00',
    );
  });

  it('prefixes the configured base URL', () => {
    expect(service('https://api.example').dashboardRequest(undefined).url).toBe(
      'https://api.example/api/v1/organisers/acct_demo/dashboard',
    );
  });
});
