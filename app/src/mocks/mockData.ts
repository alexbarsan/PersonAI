import { DreamJournalResponse, DreamResponse, InsightsResponse, MeResponse, ProfileResponse } from "@/api/dto";

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
  ]
};
