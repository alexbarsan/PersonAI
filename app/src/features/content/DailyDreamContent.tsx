import { useQuery } from "@tanstack/react-query";
import { StyleSheet, View } from "react-native";
import { useApiClient } from "@/api/apiContext";
import { Text } from "@/components/Text";
import { useTheme } from "@/theme/ThemeProvider";

function localDate() {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

function useDailyDreamContent() {
  const api = useApiClient();
  const date = localDate();
  return useQuery({
    queryKey: ["daily-dream-content", date],
    queryFn: () => api.getDailyDreamContent(date),
    staleTime: 1000 * 60 * 60 * 12
  });
}

export function DailyDreamQuote() {
  const theme = useTheme();
  const content = useDailyDreamContent();

  if (!content.data) {
    return null;
  }

  return (
    <View style={[styles.quote, { borderColor: "rgba(36, 92, 73, 0.24)", backgroundColor: "rgba(255,255,255,0.76)" }]} testID="daily-dream-quote">
      <Text style={[styles.quoteLabel, { color: theme.colors.primary }]}>Today&apos;s dream thought</Text>
      <Text style={[styles.quoteText, { color: theme.colors.text }]}>{content.data.quote}</Text>
      {content.data.attribution ? <Text style={[styles.attribution, { color: theme.colors.mutedText }]}>{content.data.attribution}</Text> : null}
    </View>
  );
}

export function DreamingFacts({ label = "While we reflect" }: { label?: string }) {
  const theme = useTheme();
  const content = useDailyDreamContent();

  return (
    <View style={[styles.facts, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]} testID="dreaming-facts">
      <Text style={[styles.factsLabel, { color: theme.colors.primary }]}>{label}</Text>
      {content.data ? content.data.facts.slice(0, 3).map((fact, index) => (
        <View key={fact} style={styles.factRow}>
          <Text style={[styles.factNumber, { color: theme.colors.primary }]}>{index + 1}</Text>
          <Text style={[styles.factText, { color: theme.colors.text }]}>{fact}</Text>
        </View>
      )) : (
        <Text style={[styles.factText, { color: theme.colors.mutedText }]}>Finding a few dream facts for you...</Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  quote: { alignSelf: "flex-start", borderLeftWidth: 3, gap: 5, marginTop: 18, maxWidth: 520, paddingHorizontal: 14, paddingVertical: 12 },
  quoteLabel: { fontSize: 11, fontWeight: "800", textTransform: "uppercase" },
  quoteText: { fontSize: 16, fontStyle: "italic", lineHeight: 23 },
  attribution: { fontSize: 12 },
  facts: { borderRadius: 8, borderWidth: 1, gap: 10, padding: 14 },
  factsLabel: { fontSize: 12, fontWeight: "800", textTransform: "uppercase" },
  factRow: { alignItems: "flex-start", flexDirection: "row", gap: 10 },
  factNumber: { fontSize: 13, fontWeight: "800", lineHeight: 20, width: 14 },
  factText: { flex: 1, fontSize: 14, lineHeight: 20 }
});
