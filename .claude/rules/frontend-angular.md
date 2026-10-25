---
paths:
  - "frontend/**"
---

# Angular frontend rules

## Architecture
The folder structure is defined in `docs/ARCHITECTURE.md`. Put every new file where that document says it goes.

- Use standalone components only (no NgModules). Use the `OnPush` change detection strategy everywhere.
- State lives in **signals** (`signal`, `computed`, plus `toSignal` for HTTP). Use RxJS only at the HTTP boundary.
- `core/`: app-wide singletons only (API config token, interceptors). It contains no UI.
- `shared/`: reusable, stateless, feature-agnostic building blocks: `components/` (stat-card, money, loading/error/empty states),
  `pipes/` (money), `models/` (money, data-quality), `utils/`. Shared code never imports from `features/`.
- `features/<name>/`: `<name>.page.ts` (container), `data-access/` (API service + signal store), `models/`, and `components/`
  (presentational). Presentational components take `input()` and emit `output()`. They never inject HTTP or the store.
- Something moves into `shared/` only when a **second** feature needs it. Don't move it there in advance. Avoid catch-all `helpers.ts` files.
- API types are written by hand in `features/<name>/models/` and `shared/models/`, mirroring the backend DTOs field for field
  (D-12). Money is always `{ amountMinor, currency }`. A backend DTO change and its TypeScript interface change go in the same commit.
- The base URL comes from an injection token fed by environment config. Use `proxy.conf.json` for dev. Never hard-code `localhost`.
- Use the file naming convention `kebab-case.<type>.ts` (`summary-cards.component.ts`, `money.pipe.ts`, `dashboard.store.ts`).
- Every component keeps its markup and styles in sibling files: `x.component.ts` + `x.component.html` + `x.component.scss`
  (`templateUrl` / `styleUrl`). Never use an inline `template` or `styles`.
- Styles use only the design tokens and shared classes in `src/styles.scss` (D-11). Never hard-code a colour, font or font size in a component.

## Money & logic
- **Never compute money figures in the frontend.** The UI displays what the API returns. Summing a list for a chart is fine
  only when the API already provides those exact components. Any new figure belongs in the backend.
- Format money with a single shared pipe (built on `Intl.NumberFormat` or `CurrencyPipe`) that takes minor units plus a currency.
- Show negative amounts (refunds, a refund after a payout) clearly with a sign and colour. Don't use colour alone.

## UX
- Every data view has explicit **loading**, **error** and **empty** states.
- The "as of" control is visible, so the organiser knows what "last week" is measured from.
- Label availability as "Available from <date>". Never write "Will be paid on".
- Accessibility: semantic HTML (`<table>` for tabular data, headings in order), labelled controls, AA contrast, keyboard reachable.
  Charts have a text/table equivalent.
- Layout works at 360px width and up.

## Code quality
- Use strict TypeScript (`strict: true`, `noImplicitAny`). Never use `any`. Use `inject()` instead of constructor DI.
- Use the built-in control flow (`@if`, `@for` with `track`). No logic-heavy templates: move logic into `computed()`.
- Keep the dependency footprint small. Justify any chart or UI library as a decision in the README.
