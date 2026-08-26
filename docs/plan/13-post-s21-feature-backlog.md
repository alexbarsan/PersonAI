# Post-S21 Feature Backlog

This backlog captures product capabilities from `docs/to-be-analyzed/catch-dreamer-features.md` that are not fully covered by the S0-S21 plan. The existing S0-S21 implementation remains the baseline; these items should be promoted into formal slices before implementation.

## Feature Slices

| Candidate Slice | Capability | Missing From Current Baseline | Backend Work | App Work |
| --- | --- | --- | --- | --- |
| S22 | Dream output schema v2 | Main vs alternative interpretations, people, places, objects, lucidity score, and nightmare/intensity score are not first-class fields. | Extend persona schema, section maps, DTO mapping, persistence, tests, and prompt snapshots. | Render new result sections without hard-coding DreamLens-only assumptions. |
| S23 | Journal v2 | Journal has list/detail/delete, but not edit, search, filters, or export. | Add update endpoint, full-text/search projection, export endpoint, and erasure/export tests. | Add edit form, search/filter controls, and export action. |
| S24 | Voice capture and transcription | Voice capture is not implemented. | Add transcription provider abstraction, upload policy, optional S3 storage, retention controls, cost ledger operation type, and abuse limits. | Add record/review/transcribe flow with clear retention UX. |
| S25 | Dream image generation | "Visualize my dream" is not implemented. | Add image provider abstraction, async job model, S3 private storage, signed access, entitlement checks, and AI cost ledger entries. | Add image request/status/result UI and paywall/credit handling. |
| S26 | Embeddings and semantic memory | Context history is summary-based; pgvector retrieval is not implemented. | Add pgvector migration, embedding provider abstraction, dream embeddings, retrieval policy, and consent-aware context packing. | No major UI required beyond settings/disclosure. |
| S27 | Similar dreams and Dream DNA | Insights currently cover recurring themes and streaks only. | Add frequent symbols/emotions/people/places, trends, correlations, clusters, and similar-dream endpoints. | Add analytics screens, trend charts, and similar-dream links. |
| S28 | Ask DreamLens | Users cannot ask questions over their own dream history. | Add retrieval-backed question endpoint, prompt/schema, safety rules, quota/cost ledger integration, and no-full-history prompt tests. | Add question UI, answer view, loading/error states, and history links. |
| S29 | Deep Interpretation | Entitlement flag exists, but premium deep-analysis flow is not wired to a stronger model or richer context. | Add deep interpretation endpoint or request mode, model routing, richer retrieved context, tier limits, and cost controls. | Add deep-analysis entry point and result state. |
| S30 | Social sign-in providers | Cognito auth is scaffolded, but provider-specific launch setup is incomplete. | Configure Cognito hosted UI providers for Google and Apple first; Facebook is optional after privacy/product review. | Add provider buttons and platform-specific OAuth validation. |
| S31 | Admin and business metrics | Metrics exist technically, but no admin view exists for MAU, conversion, revenue, AI cost, AWS cost, or gross margin. | Add admin-only metrics endpoints and least-privilege authorization. | Add internal dashboard or connect external BI later. |

## Product Rules

- Dream DNA and Ask DreamLens must present patterns and correlations, not causal, diagnostic, or predictive claims.
- Retrieval must use embeddings and compact summaries, not full journal prompts.
- Images and voice are opt-in. Do not retain voice recordings unless the retention behavior is explicit to the user.
- Completed interpretations and generated images should be persisted so reopening a dream does not trigger avoidable AI calls.
- Every AI operation must record provider, model, operation type, prompt version, tokens where available, latency, status, and estimated cost.
- Stronger models and image generation should be gated by entitlement and quota.

## Open Decisions

- Whether voice recordings are discarded immediately after transcription or stored temporarily for user review.
- Which image provider launches first.
- Which embedding provider launches first, and whether embeddings are generated synchronously or by a background job.
- Whether Facebook social login is worth the privacy/review overhead for v1 global launch.
- Whether Ask DreamLens and Deep Interpretation are premium-only at launch.
- Final monthly/yearly product IDs and tier limits after real cost data exists.
