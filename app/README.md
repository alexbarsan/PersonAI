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
