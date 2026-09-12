import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { AdminOperationsScreen } from "@/features/admin/AdminOperationsScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("AdminOperationsScreen", () => {
  it("shows operational health and audits a failed job requeue", async () => {
    const requeueAdminJob = jest.fn(mockApiClient.requeueAdminJob);
    renderWithProviders(<AdminOperationsScreen />, { requeueAdminJob });

    expect(await screen.findByText("Provider timeout")).toBeTruthy();
    expect(screen.getByText("Dead letter")).toBeTruthy();
    fireEvent.press(screen.getByTestId("operation-issue-00000000-0000-0000-0000-000000000001"));
    fireEvent.changeText(screen.getByLabelText("Operations action reason"), "Provider is healthy after investigation.");
    fireEvent.press(screen.getByText("Requeue"));

    await waitFor(() => expect(requeueAdminJob).toHaveBeenCalledWith(
      "00000000-0000-0000-0000-000000000001",
      "Provider is healthy after investigation."
    ));
  });

  it("requires a purpose before opening a private dream", async () => {
    const accessAdminDream = jest.fn(mockApiClient.accessAdminDream);
    renderWithProviders(<AdminOperationsScreen />, { accessAdminDream });

    fireEvent.press(screen.getByText("Dreams"));
    fireEvent.press(await screen.findByTestId("admin-dream-dream_mock_1"));
    expect(screen.queryByText("Original dream")).toBeNull();
    fireEvent.changeText(screen.getByLabelText("Dream access purpose"), "Investigating a reported image failure.");
    fireEvent.press(screen.getByText("Open and audit"));

    await waitFor(() => expect(accessAdminDream).toHaveBeenCalledWith("dream_mock_1", "Investigating a reported image failure."));
    expect(await screen.findByText("Original dream")).toBeTruthy();
    expect(screen.getByText("I followed a river through a quiet city at dawn.")).toBeTruthy();
    expect(screen.getByLabelText("Generated dream")).toBeTruthy();
  });
});

function renderWithProviders(ui: React.ReactElement, overrides: Partial<typeof mockApiClient> = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  const client = { ...mockApiClient, ...overrides };
  function Wrapper({ children }: PropsWithChildren) {
    return <ThemeProvider><ApiClientProvider client={client}><QueryClientProvider client={queryClient}>{children}</QueryClientProvider></ApiClientProvider></ThemeProvider>;
  }
  return render(ui, { wrapper: Wrapper });
}
