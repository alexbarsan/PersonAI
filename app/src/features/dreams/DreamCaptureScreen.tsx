import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { Pressable, ScrollView, StyleSheet, TextInput, View } from "react-native";
import { Text } from "@/components/Text";

import { AppShell, BrandMark } from "@/components/AppShell";
import { ChoiceOption, ChoiceSet, FivePointScale, TagEditor } from "@/components/FieldControls";
import { toSubmitDreamRequest } from "@/features/dreams/dreamCaptureMapping";
import { VoiceCapturePanel } from "@/features/dreams/VoiceCapturePanel";
import { DreamingFacts } from "@/features/content/DailyDreamContent";
import { defaultDreamCaptureValues, DreamCaptureValues, dreamCaptureSchema } from "@/features/dreams/dreamCaptureSchema";
import { useDreamDraftStore } from "@/state/dreamDraftStore";
import { useAuthStore } from "@/auth/authStore";
import { useDreamSubmission } from "@/features/dreams/useDreamSubmission";
import { dreamSubmissionErrorMessage } from "@/features/dreams/dreamSubmissionError";
import { useTheme } from "@/theme/ThemeProvider";

type DreamCaptureScreenProps = { onSubmitted?: (dreamId: string) => void };

const moodOptions: ChoiceOption[] = [
  { label: "Calm", value: "calm" },
  { label: "Curious", value: "curious" },
  { label: "Joyful", value: "joyful" },
  { label: "Anxious", value: "anxious" },
  { label: "Unsettled", value: "unsettled" }
];

export function DreamCaptureScreen({ onSubmitted }: DreamCaptureScreenProps) {
  const theme = useTheme();
  const subject = useAuthStore((state) => state.user?.subject);
  const draftText = useDreamDraftStore((state) => state.text);
  const draftMood = useDreamDraftStore((state) => state.mood);
  const draftSleepQuality = useDreamDraftStore((state) => state.sleepQuality);
  const draftTags = useDreamDraftStore((state) => state.tags);
  const draftOccurredAt = useDreamDraftStore((state) => state.occurredAt);
  const hasDraftHydrated = useDreamDraftStore((state) => state.hasHydrated);
  const setDraftFields = useDreamDraftStore((state) => state.setFields);
  const adoptForUser = useDreamDraftStore((state) => state.adoptForUser);
  const form = useForm<DreamCaptureValues>({
    resolver: zodResolver(dreamCaptureSchema),
    defaultValues: { ...defaultDreamCaptureValues, text: draftText, mood: draftMood, sleepQuality: draftSleepQuality, tags: draftTags, occurredAt: draftOccurredAt }
  });
  useEffect(() => {
    if (subject) adoptForUser(subject);
  }, [adoptForUser, subject]);
  useEffect(() => {
    if (!hasDraftHydrated || form.formState.isDirty) return;
    form.reset({ text: draftText, mood: draftMood, sleepQuality: draftSleepQuality, tags: draftTags, occurredAt: draftOccurredAt });
  }, [draftMood, draftOccurredAt, draftSleepQuality, draftTags, draftText, form, hasDraftHydrated]);
  useEffect(() => {
    const subscription = form.watch((values) => {
      if (!hasDraftHydrated) return;
      setDraftFields({
        text: values.text ?? "",
        mood: values.mood ?? "",
        sleepQuality: values.sleepQuality ?? "",
        tags: values.tags ?? "",
        occurredAt: values.occurredAt ?? ""
      });
    });
    return () => subscription.unsubscribe();
  }, [form, hasDraftHydrated, setDraftFields]);
  const submitDream = useDreamSubmission(onSubmitted);
  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await submitDream.submit(toSubmitDreamRequest(values));
    } catch {
      // The mutation state renders the actionable API error in the form.
    }
  });

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen}>
        <BrandMark detail="Capture while it is still close." />
        <View style={[styles.hero, { backgroundColor: theme.colors.lavender }]}>
          <Text accessibilityRole="header" style={[styles.title, { color: theme.colors.text }]}>What stayed with you?</Text>
          <Text style={[styles.subtitle, { color: theme.colors.mutedText }]}>Start with one image, feeling, place, or person. The details can arrive later.</Text>
        </View>
        <View style={[styles.form, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
          <Field control={form.control} label="Dream text" name="text" multiline placeholder="I remember..." />
          <VoiceCapturePanel onTranscript={(transcript) => form.setValue("text", transcript, { shouldDirty: true, shouldValidate: true })} />
          <DreamChoiceField control={form.control} label="Mood" name="mood" options={moodOptions} />
          <DreamScaleField control={form.control} label="Sleep quality" name="sleepQuality" />
          <DreamTagField control={form.control} label="Tags" name="tags" placeholder="Add a tag" />
          <Field control={form.control} label="Occurred at" name="occurredAt" placeholder="2026-07-01" />
           {submitDream.isError ? <ErrorMessage error={submitDream.error} /> : null}
           {submitDream.isPending ? <DreamingFacts label="Preparing your private interpretation" /> : null}
           <Pressable accessibilityRole="button" disabled={submitDream.isPending} onPress={onSubmit} testID="submit-dream" style={[styles.button, { backgroundColor: theme.colors.primary }]}>
            <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{submitDream.isPending ? "Interpreting" : "Interpret dream"}</Text>
          </Pressable>
        </View>
        <Text style={[styles.disclaimer, { color: theme.colors.mutedText }]}>Your dream is private and processed by AI to create your interpretation. Dream DNA is for reflection and entertainment, not medical or mental health advice.</Text>
      </ScrollView>
    </AppShell>
  );
}

function Field({ control, label, name, multiline, keyboardType, placeholder }: { control: ReturnType<typeof useForm<DreamCaptureValues>>["control"]; label: string; name: keyof DreamCaptureValues; multiline?: boolean; keyboardType?: "default" | "number-pad"; placeholder?: string }) {
  const theme = useTheme();
  return <Controller control={control} name={name} render={({ field, fieldState }) => <View style={[styles.field, multiline ? styles.wideField : null]}><Text style={[styles.label, { color: theme.colors.text }]}>{label}</Text><TextInput accessibilityLabel={label} keyboardType={keyboardType} multiline={multiline} onChangeText={field.onChange} placeholder={placeholder} placeholderTextColor={theme.colors.mutedText} style={[multiline ? styles.textArea : styles.input, { backgroundColor: theme.colors.background, borderColor: theme.colors.border, color: theme.colors.text }]} testID={`dream-${String(name)}`} textAlignVertical={multiline ? "top" : "center"} value={field.value} />{fieldState.error ? <Text style={[styles.error, { color: theme.colors.warning }]}>{fieldState.error.message}</Text> : null}</View>} />;
}

function DreamChoiceField({
  control,
  label,
  name,
  options
}: {
  control: ReturnType<typeof useForm<DreamCaptureValues>>["control"];
  label: string;
  name: "mood";
  options: ChoiceOption[];
}) {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <ChoiceSet
          error={fieldState.error?.message}
          label={label}
          onChange={field.onChange}
          options={options}
          testID={`dream-${name}`}
          value={field.value ?? ""}
        />
      )}
    />
  );
}

function DreamScaleField({
  control,
  label,
  name
}: {
  control: ReturnType<typeof useForm<DreamCaptureValues>>["control"];
  label: string;
  name: "sleepQuality";
}) {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <FivePointScale
          error={fieldState.error?.message}
          label={label}
          onChange={field.onChange}
          testID={`dream-${name}`}
          value={field.value ?? ""}
        />
      )}
    />
  );
}

function DreamTagField({
  control,
  label,
  name,
  placeholder
}: {
  control: ReturnType<typeof useForm<DreamCaptureValues>>["control"];
  label: string;
  name: "tags";
  placeholder?: string;
}) {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <TagEditor
          error={fieldState.error?.message}
          label={label}
          onChange={field.onChange}
          placeholder={placeholder}
          testID={`dream-${name}`}
          value={field.value ?? ""}
        />
      )}
    />
  );
}

function ErrorMessage({ error }: { error: Error }) {
  const theme = useTheme();
  return <Text accessibilityRole="alert" style={[styles.error, { color: theme.colors.warning }]}>{dreamSubmissionErrorMessage(error)}</Text>;
}

const styles = StyleSheet.create({
  screen: { gap: 16, padding: 20, paddingBottom: 28 },
  hero: { gap: 8, padding: 24, marginHorizontal: -20 },
  title: { fontSize: 29, fontWeight: "700", lineHeight: 35 },
  subtitle: { fontSize: 15, lineHeight: 22 },
  form: { gap: 22, paddingVertical: 18 },
  field: { flex: 1, gap: 6 },
  wideField: { flexBasis: "100%" },
  label: { fontSize: 13, fontWeight: "800" },
  input: { borderRadius: 6, borderWidth: 1, fontSize: 15, minHeight: 46, paddingHorizontal: 12 },
  textArea: { borderRadius: 6, borderWidth: 1, fontSize: 16, lineHeight: 23, minHeight: 180, padding: 13 },
  button: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 52, paddingHorizontal: 16 },
  buttonText: { fontSize: 16, fontWeight: "800" },
  error: { fontSize: 13, lineHeight: 18 },
  disclaimer: { fontSize: 12, lineHeight: 18, paddingHorizontal: 4 }
});
