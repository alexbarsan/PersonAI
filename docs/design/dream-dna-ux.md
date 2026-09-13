# Dream DNA UX Refresh

## Direction

Dream DNA is a personal map of the user's subconscious over time. The interface starts with a small daily action, then makes it easy to return to people, places, emotions, interpretations, and visuals.

- Reference: [Carely by Orbix Studio](https://www.designrush.com/best-designs/apps/carely-app-design), for friendly illustration, approachable mood selection, and a coherent companion identity. No Carely artwork or interface was copied.
- Feature-flow reference: [Dreamscape: Dream Meanings](https://play.google.com/store/apps/details?hl=en&id=com.usedreamscape.app), by Pineapple Software LLC. Its public listing connects capture, interpretation, illustrations, and patterns. Dream DNA keeps its own existing API and feature set.
- Skill: `frontend-design`, installed from `anthropics/skills` and read for this implementation. The shared Expo/React Native architecture is retained, not replaced with a web-only stack.

## Visual System

- Forest `#245c49`: primary actions and selected navigation.
- Mint `#e0f0e7`: calm surfaces and active choices.
- Lilac `#eeebf8`: reflection and map surfaces.
- Coral `#fae8df`: secondary warmth and contrast.
- White and cool off-white: writing and reading surfaces.
- Text `#203e36`, secondary text `#596e67`.
- Bundled Nunito Regular/Bold, fixed type sizes, zero letter spacing. Lucide icons; text labels remain on navigation.
- Mobile: safe-area-aware bottom tabs. Desktop >=1000px: persistent sidebar. Home >=1200px: capture and journal columns. Form widths remain bounded.
- Functional states use ordinary controls, not artwork. Motion is not required to navigate or read content.

## Assets

Original generated artwork lives in `app/assets/brand/`:

1. `dream-dna-owl.png`: transparent original owl logo, mint and forest feathers, ivory face, coral beak/feet, lilac accents, with intertwined chest feathers suggesting DNA. No text or borrowed mascot. Used in brand marks, native welcome, desktop navigation, landing, and web favicon.
2. `dream-garden.png`: original wide illustrated landscape, pale mint sky with generous text space, river and hills, lilac flowers, coral bridge, and owl at right. Used as the full-bleed landing hero and a subtle home banner. Landing sample visuals are explicitly marked illustrative, never passed off as a user's generated image.

These are generation briefs. Production native launcher/adaptive/splash asset variants and store artwork still need their own export and device checks.

## Experience

- Signed-out web `/`: landing page, functional web entry using the existing sign-in flow, interactive journal/map/visual examples, FAQs, and store availability placeholders.
- Signed-out native `/`: focused owl welcome and the existing sign-in flow, without desktop marketing sections.
- Signed-in `/`: saved local draft, mood choices plus custom feeling, direct voice recording, recent dreams from the real API client, map access, plan state, and account links.
- Journal: search, filters, count, capture entry, dated dream rows, result navigation, and deletion error feedback.
- Map: existing server-provided metrics, selectable fact categories, bounded proportional bars, and monthly activity. No client-invented user statistics.
- Ask: editable suggested questions and the existing source-grounded response flow.
- Capture, result, profile, and plans retain behavior with shared typography, palette, and navigation. Admin routes remain available from Profile; no admin authorization behavior is changed by this refresh.
- Privacy copy does not claim end-to-end encryption or that administrators cannot access original dreams.

## Store Links

Set `EXPO_PUBLIC_IOS_STORE_URL` and `EXPO_PUBLIC_ANDROID_STORE_URL` to the final HTTPS listing URLs before building. Empty values show disabled, clearly labeled coming-soon controls. Web entry works independently of store availability. These values are public URLs, not credentials.

## Verification And Remaining Work

- Verified locally on 2026-09-13: TypeScript, 47 unit/component tests, all 11 Playwright workflows, and web/Android/iOS exports. Screenshots cover 1440px desktop, 375px/390px phones, and 768px tablet. Native exports are bundle checks, not device or store-release validation.
- Direct icon subpath imports and individual font-weight imports keep unused glyphs/font variants out of bundles. The verified web JavaScript export is 1.9MB rather than the initial 3.8MB barrel-import build.
- Preview: `http://localhost:8082`, mock API mode. Nothing from this UI task has been committed, pushed, or deployed.
- Run `npm run typecheck`, `npm test -- --runInBand`, `npm run build:web`, and `npm run e2e:web` in `app/`.
- `redesign.spec.ts` adds landing interactions, store placeholder states, real mock-client draft/mood flow, navigation, responsive overflow checks, and desktop/phone/tablet screenshots.
- Review the local mock preview before a focused UI commit/deployment. Existing unrelated backend/admin changes must not be bundled into a redesign deployment.
- Before public launch: native device/keyboard/screen-reader testing, Android/iOS store URLs, production app-icon assets, privacy/terms pages and contact information, store accounts, RevenueCat integration, and a live Cognito/API regression pass.
- Existing backend backlog and S48 live image timing checks remain separate; this UI work does not complete them.
- Review dependency audit findings before launch. The test suite still emits some asynchronous React `act` warnings, although all tests pass; these were not suppressed by the redesign.
