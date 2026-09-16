import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/errors";
import { AppShell, BrandMark } from "@/components/AppShell";
import { Text } from "@/components/Text";
import { useAuthStore } from "@/auth/authStore";
import { useTheme } from "@/theme/ThemeProvider";

const ownerEmail = "ai.ro.dodoloata@gmail.com";

export function FriendsAndFamilyScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const email = useAuthStore((state) => state.user?.email)?.toLowerCase();
  const [recipient, setRecipient] = useState("");
  const grants = useQuery({ queryKey: ["premium-grants"], queryFn: () => api.listPremiumGrants(), enabled: email === ownerEmail });
  const grant = useMutation({
    mutationFn: () => api.grantPremium(recipient.trim()),
    onSuccess: () => {
      setRecipient("");
      void queryClient.invalidateQueries({ queryKey: ["premium-grants"] });
    }
  });
  const revoke = useMutation({
    mutationFn: (id: string) => api.revokePremium(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["premium-grants"] })
  });

  if (email !== ownerEmail) {
    return <AppShell showNavigation={false}><View style={styles.screen}><BrandMark detail="Administration" /><Text style={[styles.error, { color: theme.colors.warning }]}>This account is not authorized to manage Premium access.</Text></View></AppShell>;
  }

  return <AppShell showNavigation={false}><ScrollView contentContainerStyle={styles.screen}>
    <BrandMark detail="Administration" />
    <View style={[styles.panel, { borderColor: theme.colors.border }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>Friends and family Premium</Text>
      <Text style={[styles.body, { color: theme.colors.mutedText }]}>Grant access after the person has signed in and saved their profile.</Text>
      <TextInput accessibilityLabel="Email address" autoCapitalize="none" autoComplete="email" keyboardType="email-address" onChangeText={setRecipient} placeholder="friend@example.com" placeholderTextColor={theme.colors.mutedText} style={[styles.input, { borderColor: theme.colors.border, color: theme.colors.text }]} value={recipient} />
      <Pressable accessibilityRole="button" disabled={!recipient.trim() || grant.isPending} onPress={() => grant.mutate()} style={[styles.button, { backgroundColor: theme.colors.primary }]}><Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{grant.isPending ? "Granting" : "Grant Premium"}</Text></Pressable>
      {grant.isError ? <Text style={[styles.error, { color: theme.colors.warning }]}>{grant.error instanceof ApiError && grant.error.status === 404 ? "They need to sign in and save their profile first." : "Premium access could not be granted."}</Text> : null}
    </View>
    <View style={[styles.panel, { borderColor: theme.colors.border }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>Active grants</Text>
      {grants.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading grants</Text> : null}
      {grants.data?.length === 0 ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>No active grants.</Text> : null}
      {grants.data?.map((item) => <View key={item.id} style={[styles.grant, { borderColor: theme.colors.border }]}><Text style={[styles.email, { color: theme.colors.text }]}>{item.email}</Text><Pressable accessibilityRole="button" disabled={revoke.isPending} onPress={() => revoke.mutate(item.id)}><Text style={[styles.revoke, { color: theme.colors.warning }]}>Remove</Text></Pressable></View>)}
    </View>
  </ScrollView></AppShell>;
}

const styles = StyleSheet.create({
  screen: { gap: 22, padding: 20, paddingBottom: 44 },
  panel: { borderTopWidth: 1, gap: 14, paddingVertical: 20 },
  title: { fontSize: 19, fontWeight: "700" },
  body: { fontSize: 14, lineHeight: 21 },
  input: { borderWidth: 1, borderRadius: 8, fontSize: 16, minHeight: 46, paddingHorizontal: 12 },
  button: { alignItems: "center", borderRadius: 8, justifyContent: "center", minHeight: 46, paddingHorizontal: 16 },
  buttonText: { fontSize: 14, fontWeight: "700" },
  error: { fontSize: 13, lineHeight: 19 },
  grant: { alignItems: "center", borderTopWidth: 1, flexDirection: "row", justifyContent: "space-between", paddingVertical: 14 },
  email: { flex: 1, fontSize: 15 },
  revoke: { fontSize: 14, fontWeight: "700", paddingLeft: 16 }
});
