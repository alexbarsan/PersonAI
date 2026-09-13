import { useQuery } from "@tanstack/react-query";
import { Link, router } from "expo-router";
import { useEffect } from "react";
import {
  Image,
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  useWindowDimensions,
  View,
} from "react-native";
import ArrowRight from "lucide-react-native/icons/arrow-right";
import BookOpen from "lucide-react-native/icons/book-open";
import CloudSun from "lucide-react-native/icons/cloud-sun";
import Frown from "lucide-react-native/icons/face-slightly-frowning";
import Heart from "lucide-react-native/icons/heart";
import Save from "lucide-react-native/icons/save";
import Smile from "lucide-react-native/icons/face-slightly-smiling";
import Wind from "lucide-react-native/icons/wind";
import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/errors";
import { useAuthStore } from "@/auth/authStore";
import { useCognitoSignIn } from "@/auth/cognitoAuth";
import { AppShell, BrandMark } from "@/components/AppShell";
import { gardenSource } from "@/components/OwlMark";
import { Text } from "@/components/Text";
import { appConfig } from "@/core/config";
import { useDreamDraftStore } from "@/state/dreamDraftStore";
import { VoiceCapturePanel } from "@/features/dreams/VoiceCapturePanel";
import { LandingScreen } from "@/features/home/LandingScreen";
import { useTheme } from "@/theme/ThemeProvider";

const moods = [
  { label: "Calm", icon: Wind, color: "#e0f0e7" },
  { label: "Curious", icon: CloudSun, color: "#fae8df" },
  { label: "Joyful", icon: Smile, color: "#f8efcf" },
  { label: "Anxious", icon: Frown, color: "#eeebf8" },
  { label: "Unsettled", icon: Heart, color: "#e7edf8" },
];

export function HomeScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const user = useAuthStore((state) => state.user);
  const signIn = useAuthStore((state) => state.signInWithMockUser);
  const signOut = useAuthStore((state) => state.signOut);
  const cognito = useCognitoSignIn();
  const draft = useDreamDraftStore();
  const wide = useWindowDimensions().width >= 1200;
  const me = useQuery({
    queryKey: ["me", user?.subject],
    queryFn: () => api.getMe(),
    enabled: Boolean(user),
  });
  const entitlements = useQuery({
    queryKey: ["entitlements", user?.subject],
    queryFn: () => api.getEntitlements(),
    enabled: Boolean(user),
  });
  const profile = useQuery({
    queryKey: ["profile", user?.subject],
    queryFn: () => api.getProfile(),
    enabled: Boolean(user),
  });
  const journal = useQuery({
    queryKey: ["journal", user?.subject, { query: "", mood: "", tag: "" }],
    queryFn: () => api.listDreams(),
    enabled: Boolean(user),
  });
  useEffect(() => {
    if (profile.error instanceof ApiError && profile.error.status === 404)
      router.replace("/onboarding");
  }, [profile.error]);

  if (!user)
    return (
      <LandingScreen
        onStart={appConfig.mockApi ? signIn : cognito.signIn}
        mock={appConfig.mockApi}
        pending={cognito.isSigningIn}
        error={cognito.error}
      />
    );

  return (
    <AppShell>
      <ScrollView
        contentContainerStyle={s.screen}
        keyboardShouldPersistTaps="handled"
      >
        <BrandMark
          detail={`Good to see you, ${me.data?.displayName ?? user.displayName ?? "Dreamer"}.`}
        />
        <View style={s.greeting}>
          <Image
            source={gardenSource}
            resizeMode="cover"
            style={s.greetingImage}
          />
          <Text style={s.greetingKicker}>A moment for yourself</Text>
          <Text accessibilityRole="header" style={s.greetingTitle}>
            What followed you into today?
          </Text>
          <Text style={s.greetingBody}>
            Even the smallest fragment is worth keeping.
          </Text>
        </View>
        {profile.isLoading ? (
          <Text style={s.body}>Preparing your private journal</Text>
        ) : (
          <View style={[s.workspace, wide && s.workspaceWide]}>
            <View style={s.captureColumn}>
              <View style={s.sectionHeading}>
                <Text style={s.sectionTitle}>Today&apos;s dream</Text>
                <Text
                  testID="auth-state"
                  accessibilityLiveRegion="polite"
                  style={s.small}
                >
                  {draft.savedAt ? "Draft saved" : "Signed in"}
                </Text>
              </View>
              <View
                style={[
                  s.composer,
                  {
                    borderColor: theme.colors.border,
                    backgroundColor: theme.colors.surface,
                  },
                ]}
              >
                <TextInput
                  accessibilityLabel="Dream text"
                  multiline
                  value={draft.text}
                  onChangeText={draft.setText}
                  placeholder="I remember..."
                  placeholderTextColor={theme.colors.mutedText}
                  textAlignVertical="top"
                  style={[s.dreamInput, { color: theme.colors.text }]}
                />
                <View style={s.composerFooter}>
                  <Text style={s.small}>A few words can bring it back.</Text>
                  <Pressable
                    accessibilityRole="button"
                    onPress={draft.saveDraft}
                    style={s.save}
                  >
                    <Save size={16} color={theme.colors.primary} />
                    <Text style={s.saveText}>Save draft</Text>
                  </Pressable>
                </View>
              </View>
              <Text style={s.label}>How did it leave you feeling?</Text>
              <View style={s.moods}>
                {moods.map(({ label, icon: Icon, color }) => {
                  const selected =
                    draft.mood.toLowerCase() === label.toLowerCase();
                  return (
                    <Pressable
                      key={label}
                      accessibilityRole="radio"
                      accessibilityLabel={label}
                      accessibilityState={{ checked: selected }}
                      onPress={() =>
                        draft.setMood(selected ? "" : label.toLowerCase())
                      }
                      style={s.moodChoice}
                    >
                      <View
                        style={[
                          s.moodIcon,
                          {
                            backgroundColor: color,
                            borderColor: selected
                              ? theme.colors.primary
                              : "transparent",
                          },
                        ]}
                      >
                        <Icon
                          size={24}
                          strokeWidth={1.6}
                          color={theme.colors.primary}
                        />
                      </View>
                      <Text
                        style={[s.moodLabel, selected && { fontWeight: "700" }]}
                      >
                        {label}
                      </Text>
                    </Pressable>
                  );
                })}
              </View>
              <TextInput
                accessibilityLabel="Mood"
                onChangeText={draft.setMood}
                placeholder="Or name the feeling in your own words"
                placeholderTextColor={theme.colors.mutedText}
                value={draft.mood}
                style={[
                  s.moodInput,
                  {
                    borderColor: theme.colors.border,
                    color: theme.colors.text,
                  },
                ]}
              />
              <Link href="/dreams/capture" asChild>
                <Pressable
                  accessibilityRole="button"
                  testID="go-dream-capture"
                  style={{
                    ...s.primaryAction,
                    backgroundColor: theme.colors.primary,
                  }}
                >
                  <Text style={s.primaryActionText}>
                    Continue with this dream
                  </Text>
                  <ArrowRight size={18} color="white" />
                </Pressable>
              </Link>
              <View
                style={{ backgroundColor: theme.colors.primary, padding: 20 }}
              >
                <VoiceCapturePanel
                  prominent
                  onTranscript={(transcript) => {
                    draft.setText(transcript);
                    draft.saveDraft();
                  }}
                />
              </View>
            </View>
            <View style={[s.historyColumn, wide && s.historyWide]}>
              <View style={s.sectionHeading}>
                <Text style={s.sectionTitle}>Recent memories</Text>
                <Link href="/journal" asChild>
                  <Pressable
                    accessibilityRole="button"
                    testID="go-journal"
                    style={s.textAction}
                  >
                    <Text style={s.saveText}>Open journal</Text>
                    <ArrowRight size={16} color={theme.colors.primary} />
                  </Pressable>
                </Link>
              </View>
              {journal.isLoading ? (
                <Text style={s.body}>Loading your journal...</Text>
              ) : null}
              {journal.isError ? (
                <Text style={[s.body, { color: theme.colors.warning }]}>
                  Your journal could not be loaded.
                </Text>
              ) : null}
              {journal.data?.items.length === 0 ? (
                <View style={s.empty}>
                  <BookOpen size={28} color={theme.colors.primary} />
                  <Text style={s.body}>Your next memory starts here.</Text>
                </View>
              ) : null}
              {journal.data?.items.slice(0, 3).map((item, index) => (
                <Link key={item.id} href={`/journal/${item.id}`} asChild>
                  <Pressable
                    accessibilityRole="button"
                    style={{ ...s.memory, borderColor: theme.colors.border }}
                  >
                    <View
                      style={[
                        s.memoryIcon,
                        {
                          backgroundColor: [
                            theme.colors.sage,
                            theme.colors.lavender,
                            theme.colors.softInk,
                          ][index],
                        },
                      ]}
                    >
                      <BookOpen size={20} color={theme.colors.primary} />
                    </View>
                    <View style={s.memoryText}>
                      <Text style={s.small}>
                        {new Date(item.createdAt).toLocaleDateString("en", {
                          month: "short",
                          day: "numeric",
                        })}
                        {item.mood ? ` / ${item.mood}` : ""}
                      </Text>
                      <Text numberOfLines={3} style={s.memoryTitle}>
                        {item.summary ?? "A remembered dream"}
                      </Text>
                    </View>
                    <ArrowRight size={16} color={theme.colors.mutedText} />
                  </Pressable>
                </Link>
              ))}
              <Link href="/insights" asChild>
                <Pressable
                  accessibilityRole="button"
                  testID="go-insights"
                  style={{
                    ...s.mapLink,
                    backgroundColor: theme.colors.lavender,
                  }}
                >
                  <Text style={s.sectionTitle}>Your map is taking shape</Text>
                  <Text style={s.body}>
                    The people, places, and feelings that connect your nights.
                  </Text>
                  <View style={s.textAction}>
                    <Text style={s.saveText}>Explore your dream map</Text>
                    <ArrowRight size={17} color={theme.colors.primary} />
                  </View>
                </Pressable>
              </Link>
              <View style={[s.planRow, { borderColor: theme.colors.border }]}>
                <Text style={s.label}>
                  {entitlements.data?.tier === "premium"
                    ? "Dream DNA Premium"
                    : "Your free journal"}
                </Text>
                <Text testID="entitlement-state" style={s.small}>
                  {entitlements.data?.quotaExempt
                    ? "No daily limit"
                    : entitlements.data?.tier === "premium"
                      ? `${entitlements.data.dailyDreamLimit} dreams/day`
                      : `Free: ${entitlements.data?.dailyDreamLimit ?? 3} dreams/day`}
                </Text>
                <Link href="/paywall" asChild>
                  <Pressable
                    accessibilityRole="button"
                    testID="go-paywall"
                    style={s.textAction}
                  >
                    <Text style={s.saveText}>Plans</Text>
                    <ArrowRight size={16} color={theme.colors.primary} />
                  </Pressable>
                </Link>
              </View>
            </View>
          </View>
        )}
        <View style={[s.footer, { borderColor: theme.colors.border }]}>
          <Link href="/profile" asChild>
            <Pressable accessibilityRole="button" testID="go-profile">
              <Text style={s.small}>Profile</Text>
            </Pressable>
          </Link>
          <Link href="/onboarding" asChild>
            <Pressable accessibilityRole="button" testID="go-onboarding">
              <Text style={s.small}>Set up profile</Text>
            </Pressable>
          </Link>
          <Pressable accessibilityRole="button" onPress={signOut}>
            <Text style={s.small}>Sign out</Text>
          </Pressable>
        </View>
      </ScrollView>
    </AppShell>
  );
}

const s = StyleSheet.create({
  screen: { gap: 24, padding: 20, paddingBottom: 32 },
  greeting: {
    minHeight: 180,
    padding: 24,
    justifyContent: "center",
    gap: 8,
    backgroundColor: "#dff2ec",
    overflow: "hidden",
  },
  greetingImage: {
    position: "absolute",
    top: 0,
    left: 0,
    width: "100%",
    height: "100%",
    opacity: 0.35,
  },
  greetingKicker: { color: "#245c49", fontSize: 12, fontWeight: "700" },
  greetingTitle: {
    fontSize: 29,
    lineHeight: 36,
    fontWeight: "700",
    color: "#203e36",
    maxWidth: 460,
  },
  greetingBody: {
    fontSize: 14,
    lineHeight: 21,
    color: "#345d4e",
    maxWidth: 310,
  },
  workspace: { gap: 30 },
  workspaceWide: { flexDirection: "row", alignItems: "flex-start" },
  captureColumn: { flex: 1.35, minWidth: 0, gap: 16 },
  historyColumn: { flex: 1, minWidth: 0, gap: 14 },
  historyWide: {
    paddingLeft: 28,
    borderLeftWidth: 1,
    borderLeftColor: "#d9e5df",
  },
  sectionHeading: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    flexWrap: "wrap",
    gap: 10,
  },
  sectionTitle: {
    fontSize: 20,
    lineHeight: 27,
    color: "#203e36",
    fontWeight: "700",
  },
  small: { fontSize: 12, lineHeight: 18, color: "#596e67" },
  body: { fontSize: 14, lineHeight: 22, color: "#596e67" },
  label: { fontSize: 14, fontWeight: "700", color: "#203e36" },
  composer: { borderWidth: 1, borderRadius: 8, overflow: "hidden" },
  dreamInput: {
    minHeight: 150,
    fontSize: 17,
    fontFamily: "Nunito_400Regular",
    lineHeight: 26,
    padding: 18,
  },
  composerFooter: {
    flexDirection: "row",
    flexWrap: "wrap",
    justifyContent: "space-between",
    alignItems: "center",
    gap: 8,
    paddingHorizontal: 16,
    paddingBottom: 8,
  },
  save: { minHeight: 44, flexDirection: "row", alignItems: "center", gap: 7 },
  saveText: { color: "#245c49", fontWeight: "700", fontSize: 13 },
  moods: { flexDirection: "row", gap: 6, justifyContent: "space-between" },
  moodChoice: { flex: 1, minWidth: 0, alignItems: "center", gap: 7 },
  moodIcon: {
    width: 48,
    height: 48,
    borderRadius: 24,
    borderWidth: 2,
    alignItems: "center",
    justifyContent: "center",
  },
  moodLabel: { color: "#596e67", fontSize: 11 },
  moodInput: {
    borderBottomWidth: 1,
    paddingVertical: 10,
    minHeight: 44,
    fontSize: 13,
    fontFamily: "Nunito_400Regular",
  },
  primaryAction: {
    flexDirection: "row",
    gap: 12,
    alignItems: "center",
    justifyContent: "center",
    minHeight: 50,
    borderRadius: 8,
    padding: 12,
  },
  primaryActionText: {
    color: "white",
    fontSize: 15,
    fontWeight: "700",
    flexShrink: 1,
  },
  textAction: {
    flexDirection: "row",
    alignItems: "center",
    gap: 7,
    minHeight: 40,
  },
  memory: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    borderBottomWidth: 1,
    paddingVertical: 14,
  },
  memoryIcon: {
    width: 40,
    height: 48,
    borderRadius: 6,
    alignItems: "center",
    justifyContent: "center",
  },
  memoryText: { flex: 1, gap: 5 },
  memoryTitle: {
    fontSize: 15,
    lineHeight: 22,
    color: "#203e36",
    fontWeight: "700",
  },
  empty: { gap: 12, paddingVertical: 20 },
  mapLink: { padding: 20, gap: 10, borderRadius: 8, marginTop: 8 },
  planRow: { borderTopWidth: 1, paddingTop: 20, gap: 6, marginTop: 8 },
  footer: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 24,
    borderTopWidth: 1,
    paddingTop: 20,
  },
});
