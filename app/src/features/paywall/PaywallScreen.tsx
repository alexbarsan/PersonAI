import { useQuery } from "@tanstack/react-query";
import { Pressable, StyleSheet, Text, View } from "react-native";

import { Link } from "expo-router";

import { useApiClient } from "@/api/apiContext";
import { useAuthStore } from "@/auth/authStore";
import { useTheme } from "@/theme/ThemeProvider";

export function PaywallScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const user = useAuthStore((state) => state.user);
  const entitlement = useQuery({
    queryKey: ["entitlements", user?.subject],
    queryFn: () => api.getEntitlements(),
    enabled: Boolean(user)
  });
  const tier = entitlement.data?.tier ?? "free";

  return (
    <View style={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>Premium</Text>
      <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>
        {tier === "premium" ? "Your Premium tier is active." : "Unlock higher dream limits and deep analysis readiness."}
      </Text>

      <View style={[styles.plan, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
        <Text style={[styles.planTitle, { color: theme.colors.text }]}>Free</Text>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>3 dream interpretations per day.</Text>
      </View>

      <View style={[styles.plan, { backgroundColor: theme.colors.surface, borderColor: theme.colors.primary }]}>
        <Text style={[styles.planTitle, { color: theme.colors.text }]}>Premium</Text>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>25 dream interpretations per day.</Text>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>Deep analysis entitlement ready for provider integration.</Text>
      </View>

      <Pressable accessibilityRole="button" disabled style={[styles.button, { backgroundColor: theme.colors.primary }]}>
        <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>Purchases not connected yet</Text>
      </Pressable>

      <Link href="/" asChild>
        <Pressable accessibilityRole="button" style={styles.secondaryButton}>
          <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>Back</Text>
        </Pressable>
      </Link>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    gap: 16,
    justifyContent: "center",
    padding: 24
  },
  title: {
    fontSize: 28,
    fontWeight: "700"
  },
  subtitle: {
    fontSize: 16,
    lineHeight: 23
  },
  plan: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 8,
    padding: 16
  },
  planTitle: {
    fontSize: 18,
    fontWeight: "700"
  },
  body: {
    fontSize: 15,
    lineHeight: 22
  },
  button: {
    alignItems: "center",
    borderRadius: 8,
    justifyContent: "center",
    minHeight: 48,
    paddingHorizontal: 16
  },
  buttonText: {
    fontSize: 16,
    fontWeight: "700"
  },
  secondaryButton: {
    alignItems: "center",
    justifyContent: "center",
    minHeight: 44,
    paddingHorizontal: 16
  },
  secondaryButtonText: {
    fontSize: 16,
    fontWeight: "700"
  }
});
