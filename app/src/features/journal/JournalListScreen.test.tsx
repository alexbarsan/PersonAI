import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { DreamJournalResponse } from "@/api/dto";
import { JournalListScreen } from "@/features/journal/JournalListScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { mockJournal } from "@/mocks/mockData";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("JournalListScreen", () => {
  it("renders mock dreams", async () => {
    renderWithProviders(<JournalListScreen />);

    expect(await screen.findByText(mockJournal.items[0].summary!)).toBeTruthy();
    expect(screen.getByText(/2026-07-01/)).toBeTruthy();
  });

  it("renders an empty state", async () => {
    renderWithProviders(<JournalListScreen />, {
      listDreams: async (): Promise<DreamJournalResponse> => ({ items: [] })
    });

    expect(await screen.findByText("No dreams yet")).toBeTruthy();
    expect(screen.getByText("Capture a dream to start building your private journal.")).toBeTruthy();
  });

  it("deletes a journal item optimistically", async () => {
    const deleteDream = jest.fn(async () => undefined);
    renderWithProviders(<JournalListScreen />, { deleteDream });

    expect(await screen.findByText(mockJournal.items[0].summary!)).toBeTruthy();
    fireEvent.press(screen.getByText("Delete"));

    await waitFor(() => expect(deleteDream).toHaveBeenCalledWith(mockJournal.items[0].id));
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
