import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { InsightsResponse } from "@/api/dto";
import { InsightsScreen } from "@/features/insights/InsightsScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { mockInsights } from "@/mocks/mockData";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("InsightsScreen", () => {
  it("renders themes and streaks", async () => {
    renderWithProviders(<InsightsScreen />);

    expect(await screen.findByText("1 days")).toBeTruthy();
    expect(screen.getByText(mockInsights.recurringThemes[0].name)).toBeTruthy();
  });

  it("renders an empty state", async () => {
    renderWithProviders(<InsightsScreen />, {
      getInsights: async (): Promise<InsightsResponse> => ({
        totalDreams: 0,
        currentStreakDays: 0,
        recurringThemes: []
      })
    });

    expect(await screen.findByText("No insights yet")).toBeTruthy();
    expect(screen.getByText("Interpret dreams to reveal recurring themes.")).toBeTruthy();
  });
});

function renderWithProviders(
  ui: React.ReactElement,
  apiOverrides: Partial<typeof mockApiClient> = {}
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });
  const client = {
    ...mockApiClient,
    ...apiOverrides
  };

  function Wrapper({ children }: PropsWithChildren) {
    return (
      <ThemeProvider>
        <ApiClientProvider client={client}>
          <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
        </ApiClientProvider>
      </ThemeProvider>
    );
  }

  return render(ui, { wrapper: Wrapper });
}
