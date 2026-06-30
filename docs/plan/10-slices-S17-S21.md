# Slices S17-S21

These prompts cover cloud deployment, observability, PersonaKit reuse, and optional monetization. `decision-record.md` is authoritative.

## S17 - Terraform AWS Infrastructure

Goal: define dev/prod AWS infrastructure with Terraform.

Tests first:

- `terraform fmt -check` fails before formatting if needed.
- `terraform validate` runs for modules/envs.
- Static checks verify required variables and outputs exist.

Implementation:

- Add Terraform module layout under `infra/`.
- Add VPC, ECS Fargate, ALB, WAF, RDS PostgreSQL, Cognito, S3, CloudFront, Secrets Manager, IAM, and observability foundations.
- Add environment configs for dev and prod.
- Add remote state backend documentation or config placeholder.
- Do not commit real secrets.

Verification:

- `terraform fmt -check`
- `terraform validate`
- `terraform plan` only when AWS credentials are available.

Commit:

- `feat(S17): add Terraform infrastructure`

## S18 - CI/CD Pipelines

Goal: deploy API, web, mobile, and infra through GitHub Actions with OIDC.

Tests first:

- Workflow lint/static checks if available.
- CI dry-run or local validation for commands where possible.
- Required secrets and environment variables are documented.

Implementation:

- Add API build/test/container workflow.
- Add web test/build/deploy workflow.
- Add Terraform plan/apply workflow with protected apply.
- Add EAS build workflow placeholder for mobile.
- Use GitHub Actions OIDC, not long-lived AWS keys.

Verification:

- Run local build/test commands.
- Validate workflow syntax where tooling exists.

Commit:

- `feat(S18): add deployment pipelines`

## S19 - Observability, Hardening, k6

Goal: make operational behavior visible and guarded.

Tests first:

- Tests assert AI cost metrics are emitted.
- Tests assert sensitive data is not present in logs for key paths.
- k6 smoke script targets health and dream submission mock/staging endpoint.

Implementation:

- Add OpenTelemetry instrumentation.
- Add ADOT collector config.
- Add CloudWatch dashboard definitions or Terraform resources.
- Add alerts for error rate, latency, AI cost, quota spikes, and provider failures.
- Add k6 load scripts.
- Add final hardening pass for headers, CORS, WAF rules, and logging redaction.

Verification:

- `dotnet test api/DreamLens.sln`
- app test suite if touched
- k6 smoke when endpoint is available

Commit:

- `feat(S19): add observability and hardening`

## S20 - PersonaKit Reuse Proof: Astra

Goal: prove a sibling app can launch through config rather than backend code changes.

Tests first:

- Persona registry loads `astrologer`.
- Astra prompt snapshot is stable.
- Astra output schema validates sample output.
- Section mapper renders Astra result sections with the same renderer.
- No backend code changes are required beyond config registration/loading if PersonaKit is complete.

Implementation:

- Add `astrologer` persona config, prompt template, output schema, and section map.
- Add Astra brand config in the app.
- Add white-label app switch/build config.
- Add tests proving backend pipeline handles Astra through the same abstractions.

Verification:

- `dotnet test api/DreamLens.sln`
- `npm test` in `app/`

Commit:

- `feat(S20): prove PersonaKit reuse with Astra`

## S21 - Optional Monetization

Goal: add paid tiers, RevenueCat, paywall, and store readiness if the product decision is made.

Tests first:

- Free tier quota behavior is enforced.
- Premium tier unlocks configured quota or model behavior.
- Paywall renders correct plan options in mock mode.
- Entitlement state is reflected in API and UI.

Implementation:

- Finalize monetization decision before coding.
- Add RevenueCat integration if selected.
- Add entitlement model.
- Add paywall UI.
- Add store metadata readiness checklist.
- Add quota changes by tier.

Verification:

- `dotnet test api/DreamLens.sln`
- `npm test` in `app/`
- E2E paywall flow in mock mode

Commit:

- `feat(S21): add monetization foundation`
