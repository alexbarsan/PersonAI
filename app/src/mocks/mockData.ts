import {
  AskDreamsResponse,
  DreamJournalResponse,
  AnonymizationRequestResponse,
  DreamImageResponse,
  DreamFeedbackResponse,
  DeepInterpretationResponse,
  DreamResponse,
  EntitlementResponse,
  InsightsResponse,
  MeResponse,
  ProfileResponse,
  SensitiveSafetyReviewResponse,
  AdminOperationsResponse
} from "@/api/dto";

export const mockDailyDreamContent = {
  date: "2026-09-13",
  quote: "Dreams do not have to be solved to be worth keeping.",
  attribution: "Dream DNA editorial",
  facts: [
    "Most people dream several times a night, even when they remember none of them in the morning.",
    "Dreams often combine familiar people, places, and concerns in new arrangements.",
    "A dream journal can reveal patterns that are difficult to notice from one dream alone."
  ],
  cognitiveFacts: [
    "Memory is reconstructive, so a remembered dream can shift as it is recalled.",
    "REM is one stage in which dreams are often especially vivid.",
    "A cognitive analysis can describe possible patterns, but cannot diagnose from a dream."
  ]
};

export const mockDreamFeedback: DreamFeedbackResponse = {
  rating: null,
  reasons: [],
  details: null,
  updatedAt: null
};

export const mockAskDreams: AskDreamsResponse = {
  answer: "Water appears alongside moments of transition in the dreams currently indexed.",
  observations: ["The river dream connects water with curiosity rather than immediate danger."],
  caveat: "This is a reflective pattern from a small sample, not a diagnosis or prediction.",
  sources: [{
    id: "dream_mock_1",
    title: "The Quiet Shoreline",
    summary: "The dream points to uncertainty and a wish for steadier ground.",
    occurredAt: "2026-07-01",
    createdAt: "2026-07-01T08:00:00Z",
    retrievalRank: 1
  }],
  sampleSize: 1
};

export const mockAskDreamMemoryStatus = {
  isReady: true,
  completedDreams: 1,
  indexedDreams: 1,
  pendingDreams: 0,
  message: "Your dream memory is ready."
};

export const mockMe: MeResponse = {
  subject: "mock-user",
  email: "mock@dreamlens.local",
  displayName: "Mock Dreamer",
  authenticationScheme: "Mock"
};

export const mockProfile: ProfileResponse = {
  preferredName: "Mock Dreamer",
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
  title: "The Quiet Shoreline",
  text: "I followed a river through a quiet city at dawn.",
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
        title: "Interpretation",
        content: "The river may reflect an emotional transition that you are approaching with curiosity rather than urgency."
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

export const mockDeepInterpretation: DeepInterpretationResponse = {
  id: "deep_interpretation_mock_1",
  dreamId: mockDream.id,
  result: {
    summary: "The dream may reflect attention moving between uncertainty and a growing sense of agency.",
    sections: [
      {
        kind: "symbols",
        title: "Cognitive symbols",
        content: [{ title: "Open door", body: ["A possible representation of perceived choice and agency.", "The door appears beside the river as the dreamer considers moving forward."] }]
      }
    ],
    followUpQuestions: ["What was changing in your life when water began appearing?"],
    safety: { selfHarmRisk: "none", notes: "" }
  },
  sources: [{
    id: "dream_mock_related_1",
    summary: "A river appeared beside an open door.",
    occurredAt: "2026-06-21",
    similarity: 0.82
  }],
  model: "deepseek-v4-pro",
  createdAt: "2026-09-05T08:00:00Z"
};

export const mockDreamImage: DreamImageResponse = {
  id: "dream_image_mock_1",
  dreamId: mockDream.id,
  status: "completed",
  style: "SOFT_DIGITAL_PAINTING",
  jobId: "job_image_mock_1",
  downloadUrl: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL2ngAAAABJRU5ErkJggg==",
  errorMessage: null,
  queueWaitMilliseconds: 420,
  providerLatencyMilliseconds: 8400,
  createdAt: mockDream.createdAt,
  updatedAt: mockDream.createdAt
};

export const mockJournal: DreamJournalResponse = {
  items: [
    {
      id: mockDream.id,
      createdAt: mockDream.createdAt,
      status: mockDream.status,
      title: mockDream.title,
      summary: mockDream.result?.summary ?? null,
      mood: "curious",
      occurredAt: "2026-07-01",
      excerpt: "I followed a river through a quiet city at dawn."
    }
  ],
  total: 1,
  hasMore: false
};

export const mockInsights: InsightsResponse = {
  totalDreams: 7,
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
      facts: [{ value: "water", count: 1, percentageOfDreams: 100, averageScore: null, averageExtractionConfidence: 0.82, sourceFields: ["symbols.symbol"], lastObservedAt: "2026-07-01" }]
    },
    {
      type: "emotion",
      title: "Frequent emotions",
      facts: [{ value: "curiosity", count: 1, percentageOfDreams: 100, averageScore: 0.7, averageExtractionConfidence: 0.82, sourceFields: ["emotions.name"], lastObservedAt: "2026-07-01" }]
    }
  ],
  timingPatterns: [],
  relationships: [
    {
      firstType: "symbol",
      firstValue: "water",
      secondType: "emotion",
      secondValue: "curiosity",
      sharedDreams: 3,
      firstDreams: 4,
      secondDreams: 3,
      sharedOfSmallerPatternPercent: 100,
      relativeLift: 2.5,
      evidence: [{
        dreamId: mockDream.id,
        title: mockDream.title,
        observedAt: "2026-07-01",
        firstExtractionConfidence: 0.82,
        secondExtractionConfidence: 0.82
      }]
    }
  ],
  relationshipReadiness: {
    minimumCompletedDreams: 6,
    completedDreams: 7,
    qualifiedFactPatterns: 2,
    supportedRelationships: 1
  },
  monthlyDreamCounts: [{ month: "2026-07-01", count: 1 }],
  journalSynthesis: {
    status: "ready",
    minimumCompletedDreams: 6,
    completedDreams: 7,
    sourceDreamCount: 7,
    generatedAt: "2026-07-02T02:00:00Z",
    summary: "Water and curiosity recur together as your journal explores uncertainty through movement and changing landscapes.",
    observations: [{
      title: "Curiosity near changing water",
      reflection: "Water appears alongside curiosity in several entries. This may reflect a recurring way your dreams approach uncertain transitions without treating them only as threats.",
      evidence: [{ dreamId: mockDream.id, title: mockDream.title, observedAt: "2026-07-01" }]
    }],
    reflectionQuestions: ["What changes in waking life feel inviting rather than threatening right now?"]
  }
};

export const mockDreamObservation = {
  type: "symbol",
  value: "water",
  totalDreams: 1,
  averageExtractionConfidence: 0.82,
  sourceFields: ["symbols.symbol"],
  evidence: [{
    dreamId: mockDream.id,
    title: mockDream.title,
    observedAt: "2026-07-01",
    score: null,
    extractionConfidence: 0.82,
    sourceField: "symbols.symbol",
    sourceSchemaVersion: "1.1",
    normalizationVersion: "v1"
  }]
};

export const mockEntitlement: EntitlementResponse = {
  tier: "free",
  dailyDreamLimit: 3,
  deepAnalysisEnabled: false,
  quotaExempt: false,
  askDailyLimit: 0,
  askRemaining: 0,
  askResetsAt: "2026-09-16T00:00:00Z",
  askQuotaExempt: false
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

export const mockSensitiveSafetyReviews: SensitiveSafetyReviewResponse[] = [{
  id: "safety_mock_1",
  dreamId: mockDream.id,
  subjectPseudonym: "Dreamer-48Q",
  category: "abuse-or-trauma",
  confidence: 0.93,
  severity: "high",
  restrictsElaboration: true,
  status: "open",
  detectedAt: "2026-09-10T12:00:00Z",
  expiresAt: "2026-10-10T12:00:00Z"
}];

export const mockAdminOperations: AdminOperationsResponse = {
  generatedAt: "2026-09-12T12:00:00Z",
  queue: { status: "available", available: 2, inFlight: 1, delayed: 0, deadLetter: 1, error: null },
  jobs: { pending: 2, processing: 1, failed: 1, oldestPendingSeconds: 420, averageQueueWaitMilliseconds: 420, p95QueueWaitMilliseconds: 920, averageProcessingMilliseconds: 6200, p95ProcessingMilliseconds: 9100 },
  workloads: [
    { source: "image-safety", pending: 1, processing: 0, failed: 1, oldestActiveSeconds: 420 },
    { source: "dream-image", pending: 1, processing: 1, failed: 0, oldestActiveSeconds: 95 },
    { source: "voice", pending: 0, processing: 0, failed: 0, oldestActiveSeconds: null }
  ],
  issues: [{
    id: "00000000-0000-0000-0000-000000000001",
    jobId: "00000000-0000-0000-0000-000000000001",
    source: "job",
    operationType: "dream.image",
    status: "failed",
    attemptCount: 3,
    ageSeconds: 720,
    failure: "Provider timeout",
    acknowledged: false,
    canRequeue: true,
    updatedAt: "2026-09-12T11:48:00Z"
  }],
  providers: [{
    provider: "OpenAI",
    operationType: "dream.image",
    operations: 12,
    failed: 1,
    estimatedCostUsd: 0.118,
    averageLatencyMilliseconds: 12140,
    p95LatencyMilliseconds: 14800
  }]
};
