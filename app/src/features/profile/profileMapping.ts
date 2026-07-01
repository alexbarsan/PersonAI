import { ProfileResponse, ProfileUpdateRequest } from "@/api/dto";
import { defaultProfileFormValues, ProfileFormValues } from "@/features/profile/profileSchema";

export function toProfileUpdateRequest(values: ProfileFormValues): ProfileUpdateRequest {
  return {
    age: Number(values.age),
    sex: emptyToNull(values.sex),
    genderIdentity: emptyToNull(values.genderIdentity),
    language: values.language.trim(),
    timezone: values.timezone.trim(),
    traits: {
      fears: splitList(values.fears),
      allergies: splitList(values.allergies),
      interests: splitList(values.interests),
      occupation: emptyToNull(values.occupation),
      relationshipStatus: emptyToNull(values.relationshipStatus),
      culturalBackground: emptyToNull(values.culturalBackground),
      sleepPattern: emptyToNull(values.sleepPattern),
      stressLevel: emptyToNull(values.stressLevel),
      recentLifeEvents: splitList(values.recentLifeEvents)
    },
    consent: {
      aiProcessing: values.consentAiProcessing,
      sensitiveTraits: values.consentSensitiveTraits,
      historyUse: values.consentHistoryUse
    }
  };
}

export function toProfileFormValues(profile?: ProfileResponse | null): ProfileFormValues {
  if (!profile) {
    return defaultProfileFormValues;
  }

  return {
    age: profile.age?.toString() ?? defaultProfileFormValues.age,
    sex: profile.sex ?? "",
    genderIdentity: profile.genderIdentity ?? "",
    language: profile.language,
    timezone: profile.timezone,
    fears: joinList(profile.traits.fears),
    allergies: joinList(profile.traits.allergies),
    interests: joinList(profile.traits.interests),
    occupation: profile.traits.occupation ?? "",
    relationshipStatus: profile.traits.relationshipStatus ?? "",
    culturalBackground: profile.traits.culturalBackground ?? "",
    sleepPattern: profile.traits.sleepPattern ?? "",
    stressLevel: profile.traits.stressLevel ?? "",
    recentLifeEvents: joinList(profile.traits.recentLifeEvents),
    consentAiProcessing: profile.consent.aiProcessing,
    consentSensitiveTraits: profile.consent.sensitiveTraits,
    consentHistoryUse: profile.consent.historyUse
  };
}

function splitList(value?: string) {
  return (
    value
      ?.split(",")
      .map((item) => item.trim())
      .filter(Boolean) ?? []
  );
}

function joinList(values: string[]) {
  return values.join(", ");
}

function emptyToNull(value?: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}
