# RevenueCat Web Billing

Dream DNA's web app uses `@revenuecat/purchases-js` and the authenticated Cognito subject as the RevenueCat App User ID. The client prefixes it as `dreamdna:<cognito-subject>` so the same value can be used later by Android and iOS.

The public Web SDK key is required at web-build time. It is safe to embed a RevenueCat **public Web SDK key** in the web bundle, but do not put a RevenueCat secret API key, Stripe secret key, webhook secret, or product-management API key in Expo configuration or GitHub client variables.

## 1. Configure RevenueCat

1. In RevenueCat, connect a web billing provider. For RevenueCat Billing, connect the Stripe account first, create a **Web** billing configuration, then select the connected Stripe account.
2. Create the entitlement with identifier `dreamdna_world_unlimited`.
3. Create these web products in the billing provider and import/map them in RevenueCat:

| Product ID | Type | RevenueCat package |
| --- | --- | --- |
| `lifetime` | Non-consumable lifetime purchase | `$rc_lifetime` |
| `yearly` | Auto-renewing yearly subscription | `$rc_annual` |
| `monthly` | Auto-renewing monthly subscription | `$rc_monthly` |

4. Attach all three products to `dreamdna_world_unlimited`.
5. Create an offering, for example `default`, add the three packages, and set it as the current offering.
6. Create and publish a RevenueCat Web Paywall for that offering. Dream DNA calls `presentPaywall()` when a dashboard paywall exists. Without one, it renders the current offering's packages and starts checkout with `purchase()`.
7. Configure tax, receipt email, refund, cancellation, and customer-support settings in the selected billing provider.

## 2. Inject The Public Web Key

Add this variable to the GitHub `dev` and `prod` environments before building web:

```text
EXPO_PUBLIC_REVENUECAT_WEB_API_KEY=<RevenueCat Web public SDK key>
```

For a local development build, set the same environment variable in the PowerShell session before `npm run build:web`. Do not add the key to `app.json`, `.env` committed to Git, Terraform variables, or server secrets.

## 3. What The App Does

The implementation is isolated in these files:

- `app/src/features/monetization/revenueCat.web.ts`: the real browser-only SDK client.
- `app/src/features/monetization/revenueCat.ts`: native/test safe fallback.
- `app/src/features/monetization/useRevenueCatSubscription.ts`: React Query state and refresh.
- `app/src/features/paywall/PaywallScreen.tsx`: dashboard offering, checkout, errors, and entitlement feedback.

It configures the SDK once and changes its identified customer if a different Cognito user signs in:

```ts
const purchases = Purchases.configure({
  apiKey: publicWebSdkKey,
  appUserId: `dreamdna:${cognitoSubject}`
});

const customerInfo = await purchases.getCustomerInfo();
const hasUnlimited = "dreamdna_world_unlimited" in customerInfo.entitlements.active;

const offerings = await purchases.getOfferings();
const packages = offerings.current?.availablePackages ?? [];
const result = await purchases.purchase({ rcPackage: packages[0] });
```

Purchases and the dashboard paywall both refresh `CustomerInfo`. Cancellation is handled as a non-error; other errors show a generic recovery message without exposing provider internals. The app never treats a browser-only response as proof for a protected API request.

## 4. Secure Server Entitlements Before Launch

Dream DNA's API currently protects Premium features from its own PostgreSQL entitlement service, including Friends and Family grants. The web SDK only drives checkout and client display. Before enabling paid checkout in production, add a signed RevenueCat server-notification webhook that validates a secret header, maps the RevenueCat App User ID back to the Cognito subject, persists subscription state separately from Friends and Family grants, and drives `/v1/entitlements`.

Do not grant Premium from a request sent by the browser and do not use a public SDK key as webhook authentication. Test the webhook with RevenueCat sandbox events for purchase, renewal, expiration, cancellation, billing issue, refund, and transfer before going live.

## 5. Customer Information And Subscription Management

Use `getCustomerInfo()` when the app returns to the foreground, after a completed purchase, and after an explicit refresh. Use the entitlement identifier, not product IDs, to decide whether RevenueCat reports paid access. Keep the API as the authorization source for sensitive or billable Dream DNA actions.

RevenueCat Customer Center is a Pro/Enterprise feature with documented iOS and Android integrations. There is no equivalent `purchases-js` Customer Center UI to mount in the web app. For web, subscription management remains in the billing provider's customer portal or RevenueCat-managed web flow. Revisit Customer Center when the native clients are implemented.

## 6. Test Checklist

1. Use a separate RevenueCat sandbox/test project or the configured test Web key.
2. Confirm `getOfferings()` returns `lifetime`, `yearly`, and `monthly` through the current offering.
3. Open the remote paywall and complete one test transaction for each package.
4. Confirm `dreamdna_world_unlimited` appears in `CustomerInfo.entitlements.active` for the identified `dreamdna:<subject>` customer.
5. Test cancellation, checkout failure, browser refresh during checkout, and sign-out/sign-in as a different Cognito account.
6. After the webhook is built, verify the API's `/v1/entitlements` changes only from verified server notifications.
