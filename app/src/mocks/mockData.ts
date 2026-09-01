import {
  AskDreamsResponse,
  DreamJournalResponse,
  AnonymizationRequestResponse,
  DreamImageResponse,
  DreamResponse,
  EntitlementResponse,
  InsightsResponse,
  MeResponse,
  ProfileResponse
} from "@/api/dto";

export const mockAskDreams: AskDreamsResponse = {
  answer: "Water appears alongside moments of transition in the dreams currently indexed.",
  observations: ["The river dream connects water with curiosity rather than immediate danger."],
  caveat: "This is a reflective pattern from a small sample, not a diagnosis or prediction.",
  sources: [{
    id: "dream_mock_1",
    summary: "The dream points to uncertainty and a wish for steadier ground.",
    occurredAt: "2026-07-01",
    createdAt: "2026-07-01T08:00:00Z"
  }],
  sampleSize: 1
};

export const mockMe: MeResponse = {
  subject: "mock-user",
  email: "mock@dreamlens.local",
  displayName: "Mock Dreamer",
  authenticationScheme: "Mock"
};

export const mockProfile: ProfileResponse = {
  age: 33,
  sex: null,
  genderIdentity: null,
  language: "en",
  timezone: "America/New_York",
  traits: {
    fears: ["deep water"],
    allergies: [],
    interests: ["journaling"],
    occupation: null,
    relationshipStatus: null,
    culturalBackground: null,
    sleepPattern: "irregular",
    stressLevel: "medium",
    recentLifeEvents: ["new job"]
  },
  consent: {
    aiProcessing: true,
    sensitiveTraits: true,
    historyUse: true
  }
};

export const mockDream: DreamResponse = {
  id: "dream_mock_1",
  createdAt: "2026-07-01T08:00:00Z",
  status: "completed",
  result: {
    summary: "The dream points to uncertainty and a wish for steadier ground.",
    sections: [
      {
        kind: "symbols",
        title: "Symbols",
        content: [{ symbol: "water", meaning: "Unclear emotional depth" }]
      },
      {
        kind: "list",
        title: "Themes",
        content: ["transition", "loss of control"]
      },
      {
        kind: "text",
        title: "Guidance",
        content: "Try writing one concrete detail you remember before interpreting the whole dream."
      },
      {
        kind: "emotions",
        title: "Emotions",
        content: [{ name: "curiosity", intensity: 0.6, evidence: "The dream stays reflective rather than urgent." }]
      }
    ],
    followUpQuestions: ["What changed near the water?"],
    safety: {
      selfHarmRisk: "none",
      notes: ""
    }
  },
  errorMessage: null
};

export const mockDreamImage: DreamImageResponse = {
  id: "dream_image_mock_1",
  dreamId: mockDream.id,
  status: "completed",
  style: "SOFT_DIGITAL_PAINTING",
  jobId: "job_image_mock_1",
  downloadUrl: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL2ngAAAABJRU5ErkJggg==",
  errorMessage: null,
  createdAt: mockDream.createdAt
};

export const mockJournal: DreamJournalResponse = {
  items: [
    {
      id: mockDream.id,
      createdAt: mockDream.createdAt,
      status: mockDream.status,
      summary: mockDream.result?.summary ?? null,
      mood: "curious",
      occurredAt: "2026-07-01"
    }
  ]
};

export const mockInsights: InsightsResponse = {
  totalDreams: 1,
  currentStreakDays: 1,
  recurringThemes: [
    {
      name: "transition",
      count: 1
    }
  ],
  dateRange: { start: "2026-07-01", end: "2026-07-01" },
  factGroups: [
    {
      type: "symbol",
      title: "Recurring symbols",
      facts: [{ value: "water", count: 1, percentageOfDreams: 100, averageScore: null }]
    },
    {
      type: "emotion",
      title: "Frequent emotions",
      facts: [{ value: "curiosity", count: 1, percentageOfDreams: 100, averageScore: 0.7 }]
    }
  ],
  timingPatterns: [],
  monthlyDreamCounts: [{ month: "2026-07-01", count: 1 }]
};

export const mockEntitlement: EntitlementResponse = {
  tier: "free",
  dailyDreamLimit: 3,
  deepAnalysisEnabled: false
};

export const mockAnonymizationRequest: AnonymizationRequestResponse = {
  id: "anonymization_mock_1",
  status: "pending",
  requestedAt: "2026-08-29T19:00:00Z",
  reviewedAt: null,
  completedAt: null
};

export const mockUserDataExport = {
  generatedAt: "2026-08-29T19:00:00Z",
  profile: mockProfile,
  dreams: [{ id: mockDream.id, text: "I was near dark water." }],
  aiOperations: [{ id: "ai_operation_mock_1", operationType: "dream.interpretation", estimatedCostUsd: 0.001 }]
};
