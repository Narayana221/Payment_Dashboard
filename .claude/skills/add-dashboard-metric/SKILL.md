---
name: add-dashboard-metric
description: End-to-end recipe for adding or changing a dashboard figure, period or breakdown (domain -> API DTO -> Angular client -> component -> tests). Use when asked to add a metric, a new time window, a new fee type, a new breakdown, or similar small feature - including the live change in the follow-up interview.
---

# Add a dashboard metric (vertical slice)

Keep the change contained: one domain function, one DTO field, one UI element, and tests at each layer.

1. **Clarify.** Say the metric in one sentence in the organiser's language, plus its formula in terms of record fields.
   Check where it sits in the waterfall (see the `payments-domain` skill). If it's ambiguous, propose a behaviour and wait for the user's approval.
2. **Domain** (`Payments.Domain/Reporting`): add a pure function or property on `DashboardCalculator`. Use `Money`, not raw numbers. Respect `asOf`.
3. **Domain test**: write a small fixture using the builders in `Payments.Tests.Shared` that isolates the case. Add the known value to
   `GoldenFigures.cs` if the real dataset has one.
4. **Application**: add the field to the feature DTO (`Features/Dashboard/Dtos/DashboardSummaryDto.cs`) using `MoneyDto`,
   and map it in `DashboardService`. Make additive changes only, and never rename existing fields. If it needs a **new** endpoint, add a
   method to the feature's service interface and implementation, plus a thin controller action. For a new feature area, create
   `Features/<Area>/` with `IXxxService`, `XxxService` and `Dtos/`, and register it in `DependencyInjection.cs`.
   Reuse `Application/Shared` DTOs and mapping. Don't duplicate them.
5. **API test**: update or add a `WebApplicationFactory` test for the response shape.
6. **Frontend**: add the field to `features/dashboard/models/dashboard-summary.model.ts`, then render it in the relevant presentational
   component with `shared/` building blocks (stat-card, money). Handle a zero or negative value. Update the component test.
7. **Docs**: if a decision was made, use `/record-decision`. Update the README if the metric changes what the organiser sees.
8. **Verify**: run `/verify-numbers`, then the `payments-correctness-reviewer` agent, plus `dotnet-reviewer` / `angular-reviewer` for whichever side changed.

Typical examples: "add a calendar-month view", "show average order value", "split refunds into full and partial",
"add a 'pending payout' count", "show fees as % of gross".
