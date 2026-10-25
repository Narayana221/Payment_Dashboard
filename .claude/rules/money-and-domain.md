# Money & domain rules (always apply)

## Representing money
- Money is **`long` in minor units plus an ISO currency code**, wrapped in a `Money` value type (`readonly record struct`).
  Never use `double` or `float`, and never use `decimal` for stored amounts. In TypeScript, amounts stay as integer `number` pence until display.
- Arithmetic between different currencies throws. Totals are always grouped by currency.
- Any division (e.g. a proportional fee) happens in exactly **one** named function with an explicit, documented
  `MidpointRounding` mode, and has unit tests for the rounding cases.
- Formatting to "£1,234.56" only happens at the UI edge (Angular `CurrencyPipe` or a single `money` pipe). The API returns minor units.

## Source of truth
- The record's own `net` and `fee_details` describe what actually moved. Use them for totals. Recompute fee rules
  only to **validate**, and report mismatches as data-quality issues. Never silently overwrite.
- Sign conventions follow the file: charge nets are positive, refund nets are negative, a returned TT fee is a negative `application_fee` on the refund.
- `application_fee` = Ticket Tailor fee, `stripe_fee` = Stripe fee. Map these strings in one place (an enum plus a parser).
  An unknown fee type is a data-quality issue, not an exception that crashes the request.

## Ingestion
- Never trust file order. De-duplicate by `id` (identical duplicates are dropped and counted; conflicting duplicates
  are excluded and reported). Sort by `created`, then `id`.
- Validate referential integrity: a refund points to a known charge, a payout's ids exist, payout.amount = Σ included nets,
  Σ refunds ≤ charge amount, and the currency is consistent. Report failures in a `DataQualityReport`. Do not throw, and do not drop records silently.
- Only `status == "succeeded"` charges count toward sales.

## Time
- Never call `DateTime.Now`, `DateTime.UtcNow` or `DateTimeOffset.Now` in domain or application code. Inject `TimeProvider`
  or pass an explicit `asOf`.
- Timestamps are `DateTimeOffset` (UTC). `available_on` and `arrival_date` are `DateOnly`.
- Periods (day, week, month) are rolling windows ending at `asOf`, and are defined in one place.
- Attribute amounts to periods by the record's `created` time.

## Reporting semantics (see the README's decisions for the reasoning)
- Net earned = Σ net(charges) + Σ net(refunds).
- Paid out = payouts with status `paid` and `arrival_date ≤ asOf`. Payouts that haven't arrived yet are "in transit".
- Still to come = net earned − paid out − in transit, grouped by `available_on`. Negative buckets are allowed (e.g. a refund after a payout).
- **Never** show a predicted payout date. Only show availability.
- Every summary must satisfy (D-5): `gross − stripeFees − ttFeesNet − otherFees − refunds + unexplainedDifference == netEarned`
  and `paidOut + inTransit + stillToCome + payoutDiscrepancy == netEarned`. On clean data the reconciliation lines are zero,
  so the plain identities hold exactly. Assert `Invariants.AssertBalances` on every fixture and `AssertClean` on clean ones.
- Sign rules: charge amounts and fees are ≥ 0; a refund has amount > 0, net ≤ 0 and a Ticket Tailor fee ≤ 0.
  Wrong signs are excluded as `InvalidAmount`.
