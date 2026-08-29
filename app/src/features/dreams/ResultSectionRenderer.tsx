import { StyleSheet, Text, View } from "react-native";

import { DreamSectionResponse } from "@/api/dto";
import { useTheme } from "@/theme/ThemeProvider";

export function ResultSectionRenderer({ section }: { section: DreamSectionResponse }) {
  const theme = useTheme();

  return (
    <View style={[styles.section, { borderColor: theme.colors.border }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>{section.title}</Text>
      {renderContent(section.kind, section.content)}
    </View>
  );
}

function renderContent(kind: string, content: unknown) {
  switch (kind) {
    case "symbols":
      return <SymbolList content={content} />;
    case "emotions":
      return <EmotionList content={content} />;
    case "entities":
      return <EntityList content={content} />;
    case "list":
      return <TextList content={content} />;
    case "text":
    default:
      return <Paragraph content={content} />;
  }
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

function SymbolList({ content }: { content: unknown }) {
  const theme = useTheme();
  const items = Array.isArray(content) ? content : [];
  return (
    <View style={styles.list}>
      {items.map((item, index) => {
        const record = asRecord(item);
        return (
          <View key={`${record.symbol ?? index}`} style={styles.item}>
            <Text style={[styles.itemTitle, { color: theme.colors.text }]}>{toText(record.title ?? record.symbol ?? "Symbol")}</Text>
            <Text style={[styles.body, { color: theme.colors.mutedText }]}>{toText(record.body ?? record.meaning ?? item)}</Text>
            {record.personalRelevance ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>{toText(record.personalRelevance)}</Text> : null}
          </View>
        );
      })}
    </View>
  );
}

function EmotionList({ content }: { content: unknown }) {
  const theme = useTheme();
  const items = Array.isArray(content) ? content : [];
  return (
    <View style={styles.list}>
      {items.map((item, index) => {
        const record = asRecord(item);
        const intensity = record.value ?? record.intensity;
        return (
          <View key={`${record.name ?? index}`} style={styles.item}>
            <Text style={[styles.itemTitle, { color: theme.colors.text }]}>{toText(record.title ?? record.name ?? "Emotion")}</Text>
            <Text style={[styles.body, { color: theme.colors.mutedText }]}>
              {typeof intensity === "number" ? `Intensity ${toText(intensity)}` : toText(record.body ?? record.evidence ?? item)}
            </Text>
            {record.evidence ? (
              <Text style={[styles.body, { color: theme.colors.mutedText }]}>{toText(record.evidence)}</Text>
            ) : null}
          </View>
        );
      })}
    </View>
  );
}

function EntityList({ content }: { content: unknown }) {
  const theme = useTheme();
  const items = Array.isArray(content) ? content : [];
  return (
    <View style={styles.list}>
      {items.map((item, index) => {
        const record = asRecord(item);
        return (
          <View key={`${toText(record.title ?? index)}-${index}`} style={styles.item}>
            <Text style={[styles.itemTitle, { color: theme.colors.text }]}>{toText(record.title ?? "Detail")}</Text>
            <Text style={[styles.body, { color: theme.colors.mutedText }]}>{toText(record.body ?? item)}</Text>
          </View>
        );
      })}
    </View>
  );
}

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function toText(value: unknown) {
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
    borderRadius: 8,
    borderWidth: 1,
    gap: 10,
    padding: 14
  },
  title: {
    fontSize: 18,
    fontWeight: "700"
  },
  body: {
    fontSize: 15,
    lineHeight: 22
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
  }
});
