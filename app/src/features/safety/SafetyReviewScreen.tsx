import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/errors";
import { SensitiveSafetyReviewResponse } from "@/api/dto";
import { AppShell, BrandMark } from "@/components/AppShell";
import { useTheme } from "@/theme/ThemeProvider";

const categoryLabels: Record<string, string> = {
  "self-harm-or-suicide": "Self-harm or suicide",
  "threats-to-others": "Threats to others",
  "sexual-violence-or-coercion": "Sexual violence or coercion",
  "possible-minor-sexual-content": "Possible minor sexual content",
  "abuse-or-trauma": "Abuse or trauma",
  "explicit-adult-sexual-content": "Explicit adult sexual content"
};

export function SafetyReviewScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [purpose, setPurpose] = useState("");
  const [rawText, setRawText] = useState<string | null>(null);
  const reviews = useQuery({ queryKey: ["safety-reviews", "open"], queryFn: () => api.listSensitiveSafetyReviews("open") });
  const acknowledge = useMutation({
    mutationFn: (id: string) => api.acknowledgeSensitiveSafetyReview(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["safety-reviews"] })
  });
  const accessRawText = useMutation({
    mutationFn: ({ id, reviewPurpose }: { id: string; reviewPurpose: string }) => api.accessSensitiveSafetyReviewRawText(id, { purpose: reviewPurpose }),
    onSuccess: (response) => setRawText(response.dreamText)
  });

  const unauthorized = isUnauthorized(reviews.error) || isUnauthorized(accessRawText.error) || isUnauthorized(acknowledge.error);

  return (
    <AppShell showNavigation={false}>
      <ScrollView contentContainerStyle={styles.screen} keyboardShouldPersistTaps="handled">
        <BrandMark detail="Privacy administration" />
        <View style={[styles.hero, { backgroundColor: theme.colors.softInk }]}>
          <Text style={[styles.title, { color: theme.colors.text }]}>Safety review</Text>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>Review metadata first. Original text is released only after you provide a case-specific purpose, and that access is audited.</Text>
        </View>
        {unauthorized ? <Text style={[styles.error, { color: theme.colors.warning }]}>This account is not authorized for safety review.</Text> : null}
        {reviews.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading review queue</Text> : null}
        {reviews.isError && !unauthorized ? <Text style={[styles.error, { color: theme.colors.warning }]}>The review queue could not be loaded.</Text> : null}
        {reviews.data?.length === 0 ? <View style={[styles.empty, { backgroundColor: theme.colors.sage }]}><Text style={[styles.itemTitle, { color: theme.colors.text }]}>No open reviews</Text><Text style={[styles.body, { color: theme.colors.mutedText }]}>There are no current sensitive-content events requiring review.</Text></View> : null}
        <View style={styles.list}>
          {reviews.data?.map((review) => <ReviewRow key={review.id} review={review} selected={selectedId === review.id} purpose={purpose} rawText={selectedId === review.id ? rawText : null} onSelect={() => { setSelectedId(review.id); setPurpose(""); setRawText(null); }} onPurposeChange={setPurpose} onAcknowledge={() => acknowledge.mutate(review.id)} onAccess={() => accessRawText.mutate({ id: review.id, reviewPurpose: purpose.trim() })} isAcknowledging={acknowledge.isPending} isAccessing={accessRawText.isPending} accessError={accessRawText.error} />)}
        </View>
      </ScrollView>
    </AppShell>
  );
}

function ReviewRow({ review, selected, purpose, rawText, onSelect, onPurposeChange, onAcknowledge, onAccess, isAcknowledging, isAccessing, accessError }: {
  review: SensitiveSafetyReviewResponse;
  selected: boolean;
  purpose: string;
  rawText: string | null;
  onSelect: () => void;
  onPurposeChange: (value: string) => void;
  onAcknowledge: () => void;
  onAccess: () => void;
  isAcknowledging: boolean;
  isAccessing: boolean;
  accessError: Error | null;
}) {
  const theme = useTheme();
  const canAccess = purpose.trim().length >= 10 && !isAccessing;
  return <View style={[styles.item, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
    <View style={styles.itemHeader}>
      <View style={styles.itemText}><Text style={[styles.itemTitle, { color: theme.colors.text }]}>{categoryLabels[review.category] ?? review.category}</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{review.subjectPseudonym} | {Math.round(review.confidence * 100)}% confidence | detected {formatDate(review.detectedAt)}</Text></View>
      <View style={[styles.badge, { backgroundColor: review.severity === "high" ? theme.colors.lavender : theme.colors.sage }]}><Text style={[styles.badgeText, { color: theme.colors.text }]}>{review.severity}</Text></View>
    </View>
    <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{review.restrictsElaboration ? "Interpretation is restricted" : "Interpretation is not restricted"} | expires {formatDate(review.expiresAt)}</Text>
    <View style={styles.actions}>
      <Pressable accessibilityRole="button" onPress={onSelect} style={[styles.secondaryButton, { borderColor: theme.colors.border }]}><Text style={[styles.secondaryButtonText, { color: theme.colors.text }]}>View original</Text></Pressable>
      <Pressable accessibilityRole="button" disabled={isAcknowledging} onPress={onAcknowledge} style={styles.acknowledgeButton}><Text style={[styles.acknowledgeText, { color: theme.colors.mutedText }]}>{isAcknowledging ? "Saving" : "Acknowledge"}</Text></Pressable>
    </View>
    {selected ? <View style={[styles.accessPanel, { backgroundColor: theme.colors.background, borderColor: theme.colors.border }]}>
      <Text style={[styles.accessTitle, { color: theme.colors.text }]}>Access original text</Text>
      <TextInput accessibilityLabel="Review access purpose" multiline onChangeText={onPurposeChange} placeholder="Reason for access, at least 10 characters" placeholderTextColor={theme.colors.mutedText} style={[styles.purposeInput, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border, color: theme.colors.text }]} value={purpose} />
      <Pressable accessibilityRole="button" disabled={!canAccess} onPress={onAccess} style={[styles.primaryButton, { backgroundColor: canAccess ? theme.colors.primary : theme.colors.border }]}><Text style={[styles.primaryButtonText, { color: theme.colors.primaryText }]}>{isAccessing ? "Accessing" : "Access and audit"}</Text></Pressable>
      {accessError && !isUnauthorized(accessError) ? <Text style={[styles.error, { color: theme.colors.warning }]}>Original text could not be accessed.</Text> : null}
      {rawText ? <View testID={`review-original-${review.id}`} style={[styles.originalText, { borderColor: theme.colors.border }]}><Text style={[styles.accessTitle, { color: theme.colors.text }]}>Original submitted text</Text><Text selectable style={[styles.body, { color: theme.colors.text }]}>{rawText}</Text></View> : null}
    </View> : null}
  </View>;
}

function isUnauthorized(error: Error | null) {
  return error instanceof ApiError && error.status === 403;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(value));
}

const styles = StyleSheet.create({
  screen: { gap: 16, padding: 20, paddingBottom: 28 },
  hero: { borderRadius: 8, gap: 7, padding: 18 },
  title: { fontSize: 29, fontWeight: "700", lineHeight: 35 },
  body: { fontSize: 14, lineHeight: 21 },
  error: { fontSize: 14, fontWeight: "700", lineHeight: 20 },
  empty: { borderRadius: 8, gap: 7, padding: 18 },
  list: { gap: 12 },
  item: { borderRadius: 8, borderWidth: 1, gap: 10, padding: 14 },
  itemHeader: { alignItems: "flex-start", flexDirection: "row", gap: 10, justifyContent: "space-between" },
  itemText: { flex: 1, gap: 4 },
  itemTitle: { fontSize: 16, fontWeight: "800", lineHeight: 22 },
  meta: { fontSize: 12, lineHeight: 18 },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 4 },
  badgeText: { fontSize: 12, fontWeight: "800", textTransform: "capitalize" },
  actions: { alignItems: "center", flexDirection: "row", gap: 14 },
  secondaryButton: { borderRadius: 6, borderWidth: 1, minHeight: 38, justifyContent: "center", paddingHorizontal: 12 },
  secondaryButtonText: { fontSize: 13, fontWeight: "800" },
  acknowledgeButton: { minHeight: 38, justifyContent: "center", paddingHorizontal: 4 },
  acknowledgeText: { fontSize: 13, fontWeight: "800" },
  accessPanel: { borderRadius: 6, borderWidth: 1, gap: 10, padding: 12 },
  accessTitle: { fontSize: 14, fontWeight: "800", lineHeight: 20 },
  purposeInput: { borderRadius: 6, borderWidth: 1, fontSize: 14, lineHeight: 20, minHeight: 80, padding: 10, textAlignVertical: "top" },
  primaryButton: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 42, paddingHorizontal: 14 },
  primaryButtonText: { fontSize: 14, fontWeight: "800" },
  originalText: { borderRadius: 6, borderWidth: 1, gap: 8, padding: 12 }
});
