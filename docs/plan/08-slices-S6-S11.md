# Slices S6-S11

These prompts build the PersonaKit core and first DreamLens backend features. `decision-record.md` is authoritative.

## S6 - Persona Engine

Goal: load persona configs, render Scriban prompts in strict mode, and validate output schemas.

Tests first:

- Persona registry loads `dream-interpreter` config from `personas/`.
- Missing persona id returns a controlled error.
- Scriban strict mode fails on missing variables.
- Rendered prompt snapshot is stable with Verify.
- Output schema for `dream-interpreter` accepts canonical AI Output JSON v1 and rejects invalid shapes.

Implementation:

- Define persona config format: id, version, prompt template path, output schema path, section map path, brand defaults if needed.
- Add `IPersonaRegistry`.
- Add `IPromptRenderer`.
- Add JsonSchema.Net validator.
- Add initial `dream-interpreter` persona files.
- Add prompt snapshot tests.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S6): add persona engine`

## S7 - Context Builder

Goal: build Context JSON v1 from profile, consent, history, and dream input.

Tests first:

- Context contains `pseudonymId`, not Cognito `sub`, email, name, phone, IP, or device ids.
- Context includes full non-sensitive profile snapshot when consent allows.
- Sensitive traits are omitted or reduced when `sensitiveTraits` is false.
- History is omitted or reduced when `historyUse` is false.
- Dream text is capped at 10-4000 chars and marked as untrusted.
- Verify snapshot covers canonical context shape.

Implementation:

- Add `IContextBuilder`.
- Add pseudonym HMAC service.
- Add context DTOs matching `decision-record.md`.
- Add consent gating.
- Add history summary provider interface with fake/no-op implementation if journal does not exist yet.
- Add validation for dream input.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S7): add pseudonymized context builder`

## S8 - Interpretation Pipeline

Goal: orchestrate moderation/precheck, context building, prompt rendering, AI call, validation, repair retry, mapping, and persistence.

Tests first:

- Valid AI output maps to UI Response DTO v1 sections.
- Invalid JSON triggers exactly one repair retry.
- Second invalid response returns friendly 503-style failure.
- Provider failures are recorded as failed AI runs.
- Pipeline persists interpretation and run metadata on success.
- Pipeline never logs raw dream text.

Implementation:

- Add `IInterpretationPipeline`.
- Add pipeline step classes or a single orchestrator with small collaborators.
- Add output-to-section mapper using persona section map.
- Add AI run record persistence.
- Add friendly error types.
- Add no-op moderation/precheck placeholder if full moderation is deferred.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S8): add interpretation pipeline`

## S9 - Dream Endpoints

Goal: expose `POST /v1/dreams` and `GET /v1/dreams/{id}`.

Tests first:

- Unauthenticated dream submission returns 401.
- Invalid dream text length returns 400.
- Valid dream submission returns completed UI Response DTO v1.
- User can fetch their own dream by id.
- User cannot fetch another user's dream.
- DeepSeek invalid-output path returns friendly failure.

Implementation:

- Add dream entities and migration.
- Add submit and get handlers.
- Wire pipeline into `POST /v1/dreams`.
- Persist dream input and interpretation result.
- Return canonical DTO.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S9): add dream interpretation endpoints`

## S10 - Journal And Insights Endpoints

Goal: add journal listing/deletion and basic insight summaries.

Tests first:

- `GET /v1/dreams` lists current user's dreams.
- Delete removes or tombstones a dream according to retention rules.
- `GET /v1/insights` returns recurring themes and streaks for current user.
- User isolation is enforced.

Implementation:

- Add list endpoint if not already present in route map.
- Add delete endpoint if accepted for v1 journal management.
- Add insights read model or query projection.
- Keep computations simple and deterministic.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S10): add journal and insights endpoints`

## S11 - Rate Limits, Quotas, Abuse Protection, AI Cost Ledger

Goal: prevent cost spikes and track AI usage.

Tests first:

- Per-user daily quota blocks excess dream submissions.
- Rate limiting returns 429 with safe response body.
- AI cost ledger records provider, model, persona, tokens, latency, estimated cost, and status.
- Failed AI calls are counted according to configured policy.

Implementation:

- Configure ASP.NET Core rate limiting.
- Add quota options and quota service.
- Add AI cost ledger table and migration.
- Ensure ledger does not store raw prompt or dream text.
- Add cost metrics hooks.

Verification:

- `dotnet test api/DreamLens.sln`

Commit:

- `feat(S11): add quotas and AI cost ledger`
