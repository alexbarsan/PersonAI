import { useMutation, useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Image, Pressable, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { AdminDreamSearchItemResponse } from "@/api/dto";
import { ApiError } from "@/api/errors";
import { ResultSectionRenderer } from "@/features/dreams/ResultSectionRenderer";
import { useTheme } from "@/theme/ThemeProvider";

export function AdminDreamExplorer() {
  const api = useApiClient();
  const theme = useTheme();
  const [draft, setDraft] = useState("");
  const [query, setQuery] = useState("");
  const [selected, setSelected] = useState<AdminDreamSearchItemResponse | null>(null);
  const dreams = useQuery({ queryKey: ["admin-dreams", query], queryFn: () => api.searchAdminDreams(query) });
  const access = useMutation({ mutationFn: (id: string) => api.accessAdminDream(id) });
  const unauthorized = dreams.error instanceof ApiError && dreams.error.status === 403;
  const groups = useMemo(() => groupByOwnerEmail(dreams.data?.items ?? []), [dreams.data?.items]);

  return <View style={styles.container}>
    <View style={styles.searchRow}>
      <TextInput accessibilityLabel="Search all dreams" onChangeText={setDraft} onSubmitEditing={() => setQuery(draft.trim())} placeholder="Search email, text, tags, interpretation, or dream ID" placeholderTextColor={theme.colors.mutedText} style={[styles.searchInput, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border, color: theme.colors.text }]} value={draft} />
      <Pressable accessibilityRole="button" onPress={() => setQuery(draft.trim())} style={[styles.searchButton, { backgroundColor: theme.colors.primary }]}><Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>Search</Text></Pressable>
    </View>
    {unauthorized ? <Text style={[styles.error, { color: theme.colors.warning }]}>This account is not authorized to search private dreams.</Text> : null}
    {dreams.isError && !unauthorized ? <Text style={[styles.error, { color: theme.colors.warning }]}>Dream search could not be loaded.</Text> : null}
    {dreams.data ? <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{dreams.data.total} dream{dreams.data.total === 1 ? "" : "s"}</Text> : null}
    <View style={styles.results}>
      {groups.map((group) => <View key={group.email} style={styles.ownerGroup}>
        <View style={styles.ownerHeader}><Text selectable style={[styles.ownerEmail, { color: theme.colors.text }]}>{group.email}</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{group.dreams.length} dream{group.dreams.length === 1 ? "" : "s"} in these results</Text></View>
        {group.dreams.map((dream) => <DreamRow key={dream.id} dream={dream} onSelect={() => { setSelected(dream); access.reset(); }} />)}
      </View>)}
    </View>

    {selected && !access.data ? <View style={[styles.accessPanel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
      <View style={styles.resultHeader}><View style={styles.resultText}><Text style={[styles.summary, { color: theme.colors.text }]}>Open private dream</Text><Text selectable style={[styles.meta, { color: theme.colors.mutedText }]}>{selected.ownerEmail ?? "Email unavailable"} | {selected.id}</Text></View><Pressable accessibilityRole="button" onPress={() => setSelected(null)}><Text style={[styles.close, { color: theme.colors.mutedText }]}>Close</Text></Pressable></View>
      <Pressable accessibilityRole="button" disabled={access.isPending} onPress={() => access.mutate(selected.id)} style={[styles.accessButton, { backgroundColor: theme.colors.primary, opacity: access.isPending ? 0.5 : 1 }]}><Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{access.isPending ? "Opening" : "View original dream"}</Text></Pressable>
      {access.isError ? <Text style={[styles.error, { color: theme.colors.warning }]}>Dream details could not be opened.</Text> : null}
    </View> : null}

    {access.data ? <View style={styles.detail}>
      <View style={[styles.original, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}><View style={styles.resultHeader}><View style={styles.resultText}><Text style={[styles.detailTitle, { color: theme.colors.text }]}>Original dream</Text><Text selectable style={[styles.meta, { color: theme.colors.mutedText }]}>{access.data.ownerEmail ?? "Email unavailable"} | {formatDate(access.data.occurredAt ?? access.data.createdAt)}</Text></View><Pressable accessibilityRole="button" onPress={() => { access.reset(); setSelected(null); }}><Text style={[styles.close, { color: theme.colors.mutedText }]}>Close</Text></Pressable></View><Text selectable style={[styles.body, { color: theme.colors.text }]}>{access.data.text}</Text></View>
      {access.data.interpretation ? <View style={styles.interpretation}><Text style={[styles.detailTitle, { color: theme.colors.text }]}>Interpretation</Text><Text style={[styles.body, { color: theme.colors.text }]}>{access.data.interpretation.summary}</Text>{access.data.interpretation.sections.map((section, index) => <ResultSectionRenderer key={`${section.kind}-${index}`} section={section} />)}</View> : null}
      {access.data.deepInterpretation ? <View style={styles.interpretation}><Text style={[styles.detailTitle, { color: theme.colors.text }]}>Deep interpretation</Text><Text style={[styles.body, { color: theme.colors.text }]}>{access.data.deepInterpretation.summary}</Text>{access.data.deepInterpretation.sections.map((section, index) => <ResultSectionRenderer key={`deep-${section.kind}-${index}`} section={section} />)}</View> : null}
      {access.data.images.map((image) => <View key={image.id} style={styles.imageBlock}><View style={styles.resultHeader}><Text style={[styles.detailTitle, { color: theme.colors.text }]}>Generated image</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{image.style} | {image.status}</Text></View>{image.downloadUrl ? <Image accessibilityLabel="Generated dream" resizeMode="cover" source={{ uri: image.downloadUrl }} style={styles.image} /> : <Text style={[styles.meta, { color: theme.colors.mutedText }]}>Image is not available.</Text>}</View>)}
    </View> : null}
  </View>;
}

function DreamRow({ dream, onSelect }: { dream: AdminDreamSearchItemResponse; onSelect: () => void }) {
  const theme = useTheme();
  return <Pressable accessibilityRole="button" onPress={onSelect} style={[styles.result, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]} testID={`admin-dream-${dream.id}`}><View style={styles.resultHeader}><View style={styles.resultText}><Text numberOfLines={2} style={[styles.summary, { color: theme.colors.text }]}>{dream.summary ?? "Interpretation unavailable"}</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{dream.subjectPseudonym} | {formatDate(dream.occurredAt ?? dream.createdAt)}</Text></View><View style={[styles.imageBadge, { backgroundColor: dream.imageCount > 0 ? theme.colors.sage : theme.colors.softInk }]}><Text style={[styles.badgeText, { color: theme.colors.text }]}>{dream.imageCount} image{dream.imageCount === 1 ? "" : "s"}</Text></View></View>{dream.tags.length > 0 ? <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{dream.tags.join(" | ")}</Text> : null}</Pressable>;
}

function groupByOwnerEmail(dreams: AdminDreamSearchItemResponse[]) {
  const groups = new Map<string, AdminDreamSearchItemResponse[]>();
  for (const dream of dreams) {
    const email = dream.ownerEmail ?? "Email unavailable";
    groups.set(email, [...(groups.get(email) ?? []), dream]);
  }

  return [...groups.entries()]
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([email, groupedDreams]) => ({ email, dreams: groupedDreams }));
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(value));
}

const styles = StyleSheet.create({
  container: { gap: 14 },
  searchRow: { alignItems: "stretch", flexDirection: "row", gap: 8 },
  searchInput: { borderRadius: 6, borderWidth: 1, flex: 1, fontSize: 14, minHeight: 44, paddingHorizontal: 11 },
  searchButton: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 44, paddingHorizontal: 16 },
  buttonText: { fontSize: 13, fontWeight: "800" },
  meta: { fontSize: 12, lineHeight: 17 },
  error: { fontSize: 13, fontWeight: "700", lineHeight: 18 },
  results: { gap: 18 },
  ownerGroup: { gap: 9 },
  ownerHeader: { gap: 2, paddingHorizontal: 2 },
  ownerEmail: { fontSize: 15, fontWeight: "800", lineHeight: 20 },
  result: { borderRadius: 8, borderWidth: 1, gap: 8, padding: 13 },
  resultHeader: { alignItems: "flex-start", flexDirection: "row", gap: 10, justifyContent: "space-between" },
  resultText: { flex: 1, gap: 3, minWidth: 0 },
  summary: { fontSize: 14, fontWeight: "800", lineHeight: 20 },
  imageBadge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 5 },
  badgeText: { fontSize: 11, fontWeight: "700" },
  accessPanel: { borderRadius: 8, borderWidth: 1, gap: 11, padding: 14 },
  accessButton: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 42, paddingHorizontal: 14 },
  close: { fontSize: 13, fontWeight: "800", padding: 4 },
  detail: { gap: 18 },
  original: { borderRadius: 8, borderWidth: 1, gap: 12, padding: 15 },
  detailTitle: { fontSize: 17, fontWeight: "800", lineHeight: 22 },
  body: { fontSize: 14, lineHeight: 21 },
  interpretation: { gap: 12 },
  imageBlock: { gap: 10 },
  image: { aspectRatio: 1, borderRadius: 8, width: "100%" }
});
