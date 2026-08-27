# Deployment

DreamLens deployment uses GitHub Actions OIDC to assume AWS roles. Do not create or store long-lived AWS access keys in GitHub.

## GitHub Environments

Create these GitHub environments:

- `dev`
- `qa`
- `prod`
- `mobile`

Protect `prod` and Terraform `apply` usage with reviewers before enabling real deployments.

## Required Variables

Set these as GitHub environment variables for `dev`, `qa`, and `prod`:

- `AWS_REGION`: AWS region, for example `us-east-1`.
- `AWS_ROLE_TO_ASSUME`: deployment role ARN created by Terraform output `github_deploy_role_arn`.
- `API_ECR_REPOSITORY`: ECR repository name for the API image.
- `API_ECS_CLUSTER`: ECS cluster name.
- `API_ECS_SERVICE`: ECS API service name.
- `WEB_BUCKET`: S3 bucket that hosts the Expo web export.
- `CLOUDFRONT_DISTRIBUTION_ID`: CloudFront distribution id for the web app.
- `API_BASE_URL`: public API base URL used by the Expo web build, for example `https://api.dev.dreamdna.world`.
- `MOCK_API`: `false` for deployed environments.
- `COGNITO_DOMAIN`: Cognito hosted-login domain, after it is configured.
- `COGNITO_CLIENT_ID`: Cognito app client id.

Current `dev` auth values:

- `COGNITO_DOMAIN`: `https://dreamlens-dev-379959319368.auth.us-east-1.amazoncognito.com`
- `COGNITO_CLIENT_ID`: `lbo45bf92ungifar7qab459gi`
- `MOCK_API`: `false`

The Expo web build reads these through direct `process.env.EXPO_PUBLIC_*` references in `app/src/core/config.ts`. Metro embeds them into the generated JavaScript bundle at build time. Do not rely on browser runtime `process.env` for deployed config.

## Required Secrets

Set these as GitHub secrets only where needed:

- `EAS_TOKEN`: Expo account token used by the mobile EAS workflow.

AWS application secrets belong in Secrets Manager through Terraform and deployment operations, not GitHub secrets.

## Terraform State Bootstrap

Before running the real Terraform backend, create per-environment state resources:

- `TERRAFORM_STATE_BUCKET`
- `TERRAFORM_LOCK_TABLE`

Then copy `infra/envs/<env>/backend.tf.example` to `infra/envs/<env>/backend.tf` locally or provide backend config during CI. Do not commit account-specific backend files until the account and naming decision is explicit.

For `dev`, after the AWS profile is configured:

```powershell
powershell.exe -ExecutionPolicy Bypass -File scripts\bootstrap-terraform-state.ps1 -Environment dev -ProfileName dreamlens-dev -Region us-east-1 -WriteBackendFile
powershell.exe -ExecutionPolicy Bypass -File scripts\terraform-plan-env.ps1 -Environment dev -ProfileName dreamlens-dev
```

Repeat the same pattern for `domain`, `qa`, and `prod` after deciding which AWS profile can deploy each environment:

```powershell
powershell.exe -ExecutionPolicy Bypass -File scripts\bootstrap-terraform-state.ps1 -Environment domain -ProfileName dreamlens-dev -Region us-east-1 -WriteBackendFile
powershell.exe -ExecutionPolicy Bypass -File scripts\terraform-plan-env.ps1 -Environment domain -ProfileName dreamlens-dev

powershell.exe -ExecutionPolicy Bypass -File scripts\bootstrap-terraform-state.ps1 -Environment qa -ProfileName dreamlens-dev -Region us-east-1 -WriteBackendFile
powershell.exe -ExecutionPolicy Bypass -File scripts\terraform-plan-env.ps1 -Environment qa -ProfileName dreamlens-dev

powershell.exe -ExecutionPolicy Bypass -File scripts\bootstrap-terraform-state.ps1 -Environment prod -ProfileName dreamlens-dev -Region us-east-1 -WriteBackendFile
powershell.exe -ExecutionPolicy Bypass -File scripts\terraform-plan-env.ps1 -Environment prod -ProfileName dreamlens-dev
```

The generated `backend.tf` and `terraform.tfvars` files are local account-specific files. Review them before applying infrastructure changes.

## Custom Domains

Use lowercase DNS names for the production domain: `dreamdna.world`.

Current dev aliases are live:

- Dev web: `https://dev.dreamdna.world`
- Dev API: `https://api.dev.dreamdna.world`

Recommended public names for web-first launch:

- Dev web: `dev.dreamdna.world`
- Dev API: `api.dev.dreamdna.world`
- Web: `dreamdna.world` and `www.dreamdna.world`
- API: `api.dreamdna.world`
- QA web: `qa.dreamdna.world`
- QA API: `api.qa.dreamdna.world`

Before assigning custom domains:

1. Apply `infra/envs/domain` first. It creates the Route 53 hosted zone, public ACM certificate, and ACM DNS validation records.
2. If the domain was bought outside Route 53, update the registrar nameservers to the `hosted_zone_name_servers` Terraform output.
3. Wait until the ACM certificate is `ISSUED`.
4. Add the certificate ARN and hosted-zone id to `qa` and `prod` `terraform.tfvars`:
   - `web_acm_certificate_arn`
   - `api_acm_certificate_arn`
   - `hosted_zone_id`
5. Set `web_domain_aliases` and `api_domain_name` for the environment.
6. Apply the environment. Terraform creates DNS alias records:
   - Web aliases to the CloudFront distribution.
   - API alias to the API load balancer.

The CloudFront certificate must be in `us-east-1`. The API ALB certificate must be in the API region; because the current environments also run in `us-east-1`, the same certificate ARN can be used for web and API.

## Cognito Hosted Login

Terraform creates a Cognito hosted UI prefix domain when `cognito_domain_prefix` is set in the environment `terraform.tfvars`.

For `dev`, the prefix is:

```hcl
cognito_domain_prefix = "dreamlens-dev-379959319368"
```

The current Terraform provider manages the classic hosted UI domain (`managed_login_version = 1`). Before public launch, prefer upgrading QA/prod to a branded managed-login v2 or custom auth domain after confirming provider support for managed-login branding resources, or apply Cognito branding as a documented one-time operational step.

Registered callback/logout URLs must match the app runtime URLs exactly. The web launch path is:

- Dev callback/logout: `https://dev.dreamdna.world`
- Local web callback/logout: `http://localhost:8081`

Add exact native mobile callback URLs after the EAS/dev-client URL scheme is verified.

## Workflows

- `ci.yml`: foundation validation for pull requests and pushes.
- `infra.yml`: Terraform static checks, `fmt`, `validate`, and manual plan/apply.
- `api-deploy.yml`: test, container build, ECR push, ECS task-definition render, and ECS service rollout.
- `web-deploy.yml`: test, typecheck, Expo web export, S3 sync, and CloudFront invalidation.
- `mobile-eas.yml`: manual EAS build placeholder for iOS/Android.

S18 wires the deployment paths. Dev AWS infrastructure and custom domains are applied. Actual QA/prod rollout still requires environment-specific tfvars, remote state bootstrap, GitHub environment variables, protected approvals, and final app/domain decisions.

Terraform owns the infrastructure and bootstrap task definition. GitHub Actions owns normal application image rollouts by registering a new ECS task-definition revision from the currently deployed definition. Terraform intentionally ignores the ECS service `task_definition` pointer so infrastructure applies do not roll the service back to the bootstrap image.
