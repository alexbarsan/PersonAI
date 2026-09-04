import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { InterpretationFeedbackPanel } from "@/features/dreams/InterpretationFeedbackPanel";
import { mockApiClient } from "@/mocks/mockApi";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("InterpretationFeedbackPanel", () => {
  it("collects structured feedback after a dislike", async () => {
    const getDreamFeedback = jest.fn(mockApiClient.getDreamFeedback);
    const updateDreamFeedback = jest.fn(mockApiClient.updateDreamFeedback);
    renderWithProviders(<InterpretationFeedbackPanel dreamId="dream_mock_1" />, {
      ...mockApiClient,
      getDreamFeedback,
      updateDreamFeedback
    });
    await waitFor(() => expect(getDreamFeedback).toHaveBeenCalledWith("dream_mock_1"));

    fireEvent.press(screen.getByText("Not for me"));
    fireEvent.press(screen.getByText("Too generic"));
    fireEvent.changeText(screen.getByLabelText("Additional interpretation feedback"), "It missed the strongest detail.");
    fireEvent.press(screen.getByTestId("save-interpretation-feedback"));

    await waitFor(() => expect(updateDreamFeedback).toHaveBeenCalledWith("dream_mock_1", {
      rating: "dislike",
      reasons: ["too-generic"],
      details: "It missed the strongest detail."
    }));
    expect(await screen.findByText("Thanks. Your feedback was saved.")).toBeTruthy();
  });
});

function renderWithProviders(ui: React.ReactElement, api = mockApiClient) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  function Wrapper({ children }: PropsWithChildren) {
    return <ThemeProvider><ApiClientProvider client={api}><QueryClientProvider client={queryClient}>{children}</QueryClientProvider></ApiClientProvider></ThemeProvider>;
  }
  return render(ui, { wrapper: Wrapper });
}
