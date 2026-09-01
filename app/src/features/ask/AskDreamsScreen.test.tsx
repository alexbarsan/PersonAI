import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { AskDreamsScreen } from "@/features/ask/AskDreamsScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { mockAskDreams } from "@/mocks/mockData";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("AskDreamsScreen", () => {
  it("submits a question and renders linked evidence", async () => {
    const askDreams = jest.fn(async () => mockAskDreams);
    renderWithProviders(<AskDreamsScreen />, { askDreams });

    fireEvent.changeText(screen.getByLabelText("Dream history question"), "When does water appear?");
    fireEvent.press(screen.getByText("Ask Dream DNA"));

    await waitFor(() => expect(askDreams).toHaveBeenCalledWith({ question: "When does water appear?" }));
    expect(await screen.findByText(mockAskDreams.answer)).toBeTruthy();
    expect(screen.getByText("Dreams used")).toBeTruthy();
    expect(screen.getByText(mockAskDreams.caveat)).toBeTruthy();
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
