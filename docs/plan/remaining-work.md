# Remaining Work

Last updated after S22 structured dream facts on 2026-08-29.

## Planned Slices

All planned slices S0-S21 are implemented.

## Post-S21 Product Backlog

The imported Catch Dreamer feature notes add several capabilities that are not fully implemented yet. Track the detailed backlog in `13-post-s21-feature-backlog.md`.

- Historical fact backfill: completed dreams created before S22 do not yet have the normalized `DreamFacts` projection.
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
- Dev Cognito OAuth is wired to a hosted UI domain and the deployed web app is configured for real API mode. Production/QA Cognito domains, social providers, branded managed login, exact mobile callback URLs, and secure refresh-token persistence are still pending.
- Maestro mobile flow exists, but local verification is blocked until Maestro is installed.
- Terraform infrastructure is applied for dev. QA/prod still need remote state bootstrap, environment-specific Terraform values, GitHub environment variables, and protected `prod` approvals.
- Deployment workflows are active for dev. Real QA/prod deployment still requires environment-specific ECR/ECS/S3/CloudFront outputs, EAS project setup, and final launch approvals.
- `pgvector` foundation, SQS job wiring, and embedding handler are implemented. Titan Embeddings V2 cannot complete in dev until AWS Support resolves this account's zero on-demand RPM allocation; similar-dream product endpoints remain S27 work.
- A private KMS-encrypted S3 asset bucket and signed-access service are implemented. Image/export/upload job handlers and user-facing asset flows remain S23-S25 work.
- Encrypted SQS queue/DLQ, durable job records, worker leases, retries, and backfill mechanics are implemented. Concrete image/export/transcription job handlers remain future work.
- Local API image build verification remains blocked until Docker Desktop or another Docker daemon is running.
- k6 smoke test script exists, but local execution is blocked until k6 is installed and a local or deployed API endpoint is available.
- ADOT, CloudWatch alarms, and dashboard resources are scaffolded, but live telemetry still needs a real deployed task definition/collector sidecar configuration and AWS account validation.
- Astra config proves PersonaKit backend reuse and app brand switching, but there is not yet a separate Astra distribution, app icon/splash set, store metadata, or dedicated UI flow beyond the shared generic renderer.
- Monetization is mock-first: entitlement tiers, quota behavior, and paywall UI exist, but real RevenueCat/App Store/Google Play subscriptions, webhook validation, receipt verification, and store product IDs are not connected.
- Dev DNS aliases are live for `dev.dreamdna.world` and `api.dev.dreamdna.world`. Production DNS names, CloudFront-scoped WAF ARN, and final Cognito hosted-domain settings still need final launch confirmation.
- The current AI cost ledger covers dream interpretation. Future AI operations still need consistent per-operation ledger rows for embeddings, repair retries, image generation, transcription, Ask DreamLens, and deep interpretation. S22 fact extraction does not call an AI model and therefore does not create a new ledger operation.
- Dream result detail uses an in-memory submitted-result cache before falling back to `GET /v1/dreams/{id}`; Playwright covers the submit/result path, and S17+ should not depend on this cache behavior.
- Profile form uses simple text inputs for comma-separated traits; richer controls can be added after the core flows are complete.
