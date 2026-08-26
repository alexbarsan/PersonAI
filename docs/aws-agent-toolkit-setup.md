# AWS Agent Toolkit Setup

This repository uses the AWS Agent Toolkit setup flow for AWS-aware CI/CD work.

## Chosen Setup

- AWS experience: advanced AWS experience.
- Agent Toolkit region: `us-east-1`.
- Default application region: `us-east-1`.
- Initial environment: `dev`.
- Planned environments: `dev`, `qa`, `prod`.
- Recommended local profile names:
  - `dreamlens-dev`
  - `dreamlens-qa`
  - `dreamlens-prod`

AWS toolkit credentials are created with browser-based `aws login`. Do not create or store long-lived AWS access keys.

Credentials are valid for 12 hours and can be renewed for 90 days without re-authenticating in the browser.

## Local Tooling

Required tools:

- AWS CLI v2
- `uv`
- Terraform
- Docker Desktop
- k6
- Java 17
- Maestro

After installing tools on Windows, open a new terminal so PATH updates are visible.

## Dev Login

Use this profile unless you choose a different name:

```powershell
aws configure set region us-east-1 --profile dreamlens-dev
aws login --region us-east-1 --profile dreamlens-dev
aws sts get-caller-identity --profile dreamlens-dev
```

Then configure the Agent Toolkit:

```powershell
aws configure agent-toolkit --yes --region us-east-1 --profile dreamlens-dev
aws agent-toolkit list-available-skills --region us-east-1 --profile dreamlens-dev
```

If the toolkit wizard requires an interactive terminal, run this manually:

```powershell
aws configure agent-toolkit --region us-east-1 --profile dreamlens-dev
```

## MCP Profile Fix

After the toolkit configures MCP servers, ensure each generated `aws-mcp` entry includes:

```json
"env": {
  "AWS_MCP_PROXY_PROFILES": "dreamlens-dev"
}
```

To add QA or production later, authenticate each profile and change the value to:

```json
"env": {
  "AWS_MCP_PROXY_PROFILES": "dreamlens-dev dreamlens-qa dreamlens-prod"
}
```

Restart the AI tool after changing MCP configuration.
