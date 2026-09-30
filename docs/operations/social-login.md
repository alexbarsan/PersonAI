# Google And Apple Login

Dream DNA owns the sign-in/register UI. Email/password, password reset, and social buttons use the existing Cognito User Pool. Amplify Auth runs social login as authorization code + PKCE with OAuth state; the app receives Cognito tokens, never Google or Apple tokens. A provider click selects its Cognito IdP directly rather than sending users to Cognito Managed Login to choose a provider.

The app reads `COGNITO_DOMAIN`, `COGNITO_CLIENT_ID`, `COGNITO_USER_POOL_ID`, `COGNITO_GOOGLE_ENABLED`, and `COGNITO_APPLE_ENABLED` from the selected GitHub Actions environment. Do not hardcode an AWS Cognito hostname in app code. Production should use `https://auth.dreamdna.world` once that custom domain is active; dev can keep using its Cognito prefix domain.

The Dream DNA web callback is `/auth/callback`; the future native scheme is `dreamlens://auth/callback`. Amplify validates OAuth state and exchanges the code using PKCE. The callback waits for Cognito's session before returning to the app. The authenticated home flow calls the profile API, which provisions one profile keyed by Cognito `sub`.

## Callback URLs

Register these Dream DNA callback URLs on each Cognito app client:

| Environment | App callback |
| --- | --- |
| Dev web | `https://dev.dreamdna.world/auth/callback` |
| Production web | `https://dreamdna.world/auth/callback` and `https://www.dreamdna.world/auth/callback` |
| Local web | `http://localhost:8081/auth/callback` |
| Native scheme | `dreamlens://auth/callback` |

Google and Apple separately allow Cognito's provider return URL:

| Environment | Cognito IdP callback |
| --- | --- |
| Dev | `https://dreamlens-dev-379959319368.auth.us-east-1.amazoncognito.com/oauth2/idpresponse` |
| Production during migration | `https://dreamlens-prod-379959319368.auth.us-east-1.amazoncognito.com/oauth2/idpresponse` |
| Production after custom domain activation | `https://auth.dreamdna.world/oauth2/idpresponse` |

Keep Cognito's generated prefix domain available during transition. Set up dev first, then production with separate provider credentials.

## Google

1. Create a Google Cloud project for the environment.
2. Configure the OAuth consent screen and public privacy/terms URLs before production review.
3. Create a **Web application** OAuth client.
4. Add the Cognito IdP callback above as an authorized redirect URI.
5. Configure the client id and secret on the Cognito Google identity provider. Never put the secret in frontend config.

Because production Google was configured directly in Cognito, set `use_existing_google_identity_provider = true` in the untracked production `terraform.tfvars` before the next Terraform apply. Otherwise a future app-client update can remove Google from the supported provider list. Keep its OAuth secret out of Terraform in this case.

## Apple

1. In Apple Developer, create an environment-specific Services ID (for example `world.dreamdna.login` in prod and `world.dreamdna.dev.login` in dev).
2. Enable Sign in with Apple and set the Cognito IdP callback as its return URL.
3. Associate the Services ID with the eventual iOS App ID.
4. Create a Sign in with Apple key and keep its `.p8` private key out of source control.
5. Configure Cognito with the Services ID, Team ID, Key ID, and private key through protected Terraform inputs.

Apple returns the user's name only during first authorization. Dream DNA maps it to Cognito's standard `name` attribute when available; the required Dream DNA username is still collected in profile setup.

## Production Custom Domain

The Cognito Terraform module has an opt-in custom domain and retains the existing prefix domain. `auth.dreamdna.world` is selected in the production example, but Terraform will not create it until `cognito_custom_domain_certificate_arn` is populated.

1. Request a dedicated ACM public certificate for `auth.dreamdna.world` in `us-east-1`, in the production AWS account. Do not replace or reuse the web/API certificate.
2. Complete ACM DNS validation by adding its validation CNAME to the shared DreamDNA Route 53 zone.
3. Wait until ACM reports `ISSUED`, then put its ARN in the untracked production `terraform.tfvars` as `cognito_custom_domain_certificate_arn`.
4. Review a production Terraform plan and apply only after confirming it adds the Cognito custom domain without replacing the user pool or app client.
5. After Cognito provisions the domain, create a Route 53 CNAME for `auth.dreamdna.world` to the `custom_domain_cloudfront_target` Terraform output. Use CNAME, not an A/AAAA alias.
6. Wait until Cognito reports the domain active. Only then change the prod GitHub Actions `COGNITO_DOMAIN` variable to `https://auth.dreamdna.world`.
7. Add `https://auth.dreamdna.world/oauth2/idpresponse` to Google's authorized redirect URIs. Keep the Dream DNA `/auth/callback` URLs on the Cognito app client.

Dev exposes the same opt-in custom domain inputs, but the dev certificate and DNS are operator-managed. Do not change the deployed `COGNITO_DOMAIN` until the custom domain is active and tested.

## Account And Profile Identity

Cognito `sub` is the canonical external identity. The API's unique `UserSubject` constraint protects profile creation; duplicate-email profiles are allowed because email is mutable and does not prove two identities should be linked. Existing password and social users with different `sub` values are not silently merged, even when email claims match. Account linking needs a separate verified linking flow. The first authenticated profile read creates a default profile; repeated or concurrent reads resolve to that same profile.

The app uses `sessionStorage` on web and platform secure storage on native. Logout clears the local app session and signs out the Cognito app session without forcing a global Google or Apple sign-out.

## Verify

1. Apply the Cognito app callback URL change in dev.
2. Check that each enabled provider appears on Dream DNA's sign-in page alongside email/password.
3. Test sign-in, sign-up/verification, forgot password, and one enabled social provider in a private browser session.
4. Confirm callback returns to Dream DNA and profile save works.
5. Repeat sign-in and callback refresh; verify only one profile exists for the Cognito `sub`.
6. Confirm an identity with an existing email but a different `sub` is not silently linked.

The current code prepares web OAuth and its future native callback configuration. Native app-store setup and device testing remain later work. Amplify's native adapters require an EAS development/production build with native modules; Expo Go is not a supported test runtime for this auth integration.
