import { SubmitDreamRequest } from "@/api/dto";
import { DreamCaptureValues } from "@/features/dreams/dreamCaptureSchema";

export function toSubmitDreamRequest(values: DreamCaptureValues): SubmitDreamRequest {
  return {
    text: values.text.trim(),
    mood: emptyToNull(values.mood),
    sleepQuality: values.sleepQuality ? Number(values.sleepQuality) : null,
    tags: splitList(values.tags),
    occurredAt: emptyToNull(values.occurredAt)
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

function emptyToNull(value?: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}
