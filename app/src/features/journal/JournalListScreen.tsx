import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "expo-router";
import { useState } from "react";
import {
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from "react-native";
import { Text } from "@/components/Text";
import ArrowRight from "lucide-react-native/icons/arrow-right";
import Plus from "lucide-react-native/icons/plus";
import Search from "lucide-react-native/icons/search";
import Trash2 from "lucide-react-native/icons/trash";

import { useApiClient } from "@/api/apiContext";
import { DreamJournalResponse } from "@/api/dto";
import { AppShell, BrandMark } from "@/components/AppShell";
import { useTheme } from "@/theme/ThemeProvider";
import { useAuthStore } from "@/auth/authStore";

export function JournalListScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const subject = useAuthStore(state => state.user?.subject);
  const [query, setQuery] = useState("");
  const [mood, setMood] = useState("");
  const [tag, setTag] = useState("");
  const filters = { query, mood, tag };
  const journalKey = ["journal", subject, filters];
  const journal = useQuery({
    queryKey: journalKey,
    queryFn: () => api.listDreams(filters),
  });
  const deleteDream = useMutation({
    mutationFn: (id: string) => api.deleteDream(id),
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: ["journal"] });
      const previous = queryClient.getQueryData<DreamJournalResponse>(journalKey);
      queryClient.setQueryData<DreamJournalResponse>(journalKey, {
        items: previous?.items.filter((item) => item.id !== id) ?? [],
      });
      return { previous };
    },
    onError: (_, __, context) => {
      if (context?.previous)
        queryClient.setQueryData(journalKey, context.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: ["journal"] }),
  });

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen}>
        <BrandMark detail="Your remembered places, people, and feelings." />
        <View style={[styles.hero, { backgroundColor: theme.colors.sage }]}>
          <Text style={[styles.title, { color: theme.colors.text }]}>
            Your dreams
          </Text>
          <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>
            A private record that grows more useful with time.
          </Text>
          <Link href="/dreams/capture" asChild>
            <Pressable
              accessibilityRole="button"
              style={{
                ...styles.newDream,
                backgroundColor: theme.colors.primary,
              }}
            >
              <Plus size={18} color="white" />
              <Text style={styles.newDreamText}>Capture a dream</Text>
            </Pressable>
          </Link>
        </View>
        <View
          style={[
            styles.filters,
            {
              backgroundColor: theme.colors.surface,
              borderColor: theme.colors.border,
            },
          ]}
        >
          <View
            style={[styles.searchRow, { borderColor: theme.colors.border }]}
          >
            <Search size={19} color={theme.colors.mutedText} />
            <TextInput
              accessibilityLabel="Search dreams"
              onChangeText={setQuery}
              placeholder="Search your journal"
              placeholderTextColor={theme.colors.mutedText}
              style={[styles.searchInput, { color: theme.colors.text }]}
              value={query}
            />
          </View>
          <View style={styles.filterRow}>
            <TextInput
              accessibilityLabel="Filter mood"
              onChangeText={setMood}
              placeholder="Mood"
              placeholderTextColor={theme.colors.mutedText}
              style={[
                styles.filterInput,
                {
                  backgroundColor: theme.colors.background,
                  borderColor: theme.colors.border,
                  color: theme.colors.text,
                },
              ]}
              value={mood}
            />
            <TextInput
              accessibilityLabel="Filter tag"
              onChangeText={setTag}
              placeholder="Tag"
              placeholderTextColor={theme.colors.mutedText}
              style={[
                styles.filterInput,
                {
                  backgroundColor: theme.colors.background,
                  borderColor: theme.colors.border,
                  color: theme.colors.text,
                },
              ]}
              value={tag}
            />
          </View>
        </View>
        {journal.isLoading ? (
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>
            Loading dreams
          </Text>
        ) : null}
        {journal.isError ? (
          <Text style={[styles.body, { color: theme.colors.warning }]}>
            Journal could not be loaded.
          </Text>
        ) : null}
        {journal.data?.items.length === 0 ? <EmptyState filtered={Boolean(query || mood || tag)} onClear={() => { setQuery(""); setMood(""); setTag(""); }} /> : null}
        {journal.data ? (
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>
            {journal.data.items.length}{" "}
            {journal.data.items.length === 1 ? "memory" : "memories"}
            {query || mood || tag ? " found" : " in your journal"}
          </Text>
        ) : null}
        {deleteDream.isError ? (
          <Text
            accessibilityRole="alert"
            style={{ color: theme.colors.warning }}
          >
            Dream could not be deleted. Please try again.
          </Text>
        ) : null}
        <View style={styles.list}>
          {journal.data?.items.map((item) => (
            <View
              key={item.id}
              style={[
                styles.card,
                {
                  backgroundColor: theme.colors.surface,
                  borderColor: theme.colors.border,
                },
              ]}
            >
              <Link href={`/journal/${item.id}`} asChild>
                <Pressable
                  accessibilityRole="button"
                  style={styles.cardLink}
                  testID={`journal-item-${item.id}`}
                >
                  <View
                    style={[
                      styles.dateBadge,
                      { backgroundColor: theme.colors.lavender },
                    ]}
                  >
                    <Text
                      style={[
                        styles.dateBadgeText,
                        { color: theme.colors.text },
                      ]}
                    >
                      {formatDreamDate(item.occurredAt ?? item.createdAt)}
                    </Text>
                  </View>
                  <View style={styles.cardText}>
                    <Text
                      style={[styles.cardTitle, { color: theme.colors.text }]}
                    >
                      {item.summary ?? "Dream interpretation"}
                    </Text>
                    <Text
                      style={[styles.body, { color: theme.colors.mutedText }]}
                    >
                      {[item.occurredAt, item.mood]
                        .filter(Boolean)
                        .join(" - ") || item.createdAt}
                    </Text>
                  </View>
                  <ArrowRight size={18} color={theme.colors.mutedText} />
                </Pressable>
              </Link>
              <Pressable
                accessibilityRole="button"
                accessibilityLabel="Delete dream"
                disabled={deleteDream.isPending}
                onPress={() => deleteDream.mutate(item.id)}
                style={styles.deleteButton}
                testID={`delete-dream-${item.id}`}
              >
                <Trash2 size={17} color={theme.colors.warning} />
                <Text
                  style={[styles.deleteText, { color: theme.colors.warning }]}
                >
                  Delete
                </Text>
              </Pressable>
            </View>
          ))}
        </View>
      </ScrollView>
    </AppShell>
  );
}

function EmptyState({ filtered, onClear }: { filtered: boolean; onClear: () => void }) {
  const theme = useTheme();
  return (
    <View style={[styles.empty, { backgroundColor: theme.colors.lavender }]}>
      <Text style={[styles.cardTitle, { color: theme.colors.text }]}>
        {filtered ? "No matching dreams" : "No dreams yet"}
      </Text>
      <Text style={[styles.body, { color: theme.colors.mutedText }]}>
        {filtered ? "Try another detail, or return to your whole journal." : "Capture a dream to start building your private journal."}
      </Text>
      {filtered ? <Pressable accessibilityRole="button" onPress={onClear} style={{ minHeight: 44, justifyContent: "center" }}><Text style={{ color: theme.colors.primary, fontWeight: "700" }}>Clear filters</Text></Pressable> : null}
    </View>
  );
}

function formatDreamDate(value: string) {
  const date = new Date(`${value.slice(0, 10)}T00:00:00Z`);
  return Number.isNaN(date.valueOf())
    ? value.slice(0, 10)
    : new Intl.DateTimeFormat("en", { month: "short", day: "numeric" }).format(
        date,
      );
}

const styles = StyleSheet.create({
  screen: { gap: 16, padding: 20, paddingBottom: 28 },
  hero: { gap: 10, padding: 24, marginHorizontal: -20 },
  newDream: {
    flexDirection: "row",
    gap: 8,
    alignItems: "center",
    alignSelf: "flex-start",
    minHeight: 44,
    borderRadius: 8,
    paddingHorizontal: 16,
    marginTop: 8,
  },
  newDreamText: { color: "white", fontWeight: "700", fontSize: 14 },
  title: { fontSize: 29, fontWeight: "700", lineHeight: 35 },
  subtitle: { fontSize: 15, lineHeight: 22 },
  body: { fontSize: 14, lineHeight: 20 },
  filters: { gap: 10, paddingVertical: 8, backgroundColor: "transparent" },
  searchRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    paddingHorizontal: 12,
    borderWidth: 1,
    borderRadius: 8,
  },
  filterRow: { flexDirection: "row", gap: 10 },
  searchInput: {
    flex: 1,
    minWidth: 0,
    fontSize: 15,
    minHeight: 48,
    fontFamily: "Nunito_400Regular",
  },
  filterInput: {
    borderRadius: 6,
    borderWidth: 1,
    flex: 1,
    fontSize: 14,
    minHeight: 42,
    paddingHorizontal: 12,
  },
  list: { gap: 10 },
  card: { borderRadius: 8, borderWidth: 1, gap: 10, padding: 20 },
  cardLink: { alignItems: "flex-start", flexDirection: "row", gap: 12 },
  dateBadge: {
    alignItems: "center",
    borderRadius: 6,
    justifyContent: "center",
    minHeight: 42,
    minWidth: 54,
    paddingHorizontal: 6,
  },
  dateBadgeText: { fontSize: 12, fontWeight: "800" },
  cardText: { flex: 1, gap: 4 },
  cardTitle: { fontSize: 16, fontWeight: "800", lineHeight: 22 },
  deleteButton: {
    alignSelf: "flex-end",
    minHeight: 44,
    justifyContent: "center",
    flexDirection: "row",
    alignItems: "center",
    gap: 6,
  },
  deleteText: { fontSize: 13, fontWeight: "800" },
  empty: { borderRadius: 8, gap: 7, padding: 18 },
});
