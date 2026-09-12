# Remaining Work

Last updated during S47 admin operations and dream inspection on 2026-09-12.

## Planned Slices

All planned slices S0-S21 are implemented.

## Post-S21 Product Backlog

The imported Catch Dreamer feature notes add several capabilities that are not fully implemented yet. Track the detailed backlog in `13-post-s21-feature-backlog.md`.

- Historical fact backfill: completed dreams created before S22 do not yet have the normalized `DreamFacts` projection.
- Native mobile export sharing, Cognito disable/delete procedure after approved anonymization, and a documented support path for statutory erasure requests.
- Dream-image generation uses the OpenAI Images API in dev: Free uses `gpt-image-1-mini` Low at an estimated `$0.005` per 1024x1024 image and Premium uses Medium at `$0.011`. S46 queues `omni-moderation-latest` immediately after a completed dream is persisted and stores pending/processing/completed/failed state, provider/model, categories, prompt mode, latency, and failure kind. Image requests reuse completed classifications without another moderation call; pending, failed, and legacy unclassified dreams use a conservative symbolic prompt without blocking. The generator receives only an encrypted, bounded fact-based prompt. S45 adds daily limits of Free `1` and Premium `5`. Create separate OpenAI secrets before enabling QA or production.
- OpenAI may still decline a particular dream visual under its image safety policy. Treat this as a non-retryable, user-visible provider-policy result with no raw provider response shown; it does not block dream capture or interpretation. Never represent moderation as a policy bypass.
- Embeddings use PostgreSQL `pgvector` and 1,024-dimensional Amazon Titan Text Embeddings V2, not full-history prompts. S36 is deployed in dev, the retained `codex` corpus is embedded, and authenticated Similar Dreams and Ask Dream DNA smoke tests pass. Nova remains available for a future separate multimodal index.
- Historical fact backfill and semantic clustering: the Dream DNA overview and semantic similarity foundation are implemented, while clustering remains future work.
- S28 Ask Dream DNA is implemented with owner-scoped semantic retrieval, safety/schema validation, quotas, cost rows, evidence links, and UI. Live Titan-backed memory is available in dev and an authenticated five-source response passes.
- S34 interpretation feedback is implemented with persisted like/dislike state, controlled dislike reasons, optional details, export coverage, and anonymization cleanup.
- S29 Premium Deep Interpretation is deployed in dev with persisted owner-scoped results, `deepseek-v4-pro`, Titan/pgvector related-dream context, consent and quota controls, cost/latency ledger entries, and app UI. S39 raised the output cap to 4,096 after live 2,048-token truncation; the authenticated retry completed with five sources.
- Cognito social sign-in provider setup for Google and Apple first; Facebook remains optional after product/privacy review.
- Cognito password policy: dev now permits six-character passwords while retaining lowercase, uppercase, number, and symbol requirements. Apply and verify the same policy in QA and production before public launch.
- S31 provides an aggregate-only admin metrics API for active users, conversion, dream completion, AI cost, cost per active user, and operation latency. RevenueCat revenue and AWS Cost Explorer ingestion remain before gross margin can be calculated; an internal dashboard remains optional.
- S47 is implemented in code. Operations health and recovery use `dreamlens-metrics-admin`; cross-user dream search and detail use the stronger `dreamlens-admin` privacy role. Opening original dream text, interpretations, and signed images requires a case-specific purpose and writes an immutable access audit. Dev deployment and authenticated verification remain.
- S48 image latency optimization is a separate planned slice. Measure queue wait independently from provider generation, move workers into a separately scalable ECS service, scale on SQS backlog age/depth, add bounded concurrency, benchmark tier routes, and use live completion updates with polling fallback. Keep image generation explicitly on request; do not spend on speculative images.
- S37 sensitive-dream safety workflow: allow private adult sexual, violent, and trauma dream content while using contextual safety categories rather than keyword alerts. Notify reviewers with category, confidence, anonymized subject, dream ID, and timestamp only; raw-text access must be explicit, audited, and privacy-governed.
- Local-first voice capture: durable native recording backup, retryable upload outbox, Free device transcription when supported, Premium server transcription, and explicit local/AWS retention windows.

## Known Gaps

- Web/native authentication now persists the Cognito session across refresh and refreshes near-expiry tokens. Complete a live dev login-refresh-renewal smoke test after deployment; a future BFF with HttpOnly cookies would further reduce browser token exposure.

- Expo typed routes are disabled because the current installed Expo CLI/router pair failed during typed-route generation.
- The Expo app is on SDK 56 locally because SDK 57 produced a `jest-expo` / React Native peer conflict during install.
- `npm test` uses `--forceExit` because the Expo/RN Jest environment leaves an open handle after tests complete.
- `npm install` reports moderate third-party audit findings; no forced audit fix has been applied.
- Dev Cognito OAuth is wired to a hosted UI domain and the deployed web app is configured for real API mode. Production/QA Cognito domains, social providers, branded managed login, exact mobile callback URLs, and secure refresh-token persistence are still pending.
- Maestro mobile flow exists, but local verification is blocked until Maestro is installed.
- Terraform infrastructure is applied for dev. QA/prod still need remote state bootstrap, environment-specific Terraform values, GitHub environment variables, and protected `prod` approvals.
- The dev ECS service runs task-definition revision 47 with Amazon Transcribe, Titan Text Embeddings V2, `deepseek-v4-flash` base interpretation, and `deepseek-v4-pro` Premium Deep Interpretation with a 4,096-token output cap. Revision 47 is stable and the controlled S39 retrieval/Deep/Ask tests pass. The earlier 29-second multilingual Premium transcription also completed with ledger persistence, an empty SQS queue, and default source-object deletion. The dev WAF permits bounded multipart audio while API validation keeps the 10 MB limit.
- Deployment workflows are active for dev. Real QA/prod deployment still requires environment-specific ECR/ECS/S3/CloudFront outputs, EAS project setup, and final launch approvals.
- `pgvector`, SQS job wiring, owner-scoped retrieval, and the embedding handler are live in dev. S36 restored Titan Text Embeddings V2 after AWS access became available, filters retrieval to active vector metadata, replaces stale rows during backfill, and records background embedding cost/latency. Similar Dreams and Ask Dream DNA pass authenticated live tests against the retained corpus.
- A private KMS-encrypted S3 asset bucket and signed-access service are implemented. Voice input is private and deleted after transcription by default; explicit retention exposes it only through a short-lived signed URL. The future local-first client backup, Free device transcription, retry outbox, and tier-specific retention policy are tracked as S33. S44's moderation-aware prompt flow is live-verified with normal and adult-themed images. S45 deploys Free `1` and Premium `5` daily image limits before moderation/queueing. Complete one non-exempt Free-account `429` smoke test, then decide whether to expose quota usage in the entitlement/UI response; export job generation remains future work.
- Encrypted SQS queue/DLQ, durable job records, worker leases, retries, and backfill mechanics are implemented. Concrete image and transcription handlers are complete; export handling remains future work.
- Local API image build verification remains blocked until Docker Desktop or another Docker daemon is running.
- k6 smoke test script exists, but local execution is blocked until k6 is installed and a local or deployed API endpoint is available.
- ADOT, CloudWatch alarms, and dashboard resources are scaffolded, but live telemetry still needs a real deployed task definition/collector sidecar configuration and AWS account validation.
- Astra config proves PersonaKit backend reuse and app brand switching, but there is not yet a separate Astra distribution, app icon/splash set, store metadata, or dedicated UI flow beyond the shared generic renderer.
- Monetization is mock-first: entitlement tiers, quota behavior, and paywall UI exist, but real RevenueCat/App Store/Google Play subscriptions, webhook validation, receipt verification, and store product IDs are not connected.
- Dev DNS aliases are live for `dev.dreamdna.world` and `api.dev.dreamdna.world`. Production DNS names, CloudFront-scoped WAF ARN, and final Cognito hosted-domain settings still need final launch confirmation.
- The AI cost ledger covers base and deep interpretation, query and background embeddings, S28 answers, dream-image attempts, and voice transcription, including operation type, model/provider, status, latency, failure category, and estimated cost. Interpretation repair retries are still aggregated into their parent operation rather than stored as separate ledger rows. S22 fact extraction does not call an AI model and therefore does not create a new ledger operation.
- DeepSeek model routing uses explicit `deepseek-v4-flash` for base interpretation and `deepseek-v4-pro` for Premium deep interpretation. Four adult-only, non-graphic violent, consensual sexual, intense consensual sexual, and trauma cases completed base interpretation without refusal. The deep path now uses a 4,096-token cap and completed its controlled dev call; review current provider pricing before production.
- Dream result detail uses an in-memory submitted-result cache before falling back to `GET /v1/dreams/{id}`; Playwright covers the submit/result path, and S17+ should not depend on this cache behavior.
- Approved anonymization is implemented using the Terraform-managed `dreamlens-admin` Cognito group or configured subject allow-list. `ai.ro.dodoloata@gmail.com` is assigned to the dev admin groups; run the admin-assignment scripts after that user registers in each future QA/prod pool.
