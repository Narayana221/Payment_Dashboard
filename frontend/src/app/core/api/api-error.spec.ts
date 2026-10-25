import { HttpErrorResponse } from '@angular/common/http';
import { toApiError } from './api-error';

describe('toApiError', () => {
  it('explains a network failure', () => {
    expect(toApiError(new HttpErrorResponse({ status: 0 })).message).toContain("couldn't reach");
  });

  it('uses the validation message from a 400 ProblemDetails', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: { asOf: ['asOf must have a UTC offset.'] },
      },
    });

    expect(toApiError(error).message).toBe('asOf must have a UTC offset.');
  });

  it('never shows server internals for a 500', () => {
    const error = new HttpErrorResponse({
      status: 500,
      error: { detail: 'NullReferenceException at Payments.Foo' },
    });

    expect(toApiError(error).message).not.toContain('Exception');
  });
});
