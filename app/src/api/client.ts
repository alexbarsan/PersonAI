import {
  DreamJournalResponse,
  DreamResponse,
  InsightsResponse,
  MeResponse,
  ProfileResponse,
  SubmitDreamRequest
} from "@/api/dto";
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
  submitDream: (request: SubmitDreamRequest) => Promise<DreamResponse>;
  listDreams: () => Promise<DreamJournalResponse>;
  getDream: (id: string) => Promise<DreamResponse>;
  getInsights: () => Promise<InsightsResponse>;
};

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly body: unknown
  ) {
    super(message);
  }
}

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
    submitDream: (body) =>
      request<DreamResponse>("/v1/dreams", {
        method: "POST",
        body: JSON.stringify(body)
      }),
    listDreams: () => request<DreamJournalResponse>("/v1/dreams"),
    getDream: (id) => request<DreamResponse>(`/v1/dreams/${id}`),
    getInsights: () => request<InsightsResponse>("/v1/insights")
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
