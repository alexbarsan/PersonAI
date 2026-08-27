# Catch Dreamer --- AWS Architecture Proposal

## High-Level Architecture

``` text
iOS / Android / Web
        |
        v
   CloudFront
        |
        +---- S3 (web/static assets)
        |
        v
 API Gateway HTTP API
        |
        v
 AWS Lambda (.NET / ASP.NET Core)
        |
        +---- RDS PostgreSQL
        |       +---- Application data
        |       +---- ASP.NET Core Identity
        |       +---- pgvector
        |
        +---- S3
        |       +---- Generated dream images
        |       +---- Exports / optional assets
        |
        +---- SQS
        |       +---- Async image/AI jobs
        |
        +---- AI provider layer / Bedrock
        |       +---- Interpretation
        |       +---- Deep Interpretation
        |       +---- Embeddings
        |       +---- Image generation
        |
        +---- SES
        |       +---- Verification / password reset / email
        |
        +---- CloudWatch
                +---- Logs / metrics / alarms
```

## Backend

-   ASP.NET Core / .NET.
-   Lambda for stateless API workloads.
-   API Gateway HTTP API.
-   SQS for asynchronous/long-running operations.
-   Keep AI providers behind interfaces such as:
    -   `IDreamInterpreter`
    -   `IImageGenerator`
    -   `IEmbeddingProvider`
    -   `IModerationProvider`
    -   `ITranscriptionProvider`

This lets Catch Dreamer A/B test or switch between OpenAI, Gemini,
DeepSeek, Bedrock-hosted models and future providers.

## Database

Use **Amazon RDS PostgreSQL** for users, dreams, interpretations,
symbols, themes, people/locations, subscriptions, AI usage and image
metadata.

Use **pgvector** for: - Dream embeddings. - Similar-dream retrieval. -
AI memory. - Dream clustering. - Ask Catch Dreamer.

Start with PostgreSQL rather than a separate vector database.

## Authentication

Recommended: **ASP.NET Core Identity + PostgreSQL** with: - JWT access
tokens. - Refresh-token rotation/revocation. - Email verification. -
Password reset. - Roles/claims. - Optional MFA/passkeys later.

### Social Login

Yes. Identity can coexist with external authentication providers:

``` text
Google ----\
Apple ------> External OAuth/OIDC --> ASP.NET Core Identity --> Catch Dreamer user
Facebook ---/
Email ------/
```

Recommended launch options: - Google - Apple - Facebook - Email/password

Other OAuth/OIDC providers can be added later. For mobile apps, use each
provider's recommended secure authorization flow and never embed server
secrets in the client.

## Storage

Use **S3** for generated dream images, exports and optional voice
recordings.

Use **CloudFront** for static assets and appropriate image delivery.
Keep private dream content in private buckets and use controlled/signed
access where needed.

Avoid permanently retaining voice recordings unless the feature requires
it and the user understands the retention behavior.

## AI Flow

### Normal Interpretation

Use a fast, inexpensive model with strong writing and empathy.

### Metadata Extraction

Where possible, have the main interpretation call also return structured
symbols, emotions, themes, people, locations and scores rather than
paying for separate calls.

### Deep Interpretation

Use a stronger model with retrieved historical context and Dream DNA.

### Embeddings

Create/update an embedding when the dream changes, then store it in
pgvector.

### Image Generation

Recommended asynchronous flow: 1. API validates subscription/credit
allowance. 2. Create an SQS job. 3. Worker calls the configured image
model. 4. Save result to S3. 5. Save metadata/cost to PostgreSQL. 6.
Client is notified or polls for completion.

## Suggested Core Tables

``` text
Users / ASP.NET Identity tables
RefreshTokens
Dreams
DreamInterpretations
DreamSymbols
DreamThemes
DreamPeople
DreamLocations
DreamEmbeddings
DreamImages
Subscriptions
AIUsage
UserPreferences
```

`AIUsage` should include provider, model, operation type, prompt
version, input/output tokens, estimated USD cost, latency, status and
timestamp.

## Security

-   HTTPS everywhere.
-   Private RDS networking.
-   Secrets Manager/Parameter Store.
-   IAM least privilege.
-   Private S3 buckets.
-   Encryption at rest.
-   Refresh-token rotation and revocation.
-   Rate limiting and abuse protection.
-   Safety handling for AI input/output.
-   User export/deletion.
-   GDPR/privacy review for EU users.
-   Explicitly evaluate processing regions for every AI provider.

## Observability

CloudWatch should cover: - API/Lambda errors and latency. - Cold
starts. - SQS queue depth. - AI latency/failures. - Database health. -
Cost-related metrics.

Build an admin view for MAU, revenue, interpretations, images, AI cost,
estimated AWS cost, cost per user, conversion and gross margin.

## Recommended v1 Stack

``` text
Frontend:       Mobile + Web
Edge:           CloudFront
Static files:   S3
API:            API Gateway HTTP API
Backend:        ASP.NET Core / .NET Lambda
Authentication: ASP.NET Core Identity + PostgreSQL
Social auth:    Google + Apple + Facebook
Database:       RDS PostgreSQL + pgvector
Async:          SQS + Lambda workers
Images/files:   S3 + CloudFront
Email:          SES
AI:             Provider abstraction / Bedrock where suitable
Monitoring:     CloudWatch
```

## Key Principle

Keep the **AWS infrastructure tightly integrated**, but the **AI
provider loosely coupled** so provider/model changes are configuration
and implementation changes rather than application rewrites.
