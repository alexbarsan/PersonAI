# Product Hardening Plan

## Route and state inventory

| Area | Route or API | Current state | Target state |
| --- | --- | --- | --- |
| Today | `/` | Draft composer and recent journal entries | One shared private draft composer, compact recent entries, entitlement summary |
| Capture | `/dreams/capture` | Separate form with synchronous interpretation | Same shared draft data and composer behavior, recoverable submission state |
| Journal | `/journal`, `GET /v1/dreams` | Loads every matching dream and exposes deletion | Read-only paginated archive with excerpts, clear date order, and load more |
| Dream detail | `/dreams/{id}`, `GET /v1/dreams/{id}` | Interpretation details precede original text; journal edits exposed | Immutable original dream first, interpretation second, optional details after it |
| Ask | `/ask`, `POST /v1/dreams/ask` | Free/Premium limits differ by UI and use UTC completed-ledger counts | Premium-only server reservation, 3 account-local daily questions, 300-character maximum, remaining/reset status |
| Plans | `/paywall`, `GET /v1/entitlements` | Static copy and developer integration message | Honest configured capabilities and shared entitlement facts |
| Profile | `/profile`, `/onboarding`, `/v1/profile` | Encrypted traits and consent, direct onboarding guard | Account, personalization, privacy, subscription sections and clear context explanation |

## Delivery phases

1. **Trust contracts**: immutable dream routes, durable Ask quota reservations, account-timezone entitlement status, journal pagination, prompt provenance, reliability states.
2. **Main journey**: shared draft/composer behavior, compact read-only journal, reordered dream detail, shared entitlement presentation, private-processing language.
3. **Exploration and validation**: Map normalization/provenance and observations, Ask recovery/source UX, onboarding/auth route handling, responsive/accessibility and native validation inventory.

## Evidence and constraints

- Dream ownership is already enforced by `UserSubject` filtering. Existing dreams are preserved; no destructive data migration is planned.
- AI interpretation is currently synchronous. Image, embedding, safety, and voice work already use durable jobs. Moving primary interpretation to a job is a separate compatibility-sensitive change and remains explicitly tracked until its request/status contract can be added without breaking current clients.
- Billing is not connected. Plans will describe only configured entitlement capabilities and will not simulate purchases.
