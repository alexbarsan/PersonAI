import { useEffect } from "react";
import { Linking, Modal, Pressable, ScrollView, StyleSheet, useWindowDimensions, View } from "react-native";
import ArrowLeft from "lucide-react-native/icons/arrow-left";
import ChevronRight from "lucide-react-native/icons/chevron-right";
import ExternalLink from "lucide-react-native/icons/external-link";
import X from "lucide-react-native/icons/x";

import { DreamObservationResponse, DreamPatternRelationshipResponse } from "@/api/dto";
import { Text } from "@/components/Text";
import {
  DreamPattern,
  patternTypeLabel,
} from "@/features/insights/dreamMapModel";
import { useTheme } from "@/theme/ThemeProvider";

export type PatternDetailTab = "overview" | "related" | "dreams" | "trends";

type PatternDetailSheetProps = {
  activeTab: PatternDetailTab;
  canGoBack: boolean;
  isError: boolean;
  isLoading: boolean;
  observation?: DreamObservationResponse;
  onBack: () => void;
  onChangeTab: (tab: PatternDetailTab) => void;
  onClose: () => void;
  onOpenDream: (dreamId: string) => void;
  onSelectPattern: (pattern: DreamPattern) => void;
  pattern: DreamPattern | null;
};

const tabs: Array<{ id: PatternDetailTab; label: string }> = [
  { id: "overview", label: "Overview" },
  { id: "related", label: "Related" },
  { id: "dreams", label: "Dreams" },
  { id: "trends", label: "Trends" },
];

export function PatternDetailSheet(props: PatternDetailSheetProps) {
  const { width, height } = useWindowDimensions();
  const theme = useTheme();
  const mobile = width < 760;

  useEffect(() => {
    if (!props.pattern || typeof document === "undefined") return;
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") props.onClose();
    };
    document.addEventListener("keydown", closeOnEscape);
    return () => document.removeEventListener("keydown", closeOnEscape);
  }, [props.onClose, props.pattern]);

  if (!props.pattern) return null;

  const sheetWidth = mobile ? width : Math.min(500, Math.max(440, width * 0.4));
  return (
    <Modal animationType={mobile ? "slide" : "fade"} onRequestClose={props.onClose} transparent visible>
      <View style={[styles.overlay, mobile ? styles.mobileOverlay : styles.desktopOverlay]}>
        <Pressable accessibilityLabel="Close pattern details" onPress={props.onClose} style={styles.backdrop} />
        <View
          accessibilityLabel={`${props.pattern.name} pattern details`}
          accessibilityViewIsModal
          style={[
            styles.sheet,
            mobile ? styles.mobileSheet : styles.desktopSheet,
            {
              backgroundColor: theme.colors.surface,
              borderColor: theme.colors.border,
              height: mobile ? Math.min(height * 0.88, 760) : height,
              width: sheetWidth,
            },
          ]}
        >
          {mobile ? <View style={[styles.handle, { backgroundColor: theme.colors.border }]} /> : null}
          <View style={[styles.header, { borderColor: theme.colors.border }]}>
            <View style={styles.headerActions}>
              {props.canGoBack ? (
                <Pressable accessibilityLabel="Back to previous pattern" onPress={props.onBack} style={styles.iconButton}>
                  <ArrowLeft color={theme.colors.text} size={20} />
                </Pressable>
              ) : <View style={styles.iconButton} />}
              <Pressable accessibilityLabel="Close pattern details" onPress={props.onClose} style={styles.iconButton}>
                <X color={theme.colors.text} size={20} />
              </Pressable>
            </View>
            <View style={styles.headingCopy}>
              <Text accessibilityRole="header" style={[styles.title, { color: theme.colors.text }]}>{props.pattern.name}</Text>
              <View style={styles.metadataRow}>
                <View style={[styles.typeChip, { backgroundColor: theme.colors.lavender }]}>
                  <Text style={[styles.typeChipText, { color: theme.colors.text }]}>{patternTypeLabel(props.pattern.type)}</Text>
                </View>
                <Text style={[styles.meta, { color: theme.colors.mutedText }]}>
                  {props.pattern.count} {props.pattern.count === 1 ? "dream" : "dreams"} · {formatPercentage(props.pattern.journalPercentage)} of your journal
                </Text>
              </View>
            </View>
          </View>

          <View accessibilityRole="tablist" style={[styles.tabs, { borderColor: theme.colors.border }]}>
            {tabs.map((tab) => {
              const selected = props.activeTab === tab.id;
              return <Pressable
                accessibilityRole="tab"
                accessibilityState={{ selected }}
                key={tab.id}
                onPress={() => props.onChangeTab(tab.id)}
                style={[styles.tab, selected ? { borderColor: theme.colors.primary } : { borderColor: "transparent" }]}
              >
                <Text style={[styles.tabText, { color: selected ? theme.colors.primary : theme.colors.mutedText }]}>{tab.label}</Text>
              </Pressable>;
            })}
          </View>

          <ScrollView contentContainerStyle={styles.content}>
            {props.isLoading ? <DetailState title="Gathering this pattern" body="Loading the dreams that support this observation." /> : null}
            {props.isError ? <DetailState title="Details unavailable" body="The Dream Map is still available. Close this view or try this pattern again." warning /> : null}
            {!props.isLoading && !props.isError && props.activeTab === "overview" ? <Overview {...props} /> : null}
            {!props.isLoading && !props.isError && props.activeTab === "related" ? <Related {...props} /> : null}
            {!props.isLoading && !props.isError && props.activeTab === "dreams" ? <Dreams {...props} /> : null}
            {!props.isLoading && !props.isError && props.activeTab === "trends" ? <Trends {...props} /> : null}
          </ScrollView>
        </View>
      </View>
    </Modal>
  );
}

function Overview(props: PatternDetailSheetProps) {
  const theme = useTheme();
  const interpretation = props.observation?.personalizedInterpretation;
  const meanings = props.observation?.researchLenses ?? [];
  return <View style={styles.sections}>
    <View style={[styles.interpretation, { backgroundColor: theme.colors.sage, borderColor: theme.colors.border }]}>
      <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Your DreamDNA interpretation</Text>
      {interpretation ? (
        <>
          <Text style={[styles.interpretationText, { color: theme.colors.text }]}>{interpretation.reflection}</Text>
          <Text style={[styles.meta, { color: theme.colors.mutedText }]}>Updated {formatTimestamp(interpretation.generatedAt)}</Text>
        </>
      ) : (
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>A personalized interpretation has not been generated for this pattern yet.</Text>
      )}
    </View>
    <View style={styles.section}>
      <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Connected concepts</Text>
      {props.observation?.relatedPatterns.length ? (
        <View style={styles.chips}>{props.observation.relatedPatterns.map((relation) => <Pressable
          accessibilityHint={`Appears in ${relation.jointDreamCount} of ${relation.sourceDreamCount} dreams with this pattern, ${formatLift(relation.lift)} more common than its usual journal rate.`}
          accessibilityLabel={`Explore ${relation.name}`}
          accessibilityRole="button"
          key={relation.patternId}
          onPress={() => props.onSelectPattern(toDreamPattern(relation))}
          style={[styles.chip, { borderColor: theme.colors.border }]}
        ><Text style={[styles.chipText, { color: theme.colors.text }]}>{relation.name} · {formatRate(relation.coOccurrenceRate)}</Text></Pressable>)}</View>
      ) : <ConnectionFallback observation={props.observation} />}
    </View>
    <View style={styles.section}>
      <Text style={[styles.secondaryTitle, { color: theme.colors.text }]}>Research lenses</Text>
      {meanings.length ? <View style={styles.meanings}>{meanings.map((meaning) => <View key={`${meaning.source.id}:${meaning.text}`} style={styles.meaning}>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>{meaning.text}</Text>
        <Pressable
          accessibilityLabel={`Open source ${meaning.source.title}`}
          accessibilityRole="link"
          onPress={() => void Linking.openURL(meaning.source.url)}
          style={styles.sourceLink}
        >
          <Text style={[styles.sourceText, { color: theme.colors.primary }]}>{meaning.source.title} ({meaning.source.publishedYear})</Text>
          <ExternalLink color={theme.colors.primary} size={14} />
        </Pressable>
      </View>)}</View> : <Text style={[styles.body, { color: theme.colors.mutedText }]}>Research context is not available for this pattern yet.</Text>}
    </View>
  </View>;
}

function Related(props: PatternDetailSheetProps) {
  const theme = useTheme();
  const relatedPatterns = props.observation?.relatedPatterns ?? [];
  if (relatedPatterns.length === 0) return <ConnectionFallback observation={props.observation} state />;
  const groups = groupRelatedPatterns(relatedPatterns);
  return <View style={styles.sections}>
    <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Related patterns</Text>
    {groups.map(([type, patterns]) => <View key={type} style={styles.relatedGroup}>
      <Text style={[styles.secondaryTitle, { color: theme.colors.text }]}>{patternTypeLabel(type as DreamPattern["type"])}</Text>
      {patterns.map((relation) => <Pressable
      accessibilityLabel={`Explore ${relation.name}, connected in ${relation.jointDreamCount} dreams`}
      accessibilityRole="button"
      key={relation.patternId}
      onPress={() => props.onSelectPattern(toDreamPattern(relation))}
      style={[styles.relatedRow, { borderColor: theme.colors.border }]}
    >
      <View style={styles.relatedHeading}>
        <View style={styles.relatedCopy}>
          <Text style={[styles.rowTitle, { color: theme.colors.text }]}>{relation.name}</Text>
          <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{relation.jointDreamCount} of {relation.sourceDreamCount} dreams · {formatRate(relation.coOccurrenceRate)}</Text>
          <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{formatLift(relation.lift)} more common with this pattern</Text>
        </View>
        <ChevronRight color={theme.colors.mutedText} size={18} />
      </View>
      <View style={[styles.progressTrack, { backgroundColor: theme.colors.softInk }]}>
        <View style={[styles.progressValue, { backgroundColor: theme.colors.primary, width: `${Math.min(100, relation.coOccurrenceRate * 100)}%` }]} />
      </View>
    </Pressable>)}</View>)}
  </View>;
}

function Dreams(props: PatternDetailSheetProps) {
  const theme = useTheme();
  const evidence = props.observation?.evidence ?? [];
  if (evidence.length === 0) return <DetailState title="No supporting dreams available" body="Dream evidence will appear here when it is available for this pattern." />;
  return <View style={styles.sections}>
    <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Dreams with this {props.pattern?.type}</Text>
    {evidence.map((dream) => <Pressable
      accessibilityLabel={`Open dream ${dream.title}`}
      accessibilityRole="link"
      key={dream.dreamId}
      onPress={() => props.onOpenDream(dream.dreamId)}
      style={[styles.dreamRow, { borderColor: theme.colors.border }]}
    >
      <View style={styles.relatedCopy}>
        <Text style={[styles.rowTitle, { color: theme.colors.text }]}>{dream.title}</Text>
        <Text style={[styles.meta, { color: theme.colors.mutedText }]}>{formatDate(dream.observedAt)}</Text>
      </View>
      <ChevronRight color={theme.colors.mutedText} size={18} />
    </Pressable>)}
  </View>;
}

function ConnectionFallback({ observation, state = false }: { observation?: DreamObservationResponse; state?: boolean }) {
  const sourceCount = observation?.relationshipReadiness.sourceDreamCount ?? 0;
  const minimum = observation?.relationshipReadiness.minimumSourceDreamCount ?? 3;
  const title = sourceCount <= 1 ? "No connected patterns yet" : "No strong connections yet";
  const body = sourceCount <= 1
    ? "This pattern has appeared once so far. More dreams are needed before DreamDNA can identify recurring connections."
    : sourceCount < minimum
      ? `This pattern has appeared in ${sourceCount} dreams. More repeated dreams are needed before DreamDNA can identify recurring connections.`
      : `We found this pattern in ${sourceCount} dreams, but there is not enough repeated overlap with another pattern to show a reliable journal connection yet.`;
  if (state) return <DetailState title={title} body={body} />;
  const theme = useTheme();
  return <Text style={[styles.body, { color: theme.colors.mutedText }]}>{body}</Text>;
}

function groupRelatedPatterns(patterns: DreamPatternRelationshipResponse[]) {
  return Array.from(patterns.reduce((groups, pattern) => {
    const group = groups.get(pattern.patternType) ?? [];
    group.push(pattern);
    groups.set(pattern.patternType, group);
    return groups;
  }, new Map<string, DreamPatternRelationshipResponse[]>()).entries());
}

function toDreamPattern(relation: DreamPatternRelationshipResponse): DreamPattern {
  return {
    id: relation.patternId,
    type: relation.patternType as DreamPattern["type"],
    name: relation.name,
    count: relation.totalPatternDreamCount,
    journalPercentage: relation.baseRate * 100,
    lastObservedAt: null,
  };
}

function Trends(props: PatternDetailSheetProps) {
  const theme = useTheme();
  const months = props.observation?.monthlyOccurrences ?? [];
  const maximum = Math.max(1, ...months.map((item) => item.count));
  return <View style={styles.sections}>
    <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Pattern history</Text>
    <View style={styles.metrics}>
      <Metric label="Occurrences" value={String(props.pattern?.count ?? 0)} />
      <Metric label="Share of journal" value={formatPercentage(props.pattern?.journalPercentage ?? 0)} />
      <Metric label="First available" value={props.observation?.firstObservedAt ? formatDate(props.observation.firstObservedAt) : "Not available"} />
      <Metric label="Most recent" value={props.observation?.lastObservedAt ? formatDate(props.observation.lastObservedAt) : "Not available"} />
    </View>
    <View style={[styles.lowData, { borderColor: theme.colors.border }]}>
      <Text style={[styles.secondaryTitle, { color: theme.colors.text }]}>Occurrences over time</Text>
      {months.length > 0 ? <>
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>{trendLabel(props.observation?.trendDirection)}</Text>
        <View accessibilityLabel="Monthly pattern occurrences" style={styles.monthChart}>
          {months.map((item) => <View key={item.month} style={styles.monthColumn}>
            <Text style={[styles.monthCount, { color: theme.colors.text }]}>{item.count}</Text>
            <View style={[styles.monthTrack, { borderColor: theme.colors.border }]}>
              <View style={[styles.monthBar, { backgroundColor: theme.colors.primary, height: `${Math.max(item.count > 0 ? 12 : 0, item.count / maximum * 100)}%` }]} />
            </View>
            <Text style={[styles.monthLabel, { color: theme.colors.mutedText }]}>{formatMonth(item.month)}</Text>
          </View>)}
        </View>
      </> : <Text style={[styles.body, { color: theme.colors.mutedText }]}>Monthly history is not available for this pattern yet.</Text>}
    </View>
  </View>;
}

function Metric({ label, value }: { label: string; value: string }) {
  const theme = useTheme();
  return <View style={styles.metric}><Text style={[styles.metricValue, { color: theme.colors.text }]}>{value}</Text><Text style={[styles.meta, { color: theme.colors.mutedText }]}>{label}</Text></View>;
}

function DetailState({ title, body, warning = false }: { title: string; body: string; warning?: boolean }) {
  const theme = useTheme();
  return <View style={styles.state}><Text style={[styles.sectionTitle, { color: warning ? theme.colors.warning : theme.colors.text }]}>{title}</Text><Text style={[styles.body, { color: theme.colors.mutedText }]}>{body}</Text></View>;
}

function formatPercentage(value: number) {
  return `${Number.isInteger(value) ? value : value.toFixed(1)}%`;
}

function formatRate(value: number) {
  return formatPercentage(value * 100);
}

function formatLift(value: number) {
  return `${value.toFixed(1)}×`;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(`${value}T00:00:00Z`));
}

function formatTimestamp(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" }).format(new Date(value));
}

function formatMonth(value: string) {
  return new Intl.DateTimeFormat("en", { month: "short" }).format(new Date(`${value}T00:00:00Z`));
}

function trendLabel(direction: DreamObservationResponse["trendDirection"] | undefined) {
  switch (direction) {
    case "increasing": return "This pattern has appeared more often in recent months.";
    case "decreasing": return "This pattern has appeared less often in recent months.";
    case "steady": return "This pattern has remained relatively steady in recent months.";
    default: return "More monthly history is needed before a direction can be estimated.";
  }
}

const styles = StyleSheet.create({
  overlay: { flex: 1 },
  desktopOverlay: { alignItems: "flex-end" },
  mobileOverlay: { justifyContent: "flex-end" },
  backdrop: { ...StyleSheet.absoluteFill, backgroundColor: "rgba(31, 42, 38, 0.18)" },
  sheet: { borderWidth: 1, maxWidth: "100%", overflow: "hidden" },
  desktopSheet: { borderBottomLeftRadius: 8, borderTopLeftRadius: 8 },
  mobileSheet: { borderTopLeftRadius: 12, borderTopRightRadius: 12 },
  handle: { alignSelf: "center", borderRadius: 3, height: 4, marginTop: 8, width: 42 },
  header: { borderBottomWidth: 1, gap: 8, padding: 20, paddingBottom: 16 },
  headerActions: { flexDirection: "row", justifyContent: "space-between" },
  iconButton: { alignItems: "center", height: 40, justifyContent: "center", width: 40 },
  headingCopy: { gap: 8 },
  title: { fontSize: 25, fontWeight: "800", lineHeight: 31 },
  metadataRow: { alignItems: "center", flexDirection: "row", flexWrap: "wrap", gap: 8 },
  typeChip: { borderRadius: 6, paddingHorizontal: 9, paddingVertical: 5 },
  typeChipText: { fontSize: 12, fontWeight: "800" },
  meta: { fontSize: 12, lineHeight: 18 },
  tabs: { borderBottomWidth: 1, flexDirection: "row", paddingHorizontal: 12 },
  tab: { alignItems: "center", borderBottomWidth: 2, flex: 1, minHeight: 48, justifyContent: "center", minWidth: 0 },
  tabText: { fontSize: 13, fontWeight: "800" },
  content: { padding: 20, paddingBottom: 48 },
  sections: { gap: 24 },
  section: { gap: 10 },
  interpretation: { borderLeftWidth: 3, borderRadius: 8, gap: 10, padding: 16 },
  sectionTitle: { fontSize: 17, fontWeight: "800", lineHeight: 23 },
  secondaryTitle: { fontSize: 15, fontWeight: "800", lineHeight: 21 },
  interpretationText: { fontSize: 16, lineHeight: 25 },
  body: { fontSize: 14, lineHeight: 21 },
  chips: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
  chip: { borderRadius: 8, borderWidth: 1, justifyContent: "center", minHeight: 40, paddingHorizontal: 12 },
  chipText: { fontSize: 13, fontWeight: "700" },
  meanings: { gap: 14 },
  meaning: { gap: 6 },
  sourceLink: { alignItems: "center", alignSelf: "flex-start", flexDirection: "row", gap: 5, minHeight: 32 },
  sourceText: { flexShrink: 1, fontSize: 12, fontWeight: "700", lineHeight: 17 },
  relatedRow: { borderBottomWidth: 1, gap: 10, minHeight: 72, paddingVertical: 12 },
  relatedGroup: { gap: 4 },
  relatedHeading: { alignItems: "center", flexDirection: "row", gap: 12 },
  relatedCopy: { flex: 1, gap: 3 },
  rowTitle: { fontSize: 14, fontWeight: "800", lineHeight: 20 },
  progressTrack: { borderRadius: 4, height: 6, overflow: "hidden" },
  progressValue: { borderRadius: 4, height: 6 },
  dreamRow: { alignItems: "center", borderBottomWidth: 1, flexDirection: "row", gap: 12, minHeight: 64, paddingVertical: 10 },
  metrics: { flexDirection: "row", flexWrap: "wrap", gap: 12 },
  metric: { flexBasis: "46%", gap: 2, minWidth: 140 },
  metricValue: { fontSize: 17, fontWeight: "800", lineHeight: 23 },
  lowData: { borderTopWidth: 1, gap: 7, paddingTop: 18 },
  monthChart: { alignItems: "flex-end", flexDirection: "row", gap: 5, height: 150, marginTop: 8 },
  monthColumn: { alignItems: "center", flex: 1, gap: 4, height: "100%", justifyContent: "flex-end", minWidth: 0 },
  monthCount: { fontSize: 11, fontWeight: "800", lineHeight: 15 },
  monthTrack: { borderRadius: 3, borderWidth: 1, height: 92, justifyContent: "flex-end", overflow: "hidden", width: "70%" },
  monthBar: { borderRadius: 3, minHeight: 0, width: "100%" },
  monthLabel: { fontSize: 10, lineHeight: 14 },
  state: { gap: 8, paddingVertical: 12 },
});
