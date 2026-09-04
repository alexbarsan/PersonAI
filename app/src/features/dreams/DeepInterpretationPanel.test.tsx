import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { ApiClient, ApiError } from "@/api/client";
import { DeepInterpretationPanel } from "@/features/dreams/DeepInterpretationPanel";
import { mockApiClient } from "@/mocks/mockApi";
import { mockDeepInterpretation } from "@/mocks/mockData";
import { ThemeProvider } from "@/theme/ThemeProvider";

const mockPush = jest.fn();
jest.mock("expo-router", () => ({ router: { push: (...args: unknown[]) => mockPush(...args) } }));

describe("DeepInterpretationPanel", () => {
  beforeEach(() => mockPush.mockClear());

  it("offers Premium to users without deep-analysis access", () => {
    renderWithProviders(<DeepInterpretationPanel dreamId="dream_mock_1" enabled={false} />);

    fireEvent.press(screen.getByText("View Premium"));
    expect(mockPush).toHaveBeenCalledWith("/paywall");
  });

  it("creates and renders a missing Premium analysis", async () => {
    const createDeepInterpretation = jest.fn(async () => mockDeepInterpretation);
    const api: ApiClient = {
      ...mockApiClient,
      getDeepInterpretation: async () => {
        throw new ApiError("missing", 404, null);
      },
      createDeepInterpretation
    };
    renderWithProviders(<DeepInterpretationPanel dreamId="dream_mock_1" enabled />, api);

    fireEvent.press(await screen.findByText("Go deeper"));

    await waitFor(() => expect(createDeepInterpretation).toHaveBeenCalledWith("dream_mock_1"));
    expect(await screen.findByTestId("deep-interpretation-result")).toBeTruthy();
    expect(screen.getByText(mockDeepInterpretation.result.summary)).toBeTruthy();
    expect(screen.getByText("A river appeared beside an open door.")).toBeTruthy();
  });
});

function renderWithProviders(ui: React.ReactElement, api: ApiClient = mockApiClient) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  function Wrapper({ children }: PropsWithChildren) {
    return <ThemeProvider><ApiClientProvider client={api}><QueryClientProvider client={queryClient}>{children}</QueryClientProvider></ApiClientProvider></ThemeProvider>;
  }
  return render(ui, { wrapper: Wrapper });
}
