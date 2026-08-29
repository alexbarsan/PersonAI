# DreamLens / PersonaKit — Development Master Plan (Index)

This folder is the complete, agent-executable development master plan for DreamLens — a dream-interpretation app where the user describes a dream, the API assembles a pseudonymized Context JSON snapshot of the user, sends it to DeepSeek, validates and maps the AI’s JSON response, and returns a UI-friendly result. The long-term product target is a personal map of the user's subconscious over time: recurring symbols, emotions, people, places, scenarios, trends, and correlations from months of journal history. PersonaKit is the reusable AI core (provider abstraction + persona engine + context builder + interpretation pipeline) extracted from day one so sibling apps (astrology “Astra”, gym coach “Coach”, cooking “Sage”) launch by swapping a persona config, not by writing new backend code. The plan is split into 21 linear slices (S0–S21) that a coding agent executes end to end, TDD-first, with a human auditing every slice.

## How to use this plan
1.	**Create the new repo**. Make an empty `dreamlens` git repository (GitHub, default branch `main`).
2.	**Copy this folder into it**. Move the entire contents of this folder to `docs/plan/` in the new repo (S0 formalizes this — the plan docs are committed as part of the first slice).
3.	**Start the agent**. Open a coding-agent session (e.g., Claude Code) at the repo root and paste the **MASTER ORCHESTRATOR PROMPT** from 06-dev-orchestrator.md as the first message.
4.	**Let the agent run S0 → S21 in order**. For each slice the agent writes the listed tests first, confirms they fail (red), implements to green, refactors, runs the full suite (`dotnet test` in `api/`, `npm test` in `app/` once it exists, Maestro/Playwright when UI flows changed), commits once (`feat(Sx): ...`), and updates `SLICE-STATUS.md` in the same commit.
5.	**Human-review every slice**. After each slice, run **the 5-minute audit checklist** in 06-dev-orchestrator.md before telling the agent to proceed to the next slice.

## Reading order
**First**: decision-record.md (the law), then 00-overview.md (the shape).
**Architecture**: 01-backend-architecture.md + 02-frontend-architecture.md + 03-aws-infrastructure.md + 04-security-privacy.md + 05-testing-strategy.md.
**Execution**: 06-dev-orchestrator.md, then the slice prompt files 07-slices-S0-S5.md → 08-slices-S6-S11.md → 09-slices-S12-S16.md → 10-slices-S17-S21.md.
**Reference while building**: 11-runtime-prompts.md and 12-reuse-playbook.md.

## Document map

| File | Purpose |
|------|---------|
| README.md | This index — what the plan is and how to drive it. |
| 00-overview.md | Product vision, DreamLens + PersonaKit scope, phase/slice overview, end-to-end request flow. |
| 01-backend-architecture.md | .NET 9 Minimal API, Vertical Slice Architecture, CQRS-lite (no MediatR), PersonaKit design, EF Core 9 + PostgreSQL/pgvector, AI provider abstractions, SQS async jobs, private S3 asset storage, design patterns and SOLID mapping. |
| 02-frontend-architecture.md | Expo (React Native + TypeScript) app for iOS + Android + Web: Expo Router, TanStack Query + Zustand, react-hook-form + zod, generic `sections[]` renderer, white-label brand config, EAS builds. |
| 03-aws-infrastructure.md | Terraform IaC: ECS Fargate + ALB + WAF, RDS PostgreSQL/pgvector, Cognito, S3 + CloudFront web, private S3 assets, SQS async jobs, Bedrock embeddings, Secrets Manager, GitHub Actions OIDC. |
| 04-security-privacy.md | Pseudonymization (HMAC pseudonymId), consent gating, AES-GCM column encryption with KMS envelope keys, prompt-injection firewall, data export, and approval-gated anonymization. |
| 05-testing-strategy.md | Test pyramid: xUnit + FluentAssertions + NSubstitute, WebApplicationFactory + Testcontainers + WireMock.Net, Verify snapshots, RNTL + Maestro + Playwright + MSW, k6 load. |
| 06-dev-orchestrator.md | The MASTER ORCHESTRATOR PROMPT, per-slice workflow rules, the 5-minute human audit checklist, SLICE-STATUS.md format, stuck/red-suite recovery protocol. |
| 07-slices-S0-S5.md | Copy-paste slice prompts S0–S5: monorepo + CI, walking skeleton, Postgres/EF Core, Cognito auth, encrypted user profile, DeepSeek `IChatClient` + resilience decorators. |
| 08-slices-S6-S11.md | Slice prompts S6–S11: persona engine (Scriban + JsonSchema.Net), context builder, interpretation pipeline, dream endpoints, journal/insights, rate limits + AI cost ledger. |
| 09-slices-S12-S16.md | Slice prompts S12–S16: Expo scaffold, onboarding wizard + profile UI, dream capture + result screens, journal/insights UI, Maestro + Playwright E2E harness. |
| 10-slices-S17-S21.md | Slice prompts S17–S21: Terraform AWS infra, CI/CD pipelines, observability + hardening + k6, astrologer persona + white-label Astra build, optional monetization/store readiness. |
| 11-runtime-prompts.md | Runtime AI assets: persona system prompts (dream-interpreter and siblings), injection-firewall rules, output schemas, repair-retry prompt. |
| 12-reuse-playbook.md | Step-by-step recipe for launching a new PersonaKit app (persona config + section map + brand config) without backend code changes. |
| 13-post-s21-feature-backlog.md | Post-S21 feature backlog: richer dream outputs, journal v2, voice, images, embeddings, Dream DNA, similar dreams, Ask DreamLens, deep interpretation, social sign-in, and admin metrics. |
| decision-record.md | **Single source of truth**: every decision, canonical names, repo layout, the three v1 schemas, slice map, workflow rules, privacy invariants. |

## The three load-bearing rules

1. **decision-record.md wins every conflict**. If any document, prompt, or agent output disagrees with it, the decision record is right. Changing a decision means updating decision-record.md first, then the affected docs.
2. **TDD red-green per slice, agent-run**. Every slice starts with the listed tests written first and run to confirm they fail; after implementing, the agent runs the FULL test suite and must NOT proceed to the next slice on red.
3. **Slices run in linear order S0 → S21**. No skipping, no reordering, one conventional commit per slice with `SLICE-STATUS.md` updated in the same commit.

