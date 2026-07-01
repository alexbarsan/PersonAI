# Slice Status

Status values: `Not started`, `In progress`, `Blocked`, `Done`.

| Slice | Status | Date completed | Commit | Verification | Notes | Next Step |
| --- | --- | --- | --- | --- | --- | --- |
| S0 | Done | 2026-06-30 | S0 commit | `powershell -ExecutionPolicy Bypass -File scripts/check-s0-structure.ps1` | Normalized monorepo foundation, CI skeleton, and plan docs under `docs/plan/`. | Build S1 walking skeleton API. |
| S1 | Done | 2026-07-01 | S1 commit | `dotnet test api/DreamLens.sln --configuration Release` | Added .NET 9 API solution, health endpoints, development OpenAPI, Dockerfile, CI API test step, and unit/integration test projects. Docker image build was attempted but skipped because the Docker daemon is not running. | Start S2: add PostgreSQL, EF Core 9, migrations, and Testcontainers integration fixture. |
| S2 | Done | 2026-07-01 | S2 commit | `dotnet test api/DreamLens.sln --configuration Release`; `powershell -ExecutionPolicy Bypass -File scripts/check-s0-structure.ps1` | Added PostgreSQL EF Core 9 foundation, initial migration, DbContext, readiness probing, and Testcontainers PostgreSQL fixture. Docker-backed tests skip locally when the Docker daemon is unavailable; CI now runs on Ubuntu for Docker support. | Start S3: add Cognito JWT validation, local dev/test tokens, `ICurrentUser`, and `/v1/me`. |
| S3 | Done | 2026-07-01 | S3 commit | `dotnet test api/DreamLens.sln --configuration Release`; `powershell -ExecutionPolicy Bypass -File scripts/check-s0-structure.ps1` | Added JWT bearer/Cognito auth wiring, testing-only auth scheme, `ICurrentUser`, and `/v1/me`. Test auth headers are ignored outside the `Testing` environment. | Start S4: add encrypted user profile, traits, consent flags, and profile endpoints. |
| S4 | Done | 2026-07-01 | S4 commit | `dotnet test api/DreamLens.sln --configuration Release`; `powershell -ExecutionPolicy Bypass -File scripts/check-s0-structure.ps1` | Added encrypted user profile storage, traits, consent flags, profile endpoints, and EF migration. Sensitive traits are encrypted at rest. | Start S5: add PersonaKit `IChatClient` DeepSeek adapter, fake chat client, resilience/logging decorators. |
| S5 | Done | 2026-07-01 | S5 commit | `dotnet test api/DreamLens.sln --configuration Release`; `powershell -ExecutionPolicy Bypass -File scripts/check-s0-structure.ps1` | Added PersonaKit project, `IChatClient` DeepSeek adapter, fake chat client, Polly resilience decorator, usage logging decorator, and DreamLens API registration. | Start S6: add persona config loading, prompt rendering, and schema validation. |
| S6 | Done | 2026-07-01 | S6 commit | `dotnet test api/DreamLens.sln --configuration Release`; `powershell -ExecutionPolicy Bypass -File scripts/check-s0-structure.ps1` | Added PersonaKit persona registry, strict Scriban prompt renderer, JsonSchema.Net output validator, DreamLens persona assets, and Verify prompt snapshot coverage. | Start S7: add pseudonymized Context JSON builder. |
| S7 | Done | 2026-07-01 | S7 commit | `dotnet test api/DreamLens.sln --configuration Release`; `powershell -ExecutionPolicy Bypass -File scripts/check-s0-structure.ps1` | Added PersonaKit Context JSON v1 builder, HMAC pseudonym service, consent-gated traits/history, dream input validation/capping, and snapshot coverage. | Start S8: add interpretation pipeline orchestration. |
| S8 | Not started |  |  |  |  | Add interpretation pipeline orchestration. |
| S9 | Not started |  |  |  |  | Add dream submit and get endpoints. |
| S10 | Not started |  |  |  |  | Add journal and insights endpoints. |
| S11 | Not started |  |  |  |  | Add rate limits, quotas, abuse protection, and AI cost ledger. |
| S12 | Not started |  |  |  |  | Scaffold Expo app with router, theme, API client, auth, and mock mode. |
| S13 | Not started |  |  |  |  | Add onboarding wizard and profile UI. |
| S14 | Not started |  |  |  |  | Add dream capture and result screens. |
| S15 | Not started |  |  |  |  | Add journal and insights UI. |
| S16 | Not started |  |  |  |  | Add Maestro and Playwright E2E harness. |
| S17 | Not started |  |  |  |  | Add Terraform AWS infrastructure. |
| S18 | Not started |  |  |  |  | Add CI/CD pipelines for API, web, mobile, and infra. |
| S19 | Not started |  |  |  |  | Add observability, hardening, and k6 load tests. |
| S20 | Not started |  |  |  |  | Prove PersonaKit reuse with Astra config. |
| S21 | Not started |  |  |  | Optional monetization slice. | Decide whether monetization is in v1, then add tiers/paywall if approved. |
