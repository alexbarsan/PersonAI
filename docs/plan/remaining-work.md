# Remaining Work

Last updated after S28 Ask Dream DNA implementation on 2026-09-02.

## Planned Slices

All planned slices S0-S21 are implemented.

## Post-S21 Product Backlog

The imported Catch Dreamer feature notes add several capabilities that are not fully implemented yet. Track the detailed backlog in `13-post-s21-feature-backlog.md`.

- Historical fact backfill: completed dreams created before S22 do not yet have the normalized `DreamFacts` projection.
- Native mobile export sharing, Cognito disable/delete procedure after approved anonymization, and a documented support path for statutory erasure requests.
- Select a supported launch image model, approve its access/cost profile, configure an image quota, and then enable the completed SQS/private-S3 dream-image workflow in a controlled environment.
- Embeddings and semantic memory using PostgreSQL `pgvector` and Amazon Bedrock Titan Embeddings V2 by default, not full-history prompts.
- Historical fact backfill and semantic clustering: the Dream DNA overview is implemented, while similarity has no matches until embeddings are available and clustering remains future work.
- S28 Ask Dream DNA is implemented with owner-scoped semantic retrieval, safety/schema validation, quotas, cost rows, evidence links, and UI. Live answers require Titan quota resolution and embedding backfill.
- Premium Deep Interpretation using a stronger model and richer retrieved context.
- Cognito social sign-in provider setup for Google and Apple first; Facebook remains optional after product/privacy review.
- Admin/business metrics view for MAU, conversion, revenue, AI cost, AWS cost, cost per user, and gross margin.
- Local-first voice capture: durable native recording backup, retryable upload outbox, Free device transcription when supported, Premium server transcription, and explicit local/AWS retention windows.

## Known Gaps

- Expo typed routes are disabled because the current installed Expo CLI/router pair failed during typed-route generation.
- The Expo app is on SDK 56 locally because SDK 57 produced a `jest-expo` / React Native peer conflict during install.
- `npm test` uses `--forceExit` because the Expo/RN Jest environment leaves an open handle after tests complete.
- `npm install` reports moderate third-party audit findings; no forced audit fix has been applied.
- Dev Cognito OAuth is wired to a hosted UI domain and the deployed web app is configured for real API mode. Production/QA Cognito domains, social providers, branded managed login, exact mobile callback URLs, and secure refresh-token persistence are still pending.
- Maestro mobile flow exists, but local verification is blocked until Maestro is installed.
- Terraform infrastructure is applied for dev. QA/prod still need remote state bootstrap, environment-specific Terraform values, GitHub environment variables, and protected `prod` approvals.
- The dev ECS service runs task-definition revision 27 with Amazon Transcribe enabled, API image `946afe1`, and a local mock Premium grant for the confirmed dev account. Public `/health/live` and `/health/ready` checks pass. A controlled 29-second multilingual Premium transcription completed on 2026-09-01; CloudWatch showed the `voice.transcription` ledger write, SQS drained, and both temporary voice S3 prefixes were empty afterward. The dev WAF counts only the managed `SizeRestrictions_BODY` subrule so bounded multipart audio reaches the API's 10 MB validation. Terraform revision 26 corrected the batch `TranscribeAudio` estimate from `$0.0004` to `$0.0001` per second based on the 2026-09-02 AWS Price List; CI revision 27 deployed the matching application defaults and preserved that runtime value.
- Deployment workflows are active for dev. Real QA/prod deployment still requires environment-specific ECR/ECS/S3/CloudFront outputs, EAS project setup, and final launch approvals.
- `pgvector` foundation, SQS job wiring, embedding handler, and owner-scoped similar-dream endpoint are implemented. Titan Embeddings V2 cannot complete in dev until AWS Support resolves this account's zero on-demand RPM allocation, so the similarity endpoint correctly returns no matches until embeddings are backfilled.
- A private KMS-encrypted S3 asset bucket and signed-access service are implemented. Voice input is private and deleted after transcription by default; explicit retention exposes it only through a short-lived signed URL. The future local-first client backup, Free device transcription, retry outbox, and tier-specific retention policy are tracked as S33. The premium dream-image handler and result UI are complete but disabled pending supported-model selection, explicit cost/quota configuration, Terraform IAM apply, and controlled dev verification. Export job generation remains future work.
- Encrypted SQS queue/DLQ, durable job records, worker leases, retries, and backfill mechanics are implemented. Concrete image and transcription handlers are complete; export handling remains future work.
- Local API image build verification remains blocked until Docker Desktop or another Docker daemon is running.
- k6 smoke test script exists, but local execution is blocked until k6 is installed and a local or deployed API endpoint is available.
- ADOT, CloudWatch alarms, and dashboard resources are scaffolded, but live telemetry still needs a real deployed task definition/collector sidecar configuration and AWS account validation.
- Astra config proves PersonaKit backend reuse and app brand switching, but there is not yet a separate Astra distribution, app icon/splash set, store metadata, or dedicated UI flow beyond the shared generic renderer.
- Monetization is mock-first: entitlement tiers, quota behavior, and paywall UI exist, but real RevenueCat/App Store/Google Play subscriptions, webhook validation, receipt verification, and store product IDs are not connected.
- Dev DNS aliases are live for `dev.dreamdna.world` and `api.dev.dreamdna.world`. Production DNS names, CloudFront-scoped WAF ARN, and final Cognito hosted-domain settings still need final launch confirmation.
- The AI cost ledger covers dream interpretation, S28 query embeddings/answers, dream-image attempts, and voice transcription, including operation type, model/provider, status, latency, failure category, and estimated cost. Background dream embeddings, interpretation repair retries, and deep interpretation still need consistent per-operation rows. S22 fact extraction does not call an AI model and therefore does not create a new ledger operation.
- DeepSeek currently maps the configured `deepseek-chat` compatibility alias to `deepseek-v4-flash`. Validate output compatibility and move to the explicit model id before production instead of relying on the deprecated alias.
- Dream result detail uses an in-memory submitted-result cache before falling back to `GET /v1/dreams/{id}`; Playwright covers the submit/result path, and S17+ should not depend on this cache behavior.
- Approved anonymization is implemented using the Terraform-managed `dreamlens-admin` Cognito group or configured subject allow-list. `ai.ro.dodoloata@gmail.com` has not yet registered in the existing dev pool; run `scripts/add-cognito-privacy-admin.ps1` for each environment after registration.
