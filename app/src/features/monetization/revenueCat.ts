import { RevenueCatPurchaseError, type RevenueCatClient } from "@/features/monetization/revenueCat.contract";

export * from "@/features/monetization/revenueCat.contract";

const unavailableClient: RevenueCatClient = {
  async sync() {
    return { activeEntitlement: false, hasPaywall: false, plans: [] };
  },
  async purchase() {
    throw new RevenueCatPurchaseError("Purchases are available on the Dream DNA web app.");
  },
  async presentPaywall() {
    throw new RevenueCatPurchaseError("Purchases are available on the Dream DNA web app.");
  }
};

export const revenueCat: RevenueCatClient = unavailableClient;
