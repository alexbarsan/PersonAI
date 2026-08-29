import type { ApiClient } from "@/api/client";
import { ApiError } from "@/api/errors";
import { mockAnonymizationRequest, mockDream, mockDreamImage, mockEntitlement, mockInsights, mockJournal, mockMe, mockProfile, mockUserDataExport } from "@/mocks/mockData";

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
  listDreams: async () => mockJournal,
  getDream: async () => mockDream,
  updateDreamJournal: async (_, request) => ({ ...mockDream, ...request }),
  requestDreamImage: async () => mockDreamImage,
  getDreamImage: async () => mockDreamImage,
  deleteDream: async () => undefined,
  getInsights: async () => mockInsights,
  getEntitlements: async () => mockEntitlement,
  exportUserData: async () => mockUserDataExport,
  requestAnonymization: async () => mockAnonymizationRequest
};

function readMockSubmitMode() {
  if (typeof globalThis.localStorage === "undefined") {
    return null;
  }

  return globalThis.localStorage.getItem("dreamlens.mockSubmitMode");
}
