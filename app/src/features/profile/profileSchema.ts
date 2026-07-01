import { z } from "zod";

export const profileFormSchema = z.object({
  age: z
    .string()
    .min(1, "Age is required.")
    .regex(/^\d+$/, "Age must be a whole number.")
    .refine((value) => Number(value) >= 13, "Age must be at least 13.")
    .refine((value) => Number(value) <= 120, "Age must be 120 or less."),
  sex: z.string().optional(),
  genderIdentity: z.string().optional(),
  language: z.string().min(2, "Language is required."),
  timezone: z.string().min(2, "Timezone is required."),
  fears: z.string().optional(),
  allergies: z.string().optional(),
  interests: z.string().optional(),
  occupation: z.string().optional(),
  relationshipStatus: z.string().optional(),
  culturalBackground: z.string().optional(),
  sleepPattern: z.string().optional(),
  stressLevel: z.string().optional(),
  recentLifeEvents: z.string().optional(),
  consentAiProcessing: z.boolean().refine((value) => value, "AI processing consent is required."),
  consentSensitiveTraits: z.boolean(),
  consentHistoryUse: z.boolean()
});

export type ProfileFormValues = z.infer<typeof profileFormSchema>;

export const defaultProfileFormValues: ProfileFormValues = {
  age: "33",
  sex: "",
  genderIdentity: "",
  language: "en",
  timezone: "America/New_York",
  fears: "",
  allergies: "",
  interests: "",
  occupation: "",
  relationshipStatus: "",
  culturalBackground: "",
  sleepPattern: "",
  stressLevel: "",
  recentLifeEvents: "",
  consentAiProcessing: true,
  consentSensitiveTraits: true,
  consentHistoryUse: true
};
