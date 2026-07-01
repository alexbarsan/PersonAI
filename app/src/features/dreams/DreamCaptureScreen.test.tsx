import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { DreamCaptureScreen } from "@/features/dreams/DreamCaptureScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { mockDream } from "@/mocks/mockData";
import { useDreamResultStore } from "@/state/dreamResultStore";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("DreamCaptureScreen", () => {
  beforeEach(() => {
    useDreamResultStore.setState({ dreamsById: {} });
  });

  it("validates dream text length", async () => {
    renderWithProviders(<DreamCaptureScreen />);

    fireEvent.changeText(screen.getByLabelText("Dream text"), "short");
    fireEvent.press(screen.getByText("Interpret dream"));

    expect(await screen.findByText("Dream text must be at least 10 characters.")).toBeTruthy();
  });

  it("submits through the API client and navigates to result", async () => {
    const submitDream = jest.fn(async () => mockDream);
    const onSubmitted = jest.fn();
    renderWithProviders(<DreamCaptureScreen onSubmitted={onSubmitted} />, { submitDream });

    fireEvent.changeText(screen.getByLabelText("Dream text"), "I was walking through a quiet station.");
    fireEvent.changeText(screen.getByLabelText("Mood"), "calm");
    fireEvent.press(screen.getByText("Interpret dream"));

    await waitFor(() => expect(submitDream).toHaveBeenCalledTimes(1));
    expect(submitDream).toHaveBeenCalledWith(
      expect.objectContaining({
        text: "I was walking through a quiet station.",
        mood: "calm"
      })
    );
    await waitFor(() => expect(onSubmitted).toHaveBeenCalledWith(mockDream.id));
    expect(useDreamResultStore.getState().getDream(mockDream.id)).toEqual(mockDream);
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
