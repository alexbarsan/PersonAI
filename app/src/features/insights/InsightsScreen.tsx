import { useQuery } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { Pressable, ScrollView, StyleSheet, useWindowDimensions, View } from "react-native";
import Svg, { Circle, G } from "react-native-svg";
import { useRouter } from "expo-router";
import ChevronRight from "lucide-react-native/icons/chevron-right";
import { Text } from "@/components/Text";

import { useApiClient } from "@/api/apiContext";
import {
  FactInsightGroupResponse,
  FactInsightResponse,
  JournalSynthesisResponse,
  RelationshipInsightResponse,
  RelationshipReadinessResponse,
  ThemeInsightResponse,
  TimingPatternInsightResponse,
} from "@/api/dto";
import { AppShell, BrandMark } from "@/components/AppShell";
import {
  DreamPattern,
  patternFromFact,
  relationsForPattern,
} from "@/features/insights/dreamMapModel";
import { PatternDetailSheet, PatternDetailTab } from "@/features/insights/PatternDetailSheet";
import { useTheme } from "@/theme/ThemeProvider";

const chartColors = ["#4f8b73", "#8e78b8", "#cf765b", "#d3a63b", "#5f84ad", "#a86f86", "#72928b", "#977352"];

export function InsightsScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const router = useRouter();
  const { width } = useWindowDimensions();
  const wide = width >= 900;
  const [groupFilter, setGroupFilter] = useState("all");
  const [selectedPattern, setSelectedPattern] = useState<DreamPattern | null>(null);
  const [patternHistory, setPatternHistory] = useState<DreamPattern[]>([]);
  const [activeDetailTab, setActiveDetailTab] = useState<PatternDetailTab>("overview");
  const lastFocusedElement = useRef<HTMLElement | null>(null);
  const insights = useQuery({
    queryKey: ["insights"],
    queryFn: () => api.getInsights(),
  });
  const observation = useQuery({
    queryKey: ["dream-observation", selectedPattern?.type, selectedPattern?.name],
    queryFn: () => api.getDreamObservation(selectedPattern!.type, selectedPattern!.name),
    enabled: selectedPattern !== null,
  });

  const openPattern = (pattern: DreamPattern) => {
    if (!selectedPattern && typeof document !== "undefined") {
      lastFocusedElement.current = document.activeElement as HTMLElement | null;
    }
    if (selectedPattern && selectedPattern.id !== pattern.id) {
      setPatternHistory((current) => [...current, selectedPattern]);
    }
    setSelectedPattern(pattern);
    setActiveDetailTab("overview");
  };

  const closePattern = () => {
    setSelectedPattern(null);
    setPatternHistory([]);
    setActiveDetailTab("overview");
    setTimeout(() => lastFocusedElement.current?.focus(), 0);
  };

  const goBackPattern = () => {
    setPatternHistory((current) => {
      const previous = current.at(-1);
      if (previous) setSelectedPattern(previous);
      return current.slice(0, -1);
    });
  };

  return (
    <AppShell>
      <>
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
            <JournalReflection synthesis={insights.data.journalSynthesis} />
            {insights.data.factGroups.length > 0 ? (
              <>
                <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.tabs} accessibilityRole="tablist">
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
                </ScrollView>
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
                        selectedPatternId={selectedPattern?.id}
                        wide={wide && group.type !== "scenario"}
                        onSelect={(fact) => openPattern(patternFromFact(group.type, fact))}
                      />
                    ))}
                </View>
              </>
            ) : (
              <ThemePanel
                themes={insights.data.recurringThemes}
                totalDreams={insights.data.totalDreams}
                selectedPatternId={selectedPattern?.id}
                onSelect={openPattern}
              />
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
      <PatternDetailSheet
        activeTab={activeDetailTab}
        canGoBack={patternHistory.length > 0}
        isError={observation.isError}
        isLoading={observation.isLoading}
        observation={observation.data}
        onBack={goBackPattern}
        onChangeTab={setActiveDetailTab}
        onClose={closePattern}
        onOpenDream={(dreamId) => { closePattern(); router.push(`/dreams/${dreamId}`); }}
        onSelectPattern={openPattern}
        pattern={selectedPattern}
        relatedPatterns={insights.data && selectedPattern ? relationsForPattern(insights.data, selectedPattern) : []}
      />
      </>
    </AppShell>
  );
}

function JournalReflection({ synthesis }: { synthesis: JournalSynthesisResponse }) {
  const theme = useTheme();
  const router = useRouter();

  if (synthesis.status === "not_ready") {
    return (
      <View style={[styles.reflection, { borderColor: theme.colors.border }]}>
        <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Journal reflection</Text>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>
          Your journal reflection begins after {synthesis.minimumCompletedDreams} completed dreams. You have {synthesis.completedDreams}.
        </Text>
      </View>
    );
  }

  if (!synthesis.summary) {
    return (
      <View style={[styles.reflection, { borderColor: theme.colors.border }]}>
        <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Journal reflection</Text>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>Preparing a reflection from the patterns in your journal.</Text>
      </View>
    );
  }

  return (
    <View style={[styles.reflection, { borderColor: theme.colors.border }]}>
      <View style={styles.reflectionHeading}>
        <Text style={[styles.panelTitle, { color: theme.colors.text }]}>Journal reflection</Text>
        {synthesis.status === "updating" ? (
          <Text style={[styles.refreshing, { color: theme.colors.primary }]}>Updating</Text>
        ) : null}
      </View>
      <Text style={[styles.reflectionSummary, { color: theme.colors.text }]}>{synthesis.summary}</Text>
      {synthesis.observations.map((observation) => (
        <View key={observation.title} style={[styles.reflectionObservation, { borderColor: theme.colors.border }]}>
          <Text style={[styles.factName, { color: theme.colors.text }]}>{observation.title}</Text>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>{observation.reflection}</Text>
          <View style={styles.evidenceLinks}>
            {observation.evidence.map((dream) => (
              <Pressable
                key={dream.dreamId}
                accessibilityLabel={`Open evidence dream ${dream.title}`}
                accessibilityRole="link"
                onPress={() => router.push(`/dreams/${dream.dreamId}`)}
                style={styles.evidenceLink}
              >
                <Text style={[styles.provenance, { color: theme.colors.primary }]}>{dream.title}</Text>
              </Pressable>
            ))}
          </View>
        </View>
      ))}
      {synthesis.reflectionQuestions.length > 0 ? (
        <View style={styles.reflectionQuestions}>
          <Text style={[styles.factName, { color: theme.colors.text }]}>Questions to keep nearby</Text>
          {synthesis.reflectionQuestions.map((question) => (
            <Text key={question} style={[styles.body, { color: theme.colors.mutedText }]}>{question}</Text>
          ))}
        </View>
      ) : null}
      <Text style={[styles.note, { color: theme.colors.mutedText }]}>For reflection, not diagnosis.</Text>
    </View>
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
  selectedPatternId,
  wide,
}: {
  group: FactInsightGroupResponse;
  onSelect: (fact: FactInsightResponse) => void;
  selectedPatternId?: string;
  wide: boolean;
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
        wide && styles.groupPanelWide,
      ]}
    >
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>
        {group.title}
      </Text>
      <PieChart
        accessibilityLabel={`${group.title} distribution`}
        selectedLabel={group.facts.find((fact) => patternFromFact(group.type, fact).id === selectedPatternId)?.value}
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

function ThemePanel({
  themes,
  totalDreams,
  selectedPatternId,
  onSelect,
}: {
  themes: ThemeInsightResponse[];
  totalDreams: number;
  selectedPatternId?: string;
  onSelect: (pattern: DreamPattern) => void;
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
        Recurring themes
      </Text>
      {themes.length === 0 ? (
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>
          No repeated themes found yet.
        </Text>
      ) : (
        <PieChart
          accessibilityLabel="Recurring themes distribution"
          selectedLabel={themes.find((theme) => `theme:${theme.name.trim().toLocaleLowerCase()}` === selectedPatternId)?.name}
          data={themes.map((item, index) => ({
            label: item.name,
            value: item.count,
            color: chartColors[index % chartColors.length],
            detail: `${item.count} ${item.count === 1 ? "dream" : "dreams"}`,
            onPress: () => onSelect({
              id: `theme:${item.name.trim().toLocaleLowerCase()}`,
              type: "theme",
              name: item.name,
              count: item.count,
              journalPercentage: totalDreams > 0 ? Math.round((item.count / totalDreams) * 1000) / 10 : 0,
              lastObservedAt: null,
            }),
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
  const [activeMonth, setActiveMonth] = useState<string | null>(null);
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
        <Pressable
          accessibilityLabel={`${formatMonth(count.month)}: ${count.count} ${count.count === 1 ? "dream" : "dreams"}`}
          key={count.month}
          onBlur={() => setActiveMonth(null)}
          onFocus={() => setActiveMonth(count.month)}
          onHoverIn={() => setActiveMonth(count.month)}
          onHoverOut={() => setActiveMonth(null)}
          onPress={() => setActiveMonth((current) => current === count.month ? null : count.month)}
          style={styles.activityRow}
        >
          <Text
            style={[
              styles.factMeta,
              { color: theme.colors.mutedText, width: 76 },
            ]}
          >
            {formatMonth(count.month)}
          </Text>
          <View style={styles.activityTrackWrap}>
          <View style={[styles.track, { backgroundColor: theme.colors.sage }]}>
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
          {activeMonth === count.month ? <View style={[styles.activityTooltip, { backgroundColor: theme.colors.text }]}><Text style={[styles.activityTooltipText, { color: theme.colors.surface }]}>{count.count} {count.count === 1 ? "dream" : "dreams"}</Text></View> : null}
          </View>
          <Text style={[styles.factPercent, { color: theme.colors.text }]}>
            {count.count}
          </Text>
        </Pressable>
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
    <View style={[styles.panel, { backgroundColor: theme.colors.sage, borderColor: theme.colors.border }]}>
      <Text style={[styles.panelTitle, { color: theme.colors.text }]}>
        Timing observations
      </Text>
      <View style={styles.timingGrid}>{patterns.map((pattern) => {
        const total = pattern.weekdayDreams + pattern.weekendDreams;
        const weekdayPercent = total > 0 ? (pattern.weekdayDreams / total) * 100 : 0;
        return (
        <View
          key={`${pattern.type}-${pattern.value}`}
          style={[styles.timingPattern, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}
        >
          <View style={styles.timingHeading}><Text style={[styles.factName, { color: theme.colors.text }]}>{pattern.value}</Text><Text style={[styles.typeLabel, { color: theme.colors.mutedText }]}>{pattern.type}</Text></View>
          <View accessibilityLabel={`${pattern.weekdayDreams} weekday and ${pattern.weekendDreams} weekend dreams`} style={[styles.timingTrack, { backgroundColor: chartColors[1] }]}>
            <View style={[styles.timingWeekday, { backgroundColor: chartColors[0], width: `${weekdayPercent}%` }]} />
          </View>
          <View style={styles.timingCounts}><Text style={[styles.factMeta, { color: theme.colors.mutedText }]}>Weekdays {pattern.weekdayDreams}</Text><Text style={[styles.factMeta, { color: theme.colors.mutedText }]}>Weekends {pattern.weekendDreams}</Text></View>
          <Text style={[styles.timingSummary, { color: theme.colors.text }]}>{timingSummary(pattern.weekdayToWeekendRatio)}</Text>
        </View>
      );})}</View>
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
          <View style={[styles.relationshipTrack, { backgroundColor: theme.colors.softInk }]}><View style={[styles.relationshipValue, { backgroundColor: theme.colors.primary, width: `${Math.min(100, relationship.sharedOfSmallerPatternPercent)}%` }]} /></View>
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

function PieChart({ data, accessibilityLabel, selectedLabel }: { data: PieDatum[]; accessibilityLabel: string; selectedLabel?: string }) {
  const theme = useTheme();
  const mobile = useWindowDimensions().width < 600;
  const [hoveredSliceLabel, setHoveredSliceLabel] = useState<string | null>(null);
  const [hoveredLegendLabel, setHoveredLegendLabel] = useState<string | null>(null);
  const positiveData = data.filter((item) => item.value > 0);
  const total = positiveData.reduce((sum, item) => sum + item.value, 0);
  const activeLabel = hoveredLegendLabel ?? hoveredSliceLabel ?? selectedLabel ?? null;
  const size = 144;
  const center = size / 2;
  const radius = 50;
  const strokeWidth = 24;
  const circumference = 2 * Math.PI * radius;
  let offset = 0;

  if (total === 0) {
    return <Text style={[styles.body, { color: theme.colors.mutedText }]}>Not enough data to chart yet.</Text>;
  }

  return (
    <View style={[styles.pieLayout, mobile && styles.pieLayoutMobile]}>
      <View style={styles.pieChartWrap}><Svg accessibilityLabel={accessibilityLabel} accessibilityRole="image" height={size} width={size}>
        <Circle cx={center} cy={center} fill="none" r={radius} stroke={theme.colors.background} strokeWidth={strokeWidth} />
        <G rotation="-90" origin={`${center}, ${center}`}>
          {positiveData.map((item) => {
            const length = (item.value / total) * circumference;
            const dashOffset = -offset;
            offset += length;
            const interactive = Boolean(item.onPress);
            const active = activeLabel === item.label;
            const dimmed = Boolean(activeLabel) && !active;
            const hoverProps = interactive
              ? ({
                  onMouseEnter: () => setHoveredSliceLabel(item.label),
                  onMouseLeave: () => setHoveredSliceLabel(null),
                  cursor: "pointer",
                } as Record<string, unknown>)
              : {};
            return (
              <Circle
                {...hoverProps}
                accessibilityLabel={interactive ? `Explore ${item.label}, appearing in ${dreamCountLabel(item.value)}` : item.label}
                key={item.label}
                cx={center}
                cy={center}
                fill="none"
                onPress={item.onPress}
                opacity={dimmed ? 0.28 : 1}
                r={radius}
                stroke={item.color}
                strokeDasharray={`${length} ${circumference - length}`}
                strokeDashoffset={dashOffset}
                strokeWidth={active ? strokeWidth + 4 : strokeWidth}
              />
            );
          })}
        </G>
      </Svg></View>
      <View style={[styles.pieLegend, mobile && styles.pieLegendMobile]}>
        {data.map((item) => {
          const active = activeLabel === item.label;
          const dimmed = Boolean(activeLabel) && !active;
          const content = (
            <>
              <View style={[styles.legendDot, { backgroundColor: item.color, opacity: dimmed ? 0.35 : 1 }]} />
              <View style={styles.legendCopy}>
                <Text style={[styles.legendLabel, { color: theme.colors.text, opacity: dimmed ? 0.55 : 1 }]}>{item.label}</Text>
                <Text style={[styles.factMeta, { color: theme.colors.mutedText }]}>{item.detail ?? `${Math.round((item.value / total) * 100)}%`}</Text>
              </View>
              {item.onPress ? <ChevronRight color={theme.colors.mutedText} opacity={active ? 1 : 0} size={17} /> : null}
            </>
          );
          return item.onPress ? (
            <Pressable
              key={item.label}
              accessibilityLabel={`Explore ${item.label}, appearing in ${dreamCountLabel(item.value)}`}
              accessibilityRole="button"
              accessibilityState={{ selected: selectedLabel === item.label }}
              onBlur={() => setHoveredLegendLabel(null)}
              onFocus={() => setHoveredLegendLabel(item.label)}
              onHoverIn={() => setHoveredLegendLabel(item.label)}
              onHoverOut={() => setHoveredLegendLabel(null)}
              onPress={item.onPress}
              style={[styles.legendRow, active && { backgroundColor: theme.colors.softInk }]}
            >{content}</Pressable>
          ) : (
            <View key={item.label} style={styles.legendRow}>{content}</View>
          );
        })}
      </View>
    </View>
  );
}

function timingSummary(weekdayToWeekendRatio: number) {
  if (weekdayToWeekendRatio === 1) return "Similar weekday and weekend frequency";
  if (weekdayToWeekendRatio > 1) return `${formatRatio(weekdayToWeekendRatio)} more common on weekdays`;
  if (weekdayToWeekendRatio > 0) return `${formatRatio(1 / weekdayToWeekendRatio)} more common on weekends`;
  return "Observed on weekends only";
}

function formatRatio(value: number) {
  return `${Number.isInteger(value) ? value : value.toFixed(1)}x`;
}

function dreamCountLabel(value: number) {
  return `${value} ${value === 1 ? "dream" : "dreams"}`;
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
  tabs: { flexDirection: "row", gap: 8, paddingVertical: 8, paddingRight: 4 },
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
  groupPanelWide: { flexBasis: "48%", width: "48%" },
  panelTitle: { fontSize: 17, fontWeight: "800", lineHeight: 23 },
  reflection: { borderBottomWidth: 1, borderTopWidth: 1, gap: 18, paddingVertical: 22 },
  reflectionHeading: { alignItems: "center", flexDirection: "row", justifyContent: "space-between" },
  reflectionSummary: { fontSize: 17, fontWeight: "600", lineHeight: 26, maxWidth: 760 },
  reflectionObservation: { borderTopWidth: 1, gap: 8, paddingTop: 16 },
  reflectionQuestions: { gap: 8 },
  refreshing: { fontSize: 12, fontWeight: "800", lineHeight: 18 },
  evidenceLinks: { flexDirection: "row", flexWrap: "wrap", gap: 10 },
  evidenceLink: { justifyContent: "center", minHeight: 32 },
  fact: { gap: 6 },
  pieLayout: { alignItems: "center", flexDirection: "row", gap: 24, width: "100%" },
  pieLayoutMobile: { alignItems: "stretch", flexDirection: "column", gap: 12 },
  pieChartWrap: { alignSelf: "center", height: 144, width: 144 },
  pieLegend: { flex: 1, gap: 9, minWidth: 160 },
  pieLegendMobile: { flex: 0, minWidth: 0, width: "100%" },
  legendRow: { alignItems: "center", borderRadius: 6, flexDirection: "row", gap: 9, minHeight: 44, paddingHorizontal: 8, paddingVertical: 4 },
  legendDot: { borderRadius: 5, height: 10, width: 10 },
  legendCopy: { flex: 1, gap: 1 },
  legendLabel: { fontSize: 14, fontWeight: "800", lineHeight: 19 },
  provenance: { fontSize: 12, lineHeight: 18 },
  relationship: { borderTopWidth: 1, gap: 6, paddingTop: 14 },
  relationshipTrack: { borderRadius: 4, height: 6, overflow: "hidden" },
  relationshipValue: { borderRadius: 4, height: 6 },
  timingGrid: { flexDirection: "row", flexWrap: "wrap", gap: 12 },
  timingPattern: { borderRadius: 8, borderWidth: 1, flexGrow: 1, flexBasis: 240, gap: 9, minWidth: 220, padding: 14 },
  timingHeading: { alignItems: "center", flexDirection: "row", justifyContent: "space-between" },
  typeLabel: { fontSize: 11, fontWeight: "700", textTransform: "capitalize" },
  timingTrack: { borderRadius: 4, height: 8, overflow: "hidden" },
  timingWeekday: { height: 8 },
  timingCounts: { flexDirection: "row", justifyContent: "space-between" },
  timingSummary: { fontSize: 13, fontWeight: "700", lineHeight: 19 },
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
  activityRow: { alignItems: "center", flexDirection: "row", gap: 10, minHeight: 34 },
  activityTrackWrap: { flex: 1, justifyContent: "center", position: "relative" },
  activityTooltip: { borderRadius: 5, paddingHorizontal: 7, paddingVertical: 3, position: "absolute", right: 4, top: -25 },
  activityTooltipText: { fontSize: 11, fontWeight: "700" },
  error: { fontSize: 13, lineHeight: 18 },
});
