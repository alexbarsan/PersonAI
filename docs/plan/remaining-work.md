# Remaining Work

Last updated after adding the post-S21 semantic memory, private asset storage, and async AI job plan on 2026-08-28.

## Planned Slices

All planned slices S0-S21 are implemented.

## Post-S21 Product Backlog

The imported Catch Dreamer feature notes add several capabilities that are not fully implemented yet. Track the detailed backlog in `13-post-s21-feature-backlog.md`.

- Dream output schema v2: main and alternative interpretations, people, places, objects, repeated scenarios, lucidity score, nightmare/intensity score, and extraction confidence.
- Journal v2: edit dreams, search/filter history, and export user data from the app.
- Voice capture and transcription with explicit retention controls.
- Dream image generation with SQS async jobs, private S3 storage, signed access, and entitlement/cost limits.
- Embeddings and semantic memory using PostgreSQL `pgvector` and Amazon Bedrock Titan Embeddings V2 by default, not full-history prompts.
- Similar-dream search, automatic clustering, and Dream DNA analytics that turn months of journal history into a personal subconscious map across symbols, emotions, people, places, scenarios, trends, and correlations.
- Ask DreamLens: retrieval-backed questions over the user's own dream history.
- Premium Deep Interpretation using a stronger model and richer retrieved context.
- Cognito social sign-in provider setup for Google and Apple first; Facebook remains optional after product/privacy review.
- Admin/business metrics view for MAU, conversion, revenue, AI cost, AWS cost, cost per user, and gross margin.

## Known Gaps

- Expo typed routes are disabled because the current installed Expo CLI/router pair failed during typed-route generation.
- The Expo app is on SDK 56 locally because SDK 57 produced a `jest-expo` / React Native peer conflict during install.
- `npm test` uses `--forceExit` because the Expo/RN Jest environment leaves an open handle after tests complete.
- `npm install` reports moderate third-party audit findings; no forced audit fix has been applied.
- Production Cognito OAuth is scaffolded but not fully wired to hosted Cognito configuration or secure token persistence.
- Maestro mobile flow exists, but local verification is blocked until Maestro is installed.
- Terraform infrastructure is scaffolded, but local `terraform fmt`/`terraform validate`/`terraform plan` verification is blocked until Terraform is installed and AWS backend/account values exist.
- Terraform backend files are placeholders; real remote state bootstrap and GitHub environment variables are still needed before first cloud apply.
- Deployment workflows are scaffolded, and dev custom domains are live. Real QA/prod deployment still requires environment-specific Terraform values, GitHub environment setup, ECR/ECS/S3/CloudFront outputs, EAS project setup, and protected `prod` approvals.
- `pgvector` is planned but not implemented in API migrations or Terraform RDS initialization yet.
- Private S3 asset buckets for generated dream images, exports, and optional uploads are planned but not implemented yet.
- SQS queues/workers for image jobs, embedding backfills, exports, and future batch AI work are planned but not implemented yet.
- Local API image build verification remains blocked until Docker Desktop or another Docker daemon is running.
- k6 smoke test script exists, but local execution is blocked until k6 is installed and a local or deployed API endpoint is available.
- ADOT, CloudWatch alarms, and dashboard resources are scaffolded, but live telemetry still needs a real deployed task definition/collector sidecar configuration and AWS account validation.
- Astra config proves PersonaKit backend reuse and app brand switching, but there is not yet a separate Astra distribution, app icon/splash set, store metadata, or dedicated UI flow beyond the shared generic renderer.
- Monetization is mock-first: entitlement tiers, quota behavior, and paywall UI exist, but real RevenueCat/App Store/Google Play subscriptions, webhook validation, receipt verification, and store product IDs are not connected.
- Dev DNS aliases are live for `dev.dreamdna.world` and `api.dev.dreamdna.world`. Production DNS names, CloudFront-scoped WAF ARN, and final Cognito hosted-domain settings still need final launch confirmation.
- The current AI cost ledger covers dream interpretation. Future AI operations still need consistent per-operation ledger rows for embeddings, repair retries, image generation, transcription, Ask DreamLens, and deep interpretation.
- Dream result detail uses an in-memory submitted-result cache before falling back to `GET /v1/dreams/{id}`; Playwright covers the submit/result path, and S17+ should not depend on this cache behavior.
- Profile form uses simple text inputs for comma-separated traits; richer controls can be added after the core flows are complete.
