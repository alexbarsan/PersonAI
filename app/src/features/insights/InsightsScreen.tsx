import { useQuery } from "@tanstack/react-query";
import { ScrollView, StyleSheet, Text, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ThemeInsightResponse } from "@/api/dto";
import { useTheme } from "@/theme/ThemeProvider";

export function InsightsScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const insights = useQuery({
    queryKey: ["insights"],
    queryFn: () => api.getInsights()
  });

  return (
    <ScrollView contentContainerStyle={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <View style={styles.header}>
        <Text style={[styles.title, { color: theme.colors.text }]}>Insights</Text>
        <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>Recurring patterns from your journal.</Text>
      </View>

      {insights.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading insights</Text> : null}
      {insights.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Insights could not be loaded.</Text> : null}
      {insights.data && insights.data.totalDreams === 0 ? (
        <View style={[styles.panel, { borderColor: theme.colors.border }]}>
          <Text style={[styles.panelTitle, { color: theme.colors.text }]}>No insights yet</Text>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>Interpret dreams to reveal recurring themes.</Text>
        </View>
      ) : null}

      {insights.data && insights.data.totalDreams > 0 ? (
        <>
          <View style={styles.stats}>
            <Stat label="Dreams" value={insights.data.totalDreams.toString()} />
            <Stat label="Streak" value={`${insights.data.currentStreakDays} days`} />
          </View>
          <View style={[styles.panel, { borderColor: theme.colors.border }]}>
            <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Recurring themes</Text>
            {insights.data.recurringThemes.length === 0 ? (
              <Text style={[styles.body, { color: theme.colors.mutedText }]}>No repeated themes found yet.</Text>
            ) : (
              insights.data.recurringThemes.map((theme) => <ThemeBar key={theme.name} theme={theme} />)
            )}
          </View>
        </>
      ) : null}
    </ScrollView>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  const theme = useTheme();
  return (
    <View style={[styles.stat, { borderColor: theme.colors.border, backgroundColor: theme.colors.surface }]}>
      <Text style={[styles.statValue, { color: theme.colors.text }]}>{value}</Text>
      <Text style={[styles.body, { color: theme.colors.mutedText }]}>{label}</Text>
    </View>
  );
}

function ThemeBar({ theme }: { theme: ThemeInsightResponse }) {
  const appTheme = useTheme();
  const width = Math.min(100, Math.max(16, theme.count * 24));

  return (
    <View style={styles.themeRow}>
      <View style={styles.themeHeader}>
        <Text style={[styles.bodyStrong, { color: appTheme.colors.text }]}>{theme.name}</Text>
        <Text style={[styles.body, { color: appTheme.colors.mutedText }]}>{theme.count}</Text>
      </View>
      <View style={[styles.track, { backgroundColor: appTheme.colors.border }]}>
        <View style={[styles.bar, { backgroundColor: appTheme.colors.primary, width: `${width}%` }]} />
      </View>
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
  bodyStrong: {
    fontSize: 15,
    fontWeight: "700",
    lineHeight: 22
  },
  stats: {
    flexDirection: "row",
    gap: 12
  },
  stat: {
    borderRadius: 8,
    borderWidth: 1,
    flex: 1,
    gap: 4,
    padding: 14
  },
  statValue: {
    fontSize: 24,
    fontWeight: "700"
  },
  panel: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 12,
    padding: 14
  },
  panelTitle: {
    fontSize: 18,
    fontWeight: "700"
  },
  themeRow: {
    gap: 6
  },
  themeHeader: {
    flexDirection: "row",
    justifyContent: "space-between"
  },
  track: {
    borderRadius: 8,
    height: 8,
    overflow: "hidden"
  },
  bar: {
    borderRadius: 8,
    height: 8
  }
});
