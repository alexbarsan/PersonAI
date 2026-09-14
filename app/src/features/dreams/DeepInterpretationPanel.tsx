import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { Pressable, StyleSheet, View } from "react-native";
import { Text } from "@/components/Text";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/client";
import { DeepInterpretationResponse } from "@/api/dto";
import { ResultSectionRenderer } from "@/features/dreams/ResultSectionRenderer";
import { DreamingFacts } from "@/features/content/DailyDreamContent";
import { useTheme } from "@/theme/ThemeProvider";

export function DeepInterpretationPanel({ dreamId, enabled }: { dreamId: string; enabled: boolean }) {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const deep = useQuery({
    queryKey: ["deep-interpretation", dreamId],
    queryFn: () => api.getDeepInterpretation(dreamId),
    enabled,
    retry: false
  });
  const create = useMutation({
    mutationFn: () => api.createDeepInterpretation(dreamId),
    onSuccess: (result) => queryClient.setQueryData<DeepInterpretationResponse>(["deep-interpretation", dreamId], result)
  });
  const expectedMissing = deep.error instanceof ApiError && deep.error.status === 404;
  const result = deep.data ?? create.data;

  if (!enabled) {
    return (
      <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
        <Text style={[styles.eyebrow, { color: theme.colors.mutedText }]}>Premium</Text>
        <Text style={[styles.title, { color: theme.colors.text }]}>Cognitive Analysis</Text>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>Explore this dream alongside related patterns from your journal.</Text>
        <Pressable accessibilityRole="button" onPress={() => router.push("/paywall")} style={[styles.secondaryButton, { borderColor: theme.colors.primary }]}>
          <Text style={[styles.buttonText, { color: theme.colors.primary }]}>Cognitive Analysis</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
      <Text style={[styles.eyebrow, { color: theme.colors.mutedText }]}>Premium</Text>
      <Text style={[styles.title, { color: theme.colors.text }]}>Cognitive Analysis</Text>
      {!result ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Explore possible thought patterns, attention, agency, and emotional responses using this dream and relevant journal context.</Text> : null}
      {deep.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Checking for saved analysis</Text> : null}
      {deep.isError && !expectedMissing ? <Text style={[styles.error, { color: theme.colors.warning }]}>Saved Cognitive Analysis could not be loaded.</Text> : null}
       {create.isError ? <Text style={[styles.error, { color: theme.colors.warning }]}>{mapCreateError(create.error)}</Text> : null}
       {create.isPending ? <DreamingFacts label="While your Cognitive Analysis takes shape" /> : null}
       {!result && (!deep.isLoading || expectedMissing) ? (
        <Pressable
          accessibilityRole="button"
          disabled={create.isPending}
          onPress={() => create.mutate()}
          style={[styles.primaryButton, { backgroundColor: theme.colors.primary }, create.isPending && styles.disabled]}
          testID="create-deep-interpretation"
        >
          <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{create.isPending ? "Creating Cognitive Analysis" : "Cognitive Analysis"}</Text>
        </Pressable>
      ) : null}
      {result ? (
        <View style={styles.result} testID="deep-interpretation-result">
          <View style={[styles.interpretation, { borderColor: theme.colors.primary }]}>
            <Text style={[styles.interpretationTitle, { color: theme.colors.primary }]}>Interpretation</Text>
            <Text style={[styles.summaryText, { color: theme.colors.text }]}>{getCognitiveInterpretation(result)}</Text>
          </View>
          {result.result.sections
            .filter((section) => section.title.trim().toLowerCase() === "cognitive symbols")
            .map((section, index) => <ResultSectionRenderer key={`${section.title}-${index}`} section={section} />)}
          <Text style={[styles.caveat, { color: theme.colors.mutedText }]}>A reflective reading, not a diagnosis or prediction.</Text>
        </View>
      ) : null}
    </View>
  );
}

function getCognitiveInterpretation(result: DeepInterpretationResponse) {
  const interpretation = result.result.sections.find(
    (section) => section.title.trim().toLowerCase() === "interpretation" && typeof section.content === "string"
  );
  return typeof interpretation?.content === "string" && interpretation.content.trim()
    ? interpretation.content
    : result.result.summary;
}

function mapCreateError(error: Error) {
  if (error instanceof ApiError) {
    if (error.status === 403) return "Cognitive Analysis requires Premium.";
    if (error.status === 409) return "Your profile, consent, and interpretation must be ready before Cognitive Analysis.";
    if (error.status === 429) return "You have reached today's Cognitive Analysis limit.";
    if (error.status === 503) return "Cognitive Analysis is temporarily unavailable. Please try again.";
  }
  return "Cognitive Analysis could not be created. Please try again.";
}

const styles = StyleSheet.create({
  panel: { borderTopWidth: 1, gap: 14, paddingVertical: 24 },
  eyebrow: { fontSize: 12, fontWeight: "700" },
  title: { fontSize: 19, fontWeight: "700" },
  body: { fontSize: 15, lineHeight: 22 },
  error: { fontSize: 13, lineHeight: 18 },
  primaryButton: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 46, paddingHorizontal: 16 },
  secondaryButton: { alignItems: "center", borderRadius: 6, borderWidth: 1, justifyContent: "center", minHeight: 44, paddingHorizontal: 16 },
  buttonText: { fontSize: 15, fontWeight: "800" },
  disabled: { opacity: 0.6 },
  result: { gap: 12 },
  interpretation: { borderLeftWidth: 3, gap: 8, paddingLeft: 16, paddingVertical: 4 },
  interpretationTitle: { fontSize: 15, fontWeight: "700" },
  summaryText: { fontSize: 17, fontWeight: "700", lineHeight: 24 },
  caveat: { fontSize: 12, lineHeight: 18 }
});
