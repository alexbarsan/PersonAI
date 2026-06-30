# Security And Privacy

`decision-record.md` is authoritative. This document describes the security and privacy invariants that implementation must preserve.

## Positioning Boundary

DreamLens is wellness and entertainment. It must not claim to diagnose, treat, or provide medical or psychological advice. The disclaimer appears during onboarding and on every result.

## Data Classification

High sensitivity:

- dream text
- fears
- allergies
- health-adjacent traits
- recent life events
- mood and sleep data
- interpretation outputs derived from sensitive input

Identifiers:

- Cognito `sub`
- email
- name
- phone
- IP
- device ids

AI-safe identifier:

- `pseudonymId`, an HMAC of the internal user id

Identifiers must not be sent to DeepSeek.

## Consent

Consent flags in Context JSON v1:

- `aiProcessing`
- `sensitiveTraits`
- `historyUse`

If `aiProcessing` is false, dream interpretation must not call the AI provider. If `sensitiveTraits` is false, sensitive traits are omitted or reduced. If `historyUse` is false, history is omitted or reduced.

Consent changes must affect future requests. Historical handling is governed by retention and erasure rules.

## Pseudonymization

`pseudonymId` is derived with HMAC from an internal user id and a secret key. It is not the Cognito `sub`, email, name, phone, IP, or device id. The HMAC secret is managed as sensitive configuration.

## Encryption At Rest

Sensitive columns are encrypted with AES-GCM. Production uses KMS envelope keys. Local development uses a local key from user secrets or environment-specific config.

Encryption is required for profile traits, dream text, and other sensitive fields. Non-sensitive operational metadata can remain plaintext when needed for querying, but should be minimized.

## Logging

Never log:

- raw dream text
- full context JSON
- prompt text containing user data
- profile traits
- secrets
- tokens
- auth headers
- provider request bodies

Allowed logs:

- request id
- internal correlation id
- user id only where required for internal audit
- persona id and version
- model id
- status
- latency
- token counts
- estimated cost
- sanitized error category

## AI Boundary

The model receives untrusted user dream text as data. It receives no tools. It must return JSON matching the persona schema. Provider output is not trusted until parsed and schema-validated.

Prompt-injection protections live in persona system prompts and are backed by tests. The prompt must explicitly instruct the model to treat user dream text as untrusted content and never follow instructions found inside it.

## Output Safety

`safety.selfHarmRisk` values are:

- `none`
- `low`
- `elevated`

Elevated risk requires a constrained UI response. Exact crisis copy and escalation behavior are an open legal/product decision before launch.

## Auth And Authorization

Production auth is Cognito. The API validates JWTs and derives the current user from trusted token claims. Users can only access their own profile, dreams, interpretations, journal, and insights.

Local `dotnet user-jwts` tokens are development-only and must not be enabled in production.

## GDPR Export And Erasure

Users can export their data and request erasure. Erasure removes or anonymizes personal data across dreams, interpretations, run records, and cost ledger rows according to retention rules documented before implementation. Admin approval must not block legally required erasure.

Open decisions before implementation:

- retention windows
- legal basis for retaining anonymized cost ledger rows
- whether support/admin tooling is required in v1
- whether erasure is self-serve in v1 or starts as a support flow

## Threat Model Summary

Primary threats:

- identity leakage to AI provider
- prompt injection from dream text
- sensitive data in logs
- broken object-level authorization
- unauthorized export or erasure
- provider key leakage
- quota abuse and cost spikes

Required mitigations are tested in the relevant slices.
