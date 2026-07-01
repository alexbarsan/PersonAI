import { z } from "zod";

export const dreamCaptureSchema = z.object({
  text: z
    .string()
    .trim()
    .min(10, "Dream text must be at least 10 characters.")
    .max(4000, "Dream text must be 4000 characters or fewer."),
  mood: z.string().optional(),
  sleepQuality: z
    .string()
    .optional()
    .refine((value) => !value || /^[1-5]$/.test(value), "Sleep quality must be between 1 and 5."),
  tags: z.string().optional(),
  occurredAt: z.string().optional()
});

export type DreamCaptureValues = z.infer<typeof dreamCaptureSchema>;

export const defaultDreamCaptureValues: DreamCaptureValues = {
  text: "",
  mood: "",
  sleepQuality: "",
  tags: "",
  occurredAt: ""
};
