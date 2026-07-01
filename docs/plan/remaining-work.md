# Remaining Work

Last updated after S13 on 2026-07-01.

## Planned Slices

- S14: dream capture validation, submit flow, result screen, and generic section renderer.
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
- UI routes beyond onboarding/profile are still placeholders until S14-S16.
- Profile form uses simple text inputs for comma-separated traits; richer controls can be added after the core flows are complete.
