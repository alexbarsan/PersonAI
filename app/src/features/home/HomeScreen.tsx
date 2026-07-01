import { useQuery } from "@tanstack/react-query";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";

import { Link } from "expo-router";

import { useApiClient } from "@/api/apiContext";
import { useAuthStore } from "@/auth/authStore";
import { useDreamDraftStore } from "@/state/dreamDraftStore";
import { useTheme } from "@/theme/ThemeProvider";

export function HomeScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const user = useAuthStore((state) => state.user);
  const signInWithMockUser = useAuthStore((state) => state.signInWithMockUser);
  const signOut = useAuthStore((state) => state.signOut);
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

  return (
    <View style={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <View style={styles.header}>
        <Text style={[styles.title, { color: theme.colors.text }]}>{theme.appName}</Text>
        <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>
          {user ? `Hi, ${me.data?.displayName ?? user.displayName ?? "Dreamer"}` : "Private dream journal"}
        </Text>
      </View>

      <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
        {user ? (
          <>
            <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Today&apos;s dream</Text>
            <TextInput
              accessibilityLabel="Dream text"
              multiline
              onChangeText={setDraftText}
              placeholder="I remember..."
              placeholderTextColor={theme.colors.mutedText}
              style={[styles.input, { borderColor: theme.colors.border, color: theme.colors.text }]}
              textAlignVertical="top"
              value={draftText}
            />
            <TextInput
              accessibilityLabel="Mood"
              onChangeText={setMood}
              placeholder="Mood"
              placeholderTextColor={theme.colors.mutedText}
              style={[styles.singleInput, { borderColor: theme.colors.border, color: theme.colors.text }]}
              value={mood}
            />
            <Text testID="auth-state" style={[styles.body, { color: theme.colors.text }]}>
              {savedAt ? "Draft saved" : "Signed in"}
            </Text>
            <Pressable
              accessibilityRole="button"
              onPress={saveDraft}
              style={[styles.button, { backgroundColor: theme.colors.primary }]}
            >
              <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>Save draft</Text>
            </Pressable>
            <Link href="/dreams/capture" asChild>
              <Pressable accessibilityRole="button" style={styles.secondaryButton}>
                <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>Interpret dream</Text>
              </Pressable>
            </Link>
            <Link href="/journal" asChild>
              <Pressable accessibilityRole="button" style={styles.secondaryButton}>
                <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>Journal</Text>
              </Pressable>
            </Link>
            <Link href="/insights" asChild>
              <Pressable accessibilityRole="button" style={styles.secondaryButton}>
                <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>Insights</Text>
              </Pressable>
            </Link>
            <Link href="/profile" asChild>
              <Pressable accessibilityRole="button" style={styles.secondaryButton}>
                <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>Profile</Text>
              </Pressable>
            </Link>
            <Pressable accessibilityRole="button" onPress={signOut} style={styles.secondaryButton}>
              <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>Sign out</Text>
            </Pressable>
          </>
        ) : (
          <>
            <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Sign in</Text>
            <Text testID="auth-state" style={[styles.body, { color: theme.colors.text }]}>
              Signed out
            </Text>
            <Pressable
              accessibilityRole="button"
              onPress={signInWithMockUser}
              style={[styles.button, { backgroundColor: theme.colors.primary }]}
            >
              <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>Use mock account</Text>
            </Pressable>
            <Link href="/onboarding" asChild>
              <Pressable accessibilityRole="button" style={styles.secondaryButton}>
                <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>Onboarding</Text>
              </Pressable>
            </Link>
          </>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    padding: 24,
    justifyContent: "center"
  },
  header: {
    gap: 8,
    marginBottom: 24
  },
  title: {
    fontSize: 34,
    fontWeight: "700"
  },
  subtitle: {
    fontSize: 16,
    lineHeight: 23
  },
  panel: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 16,
    padding: 20
  },
  panelTitle: {
    fontSize: 20,
    fontWeight: "700"
  },
  body: {
    fontSize: 15,
    lineHeight: 22
  },
  input: {
    borderRadius: 8,
    borderWidth: 1,
    fontSize: 16,
    minHeight: 140,
    padding: 12
  },
  singleInput: {
    borderRadius: 8,
    borderWidth: 1,
    fontSize: 16,
    minHeight: 48,
    paddingHorizontal: 12
  },
  button: {
    alignItems: "center",
    borderRadius: 8,
    minHeight: 48,
    justifyContent: "center",
    paddingHorizontal: 16
  },
  buttonText: {
    fontSize: 16,
    fontWeight: "700"
  },
  secondaryButton: {
    alignItems: "center",
    minHeight: 44,
    justifyContent: "center",
    paddingHorizontal: 16
  },
  secondaryButtonText: {
    fontSize: 16,
    fontWeight: "700"
  }
});
