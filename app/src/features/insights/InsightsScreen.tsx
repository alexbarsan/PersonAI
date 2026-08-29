import { useQuery } from "@tanstack/react-query";
import { ScrollView, StyleSheet, Text, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { FactInsightGroupResponse, FactInsightResponse, ThemeInsightResponse, TimingPatternInsightResponse } from "@/api/dto";
import { useTheme } from "@/theme/ThemeProvider";

export function InsightsScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const insights = useQuery({ queryKey: ["insights"], queryFn: () => api.getInsights() });

  return (
    <ScrollView contentContainerStyle={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <View style={styles.header}>
        <Text style={[styles.title, { color: theme.colors.text }]}>Your dream map</Text>
        <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>Patterns observed across your own journal.</Text>
        {insights.data?.dateRange ? (
          <Text style={[styles.coverage, { color: theme.colors.mutedText }]}>
            {formatDate(insights.data.dateRange.start)} to {formatDate(insights.data.dateRange.end)}
          </Text>
        ) : null}
      </View>

      {insights.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading insights</Text> : null}
      {insights.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Insights could not be loaded.</Text> : null}
      {insights.data && insights.data.totalDreams === 0 ? (
        <View style={[styles.panel, { borderColor: theme.colors.border }]}>
          <Text style={[styles.panelTitle, { color: theme.colors.text }]}>No insights yet</Text>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>Interpret dreams to reveal recurring patterns.</Text>
        </View>
      ) : null}

      {insights.data && insights.data.totalDreams > 0 ? (
        <>
          <View style={styles.stats}>
            <Stat label="Dreams" value={insights.data.totalDreams.toString()} />
            <Stat label="Streak" value={`${insights.data.currentStreakDays} days`} />
          </View>
          {insights.data.factGroups.length > 0 ? (
            <View style={styles.groups}>
              {insights.data.factGroups.map((group) => <FactGroup key={group.type} group={group} />)}
            </View>
          ) : (
            <View style={[styles.panel, { borderColor: theme.colors.border }]}>
              <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Recurring themes</Text>
              {insights.data.recurringThemes.length === 0 ? (
                <Text style={[styles.body, { color: theme.colors.mutedText }]}>No repeated themes found yet.</Text>
              ) : (
                insights.data.recurringThemes.map((theme) => <ThemeBar key={theme.name} theme={theme} />)
              )}
            </View>
          )}
          {insights.data.monthlyDreamCounts.length > 0 ? <ActivityPanel counts={insights.data.monthlyDreamCounts} /> : null}
          {insights.data.timingPatterns.length > 0 ? <TimingPanel patterns={insights.data.timingPatterns} /> : null}
          <Text style={[styles.note, { color: theme.colors.mutedText }]}>Patterns are reflective observations, not predictions or diagnoses.</Text>
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

function FactGroup({ group }: { group: FactInsightGroupResponse }) {
  const theme = useTheme();
  return (
    <View style={[styles.panel, { borderColor: theme.colors.border }]}>
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>{group.title}</Text>
      {group.facts.map((fact) => <FactRow key={fact.value} fact={fact} />)}
    </View>
  );
}

function FactRow({ fact }: { fact: FactInsightResponse }) {
  const theme = useTheme();
  return (
    <View style={styles.factRow}>
      <View style={styles.factText}>
        <Text style={[styles.bodyStrong, { color: theme.colors.text }]}>{fact.value}</Text>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>
          {fact.count} {fact.count === 1 ? "dream" : "dreams"} | {fact.percentageOfDreams}%
          {fact.averageScore === null ? "" : ` | average intensity ${Math.round(fact.averageScore * 100)}%`}
        </Text>
      </View>
      <View style={[styles.dot, { backgroundColor: theme.colors.primary }]} />
    </View>
  );
}

function ActivityPanel({ counts }: { counts: { month: string; count: number }[] }) {
  const theme = useTheme();
  const maximum = Math.max(...counts.map((count) => count.count));
  return (
    <View style={[styles.panel, { borderColor: theme.colors.border }]}>
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Journal activity</Text>
      {counts.map((count) => (
        <View key={count.month} style={styles.activityRow}>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>{formatMonth(count.month)}</Text>
          <View style={[styles.track, { backgroundColor: theme.colors.border }]}>
            <View style={[styles.bar, { backgroundColor: theme.colors.primary, width: `${Math.max(12, (count.count / maximum) * 100)}%` }]} />
          </View>
          <Text style={[styles.bodyStrong, { color: theme.colors.text }]}>{count.count}</Text>
        </View>
      ))}
    </View>
  );
}

function TimingPanel({ patterns }: { patterns: TimingPatternInsightResponse[] }) {
  const theme = useTheme();
  return (
    <View style={[styles.panel, { borderColor: theme.colors.border }]}>
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Timing observations</Text>
      {patterns.map((pattern) => (
        <Text key={`${pattern.type}-${pattern.value}`} style={[styles.body, { color: theme.colors.mutedText }]}>
          {pattern.value} appeared in {pattern.occurrences} dreams, with a weekday observation rate {pattern.weekdayToWeekendRatio}x the weekend rate.
        </Text>
      ))}
    </View>
  );
}

function ThemeBar({ theme }: { theme: ThemeInsightResponse }) {
  const appTheme = useTheme();
  return (
    <View style={styles.factRow}>
      <Text style={[styles.bodyStrong, { color: appTheme.colors.text }]}>{theme.name}</Text>
      <Text style={[styles.body, { color: appTheme.colors.mutedText }]}>{theme.count}</Text>
    </View>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(`${value}T00:00:00Z`));
}

function formatMonth(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", year: "numeric" }).format(new Date(`${value}T00:00:00Z`));
}

const styles = StyleSheet.create({
  screen: { gap: 18, padding: 20, paddingBottom: 48 },
  header: { gap: 8 },
  title: { fontSize: 30, fontWeight: "700" },
  subtitle: { fontSize: 16, lineHeight: 23 },
  coverage: { fontSize: 13, lineHeight: 18 },
  body: { fontSize: 15, lineHeight: 22 },
  bodyStrong: { fontSize: 15, fontWeight: "700", lineHeight: 22 },
  note: { fontSize: 13, lineHeight: 19 },
  stats: { flexDirection: "row", gap: 12 },
  stat: { borderRadius: 8, borderWidth: 1, flex: 1, gap: 4, padding: 14 },
  statValue: { fontSize: 24, fontWeight: "700" },
  groups: { gap: 14 },
  panel: { borderRadius: 8, borderWidth: 1, gap: 12, padding: 14 },
  panelTitle: { fontSize: 18, fontWeight: "700" },
  factRow: { alignItems: "center", flexDirection: "row", gap: 10, justifyContent: "space-between" },
  factText: { flex: 1, gap: 2 },
  dot: { borderRadius: 4, height: 8, width: 8 },
  activityRow: { alignItems: "center", flexDirection: "row", gap: 10 },
  track: { borderRadius: 8, flex: 1, height: 8, overflow: "hidden" },
  bar: { borderRadius: 8, height: 8 }
});
