import { TestBed } from '@angular/core/testing';

/**
 * Runs effects and lets the HTTP resource process a flushed response. Tests use this rather than `whenStable()`,
 * which waits for in-flight requests and so can't settle while the test itself still has a request to flush.
 */
export async function flushSignals(): Promise<void> {
  TestBed.tick();
  await new Promise((resolve) => setTimeout(resolve));
  TestBed.tick();
}
