# Architecture & folder structure

This is the single source of truth for where code lives. If a new file doesn't fit anywhere below, ask before inventing a new folder.

## Backend: .NET 10, Controllers + feature services (read-only, so no CQRS and no MediatR; see D-2)

```
backend/
  TicketTailor.Payments.sln
  Directory.Build.props                 shared compiler settings (nullable, warnings-as-errors, LangVersion)
  src/
    Payments.Domain/                    pure business rules: no IO, no clock, no framework
      Shared/                           value types used across the domain
        Money.cs                        readonly record struct (long MinorUnits, Currency)
        Currency.cs
      Records/                          the ledger record model
        BalanceTransaction.cs           abstract base: Id, Created, Amount, Net, AvailableOn, Fees
        Charge.cs
        Refund.cs
        Payout.cs                       + PayoutStatus
        Fee.cs                          Fee + FeeType (StripeFee | TicketTailorFee | Other)
        Ledger.cs                       charges/refunds/payouts, sorted; AsOf(), InCurrency()
      Reporting/                        calculations over the records
        DashboardCalculator.cs          builds the waterfall, paid out, in transit, still to come
        DashboardSummary.cs             the result: + PeriodBreakdown, AvailabilityBucket, PayoutLine
        SalesBreakdown.cs               the waterfall for a set of records (all-time or one period)
        ReportingPeriod.cs              Day | Week | Month + ReportingWindow (rolling, (start, end])
        RefundFeeRule.cs                proportional TT-fee return + rounding (one place)
      DataQuality/
        DataQualityIssue.cs             + DataQualityIssueType
        RecordValidator.cs              orphans, signs, payout mismatch, over-refund, currency, fee rule

    Payments.Application/               feature services: load records, call Domain, map to DTOs
      Abstractions/
        IRecordSource.cs                port (+ OrganiserRecords), implemented in Infrastructure
      Features/                         one folder per feature area
        Dashboard/
          IDashboardService.cs          Task<DashboardResponseDto?> GetSummaryAsync(organiserId, asOf?, ct)
          DashboardService.cs           load -> validate -> calculate -> map
          DashboardMappingExtensions.cs DashboardSummary -> CurrencySummaryDto
          Dtos/                         DTOs used only by this feature
            DashboardResponseDto.cs, CurrencySummaryDto.cs, SalesBreakdownDto.cs,
            PeriodBreakdownDto.cs, AvailabilityBucketDto.cs, PayoutLineDto.cs
      Shared/                           cross-cutting contract primitives
        Dtos/
          MoneyDto.cs                   { amountMinor, currency }
          DataQualityIssueDto.cs
        Mapping/
          MappingExtensions.cs          Money -> MoneyDto, issue -> DTO, enum -> camelCase name
      DependencyInjection.cs            AddApplication(): feature services + TimeProvider.System

    Payments.Infrastructure/            IO adapters
      Records/
        JsonFileRecordSource.cs         implements IRecordSource (the only public entry point); loads once, caches
        RecordJsonParser.cs             internal: JSON text/stream -> Ledger + parse issues (testable without IO)
        RecordJson.cs                   raw JSON shapes (snake_case), internal
        RecordJsonMapper.cs             raw JSON -> Domain records
        RecordDeduplicator.cs
      Options/
        PaymentsOptions.cs              OrganiserId, RecordsPath, MaxFileBytes
      DependencyInjection.cs            AddInfrastructure(config, contentRoot): options (validated on start) + source

    Payments.Api/                       HTTP edge only
      Controllers/
        DashboardController.cs          GET api/v1/organisers/{organiserId}/dashboard?asOf=
      Shared/
        Errors/GlobalExceptionHandler.cs   IExceptionHandler -> ProblemDetails (no details outside Development)
        Extensions/ApiExtensions.cs     AddApi()/UseApi(): CORS, health, HSTS, nosniff
        Options/FrontendOptions.cs      Payments:AllowedOrigin, validated at start-up
        Validation/AsOfParameter.cs     strict asOf parsing: offset required, 2000–2099
      Program.cs                        composition root only
      Payments.Api.http                 sample requests
      appsettings.json                  Payments section (organiser, records path, size limit, allowed origin)

  tests/
    Payments.Tests.Shared/              reused by all test projects
      Builders/TestRecords.cs           factory methods for consistent charges/refunds/payouts (nets derived)
      Fixtures/RealRecords.cs           the real records.json, parsed once
      Fakes/InMemoryRecordSource.cs
      GoldenFigures.cs                  the expected values (derivation in the README), in one place
      ContractIdentities.cs             the D-5 identities checked on the DTOs (catches mapping swaps)
    Payments.Domain.Tests/              calculator, rules, validator, periods, invariants, golden
                                        (reaches Infrastructure transitively via Tests.Shared, for the golden test)
    Payments.Infrastructure.Tests/      parsing, de-duplication, malformed input, file source
    Payments.Application.Tests/         DashboardService over the real data and fixtures
    Payments.Api.Tests/                 WebApplicationFactory (+ FakeTimeProvider): contract, 400/404/405/500,
                                        CORS + preflight, nosniff, health, start-up validation
```

**Dependencies flow one way:** `Api → Application → Domain` and `Infrastructure → Application → Domain`.
Domain depends on nothing. Api references Infrastructure only to call `AddInfrastructure()` in `Program.cs`.

**Request flow:** `DashboardController` → `IDashboardService.GetSummaryAsync` →
`IRecordSource` + `RecordValidator` + `DashboardCalculator` → DTO → JSON.

The system is read-only, so there are no commands and no separate write model. If writes are ever added (e.g. organiser
annotations or payout settings), give them their own service methods. Reconsider CQRS only if reads and writes start to need different models.

## Frontend: Angular, standalone + signals

```
frontend/
  proxy.conf.json                       ng serve: /api -> http://localhost:5214 (no CORS needed in dev)
  src/app/
    app.ts, app.config.ts               root renders the dashboard page; HttpClient (fetch); zoneless
    core/                               app-wide, no UI
      api/
        api-config.ts                   API_BASE_URL and ORGANISER_ID injection tokens
        api-error.ts                    HTTP failure -> organiser-facing message (never shows internals)
    shared/                             reusable, stateless, feature-agnostic
      components/
        stat-card/                      label + money + optional hint
        money/                          <app-money> with a visible − sign as well as colour
        states/                         loading-state, error-state (retry), empty-state
      pipes/
        money.pipe.ts                   minor units + currency -> "£1,234.56" (the only formatting point)
      models/
        money.model.ts                  interface mirroring MoneyDto
        data-quality.model.ts           interface mirroring DataQualityIssueDto
    features/
      dashboard/
        dashboard.page.ts/.html/.scss   container: provides the store, the "as of" control, layout of Q1–Q3
        data-access/
          dashboard-api.service.ts      builds the request (encodes asOf correctly)
          dashboard.store.ts            signals over httpResource: asOf defaulting (A7), period, currency
        models/
          dashboard.models.ts           hand-written interfaces mirroring the C# DTOs (D-12) + period labels
        components/                     presentational: input()/output() only, no HTTP, no store;
                                        each is .component.ts + .html + .scss (no inline templates)
          period-summary/               Q1 cards + period picker
          payout-position/              Q2 cards + payouts table
          availability-table/           Q2 still to come by date ("Available from", never "paid on")
          reconciliation-waterfall/     Q3 gross -> net -> bank, data-problem rows only when non-zero
          data-quality-panel/           excluded issues open, informational ones collapsed
        testing/                        fixtures (golden-shaped responses) and a signal-flush helper for specs
```

## Shared-folder rules (both stacks)

- Code goes in `Shared/` (or `shared/`) only when **two or more features use it today**, or it is a cross-cutting
  primitive like money, errors or date helpers. Do not move code there in case it's needed later.
- Shared code has **no feature knowledge**: it never imports from `Features/` or `features/`.
- Feature-specific DTOs, models and components stay inside their feature folder. Once a second feature needs one, move it to Shared
  in the same change.
- Shared is not a dumping ground. Group it by kind (`Dtos/`, `Mapping/`, `components/`, `pipes/`). Do not use `Helpers.cs` or `utils.ts` catch-alls.
- Test helpers (builders, fixtures, fakes, golden figures) live once in `Payments.Tests.Shared`. Never copy them between test projects.
