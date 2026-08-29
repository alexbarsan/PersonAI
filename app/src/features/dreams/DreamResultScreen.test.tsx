import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { ApiClient } from "@/api/client";
import { DreamResponse } from "@/api/dto";
import { DreamResultScreen } from "@/features/dreams/DreamResultScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { mockDream } from "@/mocks/mockData";
import { useDreamResultStore } from "@/state/dreamResultStore";
import { ThemeProvider } from "@/theme/ThemeProvider";

jest.mock("expo-router", () => ({
  useLocalSearchParams: () => ({ id: "dream_mock_1" })
}));

describe("DreamResultScreen", () => {
  beforeEach(() => {
    useDreamResultStore.setState({ dreamsById: {} });
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
