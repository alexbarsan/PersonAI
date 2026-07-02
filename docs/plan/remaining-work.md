# Remaining Work

Last updated after S19 on 2026-07-02.

## Planned Slices

- S20: prove PersonaKit reuse with Astra configuration.
- S21: optional monetization decision and implementation if approved.

## Known Gaps

- Expo typed routes are disabled because the current installed Expo CLI/router pair failed during typed-route generation.
- The Expo app is on SDK 56 locally because SDK 57 produced a `jest-expo` / React Native peer conflict during install.
- `npm test` uses `--forceExit` because the Expo/RN Jest environment leaves an open handle after tests complete.
- `npm install` reports moderate third-party audit findings; no forced audit fix has been applied.
- Production Cognito OAuth is scaffolded but not fully wired to hosted Cognito configuration or secure token persistence.
- Maestro mobile flow exists, but local verification is blocked until Maestro is installed.
- Terraform infrastructure is scaffolded, but local `terraform fmt`/`terraform validate`/`terraform plan` verification is blocked until Terraform is installed and AWS backend/account values exist.
- Terraform backend files are placeholders; real remote state bootstrap and GitHub environment variables are still needed before first cloud apply.
- Deployment workflows are scaffolded, but real cloud deployment requires AWS account values, GitHub environment setup, ECR/ECS/S3/CloudFront outputs, EAS project setup, and protected `prod` approvals.
- Local API image build verification remains blocked until Docker Desktop or another Docker daemon is running.
- k6 smoke test script exists, but local execution is blocked until k6 is installed and a local or deployed API endpoint is available.
- ADOT, CloudWatch alarms, and dashboard resources are scaffolded, but live telemetry still needs a real deployed task definition/collector sidecar configuration and AWS account validation.
- Production DNS names, ACM certificates, CloudFront-scoped WAF ARN, and final Cognito hosted-domain settings still need real account/domain decisions.
- Dream result detail uses an in-memory submitted-result cache before falling back to `GET /v1/dreams/{id}`; Playwright covers the submit/result path, and S17+ should not depend on this cache behavior.
- Profile form uses simple text inputs for comma-separated traits; richer controls can be added after the core flows are complete.
