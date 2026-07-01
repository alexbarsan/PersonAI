import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { Controller, useForm } from "react-hook-form";
import { Pressable, ScrollView, StyleSheet, Switch, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ProfileResponse } from "@/api/dto";
import { toProfileFormValues, toProfileUpdateRequest } from "@/features/profile/profileMapping";
import {
  defaultProfileFormValues,
  ProfileFormValues,
  profileFormSchema
} from "@/features/profile/profileSchema";
import { useOnboardingDraftStore } from "@/state/onboardingDraftStore";
import { useTheme } from "@/theme/ThemeProvider";

type ProfileFormProps = {
  mode: "onboarding" | "profile";
};

export function ProfileForm({ mode }: ProfileFormProps) {
  const api = useApiClient();
  const theme = useTheme();
  const queryClient = useQueryClient();
  const draft = useOnboardingDraftStore((state) => state.values);
  const setDraft = useOnboardingDraftStore((state) => state.setValues);
  const resetDraft = useOnboardingDraftStore((state) => state.reset);
  const profile = useQuery({
    queryKey: ["profile"],
    queryFn: () => api.getProfile(),
    enabled: mode === "profile"
  });
  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    values: mode === "profile" ? toProfileFormValues(profile.data) : draft,
    defaultValues: defaultProfileFormValues
  });
  const saveProfile = useMutation({
    mutationFn: (values: ProfileFormValues) => api.updateProfile(toProfileUpdateRequest(values)),
    onSuccess: (saved) => {
      queryClient.setQueryData<ProfileResponse>(["profile"], saved);
      resetDraft();
      router.replace("/");
    }
  });

  const onSubmit = form.handleSubmit((values) => {
    setDraft(values);
    saveProfile.mutate(values);
  });

  return (
    <ScrollView contentContainerStyle={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <View style={styles.header}>
        <Text style={[styles.title, { color: theme.colors.text }]}>
          {mode === "onboarding" ? "Set up DreamLens" : "Profile"}
        </Text>
        <Text style={[styles.disclaimer, { color: theme.colors.warning }]} testID="wellness-disclaimer">
          DreamLens is for reflection and entertainment. It is not medical, mental health, or safety advice.
        </Text>
      </View>

      <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
        <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Basics</Text>
        <Field control={form.control} label="Age" name="age" keyboardType="number-pad" />
        <Field control={form.control} label="Language" name="language" />
        <Field control={form.control} label="Timezone" name="timezone" />
        <Field control={form.control} label="Sex" name="sex" />
        <Field control={form.control} label="Gender identity" name="genderIdentity" />

        <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Traits</Text>
        <Field control={form.control} label="Fears" name="fears" placeholder="deep water, exams" />
        <Field control={form.control} label="Allergies" name="allergies" />
        <Field control={form.control} label="Interests" name="interests" placeholder="journaling, hiking" />
        <Field control={form.control} label="Occupation" name="occupation" />
        <Field control={form.control} label="Relationship status" name="relationshipStatus" />
        <Field control={form.control} label="Cultural background" name="culturalBackground" />
        <Field control={form.control} label="Sleep pattern" name="sleepPattern" />
        <Field control={form.control} label="Stress level" name="stressLevel" />
        <Field control={form.control} label="Recent life events" name="recentLifeEvents" />

        <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Consent</Text>
        <Consent control={form.control} label="AI processing" name="consentAiProcessing" required />
        <Consent control={form.control} label="Sensitive traits" name="consentSensitiveTraits" />
        <Consent control={form.control} label="History use" name="consentHistoryUse" />

        {saveProfile.isError ? (
          <Text style={[styles.error, { color: theme.colors.warning }]}>Profile could not be saved.</Text>
        ) : null}

        <Pressable
          accessibilityRole="button"
          onPress={onSubmit}
          style={[styles.button, { backgroundColor: theme.colors.primary }]}
        >
          <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>
            {saveProfile.isPending ? "Saving" : "Save profile"}
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
  keyboardType,
  placeholder
}: {
  control: ReturnType<typeof useForm<ProfileFormValues>>["control"];
  label: string;
  name: keyof ProfileFormValues;
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
            onChangeText={field.onChange}
            placeholder={placeholder}
            placeholderTextColor={theme.colors.mutedText}
            style={[styles.input, { borderColor: theme.colors.border, color: theme.colors.text }]}
            value={String(field.value ?? "")}
          />
          {fieldState.error ? (
            <Text style={[styles.error, { color: theme.colors.warning }]}>{fieldState.error.message}</Text>
          ) : null}
        </View>
      )}
    />
  );
}

function Consent({
  control,
  label,
  name,
  required = false
}: {
  control: ReturnType<typeof useForm<ProfileFormValues>>["control"];
  label: string;
  name: keyof ProfileFormValues;
  required?: boolean;
}) {
  const theme = useTheme();
  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <View style={styles.field}>
          <View style={styles.consentRow}>
            <View style={styles.consentText}>
              <Text style={[styles.label, { color: theme.colors.text }]}>{label}</Text>
              <Text style={[styles.helpText, { color: theme.colors.mutedText }]}>
                {required ? "Required to interpret dreams." : "You can change this later."}
              </Text>
            </View>
            <Switch
              accessibilityLabel={label}
              onValueChange={field.onChange}
              value={Boolean(field.value)}
            />
          </View>
          {fieldState.error ? (
            <Text style={[styles.error, { color: theme.colors.warning }]}>{fieldState.error.message}</Text>
          ) : null}
        </View>
      )}
    />
  );
}

const styles = StyleSheet.create({
  screen: {
    gap: 20,
    padding: 20,
    paddingBottom: 48
  },
  header: {
    gap: 10
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
  sectionTitle: {
    fontSize: 18,
    fontWeight: "700"
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
  error: {
    fontSize: 13,
    lineHeight: 18
  },
  helpText: {
    fontSize: 13,
    lineHeight: 18
  },
  consentRow: {
    alignItems: "center",
    flexDirection: "row",
    gap: 16,
    justifyContent: "space-between"
  },
  consentText: {
    flex: 1,
    gap: 2
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
