import { HttpErrorResponse } from '@angular/common/http';

/** What the UI needs to know about a failed request: a message a person can act on. */
export interface ApiError {
  readonly status: number;
  readonly message: string;
}

/** Turns an HTTP failure (ProblemDetails body or not) into an organiser-facing message. Never shows internals. */
export function toApiError(error: unknown): ApiError {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, message: 'Something went wrong while loading your payments.' };
  }

  switch (error.status) {
    case 0:
      return {
        status: 0,
        message: "We couldn't reach the payments service. Check your connection and try again.",
      };
    case 400:
      return {
        status: 400,
        message: problemTitle(error) ?? 'That date and time could not be used.',
      };
    case 404:
      return { status: 404, message: "We couldn't find payments for this account." };
    default:
      return {
        status: error.status,
        message: 'Something went wrong while loading your payments. Please try again.',
      };
  }
}

function problemTitle(error: HttpErrorResponse): string | undefined {
  const body: unknown = error.error;
  if (typeof body === 'object' && body !== null && 'errors' in body) {
    const errors = (body as { errors: Record<string, string[]> }).errors;
    return Object.values(errors).flat()[0];
  }
  return undefined;
}
