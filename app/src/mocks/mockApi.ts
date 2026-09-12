import type { ApiClient } from "@/api/client";
import { ApiError } from "@/api/errors";
import { mockAdminOperations, mockAnonymizationRequest, mockAskDreams, mockDeepInterpretation, mockDream, mockDreamFeedback, mockDreamImage, mockEntitlement, mockInsights, mockJournal, mockMe, mockProfile, mockSensitiveSafetyReviews, mockUserDataExport } from "@/mocks/mockData";

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
  }),
  getAdminOperations: async () => mockAdminOperations,
  requeueAdminJob: async (id) => ({ targetId: id, jobId: id, action: "requeue", status: "pending", createdAt: new Date().toISOString() }),
  acknowledgeAdminIssue: async (source, id) => ({ targetId: id, jobId: source === "job" ? id : null, action: "acknowledge", status: "acknowledged", createdAt: new Date().toISOString() }),
  searchAdminDreams: async () => ({
    page: 1,
    pageSize: 50,
    total: 1,
    items: [{ id: mockDream.id, subjectPseudonym: "dreamer_mock", createdAt: mockDream.createdAt, occurredAt: mockDream.occurredAt ?? null, status: mockDream.status, mood: "curious", tags: ["river"], summary: mockDream.result?.summary ?? null, imageCount: 1, latestImageStatus: "completed" }]
  }),
  accessAdminDream: async () => ({
    id: mockDream.id,
    subjectPseudonym: "dreamer_mock",
    createdAt: mockDream.createdAt,
    occurredAt: mockDream.occurredAt ?? null,
    status: mockDream.status,
    text: "I followed a river through a quiet city at dawn.",
    mood: "curious",
    sleepQuality: 4,
    tags: ["river"],
    journalNote: null,
    interpretation: mockDream.result,
    deepInterpretation: mockDeepInterpretation.result,
    images: [{ id: mockDreamImage.id, status: mockDreamImage.status, style: mockDreamImage.style, downloadUrl: mockDreamImage.downloadUrl, createdAt: mockDreamImage.createdAt }]
  })
};

function readMockSubmitMode() {
  if (typeof globalThis.localStorage === "undefined") {
    return null;
  }

  return globalThis.localStorage.getItem("dreamlens.mockSubmitMode");
}
