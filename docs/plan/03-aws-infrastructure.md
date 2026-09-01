# AWS Infrastructure

`decision-record.md` is authoritative. This document outlines the Terraform and AWS target architecture.

## Environments

Target environments:

- `dev`: low-cost, safe for experimentation, minimal scale.
- `qa`: production-like validation with controlled scale.
- `prod`: production-grade, protected by review and CI/CD gates.

Terraform layout:

```text
infra/
  modules/
    domain/
    network/
    ecs-api/
    rds-postgres/
    cognito/
    web-cdn/
    asset-storage/
    async-jobs/
    observability/
    security/
  envs/
    domain/
    dev/
    qa/
    prod/
```

## Core AWS Services

- ECS Fargate for the API container.
- Application Load Balancer in front of the API.
- AWS WAF attached to the ALB and CloudFront.
- RDS PostgreSQL for relational data.
- RDS PostgreSQL `pgvector` for dream embeddings and semantic retrieval.
- Cognito user pool for auth.
- S3 and CloudFront for Expo web build hosting.
- Private S3 buckets for generated dream images, exports, and optional assets.
- SQS queues for async image generation, embedding generation/backfill, exports, and future batch AI jobs.
- Amazon Bedrock Titan Embeddings V2 as the default embedding provider, called through an application abstraction.
- Secrets Manager for provider keys, encryption settings, and app secrets.
- CloudWatch and X-Ray through OpenTelemetry and ADOT.
- GitHub Actions OIDC for deployments without long-lived AWS keys.

## Network

Use a VPC with public subnets for ingress resources and private subnets for ECS tasks and RDS. RDS is not publicly accessible. ECS tasks reach Secrets Manager, CloudWatch, and external AI providers through controlled outbound access.

## API Deployment

The API is containerized and deployed to ECS Fargate. Health checks use:

- `/health/live` for process liveness
- `/health/ready` for dependency readiness

Autoscaling is based on CPU, memory, and request pressure. Initial minimum capacity may be one task in dev and at least two tasks in prod.

## Database

RDS PostgreSQL stores app data. Production must enable encryption at rest, automated backups, and deletion protection unless intentionally disabled for a temporary environment.

Enable the PostgreSQL `pgvector` extension before adding embedding columns/tables. Store embedding provider, model id, dimensions, version, creation status, and timestamps alongside the vector data. Retrieval queries must filter by user ownership and consent before ranking similar dreams.

Migrations are applied by CI/CD or a controlled deployment job, not manually from a developer machine.

## Asset Storage

Use private S3 buckets for user-owned generated and exportable assets:

- generated dream images
- user data exports
- transient or retained voice source assets
- optional future upload assets

These buckets are distinct from the public Expo web hosting bucket. They must use Block Public Access, encryption at rest, least-privilege IAM, lifecycle rules, and signed access. CloudFront can be added for optimized image delivery only when access control is preserved.

Voice objects use separate prefixes and lifecycle rules from generated images and exports. Premium server transcription can retain audio only for the configured recovery window or when an explicit retention choice allows it. Free-tier AWS audio retention remains disabled unless product economics approve a short recovery window. Client backup lifetime and S3 lifetime are separate policies; successful API synchronization must be observable before the client removes its local recovery copy.

## Async Jobs

Use SQS for durable async work:

- image generation jobs
- embedding generation and backfill jobs
- export jobs
- future batch AI analysis

Each production queue should have long polling, encryption, a DLQ, alarms on age/depth/failures, and least-privilege send/receive/delete permissions. Workers can start as ECS Fargate services to share the .NET codebase and runtime with the API; Lambda remains an option for short, bursty jobs after timeout and dependency size are validated.

## Embedding Provider

Default to Amazon Bedrock Titan Embeddings V2 for dream vectors because it stays inside AWS IAM, billing, and observability boundaries. Keep the embedding provider abstract so Cohere Embed, OpenAI embeddings, or another provider can replace it if quality, language coverage, cost, or regional availability requires a change.

Before implementation, verify current Bedrock model availability in the target region with:

```powershell
aws bedrock list-foundation-models --by-output-modality EMBEDDING --region us-east-1
```

Embedding model dimensions must match the pgvector index dimensions.

## Secrets And Keys

Secrets live in AWS Secrets Manager:

- DeepSeek API key
- encryption key material references
- Cognito settings
- database credentials

Production column encryption uses KMS envelope keys. Local development uses local configuration or user secrets.

## Web Hosting

The Expo web build is published to S3 and served through CloudFront. CloudFront handles TLS, caching, and WAF association. Cache invalidation is part of deployment.

## Cognito

Cognito owns user registration, login, MFA readiness, token issuance, and hosted OAuth configuration. The API validates JWTs; it does not store passwords.

## CI/CD

GitHub Actions uses OIDC to assume AWS roles. Pipelines:

- API: build, test, containerize, push image, deploy ECS service.
- Web: test, build Expo web, upload to S3, invalidate CloudFront.
- Infra: `terraform fmt`, `validate`, `plan`, gated `apply`.
- Mobile: EAS build and submit flow when store readiness begins.

## Observability

OpenTelemetry traces and metrics flow through ADOT to CloudWatch and X-Ray. Dashboards should include:

- request rate and latency
- error rate
- AI provider latency
- token usage and estimated cost
- quota rejections
- circuit breaker state
- database health
- SQS queue depth, oldest message age, DLQ message count
- asset bucket request errors and storage growth
- embedding generation latency, failures, and backlog

## Security Controls

- WAF managed rules.
- HTTPS only.
- No long-lived AWS keys in GitHub.
- RDS private access only.
- Private S3 buckets for user assets with Block Public Access.
- SQS resource policies scoped with source account/resource conditions where service principals are used.
- Bedrock permissions scoped to required embedding models.
- Secrets not printed in logs.
- Least-privilege IAM per service.
- Environment separation between dev and prod.

## Cost Controls

Dev infrastructure should be small by default. AI cost alarms and quota metrics are product requirements, not optional observability extras.

Generated images are expected to be more expensive than embeddings and should be opt-in, quota-gated, and cached in S3. Embeddings should be generated once per dream version and reused for similar-dream retrieval instead of sending full journal history to a chat model.

## Open Infrastructure Decisions

- Backup retention period.
- Native local voice backup window and Premium/free S3 voice retention windows.
- Exact ECS task sizes.
- Whether dev RDS can be single-AZ while prod is multi-AZ.
- Whether async workers launch as ECS Fargate services or Lambda functions.
- Whether CloudFront is needed in front of private generated-image delivery at launch.
- Final Bedrock embedding model id and dimensions after checking current regional availability.
