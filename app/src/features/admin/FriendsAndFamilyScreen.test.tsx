import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { useAuthStore } from "@/auth/authStore";
import { FriendsAndFamilyScreen } from "@/features/admin/FriendsAndFamilyScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("FriendsAndFamilyScreen", () => {
  afterEach(() => act(() => useAuthStore.getState().signOut()));

  it("allows the configured owner to grant and revoke Premium", async () => {
    const grantPremium = jest.fn(async (email: string) => ({
      id: "grant-1",
      email,
      userSubject: "friend-subject",
      grantedAt: "2026-09-16T00:00:00Z",
      grantedByEmail: "ai.ro.dodoloata@gmail.com"
    }));
    const revokePremium = jest.fn(async () => undefined);
    useAuthStore.getState().setSession("owner-token", { subject: "owner", email: "ai.ro.dodoloata@gmail.com" });
    renderWithProviders(<FriendsAndFamilyScreen />, { grantPremium, revokePremium, listPremiumGrants: async () => [{ id: "grant-1", email: "friend@example.com", userSubject: "friend-subject", grantedAt: "2026-09-16T00:00:00Z", grantedByEmail: "ai.ro.dodoloata@gmail.com" }] });

    fireEvent.changeText(screen.getByLabelText("Email address"), "new.friend@example.com");
    fireEvent.press(screen.getByText("Grant Premium"));
    expect(await screen.findByText("friend@example.com")).toBeTruthy();
    expect(grantPremium).toHaveBeenCalledWith("new.friend@example.com");

    fireEvent.press(screen.getByText("Remove"));
    await waitFor(() => expect(revokePremium).toHaveBeenCalledWith("grant-1"));
  });

  it("does not expose grant controls to another account", () => {
    useAuthStore.getState().setSession("other-token", { subject: "other", email: "other@example.com" });
    renderWithProviders(<FriendsAndFamilyScreen />);

    expect(screen.getByText("This account is not authorized to manage Premium access.")).toBeTruthy();
    expect(screen.queryByText("Grant Premium")).toBeNull();
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
