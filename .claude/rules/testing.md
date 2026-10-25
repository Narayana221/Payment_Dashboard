# Testing rules

- **Golden-figure test.** One end-to-end domain test loads the real `records.json` and asserts every figure in
  the README ("How I know the numbers are right"). If it fails, the code is wrong until proven otherwise. Do not change expected values
  without the user's explicit approval and a matching decision in the README.
- **Edge cases each get their own small test** with a hand-built fixture: duplicate (identical), duplicate (conflicting),
  out-of-order input, partial refund with rounding, refund after payout, orphan refund, payout total mismatch,
  over-refund, mixed currency, non-succeeded charge, and period boundaries (exactly at `asOf − 7d`).
- **Invariant tests** for both identities in `money-and-domain.md`, run against every fixture.
- Backend: use xUnit with plain `Assert` (or FluentAssertions if it's already a dependency). Name tests `Method_Scenario_Expected`.
  Integration tests use `WebApplicationFactory` with a fixed `FakeTimeProvider`.
- Frontend: component tests cover loading, error and empty states, plus a test that the money pipe formats negatives and pence correctly.
  Do not re-test backend maths in the frontend.
- Tests are deterministic: no real clock, no network, no test-order dependence.
- Before a task is "done": `dotnet test backend` and `npm test` (headless) both pass, and the output is quoted in your report.
