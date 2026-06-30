# Reuse Playbook

`decision-record.md` is authoritative. This playbook describes how to launch a new PersonaKit app without backend rewrites.

## Goal

A sibling app should be created by adding configuration:

- persona prompt template
- output schema
- result-section map
- brand config
- mock fixtures
- tests

Backend pipeline code should remain unchanged. If a new persona requires changing `InterpretationPipeline`, `ContextBuilder`, provider decorators, or core API behavior, treat that as a PersonaKit design failure unless the decision record is updated.

## Add A Persona

1. Create `personas/<persona-id>/`.
2. Add `persona.json`.
3. Add `prompt.scriban`.
4. Add `output.schema.json`.
5. Add `section-map.json`.
6. Add sample valid and invalid outputs for tests.
7. Add Verify snapshots for rendered prompts and mapped sections.

Persona ids currently reserved:

- `dream-interpreter`
- `astrologer`
- `gym-coach`
- `chef`

## Add Brand Config

Brand config should define:

- app name
- persona id
- primary colors
- typography tokens
- icon choices
- legal/disclaimer copy where product-specific
- app store metadata draft if relevant

The shared UI reads brand config; it should not fork screens for each persona unless the user workflow is truly different.

## Backend Checklist

- Persona registry loads the new persona.
- Prompt renderer snapshot passes.
- Output schema validates sample output.
- Section mapper produces generic UI sections.
- Pipeline works with the same `IInterpretationPipeline`.
- Provider abstraction remains unchanged.
- Context builder remains generic or uses persona-declared context requirements.

## Frontend Checklist

- Generic section renderer supports the persona section map.
- Brand config switches labels, colors, icons, and persona id.
- Mock API fixtures exist.
- Onboarding/profile asks only fields needed by the persona or shared profile.
- E2E happy path works in mock mode.

## When Code Changes Are Allowed

Code changes are acceptable for:

- adding generic renderer support for a new section kind
- adding reusable context fields that multiple personas can use
- fixing abstractions that were too DreamLens-specific
- adding tests and fixtures

Code changes are not acceptable for:

- hard-coding persona-specific branches in the pipeline
- adding provider-specific behavior outside adapters/decorators
- bypassing schema validation for a persona
- sending identifying user data to AI providers

## Astra Proof

S20 must prove:

- `astrologer` persona loads from config
- backend output validation works through the same interfaces
- UI renders Astra result sections using the generic renderer
- brand switch creates a white-label Astra build
- no DreamLens-only backend code path is copied

## Release Checklist For A New App

- Decision record updated if the product scope changes.
- Persona prompt reviewed for safety and injection rules.
- Output schema and section map tested.
- Brand config reviewed.
- Privacy wording reviewed.
- Quotas and costs configured.
- E2E mock flow passing.
- Store metadata and screenshots prepared if mobile release is in scope.
