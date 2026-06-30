# Slices S12-S16

These prompts build the Expo app and E2E harness. `decision-record.md` is authoritative.

## S12 - Expo Scaffold, Router, Theme, API Client, Auth, Mock Mode

Goal: create the cross-platform app foundation.

Tests first:

- Component test renders the initial route.
- API client test calls MSW mock endpoint.
- Auth state test handles signed-out and signed-in states.
- Theme provider renders without platform-specific failures.

Implementation:

- Create Expo React Native TypeScript app in `app/`.
- Add Expo Router.
- Add TanStack Query and Zustand.
- Add base theme and brand config for DreamLens.
- Add API client with typed DTOs.
- Add mock API mode with MSW.
- Add test setup for RNTL.

Verification:

- `npm test` in `app/`
- Start Expo web if practical and smoke test manually or with Playwright later.

Commit:

- `feat(S12): scaffold Expo app`

## S13 - Onboarding Wizard And Profile UI

Goal: collect profile traits and consent.

Tests first:

- Form validation rejects invalid age and required consent omissions.
- User can complete onboarding in mock mode.
- Profile save sends expected DTO.
- Disclaimer is visible during onboarding.

Implementation:

- Add onboarding routes.
- Add profile form using react-hook-form and zod.
- Add consent controls for AI processing, sensitive traits, and history use.
- Add profile query/mutation hooks.
- Persist incomplete drafts locally with Zustand if useful.

Verification:

- `npm test` in `app/`

Commit:

- `feat(S13): add onboarding and profile UI`

## S14 - Dream Capture And Result Screens

Goal: submit a dream and render the generic result sections.

Tests first:

- Dream capture validates text length.
- Submit calls mock API and navigates to result.
- Generic renderer handles `text`, `symbols`, `emotions`, and `list`.
- Result screen shows disclaimer.
- Elevated safety response renders constrained safety UI.

Implementation:

- Add dream capture route.
- Add loading state for synchronous AI call.
- Add result detail route.
- Add generic section renderer.
- Add safety card.
- Add error states for validation, quota, auth, and provider failure.

Verification:

- `npm test` in `app/`

Commit:

- `feat(S14): add dream capture and result UI`

## S15 - Journal And Insights UI

Goal: let users review past dreams and see basic insights.

Tests first:

- Journal list renders mock dreams.
- Journal detail renders a stored interpretation.
- Empty states render cleanly.
- Insights screen renders themes and streaks.
- Delete behavior is covered if delete endpoint is in scope.

Implementation:

- Add journal list and detail routes.
- Add insights route.
- Add query hooks and mock handlers.
- Add simple charts or visual summaries using existing React Native-compatible libraries only if already justified by the app stack.

Verification:

- `npm test` in `app/`

Commit:

- `feat(S15): add journal and insights UI`

## S16 - UI E2E Harness

Goal: add mobile and web E2E coverage and CI wiring.

Tests first:

- Playwright web happy path: onboarding, submit dream, view result.
- Playwright web error path: quota or provider failure.
- Maestro mobile happy path with mock API mode.

Implementation:

- Add Playwright config for Expo web.
- Add Maestro flows.
- Add stable test ids where useful.
- Add CI scripts for component and E2E suites.
- Ensure mock mode is deterministic.

Verification:

- `npm test` in `app/`
- `npx playwright test`
- `maestro test` when Maestro is installed; otherwise document environment blocker.

Commit:

- `feat(S16): add UI E2E harness`
