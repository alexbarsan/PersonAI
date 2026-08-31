# Dream DNA / PersonaKit

Dream DNA is a dream-interpretation app built on PersonaKit, a reusable AI/persona pipeline for sibling apps. The product goal is to become a personal map of the user's subconscious over time, using structured dream history to surface recurring symbols, emotions, people, places, scenarios, and correlations. The current roadmap includes PostgreSQL `pgvector` semantic memory, Amazon Bedrock Titan embeddings by default, private S3 storage for generated images/exports/assets, SQS-backed async AI jobs, and per-operation AI cost/response-time tracking.

The planning source of truth lives in [docs/plan/readme.md](docs/plan/readme.md). Start with [docs/plan/decision-record.md](docs/plan/decision-record.md), then follow the slice workflow in [docs/plan/06-dev-orchestrator.md](docs/plan/06-dev-orchestrator.md).

Current implementation status is tracked in [SLICE-STATUS.md](SLICE-STATUS.md).

Manuals:

- [User manual](docs/user-manual.md)
- [Developer manual](docs/developer-manual.md)
- [Deployment manual](docs/deployment.md)
- [AWS Agent Toolkit setup](docs/aws-agent-toolkit-setup.md)
- [Observability manual](docs/observability.md)
