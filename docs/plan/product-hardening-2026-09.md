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
2. **Main journey**: shared draft/composer behavior, compact read-only journal, reordered dream detail, shared entitlement presentation, private-processing language. Completed 2026-09-15: primary interpretation is a durable SQS job in production. Submission returns an owner-scoped pending dream, the worker persists completion/failure, and clients poll the canonical dream route with retry and cancellation controls.
3. **Exploration and validation**: Map normalization/provenance and observations, Ask recovery/source UX, onboarding/auth route handling, responsive/accessibility and native validation inventory. Map provenance completed 2026-09-15: each new fact retains its normalized form version, source output field, source schema, and extraction confidence; an owner can open the contributing journal entries from the Map. Existing facts intentionally retain an `unknown` source field until historical re-extraction is explicitly scheduled. Ask recovery/source UX completed 2026-09-15: the client obtains owner-scoped active-index readiness before asking, shows pending indexing rather than consuming a question, and renders named ranked citations with direct journal links after an answer. Web accessibility hardening completed 2026-09-15: navigation and journal entries use real links with keyboard activation, primary screen headings are exposed semantically, and Map/result tab semantics include explicit list and selection state. Native device validation remains intentionally deferred.

## Evidence and constraints

- Dream ownership is already enforced by `UserSubject` filtering. Existing dreams are preserved; no destructive data migration is planned. Historical DreamFacts are not automatically backfilled: pre-S22 records retain rendered interpretation JSON rather than the raw structured schema required to produce accurate, provenance-bearing facts. Re-extracting rendered text would degrade map quality; re-asking AI would change historical output and add cost. Relationship analytics must therefore use verified normalized facts only.
- Primary interpretation uses the same durable job framework as image, embedding, safety, and voice work. The production request contract is `202 Accepted` plus `GET /v1/dreams/{id}` polling; controlled test mode retains the former synchronous path for legacy contract coverage.
- Billing is not connected. Plans will describe only configured entitlement capabilities and will not simulate purchases.
