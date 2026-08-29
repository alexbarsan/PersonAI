import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "expo-router";
import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { DreamJournalResponse } from "@/api/dto";
import { useTheme } from "@/theme/ThemeProvider";

export function JournalListScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const [query, setQuery] = useState("");
  const [mood, setMood] = useState("");
  const [tag, setTag] = useState("");
  const filters = { query, mood, tag };
  const journal = useQuery({
    queryKey: ["journal", filters],
    queryFn: () => api.listDreams(filters)
  });
  const deleteDream = useMutation({
    mutationFn: (id: string) => api.deleteDream(id),
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: ["journal"] });
      const previous = queryClient.getQueryData<DreamJournalResponse>(["journal"]);
      queryClient.setQueryData<DreamJournalResponse>(["journal"], {
        items: previous?.items.filter((item) => item.id !== id) ?? []
      });
      return { previous };
    },
    onError: (_, __, context) => {
      if (context?.previous) {
        queryClient.setQueryData(["journal"], context.previous);
      }
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: ["journal"] })
  });

  return (
    <ScrollView contentContainerStyle={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <View style={styles.header}>
        <Text style={[styles.title, { color: theme.colors.text }]}>Journal</Text>
        <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>Review dreams you have interpreted.</Text>
      </View>

      <View style={[styles.filters, { borderColor: theme.colors.border }]}>
        <TextInput
          accessibilityLabel="Search dreams"
          onChangeText={setQuery}
          placeholder="Search dreams"
          placeholderTextColor={theme.colors.mutedText}
          style={[styles.input, { borderColor: theme.colors.border, color: theme.colors.text }]}
          value={query}
        />
        <View style={styles.filterRow}>
          <TextInput
            accessibilityLabel="Filter mood"
            onChangeText={setMood}
            placeholder="Mood"
            placeholderTextColor={theme.colors.mutedText}
            style={[styles.filterInput, { borderColor: theme.colors.border, color: theme.colors.text }]}
            value={mood}
          />
          <TextInput
            accessibilityLabel="Filter tag"
            onChangeText={setTag}
            placeholder="Tag"
            placeholderTextColor={theme.colors.mutedText}
            style={[styles.filterInput, { borderColor: theme.colors.border, color: theme.colors.text }]}
            value={tag}
          />
        </View>
      </View>

      {journal.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading dreams</Text> : null}
      {journal.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Journal could not be loaded.</Text> : null}
      {journal.data?.items.length === 0 ? (
        <EmptyState title="No dreams yet" body="Capture a dream to start building your private journal." />
      ) : null}

      <View style={styles.list}>
        {journal.data?.items.map((item) => (
          <View key={item.id} style={[styles.card, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
            <Link href={`/journal/${item.id}`} asChild>
              <Pressable accessibilityRole="button" style={styles.cardLink} testID={`journal-item-${item.id}`}>
                <Text style={[styles.cardTitle, { color: theme.colors.text }]}>{item.summary ?? "Dream interpretation"}</Text>
                <Text style={[styles.body, { color: theme.colors.mutedText }]}>
                  {[item.occurredAt, item.mood].filter(Boolean).join(" - ") || item.createdAt}
                </Text>
              </Pressable>
            </Link>
            <Pressable
              accessibilityRole="button"
              onPress={() => deleteDream.mutate(item.id)}
              style={styles.deleteButton}
              testID={`delete-dream-${item.id}`}
            >
              <Text style={[styles.deleteText, { color: theme.colors.warning }]}>Delete</Text>
            </Pressable>
          </View>
        ))}
      </View>
    </ScrollView>
  );
}

function EmptyState({ title, body }: { title: string; body: string }) {
  const theme = useTheme();
  return (
    <View style={[styles.empty, { borderColor: theme.colors.border }]}>
      <Text style={[styles.cardTitle, { color: theme.colors.text }]}>{title}</Text>
      <Text style={[styles.body, { color: theme.colors.mutedText }]}>{body}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    gap: 18,
    padding: 20,
    paddingBottom: 48
  },
  header: {
    gap: 8
  },
  title: {
    fontSize: 30,
    fontWeight: "700"
  },
  subtitle: {
    fontSize: 16,
    lineHeight: 23
  },
  body: {
    fontSize: 15,
    lineHeight: 22
  },
  list: {
    gap: 12
  },
  filters: {
    borderBottomWidth: 1,
    gap: 8,
    paddingBottom: 14
  },
  filterRow: {
    flexDirection: "row",
    gap: 8
  },
  input: {
    borderRadius: 8,
    borderWidth: 1,
    fontSize: 16,
    minHeight: 44,
    paddingHorizontal: 12
  },
  filterInput: {
    borderRadius: 8,
    borderWidth: 1,
    flex: 1,
    fontSize: 16,
    minHeight: 42,
    paddingHorizontal: 12
  },
  card: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 8,
    padding: 14
  },
  cardLink: {
    gap: 6
  },
  cardTitle: {
    fontSize: 17,
    fontWeight: "700",
    lineHeight: 23
  },
  deleteButton: {
    alignSelf: "flex-start",
    minHeight: 36,
    justifyContent: "center"
  },
  deleteText: {
    fontSize: 15,
    fontWeight: "700"
  },
  empty: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 8,
    padding: 16
  }
});
