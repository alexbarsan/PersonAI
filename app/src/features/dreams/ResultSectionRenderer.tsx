import { useState } from "react";
import { Pressable, StyleSheet, View } from "react-native";
import { Text } from "@/components/Text";

import { DreamSectionResponse } from "@/api/dto";
import { useTheme } from "@/theme/ThemeProvider";

export function ResultSectionRenderer({ section }: { section: DreamSectionResponse }) {
  const theme = useTheme();

  if (!isSectionVisible(section)) {
    return null;
  }

  return (
    <View style={[styles.section, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>{section.title}</Text>
      {renderContent(section)}
    </View>
  );
}

const hiddenSectionTitles = new Set([
  "themes",
  "scenarios",
  "alternative interpretation",
  "alternative interpretations"
]);

export function isSectionVisible(section: DreamSectionResponse) {
  return !hiddenSectionTitles.has(section.title.trim().toLowerCase()) && hasContent(section.content);
}

function hasContent(value: unknown): boolean {
  if (typeof value === "string") return value.trim().length > 0;
  if (typeof value === "number" || typeof value === "boolean") return true;
  if (Array.isArray(value)) return value.some(hasContent);
  if (value && typeof value === "object") return Object.values(value).some(hasContent);
  return false;
}

function renderContent(section: DreamSectionResponse) {
  if (section.title.trim().toLowerCase() === "objects") {
    return <ObjectStrip content={section.content} />;
  }

  switch (section.kind) {
    case "symbols":
      return <DetailStrip kind={section.kind} content={section.content} />;
    case "emotions":
      return <DetailStrip kind={section.kind} content={section.content} />;
    case "entities":
      return <DetailStrip kind={section.kind} content={section.content} />;
    case "list":
      return <TextList content={section.content} />;
    case "text":
    default:
      return <Paragraph content={section.content} />;
  }
}

function ObjectStrip({ content }: { content: unknown }) {
  const theme = useTheme();
  const items = Array.isArray(content) ? content : [content];
  const labels = items
    .map((item) => {
      const record = asRecord(item);
      return toText(record.title ?? record.name ?? record.object ?? item);
    })
    .filter(Boolean);

  if (!labels.length) return null;

  return <View style={styles.selectors} accessibilityLabel="Objects">
    {labels.map((label, index) => (
      <View key={`${label}-${index}`} accessibilityLabel={`Object: ${label}`} style={[styles.selector, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
        <Text style={[styles.itemTitle, { color: theme.colors.text }]}>{label}</Text>
      </View>
    ))}
  </View>;
}

function DetailStrip({ kind, content }: { kind: string; content: unknown }) {
  const theme = useTheme();
  const [selected, setSelected] = useState(0);
  const items = Array.isArray(content) ? content : [];
  if (!items.length) return null;
  const activeIndex = Math.min(selected, items.length - 1);
  const active = asRecord(items[activeIndex]);
  const label = (item: unknown) => {
    const record = asRecord(item);
    return toText(record.title ?? record.symbol ?? record.name ?? "Detail");
  };
  const description = toText(active.body ?? active.meaning ?? active.evidence);
  const intensity = active.value ?? active.intensity;
  return <View style={styles.detailStrip}>
    <View style={styles.selectors} accessibilityRole="tablist">
      {items.map((item, index) => <Pressable key={index} aria-selected={index === activeIndex} accessibilityLabel={`${kind}: ${label(item)}`} accessibilityRole="tab"
        accessibilityState={{ selected: index === activeIndex }} onPress={() => setSelected(index)}
        style={[styles.selector, { borderColor: index === activeIndex ? theme.colors.primary : theme.colors.border,
          backgroundColor: index === activeIndex ? theme.colors.primary : theme.colors.surface }]}>
        <Text style={[styles.itemTitle, { color: index === activeIndex ? theme.colors.primaryText : theme.colors.text }]}>{label(item)}</Text>
      </Pressable>)}
    </View>
    <View style={[styles.detail, { borderColor: theme.colors.border }]}>
      {description ? <Paragraph content={description} /> : null}
      {active.personalRelevance ? <Paragraph content={active.personalRelevance} /> : null}
      {active.evidence && toText(active.evidence) !== description ? <Paragraph content={active.evidence} /> : null}
      {kind === "emotions" && typeof intensity === "number" ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Intensity {intensity}</Text> : null}
    </View>
  </View>;
}

function Paragraph({ content }: { content: unknown }) {
  const theme = useTheme();
  return <Text style={[styles.body, { color: theme.colors.mutedText }]}>{toText(content)}</Text>;
}

function TextList({ content }: { content: unknown }) {
  const theme = useTheme();
  const items = Array.isArray(content) ? content : [content];
  return (
    <View style={styles.list}>
      {items.map((item, index) => (
        <Text key={`${toText(item)}-${index}`} style={[styles.body, { color: theme.colors.mutedText }]}>
          {toText(item)}
        </Text>
      ))}
    </View>
  );
}


function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function toText(value: unknown): string {
  if (typeof value === "string") {
    return value;
  }

  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }

  if (value === null || value === undefined) {
    return "";
  }

  if (Array.isArray(value)) {
    return value.map(toText).filter(Boolean).join("\n");
  }

  return JSON.stringify(value);
}

const styles = StyleSheet.create({
  section: {
    borderTopWidth: 1,
    gap: 16,
    paddingVertical: 24
  },
  title: {
    fontSize: 18,
    fontWeight: "700"
  },
  body: {
    fontSize: 16,
    lineHeight: 26,
    maxWidth: 720
  },
  list: {
    gap: 10
  },
  item: {
    gap: 4
  },
  itemTitle: {
    fontSize: 15,
    fontWeight: "700"
  },
  detailStrip: { gap: 16 },
  selectors: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
  selector: { minHeight: 44, maxWidth: "100%", justifyContent: "center", paddingHorizontal: 16, paddingVertical: 10, borderRadius: 8, borderWidth: 1 },
  detail: { borderLeftWidth: 2, paddingLeft: 16, gap: 8 }
});
