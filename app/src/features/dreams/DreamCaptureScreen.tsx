import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { router } from "expo-router";
import { Controller, useForm } from "react-hook-form";
import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/client";
import { toSubmitDreamRequest } from "@/features/dreams/dreamCaptureMapping";
import {
  defaultDreamCaptureValues,
  DreamCaptureValues,
  dreamCaptureSchema
} from "@/features/dreams/dreamCaptureSchema";
import { useDreamDraftStore } from "@/state/dreamDraftStore";
import { useDreamResultStore } from "@/state/dreamResultStore";
import { useTheme } from "@/theme/ThemeProvider";

type DreamCaptureScreenProps = {
  onSubmitted?: (dreamId: string) => void;
};

export function DreamCaptureScreen({ onSubmitted }: DreamCaptureScreenProps) {
  const api = useApiClient();
  const theme = useTheme();
  const rememberDream = useDreamResultStore((state) => state.rememberDream);
  const draftText = useDreamDraftStore((state) => state.text);
  const draftMood = useDreamDraftStore((state) => state.mood);
  const form = useForm<DreamCaptureValues>({
    resolver: zodResolver(dreamCaptureSchema),
    defaultValues: {
      ...defaultDreamCaptureValues,
      text: draftText,
      mood: draftMood
    }
  });
  const submitDream = useMutation({
    mutationFn: (values: DreamCaptureValues) => api.submitDream(toSubmitDreamRequest(values))
  });

  const onSubmit = form.handleSubmit(async (values) => {
    const dream = await submitDream.mutateAsync(values);
    rememberDream(dream);
    if (onSubmitted) {
      onSubmitted(dream.id);
      return;
    }

    router.push(`/dreams/${dream.id}`);
  });

  return (
    <ScrollView contentContainerStyle={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>Capture dream</Text>
      <Text style={[styles.disclaimer, { color: theme.colors.warning }]}>
        DreamLens is for reflection and entertainment. It is not medical, mental health, or safety advice.
      </Text>

      <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
        <Field control={form.control} label="Dream text" name="text" multiline />
        <Field control={form.control} label="Mood" name="mood" />
        <Field control={form.control} label="Sleep quality" name="sleepQuality" keyboardType="number-pad" />
        <Field control={form.control} label="Tags" name="tags" placeholder="recurring, water" />
        <Field control={form.control} label="Occurred at" name="occurredAt" placeholder="2026-07-01" />

        {submitDream.isError ? <ErrorMessage error={submitDream.error} /> : null}

        <Pressable
          accessibilityRole="button"
          onPress={onSubmit}
          testID="submit-dream"
          style={[styles.button, { backgroundColor: theme.colors.primary }]}
        >
          <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>
            {submitDream.isPending ? "Interpreting" : "Interpret dream"}
          </Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

function Field({
  control,
  label,
  name,
  multiline,
  keyboardType,
  placeholder
}: {
  control: ReturnType<typeof useForm<DreamCaptureValues>>["control"];
  label: string;
  name: keyof DreamCaptureValues;
  multiline?: boolean;
  keyboardType?: "default" | "number-pad";
  placeholder?: string;
}) {
  const theme = useTheme();
  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <View style={styles.field}>
          <Text style={[styles.label, { color: theme.colors.text }]}>{label}</Text>
          <TextInput
            accessibilityLabel={label}
            keyboardType={keyboardType}
            multiline={multiline}
            onChangeText={field.onChange}
            placeholder={placeholder}
            placeholderTextColor={theme.colors.mutedText}
            style={[
              multiline ? styles.textArea : styles.input,
              { borderColor: theme.colors.border, color: theme.colors.text }
            ]}
            testID={`dream-${String(name)}`}
            textAlignVertical={multiline ? "top" : "center"}
            value={field.value}
          />
          {fieldState.error ? (
            <Text style={[styles.error, { color: theme.colors.warning }]}>{fieldState.error.message}</Text>
          ) : null}
        </View>
      )}
    />
  );
}

function ErrorMessage({ error }: { error: Error }) {
  const theme = useTheme();
  const message = mapErrorMessage(error);
  return <Text style={[styles.error, { color: theme.colors.warning }]}>{message}</Text>;
}

function mapErrorMessage(error: Error) {
  if (error instanceof ApiError) {
    if (error.status === 401 || error.status === 403) {
      return "Please sign in again before submitting a dream.";
    }

    if (error.status === 429) {
      return "You have reached today's dream limit. Try again tomorrow.";
    }

    if (error.status === 503) {
      return "The interpretation service is temporarily unavailable. Please try again.";
    }
  }

  return "Dream submission failed. Please try again.";
}

const styles = StyleSheet.create({
  screen: {
    gap: 18,
    padding: 20,
    paddingBottom: 48
  },
  title: {
    fontSize: 30,
    fontWeight: "700"
  },
  disclaimer: {
    fontSize: 14,
    lineHeight: 20
  },
  panel: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 16,
    padding: 16
  },
  field: {
    gap: 6
  },
  label: {
    fontSize: 15,
    fontWeight: "700"
  },
  input: {
    borderRadius: 8,
    borderWidth: 1,
    fontSize: 16,
    minHeight: 46,
    paddingHorizontal: 12
  },
  textArea: {
    borderRadius: 8,
    borderWidth: 1,
    fontSize: 16,
    minHeight: 180,
    padding: 12
  },
  error: {
    fontSize: 13,
    lineHeight: 18
  },
  button: {
    alignItems: "center",
    borderRadius: 8,
    minHeight: 48,
    justifyContent: "center",
    paddingHorizontal: 16
  },
  buttonText: {
    fontSize: 16,
    fontWeight: "700"
  }
});
