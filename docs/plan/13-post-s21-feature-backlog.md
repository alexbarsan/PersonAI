# Post-S21 Feature Backlog

This backlog captures product capabilities from `docs/to-be-analyzed/catch-dreamer-features.md` that are not fully covered by the S0-S21 plan. The existing S0-S21 implementation remains the baseline; these items should be promoted into formal slices before implementation.

## Feature Slices

| Candidate Slice | Capability | Missing From Current Baseline | Backend Work | App Work |
| --- | --- | --- | --- | --- |
| S22 | Dream output schema v2 | Main vs alternative interpretations, people, places, objects, lucidity score, nightmare/intensity score, repeated scenarios, and extraction confidence are not first-class fields. | Extend persona schema, section maps, DTO mapping, structured fact persistence, tests, and prompt snapshots. | Render new result sections without hard-coding DreamLens-only assumptions. |
| S23 | Journal v2 | Journal has list/detail/delete, but not edit, search, filters, or export. | Add update endpoint, full-text/search projection, export endpoint, and erasure/export tests. | Add edit form, search/filter controls, and export action. |
| S24 | Voice capture and transcription | Voice capture is not implemented. | Add transcription provider abstraction, upload policy, optional private S3 storage, retention controls, cost ledger operation type, and abuse limits. | Add record/review/transcribe flow with clear retention UX. |
| S25 | Dream image generation | "Visualize my dream" is not implemented. | Add image provider abstraction, SQS async job model, private S3 storage, signed access, entitlement checks, and AI cost ledger entries. | Add image request/status/result UI and paywall/credit handling. |
| S26 | Embeddings and semantic memory | Context history is summary-based; pgvector retrieval is not implemented. | Add pgvector migration, embedding provider abstraction, Bedrock Titan Embeddings V2 adapter, dream embeddings, SQS backfill job, retrieval policy, and consent-aware context packing. | Add settings/disclosure copy and similar-context indicators where useful. |
| S27 | Similar dreams and Dream DNA | Insights currently cover recurring themes and streaks only; the app is not yet a personal subconscious map over time. | Add frequent symbols/emotions/people/places/scenarios, trend windows, sample sizes, weekday/weekend correlations, clusters, and similar-dream endpoints. | Add analytics screens, trend charts, frequency cards, correlation callouts, and similar-dream links. |
| S28 | Ask DreamLens | Users cannot ask questions over their own dream history. | Add retrieval-backed question endpoint, prompt/schema, safety rules, quota/cost ledger integration, and no-full-history prompt tests. | Add question UI, answer view, loading/error states, and history links. |
| S29 | Deep Interpretation | Entitlement flag exists, but premium deep-analysis flow is not wired to a stronger model or richer context. | Add deep interpretation endpoint or request mode, model routing, richer retrieved context, tier limits, and cost controls. | Add deep-analysis entry point and result state. |
| S30 | Social sign-in providers | Cognito auth is scaffolded, but provider-specific launch setup is incomplete. | Configure Cognito hosted UI providers for Google and Apple first; Facebook is optional after privacy/product review. | Add provider buttons and platform-specific OAuth validation. |
| S31 | Admin and business metrics | Metrics exist technically, but no admin view exists for MAU, conversion, revenue, AI cost, AWS cost, or gross margin. | Add admin-only metrics endpoints and least-privilege authorization. | Add internal dashboard or connect external BI later. |
| S32 | Async assets foundation | Private user asset storage and durable async workers are not implemented. | Add private S3 asset bucket/module, signed access service, SQS queues with DLQs, worker host pattern, idempotent job records, retry policy, alarms, and cost/latency metrics. | Add shared job status client patterns that S25, exports, and future long-running features can reuse. |

## Product Rules

- Dream DNA and Ask DreamLens must present patterns and correlations, not causal, diagnostic, or predictive claims.
- Retrieval must use embeddings and compact summaries, not full journal prompts.
- Dream DNA is the long-term product center: after enough history exists, show a personal map of recurring symbols, emotions, people, places, scenarios, timing patterns, and month-over-month changes.
- Pattern statements must include sample size/date range when practical. Example: `water` in 3 dreams, `flying` in 11 dreams, anxiety 15%, happiness 55%, dreams about being late 3.3x more common during the work week, recurring person `Alex` 5 times.
- Images and voice are opt-in. Do not retain voice recordings unless the retention behavior is explicit to the user.
- Completed interpretations and generated images should be persisted so reopening a dream does not trigger avoidable AI calls.
- Every individual AI operation must record provider, model, operation type, prompt/schema version where applicable, tokens where available, response time, status, failure category, and estimated cost.
- Stronger models and image generation should be gated by entitlement and quota.
- Generated images, exports, and optional assets must use private S3 storage with signed access; do not mix them with the public web hosting bucket.
- Image generation, embedding backfills, exports, and future batch AI work should use SQS-backed async jobs with DLQs and cost/latency telemetry.
- Titan Embeddings V2 is the default embedding provider for launch, but embedding provider, model id, dimensions, and embedding version must be recorded to support future provider migration.

## Open Decisions

- Whether voice recordings are discarded immediately after transcription or stored temporarily for user review.
- Which image provider launches first.
- Whether embeddings for newly submitted dreams are generated synchronously immediately after interpretation or queued asynchronously; backfills should use SQS.
- Whether Facebook social login is worth the privacy/review overhead for v1 global launch.
- Whether Ask DreamLens and Deep Interpretation are premium-only at launch.
- Final monthly/yearly product IDs and tier limits after real cost data exists.

## Recommended Order

1. S26 semantic memory foundation: pgvector, embedding abstraction, Titan Embeddings V2 adapter, and retrieval tests.
2. S32 async job and asset foundation: SQS queues, DLQ policy, worker host pattern, private S3 asset bucket, signed access.
3. S22 structured dream schema v2: persist queryable symbols, emotions, people, places, scenarios, scores, and model/schema provenance for analytics.
4. S27 Dream DNA analytics: longitudinal stats, trends, correlations, clusters, and similar-dream links.
5. S25 dream image generation: image provider, entitlement/credit checks, async status API, S3 persistence, and UI.
