import { useMutation } from "@tanstack/react-query";
import { router } from "expo-router";
import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/errors";
import { AppShell, BrandMark } from "@/components/AppShell";
import { useTheme } from "@/theme/ThemeProvider";

export function AskDreamsScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const [question, setQuestion] = useState("");
  const ask = useMutation({ mutationFn: () => api.askDreams({ question: question.trim() }) });
  const valid = question.trim().length >= 5 && question.trim().length <= 500;

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen} keyboardShouldPersistTaps="handled">
        <BrandMark detail="Explore patterns grounded in your own dream journal." />
        <View style={[styles.intro, { backgroundColor: theme.colors.sage }]}>
          <Text style={[styles.title, { color: theme.colors.text }]}>Ask your dream history</Text>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>Dream DNA finds relevant dreams first, then answers from those memories.</Text>
        </View>

        <View style={styles.form}>
          <Text style={[styles.label, { color: theme.colors.text }]}>What pattern are you curious about?</Text>
          <TextInput
            accessibilityLabel="Dream history question"
            multiline
            maxLength={500}
            onChangeText={setQuestion}
            placeholder="When do water dreams tend to appear?"
            placeholderTextColor={theme.colors.mutedText}
            style={[styles.input, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border, color: theme.colors.text }]}
            value={question}
          />
          <View style={styles.formFooter}>
            <Text style={[styles.counter, { color: theme.colors.mutedText }]}>{question.length}/500</Text>
            <Pressable
              accessibilityRole="button"
              disabled={!valid || ask.isPending}
              onPress={() => ask.mutate()}
              style={[styles.button, { backgroundColor: theme.colors.primary }, (!valid || ask.isPending) && styles.buttonDisabled]}
            >
              <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{ask.isPending ? "Finding patterns" : "Ask Dream DNA"}</Text>
            </Pressable>
          </View>
        </View>

        {ask.isError ? <View style={[styles.message, { borderColor: theme.colors.border }]}><Text style={[styles.messageTitle, { color: theme.colors.text }]}>No answer yet</Text><Text style={[styles.body, { color: theme.colors.warning }]}>{errorMessage(ask.error)}</Text></View> : null}

        {ask.data ? <View style={styles.answer}>
          <View style={[styles.answerPanel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
            <Text style={[styles.eyebrow, { color: theme.colors.mutedText }]}>From {ask.data.sampleSize} relevant {ask.data.sampleSize === 1 ? "dream" : "dreams"}</Text>
            <Text style={[styles.answerText, { color: theme.colors.text }]}>{ask.data.answer}</Text>
          </View>
          {ask.data.observations.length > 0 ? <View style={styles.observations}><Text style={[styles.sectionTitle, { color: theme.colors.text }]}>What stood out</Text>{ask.data.observations.map((observation) => <View key={observation} style={styles.observation}><View style={[styles.dot, { backgroundColor: theme.colors.primary }]} /><Text style={[styles.body, styles.observationText, { color: theme.colors.mutedText }]}>{observation}</Text></View>)}</View> : null}
          <View style={styles.sources}><Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Dreams used</Text>{ask.data.sources.map((source) => <Pressable key={source.id} accessibilityRole="button" onPress={() => router.push(`/dreams/${source.id}`)} style={[styles.source, { borderColor: theme.colors.border }]}><Text numberOfLines={2} style={[styles.sourceText, { color: theme.colors.text }]}>{source.summary}</Text><Text style={[styles.sourceDate, { color: theme.colors.mutedText }]}>{formatDate(source.occurredAt ?? source.createdAt)}</Text></Pressable>)}</View>
          <Text style={[styles.caveat, { color: theme.colors.mutedText }]}>{ask.data.caveat}</Text>
        </View> : null}
      </ScrollView>
    </AppShell>
  );
}

function errorMessage(error: Error) {
  if (error instanceof ApiError) {
    if (error.status === 429) return "Today's question limit has been reached.";
    if (error.status === 409) return "Enable AI processing and dream history use in your profile first.";
    if (error.status === 503) return "Your semantic dream memory is not ready yet. Try again after your journal has been indexed.";
  }
  return "Dream DNA could not answer right now. Please try again.";
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(value.length === 10 ? `${value}T00:00:00Z` : value));
}

const styles = StyleSheet.create({
  screen: { gap: 18, padding: 20, paddingBottom: 28 },
  intro: { borderRadius: 8, gap: 7, padding: 18 },
  title: { fontSize: 28, fontWeight: "700", lineHeight: 34 },
  body: { fontSize: 14, lineHeight: 21 },
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
  sourceDate: { fontSize: 12, lineHeight: 17 },
  caveat: { fontSize: 12, lineHeight: 18 }
});
