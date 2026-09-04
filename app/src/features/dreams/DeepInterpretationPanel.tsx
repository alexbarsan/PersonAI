import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { Pressable, StyleSheet, Text, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/client";
import { DeepInterpretationResponse } from "@/api/dto";
import { ResultSectionRenderer } from "@/features/dreams/ResultSectionRenderer";
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
        <Text style={[styles.title, { color: theme.colors.text }]}>Deep Interpretation</Text>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>Explore this dream alongside related patterns from your journal.</Text>
        <Pressable accessibilityRole="button" onPress={() => router.push("/paywall")} style={[styles.secondaryButton, { borderColor: theme.colors.primary }]}>
          <Text style={[styles.buttonText, { color: theme.colors.primary }]}>View Premium</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
      <Text style={[styles.eyebrow, { color: theme.colors.mutedText }]}>Premium</Text>
      <Text style={[styles.title, { color: theme.colors.text }]}>Deep Interpretation</Text>
      {!result ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Look more closely at this dream using your profile and the most relevant patterns in your journal.</Text> : null}
      {deep.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Checking for saved analysis</Text> : null}
      {deep.isError && !expectedMissing ? <Text style={[styles.error, { color: theme.colors.warning }]}>Saved Deep Interpretation could not be loaded.</Text> : null}
      {create.isError ? <Text style={[styles.error, { color: theme.colors.warning }]}>{mapCreateError(create.error)}</Text> : null}
      {!result && (!deep.isLoading || expectedMissing) ? (
        <Pressable
          accessibilityRole="button"
          disabled={create.isPending}
          onPress={() => create.mutate()}
          style={[styles.primaryButton, { backgroundColor: theme.colors.primary }, create.isPending && styles.disabled]}
          testID="create-deep-interpretation"
        >
          <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{create.isPending ? "Looking for deeper patterns" : "Go deeper"}</Text>
        </Pressable>
      ) : null}
      {result ? (
        <View style={styles.result} testID="deep-interpretation-result">
          <View style={[styles.summary, { backgroundColor: theme.colors.lavender }]}>
            <Text style={[styles.summaryText, { color: theme.colors.text }]}>{result.result.summary}</Text>
          </View>
          {result.result.sections.map((section, index) => <ResultSectionRenderer key={`${section.title}-${index}`} section={section} />)}
          {result.sources.length > 0 ? (
            <View style={styles.sources}>
              <Text style={[styles.sourceTitle, { color: theme.colors.text }]}>Related journal patterns</Text>
              {result.sources.map((source) => (
                <Pressable key={source.id} accessibilityRole="button" onPress={() => router.push(`/dreams/${source.id}`)}>
                  <Text style={[styles.source, { color: theme.colors.primary }]} numberOfLines={2}>{source.summary}</Text>
                </Pressable>
              ))}
            </View>
          ) : null}
          <Text style={[styles.caveat, { color: theme.colors.mutedText }]}>A reflective reading, not a diagnosis or prediction.</Text>
        </View>
      ) : null}
    </View>
  );
}

function mapCreateError(error: Error) {
  if (error instanceof ApiError) {
    if (error.status === 403) return "Deep Interpretation requires Premium.";
    if (error.status === 409) return "Your profile, consent, and first interpretation must be ready before going deeper.";
    if (error.status === 429) return "You have reached today's Deep Interpretation limit.";
    if (error.status === 503) return "Deep Interpretation is temporarily unavailable. Please try again.";
  }
  return "Deep Interpretation could not be created. Please try again.";
}

const styles = StyleSheet.create({
  panel: { borderRadius: 8, borderWidth: 1, gap: 10, padding: 16 },
  eyebrow: { fontSize: 11, fontWeight: "800", textTransform: "uppercase" },
  title: { fontSize: 19, fontWeight: "700" },
  body: { fontSize: 15, lineHeight: 22 },
  error: { fontSize: 13, lineHeight: 18 },
  primaryButton: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 46, paddingHorizontal: 16 },
  secondaryButton: { alignItems: "center", borderRadius: 6, borderWidth: 1, justifyContent: "center", minHeight: 44, paddingHorizontal: 16 },
  buttonText: { fontSize: 15, fontWeight: "800" },
  disabled: { opacity: 0.6 },
  result: { gap: 12 },
  summary: { borderRadius: 6, padding: 14 },
  summaryText: { fontSize: 17, fontWeight: "700", lineHeight: 24 },
  sources: { gap: 8 },
  sourceTitle: { fontSize: 15, fontWeight: "700" },
  source: { fontSize: 14, lineHeight: 20 },
  caveat: { fontSize: 12, lineHeight: 18 }
});
