import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";

import { AppProviders } from "@/core/AppProviders";
import { useAuthStore } from "@/auth/authStore";
import { HomeScreen } from "@/features/home/HomeScreen";
import { useDreamDraftStore } from "@/state/dreamDraftStore";

describe("HomeScreen", () => {
  beforeEach(() => {
    useAuthStore.getState().signOut();
    useDreamDraftStore.getState().clearDraft();
  });

  it("renders the initial route in signed-out state", () => {
    render(
      <AppProviders>
        <HomeScreen />
      </AppProviders>
    );

    expect(screen.getByText("Dream DNA")).toBeTruthy();
    expect(screen.getByText("Signed out")).toBeTruthy();
  });

  it("handles mock sign-in and local draft state", async () => {
    render(
      <AppProviders>
        <HomeScreen />
      </AppProviders>
    );

    fireEvent.press(screen.getByText("Use mock account"));

    await waitFor(() => expect(screen.getByText("Today's dream")).toBeTruthy());
    await waitFor(() => expect(screen.getByText("Free: 3 dreams/day")).toBeTruthy());

    fireEvent.changeText(screen.getByLabelText("Dream text"), "I was walking through a quiet station.");
    fireEvent.changeText(screen.getByLabelText("Mood"), "calm");
    fireEvent.press(screen.getByText("Save draft"));

    expect(screen.getByText("Draft saved")).toBeTruthy();
    expect(useDreamDraftStore.getState().text).toBe("I was walking through a quiet station.");
  });
});
