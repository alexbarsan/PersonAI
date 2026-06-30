# Testing Strategy

`decision-record.md` is authoritative. Every slice follows red, green, refactor.

## Principles

- Write the listed tests first.
- Confirm they fail for the expected reason.
- Implement the minimum production code to pass.
- Refactor with tests green.
- Run the full relevant suite before completing a slice.
- Do not proceed to the next slice on red.

## Backend Test Pyramid

Unit tests:

- xUnit
- FluentAssertions
- NSubstitute
- deterministic clocks and ids
- no network or database

Integration tests:

- WebApplicationFactory
- Testcontainers PostgreSQL
- WireMock.Net for DeepSeek
- real DI configuration with test overrides

Snapshot tests:

- Verify for rendered prompts
- Verify for Context JSON v1
- Verify for mapped UI Response DTO sections where useful

Load tests:

- k6 for key endpoints after the app has meaningful behavior

## Frontend Tests

Component tests:

- RNTL
- form validation behavior
- generic section renderer variants
- onboarding and dream capture state

Mock network:

- MSW
- deterministic DTO fixtures
- success, validation error, auth error, provider failure, quota exceeded

E2E:

- Maestro for mobile flows
- Playwright for web flows

## Contract Testing

Canonical schemas in `decision-record.md` must stay aligned with:

- PersonaKit context models
- persona output schemas
- API response DTOs
- frontend TypeScript types
- MSW fixtures
- E2E assertions

When a schema changes, update the decision record first.

## AI Boundary Tests

Required tests:

- prompt rendering includes injection-firewall language
- dream text is treated as data
- invalid AI JSON triggers one repair retry
- second invalid response returns friendly failure
- provider 429 and 5xx use retry policy
- timeout and circuit breaker behavior are observable
- usage logging records tokens, latency, model, persona, and cost estimate

## Privacy Tests

Required tests:

- context never includes email, name, phone, Cognito `sub`, IP, or device ids
- `pseudonymId` is stable for a user and not equal to raw identifiers
- sensitive traits are omitted when consent is false
- history is omitted when history consent is false
- logs do not contain raw dream text in tested paths
- export and erasure behavior is covered before release

## CI Quality Gates

Minimum early gates:

```powershell
dotnet test api/DreamLens.sln
```

After app exists:

```powershell
npm test
npx playwright test
```

After mobile E2E exists:

```powershell
maestro test app/e2e
```

After infra exists:

```powershell
terraform fmt -check
terraform validate
```

Commands may be wrapped by package scripts during S0/S12/S17.

## Test Data

Use synthetic data only. Do not include real dreams, real user profiles, real secrets, or production provider payloads in the repository.

## Slice Completion

A slice is complete only when:

- tests were written first and observed failing
- production code is implemented
- relevant full suite passes
- docs/status are updated
- one conventional commit is created if the user has asked the agent to run the full slice workflow
