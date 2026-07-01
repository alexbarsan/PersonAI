import type { ApiClient } from "@/api/client";
import { mockDream, mockInsights, mockJournal, mockMe, mockProfile } from "@/mocks/mockData";

export const mockApiClient: ApiClient = {
  getMe: async () => mockMe,
  getProfile: async () => mockProfile,
  submitDream: async () => mockDream,
  listDreams: async () => mockJournal,
  getDream: async () => mockDream,
  getInsights: async () => mockInsights
};
