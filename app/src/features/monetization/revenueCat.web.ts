import { ErrorCode, Purchases, PurchasesError, type Offering, type Package } from "@revenuecat/purchases-js";

import {
  dreamDnaUnlimitedEntitlement,
  type RevenueCatConfiguration,
  type RevenueCatPlan,
  RevenueCatPurchaseError,
  type RevenueCatSnapshot,
  type RevenueCatClient
} from "@/features/monetization/revenueCat.contract";

let purchases: Purchases | null = null;
let packagesByIdentifier = new Map<string, Package>();

export const revenueCat: RevenueCatClient = {
  async sync(configuration) {
    const client = await getClient(configuration);
    return loadSnapshot(client);
  },

  async purchase(configuration, packageIdentifier) {
    const client = await getClient(configuration);
    await loadSnapshot(client);
    const rcPackage = packagesByIdentifier.get(packageIdentifier);

    if (!rcPackage) {
      throw new RevenueCatPurchaseError("That plan is no longer available. Refresh the page and try again.");
    }

    try {
      await client.purchase({ rcPackage });
      return loadSnapshot(client);
    } catch (error) {
      throw toPurchaseError(error);
    }
  },

  async presentPaywall(configuration) {
    const client = await getClient(configuration);
    const offerings = await client.getOfferings();
    const offering = offerings.current;

    if (!offering?.hasPaywall) {
      throw new RevenueCatPurchaseError("The Premium paywall is not configured yet.");
    }

    try {
      await client.presentPaywall({ offering });
      return loadSnapshot(client);
    } catch (error) {
      throw toPurchaseError(error);
    }
  }
};

async function getClient(configuration: RevenueCatConfiguration) {
  if (!purchases) {
    purchases = Purchases.isConfigured()
      ? Purchases.getSharedInstance()
      : Purchases.configure({ apiKey: configuration.apiKey, appUserId: configuration.appUserId });
  }

  if (purchases.getAppUserId() !== configuration.appUserId) {
    await purchases.changeUser(configuration.appUserId);
  }

  return purchases;
}

async function loadSnapshot(client: Purchases): Promise<RevenueCatSnapshot> {
  const [customerInfo, offerings] = await Promise.all([client.getCustomerInfo(), client.getOfferings()]);
  const offering = offerings.current;
  const plans = offering ? indexPlans(offering) : [];

  return {
    activeEntitlement: dreamDnaUnlimitedEntitlement in customerInfo.entitlements.active,
    hasPaywall: offering?.hasPaywall === true,
    plans
  };
}

function indexPlans(offering: Offering): RevenueCatPlan[] {
  packagesByIdentifier = new Map(offering.availablePackages.map((rcPackage) => [rcPackage.identifier, rcPackage]));
  return offering.availablePackages.map((rcPackage) => ({
    identifier: rcPackage.identifier,
    productIdentifier: rcPackage.webBillingProduct.identifier,
    title: rcPackage.webBillingProduct.title,
    description: rcPackage.webBillingProduct.description,
    price: rcPackage.webBillingProduct.price.formattedPrice,
    period: rcPackage.webBillingProduct.normalPeriodDuration
  }));
}

function toPurchaseError(error: unknown) {
  if (error instanceof PurchasesError && error.errorCode === ErrorCode.UserCancelledError) {
    return new RevenueCatPurchaseError("Purchase cancelled.", true);
  }

  return new RevenueCatPurchaseError("We could not complete the purchase. Please try again or contact support.");
}
