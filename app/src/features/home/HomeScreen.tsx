import { useQuery } from "@tanstack/react-query";
import { Link } from "expo-router";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { useAuthStore } from "@/auth/authStore";
import { useCognitoSignIn } from "@/auth/cognitoAuth";
import { AppShell, BrandMark } from "@/components/AppShell";
import { appConfig } from "@/core/config";
import { useDreamDraftStore } from "@/state/dreamDraftStore";
import { useTheme } from "@/theme/ThemeProvider";

export function HomeScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const user = useAuthStore((state) => state.user);
  const signInWithMockUser = useAuthStore((state) => state.signInWithMockUser);
  const signOut = useAuthStore((state) => state.signOut);
  const cognitoSignIn = useCognitoSignIn();
  const draftText = useDreamDraftStore((state) => state.text);
  const mood = useDreamDraftStore((state) => state.mood);
  const savedAt = useDreamDraftStore((state) => state.savedAt);
  const setDraftText = useDreamDraftStore((state) => state.setText);
  const setMood = useDreamDraftStore((state) => state.setMood);
  const saveDraft = useDreamDraftStore((state) => state.saveDraft);
  const me = useQuery({
    queryKey: ["me", user?.subject],
    queryFn: () => api.getMe(),
    enabled: Boolean(user)
  });
  const entitlements = useQuery({
    queryKey: ["entitlements", user?.subject],
    queryFn: () => api.getEntitlements(),
    enabled: Boolean(user)
  });

  return (
    <AppShell showNavigation={Boolean(user)}>
      <ScrollView contentContainerStyle={styles.screen}>
        <View style={styles.topline}>
          <BrandMark
            detail={
              user
                ? `Good to see you, ${me.data?.displayName ?? user.displayName ?? "Dreamer"}.`
                : "A private place for what stays with you."
            }
          />
          {user ? (
            <Text style={{ ...styles.quietStatus, color: theme.colors.mutedText }}>{savedAt ? "Saved" : "Private"}</Text>
          ) : null}
        </View>

        {user ? (
          <>
            <View style={{ ...styles.captureCard, backgroundColor: theme.colors.primary }}>
              <Text style={{ ...styles.eyebrow, color: theme.colors.primaryText }}>Today&apos;s dream</Text>
              <Text style={{ ...styles.captureTitle, color: theme.colors.primaryText }}>
                A few seconds is enough to keep a dream from disappearing.
              </Text>
              <TextInput
                accessibilityLabel="Dream text"
                multiline
                onChangeText={setDraftText}
                placeholder="I remember..."
                placeholderTextColor="#c9d1e2"
                style={{ ...styles.dreamInput, backgroundColor: theme.colors.surface, color: theme.colors.text }}
                textAlignVertical="top"
                value={draftText}
              />
              <View style={styles.captureActions}>
                <TextInput
                  accessibilityLabel="Mood"
                  onChangeText={setMood}
                  placeholder="Mood, if you know it"
                  placeholderTextColor={theme.colors.mutedText}
                  style={{ ...styles.moodInput, backgroundColor: theme.colors.surface, color: theme.colors.text }}
                  value={mood}
                />
                <Pressable
                  accessibilityRole="button"
                  onPress={saveDraft}
                  style={{ ...styles.saveButton, borderColor: "#93a1bf" }}
                >
                  <Text style={{ ...styles.saveButtonText, color: theme.colors.primaryText }}>Save draft</Text>
                </Pressable>
              </View>
              <Text testID="auth-state" style={{ ...styles.hiddenStatus, color: theme.colors.primaryText }}>{savedAt ? "Draft saved" : "Signed in"}</Text>
            </View>

            <Link href="/dreams/capture" asChild>
              <Pressable
                accessibilityRole="button"
                style={{ ...styles.primaryAction, backgroundColor: theme.colors.sage }}
                testID="go-dream-capture"
              >
                <Text style={{ ...styles.primaryActionText, color: theme.colors.text }}>Continue with this dream</Text>
              </Pressable>
            </Link>

            <View style={styles.summaryRow}>
              <View style={{ ...styles.summaryCard, backgroundColor: theme.colors.lavender }}>
                <Text style={{ ...styles.summaryValue, color: theme.colors.text }}>
                  {entitlements.data?.tier === "premium" ? "Premium" : "Free"}
                </Text>
                <Text testID="entitlement-state" style={{ ...styles.summaryLabel, color: theme.colors.mutedText }}>
                  {entitlements.data?.tier === "premium"
                    ? `${entitlements.data.dailyDreamLimit} dreams/day`
                    : `Free: ${entitlements.data?.dailyDreamLimit ?? 3} dreams/day`}
                </Text>
              </View>
              <Link href="/insights" asChild>
                <Pressable
                  accessibilityRole="button"
                  style={{ ...styles.summaryCard, backgroundColor: theme.colors.softInk }}
                  testID="go-insights"
                >
                  <Text style={{ ...styles.summaryValue, color: theme.colors.text }}>Your map</Text>
                  <Text style={{ ...styles.summaryLabel, color: theme.colors.mutedText }}>Notice patterns over time</Text>
                </Pressable>
              </Link>
            </View>

            <View style={styles.linkRow}>
              <Link href="/journal" asChild>
                <Pressable accessibilityRole="button" testID="go-journal"><Text style={{ ...styles.linkText, color: theme.colors.text }}>Open journal</Text></Pressable>
              </Link>
              <Link href="/profile" asChild>
                <Pressable accessibilityRole="button" testID="go-profile"><Text style={{ ...styles.linkText, color: theme.colors.text }}>Profile</Text></Pressable>
              </Link>
              <Link href="/paywall" asChild>
                <Pressable accessibilityRole="button" testID="go-paywall"><Text style={{ ...styles.linkText, color: theme.colors.text }}>Plans</Text></Pressable>
              </Link>
              <Pressable accessibilityRole="button" onPress={signOut}><Text style={{ ...styles.linkText, color: theme.colors.mutedText }}>Sign out</Text></Pressable>
            </View>
          </>
        ) : (
          <View style={styles.welcome}>
            <Text style={[styles.welcomeTitle, { color: theme.colors.text }]}>A few seconds is enough to keep a dream from disappearing.</Text>
            <Text style={[styles.welcomeBody, { color: theme.colors.mutedText }]}>Capture the fragments. Return when you&apos;re ready to see what repeats.</Text>
            <Text testID="auth-state" style={[styles.hiddenStatus, { color: theme.colors.mutedText }]}>Signed out</Text>
            {appConfig.mockApi ? (
              <Pressable
                accessibilityRole="button"
                onPress={signInWithMockUser}
                style={[styles.primaryAction, { backgroundColor: theme.colors.primary }]}
                testID="mock-sign-in"
              >
                <Text style={[styles.primaryActionText, { color: theme.colors.primaryText }]}>Use mock account</Text>
              </Pressable>
            ) : (
              <>
                <Pressable
                  accessibilityRole="button"
                  disabled={cognitoSignIn.isSigningIn}
                  onPress={cognitoSignIn.signIn}
                  style={[styles.primaryAction, { backgroundColor: theme.colors.primary }]}
                  testID="cognito-sign-in"
                >
                  <Text style={[styles.primaryActionText, { color: theme.colors.primaryText }]}>
                    {cognitoSignIn.isSigningIn ? "Signing in..." : "Sign in"}
                  </Text>
                </Pressable>
                {cognitoSignIn.error ? <Text testID="auth-error" style={[styles.error, { color: theme.colors.warning }]}>{cognitoSignIn.error}</Text> : null}
              </>
            )}
            <Link href="/onboarding" asChild>
              <Pressable accessibilityRole="button" testID="go-onboarding"><Text style={[styles.linkText, { color: theme.colors.text }]}>Set up your profile first</Text></Pressable>
            </Link>
          </View>
        )}
      </ScrollView>
    </AppShell>
  );
}

const styles = StyleSheet.create({
  screen: { gap: 20, padding: 20, paddingBottom: 28 },
  topline: { alignItems: "flex-start", flexDirection: "row", justifyContent: "space-between" },
  quietStatus: { fontSize: 12, fontWeight: "700" },
  captureCard: { borderRadius: 8, gap: 16, padding: 20 },
  eyebrow: { fontSize: 12, fontWeight: "800", textTransform: "uppercase" },
  captureTitle: { fontSize: 25, fontWeight: "700", lineHeight: 31 },
  dreamInput: { borderRadius: 6, fontSize: 16, lineHeight: 23, minHeight: 150, padding: 14 },
  captureActions: { alignItems: "center", flexDirection: "row", gap: 10 },
  moodInput: { borderRadius: 6, flex: 1, fontSize: 14, minHeight: 44, paddingHorizontal: 12 },
  saveButton: { alignItems: "center", borderRadius: 6, borderWidth: 1, justifyContent: "center", minHeight: 44, minWidth: 120 },
  saveButtonText: { fontSize: 14, fontWeight: "800" },
  primaryAction: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 52, paddingHorizontal: 18 },
  primaryActionText: { fontSize: 16, fontWeight: "800" },
  summaryRow: { flexDirection: "row", gap: 12 },
  summaryCard: { borderRadius: 8, flex: 1, gap: 5, minHeight: 112, padding: 16 },
  summaryValue: { fontSize: 17, fontWeight: "800" },
  summaryLabel: { fontSize: 13, lineHeight: 18 },
  linkRow: { flexDirection: "row", flexWrap: "wrap", gap: 16, paddingTop: 2 },
  linkText: { fontSize: 14, fontWeight: "700", lineHeight: 22 },
  welcome: { gap: 20, paddingTop: 54 },
  welcomeTitle: { fontSize: 32, fontWeight: "700", lineHeight: 39 },
  welcomeBody: { fontSize: 17, lineHeight: 25, maxWidth: 430 },
  hiddenStatus: { fontSize: 12, opacity: 0.75 },
  error: { fontSize: 14, lineHeight: 20 }
});
