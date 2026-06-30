# AWS Infrastructure

`decision-record.md` is authoritative. This document outlines the Terraform and AWS target architecture.

## Environments

Target environments:

- `dev`: low-cost, safe for experimentation, minimal scale.
- `prod`: production-grade, protected by review and CI/CD gates.

Terraform layout:

```text
infra/
  modules/
    network/
    ecs-api/
    rds-postgres/
    cognito/
    web-cdn/
    observability/
    security/
  envs/
    dev/
    prod/
```

## Core AWS Services

- ECS Fargate for the API container.
- Application Load Balancer in front of the API.
- AWS WAF attached to the ALB and CloudFront.
- RDS PostgreSQL for relational data.
- Cognito user pool for auth.
- S3 and CloudFront for Expo web build hosting.
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

Migrations are applied by CI/CD or a controlled deployment job, not manually from a developer machine.

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

## Security Controls

- WAF managed rules.
- HTTPS only.
- No long-lived AWS keys in GitHub.
- RDS private access only.
- Secrets not printed in logs.
- Least-privilege IAM per service.
- Environment separation between dev and prod.

## Cost Controls

Dev infrastructure should be small by default. AI cost alarms and quota metrics are product requirements, not optional observability extras.

## Open Infrastructure Decisions

- AWS region.
- Domain names.
- Backup retention period.
- Exact ECS task sizes.
- Whether dev RDS can be single-AZ while prod is multi-AZ.
