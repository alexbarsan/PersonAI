# Post-S21 Feature Backlog

This backlog captures product capabilities from `docs/to-be-analyzed/catch-dreamer-features.md` that are not fully covered by the S0-S21 plan. The existing S0-S21 implementation remains the baseline; these items should be promoted into formal slices before implementation.

## Feature Slices

| Candidate Slice | Capability | Missing From Current Baseline | Backend Work | App Work |
| --- | --- | --- | --- | --- |
| S22 | Dream output schema v2 | Complete. Structured observations are first-class, versioned output fields and normalized fact rows. | Added schema v1.1, fact extraction/persistence, owner-scoped fact API, deletion cleanup, migration, and coverage. Existing dreams are not automatically backfilled. | Added generic entity rendering for people and locations plus result sections for structured observations. |
| S23 | Journal v2 and privacy controls | Complete. | Added owner-scoped journal metadata editing, query/mood/tag/date filters, Premium-gated authenticated JSON export, user anonymization requests, Cognito-group/admin approval, S3 asset cleanup, HMAC tombstones, and approval/export coverage. Approval deletes profile, raw dreams, results, facts, embeddings, jobs, and image records/assets; AI cost rows retain only anonymized audit data. | Added journal search/filter controls, journal-details editing, Premium browser JSON export, and pending anonymization-request UI. Native export sharing remains a mobile follow-up. |
| S24 | Voice capture and transcription | Complete and live-verified in dev. | Added a provider abstraction, Amazon Transcribe adapter, multipart upload policy, private S3 input/output handling, Premium gating, daily cap, byte/type/duration checks, owner-scoped polling, default deletion, explicit retention, SQS worker, EF migration, and per-operation cost/latency ledger rows. Terraform enables the controlled dev path; QA/prod remain disabled. A 29-second multilingual capture completed on 2026-09-01, wrote its ledger row, removed temporary S3 objects, and drained from SQS. | Added record/review/transcribe flow with microphone permission, an explicit default-off retention switch, processing feedback, and transcript insertion for user review before interpretation. Local-first recovery and device transcription move to S33. |
| S25 | Dream image generation | Complete behind a disabled-by-default launch flag. | Added image provider abstraction, premium entitlement check, idempotent SQS job, private S3 persistence, signed access, retry-safe worker handling, and per-attempt AI cost/latency ledger rows. Nova Canvas is configured as a switchable Bedrock adapter but is not enabled. | Added premium-aware image request, queued/generating/failed/completed states, and signed-image rendering in the dream result screen. |
| S26 | Embeddings and semantic memory | Context history is summary-based; async embedding jobs and production retrieval integration remain. | Add pgvector migration, embedding provider abstraction, Bedrock Titan Embeddings V2 adapter, dream embeddings, and consent-aware retrieval foundation. SQS backfill and pipeline wiring move to S32. | Add settings/disclosure copy and similar-context indicators where useful. |
| S27 | Similar dreams and Dream DNA | Complete for fact-based analytics. Semantic matches remain empty until new embeddings can be generated. | Added recurring fact groups, sample sizes, date coverage, monthly activity, guarded weekday/weekend rate comparisons, and owner-scoped similar-dream endpoint. Similarity is empty without a source embedding and never crosses user boundaries. | Reworked Insights into a personal dream map with recurring categories, percentages, score averages, activity, timing observations, and reflective language. |
| S28 | Ask Dream DNA | Complete in code; live answers remain dependent on Titan embeddings becoming available and existing dreams being backfilled. | Added `POST /v1/dreams/ask`, owner-scoped pgvector retrieval, compact-summary prompts, consent checks, tier quotas, schema/source validation, fail-closed memory behavior, and separate embedding/answer cost rows. | Added question, answer, evidence-link, loading, quota, consent, and memory-not-ready states with deterministic mock coverage. |
| S29 | Deep Interpretation | Entitlement flag exists, but premium deep-analysis flow is not wired to a stronger model or richer context. | Add deep interpretation endpoint or request mode, model routing, richer retrieved context, tier limits, and cost controls. | Add deep-analysis entry point and result state. |
| S30 | Social sign-in providers | Cognito auth is scaffolded, but provider-specific launch setup is incomplete. | Configure Cognito hosted UI providers for Google and Apple first; Facebook is optional after privacy/product review. | Add provider buttons and platform-specific OAuth validation. |
| S31 | Admin and business metrics | Metrics exist technically, but no admin view exists for MAU, conversion, revenue, AI cost, AWS cost, or gross margin. | Add admin-only metrics endpoints and least-privilege authorization. | Add internal dashboard or connect external BI later. |
| S32 | Async assets foundation | Image and transcription handlers are complete; export handler remains. | Added private S3 asset bucket/module, KMS-encrypted SQS queue with DLQ, long polling, scoped ECS permissions, idempotent `AsyncJobs` persistence with target indexing, publisher, hosted worker, embedding/image/transcription handlers, opt-in bounded backfill worker, job-status endpoint, presigned S3 asset service, and retry/lease/duration metrics. | Shared job-status patterns remain available for exports and future long-running features. |
| S33 | Local-first voice and tiered transcription | Recordings currently depend on an immediate server upload and server transcription is Premium-only. There is no durable client recovery copy or Free device-transcription path. | Accept client-generated idempotency ids, reconcile retried uploads without duplicate jobs or charges, define tier-specific server retention, expose synchronization state, and add S3 lifecycle/cleanup coverage. Keep Premium server transcription and accept Free device transcripts as editable dream text without a transcription charge. | Save native recordings to app-private files before network work, persist an upload outbox, add capability-aware device transcription for Free, route Premium to server transcription, expose recovery/retry/delete states, and use best-effort IndexedDB or OPFS recovery on web. |

## Product Rules

- Dream DNA questions must present patterns and correlations, not causal, diagnostic, or predictive claims. Timing callouts require at least three occurrences plus both weekday and weekend dream samples.
- Retrieval must use embeddings and compact summaries, not full journal prompts.
- Dream DNA is the long-term product center: after enough history exists, show a personal map of recurring symbols, emotions, people, places, scenarios, timing patterns, and month-over-month changes.
- Pattern statements must include sample size/date range when practical. Example: `water` in 3 dreams, `flying` in 11 dreams, anxiety 15%, happiness 55%, dreams about being late 3.3x more common during the work week, recurring person `Alex` 5 times.
- Images and voice are opt-in. Voice capture is local-first: retain a device recovery copy according to an explicit local policy, and retain AWS audio only according to an explicit tier-specific server policy.
- Completed interpretations and generated images should be persisted so reopening a dream does not trigger avoidable AI calls.
- Every individual AI operation must record provider, model, operation type, prompt/schema version where applicable, tokens where available, response time, status, failure category, and estimated cost.
- Stronger models and image generation should be gated by entitlement and quota.
- Generated images, exports, and optional assets must use private S3 storage with signed access; do not mix them with the public web hosting bucket. The current data export is an authenticated JSON response downloaded by the web client.
- Standard user privacy requests use administrator-approved anonymization. Approval removes user content and identifiers, deletes private assets, retains only anonymized financial/audit rows, and blocks the original Cognito subject using an HMAC tombstone.
- Image generation, embedding backfills, exports, and future batch AI work should use SQS-backed async jobs with DLQs and cost/latency telemetry.
- Titan Embeddings V2 is the default embedding provider for launch, but embedding provider, model id, dimensions, and embedding version must be recorded to support future provider migration.

## Open Decisions

- Which currently supported image provider launches first. The Nova Canvas adapter is implemented but disabled because the available model is marked legacy; select, authorize, and cost-review a supported model before enabling it.
- Whether historical completed dreams should be backfilled into the new `DreamFacts` projection before S27 analytics launches.
- Whether Facebook social login is worth the privacy/review overhead for v1 global launch.
- Whether Ask Dream DNA should remain available to Free users at one question per day or become Premium-only at launch.
- Final monthly/yearly product IDs and tier limits after real cost data exists.
- Native local backup duration, Premium AWS retention duration, and whether Free audio is ever uploaded or retained in AWS.
- Device transcription libraries and minimum supported language/platform matrix for iOS, Android, and web.

## Recommended Order

1. Resolve the Titan Embeddings V2 account quota with AWS Support, then retry/backfill failed embedding jobs and verify semantic matches return results.
2. Decide whether historical completed dreams should be backfilled into `DreamFacts` before analytics launches.
3. Select and approve a supported image provider, then configure cost/quotas and enable S25 in a controlled dev test.
4. Live-verify S28 Ask Dream DNA after semantic backfill, then start S29 Deep Interpretation.
5. S33 local-first voice before native store launch, after selecting the device transcription adapter and retention windows.
