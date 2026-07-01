# E2E Harness

## Web

Run from `app/`:

```powershell
npm run e2e:web
```

Playwright starts Expo web on port `8081` and reuses an existing server when available.

## Mobile

Maestro flows live in `e2e/maestro/`.

Run after installing Maestro and starting the Expo app on a simulator/emulator:

```powershell
maestro test e2e/maestro
```

The current mobile flow targets Expo Go with `appId: host.exp.Exponent`. Native app IDs should replace this when EAS builds are introduced.
