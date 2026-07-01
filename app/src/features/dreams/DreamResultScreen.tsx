import { useQuery } from "@tanstack/react-query";
import { useLocalSearchParams } from "expo-router";
import { ScrollView, StyleSheet, Text, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ResultSectionRenderer } from "@/features/dreams/ResultSectionRenderer";
import { SafetyCard } from "@/features/dreams/SafetyCard";
import { useDreamResultStore } from "@/state/dreamResultStore";
import { useTheme } from "@/theme/ThemeProvider";

export function DreamResultScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const params = useLocalSearchParams<{ id?: string }>();
  const id = Array.isArray(params.id) ? params.id[0] : params.id;
  const cachedDream = useDreamResultStore((state) => (id ? state.getDream(id) : null));
  const dream = useQuery({
    queryKey: ["dream", id],
    queryFn: () => api.getDream(id!),
    enabled: Boolean(id) && !cachedDream,
    initialData: cachedDream ?? undefined
  });
  const result = dream.data?.result;
  const elevatedSafety = result?.safety?.selfHarmRisk === "elevated";

  return (
    <ScrollView contentContainerStyle={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>Dream result</Text>
      <Text style={[styles.disclaimer, { color: theme.colors.warning }]} testID="result-disclaimer">
        DreamLens is for reflection and entertainment. It is not medical, mental health, or safety advice.
      </Text>

      {dream.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading result</Text> : null}
      {dream.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Result could not be loaded.</Text> : null}
      {dream.data?.status === "failed" ? (
        <Text style={[styles.body, { color: theme.colors.warning }]}>
          {dream.data.errorMessage ?? "The interpretation service could not produce a result."}
        </Text>
      ) : null}

      {result ? (
        <View style={styles.content}>
          <Text style={[styles.summary, { color: theme.colors.text }]}>{result.summary}</Text>
          <SafetyCard safety={result.safety} />
          {elevatedSafety
            ? null
            : result.sections.map((section, index) => (
                <ResultSectionRenderer key={`${section.title}-${index}`} section={section} />
              ))}
          {elevatedSafety || result.followUpQuestions.length === 0 ? null : (
            <View style={[styles.questions, { borderColor: theme.colors.border }]}>
              <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Follow-up questions</Text>
              {result.followUpQuestions.map((question) => (
                <Text key={question} style={[styles.body, { color: theme.colors.mutedText }]}>
                  {question}
                </Text>
              ))}
            </View>
          )}
        </View>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: {
    gap: 18,
    padding: 20,
    paddingBottom: 48
  },
  title: {
    fontSize: 30,
    fontWeight: "700"
  },
  disclaimer: {
    fontSize: 14,
    lineHeight: 20
  },
  body: {
    fontSize: 15,
    lineHeight: 22
  },
  content: {
    gap: 14
  },
  summary: {
    fontSize: 18,
    fontWeight: "700",
    lineHeight: 25
  },
  questions: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 8,
    padding: 14
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: "700"
  }
});
