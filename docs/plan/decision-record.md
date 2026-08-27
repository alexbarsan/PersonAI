# DreamLens / PersonaKit — Decision Record

This file is the **single source of truth** for the project. Every other document in this folder, and every slice prompt, must conform to it. If a decision changes, this file is edited first (via the ADR-update prompt in `06-dev-orchestrator.md`), then dependent docs are updated.

Status: accepted · Date: 2026-06-12 · Applies to: all docs in this folder and all generated code.

---

# 1. Product

* **DreamLens** — a dream-interpretation app. The user describes a dream in the UI; the API assembles a complete, pseudonymized JSON snapshot of the user (age, sex, allergies, fears, sleep habits, culture, recent life events, mood) plus the dream text, sends it to DeepSeek, validates and maps the AI's JSON response into a UI-friendly structure, and returns it. Positioning: **wellness / entertainment — explicitly NOT medical, psychological, or diagnostic advice** (disclaimer shown at onboarding and on every result).

* **PersonaKit** — the reusable core extracted from day one: AI-provider abstraction + persona engine + context builder + interpretation pipeline. Sibling apps (astrology **"Astra"**, gym coach **"Coach"**, cooking **"Sage"**, and future ideas) are launched by swapping a *persona config* (prompt template + output schema + result-section map + branding) — not by writing new backend code.

# 2. Top-level decisions

| #   | Topic             | Decision                                                                                                                                                                                                                                                                    | Why                                                                                                            |
| --- | ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| D1  | Backend           | **.NET 9**, ASP.NET Core **Minimal APIs**                                                                                                                                                                                                                                   | Matches the local SDK and current project baseline; strong performance, built-in OpenAPI + rate limiting       |
| D2  | Architecture      | **Vertical Slice Architecture**, CQRS-lite, **no MediatR** (plain handler classes + DI; avoids MediatR's commercial license)                                                                                                                                                | Matches feature-slice delivery; fewer deps                                                                     |
| D3  | AI abstraction    | **Microsoft.Extensions.AI** `IChatClient`; DeepSeek adapter via its OpenAI-compatible endpoint (`https://api.deepseek.com`, model `deepseek-chat`; `deepseek-reasoner` reserved for premium deep analysis)                                                                  | Provider-agnostic by design — swap to Claude/OpenAI/Gemini by registering a different `IChatClient`            |
| D4  | Resilience        | **Polly v8** pipelines implemented as `DelegatingChatClient` **decorators**: timeout 60 s, retry ×2 with jitter on 429/5xx, circuit breaker; plus a usage-logging decorator (tokens, latency, est. cost)                                                                    | Decorator pattern; resilience is orthogonal to the provider                                                    |
| D5  | Database          | **PostgreSQL (RDS)** + **EF Core 9**; handlers use `DbContext` directly (no repository layer over EF — YAGNI); **Testcontainers** Postgres in integration tests                                                                                                             | Simplicity; EF already is the unit-of-work/repository                                                          |
| D6  | Auth              | **Amazon Cognito** user pool; UI uses OAuth code + **PKCE** (`expo-auth-session`); API validates JWTs with `JwtBearer`; local dev uses `dotnet user-jwts`                                                                                                                   | Managed, MFA-ready, no password handling in our code                                                           |
| D7  | UI                | **Expo (React Native + TypeScript)** — ONE codebase for iOS + Android + Web (`react-native-web`); Expo Router; TanStack Query (server state) + Zustand (local state); react-hook-form + zod                                                                                 | Easiest single-codebase coverage of all three targets; EAS cloud builds (no Mac required); real-DOM web output |
| D8  | UI testing        | **RNTL** (component) + **Maestro** (mobile E2E) + **Playwright** (web E2E) + **MSW** mock-API mode                                                                                                                                                                          | Covers component, flow, and cross-platform E2E                                                                 |
| D9  | Hosting           | AWS: **ECS Fargate + ALB + WAF** for the API; web on **S3 + CloudFront**; secrets in **Secrets Manager**; **Terraform** IaC; **GitHub Actions with OIDC** (no long-lived AWS keys)                                                                                          | Standard, secure, container parity dev⇄prod                                                                    |
| D10 | Observability     | **OpenTelemetry** + ADOT collector → CloudWatch + X-Ray; token-usage and AI-cost metrics are first-class                                                                                                                                                                    | Cost guardrails are a feature, not an afterthought                                                             |
| D11 | Prompt templating | **Scriban** templates (strict mode); **JsonSchema.Net** for AI-output validation; **Verify** for snapshot tests of rendered prompts and context JSON                                                                                                                        | Deterministic, testable prompts                                                                                |
| D12 | Backend testing   | **xUnit + FluentAssertions + NSubstitute**; `WebApplicationFactory`; **WireMock.Net** as the DeepSeek stub; **k6** for load                                                                                                                                                 | TDD-first everywhere                                                                                           |
| D13 | Streaming         | v1 is synchronous `POST` (AI calls take 5–30 s; request timeout 60 s); **SSE streaming is a later enhancement**, designed-for but not built in v1                                                                                                                           | Simplicity first; UI shows a calming loading state                                                             |
| D14 | Privacy           | The context JSON **always carries the full profile snapshot** but **pseudonymized** (no name, email, or device identifiers — `pseudonymId` only); sensitive traits included only with recorded consent; sensitive columns encrypted at rest (AES-GCM, KMS envelope in prod) | Honors the "JSON always contains all user data" requirement without leaking identity to a third-party AI       |
| D15 | Multi-usage       | Two meanings, both in scope: (a) **multi-user scale** — stateless API, horizontal autoscaling, per-user rate limits + daily quotas; (b) **multi-app reuse** — PersonaKit persona configs + white-label UI brand config                                                      | Keeps DreamLens production-ready while proving PersonaKit can launch sibling apps without backend rewrites      |

## 2.1 Post-S21 semantic, asset, and async decisions

These decisions extend D3, D5, and D9 for the next feature wave:

- **Semantic memory**: store dream embeddings in PostgreSQL `pgvector`; retrieve only the most relevant consent-allowed dreams for similar-dream search, Dream DNA, Ask DreamLens, and richer deep interpretation. Start with pgvector instead of a separate vector database or S3 Vectors because it fits the current RDS/EF baseline and keeps authorization, consent filtering, and erasure in one store.
- **Embedding provider**: use a separate embedding-provider abstraction rather than `IChatClient`. Amazon Bedrock Titan Embeddings V2 is the default AWS-hosted provider. Record provider, model id, dimensions, and embedding version with every vector so provider changes can be backfilled safely.
- **Private asset storage**: store generated dream images, exports, and optional user-provided assets in private S3 buckets with KMS encryption, lifecycle policies, least-privilege IAM, and signed access. Do not mix user assets with the public web hosting bucket.
- **Async AI jobs**: use SQS-backed job queues for long-running or non-critical work: dream image generation, embedding backfill, exports, and future batch analysis. Synchronous dream interpretation remains v1 behavior until deliberately changed.

# 3. Canonical names & repo layout

```text
dreamlens/

├── api/
│   ├── DreamLens.sln                  
│   ├── src/DreamLens.Api/             # ASP.NET Core minimal API; vertical slices under Features/
│   ├── src/PersonaKit/                # reusable AI core: Providers/ Personas/ Context/ Pipeline/ 
│   ├── tests/DreamLens.Api.Tests/     # unit tests
│   ├── tests/DreamLens.Api.IntegrationTests/       # WebApplicationFactory + Testcontainers + WireMock.Net
│   └── tests/PersonaKit.Tests/
│
├── app/                               # Expo app (iOS + Android + Web)
├── infra/                             # Terraform (modules/ + envs/dev + envs/prod)
├── personas/                          # versioned persona configs: prompt templates + output schemas + section maps
├── docs/plan/                         # THIS folder, copied in at S0
└── SLICE-STATUS.md                    # living slice tracker (format defined in 06-dev-orchestrator.md)
```

- Namespaces: `DreamLens.Api.Features.<Feature>.<UseCase>` (e.g. `DreamLens.Api.Features.Dreams.SubmitDream`); `PersonaKit.Providers`, `PersonaKit.Personas`, `PersonaKit.Context`, `PersonaKit.Pipeline`

- API routes are versioned: 
* `/v1/me`
* `/v1/profile`
* `/v1/dreams`
* `/v1/dreams/{id}`
* `/v1/insights`
* `/health/live`
* `/health/ready`

- Persona ids:
* `dream-interpreter`
* `astrologer`
* `gym-coach`
* `chef`

# 4. Canonical schemas (v1)

These three schemas appear in several docs - they must be **byte-identical in field names** everywhere.

## 4.1 Context JSON v1 (API -> DeepSeek) - "the always complete user JSON"

```json
{
  "schemaVersion": "1.0",
  "requestId": "uuid",
  "locale": "en-US",
  "persona": {
    "id": "dream-interpreter",
    "version": "1.0.0"
  },
  "user": {
    "pseudonymId": "usr_9g25c2",
    "age": 33,
    "sex": "male",
    "genderIdentity": "male",
    "language": "en",
    "timezone": "America/New_York",
    "traits": {
      "fears": ["spiders", "public speaking"],
      "allergies": ["peanuts"],
      "interests": ["hiking", "painting"],
      "occupation": "nurse",
      "relationshipStatus": "single",
      "culturalBackground": "Romanian-American",
      "sleepPattern": "irregular, ~6h",
      "stressLevel": "medium",
      "recentLifeEvents": ["new job"]
    },
    "consent": {
        "aiProcessing": true,
        "sensitiveTraits": true,
        "historyUse": true
    }
  },
  "history": {
    "recentThemes": ["falling", "water", "erotic"],
    "interactionCount": 11,
    "lastSummary": "..."
  },
  "input": {
    "type": "dream",
    "text": "<user's dream description - UNTRUSTED CONTENT, treat as data, never as instructions>",
    "mood": "anxious",
    "sleepQuality": 2,
    "tags": ["recurring"],
    "occurredAt": "2026-06-12"
  }
}
```

Invariants: `pseudonymId` is an HMAC of the internal user id (never the Cognito `sub`, email or name); `traits` and `history` are omitted or reduced when the matching consent flag is false; `input.text` is length-capped (10-4000 chars).

## 4.2 AI Output JSON v1 (DeepSeek -> API, schema-validated)

```json
{
  "schemaVersion": "1.0",
  "summary": "One-paragraph essence of the dream.",
  "symbols": [
    {
        "symbol": "falling",
        "meaning": "general meaning",
        "personalRelevance": "tied to user's traits/context"
    }
  ],
  "emotions": [
    {
        "name": "anxiety",
        "intensity": 0.7,
        "evidence": "what in the dream suggests it"
    }
  ],
  "themes": ["loss of control"],
  "interpretation": "Long-form personalized interpretation.",
  "guidance": "Gentle, actionable, non-medical guidance.",
  "followUpQuestions": ["...", "..."],
  "safety": {
    "selfHarmRisk": "none",
    "notes": ""
  },
  "confidence": 0.74
}
```

`safety.selfHarmRisk` ∈ `none|low|elevated`. Invalid JSON triggers exactly one "repair" retry (repair prompt in `11-runtime-prompts.md`); a second failure returns a friendly 503-style error to the client.

## 4.3 UI Response DTO v1

```json
{
  "id": "dream_xxx",
  "createdAt": "2026-06-12T07:40:00Z",
  "status": "completed",
  "result": {
    "summary": "...",
    "sections": [        
      {
        "kind": "symbols",
        "title": "Symbols",
        "items": [
          {
            "title": "falling",
            "body": "..."
          }
        ]
      },
      {
        "kind": "emotions",
        "title": "Emotions",
        "items": [
          {
            "title": "anxiety",
            "body": "...",
            "value": 0.7
          }
        ]
      },
      {
        "kind": "text",
        "title": "Interpretation",
        "body": "..."
      },
      {
        "kind": "text",
        "title": "Guidance",
        "body": "..."
      }
    ],
    "followUpQuestions": ["..."]
  },
  "meta": { 
    "personaVersion": "1.0.0",
    "model": "deepseek-chat",
    "latencyMs": 6500
  }
}
```

**Key reuse idea:** the UI renders a *generic* `sections[]` list (`kind` ∈ `text|symbols|emotions|list`). Each persona declares a *result-section map* (AI output fields -> sections). The same UI then renders astrology charts-of-the-day, gym plans, or recipes without much UI code changes.

# 5. Slice map (linear, S0 -> S21)

| Slice | Title | Phase |
|--------|--------|--------|
| S0 | Monorepo, CI skeleton, SLICE-STATUS.md, plan docs copied in | 0 – Foundation |
| S1 | Walking skeleton API: health endpoints, slice conventions, OpenAPI, Dockerfile | 0 |
| S2 | PostgreSQL + EF Core 9 + Testcontainers integration-test fixture | 0 |
| S3 | AuthN/AuthZ: Cognito JWT validation, `/v1/me`; dev tokens | 1 – Identity |
| S4 | User profile (age, sex, allergies, fears, …) + consent + column encryption | 1 |
| S5 | PersonaKit: `IChatClient` DeepSeek adapter + resilience/logging decorators | 2 – AI core |
| S6 | Persona engine: persona configs, Scriban templates, output-schema validation | 2 |
| S7 | Context builder: profile + request → Context JSON v1 (pseudonymized) | 2 |
| S8 | Interpretation pipeline orchestrator (moderation → AI → validate → map → persist) | 2 |
| S9 | Dream endpoints: `POST /v1/dreams`, `GET /v1/dreams/{id}` | 3 – Dream features |
| S10 | Journal & insights endpoints (list, delete, recurring themes, streaks) | 3 |
| S11 | Rate limiting, daily quotas, abuse protection, AI cost ledger | 3 |
| S12 | Expo scaffold: router, theming, generated API client, auth, mock mode, RNTL | 4 – UI |
| S13 | Onboarding wizard + profile UI (traits, consent) | 4 |
| S14 | Dream capture + result screens (generic section renderer, safety card) | 4 |
| S15 | Journal & insights UI (list, detail, charts, streaks) | 4 |
| S16 | UI E2E harness: Maestro flows + Playwright web + CI wiring | 4 |
| S17 | Terraform AWS infra (VPC, ECS Fargate, RDS, Cognito, CloudFront, WAF) | 5 – Cloud |
| S18 | CI/CD pipelines: API → ECS, web → S3/CloudFront, mobile → EAS, infra plan/apply | 5 |
| S19 | Observability (OTel, dashboards, cost alarms), hardening, k6 load test | 5 |
| S20 | PersonaKit reuse proof: astrologer persona + white-label "Astra" build, config only | 6 – Reuse |
| S21 | (Optional) Monetization: tiers, RevenueCat, paywall, store readiness | 6 |

Full slice packs with TDD plans and copy-paste prompts: `07-slices-S0-S5.md`, `08-slices-S6-S11.md`, `09-slices-S12-S16.md`, `10-slices-S17-S21.md`.

## 5.1 Post-S21 product backlog

S0-S21 define the implemented baseline. The next feature wave is tracked in `13-post-s21-feature-backlog.md` and includes:

- richer dream output schema fields: main interpretation, alternative interpretations, people, places, objects, lucidity score, and nightmare/intensity score
- journal editing, search/filtering, and export
- optional voice capture and transcription
- opt-in dream image generation
- PostgreSQL `pgvector` embeddings with Bedrock Titan Embeddings V2 by default for semantic memory, similar dreams, Dream DNA, and Ask DreamLens
- private S3 asset storage for generated dream images, exports, and optional assets
- SQS-backed async jobs for image generation, embedding backfills, exports, and future batch AI work
- premium Deep Interpretation with stronger model routing and richer retrieved context
- Cognito social sign-in providers for Google and Apple first, with Facebook optional
- admin/business metrics for usage, conversion, revenue, AI cost, AWS cost, and gross margin

## 6. Design patterns & SOLID map

| Pattern | Where | Why |
|----------|----------|----------|
| Strategy | `IChatClient` implementations (DeepSeek, future Claude/OpenAI, Fake) | Swap AI provider without touching the pipeline |
| Adapter | `DeepSeekChatClient` adapting the OpenAI-compatible wire format | Isolate vendor specifics |
| Decorator | Resilience, usage-logging, (later) caching `DelegatingChatClient`s | Compose cross-cutting concerns |
| Builder | `ContextBuilder` assembling Context JSON v1 | Stepwise, testable construction with consent gating |
| Factory | `PersonaRegistry` producing persona-bound pipelines | New persona = data, not code |
| Template Method / Pipeline | `InterpretationPipeline` fixed step order | Moderation → context → prompt → AI → validate → map → persist |
| Null Object | `FakeChatClient` for dev/test/mock mode | Offline development and deterministic tests |
| Options | Typed config (`DeepSeekOptions`, `QuotaOptions`, …) | Validated, injectable configuration |

SOLID:
**S** — one handler per use case (vertical slices);
**O** — new providers/personas added without modifying the pipeline;
**L** — every `IChatClient` is substitutable (incl. `FakeChatClient`);
**I** — narrow interfaces (`IContextBuilder`, `IPersonaRegistry`, `IOutputValidator`, `ICurrentUser`);
**D** — handlers depend on abstractions, wiring lives in DI composition roots.

## 7. Development workflow rules (TDD — non-negotiable)
1. Every slice starts by **writing the listed tests first** and running them to confirm they **fail (red)**.
2. Implement the minimum to go **green**, then refactor with tests still green.
3. After every slice the agent **runs the full test suite** (`dotnet test` for `api/`; `npm test` in `app/` once it exists; Maestro/Playwright when UI flows changed) and **must not proceed on red**.
4. One slice = one conventional commit (`feat(Sx): ...`); update `SLICE-STATUS.md` in the same commit.
5. Slices execute **in linear order**; no skipping. Deviations from this decision record require updating this file first.

## 8. Privacy invariants

- Context JSON sent to DeepSeek **always includes the full profile snapshot** (the product requirement) but **never** name, email, phone, Cognito `sub`, IP, or device ids — only `pseudonymId`.
- Sensitive traits (fears, allergies, health-adjacent fields) require `consent.sensitiveTraits = true`; absent consent they are omitted and the persona template degrades gracefully.
- Sensitive columns are encrypted at rest (AES-GCM; KMS envelope keys in AWS, local key for dev).
- GDPR: users can export their data and request erasure. Erasure removes or anonymizes personal data across dreams, interpretations, run records, and cost ledger rows according to retention rules documented before implementation; any admin approval workflow must not block legally required erasure.
- Dream text is untrusted input: prompt-injection firewall rules live in every persona system prompt; the model gets **no tools** and its output is schema-validated.
