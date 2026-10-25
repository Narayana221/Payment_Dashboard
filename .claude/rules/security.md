# Security rules

- **Tenant scoping by design.** Every query is for an organiser (connected account) id, even though the fixture has only one.
  The service signature takes `organiserId`. A production version would resolve it from the authenticated principal, never from a client-supplied value. State this in the README.
- No secrets in the repo. Configuration goes in `appsettings*.json` (non-secret values only) or user-secrets and environment variables.
- CORS: allow only the configured frontend origin. Never use `AllowAnyOrigin` together with credentials.
- Validate all input: query params are parsed with bounds, and unknown values give a `400 ProblemDetails`.
- Treat `records.json` as untrusted input. Use size limits, tolerate malformed records (report them, don't crash), and never
  `eval` or reflect on its content.
- Never log or return buyer PII. `order_ref` may be shown to the organiser but is never logged.
- Security headers: HTTPS redirection and HSTS outside Development, plus `X-Content-Type-Options: nosniff`.
- The API is read-only (GET only). There are no endpoints that mutate state.
- Angular: never use `bypassSecurityTrust*` or `innerHTML` with API data.
- The README has a "Security considerations" section listing the points above and what is deliberately out of scope (authentication).
