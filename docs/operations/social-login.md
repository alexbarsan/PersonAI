# Google And Apple Login

Dream DNA uses the existing Cognito managed login with authorization code plus PKCE. When a provider is configured, Cognito displays its button on the same Sign In screen used by web, Android, and iOS. The app receives only Cognito tokens, never a Google or Apple token directly.

## Before Applying Terraform

Configure each provider independently in `dev` first, then repeat with separate production credentials. Do not reuse an Apple private key or Google secret across environments.

The Cognito callback URI for each environment is:

| Environment | Callback URI |
| --- | --- |
| Dev | `https://dreamlens-dev-379959319368.auth.us-east-1.amazoncognito.com/oauth2/idpresponse` |
| Production | `https://dreamlens-prod-379959319368.auth.us-east-1.amazoncognito.com/oauth2/idpresponse` |

## Google

1. In Google Cloud Console, create a project for the applicable environment.
2. Configure the OAuth consent screen, including the public privacy-policy and terms URLs before production review.
3. Create an OAuth client of type **Web application**.
4. Add the matching Cognito callback URI as an authorized redirect URI.
5. Copy the client ID and client secret. Do not create an Android or iOS OAuth client for this Cognito federation flow yet; Cognito is the relying-party callback.

## Apple

1. In Apple Developer, create a separate Services ID for the applicable environment, for example `world.dreamdna.login` in production and `world.dreamdna.dev.login` in dev.
2. Enable Sign in with Apple for that Services ID and set the matching Cognito callback URI as its return URL.
3. Associate the Services ID with the primary App ID that will later be used by the iOS app.
4. Create a Sign in with Apple key, enable the service, and download its `.p8` private key once.
5. Record the Services ID as `client_id`, plus the Apple Team ID, Key ID, and full PEM private-key content.

Apple only returns the user name on the first successful authorization. Dream DNA maps it to Cognito's standard `name` attribute when available; the profile username remains the app's required identity field.

## Supply Credentials

Copy the commented examples in `infra/envs/<environment>/terraform.tfvars.example` into the untracked local `terraform.tfvars` file, then fill in the values. Alternatively provide `TF_VAR_google_oauth` and `TF_VAR_apple_oauth` through a protected deployment environment.

The values are marked sensitive in Terraform output, but Cognito federation credentials are represented in encrypted remote Terraform state. Restrict state access to production operators. Never commit `.tfvars`, `.p8` files, client secrets, or shell history containing those values.

## Apply And Verify

1. Run `terraform plan` then `terraform apply` for the environment.
2. Open the Dream DNA Sign In action. Cognito should show Google and/or Apple alongside email sign-in.
3. Complete one new-provider sign-in in a private browser session.
4. Confirm Dream DNA loads the authenticated home page and a profile can be saved.
5. Confirm an existing email/password account is not silently merged with a social account. Account linking requires an explicit, separately designed verified-email flow.

## Current Scope

This setup supports web immediately. The existing Expo authorization-code and PKCE flow also supports the eventual Android and iOS builds through the same Cognito client. Native store setup, package identifiers, universal links, and device testing remain separate launch work.
