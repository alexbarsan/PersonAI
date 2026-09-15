import { useQuery } from "@tanstack/react-query";
import { router } from "expo-router";
import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, TextInput, View } from "react-native";
import ArrowRight from "lucide-react-native/icons/arrow-right";
import Plus from "lucide-react-native/icons/plus";
import Search from "lucide-react-native/icons/search";

import { useApiClient } from "@/api/apiContext";
import { useAuthStore } from "@/auth/authStore";
import { AppShell, BrandMark } from "@/components/AppShell";
import { Text } from "@/components/Text";
import { useTheme } from "@/theme/ThemeProvider";

const pageSize = 25;

export function JournalListScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const subject = useAuthStore(state => state.user?.subject);
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const filters = { query: query.trim(), page, pageSize };
  const journal = useQuery({
    queryKey: ["journal", subject, filters],
    queryFn: () => api.listDreams(filters)
  });
  const total = journal.data?.total ?? 0;
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, total);

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen} keyboardShouldPersistTaps="handled">
        <BrandMark detail="Your remembered places, people, and feelings." />
        <View style={[styles.hero, { backgroundColor: theme.colors.sage }]}>
          <Text style={[styles.title, { color: theme.colors.text }]}>Your dreams</Text>
          <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>A private record that grows more useful with time.</Text>
          <Pressable accessibilityRole="button" onPress={() => router.push("/dreams/capture")} style={[styles.newDream, { backgroundColor: theme.colors.primary }]}>
            <Plus size={18} color={theme.colors.primaryText} />
            <Text style={[styles.newDreamText, { color: theme.colors.primaryText }]}>Capture a dream</Text>
          </Pressable>
        </View>

        <View style={[styles.searchRow, { borderColor: theme.colors.border, backgroundColor: theme.colors.surface }]}>
          <Search size={19} color={theme.colors.mutedText} />
          <TextInput
            accessibilityLabel="Search dreams"
            onChangeText={(value) => { setQuery(value); setPage(1); }}
            placeholder="Search your journal"
            placeholderTextColor={theme.colors.mutedText}
            style={[styles.searchInput, { color: theme.colors.text }]}
            value={query}
          />
        </View>

        {journal.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading dreams</Text> : null}
        {journal.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Journal could not be loaded.</Text> : null}
        {journal.data?.items.length === 0 ? <EmptyState filtered={Boolean(query)} onClear={() => setQuery("")} /> : null}
        {journal.data && total > 0 ? <Text style={[styles.meta, { color: theme.colors.mutedText }]}>Showing {start}-{end} of {total} dreams</Text> : null}

        <View style={styles.list}>
          {journal.data?.items.map((item) => (
            <View key={item.id} style={[styles.card, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
              <Pressable accessibilityRole="button" onPress={() => router.push(`/journal/${item.id}`)} style={styles.cardLink} testID={`journal-item-${item.id}`}>
                <View style={[styles.dateBadge, { backgroundColor: theme.colors.lavender }]}>
                  <Text style={[styles.dateBadgeText, { color: theme.colors.text }]}>{formatDreamDate(item.occurredAt ?? item.createdAt)}</Text>
                </View>
                <View style={styles.cardText}>
                  <Text style={[styles.cardTitle, { color: theme.colors.text }]}>{item.title}</Text>
                  <Text numberOfLines={2} style={[styles.excerpt, { color: theme.colors.mutedText }]}>{item.excerpt}</Text>
                  {item.mood ? <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{item.mood}</Text> : null}
                </View>
                <ArrowRight size={18} color={theme.colors.mutedText} />
              </Pressable>
            </View>
          ))}
        </View>
        {journal.data && (page > 1 || journal.data.hasMore) ? <View style={styles.pagination}>
          <Pressable accessibilityRole="button" disabled={page === 1} onPress={() => setPage(current => Math.max(1, current - 1))} style={[styles.pageButton, { borderColor: theme.colors.border }, page === 1 && styles.disabled]}>
            <Text style={[styles.pageButtonText, { color: theme.colors.primary }]}>Previous</Text>
          </Pressable>
          <Pressable accessibilityRole="button" disabled={!journal.data.hasMore} onPress={() => setPage(current => current + 1)} style={[styles.pageButton, { borderColor: theme.colors.border }, !journal.data.hasMore && styles.disabled]}>
            <Text style={[styles.pageButtonText, { color: theme.colors.primary }]}>Next</Text>
          </Pressable>
        </View> : null}
      </ScrollView>
    </AppShell>
  );
}

function EmptyState({ filtered, onClear }: { filtered: boolean; onClear: () => void }) {
  const theme = useTheme();
  return <View style={[styles.empty, { backgroundColor: theme.colors.lavender }]}>
    <Text style={[styles.cardTitle, { color: theme.colors.text }]}>{filtered ? "No matching dreams" : "No dreams yet"}</Text>
    <Text style={[styles.body, { color: theme.colors.mutedText }]}>{filtered ? "Try another word, or return to your whole journal." : "Capture a dream to start building your private journal."}</Text>
    {filtered ? <Pressable accessibilityRole="button" onPress={onClear} style={styles.clearButton}><Text style={{ color: theme.colors.primary, fontWeight: "700" }}>Clear search</Text></Pressable> : null}
  </View>;
}

function formatDreamDate(value: string) {
  const date = new Date(`${value.slice(0, 10)}T00:00:00Z`);
  return Number.isNaN(date.valueOf()) ? value.slice(0, 10) : new Intl.DateTimeFormat("en", { month: "short", day: "numeric" }).format(date);
}

const styles = StyleSheet.create({
  screen: { gap: 16, padding: 20, paddingBottom: 28 },
  hero: { gap: 10, marginHorizontal: -20, padding: 24 },
  title: { fontSize: 29, fontWeight: "700", lineHeight: 35 },
  subtitle: { fontSize: 15, lineHeight: 22 },
  newDream: { alignItems: "center", alignSelf: "flex-start", borderRadius: 8, flexDirection: "row", gap: 8, justifyContent: "center", marginTop: 8, minHeight: 44, paddingHorizontal: 16 },
  newDreamText: { fontSize: 14, fontWeight: "700" },
  searchRow: { alignItems: "center", borderRadius: 8, borderWidth: 1, flexDirection: "row", gap: 10, paddingHorizontal: 12 },
  searchInput: { flex: 1, fontSize: 15, minHeight: 48, minWidth: 0 },
  list: { gap: 10 },
  card: { borderRadius: 8, borderWidth: 1, padding: 16 },
  cardLink: { alignItems: "flex-start", flexDirection: "row", gap: 12 },
  dateBadge: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 42, minWidth: 54, paddingHorizontal: 6 },
  dateBadgeText: { fontSize: 12, fontWeight: "800" },
  cardText: { flex: 1, gap: 4, minWidth: 0 },
  cardTitle: { fontSize: 16, fontWeight: "800", lineHeight: 22 },
  excerpt: { fontSize: 14, lineHeight: 20 },
  body: { fontSize: 14, lineHeight: 20 },
  meta: { fontSize: 12, lineHeight: 17 },
  empty: { borderRadius: 8, gap: 7, padding: 18 },
  clearButton: { justifyContent: "center", minHeight: 44 },
  pagination: { flexDirection: "row", gap: 10, justifyContent: "flex-end" },
  pageButton: { alignItems: "center", borderRadius: 6, borderWidth: 1, justifyContent: "center", minHeight: 40, minWidth: 90, paddingHorizontal: 12 },
  pageButtonText: { fontSize: 14, fontWeight: "700" },
  disabled: { opacity: 0.4 }
});
