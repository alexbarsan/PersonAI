# Dream DNA App

Expo Router app for iOS, Android, and Web.

## Commands

- `npm test`
- `npm run typecheck`
- `npm run web`

Mock API mode is enabled by default through `app.json` `extra.mockApi`.

Deployed builds override `app.json` through direct `process.env.EXPO_PUBLIC_*` reads in `src/core/config.ts`, which Metro inlines at build time:

- `EXPO_PUBLIC_API_BASE_URL`
- `EXPO_PUBLIC_MOCK_API`
- `EXPO_PUBLIC_COGNITO_DOMAIN`
- `EXPO_PUBLIC_COGNITO_CLIENT_ID`

## Dream DNA Design

The signed-out web route is the landing page; signing in opens the journal workspace. Native builds use a compact welcome screen. The desktop sidebar and mobile bottom tabs share the existing routes.

- `EXPO_PUBLIC_IOS_STORE_URL`: final HTTPS App Store listing, empty until ready.
- `EXPO_PUBLIC_ANDROID_STORE_URL`: final HTTPS Google Play listing, empty until ready.

Store buttons remain disabled and marked coming soon until configured. The web app remains available independently. Design decisions and original asset briefs: [Dream DNA UX](../docs/design/dream-dna-ux.md).

Run `npm run e2e:web` for workflow and responsive UI checks. Screenshots are written to `test-results/`.
