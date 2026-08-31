import { useQuery } from "@tanstack/react-query";
import { ScrollView, StyleSheet, Text, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { FactInsightGroupResponse, FactInsightResponse, ThemeInsightResponse, TimingPatternInsightResponse } from "@/api/dto";
import { AppShell, BrandMark } from "@/components/AppShell";
import { useTheme } from "@/theme/ThemeProvider";

export function InsightsScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const insights = useQuery({ queryKey: ["insights"], queryFn: () => api.getInsights() });

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen}>
        <BrandMark detail="A personal map of your subconscious, over time." />
        <View style={[styles.hero, { backgroundColor: theme.colors.lavender }]}>
          <Text style={[styles.title, { color: theme.colors.text }]}>Your dream map</Text>
          <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>Patterns observed across your own journal.</Text>
          {insights.data?.dateRange ? <Text style={[styles.coverage, { color: theme.colors.mutedText }]}>{formatDate(insights.data.dateRange.start)} to {formatDate(insights.data.dateRange.end)}</Text> : null}
        </View>
        {insights.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading insights</Text> : null}
        {insights.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Insights could not be loaded.</Text> : null}
        {insights.data && insights.data.totalDreams === 0 ? <EmptyState /> : null}
        {insights.data && insights.data.totalDreams > 0 ? <>
          <View style={styles.stats}>
            <Stat label="Dreams recorded" value={insights.data.totalDreams.toString()} color="sage" />
            <Stat label="Current streak" value={`${insights.data.currentStreakDays} days`} color="ink" />
          </View>
          {insights.data.factGroups.length > 0 ? <View style={styles.groups}>{insights.data.factGroups.map((group) => <FactGroup key={group.type} group={group} />)}</View> : <ThemePanel themes={insights.data.recurringThemes} />}
          {insights.data.monthlyDreamCounts.length > 0 ? <ActivityPanel counts={insights.data.monthlyDreamCounts} /> : null}
          {insights.data.timingPatterns.length > 0 ? <TimingPanel patterns={insights.data.timingPatterns} /> : null}
          <Text style={[styles.note, { color: theme.colors.mutedText }]}>Patterns are reflective observations, not predictions or diagnoses.</Text>
        </> : null}
      </ScrollView>
    </AppShell>
  );
}

function EmptyState() {
  const theme = useTheme();
  return <View style={[styles.panel, { backgroundColor: theme.colors.sage }]}><Text style={[styles.panelTitle, { color: theme.colors.text }]}>No insights yet</Text><Text style={[styles.body, { color: theme.colors.mutedText }]}>Interpret dreams to reveal recurring patterns.</Text></View>;
}

function Stat({ label, value, color }: { label: string; value: string; color: "sage" | "ink" }) {
  const theme = useTheme();
  const backgroundColor = color === "sage" ? theme.colors.sage : theme.colors.softInk;
  return <View style={[styles.stat, { backgroundColor }]}><Text style={[styles.statValue, { color: theme.colors.text }]}>{value}</Text><Text style={[styles.statLabel, { color: theme.colors.mutedText }]}>{label}</Text></View>;
}

function FactGroup({ group }: { group: FactInsightGroupResponse }) {
  const theme = useTheme();
  return <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}><Text style={[styles.panelTitle, { color: theme.colors.text }]}>{group.title}</Text>{group.facts.map((fact) => <FactRow key={fact.value} fact={fact} />)}</View>;
}

function FactRow({ fact }: { fact: FactInsightResponse }) {
  const theme = useTheme();
  return <View style={styles.fact}><View style={styles.factHeader}><Text style={[styles.factName, { color: theme.colors.text }]}>{fact.value}</Text><Text style={[styles.factPercent, { color: theme.colors.text }]}>{fact.percentageOfDreams}%</Text></View><View style={[styles.track, { backgroundColor: theme.colors.softInk }]}><View style={[styles.bar, { backgroundColor: theme.colors.primary, width: `${Math.max(8, fact.percentageOfDreams)}%` }]} /></View><Text style={[styles.factMeta, { color: theme.colors.mutedText }]}>{fact.count} {fact.count === 1 ? "dream" : "dreams"}{fact.averageScore === null ? "" : ` | average intensity ${Math.round(fact.averageScore * 100)}%`}</Text></View>;
}

function ThemePanel({ themes }: { themes: ThemeInsightResponse[] }) {
  const theme = useTheme();
  return <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}><Text style={[styles.panelTitle, { color: theme.colors.text }]}>Recurring themes</Text>{themes.length === 0 ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>No repeated themes found yet.</Text> : themes.map((item) => <View key={item.name} style={styles.themeRow}><Text style={[styles.factName, { color: theme.colors.text }]}>{item.name}</Text><Text style={[styles.factPercent, { color: theme.colors.text }]}>{item.count}</Text></View>)}</View>;
}

function ActivityPanel({ counts }: { counts: { month: string; count: number }[] }) {
  const theme = useTheme();
  const maximum = Math.max(...counts.map((count) => count.count));
  return <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}><Text style={[styles.panelTitle, { color: theme.colors.text }]}>Journal activity</Text>{counts.map((count) => <View key={count.month} style={styles.activityRow}><Text style={[styles.factMeta, { color: theme.colors.mutedText }]}>{formatMonth(count.month)}</Text><View style={[styles.track, { backgroundColor: theme.colors.softInk }]}><View style={[styles.bar, { backgroundColor: theme.colors.primary, width: `${Math.max(12, (count.count / maximum) * 100)}%` }]} /></View><Text style={[styles.factPercent, { color: theme.colors.text }]}>{count.count}</Text></View>)}</View>;
}

function TimingPanel({ patterns }: { patterns: TimingPatternInsightResponse[] }) {
  const theme = useTheme();
  return <View style={[styles.panel, { backgroundColor: theme.colors.sage }]}><Text style={[styles.panelTitle, { color: theme.colors.text }]}>Timing observations</Text>{patterns.map((pattern) => <Text key={`${pattern.type}-${pattern.value}`} style={[styles.body, { color: theme.colors.mutedText }]}>{pattern.value} appeared in {pattern.occurrences} dreams, with a weekday observation rate {pattern.weekdayToWeekendRatio}x the weekend rate.</Text>)}</View>;
}

function formatDate(value: string) { return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(`${value}T00:00:00Z`)); }
function formatMonth(value: string) { return new Intl.DateTimeFormat("en", { month: "short", year: "numeric" }).format(new Date(`${value}T00:00:00Z`)); }

const styles = StyleSheet.create({
  screen: { gap: 16, padding: 20, paddingBottom: 28 },
  hero: { borderRadius: 8, gap: 7, padding: 18 },
  title: { fontSize: 29, fontWeight: "700", lineHeight: 35 },
  subtitle: { fontSize: 15, lineHeight: 22 },
  coverage: { fontSize: 12, fontWeight: "700", lineHeight: 18, paddingTop: 3 },
  body: { fontSize: 14, lineHeight: 21 },
  note: { fontSize: 12, lineHeight: 18, paddingHorizontal: 4 },
  stats: { flexDirection: "row", gap: 12 },
  stat: { borderRadius: 8, flex: 1, gap: 5, minHeight: 104, padding: 16 },
  statValue: { fontSize: 24, fontWeight: "800", lineHeight: 29 },
  statLabel: { fontSize: 13, lineHeight: 18 },
  groups: { gap: 12 },
  panel: { borderRadius: 8, borderWidth: 1, gap: 12, padding: 16 },
  panelTitle: { fontSize: 17, fontWeight: "800", lineHeight: 23 },
  fact: { gap: 6 },
  factHeader: { alignItems: "center", flexDirection: "row", justifyContent: "space-between" },
  factName: { fontSize: 15, fontWeight: "800", lineHeight: 21 },
  factPercent: { fontSize: 14, fontWeight: "800", lineHeight: 20 },
  factMeta: { fontSize: 12, lineHeight: 17 },
  track: { borderRadius: 4, height: 7, overflow: "hidden" },
  bar: { borderRadius: 4, height: 7 },
  themeRow: { alignItems: "center", flexDirection: "row", justifyContent: "space-between" },
  activityRow: { alignItems: "center", flexDirection: "row", gap: 10 },
  error: { fontSize: 13, lineHeight: 18 }
});
