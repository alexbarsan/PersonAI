import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { Controller, useForm } from "react-hook-form";
import { Pressable, ScrollView, StyleSheet, Switch, Text, TextInput, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ProfileResponse } from "@/api/dto";
import { AppShell, BrandMark } from "@/components/AppShell";
import { ChoiceOption, ChoiceSet, FivePointScale, TagEditor } from "@/components/FieldControls";
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

const sexOptions: ChoiceOption[] = [
  { label: "Female", value: "female" },
  { label: "Male", value: "male" },
  { label: "Intersex", value: "intersex" },
  { label: "Prefer not to say", value: "" }
];

const relationshipOptions: ChoiceOption[] = [
  { label: "Single", value: "single" },
  { label: "Partnered", value: "partnered" },
  { label: "Married", value: "married" },
  { label: "Other", value: "other" }
];

const sleepPatternOptions: ChoiceOption[] = [
  { label: "Regular", value: "regular" },
  { label: "Light", value: "light" },
  { label: "Restless", value: "restless" },
  { label: "Shift work", value: "shift-work" }
];

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
    <AppShell showNavigation={mode === "profile"}>
      <ScrollView contentContainerStyle={styles.screen}>
      <BrandMark detail={mode === "onboarding" ? "A few details make your reflections more personal." : "Only share what feels useful to you."} />
      <View style={[styles.header, { backgroundColor: theme.colors.lavender }]}>
        <Text style={[styles.title, { color: theme.colors.text }]}>
          {mode === "onboarding" ? "Set up Dream DNA" : "Profile"}
        </Text>
        <Text style={[styles.disclaimer, { color: theme.colors.mutedText }]} testID="wellness-disclaimer">
          Dream DNA is for reflection and entertainment. It is not medical, mental health, or safety advice.
        </Text>
      </View>

      <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
        <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Basics</Text>
        <Field control={form.control} label="Age" name="age" keyboardType="number-pad" placeholder="33" />
        <Field control={form.control} label="Language" name="language" placeholder="en" />
        <Field control={form.control} label="Timezone" name="timezone" placeholder="Europe/Bucharest" />
        <ChoiceField control={form.control} label="Sex" name="sex" options={sexOptions} />
        <Field control={form.control} label="Gender identity" name="genderIdentity" />

        <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Traits</Text>
        <TagField control={form.control} label="Fears" name="fears" placeholder="Add a fear" />
        <TagField control={form.control} label="Allergies" name="allergies" placeholder="Add an allergy" />
        <TagField control={form.control} label="Interests" name="interests" placeholder="Add an interest" />
        <Field control={form.control} label="Occupation" name="occupation" />
        <ChoiceField control={form.control} label="Relationship status" name="relationshipStatus" options={relationshipOptions} />
        <Field control={form.control} label="Cultural background" name="culturalBackground" />
        <ChoiceField control={form.control} label="Sleep pattern" name="sleepPattern" options={sleepPatternOptions} />
        <ScaleField control={form.control} label="Stress level" name="stressLevel" />
        <TagField control={form.control} label="Recent life events" name="recentLifeEvents" placeholder="Add a recent event" />

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
          testID="save-profile"
          style={[styles.button, { backgroundColor: theme.colors.primary }]}
        >
          <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>
            {saveProfile.isPending ? "Saving" : "Save profile"}
          </Text>
        </Pressable>
      </View>
      {mode === "profile" ? <PrivacyActions /> : null}
      </ScrollView>
    </AppShell>
  );
}

function PrivacyActions() {
  const api = useApiClient();
  const theme = useTheme();
  const entitlement = useQuery({ queryKey: ["entitlements"], queryFn: () => api.getEntitlements() });
  const exportData = useMutation({
    mutationFn: () => api.exportUserData(),
    onSuccess: (data) => downloadExport(data)
  });
  const anonymization = useMutation({ mutationFn: () => api.requestAnonymization() });

  return (
    <View style={[styles.panel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
      <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Your data</Text>
      {entitlement.data?.tier === "premium" ? (
        <>
          <Pressable accessibilityRole="button" onPress={() => exportData.mutate()} style={[styles.secondaryButton, { borderColor: theme.colors.primary }]} testID="prepare-data-export">
            <Text style={[styles.secondaryButtonText, { color: theme.colors.primary }]}>{exportData.isPending ? "Preparing export" : "Prepare data export"}</Text>
          </Pressable>
          {exportData.data ? <Text style={[styles.helpText, { color: theme.colors.mutedText }]}>Export downloaded with {exportData.data.dreams.length} dream(s).</Text> : null}
        </>
      ) : (
        <Text style={[styles.helpText, { color: theme.colors.mutedText }]}>Data export is available with Premium.</Text>
      )}
      <Pressable accessibilityRole="button" onPress={() => anonymization.mutate()} style={[styles.secondaryButton, { borderColor: theme.colors.warning }]} testID="request-anonymization">
        <Text style={[styles.secondaryButtonText, { color: theme.colors.warning }]}>{anonymization.isPending ? "Requesting approval" : "Request anonymization"}</Text>
      </Pressable>
      {anonymization.data ? <Text style={[styles.helpText, { color: theme.colors.mutedText }]}>Anonymization request is pending administrator approval.</Text> : null}
      {anonymization.isError ? <Text style={[styles.error, { color: theme.colors.warning }]}>Anonymization request could not be created.</Text> : null}
    </View>
  );
}

function downloadExport(data: unknown) {
  if (typeof document === "undefined") {
    return;
  }

  const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "dream-dna-data-export.json";
  link.click();
  URL.revokeObjectURL(url);
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
            testID={`profile-${String(name)}`}
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

function ChoiceField({
  control,
  label,
  name,
  options
}: {
  control: ReturnType<typeof useForm<ProfileFormValues>>["control"];
  label: string;
  name: "sex" | "relationshipStatus" | "sleepPattern";
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
          testID={`profile-${name}`}
          value={String(field.value ?? "")}
        />
      )}
    />
  );
}

function ScaleField({
  control,
  label,
  name
}: {
  control: ReturnType<typeof useForm<ProfileFormValues>>["control"];
  label: string;
  name: "stressLevel";
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
          testID={`profile-${name}`}
          value={String(field.value ?? "")}
        />
      )}
    />
  );
}

function TagField({
  control,
  label,
  name,
  placeholder
}: {
  control: ReturnType<typeof useForm<ProfileFormValues>>["control"];
  label: string;
  name: "fears" | "allergies" | "interests" | "recentLifeEvents";
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
          testID={`profile-${name}`}
          value={String(field.value ?? "")}
        />
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
              testID={`profile-${String(name)}`}
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
    borderRadius: 8,
    gap: 10,
    padding: 18
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
  },
  secondaryButton: {
    alignItems: "center",
    borderRadius: 8,
    borderWidth: 1,
    justifyContent: "center",
    minHeight: 44,
    paddingHorizontal: 16
  },
  secondaryButtonText: {
    fontSize: 15,
    fontWeight: "700"
  }
});
