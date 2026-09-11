import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { ApiClient } from "@/api/client";
import { DreamResponse } from "@/api/dto";
import { DreamResultScreen } from "@/features/dreams/DreamResultScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { mockDream } from "@/mocks/mockData";
import { useDreamResultStore } from "@/state/dreamResultStore";
import { ThemeProvider } from "@/theme/ThemeProvider";

const mockPush = jest.fn();
jest.mock("expo-router", () => ({
  router: { push: (...args: unknown[]) => mockPush(...args) },
  useLocalSearchParams: () => ({ id: "dream_mock_1" })
}));

jest.mock("@/features/dreams/InterpretationFeedbackPanel", () => ({
  InterpretationFeedbackPanel: () => null
}));

describe("DreamResultScreen", () => {
  beforeEach(() => {
    useDreamResultStore.setState({ dreamsById: {} });
    mockPush.mockClear();
  });

  it("shows the disclaimer and rendered result", () => {
    useDreamResultStore.getState().rememberDream(mockDream);

    renderWithProviders(<DreamResultScreen />);

    expect(screen.getByTestId("result-disclaimer")).toBeTruthy();
    expect(screen.getByText(mockDream.result!.summary)).toBeTruthy();
    expect(screen.getByText("Guidance")).toBeTruthy();
  });

  it("renders constrained safety UI for elevated safety responses", () => {
    const elevatedDream: DreamResponse = {
      ...mockDream,
      result: {
        ...mockDream.result!,
        sections: [{ kind: "text", title: "Interpretation", content: "This should not be shown." }],
        safety: {
          selfHarmRisk: "elevated",
          notes: "Use immediate support if you feel at risk."
        }
      }
    };
    useDreamResultStore.getState().rememberDream(elevatedDream);

    renderWithProviders(<DreamResultScreen />);

    expect(screen.getByText("Support first")).toBeTruthy();
    expect(screen.getByText("Use immediate support if you feel at risk.")).toBeTruthy();
    expect(screen.queryByText("This should not be shown.")).toBeNull();
  });

  it("shows the image action and completed visual for premium users", async () => {
    useDreamResultStore.getState().rememberDream(mockDream);
    const premiumApi: ApiClient = {
      ...mockApiClient,
      getEntitlements: async () => ({ tier: "premium", dailyDreamLimit: 25, deepAnalysisEnabled: true })
    };

    renderWithProviders(<DreamResultScreen />, premiumApi);

    expect(await screen.findByLabelText("Generated dream visual")).toBeTruthy();
  });

  it("lets free users request a dream visual", async () => {
    useDreamResultStore.getState().rememberDream(mockDream);
    const freeApi: ApiClient = {
      ...mockApiClient,
      getEntitlements: async () => ({ tier: "free", dailyDreamLimit: 3, deepAnalysisEnabled: false }),
      getDreamImage: async () => ({
        id: "image_free_1",
        dreamId: mockDream.id,
        status: "failed",
        style: "SOFT_DIGITAL_PAINTING",
        jobId: "job_free_1",
        downloadUrl: null,
        errorMessage: null,
        createdAt: "2026-09-11T00:00:00Z"
      })
    };

    renderWithProviders(<DreamResultScreen />, freeApi);

    expect(await screen.findByTestId("request-dream-image")).toBeTruthy();
    expect(mockPush).not.toHaveBeenCalled();
  });

  it("explains image-provider policy blocks without exposing the raw provider error", async () => {
    useDreamResultStore.getState().rememberDream(mockDream);
    const blockedApi: ApiClient = {
      ...mockApiClient,
      getDreamImage: async () => ({
        id: "image_1",
        dreamId: mockDream.id,
        status: "failed",
        style: "SOFT_DIGITAL_PAINTING",
        jobId: "job_1",
        downloadUrl: null,
        errorMessage: "OpenAI image generation failed with 400: moderation_blocked",
        createdAt: "2026-09-11T00:00:00Z"
      })
    };

    renderWithProviders(<DreamResultScreen />, blockedApi);

    expect(await screen.findByText("This dream cannot be visualized by the selected image provider.")).toBeTruthy();
    expect(screen.queryByText(/moderation_blocked/)).toBeNull();
  });

});

function renderWithProviders(ui: React.ReactElement, api: ApiClient = mockApiClient) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });

  function Wrapper({ children }: PropsWithChildren) {
    return (
      <ThemeProvider>
        <ApiClientProvider client={api}>
          <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
        </ApiClientProvider>
      </ThemeProvider>
    );
  }

  return render(ui, { wrapper: Wrapper });
}
