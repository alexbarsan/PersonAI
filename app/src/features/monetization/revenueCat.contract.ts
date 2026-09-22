export const dreamDnaUnlimitedEntitlement = "dreamdna_world_unlimited";

export type RevenueCatPlan = {
  identifier: string;
  productIdentifier: string;
  title: string;
  description: string | null;
  price: string;
  period: string | null;
};

export type RevenueCatSnapshot = {
  activeEntitlement: boolean;
  hasPaywall: boolean;
  plans: RevenueCatPlan[];
};

export type RevenueCatConfiguration = {
  apiKey: string;
  appUserId: string;
};

export class RevenueCatPurchaseError extends Error {
  constructor(message: string, public readonly isCancelled = false) {
    super(message);
    this.name = "RevenueCatPurchaseError";
  }
}

export interface RevenueCatClient {
  sync(configuration: RevenueCatConfiguration): Promise<RevenueCatSnapshot>;
  purchase(configuration: RevenueCatConfiguration, packageIdentifier: string): Promise<RevenueCatSnapshot>;
  presentPaywall(configuration: RevenueCatConfiguration): Promise<RevenueCatSnapshot>;
}
