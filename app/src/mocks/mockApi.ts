import type { ApiClient } from "@/api/client";
import { ApiError } from "@/api/errors";
import { mockAdminOperations, mockAnonymizationRequest, mockAskDreams, mockAskDreamMemoryStatus, mockDailyDreamContent, mockDeepInterpretation, mockDream, mockDreamFeedback, mockDreamImage, mockDreamObservation, mockEntitlement, mockInsights, mockJournal, mockMe, mockProfile, mockSensitiveSafetyReviews, mockUserDataExport } from "@/mocks/mockData";

export const mockApiClient: ApiClient = {
  getDailyDreamContent: async () => mockDailyDreamContent,
  getMe: async () => mockMe,
  getProfile: async () => mockProfile,
  updateProfile: async (request) => request,
  submitDream: async (request) => {
    if (readMockSubmitMode() === "provider-failure") {
      throw new ApiError("Mock provider failure", 503, {
        error: "provider_failure"
      });
    }

    return { ...mockDream, text: request.text };
  },
  askDreams: async () => mockAskDreams,
  getAskDreamMemoryStatus: async () => mockAskDreamMemoryStatus,
  listDreams: async (filters = {}) => {
    const filtered = mockJournal.items.filter(item => {
      const date = (item.occurredAt ?? item.createdAt).slice(0, 10);
      return (!filters.query || `${item.title} ${item.summary ?? ""}`.toLowerCase().includes(filters.query.toLowerCase()))
        && (!filters.mood || item.mood?.toLowerCase() === filters.mood.toLowerCase())
        && (!filters.tag || (item.id === mockDream.id && mockDream.tags?.some(tag => tag.toLowerCase() === filters.tag!.toLowerCase())))
        && (!filters.from || date >= filters.from)
        && (!filters.to || date <= filters.to);
    });
    const page = filters.page ?? 1;
    const pageSize = filters.pageSize ?? 25;
    return {
      items: filtered.slice((page - 1) * pageSize, page * pageSize),
      total: filtered.length,
      hasMore: page * pageSize < filtered.length
    };
  },
  getDream: async () => mockDream,
  retryDreamInterpretation: async () => mockDream,
  cancelDreamInterpretation: async () => ({ ...mockDream, status: "canceled", result: null, processing: null }),
  getDreamFeedback: async () => mockDreamFeedback,
  getDeepInterpretation: async () => mockDeepInterpretation,
  createDeepInterpretation: async () => mockDeepInterpretation,
  updateDreamFeedback: async (_, request) => ({
    rating: request.rating,
    reasons: request.reasons ?? [],
    details: request.details ?? null,
    updatedAt: "2026-09-05T08:00:00Z"
  }),
  requestDreamImage: async () => mockDreamImage,
    getDreamImage: async () => mockDreamImage,
    waitForDreamImage: async () => mockDreamImage,
  getInsights: async () => mockInsights,
  getDreamObservation: async () => mockDreamObservation,
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
