import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "expo-router";
import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { DreamJournalResponse } from "@/api/dto";
import { AppShell, BrandMark } from "@/components/AppShell";
import { useTheme } from "@/theme/ThemeProvider";

export function JournalListScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const [query, setQuery] = useState("");
  const [mood, setMood] = useState("");
  const [tag, setTag] = useState("");
  const filters = { query, mood, tag };
  const journal = useQuery({ queryKey: ["journal", filters], queryFn: () => api.listDreams(filters) });
  const deleteDream = useMutation({
    mutationFn: (id: string) => api.deleteDream(id),
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: ["journal"] });
      const previous = queryClient.getQueryData<DreamJournalResponse>(["journal"]);
      queryClient.setQueryData<DreamJournalResponse>(["journal"], { items: previous?.items.filter((item) => item.id !== id) ?? [] });
      return { previous };
    },
    onError: (_, __, context) => {
      if (context?.previous) queryClient.setQueryData(["journal"], context.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: ["journal"] })
  });

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen}>
        <BrandMark detail="Your remembered places, people, and feelings." />
        <View style={[styles.hero, { backgroundColor: theme.colors.sage }]}>
          <Text style={[styles.title, { color: theme.colors.text }]}>Your dreams</Text>
          <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>A private record that grows more useful with time.</Text>
        </View>
        <View style={[styles.filters, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
          <TextInput accessibilityLabel="Search dreams" onChangeText={setQuery} placeholder="Search your journal" placeholderTextColor={theme.colors.mutedText} style={[styles.searchInput, { backgroundColor: theme.colors.background, borderColor: theme.colors.border, color: theme.colors.text }]} value={query} />
          <View style={styles.filterRow}>
            <TextInput accessibilityLabel="Filter mood" onChangeText={setMood} placeholder="Mood" placeholderTextColor={theme.colors.mutedText} style={[styles.filterInput, { backgroundColor: theme.colors.background, borderColor: theme.colors.border, color: theme.colors.text }]} value={mood} />
            <TextInput accessibilityLabel="Filter tag" onChangeText={setTag} placeholder="Tag" placeholderTextColor={theme.colors.mutedText} style={[styles.filterInput, { backgroundColor: theme.colors.background, borderColor: theme.colors.border, color: theme.colors.text }]} value={tag} />
          </View>
        </View>
        {journal.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading dreams</Text> : null}
        {journal.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Journal could not be loaded.</Text> : null}
        {journal.data?.items.length === 0 ? <EmptyState /> : null}
        <View style={styles.list}>
          {journal.data?.items.map((item) => (
            <View key={item.id} style={[styles.card, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
              <Link href={`/journal/${item.id}`} asChild>
                <Pressable accessibilityRole="button" style={styles.cardLink} testID={`journal-item-${item.id}`}>
                  <View style={[styles.dateBadge, { backgroundColor: theme.colors.lavender }]}>
                    <Text style={[styles.dateBadgeText, { color: theme.colors.text }]}>{formatDreamDate(item.occurredAt ?? item.createdAt)}</Text>
                  </View>
                  <View style={styles.cardText}>
                    <Text style={[styles.cardTitle, { color: theme.colors.text }]}>{item.summary ?? "Dream interpretation"}</Text>
                    <Text style={[styles.body, { color: theme.colors.mutedText }]}>{[item.occurredAt, item.mood].filter(Boolean).join(" - ") || item.createdAt}</Text>
                  </View>
                </Pressable>
              </Link>
              <Pressable accessibilityRole="button" onPress={() => deleteDream.mutate(item.id)} style={styles.deleteButton} testID={`delete-dream-${item.id}`}>
                <Text style={[styles.deleteText, { color: theme.colors.warning }]}>Delete</Text>
              </Pressable>
            </View>
          ))}
        </View>
      </ScrollView>
    </AppShell>
  );
}

function EmptyState() {
  const theme = useTheme();
  return <View style={[styles.empty, { backgroundColor: theme.colors.lavender }]}><Text style={[styles.cardTitle, { color: theme.colors.text }]}>No dreams yet</Text><Text style={[styles.body, { color: theme.colors.mutedText }]}>Capture a dream to start building your private journal.</Text></View>;
}

function formatDreamDate(value: string) {
  const date = new Date(`${value.slice(0, 10)}T00:00:00Z`);
  return Number.isNaN(date.valueOf()) ? value.slice(0, 10) : new Intl.DateTimeFormat("en", { month: "short", day: "numeric" }).format(date);
}

const styles = StyleSheet.create({
  screen: { gap: 16, padding: 20, paddingBottom: 28 },
  hero: { borderRadius: 8, gap: 7, padding: 18 },
  title: { fontSize: 29, fontWeight: "700", lineHeight: 35 },
  subtitle: { fontSize: 15, lineHeight: 22 },
  body: { fontSize: 14, lineHeight: 20 },
  filters: { borderRadius: 8, borderWidth: 1, gap: 10, padding: 12 },
  filterRow: { flexDirection: "row", gap: 10 },
  searchInput: { borderRadius: 6, borderWidth: 1, fontSize: 15, minHeight: 46, paddingHorizontal: 12 },
  filterInput: { borderRadius: 6, borderWidth: 1, flex: 1, fontSize: 14, minHeight: 42, paddingHorizontal: 12 },
  list: { gap: 10 },
  card: { borderRadius: 8, borderWidth: 1, gap: 10, padding: 14 },
  cardLink: { alignItems: "flex-start", flexDirection: "row", gap: 12 },
  dateBadge: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 42, minWidth: 54, paddingHorizontal: 6 },
  dateBadgeText: { fontSize: 12, fontWeight: "800" },
  cardText: { flex: 1, gap: 4 },
  cardTitle: { fontSize: 16, fontWeight: "800", lineHeight: 22 },
  deleteButton: { alignSelf: "flex-end", minHeight: 28, justifyContent: "center" },
  deleteText: { fontSize: 13, fontWeight: "800" },
  empty: { borderRadius: 8, gap: 7, padding: 18 }
});
