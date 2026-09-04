import {
  AskDreamsRequest,
  AskDreamsResponse,
  DreamJournalResponse,
  DreamJournalFilters,
  DreamImageResponse,
  DreamFeedbackResponse,
  DeepInterpretationResponse,
  DreamResponse,
  EntitlementResponse,
  InsightsResponse,
  MeResponse,
  ProfileUpdateRequest,
  ProfileResponse,
  AnonymizationRequestResponse,
  RequestDreamImageRequest,
  SubmitDreamRequest,
  UpdateDreamJournalRequest,
  UpdateDreamFeedbackRequest,
  UserDataExportResponse
  ,
  VoiceCaptureResponse,
  VoiceCaptureUpload
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
  askDreams: (request: AskDreamsRequest) => Promise<AskDreamsResponse>;
  listDreams: (filters?: DreamJournalFilters) => Promise<DreamJournalResponse>;
  getDream: (id: string) => Promise<DreamResponse>;
  getDreamFeedback: (id: string) => Promise<DreamFeedbackResponse>;
  getDeepInterpretation: (id: string) => Promise<DeepInterpretationResponse>;
  createDeepInterpretation: (id: string) => Promise<DeepInterpretationResponse>;
  updateDreamFeedback: (id: string, request: UpdateDreamFeedbackRequest) => Promise<DreamFeedbackResponse>;
  updateDreamJournal: (id: string, request: UpdateDreamJournalRequest) => Promise<DreamResponse>;
  requestDreamImage: (id: string, request?: RequestDreamImageRequest) => Promise<DreamImageResponse>;
  getDreamImage: (id: string) => Promise<DreamImageResponse>;
  deleteDream: (id: string) => Promise<void>;
  getInsights: () => Promise<InsightsResponse>;
  getEntitlements: () => Promise<EntitlementResponse>;
  exportUserData: () => Promise<UserDataExportResponse>;
  requestAnonymization: () => Promise<AnonymizationRequestResponse>;
  uploadVoiceCapture: (capture: VoiceCaptureUpload) => Promise<VoiceCaptureResponse>;
  getVoiceCapture: (id: string) => Promise<VoiceCaptureResponse>;
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

  async function requestForm<T>(path: string, form: FormData): Promise<T> {
    const headers = new Headers({ Accept: "application/json" });
    const token = options.getAccessToken();
    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }

    const response = await fetcher(`${options.baseUrl}${path}`, {
      method: "POST",
      headers,
      body: form
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
    askDreams: (body) =>
      request<AskDreamsResponse>("/v1/dreams/ask", {
        method: "POST",
        body: JSON.stringify(body)
      }),
    listDreams: (filters = {}) => request<DreamJournalResponse>(`/v1/dreams${toQueryString(filters)}`),
    getDream: (id) => request<DreamResponse>(`/v1/dreams/${id}`),
    getDreamFeedback: (id) => request<DreamFeedbackResponse>(`/v1/dreams/${id}/feedback`),
    getDeepInterpretation: (id) => request<DeepInterpretationResponse>(`/v1/dreams/${id}/deep-interpretation`),
    createDeepInterpretation: (id) => request<DeepInterpretationResponse>(`/v1/dreams/${id}/deep-interpretation`, { method: "POST" }),
    updateDreamFeedback: (id, body) =>
      request<DreamFeedbackResponse>(`/v1/dreams/${id}/feedback`, {
        method: "PUT",
        body: JSON.stringify(body)
      }),
    updateDreamJournal: (id, body) =>
      request<DreamResponse>(`/v1/dreams/${id}/journal`, {
        method: "PUT",
        body: JSON.stringify(body)
      }),
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
    getEntitlements: () => request<EntitlementResponse>("/v1/entitlements"),
    exportUserData: () => request<UserDataExportResponse>("/v1/privacy/export"),
    requestAnonymization: () =>
      request<AnonymizationRequestResponse>("/v1/privacy/anonymization-requests", {
        method: "POST"
      }),
    uploadVoiceCapture: async (capture) => {
      const source = await fetcher(capture.uri);
      const audio = await source.blob();
      const form = new FormData();
      form.append("audio", audio, `dream-recording${extensionFor(capture.contentType)}`);
      form.append("durationSeconds", String(capture.durationSeconds));
      form.append("retainRecording", String(capture.retainRecording));
      if (capture.language) {
        form.append("language", capture.language);
      }

      return requestForm<VoiceCaptureResponse>("/v1/voice-captures", form);
    },
    getVoiceCapture: (id) => request<VoiceCaptureResponse>(`/v1/voice-captures/${id}`)
  };
}

function extensionFor(contentType: string) {
  switch (contentType) {
    case "audio/mpeg":
      return ".mp3";
    case "audio/wav":
      return ".wav";
    case "audio/ogg":
      return ".ogg";
    case "audio/webm":
      return ".webm";
    case "audio/m4a":
      return ".m4a";
    default:
      return ".mp4";
  }
}

function toQueryString(filters: DreamJournalFilters) {
  const parameters = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value?.trim()) {
      parameters.set(key, value.trim());
    }
  });
  const serialized = parameters.toString();
  return serialized ? `?${serialized}` : "";
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
