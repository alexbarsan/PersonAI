import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { AskDreamsScreen } from "@/features/ask/AskDreamsScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { mockAskDreamMemoryStatus, mockAskDreams, mockEntitlement } from "@/mocks/mockData";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("AskDreamsScreen", () => {
  it("submits a question and renders linked evidence", async () => {
    const askDreams = jest.fn(async () => mockAskDreams);
    renderWithProviders(<AskDreamsScreen />, { askDreams, getEntitlements: async () => ({ ...mockEntitlement, tier: "premium", askDailyLimit: 3, askRemaining: 3, askResetsAt: "2026-09-16T00:00:00Z" }) });

    fireEvent.changeText(await screen.findByLabelText("Dream history question"), "When does water appear?");
    fireEvent.press(screen.getByText("Ask Dream DNA"));

    await waitFor(() => expect(askDreams).toHaveBeenCalledWith({ question: "When does water appear?" }));
    expect(await screen.findByText(mockAskDreams.answer)).toBeTruthy();
    expect(screen.getByText("Cited dreams")).toBeTruthy();
    expect(screen.getByText("The Quiet Shoreline")).toBeTruthy();
    expect(screen.getByText("The answer cites 1 of 1 retrieved journal entries.")).toBeTruthy();
    expect(screen.getByText(mockAskDreams.caveat)).toBeTruthy();
  });

  it("explains when interpreted dreams are still being indexed", async () => {
    renderWithProviders(<AskDreamsScreen />, {
      getEntitlements: async () => ({ ...mockEntitlement, tier: "premium", askDailyLimit: 3, askRemaining: 3, askResetsAt: "2026-09-16T00:00:00Z" }),
      getAskDreamMemoryStatus: async () => ({
        ...mockAskDreamMemoryStatus,
        isReady: false,
        completedDreams: 2,
        indexedDreams: 1,
        pendingDreams: 1,
        message: "Your interpreted dreams are still being indexed. This usually takes a moment."
      })
    });

    expect(await screen.findByText("Dream memory is getting ready")).toBeTruthy();
    expect(screen.getByText("1 of 2 interpreted dreams indexed | 1 pending")).toBeTruthy();
    expect(screen.getByText("Refresh status")).toBeTruthy();
  });
});

function renderWithProviders(ui: React.ReactElement, apiOverrides: Partial<typeof mockApiClient> = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  const client = { ...mockApiClient, ...apiOverrides };
  function Wrapper({ children }: PropsWithChildren) {
    return <ThemeProvider><ApiClientProvider client={client}><QueryClientProvider client={queryClient}>{children}</QueryClientProvider></ApiClientProvider></ThemeProvider>;
  }
  return render(ui, { wrapper: Wrapper });
}
