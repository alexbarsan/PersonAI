import {
  DreamJournalResponse,
  DreamImageResponse,
  DreamResponse,
  EntitlementResponse,
  InsightsResponse,
  MeResponse,
  ProfileUpdateRequest,
  ProfileResponse,
  RequestDreamImageRequest,
  SubmitDreamRequest
} from "@/api/dto";
import { ApiError } from "@/api/errors";
import { mockApiClient } from "@/mocks/mockApi";

export type ApiClientOptions = {
  baseUrl: string;
  getAccessToken: () => string | null;
  mockMode?: boolean;
  fetchImpl?: typeof fetch;
};

export type ApiClient = {
  getMe: () => Promise<MeResponse>;
  getProfile: () => Promise<ProfileResponse>;
  updateProfile: (request: ProfileUpdateRequest) => Promise<ProfileResponse>;
  submitDream: (request: SubmitDreamRequest) => Promise<DreamResponse>;
  listDreams: () => Promise<DreamJournalResponse>;
  getDream: (id: string) => Promise<DreamResponse>;
  requestDreamImage: (id: string, request?: RequestDreamImageRequest) => Promise<DreamImageResponse>;
  getDreamImage: (id: string) => Promise<DreamImageResponse>;
  deleteDream: (id: string) => Promise<void>;
  getInsights: () => Promise<InsightsResponse>;
  getEntitlements: () => Promise<EntitlementResponse>;
};

export { ApiError };

export function createApiClient(options: ApiClientOptions): ApiClient {
  if (options.mockMode) {
    return mockApiClient;
  }

  const fetcher = options.fetchImpl ?? fetch;

  async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set("Accept", "application/json");
    const token = options.getAccessToken();

    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }

    if (init.body) {
      headers.set("Content-Type", "application/json");
    }

    const response = await fetcher(`${options.baseUrl}${path}`, {
      ...init,
      headers
    });
    const body = await readBody(response);

    if (!response.ok) {
      throw new ApiError("API request failed", response.status, body);
    }

    return body as T;
  }

  return {
    getMe: () => request<MeResponse>("/v1/me"),
    getProfile: () => request<ProfileResponse>("/v1/profile"),
    updateProfile: (body) =>
      request<ProfileResponse>("/v1/profile", {
        method: "PUT",
        body: JSON.stringify(body)
      }),
    submitDream: (body) =>
      request<DreamResponse>("/v1/dreams", {
        method: "POST",
        body: JSON.stringify(body)
    }),
    listDreams: () => request<DreamJournalResponse>("/v1/dreams"),
    getDream: (id) => request<DreamResponse>(`/v1/dreams/${id}`),
    requestDreamImage: (id, body = {}) =>
      request<DreamImageResponse>(`/v1/dreams/${id}/image`, {
        method: "POST",
        body: JSON.stringify(body)
      }),
    getDreamImage: (id) => request<DreamImageResponse>(`/v1/dreams/${id}/image`),
    deleteDream: (id) =>
      request<void>(`/v1/dreams/${id}`, {
        method: "DELETE"
      }),
    getInsights: () => request<InsightsResponse>("/v1/insights"),
    getEntitlements: () => request<EntitlementResponse>("/v1/entitlements")
  };
}

async function readBody(response: Response) {
  const text = await response.text();
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}
