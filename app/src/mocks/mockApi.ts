import type { ApiClient } from "@/api/client";
import { mockDream, mockInsights, mockJournal, mockMe, mockProfile } from "@/mocks/mockData";

export const mockApiClient: ApiClient = {
  getMe: async () => mockMe,
  getProfile: async () => mockProfile,
  updateProfile: async (request) => request,
  submitDream: async () => mockDream,
  listDreams: async () => mockJournal,
  getDream: async () => mockDream,
  deleteDream: async () => undefined,
  getInsights: async () => mockInsights
};
