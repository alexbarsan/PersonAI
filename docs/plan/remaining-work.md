# Remaining Work

Last updated after S14 on 2026-07-01.

## Planned Slices

- S15: journal and insights UI.
- S16: Maestro mobile E2E and Playwright web E2E harness.
- S17: Terraform AWS infrastructure.
- S18: CI/CD pipelines for API, web, mobile, and infra.
- S19: observability, hardening, and k6 load tests.
- S20: prove PersonaKit reuse with Astra configuration.
- S21: optional monetization decision and implementation if approved.

## Known Gaps

- Expo typed routes are disabled because the current installed Expo CLI/router pair failed during typed-route generation.
- The Expo app is on SDK 56 locally because SDK 57 produced a `jest-expo` / React Native peer conflict during install.
- `npm test` uses `--forceExit` because the Expo/RN Jest environment leaves an open handle after tests complete.
- `npm install` reports moderate third-party audit findings; no forced audit fix has been applied.
- Production Cognito OAuth is scaffolded but not fully wired to hosted Cognito configuration or secure token persistence.
- UI routes beyond onboarding/profile/dream capture/result are still pending until S15-S16.
- Journal, insights, and E2E routes are still pending until S15-S16.
- Dream result detail currently uses an in-memory submitted-result cache before falling back to `GET /v1/dreams/{id}`; S15 should align journal detail navigation with persisted results.
- Profile form uses simple text inputs for comma-separated traits; richer controls can be added after the core flows are complete.
