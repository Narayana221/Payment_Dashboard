---
paths:
  - "backend/**/*.cs"
  - "backend/**/*.csproj"
  - "backend/**/*.json"
  - "backend/**/*.props"
---

# .NET backend rules

The folder structure is defined in `docs/ARCHITECTURE.md`. Put every new file where that document says it goes.

## Project boundaries
- `Payments.Domain` references nothing except the BCL. It contains no ASP.NET, no `System.IO`, no `HttpClient`, no clock and no logging.
- `Payments.Application` references Domain only. It holds feature services (`Features/<Area>/`), ports (`Abstractions/`)
  and reused DTOs and mapping (`Shared/`).
- `Payments.Infrastructure` references Application and Domain. It implements ports such as `IRecordSource`.
- `Payments.Api` is the HTTP edge and composition root. `Program.cs` only wires things up (`AddApplication()`, `AddInfrastructure()`, middleware).
- Each project exposes one `DependencyInjection.cs` with an `Add<Layer>()` extension. Do not register services ad hoc in `Program.cs`.

## Controllers + feature services (read-only app: no CQRS, no MediatR)
- Use controllers with `[ApiController]` and attribute routing under `api/v1/...`. Each controller is grouped by resource (`DashboardController`).
- **Controllers are thin**: bind route and query, call one service method, and return `Ok(dto)`. A controller contains no LINQ over records and no money maths.
- Each feature area has one interface and one implementation (`IDashboardService` / `sealed DashboardService`), injected through the controller
  constructor. Add a method for each new read. Split into a second service only when a method group has a clearly different responsibility.
- Services orchestrate: load records via `IRecordSource`, validate, call Domain, and map to DTOs. Business rules live in Domain, not in services.
- Services don't call other feature services. Shared logic belongs in Domain or `Application/Shared`.
- Register services in `Application/DependencyInjection.cs`.
- Cross-cutting concerns (validation, errors, logging) live in the API layer: `[ApiController]` model validation, data annotations
  or a small validator, and `GlobalExceptionHandler`.
- Do not add CQRS abstractions (`IQueryHandler`, commands), MediatR, AutoMapper or similar indirection without an approved decision
  in the README.

## DTOs and the Shared folder
- Services return **DTOs, never domain types**. Feature-specific DTOs live in the feature folder.
  A DTO, mapper or helper moves to `Application/Shared/` only when a **second** feature needs it (e.g. `MoneyDto`, `PeriodTotalsDto`).
- Money in DTOs is always `MoneyDto { long AmountMinor, string Currency }`. Never use a bare `long` or a formatted string.
- Mapping lives in explicit extension methods (`ToDto()`) in `Shared/Mapping` or next to the feature DTO. There is no reflection mapping.
- Shared code never references `Features/`. Do not create catch-all `Helpers` or `Utils` classes.

## Style
- `Directory.Build.props` sets the defaults for every project: .NET 10, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<ImplicitUsings>enable</ImplicitUsings>`.
  Use file-scoped namespaces that match the folders (`Payments.Application.Features.Dashboard`).
- Prefer immutable `record`/`record struct` types, `required`/`init` members, and `IReadOnlyList<T>` on public surfaces. Make classes `sealed` by default.
- Use the type system to model record kinds (abstract `BalanceTransaction` with `Charge` and `Refund` subtypes, plus `Payout`). Never pass `object` or a bag of nullable fields.
- Raw JSON shapes (`snake_case`) are `internal` to Infrastructure and mapped straight into domain types. Use `System.Text.Json` with `JsonNamingPolicy.SnakeCaseLower`.
- Name things in the organiser's language: `GrossSales`, `NetEarned`, `StillToCome`, `AvailableOn`.
- No `static` mutable state. Use correct DI lifetimes (`IRecordSource` is a singleton that loads once and caches; feature services are scoped).

## API
- Validate query params (`asOf` is ISO-8601 and within sane bounds). Invalid input returns `400 ProblemDetails`.
- Unhandled errors go through `GlobalExceptionHandler` (`IExceptionHandler`) to `ProblemDetails`. Never return stack traces outside Development.
- Declare `[ProducesResponseType]` on actions and add `/health`. No OpenAPI document is published (D-12).
- Configuration is bound through `IOptions<PaymentsOptions>` (`Payments:RecordsPath`, `Payments:AllowedOrigin`). Never hard-code paths or origins.

## Async & performance
- Use `async` all the way down for IO. Never use `.Result` or `.Wait()`. Pass a `CancellationToken` from the action through the service to IO.
- Readable LINQ beats premature optimisation, but build `Dictionary<string, …>` indexes for id joins.

## Logging
- Use `ILogger<T>` with structured templates (`"Loaded {Count} records"`). Never log full records or buyer data.
