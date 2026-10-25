---
name: payments-domain
description: Domain knowledge for the organiser payments dashboard - record shapes (charge/refund/payout), fee rules, reconciliation formulas, edge cases and golden figures. Use whenever writing or reviewing code that parses records.json, computes money figures, periods, availability or payouts.
---

# Payments domain

## Read first
- `README.md`: the problem, the golden figures with their derivation, and every decision (A1–A11, D-n) with its agreed behaviour.
- `.claude/rules/money-and-domain.md`: the hard rules.

## Mental model
A ledger of **balance transactions** (charges +, refunds −), each with a `net` and an `available_on`.
Payouts move a set of those transactions (`included_balance_transactions`) from the Stripe balance to the bank.

```
gross sales
 − Stripe fees                  (never returned)
 − Ticket Tailor fees charged
 + Ticket Tailor fees returned  (proportional to the amount refunded)
 − refunds issued
 = net earned
 = paid out (arrived) + in transit + still to come (grouped by available_on, may be negative)
```

## Procedure for any change that touches money
1. Find which line of the waterfall the change affects. If it fits no line, stop and ask the user. It probably needs a decision.
2. Put the logic in `Payments.Domain`, in a pure function that takes records and `asOf`.
3. Add or extend a small fixture test for the specific case, plus the invariant assertions.
4. Run `/verify-numbers`. The golden figures must still match.
5. If behaviour for an unclear case was chosen, run `/record-decision`.

## Known traps in this dataset
- `ch_0005` and `re_0002` are duplicated. Totals that come out at 116,250 + 800 gross, or refunds of 12,250, mean de-duplication is broken.
- `re_0003` is a partial refund (2,250 of 4,500). If TT fees returned ≠ 320 total, the proportional logic is wrong.
- `re_0004` refunds a charge that was already paid out. It must make the 2026-06-20 bucket −4,380 and leave paid out unchanged.
- ch_0027–0030 became available on 06-16 but are not in `po_0001`. They belong in still to come, not paid out.
- `po_0001` arrives on 2026-06-18. With `asOf` before that, it is in transit.
- The data is from June 2026. A clock-based "last week" will be empty, so always drive tests with a fixed `asOf`.
