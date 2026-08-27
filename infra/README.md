# DreamLens Infrastructure

Terraform code for DreamLens AWS infrastructure.

## Layout

```text
infra/
  modules/
    network/
    security/
    rds-postgres/
    cognito/
    ecs-api/
    web-cdn/
    observability/
  envs/
    dev/
    qa/
    prod/
```

## Local Checks

Terraform is required for native validation:

```powershell
terraform fmt -check -recursive infra
terraform -chdir=infra/envs/dev init -backend=false
terraform -chdir=infra/envs/dev validate
terraform -chdir=infra/envs/qa init -backend=false
terraform -chdir=infra/envs/qa validate
terraform -chdir=infra/envs/prod init -backend=false
terraform -chdir=infra/envs/prod validate
```

The repository also includes a static structure check that does not require Terraform:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-s17-infra.ps1
```

## Remote State

Remote state is intentionally not hard-coded because bucket names, lock tables, regions, and account ids are account-specific. Copy the relevant `backend.tf.example` file to `backend.tf` in each environment and fill in the values after the AWS account bootstrap exists.

Do not commit real secrets, account ids, backend bucket names, or generated state.
