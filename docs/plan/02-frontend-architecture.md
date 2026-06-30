# Frontend Architecture

`decision-record.md` is authoritative. This document defines the Expo app shape for iOS, Android, and Web.

## Stack

- Expo with React Native and TypeScript.
- Expo Router for navigation.
- TanStack Query for server state.
- Zustand for local UI state.
- react-hook-form and zod for forms and validation.
- MSW for mock API mode.
- RNTL for component tests.
- Maestro for mobile E2E.
- Playwright for web E2E.

## App Shape

```text
app/
  app/
    _layout.tsx
    index.tsx
    (auth)/
    (tabs)/
      dreams/
      journal/
      insights/
      profile/
  src/
    api/
    auth/
    components/
    features/
      onboarding/
      dreams/
      journal/
      insights/
      profile/
    personas/
    state/
    theme/
    test/
```

The first screen after auth should be the usable DreamLens experience, not a marketing page.

## Routes

Initial app routes:

- onboarding wizard
- dream capture
- dream result
- journal list
- journal detail
- insights
- profile and consent settings

Web should use real DOM output through React Native Web and must be covered by Playwright.

## Auth

Production auth uses Cognito OAuth code flow with PKCE through `expo-auth-session`. Tokens are stored with platform-appropriate secure storage. The API client attaches the access token to requests.

Development supports mock auth and local API dev tokens so UI work can proceed without deployed Cognito.

## API Client

Generate or maintain a typed API client from the backend OpenAPI contract once S1 exposes OpenAPI. Until then, keep client types hand-written and close to the canonical DTOs in `decision-record.md`.

TanStack Query owns API request state, cache invalidation, loading states, retry policy, and error states. Zustand owns local draft state such as incomplete onboarding and dream draft text.

## Forms

Use react-hook-form with zod schemas for:

- onboarding profile fields
- consent settings
- dream capture
- profile editing

Client validation should improve UX, but server validation remains authoritative.

## Generic Result Renderer

The UI renders `result.sections[]` generically. Supported section kinds for v1:

- `text`
- `symbols`
- `emotions`
- `list`

Each renderer must handle missing optional fields gracefully and avoid hard-coding DreamLens-only assumptions into the shared renderer. Persona-specific labels, section order, and icons come from persona or brand config.

## Safety And Disclaimers

The wellness/entertainment disclaimer appears:

- during onboarding
- on every result screen
- in profile or settings where consent is reviewed

If `safety.selfHarmRisk` is `elevated`, the UI must avoid interpretive flourish and show a calm safety-oriented message from backend-approved content. Crisis handling content is an open legal/product decision before launch.

## Theme And White Labeling

Brand config controls:

- app name
- colors
- typography tokens
- icons
- persona id
- copy strings where not legally fixed

DreamLens and Astra should share components. Brand config should change presentation, not backend behavior.

## Mock Mode

MSW mock mode returns realistic DTOs for onboarding, dream submission, result display, journal, and insights. Mock mode is required for UI tests and for frontend development when the API is unavailable.

## Accessibility And UX

Support Dynamic Type where practical, accessible labels for controls, keyboard-friendly web flows, and responsive layouts. Loading during AI calls should be calm and informative without exposing implementation details.

## Testing

RNTL covers component behavior, form validation, and renderer variants. Maestro covers mobile happy paths. Playwright covers web happy paths and at least one error flow. MSW supplies deterministic network responses.
