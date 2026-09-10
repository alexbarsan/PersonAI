import type { ApiClient } from "@/api/client";
import { ApiError } from "@/api/errors";
import { mockAnonymizationRequest, mockAskDreams, mockDeepInterpretation, mockDream, mockDreamFeedback, mockDreamImage, mockEntitlement, mockInsights, mockJournal, mockMe, mockProfile, mockSensitiveSafetyReviews, mockUserDataExport } from "@/mocks/mockData";

export const mockApiClient: ApiClient = {
  getMe: async () => mockMe,
  getProfile: async () => mockProfile,
  updateProfile: async (request) => request,
  submitDream: async () => {
    if (readMockSubmitMode() === "provider-failure") {
      throw new ApiError("Mock provider failure", 503, {
        error: "provider_failure"
      });
    }

    return mockDream;
  },
  askDreams: async () => mockAskDreams,
  listDreams: async () => mockJournal,
  getDream: async () => mockDream,
  getDreamFeedback: async () => mockDreamFeedback,
  getDeepInterpretation: async () => mockDeepInterpretation,
  createDeepInterpretation: async () => mockDeepInterpretation,
  updateDreamFeedback: async (_, request) => ({
    rating: request.rating,
    reasons: request.reasons ?? [],
    details: request.details ?? null,
    updatedAt: "2026-09-05T08:00:00Z"
  }),
  updateDreamJournal: async (_, request) => ({ ...mockDream, ...request }),
  requestDreamImage: async () => mockDreamImage,
  getDreamImage: async () => mockDreamImage,
  deleteDream: async () => undefined,
  getInsights: async () => mockInsights,
  getEntitlements: async () => mockEntitlement,
  exportUserData: async () => mockUserDataExport,
  requestAnonymization: async () => mockAnonymizationRequest,
  uploadVoiceCapture: async () => ({
    id: "voice-1",
    status: "completed",
    durationSeconds: 12,
    retainRecording: false,
    transcript: "I was walking beside a quiet river at dawn.",
    recordingUrl: null,
    jobId: "voice-job-1",
    errorMessage: null,
    createdAt: "2026-08-30T00:00:00Z"
  }),
  getVoiceCapture: async () => ({
    id: "voice-1",
    status: "completed",
    durationSeconds: 12,
    retainRecording: false,
    transcript: "I was walking beside a quiet river at dawn.",
    recordingUrl: null,
    jobId: "voice-job-1",
    errorMessage: null,
    createdAt: "2026-08-30T00:00:00Z"
  }),
  listSensitiveSafetyReviews: async () => mockSensitiveSafetyReviews,
  acknowledgeSensitiveSafetyReview: async (id) => ({ ...mockSensitiveSafetyReviews.find((review) => review.id === id)!, status: "acknowledged" }),
  accessSensitiveSafetyReviewRawText: async (id) => ({
    safetyEventId: id,
    dreamId: mockDream.id,
    dreamText: "A mock sensitive dream is available only to test the explicit access path.",
    expiresAt: mockSensitiveSafetyReviews[0].expiresAt
  })
};

function readMockSubmitMode() {
  if (typeof globalThis.localStorage === "undefined") {
    return null;
  }

  return globalThis.localStorage.getItem("dreamlens.mockSubmitMode");
}
