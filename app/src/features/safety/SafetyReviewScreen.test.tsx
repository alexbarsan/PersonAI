import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { SafetyReviewScreen } from "@/features/safety/SafetyReviewScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("SafetyReviewScreen", () => {
  it("opens original text for an authorized safety administrator", async () => {
    const accessSensitiveSafetyReviewRawText = jest.fn(mockApiClient.accessSensitiveSafetyReviewRawText);
    renderWithProviders(<SafetyReviewScreen />, { accessSensitiveSafetyReviewRawText });

    expect(await screen.findByText("Abuse or trauma")).toBeTruthy();
    expect(screen.queryByText("Original submitted text")).toBeNull();

    fireEvent.press(screen.getByText("View original"));

    await waitFor(() => expect(accessSensitiveSafetyReviewRawText).toHaveBeenCalledWith("safety_mock_1"));
    expect(await screen.findByText("Original submitted text")).toBeTruthy();
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
