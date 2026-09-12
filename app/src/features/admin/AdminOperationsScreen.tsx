import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { AdminOperationsIssueResponse } from "@/api/dto";
import { ApiError } from "@/api/errors";
import { AppShell, BrandMark } from "@/components/AppShell";
import { useTheme } from "@/theme/ThemeProvider";
import { AdminDreamExplorer } from "@/features/admin/AdminDreamExplorer";

type IssueFilter = "all" | "failed" | "stale";

export function AdminOperationsScreen() {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<IssueFilter>("all");
  const [view, setView] = useState<"health" | "dreams">("health");
  const [selected, setSelected] = useState<AdminOperationsIssueResponse | null>(null);
  const [reason, setReason] = useState("");
  const operations = useQuery({
    queryKey: ["admin-operations"],
    queryFn: () => api.getAdminOperations(),
    refetchInterval: 15_000
  });
  const completeAction = () => {
    setSelected(null);
    setReason("");
    void queryClient.invalidateQueries({ queryKey: ["admin-operations"] });
  };
  const requeue = useMutation({
    mutationFn: ({ jobId, actionReason }: { jobId: string; actionReason: string }) => api.requeueAdminJob(jobId, actionReason),
    onSuccess: completeAction
  });
  const acknowledge = useMutation({
    mutationFn: ({ source, id, actionReason }: { source: string; id: string; actionReason: string }) => api.acknowledgeAdminIssue(source, id, actionReason),
    onSuccess: completeAction
  });
  const issues = useMemo(() => operations.data?.issues.filter((issue) => {
    if (filter === "failed") return issue.status === "failed";
    if (filter === "stale") return issue.status !== "failed";
    return true;
  }) ?? [], [filter, operations.data?.issues]);
  const unauthorized = operations.error instanceof ApiError && operations.error.status === 403;
  const validReason = reason.trim().length >= 10 && reason.trim().length <= 500;

  return (
    <AppShell showNavigation={false}>
      <ScrollView contentContainerStyle={styles.screen}>
        <View style={styles.pageHeader}>
          <BrandMark detail="Operations" />
          <Pressable accessibilityRole="button" onPress={() => operations.refetch()} style={[styles.refreshButton, { borderColor: theme.colors.border }]}>
            <Text style={[styles.buttonLabel, { color: theme.colors.text }]}>{operations.isFetching ? "Refreshing" : "Refresh"}</Text>
          </Pressable>
        </View>

        <View style={[styles.segmented, { borderColor: theme.colors.border }]}>
          <Pressable accessibilityRole="button" onPress={() => setView("health")} style={[styles.segment, view === "health" && { backgroundColor: theme.colors.softInk }]}><Text style={[styles.segmentLabel, { color: theme.colors.text }]}>Health</Text></Pressable>
          <Pressable accessibilityRole="button" onPress={() => setView("dreams")} style={[styles.segment, view === "dreams" && { backgroundColor: theme.colors.softInk }]}><Text style={[styles.segmentLabel, { color: theme.colors.text }]}>Dreams</Text></Pressable>
        </View>

        {view === "health" && unauthorized ? <Text style={[styles.error, { color: theme.colors.warning }]}>This account is not authorized for operations.</Text> : null}
        {view === "health" && operations.isError && !unauthorized ? <Text style={[styles.error, { color: theme.colors.warning }]}>Operations data could not be loaded.</Text> : null}
        {view === "health" && operations.isLoading ? <Text style={[styles.muted, { color: theme.colors.mutedText }]}>Loading operations</Text> : null}

        {view === "health" && operations.data ? <>
          <View style={styles.metricGrid}>
            <Metric label="Queue" value={operations.data.queue.available} tone="neutral" />
            <Metric label="In flight" value={operations.data.queue.inFlight} tone="neutral" />
            <Metric label="Failed jobs" value={operations.data.jobs.failed} tone={operations.data.jobs.failed > 0 ? "warning" : "good"} />
            <Metric label="Dead letter" value={operations.data.queue.deadLetter} tone={operations.data.queue.deadLetter > 0 ? "warning" : "good"} />
          </View>
          {operations.data.queue.error ? <Text style={[styles.error, { color: theme.colors.warning }]}>{operations.data.queue.error}</Text> : null}

          <SectionTitle title="Workloads" detail={`Updated ${formatTimestamp(operations.data.generatedAt)}`} />
          <View style={[styles.table, { borderColor: theme.colors.border }]}>
            {operations.data.workloads.map((workload) => (
              <View key={workload.source} style={[styles.tableRow, { borderColor: theme.colors.border }]}>
                <View style={styles.tableName}><Text style={[styles.rowTitle, { color: theme.colors.text }]}>{labelOperation(workload.source)}</Text><Text style={[styles.muted, { color: theme.colors.mutedText }]}>{workload.oldestActiveSeconds === null ? "No active work" : `Oldest ${formatAge(workload.oldestActiveSeconds)}`}</Text></View>
                <Count label="Pending" value={workload.pending} />
                <Count label="Active" value={workload.processing} />
                <Count label="Failed" value={workload.failed} warning={workload.failed > 0} />
              </View>
            ))}
          </View>

          <View style={styles.sectionHeader}>
            <SectionTitle title="Issues" detail={`${operations.data.issues.length} requiring attention`} />
            <View style={[styles.segmented, { borderColor: theme.colors.border }]}>
              {(["all", "failed", "stale"] as const).map((item) => <Pressable key={item} accessibilityRole="button" onPress={() => setFilter(item)} style={[styles.segment, filter === item && { backgroundColor: theme.colors.softInk }]}><Text style={[styles.segmentLabel, { color: theme.colors.text }]}>{item === "all" ? "All" : item === "failed" ? "Failed" : "Stale"}</Text></Pressable>)}
            </View>
          </View>
          {issues.length === 0 ? <View style={[styles.empty, { backgroundColor: theme.colors.sage }]}><Text style={[styles.rowTitle, { color: theme.colors.text }]}>No matching issues</Text></View> : null}
          <View style={styles.issueList}>
            {issues.map((issue) => <IssueRow key={`${issue.source}-${issue.id}`} issue={issue} onSelect={() => { setSelected(issue); setReason(""); }} />)}
          </View>

          <SectionTitle title="Providers, last 24 hours" detail="Latency and estimated AI cost" />
          <View style={[styles.table, { borderColor: theme.colors.border }]}>
            {operations.data.providers.map((provider) => <View key={`${provider.provider}-${provider.operationType}`} style={[styles.providerRow, { borderColor: theme.colors.border }]}><View style={styles.tableName}><Text style={[styles.rowTitle, { color: theme.colors.text }]}>{provider.provider}</Text><Text style={[styles.muted, { color: theme.colors.mutedText }]}>{labelOperation(provider.operationType)} | {provider.operations} calls | {provider.failed} failed</Text></View><View style={styles.providerNumbers}><Text style={[styles.number, { color: theme.colors.text }]}>{formatDuration(provider.p95LatencyMilliseconds)} p95</Text><Text style={[styles.muted, { color: theme.colors.mutedText }]}>${provider.estimatedCostUsd.toFixed(4)}</Text></View></View>)}
          </View>
        </> : null}
        {view === "dreams" ? <AdminDreamExplorer /> : null}

        {selected ? <View style={[styles.actionPanel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
          <View style={styles.actionHeader}><View style={styles.tableName}><Text style={[styles.rowTitle, { color: theme.colors.text }]}>{labelOperation(selected.operationType)}</Text><Text style={[styles.muted, { color: theme.colors.mutedText }]}>{selected.status} | {formatAge(selected.ageSeconds)} old</Text></View><Pressable accessibilityRole="button" onPress={() => setSelected(null)}><Text style={[styles.buttonLabel, { color: theme.colors.mutedText }]}>Close</Text></Pressable></View>
          <TextInput accessibilityLabel="Operations action reason" multiline onChangeText={setReason} placeholder="Reason for this action" placeholderTextColor={theme.colors.mutedText} style={[styles.reasonInput, { borderColor: theme.colors.border, color: theme.colors.text }]} value={reason} />
          <View style={styles.actionButtons}>
            <Pressable accessibilityRole="button" disabled={!validReason || acknowledge.isPending} onPress={() => acknowledge.mutate({ source: selected.source, id: selected.id, actionReason: reason.trim() })} style={[styles.secondaryButton, { borderColor: theme.colors.border, opacity: validReason ? 1 : 0.5 }]}><Text style={[styles.buttonLabel, { color: theme.colors.text }]}>Acknowledge</Text></Pressable>
            {selected.canRequeue && selected.jobId ? <Pressable accessibilityRole="button" disabled={!validReason || requeue.isPending} onPress={() => requeue.mutate({ jobId: selected.jobId!, actionReason: reason.trim() })} style={[styles.primaryButton, { backgroundColor: theme.colors.primary, opacity: validReason ? 1 : 0.5 }]}><Text style={[styles.buttonLabel, { color: theme.colors.primaryText }]}>Requeue</Text></Pressable> : null}
          </View>
          {requeue.isError || acknowledge.isError ? <Text style={[styles.error, { color: theme.colors.warning }]}>The action could not be completed.</Text> : null}
        </View> : null}
      </ScrollView>
    </AppShell>
  );
}

function Metric({ label, value, tone }: { label: string; value: number; tone: "neutral" | "warning" | "good" }) {
  const theme = useTheme();
  const backgroundColor = tone === "warning" ? theme.colors.lavender : tone === "good" ? theme.colors.sage : theme.colors.surface;
  return <View style={[styles.metric, { backgroundColor, borderColor: theme.colors.border }]}><Text style={[styles.metricValue, { color: theme.colors.text }]}>{value}</Text><Text style={[styles.muted, { color: theme.colors.mutedText }]}>{label}</Text></View>;
}

function Count({ label, value, warning = false }: { label: string; value: number; warning?: boolean }) {
  const theme = useTheme();
  return <View style={styles.count}><Text style={[styles.number, { color: warning ? theme.colors.warning : theme.colors.text }]}>{value}</Text><Text style={[styles.countLabel, { color: theme.colors.mutedText }]}>{label}</Text></View>;
}

function SectionTitle({ title, detail }: { title: string; detail: string }) {
  const theme = useTheme();
  return <View style={styles.titleBlock}><Text style={[styles.sectionTitle, { color: theme.colors.text }]}>{title}</Text><Text style={[styles.muted, { color: theme.colors.mutedText }]}>{detail}</Text></View>;
}

function IssueRow({ issue, onSelect }: { issue: AdminOperationsIssueResponse; onSelect: () => void }) {
  const theme = useTheme();
  return <Pressable accessibilityRole="button" onPress={onSelect} testID={`operation-issue-${issue.id}`} style={[styles.issue, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}><View style={styles.issueTop}><View style={styles.tableName}><Text style={[styles.rowTitle, { color: theme.colors.text }]}>{labelOperation(issue.operationType)}</Text><Text numberOfLines={2} style={[styles.muted, { color: theme.colors.mutedText }]}>{issue.failure ?? `${issue.status} for ${formatAge(issue.ageSeconds)}`}</Text></View><View style={[styles.status, { backgroundColor: issue.status === "failed" ? theme.colors.lavender : theme.colors.softInk }]}><Text style={[styles.statusText, { color: theme.colors.text }]}>{issue.acknowledged ? "acknowledged" : issue.status}</Text></View></View><Text style={[styles.muted, { color: theme.colors.mutedText }]}>Attempt {issue.attemptCount} | {formatAge(issue.ageSeconds)} old</Text></Pressable>;
}

function labelOperation(value: string) {
  return value.split(/[.-]/).map((word) => word.charAt(0).toUpperCase() + word.slice(1)).join(" ");
}

function formatAge(seconds: number) {
  if (seconds < 60) return `${seconds}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
  return `${Math.floor(seconds / 3600)}h`;
}

function formatDuration(milliseconds: number) {
  return milliseconds < 1000 ? `${milliseconds}ms` : `${(milliseconds / 1000).toFixed(1)}s`;
}

function formatTimestamp(value: string) {
  return new Intl.DateTimeFormat("en", { hour: "2-digit", minute: "2-digit" }).format(new Date(value));
}

const styles = StyleSheet.create({
  screen: { gap: 18, padding: 20, paddingBottom: 48 },
  pageHeader: { alignItems: "center", flexDirection: "row", justifyContent: "space-between" },
  refreshButton: { borderRadius: 6, borderWidth: 1, justifyContent: "center", minHeight: 38, paddingHorizontal: 12 },
  metricGrid: { flexDirection: "row", flexWrap: "wrap", gap: 10 },
  metric: { borderRadius: 8, borderWidth: 1, flexBasis: "47%", flexGrow: 1, gap: 2, minHeight: 84, padding: 14 },
  metricValue: { fontSize: 26, fontWeight: "800" },
  muted: { fontSize: 12, lineHeight: 17 },
  error: { fontSize: 13, fontWeight: "700", lineHeight: 18 },
  sectionHeader: { gap: 10 },
  titleBlock: { gap: 2 },
  sectionTitle: { fontSize: 18, fontWeight: "800" },
  table: { borderRadius: 8, borderWidth: 1, overflow: "hidden" },
  tableRow: { alignItems: "center", borderBottomWidth: 1, flexDirection: "row", gap: 8, minHeight: 68, padding: 12 },
  tableName: { flex: 1, gap: 3, minWidth: 0 },
  rowTitle: { fontSize: 14, fontWeight: "800", lineHeight: 19 },
  count: { alignItems: "center", minWidth: 46 },
  number: { fontSize: 13, fontWeight: "800" },
  countLabel: { fontSize: 10 },
  segmented: { alignSelf: "flex-start", borderRadius: 6, borderWidth: 1, flexDirection: "row", overflow: "hidden" },
  segment: { justifyContent: "center", minHeight: 34, paddingHorizontal: 13 },
  segmentLabel: { fontSize: 12, fontWeight: "700" },
  empty: { borderRadius: 8, padding: 16 },
  issueList: { gap: 10 },
  issue: { borderRadius: 8, borderWidth: 1, gap: 8, padding: 14 },
  issueTop: { alignItems: "flex-start", flexDirection: "row", gap: 10 },
  status: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 5 },
  statusText: { fontSize: 11, fontWeight: "800", textTransform: "capitalize" },
  providerRow: { alignItems: "center", borderBottomWidth: 1, flexDirection: "row", gap: 10, minHeight: 66, padding: 12 },
  providerNumbers: { alignItems: "flex-end", gap: 2 },
  actionPanel: { borderRadius: 8, borderWidth: 1, gap: 12, padding: 14 },
  actionHeader: { alignItems: "flex-start", flexDirection: "row", gap: 12 },
  reasonInput: { borderRadius: 6, borderWidth: 1, fontSize: 14, lineHeight: 20, minHeight: 78, padding: 10, textAlignVertical: "top" },
  actionButtons: { flexDirection: "row", gap: 10 },
  secondaryButton: { alignItems: "center", borderRadius: 6, borderWidth: 1, flex: 1, justifyContent: "center", minHeight: 42, paddingHorizontal: 12 },
  primaryButton: { alignItems: "center", borderRadius: 6, flex: 1, justifyContent: "center", minHeight: 42, paddingHorizontal: 12 },
  buttonLabel: { fontSize: 13, fontWeight: "800" }
});
