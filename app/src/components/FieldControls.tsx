import { useState } from "react";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";

import { useTheme } from "@/theme/ThemeProvider";

export type ChoiceOption = {
  label: string;
  value: string;
};

type SharedFieldProps = {
  error?: string;
  hint?: string;
  label: string;
  testID: string;
};

export function ChoiceSet({
  error,
  hint,
  label,
  onChange,
  options,
  testID,
  value
}: SharedFieldProps & {
  onChange: (value: string) => void;
  options: ChoiceOption[];
  value: string;
}) {
  const theme = useTheme();

  return (
    <View style={styles.field} testID={testID}>
      <Text style={[styles.label, { color: theme.colors.text }]}>{label}</Text>
      {hint ? <Text style={[styles.hint, { color: theme.colors.mutedText }]}>{hint}</Text> : null}
      <View accessibilityRole="radiogroup" style={styles.optionGrid}>
        {options.map((option) => {
          const selected = option.value === value;
          return (
            <Pressable
              accessibilityLabel={`${label}: ${option.label}`}
              accessibilityRole="radio"
              accessibilityState={{ selected }}
              key={option.value}
              onPress={() => onChange(option.value)}
              style={[
                styles.option,
                {
                  backgroundColor: selected ? theme.colors.primary : theme.colors.surface,
                  borderColor: selected ? theme.colors.primary : theme.colors.border
                }
              ]}
              testID={`${testID}-${option.value}`}
            >
              <Text style={[styles.optionText, { color: selected ? theme.colors.primaryText : theme.colors.text }]}>{option.label}</Text>
            </Pressable>
          );
        })}
      </View>
      {error ? <Text style={[styles.error, { color: theme.colors.warning }]}>{error}</Text> : null}
    </View>
  );
}

export function FivePointScale({
  error,
  hint,
  label,
  onChange,
  testID,
  value
}: SharedFieldProps & {
  onChange: (value: string) => void;
  value: string;
}) {
  const theme = useTheme();

  return (
    <View style={styles.field} testID={testID}>
      <Text style={[styles.label, { color: theme.colors.text }]}>{label}</Text>
      {hint ? <Text style={[styles.hint, { color: theme.colors.mutedText }]}>{hint}</Text> : null}
      <View accessibilityRole="radiogroup" style={styles.scaleRow}>
        {["1", "2", "3", "4", "5"].map((step) => {
          const selected = step === value;
          return (
            <Pressable
              accessibilityLabel={`${label}: ${step} of 5`}
              accessibilityRole="radio"
              accessibilityState={{ selected }}
              key={step}
              onPress={() => onChange(step)}
              style={[
                styles.scaleStep,
                {
                  backgroundColor: selected ? theme.colors.primary : theme.colors.surface,
                  borderColor: selected ? theme.colors.primary : theme.colors.border
                }
              ]}
              testID={`${testID}-${step}`}
            >
              <Text style={[styles.optionText, { color: selected ? theme.colors.primaryText : theme.colors.text }]}>{step}</Text>
            </Pressable>
          );
        })}
      </View>
      <View style={styles.scaleLegend}>
        <Text style={[styles.hint, { color: theme.colors.mutedText }]}>Low</Text>
        <Text style={[styles.hint, { color: theme.colors.mutedText }]}>High</Text>
      </View>
      {error ? <Text style={[styles.error, { color: theme.colors.warning }]}>{error}</Text> : null}
    </View>
  );
}

export function TagEditor({
  error,
  hint,
  label,
  onChange,
  placeholder,
  testID,
  value
}: SharedFieldProps & {
  onChange: (value: string) => void;
  placeholder?: string;
  value: string;
}) {
  const theme = useTheme();
  const [draft, setDraft] = useState("");
  const tags = splitTags(value);

  const addTag = () => {
    const next = splitTags(draft);
    if (next.length === 0) {
      return;
    }

    onChange(joinTags([...tags, ...next]));
    setDraft("");
  };

  const removeTag = (tag: string) => onChange(joinTags(tags.filter((candidate) => candidate !== tag)));

  return (
    <View style={styles.field} testID={testID}>
      <Text style={[styles.label, { color: theme.colors.text }]}>{label}</Text>
      {hint ? <Text style={[styles.hint, { color: theme.colors.mutedText }]}>{hint}</Text> : null}
      {tags.length > 0 ? (
        <View style={styles.tagRow}>
          {tags.map((tag) => (
            <View key={tag} style={[styles.tag, { backgroundColor: theme.colors.lavender }]}>
              <Text numberOfLines={1} style={[styles.tagText, { color: theme.colors.text }]}>{tag}</Text>
              <Pressable accessibilityLabel={`Remove ${tag}`} onPress={() => removeTag(tag)} style={styles.tagRemove}>
                <Text style={[styles.tagRemoveText, { color: theme.colors.text }]}>x</Text>
              </Pressable>
            </View>
          ))}
        </View>
      ) : null}
      <View style={styles.tagInputRow}>
        <TextInput
          accessibilityLabel={label}
          onChangeText={setDraft}
          onSubmitEditing={addTag}
          placeholder={placeholder ?? `Add ${label.toLowerCase()}`}
          placeholderTextColor={theme.colors.mutedText}
          style={[styles.tagInput, { borderColor: theme.colors.border, color: theme.colors.text }]}
          testID={`${testID}-input`}
          value={draft}
        />
        <Pressable
          accessibilityLabel={`Add ${label}`}
          accessibilityRole="button"
          onPress={addTag}
          style={[styles.addButton, { backgroundColor: theme.colors.softInk, borderColor: theme.colors.border }]}
          testID={`${testID}-add`}
        >
          <Text style={[styles.addButtonText, { color: theme.colors.text }]}>+</Text>
        </Pressable>
      </View>
      {error ? <Text style={[styles.error, { color: theme.colors.warning }]}>{error}</Text> : null}
    </View>
  );
}

function splitTags(value: string) {
  return Array.from(new Set(value.split(",").map((item) => item.trim()).filter(Boolean)));
}

function joinTags(tags: string[]) {
  return tags.join(", ");
}

const styles = StyleSheet.create({
  field: { gap: 7 },
  label: { fontSize: 15, fontWeight: "700" },
  hint: { fontSize: 13, lineHeight: 18 },
  error: { fontSize: 13, lineHeight: 18 },
  optionGrid: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
  option: { alignItems: "center", borderRadius: 6, borderWidth: 1, justifyContent: "center", minHeight: 40, paddingHorizontal: 12 },
  optionText: { fontSize: 14, fontWeight: "700" },
  scaleRow: { flexDirection: "row", gap: 8 },
  scaleStep: { alignItems: "center", borderRadius: 6, borderWidth: 1, flex: 1, justifyContent: "center", minHeight: 44 },
  scaleLegend: { flexDirection: "row", justifyContent: "space-between" },
  tagRow: { flexDirection: "row", flexWrap: "wrap", gap: 6 },
  tag: { alignItems: "center", borderRadius: 6, flexDirection: "row", gap: 4, maxWidth: "100%", minHeight: 32, paddingLeft: 9, paddingRight: 5 },
  tagText: { flexShrink: 1, fontSize: 13, fontWeight: "700" },
  tagRemove: { alignItems: "center", height: 24, justifyContent: "center", width: 20 },
  tagRemoveText: { fontSize: 16, lineHeight: 18 },
  tagInputRow: { alignItems: "center", flexDirection: "row", gap: 8 },
  tagInput: { borderRadius: 6, borderWidth: 1, flex: 1, fontSize: 16, minHeight: 44, paddingHorizontal: 12 },
  addButton: { alignItems: "center", borderRadius: 6, borderWidth: 1, height: 44, justifyContent: "center", width: 44 },
  addButtonText: { fontSize: 24, lineHeight: 26 }
});
