# DreamLens / PersonaKit Overview

`decision-record.md` is the source of truth. This document explains the product shape and the build sequence in a way that is easier to scan while implementing.

## Product

DreamLens is a wellness and entertainment app for dream interpretation. A user describes a dream, optionally enriches their profile with traits and recent context, and receives a structured interpretation. The product must never present output as medical, psychological, diagnostic, or crisis advice.

The long-term product goal is a personal map of the user's subconscious over time. After enough journal history exists, DreamLens should surface patterns such as frequent symbols, dominant emotions, recurring people and locations, repeated scenarios, weekday/weekend differences, and trend changes across months. Examples: `water` in 3 dreams, `flying` in 11 dreams, anxiety at 15%, happiness at 55%, dreams about being late occurring 3.3x more often during the work week, recurring person `Alex` appearing 5 times, or a recurring location increasing after a life event.

PersonaKit is the reusable engine extracted from day one. It contains provider abstraction, persona configuration, prompt rendering, context building, AI output validation, response mapping, and pipeline orchestration. DreamLens is the first PersonaKit app; Astra, Coach, Sage, and future apps should reuse the same backend by changing persona and brand configuration.

## Primary User Flow

1. User signs in through Cognito or uses local dev JWTs in development.
2. User completes onboarding: age, sex, optional gender identity, language, timezone, traits, consent flags, and disclaimer acceptance.
3. User submits a dream with mood, sleep quality, tags, and optional occurred date.
4. API builds Context JSON v1 with a pseudonymized profile snapshot and untrusted dream text.
5. PersonaKit renders the persona prompt and calls DeepSeek through `IChatClient`.
6. API validates AI Output JSON v1 with JsonSchema.Net.
7. API maps the validated output to UI Response DTO v1 using the persona section map.
8. API persists the dream, interpretation, run metadata, and AI cost ledger entry.
9. UI renders generic `sections[]` plus fixed disclaimers and safety handling.

Post-S21 semantic features extend this flow with dream embeddings in PostgreSQL `pgvector`, private S3 storage for generated images/exports/assets, and SQS-backed jobs for image generation, embedding backfills, exports, and future batch AI work. Amazon Nova Multimodal Embeddings is the default embedding provider at 1,024 dimensions, behind an app-owned abstraction.

Dream DNA and insights require structured extraction, not only prose interpretation. Each dream should preserve queryable metadata for symbols, emotions, people, places, scenarios, scores, tags, dates, weekdays, and model versions so future analytics can be recomputed and audited.

## Request Flow

```text
Expo app
  -> /v1/dreams
  -> DreamLens.Api vertical slice handler
  -> consent and quota checks
  -> PersonaKit.Context.ContextBuilder
  -> PersonaKit.Personas.PersonaRegistry
  -> Scriban prompt template
  -> IChatClient decorators: timeout, retry, circuit breaker, usage logging
  -> DeepSeek OpenAI-compatible endpoint
  -> JsonSchema.Net validation
  -> repair retry once if invalid
  -> result-section mapper
  -> PostgreSQL persistence
  -> pgvector embedding creation or SQS embedding job
  -> UI Response DTO v1
```

## Repository Shape

```text
api/        .NET 9 solution, API, PersonaKit, backend tests
app/        Expo React Native app for iOS, Android, and Web
infra/      Terraform modules and environment stacks
personas/   persona configs, prompt templates, schemas, section maps
docs/plan/  planning documents after S0 normalizes the repo
```

Until S0 moves these plan documents, they live at the repository root.

## Delivery Phases

| Phase | Slices | Outcome |
| --- | --- | --- |
| 0 Foundation | S0-S2 | Repo, CI skeleton, health API, database fixture |
| 1 Identity | S3-S4 | Auth, profile, consent, encrypted sensitive fields |
| 2 AI core | S5-S8 | Provider adapter, persona engine, context builder, interpretation pipeline |
| 3 Dream features | S9-S11 | Dream endpoints, journal, insights, quotas, cost ledger |
| 4 UI | S12-S16 | Expo app, onboarding, dream flow, journal, E2E harness |
| 5 Cloud | S17-S19 | AWS infrastructure, CI/CD, observability, load testing |
| 6 Reuse | S20-S21 | Astra proof and optional monetization |

## Non-Goals For v1

- Streaming responses. v1 is synchronous POST with a 60 second AI timeout.
- Model tools or browsing. The model receives data and returns schema-constrained JSON only.
- Medical, psychological, or diagnostic claims.
- A repository abstraction over EF Core.
- Backend code changes for new personas after the PersonaKit contracts are complete.

## Product Risks

- AI output can sound more authoritative than intended. The disclaimer must be visible at onboarding and every result.
- Context contains sensitive information even after pseudonymization. Consent, encryption, redaction, and logging discipline are first-class requirements.
- DeepSeek latency and cost can affect UX. The product needs quotas, usage logging, calm loading states, and friendly failure modes.
- Async AI and image work can create hidden backlog and cost growth. SQS queue depth, DLQs, retries, idempotency, and per-operation ledger entries must be visible from the start.
- Semantic retrieval can over-contextualize private journal history. Similar-dream and Ask DreamLens features must filter by user, consent, retention policy, and embedding version before ranking.
- Pattern analytics can be misread as certainty. The UI must phrase Dream DNA as observed frequency and correlation, with clear sample sizes and date ranges.
- Persona reuse can drift into special-case code. S20 must prove a config-only Astra build.

## Open Decisions

- Exact retention windows for dreams, AI run records, cost ledger rows, and deleted accounts.
- Whether cost ledger rows are anonymized, aggregated, or retained under a separate audit basis after erasure.
- Subscription tiers and quota numbers.
- Whether DreamLens launches with account-free guest mode. Current plan assumes authenticated users.
- Whether new-dream embeddings run synchronously after interpretation or through SQS. Backfills should use SQS either way.
