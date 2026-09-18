import { ApiError } from "@/api/errors";

export function dreamSubmissionErrorMessage(error: Error) {
  if (error instanceof ApiError) {
    if (error.status === 401 || error.status === 403) return "Please sign in again before submitting a dream.";
    if (error.status === 429) return "You have reached today's dream limit. Try again tomorrow.";
    if (error.status === 503) return "The interpretation service is temporarily unavailable. Please try again.";
    if (error.status === 400) return readValidationMessage(error.body) ?? "Review your dream and profile details, then try again.";
  }
  return "Dream submission failed. Please try again.";
}

function readValidationMessage(body: unknown) {
  if (!body || typeof body !== "object" || Array.isArray(body)) return null;
  const errors = body as Record<string, unknown>;
  for (const key of ["submission_rejected", "profile", "consent", "text", "sleepQuality"]) {
    const value = errors[key];
    if (Array.isArray(value) && typeof value[0] === "string") return value[0];
  }
  return null;
}
