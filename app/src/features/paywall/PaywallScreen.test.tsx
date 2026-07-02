import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { useAuthStore } from "@/auth/authStore";
import { PaywallScreen } from "@/features/paywall/PaywallScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("PaywallScreen", () => {
  beforeEach(() => {
    useAuthStore.getState().signInWithMockUser();
  });

  it("renders mock paywall tiers without connecting purchases", async () => {
    renderWithProviders(<PaywallScreen />);

    expect(screen.getAllByText("Premium")).toHaveLength(2);
    expect(screen.getByText("Free")).toBeTruthy();
    expect(screen.getByText("3 dream interpretations per day.")).toBeTruthy();
    expect(screen.getByText("25 dream interpretations per day.")).toBeTruthy();
    await waitFor(() => expect(screen.getByText("Purchases not connected yet")).toBeTruthy());
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
