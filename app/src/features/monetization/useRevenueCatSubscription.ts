import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useAuthStore } from "@/auth/authStore";
import { appConfig } from "@/core/config";
import { revenueCat } from "@/features/monetization/revenueCat";

export function useRevenueCatSubscription() {
  const user = useAuthStore((state) => state.user);
  const queryClient = useQueryClient();
  const apiKey = appConfig.revenueCatWebApiKey.trim();
  const configuration = user && apiKey ? { apiKey, appUserId: `dreamdna:${user.subject}` } : null;
  const queryKey = ["revenuecat-subscription", user?.subject];

  const subscription = useQuery({
    queryKey,
    queryFn: () => revenueCat.sync(configuration!),
    enabled: Boolean(configuration),
    staleTime: 60_000
  });

  const invalidateEntitlements = async () => {
    await queryClient.invalidateQueries({ queryKey: ["entitlements"] });
    await queryClient.invalidateQueries({ queryKey });
  };

  const purchase = useMutation({
    mutationFn: (packageIdentifier: string) => revenueCat.purchase(configuration!, packageIdentifier),
    onSuccess: async () => invalidateEntitlements()
  });

  const presentPaywall = useMutation({
    mutationFn: () => revenueCat.presentPaywall(configuration!),
    onSuccess: async () => invalidateEntitlements()
  });

  return {
    isConfigured: Boolean(configuration),
    subscription,
    purchase,
    presentPaywall
  };
}
