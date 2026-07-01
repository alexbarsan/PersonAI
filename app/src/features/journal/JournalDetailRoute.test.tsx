import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import JournalDetailRoute from "../../../app/journal/[id]";
import { mockApiClient } from "@/mocks/mockApi";
import { mockDream } from "@/mocks/mockData";
import { ThemeProvider } from "@/theme/ThemeProvider";

jest.mock("expo-router", () => ({
  useLocalSearchParams: () => ({ id: "dream_mock_1" })
}));

describe("Journal detail route", () => {
  it("renders a stored interpretation", async () => {
    renderWithProviders(<JournalDetailRoute />);

    expect(await screen.findByText(mockDream.result!.summary)).toBeTruthy();
    expect(screen.getByText("Guidance")).toBeTruthy();
  });
});

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });

  function Wrapper({ children }: PropsWithChildren) {
    return (
      <ThemeProvider>
        <ApiClientProvider client={mockApiClient}>
          <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
        </ApiClientProvider>
      </ThemeProvider>
    );
  }

  return render(ui, { wrapper: Wrapper });
}
