---
name: verify-numbers
description: Prove the dashboard figures are correct - run the golden-figure and invariant tests, then independently cross-check the API output against records.json. Use after any change to domain logic, ingestion or API mapping, and before declaring a task done.
---

# Verify numbers

1. Run `dotnet test backend --filter "Category=Golden|Category=Invariant"`, then the full `dotnet test backend`. Quote the summary line.
2. Start the API and request the summary for a fixed time:
   `GET /api/v1/organisers/{id}/dashboard?asOf=2026-06-21T00:00:00Z`
3. Compare every field with the **Golden figures** derivation table in the README ("How I know the numbers are right"). At `asOf = 2026-06-21`, `po_0001` has arrived,
   so paid out = 44,536, in transit = 0, and still to come = 54,328 (06-16: 7,962 · 06-17: 50,746 · 06-20: −4,380).
4. Do an independent cross-check that doesn't reuse the app code. Recompute gross, fees, refunds and net with a short throwaway
   script (PowerShell `ConvertFrom-Json`, or `jq`) in the scratchpad, de-duplicating by `id`. Never add this script to the repo.
5. Check both identities hold:
   `gross − stripe − ttNet − refunds == net` and `paidOut + inTransit + stillToCome == net`.
6. Report a table: figure | expected | actual | ✔/✘. If anything is ✘, **stop**. Do not edit expected values. Find the cause,
   and if the cause is a genuinely new interpretation, ask the user.
