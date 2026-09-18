import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import Svg, { Circle, G } from "react-native-svg";
import { useRouter } from "expo-router";
import { Text } from "@/components/Text";

import { useApiClient } from "@/api/apiContext";
import {
  FactInsightGroupResponse,
  FactInsightResponse,
  DreamObservationResponse,
  RelationshipInsightResponse,
  RelationshipReadinessResponse,
  ThemeInsightResponse,
  TimingPatternInsightResponse,
} from "@/api/dto";
import { AppShell, BrandMark } from "@/components/AppShell";
import { useTheme } from "@/theme/ThemeProvider";

const chartColors = ["#4f8b73", "#8e78b8", "#cf765b", "#d3a63b", "#5f84ad", "#a86f86", "#72928b", "#977352"];

export function InsightsScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const [groupFilter, setGroupFilter] = useState("all");
  const [selectedObservation, setSelectedObservation] = useState<{
    type: string;
    value: string;
  } | null>(null);
  const insights = useQuery({
    queryKey: ["insights"],
    queryFn: () => api.getInsights(),
  });
  const observation = useQuery({
    queryKey: ["dream-observation", selectedObservation?.type, selectedObservation?.value],
    queryFn: () => api.getDreamObservation(selectedObservation!.type, selectedObservation!.value),
    enabled: selectedObservation !== null,
  });

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen}>
        <BrandMark detail="A personal map of your subconscious, over time." />
        <View style={[styles.hero, { backgroundColor: theme.colors.lavender }]}>
          <Text accessibilityRole="header" style={[styles.title, { color: theme.colors.text }]}>
            Your dream map
          </Text>
          <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>
            Patterns observed across your own journal.
          </Text>
          {insights.data?.dateRange ? (
            <Text style={[styles.coverage, { color: theme.colors.mutedText }]}>
              {formatDate(insights.data.dateRange.start)} to{" "}
              {formatDate(insights.data.dateRange.end)}
            </Text>
          ) : null}
        </View>
        {insights.isLoading ? (
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>
            Loading insights
          </Text>
        ) : null}
        {insights.isError ? (
          <Text style={[styles.body, { color: theme.colors.warning }]}>
            Insights could not be loaded.
          </Text>
        ) : null}
        {insights.data && insights.data.totalDreams === 0 ? (
          <EmptyState />
        ) : null}
        {insights.data && insights.data.totalDreams > 0 ? (
          <>
            <View style={styles.stats}>
              <Stat
                label="Dreams recorded"
                value={insights.data.totalDreams.toString()}
                color="sage"
              />
              <Stat
                label="Current streak"
                value={`${insights.data.currentStreakDays} ${insights.data.currentStreakDays === 1 ? "day" : "days"}`}
                color="ink"
              />
            </View>
            {insights.data.factGroups.length > 0 ? (
              <>
                <View accessibilityRole="tablist" style={styles.tabs}>
                  {[
                    { type: "all", title: "Everything" },
                    ...insights.data.factGroups,
                  ].map((group) => (
                    <Pressable
                      key={group.type}
                      aria-selected={groupFilter === group.type}
                      accessibilityRole="tab"
                      accessibilityState={{
                        selected: groupFilter === group.type,
                      }}
                      onPress={() => setGroupFilter(group.type)}
                      style={[
                        styles.tab,
                        {
                          borderColor: theme.colors.border,
                          backgroundColor:
                            groupFilter === group.type
                              ? theme.colors.sage
                              : theme.colors.surface,
                        },
                      ]}
                    >
                      <Text
                        style={[styles.tabText, { color: theme.colors.text }]}
                      >
                        {group.title}
                      </Text>
                    </Pressable>
                  ))}
                </View>
                <View style={styles.groups}>
                  {insights.data.factGroups
                    .filter(
                      (group) =>
                        groupFilter === "all" || group.type === groupFilter,
                    )
                    .map((group) => (
                      <FactGroup
                        key={group.type}
                        group={group}
                        onSelect={(fact) => setSelectedObservation({ type: group.type, value: fact.value })}
                      />
                    ))}
                </View>
                {selectedObservation ? (
                  <ObservationPanel
                    observation={observation.data}
                    isLoading={observation.isLoading}
                    isError={observation.isError}
                  />
                ) : null}
              </>
            ) : (
              <ThemePanel themes={insights.data.recurringThemes} />
            )}
            {insights.data.monthlyDreamCounts.length > 0 ? (
              <ActivityPanel counts={insights.data.monthlyDreamCounts} />
            ) : null}
            {insights.data.timingPatterns.length > 0 ? (
              <TimingPanel patterns={insights.data.timingPatterns} />
            ) : null}
            <RelationshipPanel
              relationships={insights.data.relationships}
              readiness={insights.data.relationshipReadiness}
            />
            <Text style={[styles.note, { color: theme.colors.mutedText }]}>
              Patterns are reflective observations, not predictions or
              diagnoses.
            </Text>
          </>
        ) : null}
      </ScrollView>
    </AppShell>
  );
}

function EmptyState() {
  const theme = useTheme();
  return (
    <View style={[styles.panel, { backgroundColor: theme.colors.sage }]}>
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>
        No insights yet
      </Text>
      <Text style={[styles.body, { color: theme.colors.mutedText }]}>
        Interpret dreams to reveal recurring patterns.
      </Text>
    </View>
  );
}

function Stat({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: "sage" | "ink";
}) {
  const theme = useTheme();
  const backgroundColor =
    color === "sage" ? theme.colors.sage : theme.colors.softInk;
  return (
    <View style={[styles.stat, { backgroundColor }]}>
      <Text style={[styles.statValue, { color: theme.colors.text }]}>
        {value}
      </Text>
      <Text style={[styles.statLabel, { color: theme.colors.mutedText }]}>
        {label}
      </Text>
    </View>
  );
}

function FactGroup({
  group,
  onSelect,
}: {
  group: FactInsightGroupResponse;
  onSelect: (fact: FactInsightResponse) => void;
}) {
  const theme = useTheme();
  return (
    <View
      style={[
        styles.panel,
        {
          backgroundColor: theme.colors.surface,
          borderColor: theme.colors.border,
        },
      ]}
    >
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>
        {group.title}
      </Text>
      <PieChart
        accessibilityLabel={`${group.title} distribution`}
        data={group.facts.map((fact, index) => ({
          label: fact.value,
          value: fact.count,
          color: chartColors[index % chartColors.length],
          detail: `${fact.count} ${fact.count === 1 ? "dream" : "dreams"}; ${fact.percentageOfDreams}% of your journal`,
          onPress: () => onSelect(fact),
        }))}
      />
    </View>
  );
}

function ObservationPanel({
  observation,
  isLoading,
  isError,
}: {
  observation?: DreamObservationResponse;
  isLoading: boolean;
  isError: boolean;
}) {
  const theme = useTheme();
  const router = useRouter();
  return (
    <View style={[styles.panel, { backgroundColor: theme.colors.softInk, borderColor: theme.colors.border }]}>
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Observed in your journal</Text>
      {isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading contributing dreams</Text> : null}
      {isError ? <Text style={[styles.error, { color: theme.colors.warning }]}>This observation could not be loaded.</Text> : null}
      {observation ? (
        <>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>
            {observation.value} was extracted from {observation.totalDreams} {observation.totalDreams === 1 ? "dream" : "dreams"}
            {observation.averageExtractionConfidence === null ? "." : ` with ${Math.round(observation.averageExtractionConfidence * 100)}% average extraction confidence.`}
          </Text>
          <Text style={[styles.provenance, { color: theme.colors.mutedText }]}>
            Sources: {observation.sourceFields.join(", ")}
          </Text>
          {observation.evidence.map((item) => (
            <Pressable
              key={item.dreamId}
              accessibilityRole="link"
              onPress={() => router.push(`/dreams/${item.dreamId}`)}
              style={[styles.evidence, { borderColor: theme.colors.border }]}
            >
              <Text style={[styles.factName, { color: theme.colors.text }]}>{item.title}</Text>
              <Text style={[styles.factMeta, { color: theme.colors.mutedText }]}>
                {formatDate(item.observedAt)} | {item.sourceField} | schema {item.sourceSchemaVersion}
              </Text>
            </Pressable>
          ))}
        </>
      ) : null}
    </View>
  );
}

function ThemePanel({ themes }: { themes: ThemeInsightResponse[] }) {
  const theme = useTheme();
  return (
    <View
      style={[
        styles.panel,
        {
          backgroundColor: theme.colors.surface,
          borderColor: theme.colors.border,
        },
      ]}
    >
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>
        Recurring themes
      </Text>
      {themes.length === 0 ? (
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>
          No repeated themes found yet.
        </Text>
      ) : (
        <PieChart
          accessibilityLabel="Recurring themes distribution"
          data={themes.map((item, index) => ({
            label: item.name,
            value: item.count,
            color: chartColors[index % chartColors.length],
            detail: `${item.count} ${item.count === 1 ? "dream" : "dreams"}`,
          }))}
        />
      )}
    </View>
  );
}

function ActivityPanel({
  counts,
}: {
  counts: { month: string; count: number }[];
}) {
  const theme = useTheme();
  const maximum = Math.max(...counts.map((count) => count.count));
  return (
    <View
      style={[
        styles.panel,
        {
          backgroundColor: theme.colors.surface,
          borderColor: theme.colors.border,
        },
      ]}
    >
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>
        Journal activity
      </Text>
      {counts.map((count) => (
        <View key={count.month} style={styles.activityRow}>
          <Text
            style={[
              styles.factMeta,
              { color: theme.colors.mutedText, width: 76 },
            ]}
          >
            {formatMonth(count.month)}
          </Text>
          <View
            style={[
              styles.track,
              { backgroundColor: theme.colors.sage, flex: 1 },
            ]}
          >
            <View
              style={[
                styles.bar,
                {
                  backgroundColor: theme.colors.primary,
                  width: `${maximum > 0 ? (count.count / maximum) * 100 : 0}%`,
                },
              ]}
            />
          </View>
          <Text style={[styles.factPercent, { color: theme.colors.text }]}>
            {count.count}
          </Text>
        </View>
      ))}
    </View>
  );
}

function TimingPanel({
  patterns,
}: {
  patterns: TimingPatternInsightResponse[];
}) {
  const theme = useTheme();
  return (
    <View style={[styles.panel, { backgroundColor: theme.colors.sage }]}>
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>
        Timing observations
      </Text>
      {patterns.map((pattern) => (
        <View
          key={`${pattern.type}-${pattern.value}`}
          style={[styles.timingPattern, { borderColor: theme.colors.border }]}
        >
          <Text style={[styles.factName, { color: theme.colors.text }]}>{pattern.value}</Text>
          <PieChart
            accessibilityLabel={`${pattern.value} weekday and weekend distribution`}
            compact
            data={[
              { label: "Weekdays", value: pattern.weekdayDreams, color: chartColors[0], detail: `${pattern.weekdayDreams} dreams` },
              { label: "Weekends", value: pattern.weekendDreams, color: chartColors[1], detail: `${pattern.weekendDreams} dreams` },
            ]}
          />
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>Observed {pattern.weekdayToWeekendRatio}x as often on weekdays after accounting for the number of weekday and weekend days.</Text>
        </View>
      ))}
    </View>
  );
}

function RelationshipPanel({
  relationships,
  readiness,
}: {
  relationships: RelationshipInsightResponse[];
  readiness: RelationshipReadinessResponse;
}) {
  const theme = useTheme();
  const router = useRouter();
  const needsMoreDreams = readiness.completedDreams < readiness.minimumCompletedDreams;
  return (
    <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Patterns that appear together</Text>
      <Text style={[styles.body, { color: theme.colors.mutedText }]}>These are co-occurrences in your journal, not explanations or causes.</Text>
      {needsMoreDreams ? (
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>
          Connection patterns can be checked after {readiness.minimumCompletedDreams} completed dreams. You have {readiness.completedDreams}.
        </Text>
      ) : null}
      {!needsMoreDreams && relationships.length === 0 ? (
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>
          No connection patterns meet the current evidence threshold yet.
        </Text>
      ) : null}
      {relationships.map((relationship) => (
        <View key={`${relationship.firstType}-${relationship.firstValue}-${relationship.secondType}-${relationship.secondValue}`} style={[styles.relationship, { borderColor: theme.colors.border }]}>
          <Text style={[styles.factName, { color: theme.colors.text }]}>{relationship.firstValue} + {relationship.secondValue}</Text>
          <PieChart
            accessibilityLabel={`${relationship.firstValue} and ${relationship.secondValue} co-occurrence`}
            compact
            data={[
              { label: "Together", value: relationship.sharedDreams, color: chartColors[0], detail: `${relationship.sharedDreams} dreams` },
              { label: "Apart", value: Math.max(0, Math.min(relationship.firstDreams, relationship.secondDreams) - relationship.sharedDreams), color: "#d9e5df", detail: `${Math.max(0, Math.min(relationship.firstDreams, relationship.secondDreams) - relationship.sharedDreams)} dreams` },
            ]}
          />
          <Text style={[styles.factMeta, { color: theme.colors.mutedText }]}>
            Together in {relationship.sharedDreams} {relationship.sharedDreams === 1 ? "dream" : "dreams"}; {relationship.sharedOfSmallerPatternPercent}% of the less frequent pattern's observations, {relationship.relativeLift}x above its baseline rate.
          </Text>
          {relationship.evidence.map((dream) => (
            <Pressable key={dream.dreamId} accessibilityRole="link" accessibilityLabel={`Open ${dream.title}`} onPress={() => router.push(`/dreams/${dream.dreamId}`)} style={styles.relationshipEvidence}>
              <Text style={[styles.provenance, { color: theme.colors.primary }]}>{dream.title}</Text>
            </Pressable>
          ))}
        </View>
      ))}
    </View>
  );
}

type PieDatum = {
  label: string;
  value: number;
  color: string;
  detail?: string;
  onPress?: () => void;
};

function PieChart({ data, accessibilityLabel, compact = false }: { data: PieDatum[]; accessibilityLabel: string; compact?: boolean }) {
  const theme = useTheme();
  const positiveData = data.filter((item) => item.value > 0);
  const total = positiveData.reduce((sum, item) => sum + item.value, 0);
  const size = compact ? 112 : 144;
  const center = size / 2;
  const radius = compact ? 38 : 50;
  const strokeWidth = compact ? 19 : 24;
  const circumference = 2 * Math.PI * radius;
  let offset = 0;

  if (total === 0) {
    return <Text style={[styles.body, { color: theme.colors.mutedText }]}>Not enough data to chart yet.</Text>;
  }

  return (
    <View style={[styles.pieLayout, compact && styles.pieLayoutCompact]}>
      <Svg accessibilityLabel={accessibilityLabel} accessibilityRole="image" height={size} width={size}>
        <Circle cx={center} cy={center} fill="none" r={radius} stroke={theme.colors.background} strokeWidth={strokeWidth} />
        <G rotation="-90" origin={`${center}, ${center}`}>
          {positiveData.map((item) => {
            const length = (item.value / total) * circumference;
            const dashOffset = -offset;
            offset += length;
            return (
              <Circle
                key={item.label}
                cx={center}
                cy={center}
                fill="none"
                r={radius}
                stroke={item.color}
                strokeDasharray={`${length} ${circumference - length}`}
                strokeDashoffset={dashOffset}
                strokeWidth={strokeWidth}
              />
            );
          })}
        </G>
      </Svg>
      <View style={styles.pieLegend}>
        {data.map((item) => {
          const content = (
            <>
              <View style={[styles.legendDot, { backgroundColor: item.color }]} />
              <View style={styles.legendCopy}>
                <Text style={[styles.legendLabel, { color: theme.colors.text }]}>{item.label}</Text>
                <Text style={[styles.factMeta, { color: theme.colors.mutedText }]}>{item.detail ?? `${Math.round((item.value / total) * 100)}%`}</Text>
              </View>
            </>
          );
          return item.onPress ? (
            <Pressable key={item.label} accessibilityLabel={`Show journal evidence for ${item.label}`} accessibilityRole="button" onPress={item.onPress} style={styles.legendRow}>{content}</Pressable>
          ) : (
            <View key={item.label} style={styles.legendRow}>{content}</View>
          );
        })}
      </View>
    </View>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00Z`));
}
function formatMonth(value: string) {
  return new Intl.DateTimeFormat("en", {
    month: "short",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00Z`));
}

const styles = StyleSheet.create({
  screen: { gap: 16, padding: 20, paddingBottom: 28 },
  hero: { gap: 10, padding: 24, marginHorizontal: -20 },
  title: { fontSize: 29, fontWeight: "700", lineHeight: 35 },
  subtitle: { fontSize: 15, lineHeight: 22 },
  coverage: { fontSize: 12, fontWeight: "700", lineHeight: 18, paddingTop: 3 },
  body: { fontSize: 14, lineHeight: 21 },
  note: { fontSize: 12, lineHeight: 18, paddingHorizontal: 4 },
  stats: { flexDirection: "row", gap: 12 },
  stat: { borderRadius: 8, flex: 1, gap: 5, minHeight: 104, padding: 16 },
  statValue: { fontSize: 24, fontWeight: "800", lineHeight: 29 },
  statLabel: { fontSize: 13, lineHeight: 18 },
  groups: { gap: 16, flexDirection: "row", flexWrap: "wrap" },
  tabs: { flexDirection: "row", flexWrap: "wrap", gap: 8, paddingVertical: 8 },
  tab: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: 14,
    minHeight: 44,
    justifyContent: "center",
  },
  tabText: { fontSize: 13, fontWeight: "700" },
  panel: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 18,
    padding: 22,
    flexGrow: 1,
    flexBasis: "auto",
    minWidth: 0,
  },
  panelTitle: { fontSize: 17, fontWeight: "800", lineHeight: 23 },
  fact: { gap: 6 },
  pieLayout: { alignItems: "center", flexDirection: "row", flexWrap: "wrap", gap: 24 },
  pieLayoutCompact: { gap: 16 },
  pieLegend: { flex: 1, gap: 9, minWidth: 160 },
  legendRow: { alignItems: "center", flexDirection: "row", gap: 9, minHeight: 38 },
  legendDot: { borderRadius: 5, height: 10, width: 10 },
  legendCopy: { flex: 1, gap: 1 },
  legendLabel: { fontSize: 14, fontWeight: "800", lineHeight: 19 },
  provenance: { fontSize: 12, lineHeight: 18 },
  evidence: { borderTopWidth: 1, gap: 3, paddingTop: 12 },
  relationship: { borderTopWidth: 1, gap: 6, paddingTop: 14 },
  timingPattern: { borderTopWidth: 1, gap: 10, paddingTop: 14 },
  relationshipEvidence: { minHeight: 28, justifyContent: "center" },
  factHeader: {
    alignItems: "center",
    flexDirection: "row",
    justifyContent: "space-between",
  },
  factName: {
    fontSize: 15,
    fontWeight: "800",
    lineHeight: 21,
    flexShrink: 1,
    marginRight: 12,
  },
  factPercent: { fontSize: 14, fontWeight: "800", lineHeight: 20 },
  factMeta: { fontSize: 12, lineHeight: 17 },
  track: { borderRadius: 4, height: 7, overflow: "hidden" },
  bar: { borderRadius: 4, height: 7 },
  themeRow: {
    alignItems: "center",
    flexDirection: "row",
    justifyContent: "space-between",
  },
  activityRow: { alignItems: "center", flexDirection: "row", gap: 10 },
  error: { fontSize: 13, lineHeight: 18 },
});
