# Ticket Tailor – Organiser Payments Dashboard

Tech test: an ASP.NET Core (.NET 10) API with an Angular frontend. It reports an organiser's sales, fees,
refunds, payouts and future availability using `records.json`. The problem, the golden figures (with their
derivation) and every decision (A1–A11, D-n) are in @README.md. Read it before touching domain logic.

## Working agreement (non-negotiable)

1. **Plan before code.** For any non-trivial change, write a short plan (files, approach, tests) and get
   the user's explicit approval before editing source. Rules, docs and config only change when the user asks.
2. **Timebox.** The brief allows ~3 hours. Prefer the smallest design that is correct and readable.
   Do not add features, layers or packages the brief doesn't need. If something can't be done in time, put it in the README's "Next steps" section.
3. **Numbers must be proven.** Any change that affects a money figure must keep the golden-figure tests
   passing (`/verify-numbers`). Never "fix" a test by changing its expected value without asking the user.
4. **Ambiguities are decisions.** When the data or rules are unclear, don't guess silently. Propose a behaviour,
   then record it in the README's decisions section (`/record-decision`).
5. **Review before done.** Before declaring a task complete, run the relevant reviewer subagent(s):
   `payments-correctness-reviewer` for any domain/money change, `dotnet-reviewer` for backend changes,
   `angular-reviewer` for frontend changes.

## Layout

The full tree and the rules for the shared folders are in @docs/ARCHITECTURE.md. Follow it exactly. Ask before adding a new top-level folder.

```
backend/src/Payments.Domain          pure logic, no IO / clock / framework refs
backend/src/Payments.Application     Features/<Area>/ (IXxxService + XxxService + Dtos/), Shared/ (reused DTOs, mapping)
backend/src/Payments.Infrastructure  record loading, de-duplication
backend/src/Payments.Api             Controllers/ (thin) + Shared/ (errors, extensions)
backend/tests/Payments.Tests.Shared  builders, fixtures, golden figures reused by all test projects
frontend/src/app/{core,shared,features/dashboard}
docs/                                ARCHITECTURE (folder structure), images/
README.md                            the problem, golden figures, decisions, security, trade-offs
```

Pattern: **Controller → feature service (`IDashboardService`) → Domain** (D-2).

## Commands

- Backend: `dotnet build backend` · `dotnet test backend` · `dotnet run --project backend/src/Payments.Api`
- Frontend: `cd frontend && npm ci` · `npm start` · `npm test` · `npm run lint`

## Tooling (already configured)

- **Playwright MCP** (`.mcp.json`): use it to open the running dashboard, take screenshots and check the UI states before finishing frontend work.
- **Git**: work on a feature branch. Run `/code-review` and `/security-review` before the submission zip is made.
- **API types**: the Angular models in `features/dashboard/models` and `shared/models` mirror the C# DTOs by hand (D-12). Change both together.

## Detailed rules

Path-scoped rules are in `.claude/rules/`: money and domain logic (always applies), .NET backend, Angular frontend,
testing, security. Reusable workflows are in `.claude/skills/`. Reviewer subagents are in `.claude/agents/`.
