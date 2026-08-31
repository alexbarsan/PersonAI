# Dream DNA Developer Manual

## Prerequisites

- .NET 9 SDK
- Node 22
- npm
- Docker Desktop for container/Testcontainers checks
- Terraform for infrastructure validation
- k6 for load smoke tests
- Maestro for mobile E2E

Some tools are optional until their slice is being verified. The repo is designed so missing Docker, Terraform, k6, or Maestro is documented as a local blocker rather than blocking normal API/app development.

## Repository Layout

- `api/`: .NET 9 backend and PersonaKit.
- `app/`: Expo React Native app.
- `personas/`: versioned persona configs.
- `infra/`: Terraform AWS infrastructure.
- `docs/`: user, developer, deployment, and observability docs.
- `scripts/`: repository static checks.
- `tests/load/`: k6 smoke scripts.

## Start The API

From the repository root:

```powershell
dotnet run --project api/src/DreamLens.Api/DreamLens.Api.csproj
```

Development OpenAPI is available at:

- `http://localhost:5000/openapi/v1.json`
- `http://localhost:5000/swagger`

Profile and dream endpoints require database and encryption settings. Mocked integration tests show the expected configuration shape.

## Start The App

```powershell
cd app
npm install
npm run web
```

The app defaults to mock API mode through `app/app.json`.

## Voice Transcription

`VoiceTranscription` is disabled by default in local configuration. The API accepts only authenticated Premium uploads and enforces supported audio types, a 10 MB size cap, a three-minute duration cap, and a daily request cap. Input and generated transcript objects use the private asset bucket; source audio is deleted after extraction unless `retainRecording` is explicitly true.

For AWS, use `Provider=amazon-transcribe`, a private asset bucket, SQS worker, and task-role `transcribe:StartTranscriptionJob` / `transcribe:GetTranscriptionJob` permissions. Terraform declares this only for dev, and the task-role policy is applied. The live dev task definition remains safely disabled until its Terraform revision drift is reconciled. Before enabling QA or production, complete one controlled Premium dev transcription, verify the `voice.transcription` ledger row and source-object deletion, then apply equivalent environment configuration.

To select the Astra brand variant for a build or shell session:

```powershell
$env:DREAMLENS_APP_VARIANT = "astra"
npm run web
```

## Test Commands

Repository checks:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-s0-structure.ps1
powershell -ExecutionPolicy Bypass -File scripts/check-s17-infra.ps1
powershell -ExecutionPolicy Bypass -File scripts/check-s18-workflows.ps1
powershell -ExecutionPolicy Bypass -File scripts/check-s19-observability.ps1
powershell -ExecutionPolicy Bypass -File scripts/check-s20-astra.ps1
```

API:

```powershell
dotnet test api/DreamLens.sln --configuration Release
```

Build the API container from the repository root so the runtime persona files are included:

```powershell
docker build -f api/src/DreamLens.Api/Dockerfile .
```

App:

```powershell
cd app
npm test
npm run typecheck
npm run e2e:web
npm run build:web
```

Infrastructure, when Terraform is installed:

```powershell
terraform fmt -check -recursive infra
terraform -chdir=infra/envs/dev init -backend=false
terraform -chdir=infra/envs/dev validate
terraform -chdir=infra/envs/prod init -backend=false
terraform -chdir=infra/envs/prod validate
```

Load smoke, when k6 is installed and an API is available:

```powershell
$env:DREAMLENS_BASE_URL = "http://localhost:5000"
k6 run tests/load/dream-smoke.js
```

## Modify PersonaKit

Add new personas under `personas/<persona-id>/` with:

- `persona.json`
- `prompt.scriban`
- `output.schema.json`
- `section-map.json`

The reusable pipeline should not need backend code changes for a new persona when the schema and section map fit the generic renderer contract.

## Deployment

See `docs/deployment.md`. Deployment uses GitHub Actions OIDC, Terraform, ECS Fargate, RDS PostgreSQL, Cognito, S3/CloudFront, and EAS placeholders for mobile. Planned post-S21 infrastructure adds PostgreSQL `pgvector`, private S3 asset buckets, SQS worker queues, and Amazon Bedrock embedding permissions.

## Monetization / S21

S21 can start without subscribing to third-party payment services if the work is limited to local entitlement models, mock paywall UI, quota behavior by tier, and provider abstractions.

The current implementation follows that mock-first approach:

- API endpoint: `GET /v1/entitlements`.
- Free tier: lower daily dream quota.
- Premium tier: higher daily dream quota and deep-analysis entitlement flag.
- App route: `/paywall`.
- Purchase button: intentionally disabled until a real provider is connected.

Real purchase verification and store-ready flows require third-party setup before final validation:

- RevenueCat account/project, if RevenueCat is selected.
- Apple App Store Connect products for iOS.
- Google Play Console products for Android.
- Store bundle identifiers and app metadata.
- GitHub/app secrets for payment provider keys.

Do not hard-code payment provider keys or store secrets in the repository.
