import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
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
  const [reason, setReason] = useState("");
  const dreams = useQuery({ queryKey: ["admin-dreams", query], queryFn: () => api.searchAdminDreams(query) });
  const access = useMutation({ mutationFn: ({ id, purpose }: { id: string; purpose: string }) => api.accessAdminDream(id, purpose) });
  const unauthorized = dreams.error instanceof ApiError && dreams.error.status === 403;
  const validReason = reason.trim().length >= 10 && reason.trim().length <= 500;

  return <View style={styles.container}>
    <View style={styles.searchRow}>
      <TextInput accessibilityLabel="Search all dreams" onChangeText={setDraft} onSubmitEditing={() => setQuery(draft.trim())} placeholder="Search text, tags, interpretation, or dream ID" placeholderTextColor={theme.colors.mutedText} style={[styles.searchInput, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border, color: theme.colors.text }]} value={draft} />
      <Pressable accessibilityRole="button" onPress={() => setQuery(draft.trim())} style={[styles.searchButton, { backgroundColor: theme.colors.primary }]}><Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>Search</Text></Pressable>
    </View>
    {unauthorized ? <Text style={[styles.error, { color: theme.colors.warning }]}>This account is not authorized to search private dreams.</Text> : null}
    {dreams.isError && !unauthorized ? <Text style={[styles.error, { color: theme.colors.warning }]}>Dream search could not be loaded.</Text> : null}
    {dreams.data ? <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{dreams.data.total} dream{dreams.data.total === 1 ? "" : "s"}</Text> : null}
    <View style={styles.results}>
      {dreams.data?.items.map((dream) => <Pressable key={dream.id} accessibilityRole="button" onPress={() => { setSelected(dream); setReason(""); access.reset(); }} style={[styles.result, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]} testID={`admin-dream-${dream.id}`}><View style={styles.resultHeader}><View style={styles.resultText}><Text numberOfLines={2} style={[styles.summary, { color: theme.colors.text }]}>{dream.summary ?? "Interpretation unavailable"}</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{dream.subjectPseudonym} | {formatDate(dream.occurredAt ?? dream.createdAt)}</Text></View><View style={[styles.imageBadge, { backgroundColor: dream.imageCount > 0 ? theme.colors.sage : theme.colors.softInk }]}><Text style={[styles.badgeText, { color: theme.colors.text }]}>{dream.imageCount} image{dream.imageCount === 1 ? "" : "s"}</Text></View></View>{dream.tags.length > 0 ? <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{dream.tags.join(" | ")}</Text> : null}</Pressable>)}
    </View>

    {selected && !access.data ? <View style={[styles.accessPanel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
      <View style={styles.resultHeader}><View style={styles.resultText}><Text style={[styles.summary, { color: theme.colors.text }]}>Open private dream</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{selected.subjectPseudonym} | {selected.id}</Text></View><Pressable accessibilityRole="button" onPress={() => setSelected(null)}><Text style={[styles.close, { color: theme.colors.mutedText }]}>Close</Text></Pressable></View>
      <TextInput accessibilityLabel="Dream access purpose" multiline onChangeText={setReason} placeholder="Case-specific reason for access" placeholderTextColor={theme.colors.mutedText} style={[styles.reasonInput, { borderColor: theme.colors.border, color: theme.colors.text }]} value={reason} />
      <Pressable accessibilityRole="button" disabled={!validReason || access.isPending} onPress={() => access.mutate({ id: selected.id, purpose: reason.trim() })} style={[styles.accessButton, { backgroundColor: theme.colors.primary, opacity: validReason ? 1 : 0.5 }]}><Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{access.isPending ? "Opening" : "Open and audit"}</Text></Pressable>
      {access.isError ? <Text style={[styles.error, { color: theme.colors.warning }]}>Dream details could not be opened.</Text> : null}
    </View> : null}

    {access.data ? <View style={styles.detail}>
      <View style={[styles.original, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}><View style={styles.resultHeader}><View style={styles.resultText}><Text style={[styles.detailTitle, { color: theme.colors.text }]}>Original dream</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{access.data.subjectPseudonym} | {formatDate(access.data.occurredAt ?? access.data.createdAt)}</Text></View><Pressable accessibilityRole="button" onPress={() => { access.reset(); setSelected(null); }}><Text style={[styles.close, { color: theme.colors.mutedText }]}>Close</Text></Pressable></View><Text selectable style={[styles.body, { color: theme.colors.text }]}>{access.data.text}</Text></View>
      {access.data.interpretation ? <View style={styles.interpretation}><Text style={[styles.detailTitle, { color: theme.colors.text }]}>Interpretation</Text><Text style={[styles.body, { color: theme.colors.text }]}>{access.data.interpretation.summary}</Text>{access.data.interpretation.sections.map((section, index) => <ResultSectionRenderer key={`${section.kind}-${index}`} section={section} />)}</View> : null}
      {access.data.deepInterpretation ? <View style={styles.interpretation}><Text style={[styles.detailTitle, { color: theme.colors.text }]}>Deep interpretation</Text><Text style={[styles.body, { color: theme.colors.text }]}>{access.data.deepInterpretation.summary}</Text>{access.data.deepInterpretation.sections.map((section, index) => <ResultSectionRenderer key={`deep-${section.kind}-${index}`} section={section} />)}</View> : null}
      {access.data.images.map((image) => <View key={image.id} style={styles.imageBlock}><View style={styles.resultHeader}><Text style={[styles.detailTitle, { color: theme.colors.text }]}>Generated image</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{image.style} | {image.status}</Text></View>{image.downloadUrl ? <Image accessibilityLabel="Generated dream" resizeMode="cover" source={{ uri: image.downloadUrl }} style={styles.image} /> : <Text style={[styles.meta, { color: theme.colors.mutedText }]}>Image is not available.</Text>}</View>)}
    </View> : null}
  </View>;
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
  results: { gap: 9 },
  result: { borderRadius: 8, borderWidth: 1, gap: 8, padding: 13 },
  resultHeader: { alignItems: "flex-start", flexDirection: "row", gap: 10, justifyContent: "space-between" },
  resultText: { flex: 1, gap: 3, minWidth: 0 },
  summary: { fontSize: 14, fontWeight: "800", lineHeight: 20 },
  imageBadge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 5 },
  badgeText: { fontSize: 11, fontWeight: "700" },
  accessPanel: { borderRadius: 8, borderWidth: 1, gap: 11, padding: 14 },
  reasonInput: { borderRadius: 6, borderWidth: 1, fontSize: 14, lineHeight: 20, minHeight: 78, padding: 10, textAlignVertical: "top" },
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
