import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { DreamFeedbackRating, DreamFeedbackResponse, UpdateDreamFeedbackRequest } from "@/api/dto";
import { useTheme } from "@/theme/ThemeProvider";

const reasons = [
  { code: "inaccurate", label: "Didn't feel accurate" },
  { code: "too-generic", label: "Too generic" },
  { code: "missed-details", label: "Missed important details" },
  { code: "wrong-tone", label: "Tone didn't feel right" },
  { code: "not-useful", label: "Not useful" },
  { code: "other", label: "Something else" }
] as const;

export function InterpretationFeedbackPanel({ dreamId }: { dreamId: string }) {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const feedback = useQuery({
    queryKey: ["dream-feedback", dreamId],
    queryFn: () => api.getDreamFeedback(dreamId),
    retry: false
  });
  const [showDislike, setShowDislike] = useState(false);
  const [selectedReasons, setSelectedReasons] = useState<string[]>([]);
  const [details, setDetails] = useState("");
  useEffect(() => {
    if (feedback.data?.rating === "dislike") {
      setShowDislike(true);
      setSelectedReasons(feedback.data.reasons);
      setDetails(feedback.data.details ?? "");
    }
  }, [feedback.data]);

  const save = useMutation({
    mutationFn: (request: UpdateDreamFeedbackRequest) => api.updateDreamFeedback(dreamId, request),
    onSuccess: (saved) => {
      queryClient.setQueryData<DreamFeedbackResponse>(["dream-feedback", dreamId], saved);
      setShowDislike(saved.rating === "dislike");
    }
  });

  const chooseRating = (rating: DreamFeedbackRating) => {
    if (rating === "like") {
      setShowDislike(false);
      setSelectedReasons([]);
      setDetails("");
      save.mutate({ rating: "like", reasons: [], details: null });
      return;
    }

    setShowDislike(true);
  };

  const toggleReason = (code: string) => {
    setSelectedReasons((current) => current.includes(code)
      ? current.filter((reason) => reason !== code)
      : [...current, code]);
  };

  return (
    <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]} testID="interpretation-feedback">
      <Text style={[styles.title, { color: theme.colors.text }]}>Was this interpretation helpful?</Text>
      <View style={styles.ratingRow}>
        <RatingButton disabled={save.isPending} label="Helpful" selected={feedback.data?.rating === "like" && !showDislike} onPress={() => chooseRating("like")} />
        <RatingButton disabled={save.isPending} label="Not for me" selected={feedback.data?.rating === "dislike" || showDislike} onPress={() => chooseRating("dislike")} />
      </View>

      {showDislike ? <View style={styles.dislikeForm}>
        <Text style={[styles.prompt, { color: theme.colors.text }]}>What didn't work?</Text>
        <View style={styles.reasonList}>
          {reasons.map((reason) => {
            const selected = selectedReasons.includes(reason.code);
            return <Pressable
              accessibilityRole="checkbox"
              accessibilityState={{ checked: selected }}
              key={reason.code}
              onPress={() => toggleReason(reason.code)}
              style={[styles.reason, { borderColor: selected ? theme.colors.primary : theme.colors.border, backgroundColor: selected ? theme.colors.softInk : theme.colors.surface }]}
              testID={`feedback-reason-${reason.code}`}
            ><View style={[styles.checkbox, { borderColor: theme.colors.primary }, selected && { backgroundColor: theme.colors.primary }]} /><Text style={[styles.reasonText, { color: theme.colors.text }]}>{reason.label}</Text></Pressable>;
          })}
        </View>
        <TextInput
          accessibilityLabel="Additional interpretation feedback"
          maxLength={1000}
          multiline
          onChangeText={setDetails}
          placeholder="Add a detail (optional)"
          placeholderTextColor={theme.colors.mutedText}
          style={[styles.details, { borderColor: theme.colors.border, color: theme.colors.text }]}
          textAlignVertical="top"
          value={details}
        />
        <View style={styles.formFooter}>
          <Text style={[styles.counter, { color: theme.colors.mutedText }]}>{details.length}/1000</Text>
          <Pressable
            accessibilityRole="button"
            disabled={selectedReasons.length === 0 || save.isPending}
            onPress={() => save.mutate({ rating: "dislike", reasons: selectedReasons, details: details.trim() || null })}
            style={[styles.submit, { backgroundColor: theme.colors.primary }, (selectedReasons.length === 0 || save.isPending) && styles.disabled]}
            testID="save-interpretation-feedback"
          ><Text style={[styles.submitText, { color: theme.colors.primaryText }]}>{save.isPending ? "Saving" : "Send feedback"}</Text></Pressable>
        </View>
      </View> : null}

      {save.isSuccess ? <Text style={[styles.status, { color: theme.colors.mutedText }]}>Thanks. Your feedback was saved.</Text> : null}
      {save.isError || feedback.isError ? <Text style={[styles.status, { color: theme.colors.warning }]}>Feedback could not be saved. Please try again.</Text> : null}
    </View>
  );
}

function RatingButton({ disabled, label, selected, onPress }: { disabled: boolean; label: string; selected: boolean; onPress: () => void }) {
  const theme = useTheme();
  return <Pressable
    accessibilityRole="button"
    accessibilityState={{ selected }}
    disabled={disabled}
    onPress={onPress}
    style={[styles.rating, { borderColor: selected ? theme.colors.primary : theme.colors.border, backgroundColor: selected ? theme.colors.softInk : theme.colors.surface }, disabled && styles.disabled]}
  ><Text style={[styles.ratingText, { color: theme.colors.text }]}>{label}</Text></Pressable>;
}

const styles = StyleSheet.create({
  panel: { borderRadius: 8, borderWidth: 1, gap: 12, padding: 14 },
  title: { fontSize: 17, fontWeight: "800", lineHeight: 23 },
  ratingRow: { flexDirection: "row", gap: 8 },
  rating: { alignItems: "center", borderRadius: 6, borderWidth: 1, flex: 1, justifyContent: "center", minHeight: 44, paddingHorizontal: 12 },
  ratingText: { fontSize: 14, fontWeight: "700" },
  dislikeForm: { gap: 10 },
  prompt: { fontSize: 14, fontWeight: "800", lineHeight: 20 },
  reasonList: { gap: 7 },
  reason: { alignItems: "center", borderRadius: 6, borderWidth: 1, flexDirection: "row", gap: 10, minHeight: 42, paddingHorizontal: 12, paddingVertical: 8 },
  checkbox: { borderRadius: 3, borderWidth: 1, height: 16, width: 16 },
  reasonText: { flex: 1, fontSize: 14, lineHeight: 20 },
  details: { borderRadius: 8, borderWidth: 1, fontSize: 15, lineHeight: 21, minHeight: 90, padding: 12 },
  formFooter: { alignItems: "center", flexDirection: "row", justifyContent: "space-between" },
  counter: { fontSize: 12 },
  submit: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 42, minWidth: 128, paddingHorizontal: 14 },
  submitText: { fontSize: 14, fontWeight: "800" },
  disabled: { opacity: 0.45 },
  status: { fontSize: 13, lineHeight: 18 }
});
