import { useQuery } from "@tanstack/react-query";
import { Link } from "expo-router";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { Text } from "@/components/Text";

import { useApiClient } from "@/api/apiContext";
import { useAuthStore } from "@/auth/authStore";
import { AppShell, BrandMark } from "@/components/AppShell";
import { useTheme } from "@/theme/ThemeProvider";

export function PaywallScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const user = useAuthStore((state) => state.user);
  const entitlement = useQuery({ queryKey: ["entitlements", user?.subject], queryFn: () => api.getEntitlements(), enabled: Boolean(user) });
  const tier = entitlement.data?.tier ?? "free";

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen}>
        <BrandMark detail="Choose the depth that fits your practice." />
        <View style={[styles.hero, { backgroundColor: theme.colors.primary }]}>
          <Text style={[styles.title, { color: theme.colors.primaryText }]}>Premium</Text>
          <Text style={[styles.subtitle, { color: "#c9d1e2" }]}>{tier === "premium" ? "Your Premium tier is active." : "More room to capture, revisit, and connect the patterns that matter."}</Text>
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
        {tier !== "premium" ? <Text style={[styles.availability, { color: theme.colors.mutedText }]}>Subscriptions will be available here when purchases are connected.</Text> : null}
        <Link href="/" asChild><Pressable accessibilityRole="button" style={styles.back}><Text style={[styles.backText, { color: theme.colors.text }]}>Back to Today</Text></Pressable></Link>
      </ScrollView>
    </AppShell>
  );
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
  availability: { fontSize: 13, lineHeight: 19, textAlign: "center" },
  back: { alignItems: "center", minHeight: 40, justifyContent: "center" },
  backText: { fontSize: 14, fontWeight: "800" }
});
