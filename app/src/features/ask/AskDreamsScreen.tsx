import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/errors";
import { AppShell, BrandMark } from "@/components/AppShell";
import { Text } from "@/components/Text";
import { useTheme } from "@/theme/ThemeProvider";

const maxQuestionLength = 300;

export function AskDreamsScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const [question, setQuestion] = useState("");
  const entitlement = useQuery({ queryKey: ["entitlements"], queryFn: () => api.getEntitlements() });
  const isPremium = entitlement.data?.tier === "premium" || entitlement.data?.askQuotaExempt === true;
  const memoryStatus = useQuery({
    queryKey: ["ask-dream-memory-status"],
    queryFn: () => api.getAskDreamMemoryStatus(),
    enabled: isPremium,
  });
  const remaining = entitlement.data?.askRemaining;
  const ask = useMutation({
    mutationFn: () => api.askDreams({ question: question.trim() }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["entitlements"] });
      queryClient.invalidateQueries({ queryKey: ["ask-dream-memory-status"] });
    }
  });
  const valid = question.trim().length >= 5 && question.trim().length <= maxQuestionLength;
  const isExhausted = remaining !== null && remaining !== undefined && remaining <= 0;
  const memoryNotReady = memoryStatus.data?.isReady === false;
  const needsMemoryRefresh = errorCode(ask.error) === "memory_not_ready";

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen} keyboardShouldPersistTaps="handled">
        <BrandMark detail="Explore patterns grounded in your own dream journal." />
        <View style={[styles.intro, { backgroundColor: theme.colors.sage }]}>
          <Text style={[styles.title, { color: theme.colors.text }]}>Ask your dream history</Text>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>Dream DNA finds relevant dreams first, then answers from those memories.</Text>
        </View>

        {entitlement.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Checking your plan</Text> : null}
        {entitlement.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Your plan could not be checked. Please refresh and try again.</Text> : null}
        {entitlement.data && !isPremium ? <PremiumGate /> : null}
        {entitlement.data && isPremium ? <>
          <View style={[styles.quota, { borderColor: theme.colors.border, backgroundColor: theme.colors.surface }]}>
            <Text style={[styles.quotaTitle, { color: theme.colors.text }]}>Dream history questions</Text>
            <Text style={[styles.body, { color: theme.colors.mutedText }]}>{quotaMessage(remaining, entitlement.data.askDailyLimit ?? 3, entitlement.data.askResetsAt ?? null)}</Text>
          </View>
          {memoryStatus.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Checking your dream memory</Text> : null}
          {memoryStatus.data && !memoryStatus.data.isReady ? <MemoryReadiness status={memoryStatus.data} onRefresh={() => memoryStatus.refetch()} isRefreshing={memoryStatus.isFetching} /> : null}
          {memoryStatus.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Dream memory status could not be checked. You can still try your question.</Text> : null}
          <View style={styles.form}>
            <View style={styles.suggestions}>{["When do water dreams appear?", "Which places keep returning?", "How have my dreams been feeling?"].map(suggestion => <Pressable key={suggestion} accessibilityRole="button" onPress={() => { ask.reset(); setQuestion(suggestion); }} style={[styles.suggestion, { borderColor: theme.colors.border }]}><Text style={[styles.suggestionText, { color: theme.colors.primary }]}>{suggestion}</Text></Pressable>)}</View>
            <Text style={[styles.label, { color: theme.colors.text }]}>What pattern are you curious about?</Text>
            <TextInput
              accessibilityLabel="Dream history question"
              editable={!ask.isPending && !isExhausted && !memoryNotReady}
              multiline
              maxLength={maxQuestionLength}
              onChangeText={(value) => { ask.reset(); setQuestion(value); }}
              placeholder="When do water dreams tend to appear?"
              placeholderTextColor={theme.colors.mutedText}
              style={[styles.input, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border, color: theme.colors.text }]}
              value={question}
            />
            <View style={styles.formFooter}>
              <Text style={[styles.counter, { color: theme.colors.mutedText }]}>{question.length}/{maxQuestionLength}</Text>
              <Pressable accessibilityRole="button" disabled={!valid || ask.isPending || isExhausted || memoryNotReady} onPress={() => ask.mutate()} style={[styles.button, { backgroundColor: theme.colors.primary }, (!valid || ask.isPending || isExhausted || memoryNotReady) && styles.buttonDisabled]}>
                <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{ask.isPending ? "Finding patterns" : "Ask Dream DNA"}</Text>
              </Pressable>
            </View>
          </View>
        </> : null}

        {ask.isError ? <View style={[styles.message, { borderColor: theme.colors.border }]}><Text style={[styles.messageTitle, { color: theme.colors.text }]}>No answer yet</Text><Text style={[styles.body, { color: theme.colors.warning }]}>{errorMessage(ask.error)}</Text><Pressable accessibilityRole="button" onPress={() => needsMemoryRefresh ? memoryStatus.refetch() : ask.mutate()} style={styles.retryButton}><Text style={[styles.retryText, { color: theme.colors.primary }]}>{needsMemoryRefresh ? "Refresh memory status" : "Try again"}</Text></Pressable></View> : null}

        {ask.data ? <View style={styles.answer}>
          <View style={[styles.answerPanel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
            <Text style={[styles.eyebrow, { color: theme.colors.mutedText }]}>Grounded in {ask.data.sources.length} cited {ask.data.sources.length === 1 ? "dream" : "dreams"}</Text>
            <Text style={[styles.answerText, { color: theme.colors.text }]}>{ask.data.answer}</Text>
          </View>
          {ask.data.observations.length > 0 ? <View style={styles.observations}><Text style={[styles.sectionTitle, { color: theme.colors.text }]}>What stood out</Text>{ask.data.observations.map((observation) => <View key={observation} style={styles.observation}><View style={[styles.dot, { backgroundColor: theme.colors.primary }]} /><Text style={[styles.body, styles.observationText, { color: theme.colors.mutedText }]}>{observation}</Text></View>)}</View> : null}
          <View style={styles.sources}><Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Cited dreams</Text><Text style={[styles.sourceContext, { color: theme.colors.mutedText }]}>The answer cites {ask.data.sources.length} of {ask.data.sampleSize} retrieved journal entries.</Text>{ask.data.sources.map((source) => <Pressable key={source.id} accessibilityRole="link" accessibilityLabel={`Open cited dream: ${source.title}`} onPress={() => router.push(`/dreams/${source.id}`)} style={[styles.source, { borderColor: theme.colors.border }]}><Text numberOfLines={2} style={[styles.sourceText, { color: theme.colors.text }]}>{source.title}</Text><Text numberOfLines={2} style={[styles.sourceSummary, { color: theme.colors.mutedText }]}>{source.summary}</Text><Text style={[styles.sourceDate, { color: theme.colors.mutedText }]}>{formatDate(source.occurredAt ?? source.createdAt)} | retrieved #{source.retrievalRank}</Text></Pressable>)}</View>
          <Text style={[styles.caveat, { color: theme.colors.mutedText }]}>{ask.data.caveat}</Text>
        </View> : null}
      </ScrollView>
    </AppShell>
  );
}

function PremiumGate() {
  const theme = useTheme();
  return <View style={[styles.gate, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
    <Text style={[styles.gateTitle, { color: theme.colors.text }]}>Available with Premium</Text>
    <Text style={[styles.body, { color: theme.colors.mutedText }]}>Ask Dream DNA about patterns across your own dream journal, up to three times each day.</Text>
    <Pressable accessibilityRole="button" onPress={() => router.push("/paywall")} style={[styles.secondaryButton, { borderColor: theme.colors.primary }]}>
      <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>Explore Premium</Text>
    </Pressable>
  </View>;
}

function MemoryReadiness({ status, onRefresh, isRefreshing }: {
  status: { completedDreams: number; indexedDreams: number; pendingDreams: number; message: string };
  onRefresh: () => void;
  isRefreshing: boolean;
}) {
  const theme = useTheme();
  return <View style={[styles.memory, { backgroundColor: theme.colors.lavender, borderColor: theme.colors.border }]}>
    <Text style={[styles.memoryTitle, { color: theme.colors.text }]}>Dream memory is getting ready</Text>
    <Text style={[styles.body, { color: theme.colors.mutedText }]}>{status.message}</Text>
    <Text style={[styles.memoryMeta, { color: theme.colors.mutedText }]}>{status.indexedDreams} of {status.completedDreams} interpreted {status.completedDreams === 1 ? "dream" : "dreams"} indexed{status.pendingDreams > 0 ? ` | ${status.pendingDreams} pending` : ""}</Text>
    <Pressable accessibilityRole="button" onPress={onRefresh} disabled={isRefreshing} style={styles.retryButton}><Text style={[styles.retryText, { color: theme.colors.primary }]}>{isRefreshing ? "Refreshing" : "Refresh status"}</Text></Pressable>
  </View>;
}

function quotaMessage(remaining: number | null | undefined, limit: number, resetAt: string | null) {
  const count = remaining === null || remaining === undefined ? "No question limit" : `${remaining} of ${limit} questions remaining today`;
  return resetAt ? `${count}. Resets ${formatReset(resetAt)}.` : count;
}

function formatReset(value: string) {
  return new Intl.DateTimeFormat("en", { hour: "numeric", minute: "2-digit" }).format(new Date(value));
}

function errorMessage(error: Error) {
  if (error instanceof ApiError) {
    if (error.status === 403) return "Dream history questions are available with Premium.";
    if (error.status === 429) return "Today's question limit has been reached.";
    if (error.status === 409 && errorCode(error) === "memory_not_ready") return "Your dream memory is not ready yet. Refresh its status while recent dreams are indexed.";
    if (error.status === 409) return "Enable AI processing and dream history use in your profile first.";
    if (error.status === 503 && errorCode(error) === "memory_unavailable") return "Dream memory is temporarily unavailable. Your question was not used.";
    if (error.status === 503) return "Dream DNA could not answer safely right now. Your question was not used.";
  }
  return "Dream DNA could not answer right now. Please try again.";
}

function errorCode(error: Error | null) {
  if (!(error instanceof ApiError) || !error.body || typeof error.body !== "object") return null;
  return Object.keys(error.body as Record<string, unknown>)[0] ?? null;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(value.length === 10 ? `${value}T00:00:00Z` : value));
}

const styles = StyleSheet.create({
  screen: { gap: 18, padding: 20, paddingBottom: 28 },
  intro: { gap: 10, marginHorizontal: -20, padding: 24 },
  title: { fontSize: 28, fontWeight: "700", lineHeight: 34 },
  body: { fontSize: 14, lineHeight: 21 },
  quota: { borderRadius: 8, borderWidth: 1, gap: 4, padding: 16 },
  quotaTitle: { fontSize: 15, fontWeight: "800" },
  memory: { borderRadius: 8, borderWidth: 1, gap: 6, padding: 16 },
  memoryTitle: { fontSize: 16, fontWeight: "800", lineHeight: 22 },
  memoryMeta: { fontSize: 12, lineHeight: 18 },
  gate: { borderRadius: 8, borderWidth: 1, gap: 10, padding: 18 },
  gateTitle: { fontSize: 18, fontWeight: "800" },
  secondaryButton: { alignItems: "center", alignSelf: "flex-start", borderRadius: 6, borderWidth: 1, justifyContent: "center", minHeight: 42, paddingHorizontal: 14 },
  secondaryButtonText: { fontSize: 14, fontWeight: "800" },
  suggestions: { flexDirection: "row", flexWrap: "wrap", gap: 8, marginBottom: 10 },
  suggestion: { borderWidth: 1, borderRadius: 8, padding: 12, minHeight: 44 },
  suggestionText: { fontSize: 13, lineHeight: 20 },
  form: { gap: 10 },
  label: { fontSize: 15, fontWeight: "800", lineHeight: 21 },
  input: { borderRadius: 8, borderWidth: 1, fontSize: 16, lineHeight: 23, minHeight: 116, padding: 14, textAlignVertical: "top" },
  formFooter: { alignItems: "center", flexDirection: "row", justifyContent: "space-between" },
  counter: { fontSize: 12 },
  button: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 44, minWidth: 142, paddingHorizontal: 16 },
  buttonDisabled: { opacity: 0.45 },
  buttonText: { fontSize: 14, fontWeight: "800" },
  message: { borderRadius: 8, borderWidth: 1, gap: 4, padding: 16 },
  messageTitle: { fontSize: 16, fontWeight: "800" },
  retryButton: { alignSelf: "flex-start", justifyContent: "center", minHeight: 40 },
  retryText: { fontSize: 14, fontWeight: "800" },
  answer: { gap: 18 },
  answerPanel: { borderRadius: 8, borderWidth: 1, gap: 9, padding: 18 },
  eyebrow: { fontSize: 12, fontWeight: "800", textTransform: "uppercase" },
  answerText: { fontSize: 19, fontWeight: "600", lineHeight: 28 },
  observations: { gap: 10 },
  sectionTitle: { fontSize: 16, fontWeight: "800", lineHeight: 22 },
  observation: { alignItems: "flex-start", flexDirection: "row", gap: 10 },
  observationText: { flex: 1 },
  dot: { borderRadius: 3, height: 6, marginTop: 8, width: 6 },
  sources: { gap: 8 },
  source: { borderBottomWidth: 1, gap: 4, paddingVertical: 10 },
  sourceText: { fontSize: 14, fontWeight: "700", lineHeight: 20 },
  sourceSummary: { fontSize: 13, lineHeight: 19 },
  sourceContext: { fontSize: 13, lineHeight: 19 },
  sourceDate: { fontSize: 12, lineHeight: 17 },
  caveat: { fontSize: 12, lineHeight: 18 }
});
