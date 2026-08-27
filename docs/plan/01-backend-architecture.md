# Backend Architecture

`decision-record.md` is authoritative. This document turns those decisions into implementation guidance for the .NET 9 backend.

## Projects

```text
api/
  DreamLens.sln
  src/
    DreamLens.Api/
    PersonaKit/
  tests/
    DreamLens.Api.Tests/
    DreamLens.Api.IntegrationTests/
    PersonaKit.Tests/
```

`DreamLens.Api` owns HTTP, auth, persistence, feature handlers, API DTOs, and application composition. `PersonaKit` owns provider abstraction, persona loading, prompt rendering, context contracts, output validation, mapping, and pipeline primitives that sibling apps can reuse.

## Architectural Style

Use Vertical Slice Architecture with CQRS-lite. A feature folder owns its endpoint mapping, request DTO, response DTO, validator if needed, handler, and local tests.

Example:

```text
Features/
  Dreams/
    SubmitDream/
      Endpoint.cs
      SubmitDreamRequest.cs
      SubmitDreamResponse.cs
      SubmitDreamHandler.cs
      SubmitDreamErrors.cs
    GetDream/
  Profile/
    GetProfile/
    UpdateProfile/
```

No MediatR. Handlers are ordinary classes registered in DI and called from Minimal API endpoint delegates.

## API Surface

Versioned routes:

- `GET /v1/me`
- `GET /v1/profile`
- `PUT /v1/profile`
- `POST /v1/dreams`
- `GET /v1/dreams/{id}`
- `GET /v1/insights`
- `GET /health/live`
- `GET /health/ready`

Use typed results where practical. Public errors should be ProblemDetails-compatible and should not leak provider payloads, prompt text, stack traces, secrets, PII, or full context JSON.

Future post-S21 routes should stay versioned and feature-sliced:

- `POST /v1/dreams/{id}/image-jobs`
- `GET /v1/dreams/{id}/image-jobs/{jobId}`
- `GET /v1/dreams/{id}/similar`
- `POST /v1/dreams/ask`
- `POST /v1/exports`
- `GET /v1/exports/{jobId}`

## Persistence

Use PostgreSQL through EF Core 9. Handlers use `DreamLensDbContext` directly. Do not add repositories over EF.

Initial aggregate areas:

- User profile and consent.
- Dream submissions.
- Interpretations and mapped result JSON.
- AI run records: provider, model, persona version, token counts, latency, status, cost estimate.
- Dream embeddings stored with PostgreSQL `pgvector`.
- Generated image and export metadata pointing to private S3 objects.
- Async job records for image generation, embedding generation/backfill, exports, and future batch AI work.
- Journal and insight read models.
- Quota counters.

Sensitive columns are encrypted at rest. In development use a local key from configuration or user secrets. In AWS use KMS envelope keys. Encryption must happen below feature handlers so slices cannot accidentally persist plaintext sensitive fields.

`pgvector` is the launch vector store. Keep embedding rows tied to internal dream/user ids so authorization, consent filtering, erasure, and relational filters remain in one transactional store. Revisit S3 Vectors or a dedicated vector database only if pgvector becomes a measured bottleneck.

Embedding dimensions must be stored in configuration and must match the pgvector index. Changing embedding provider or dimension requires a backfill plan and a versioned embedding column/table.

## Auth

Production auth uses Amazon Cognito. The Expo app uses OAuth code flow with PKCE. The API validates JWTs with `JwtBearer`.

Local development uses `dotnet user-jwts`. `ICurrentUser` exposes a stable internal user id and auth claims. Never send Cognito `sub`, email, name, phone, IP, or device identifiers to DeepSeek.

## PersonaKit Backend Contracts

PersonaKit should expose narrow abstractions:

- `IPersonaRegistry`
- `IPromptRenderer`
- `IContextBuilder`
- `IOutputValidator`
- `IResultSectionMapper`
- `IInterpretationPipeline`

AI providers are accessed through Microsoft.Extensions.AI `IChatClient`. The initial provider is DeepSeek through its OpenAI-compatible endpoint with model `deepseek-chat`. `deepseek-reasoner` is reserved for premium deep analysis.

Embeddings use a separate abstraction, not `IChatClient`. Default launch provider is Amazon Bedrock Titan Embeddings V2. Keep provider/model/dimension/version on every embedding record so vectors can be regenerated safely.

Cross-cutting provider behavior is composed through decorators:

- timeout: 60 seconds
- retry: two retries with jitter for 429 and 5xx
- circuit breaker
- usage logging: tokens, latency, estimated cost

Async AI work is queued through SQS-backed job handlers. Image generation, embedding backfills, exports, and future batch analysis should not run inside the interactive request unless a slice explicitly decides that latency is acceptable.

## Asset Storage

Private user assets live in S3, separate from the public web build bucket:

- generated dream images
- user exports
- optional uploaded source assets, such as future voice/image inputs

Store only S3 bucket/key/version metadata in PostgreSQL. Return signed URLs or proxied download endpoints, not public object URLs. Buckets must use Block Public Access, encryption at rest, lifecycle policies, and CloudTrail/S3 monitoring for sensitive operations.

## Context Builder

The context builder assembles Context JSON v1 from:

- current user
- profile snapshot
- consent flags
- recent history if consented
- dream input
- persona id and version
- locale and request id

Dream text is always untrusted data. Length must be capped at 10-4000 characters before it reaches prompt rendering.

## Output Validation And Repair

DeepSeek output must validate against the persona output schema. Invalid JSON or schema failure triggers exactly one repair retry using the repair prompt in `11-runtime-prompts.md`. A second failure returns a friendly 503-style error and records a failed AI run.

## Configuration

Use options classes with validation on startup:

- `DeepSeekOptions`
- `EmbeddingOptions`
- `AssetStorageOptions`
- `AsyncJobOptions`
- `QuotaOptions`
- `EncryptionOptions`
- `PersonaOptions`
- `CorsOptions`
- `ObservabilityOptions`

Secrets must come from user secrets in local development and AWS Secrets Manager in deployed environments.

## Logging And Telemetry

Use structured logs and OpenTelemetry. Never log raw dream text, full context JSON, profile traits, tokens, secrets, auth headers, or provider request bodies. Log request ids, persona ids, model ids, status, latency, token counts, cost estimates, and sanitized error categories.

## Testing

Unit tests live in `DreamLens.Api.Tests` and `PersonaKit.Tests`. Integration tests live in `DreamLens.Api.IntegrationTests` and use WebApplicationFactory, Testcontainers PostgreSQL, and WireMock.Net for the DeepSeek stub.

Each slice starts red. Tests should verify behavior at the lowest useful level and include integration tests when HTTP, database, auth, or provider boundaries are touched.
