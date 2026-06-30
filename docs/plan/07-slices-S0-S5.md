# Slices S0-S5

These prompts execute the foundation, identity, and first AI-provider slices. `decision-record.md` is authoritative.

## S0 - Monorepo, CI Skeleton, Status, Plan Docs

Goal: normalize repository structure and create the foundation for future slices.

Tests first:

- Add a lightweight repository-structure check script or test that fails until required directories/files exist.
- Check for `api/`, `app/`, `infra/`, `personas/`, `docs/plan/`, `.gitignore`, and `SLICE-STATUS.md`.

Implementation:

- Move plan docs into `docs/plan/` if they are still at repo root.
- Keep a short root `README.md` that points to `docs/plan/readme.md`.
- Add `.gitignore` for .NET, Node, Expo, Terraform, test output, secrets, and OS files.
- Create empty top-level directories: `api/`, `app/`, `infra/`, `personas/`.
- Update `SLICE-STATUS.md` with S0 in progress/done.
- Add initial CI skeleton files if no buildable projects exist yet. CI can start as format/status checks and become stricter in later slices.

Verification:

- Run the structure check.
- Run `git status --short`.

Commit:

- `feat(S0): initialize monorepo foundation`

## S1 - Walking Skeleton API

Goal: create the .NET 9 API solution with health endpoints, OpenAPI, slice conventions, and Dockerfile.

Tests first:

- Unit/integration test `GET /health/live` returns 200.
- Integration test `GET /health/ready` returns 200 without external dependencies.
- Test OpenAPI document is exposed in development.

Implementation:

- Create `api/DreamLens.sln`.
- Create `src/DreamLens.Api`.
- Create `tests/DreamLens.Api.Tests` and `tests/DreamLens.Api.IntegrationTests`.
- Add Minimal API startup with endpoint grouping conventions.
- Add health endpoints.
- Add OpenAPI.
- Add Dockerfile for API.
- Add basic appsettings and environment-specific config.

Verification:

- `dotnet test api/DreamLens.sln`
- Build Dockerfile if Docker is available; otherwise document skipped verification.

Commit:

- `feat(S1): add walking skeleton API`

## S2 - PostgreSQL, EF Core 9, Testcontainers

Goal: add database infrastructure and prove integration tests can run against PostgreSQL.

Tests first:

- Integration test starts PostgreSQL Testcontainer and applies migrations.
- Test `DreamLensDbContext` can write and read a simple migration-backed entity.
- Readiness health check reflects database availability when configured.

Implementation:

- Add EF Core 9 packages and Npgsql provider.
- Create `DreamLensDbContext`.
- Add initial migration.
- Add test fixture for PostgreSQL Testcontainers.
- Wire connection strings through options/config.
- Keep schema minimal; feature tables arrive in their slices.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S2): add postgres persistence foundation`

## S3 - AuthN/AuthZ, Cognito JWT, /v1/me

Goal: add authentication, authorization, current-user abstraction, and `/v1/me`.

Tests first:

- Unauthenticated request to `/v1/me` returns 401.
- Authenticated dev/test token returns stable user info.
- Handler uses `ICurrentUser`, not raw HTTP context.

Implementation:

- Configure JWT bearer auth.
- Add Cognito options for production.
- Add local dev/test auth support with `dotnet user-jwts` compatible claims.
- Add `ICurrentUser`.
- Add `/v1/me`.
- Ensure production does not enable test auth.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S3): add authentication and current user`

## S4 - User Profile, Consent, Column Encryption

Goal: store profile, traits, consent flags, and encrypted sensitive fields.

Tests first:

- `GET /v1/profile` returns empty/default profile for a new user.
- `PUT /v1/profile` validates and persists profile fields.
- Sensitive traits are encrypted at rest.
- Consent flags persist and control profile/context availability.
- User A cannot read or write User B profile.

Implementation:

- Add profile entity and migration.
- Add profile endpoints.
- Add validation for age, sex, language, timezone, trait lengths, and consent flags.
- Add AES-GCM column encryption abstraction.
- Use local encryption key in dev/test config.
- Add no raw sensitive values to logs.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S4): add encrypted profile and consent`

## S5 - DeepSeek IChatClient And Resilience Decorators

Goal: add PersonaKit provider abstraction with DeepSeek adapter and cross-cutting decorators.

Tests first:

- Fake chat client returns deterministic responses for tests.
- DeepSeek adapter sends OpenAI-compatible request shape to WireMock.Net.
- Timeout policy is configured.
- 429/5xx responses are retried twice with jitter.
- Circuit breaker opens after configured failures.
- Usage logging records model, latency, tokens, and estimated cost without prompt text.

Implementation:

- Add `PersonaKit` project.
- Add `IChatClient` registration.
- Add `DeepSeekOptions`.
- Add DeepSeek adapter for `https://api.deepseek.com` and model `deepseek-chat`.
- Add Polly v8 decorator pipeline.
- Add usage logging decorator.
- Add `FakeChatClient`.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S5): add DeepSeek chat client foundation`
