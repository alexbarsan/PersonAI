import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "expo-router";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { useAuthStore } from "@/auth/authStore";
import { AppShell, BrandMark } from "@/components/AppShell";
import { Text } from "@/components/Text";
import { RevenueCatPurchaseError } from "@/features/monetization/revenueCat";
import { useRevenueCatSubscription } from "@/features/monetization/useRevenueCatSubscription";
import { useTheme } from "@/theme/ThemeProvider";

export function PaywallScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const user = useAuthStore((state) => state.user);
  const queryClient = useQueryClient();
  const entitlement = useQuery({ queryKey: ["entitlements", user?.subject], queryFn: () => api.getEntitlements(), enabled: Boolean(user) });
  const revenueCat = useRevenueCatSubscription();
  const tier = entitlement.data?.tier ?? "free";
  const revenueCatPremium = revenueCat.subscription.data?.activeEntitlement === true;
  const actionError = revenueCat.purchase.error ?? revenueCat.presentPaywall.error;
  const actionPending = revenueCat.purchase.isPending || revenueCat.presentPaywall.isPending;

  const refresh = async () => {
    await Promise.all([
      entitlement.refetch(),
      revenueCat.subscription.refetch(),
      queryClient.invalidateQueries({ queryKey: ["entitlements"] })
    ]);
  };

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen}>
        <BrandMark detail="Choose the depth that fits your practice." />
        <View style={[styles.hero, { backgroundColor: theme.colors.primary }]}>
          <Text style={[styles.title, { color: theme.colors.primaryText }]}>Premium</Text>
          <Text style={[styles.subtitle, { color: "#c9d1e2" }]}>
            {tier === "premium" ? "Your Premium tier is active." : "More room to capture, revisit, and connect the patterns that matter."}
          </Text>
        </View>
        <View style={[styles.plan, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
          <Text style={[styles.planTitle, { color: theme.colors.text }]}>Free</Text>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>3 dream interpretations per day.</Text>
          <Text style={[styles.detail, { color: theme.colors.mutedText }]}>Private journal and your evolving dream map.</Text>
        </View>
        <View style={[styles.plan, { backgroundColor: theme.colors.sage, borderColor: theme.colors.sage }]}>
          <Text style={[styles.planTitle, { color: theme.colors.text }]}>Premium</Text>
          <Text style={[styles.body, { color: theme.colors.text }]}>25 dream interpretations per day.</Text>
          <Text style={[styles.detail, { color: theme.colors.mutedText }]}>Cognitive Analysis, dream visuals, data export, and three dream-history questions each day.</Text>
        </View>

        {tier !== "premium" && revenueCat.isConfigured && revenueCat.subscription.isLoading ? <Text style={[styles.availability, { color: theme.colors.mutedText }]}>Loading available plans.</Text> : null}
        {tier !== "premium" && revenueCat.isConfigured && revenueCat.subscription.data?.hasPaywall ? (
          <Pressable accessibilityRole="button" disabled={actionPending} onPress={() => revenueCat.presentPaywall.mutate()} style={[styles.primaryButton, { backgroundColor: theme.colors.primary, opacity: actionPending ? 0.65 : 1 }]}>
            <Text style={[styles.primaryButtonText, { color: theme.colors.primaryText }]}>{actionPending ? "Opening secure checkout" : "Choose Premium"}</Text>
          </Pressable>
        ) : null}
        {tier !== "premium" && revenueCat.isConfigured && !revenueCat.subscription.isLoading && revenueCat.subscription.data?.plans.map((plan) => (
          <View key={plan.identifier} style={[styles.purchasePlan, { borderColor: theme.colors.border, backgroundColor: theme.colors.surface }]}>
            <View style={styles.purchasePlanCopy}>
              <Text style={[styles.planTitle, { color: theme.colors.text }]}>{plan.title}</Text>
              <Text style={[styles.detail, { color: theme.colors.mutedText }]}>{plan.description ?? plan.productIdentifier}</Text>
              <Text style={[styles.price, { color: theme.colors.text }]}>{plan.price}{formatPeriod(plan.period)}</Text>
            </View>
            <Pressable accessibilityRole="button" disabled={actionPending} onPress={() => revenueCat.purchase.mutate(plan.identifier)} style={[styles.purchaseButton, { borderColor: theme.colors.primary, opacity: actionPending ? 0.65 : 1 }]}>
              <Text style={[styles.purchaseButtonText, { color: theme.colors.primary }]}>{actionPending ? "Please wait" : "Select"}</Text>
            </Pressable>
          </View>
        ))}
        {tier !== "premium" && !revenueCat.isConfigured ? <Text style={[styles.availability, { color: theme.colors.mutedText }]}>Premium purchases are not configured for this environment yet.</Text> : null}
        {revenueCatPremium && tier !== "premium" ? (
          <View style={[styles.notice, { borderColor: theme.colors.border }]}>
            <Text style={[styles.body, { color: theme.colors.text }]}>RevenueCat confirms your Premium purchase.</Text>
            <Text style={[styles.detail, { color: theme.colors.mutedText }]}>Dream DNA service access updates through secure server notifications. Refresh after it has synced.</Text>
            <Pressable accessibilityRole="button" onPress={() => void refresh()} style={styles.refreshButton}><Text style={[styles.purchaseButtonText, { color: theme.colors.primary }]}>Refresh access</Text></Pressable>
          </View>
        ) : null}
        {actionError && !isCancelled(actionError) ? <Text style={[styles.error, { color: theme.colors.warning }]}>{purchaseErrorMessage(actionError)}</Text> : null}
        {tier !== "premium" && revenueCat.isConfigured && !revenueCat.subscription.isLoading && revenueCat.subscription.data?.plans.length === 0 && !revenueCat.subscription.data?.hasPaywall ? <Text style={[styles.availability, { color: theme.colors.mutedText }]}>No Premium plans are available right now. Please try again later.</Text> : null}
        <Link href="/" asChild><Pressable accessibilityRole="button" style={styles.back}><Text style={[styles.backText, { color: theme.colors.text }]}>Back to Today</Text></Pressable></Link>
      </ScrollView>
    </AppShell>
  );
}

function formatPeriod(period: string | null) {
  if (period === "P1M") return " / month";
  if (period === "P1Y") return " / year";
  return "";
}

function isCancelled(error: unknown) {
  return error instanceof RevenueCatPurchaseError && error.isCancelled;
}

function purchaseErrorMessage(error: unknown) {
  return error instanceof RevenueCatPurchaseError ? error.message : "We could not complete the purchase. Please try again.";
}

const styles = StyleSheet.create({
  screen: { gap: 14, padding: 20, paddingBottom: 28 },
  hero: { borderRadius: 8, gap: 9, padding: 20 },
  title: { fontSize: 29, fontWeight: "700", lineHeight: 35 },
  subtitle: { fontSize: 15, lineHeight: 22 },
  plan: { borderRadius: 8, borderWidth: 1, gap: 8, padding: 18 },
  planTitle: { fontSize: 18, fontWeight: "800" },
  body: { fontSize: 15, fontWeight: "700", lineHeight: 22 },
  detail: { fontSize: 14, lineHeight: 20 },
  price: { fontSize: 16, fontWeight: "800", lineHeight: 22 },
  availability: { fontSize: 13, lineHeight: 19, textAlign: "center" },
  error: { fontSize: 13, lineHeight: 19, textAlign: "center" },
  primaryButton: { alignItems: "center", borderRadius: 8, justifyContent: "center", minHeight: 48, paddingHorizontal: 16 },
  primaryButtonText: { fontSize: 15, fontWeight: "800" },
  purchasePlan: { alignItems: "center", borderRadius: 8, borderWidth: 1, flexDirection: "row", gap: 12, justifyContent: "space-between", padding: 16 },
  purchasePlanCopy: { flex: 1, gap: 3 },
  purchaseButton: { alignItems: "center", borderRadius: 8, borderWidth: 1, justifyContent: "center", minHeight: 38, minWidth: 70, paddingHorizontal: 10 },
  purchaseButtonText: { fontSize: 13, fontWeight: "800" },
  notice: { borderRadius: 8, borderWidth: 1, gap: 6, padding: 15 },
  refreshButton: { alignSelf: "flex-start", minHeight: 32, justifyContent: "center" },
  back: { alignItems: "center", minHeight: 40, justifyContent: "center" },
  backText: { fontSize: 14, fontWeight: "800" }
});
