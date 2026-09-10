import { StyleSheet, Text, View } from "react-native";

import { DreamSafetyResponse } from "@/api/dto";
import { useTheme } from "@/theme/ThemeProvider";

export function SafetyCard({ safety }: { safety?: DreamSafetyResponse | null }) {
  const theme = useTheme();

  if (safety?.selfHarmRisk !== "elevated" && !safety?.isRestricted) {
    return null;
  }

  const supportFirst = safety.selfHarmRisk === "elevated";

  return (
    <View style={[styles.card, { backgroundColor: theme.colors.lavender, borderColor: theme.colors.warning }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>{supportFirst ? "Support first" : "Sensitive content care"}</Text>
      <Text style={[styles.body, { color: theme.colors.mutedText }]}>
        {supportFirst
          ? "This result is limited because the dream may include safety concerns. Consider reaching out to someone you trust or a qualified local support service."
          : "Some details are handled with extra care, so deeper analysis is not available for this dream. Your original reflection remains private and available to you."}
      </Text>
      {supportFirst && safety.notes ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>{safety.notes}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 8,
    padding: 14
  },
  title: {
    fontSize: 18,
    fontWeight: "700"
  },
  body: {
    fontSize: 15,
    lineHeight: 22
  }
});
