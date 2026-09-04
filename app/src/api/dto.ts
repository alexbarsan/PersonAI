export type MeResponse = {
  subject: string;
  email: string | null;
  displayName: string | null;
  authenticationScheme: string;
};

export type ProfileResponse = {
  age: number | null;
  sex: string | null;
  genderIdentity: string | null;
  language: string;
  timezone: string;
  traits: ProfileTraits;
  consent: ConsentFlags;
};

export type ProfileUpdateRequest = ProfileResponse;

export type ProfileTraits = {
  fears: string[];
  allergies: string[];
  interests: string[];
  occupation: string | null;
  relationshipStatus: string | null;
  culturalBackground: string | null;
  sleepPattern: string | null;
  stressLevel: string | null;
  recentLifeEvents: string[];
};

export type ConsentFlags = {
  aiProcessing: boolean;
  sensitiveTraits: boolean;
  historyUse: boolean;
};

export type SubmitDreamRequest = {
  text: string;
  mood?: string | null;
  sleepQuality?: number | null;
  tags?: string[];
  occurredAt?: string | null;
};

export type AskDreamsRequest = {
  question: string;
};

export type AskDreamsResponse = {
  answer: string;
  observations: string[];
  caveat: string;
  sources: AskDreamSourceResponse[];
  sampleSize: number;
};

export type AskDreamSourceResponse = {
  id: string;
  summary: string;
  occurredAt: string | null;
  createdAt: string;
};

export type DreamResponse = {
  id: string;
  createdAt: string;
  status: "completed" | "failed";
  result: DreamResultResponse | null;
  errorMessage: string | null;
  mood?: string | null;
  sleepQuality?: number | null;
  tags?: string[];
  occurredAt?: string | null;
  journalNote?: string | null;
};

export type DreamFeedbackRating = "like" | "dislike";

export type DreamFeedbackResponse = {
  rating: DreamFeedbackRating | null;
  reasons: string[];
  details: string | null;
  updatedAt: string | null;
};

export type UpdateDreamFeedbackRequest = {
  rating: DreamFeedbackRating;
  reasons?: string[];
  details?: string | null;
};

export type DreamJournalFilters = {
  query?: string;
  mood?: string;
  tag?: string;
  from?: string;
  to?: string;
};

export type UpdateDreamJournalRequest = {
  mood?: string | null;
  sleepQuality?: number | null;
  tags?: string[];
  occurredAt?: string | null;
  journalNote?: string | null;
};

export type RequestDreamImageRequest = {
  style?: string | null;
};

export type DreamImageResponse = {
  id: string;
  dreamId: string;
  status: "pending" | "generating" | "completed" | "failed";
  style: string;
  jobId: string | null;
  downloadUrl: string | null;
  errorMessage: string | null;
  createdAt: string;
};

export type VoiceCaptureResponse = {
  id: string;
  status: "pending" | "transcribing" | "completed" | "failed";
  durationSeconds: number;
  retainRecording: boolean;
  transcript: string | null;
  recordingUrl: string | null;
  jobId: string | null;
  errorMessage: string | null;
  createdAt: string;
};

export type VoiceCaptureUpload = {
  uri: string;
  contentType: string;
  durationSeconds: number;
  retainRecording: boolean;
  language?: string;
};

export type DreamResultResponse = {
  summary: string;
  sections: DreamSectionResponse[];
  followUpQuestions: string[];
  safety?: DreamSafetyResponse | null;
};

export type DreamSectionResponse = {
  kind: string;
  title: string;
  content: unknown;
};

export type DreamSafetyResponse = {
  selfHarmRisk: "none" | "elevated";
  notes: string;
};

export type DreamJournalResponse = {
  items: DreamJournalItemResponse[];
};

export type DreamJournalItemResponse = {
  id: string;
  createdAt: string;
  status: string;
  summary: string | null;
  mood: string | null;
  occurredAt: string | null;
};

export type InsightsResponse = {
  totalDreams: number;
  currentStreakDays: number;
  recurringThemes: ThemeInsightResponse[];
  dateRange: InsightDateRangeResponse | null;
  factGroups: FactInsightGroupResponse[];
  timingPatterns: TimingPatternInsightResponse[];
  monthlyDreamCounts: MonthlyDreamCountResponse[];
};

export type ThemeInsightResponse = {
  name: string;
  count: number;
};

export type InsightDateRangeResponse = {
  start: string;
  end: string;
};

export type FactInsightGroupResponse = {
  type: string;
  title: string;
  facts: FactInsightResponse[];
};

export type FactInsightResponse = {
  value: string;
  count: number;
  percentageOfDreams: number;
  averageScore: number | null;
};

export type TimingPatternInsightResponse = {
  type: string;
  value: string;
  occurrences: number;
  weekdayDreams: number;
  weekendDreams: number;
  weekdayRate: number;
  weekendRate: number;
  weekdayToWeekendRatio: number;
};

export type MonthlyDreamCountResponse = {
  month: string;
  count: number;
};

export type EntitlementResponse = {
  tier: "free" | "premium";
  dailyDreamLimit: number;
  deepAnalysisEnabled: boolean;
};

export type AnonymizationRequestResponse = {
  id: string;
  status: "pending" | "approved";
  requestedAt: string;
  reviewedAt: string | null;
  completedAt: string | null;
};

export type UserDataExportResponse = {
  generatedAt: string;
  profile: ProfileResponse;
  dreams: Array<{ id: string; text: string }>;
  aiOperations: Array<{ id: string; operationType: string; estimatedCostUsd: number }>;
};
