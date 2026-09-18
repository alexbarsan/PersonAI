import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { mockApiClient } from "@/mocks/mockApi";
import { ProfileForm } from "@/features/profile/ProfileForm";
import { profileSaveErrorMessage } from "@/features/profile/ProfileForm";
import { ApiError } from "@/api/errors";
import { useOnboardingDraftStore } from "@/state/onboardingDraftStore";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("ProfileForm", () => {
  beforeEach(() => {
    useOnboardingDraftStore.getState().reset();
  });

  it("shows the disclaimer during onboarding", () => {
    renderWithProviders(<ProfileForm mode="onboarding" />);

    expect(screen.getByTestId("wellness-disclaimer")).toBeTruthy();
    expect(screen.getByText(/not medical, mental health, or safety advice/i)).toBeTruthy();
  });

  it("rejects invalid age, missing username, and required consent", async () => {
    renderWithProviders(<ProfileForm mode="onboarding" />);

    fireEvent.changeText(screen.getByLabelText("Age"), "9");
    fireEvent(screen.getByLabelText("AI processing"), "valueChange", false);
    fireEvent(screen.getByLabelText("History use"), "valueChange", false);
    fireEvent.press(screen.getByText("Save profile"));

    expect(await screen.findByText("Age must be at least 13.")).toBeTruthy();
    expect(await screen.findByText("Username is required.")).toBeTruthy();
    expect(await screen.findByText("AI processing consent is required.")).toBeTruthy();
    expect(await screen.findByText("History use consent is required.")).toBeTruthy();
  });

  it("can complete onboarding in mock mode and sends the expected DTO", async () => {
    const updateProfile = jest.fn(mockApiClient.updateProfile);
    renderWithProviders(<ProfileForm mode="onboarding" />, {
      updateProfile
    });

    fireEvent.changeText(screen.getByLabelText("Age"), "41");
    fireEvent.changeText(screen.getByLabelText("Username"), "Test dreamer");
    fireEvent.changeText(screen.getByLabelText("Fears"), "heights, exams");
    fireEvent.press(screen.getByTestId("profile-fears-add"));
    fireEvent.changeText(screen.getByLabelText("Interests"), "music, walking");
    fireEvent.press(screen.getByTestId("profile-interests-add"));
    fireEvent.changeText(screen.getByLabelText("Recent life events"), "new home");
    fireEvent.press(screen.getByTestId("profile-recentLifeEvents-add"));
    fireEvent.press(screen.getByTestId("profile-sex-female"));
    fireEvent.press(screen.getByTestId("profile-stressLevel-4"));
    fireEvent.press(screen.getByText("Save profile"));

    await waitFor(() => expect(updateProfile).toHaveBeenCalledTimes(1));
    expect(updateProfile).toHaveBeenCalledWith(
      expect.objectContaining({
        age: 41,
        sex: "female",
        traits: expect.objectContaining({
          fears: ["heights", "exams"],
          interests: ["music", "walking"],
          recentLifeEvents: ["new home"],
          stressLevel: "4"
        }),
        consent: {
          aiProcessing: true,
          sensitiveTraits: true,
          historyUse: true
        }
      })
    );
  });

  it("keeps anonymization out of the profile screen", async () => {
    renderWithProviders(<ProfileForm mode="profile" />);

    await waitFor(() => expect(screen.queryByTestId("request-anonymization")).toBeNull());
  });

  it("explains profile save failures without discarding the draft", () => {
    expect(profileSaveErrorMessage(new ApiError("bad request", 400, { timezone: ["Timezone is required."] })))
      .toBe("Timezone is required.");
    expect(profileSaveErrorMessage(new ApiError("expired", 401, null)))
      .toMatch(/sign-in has expired/i);
    expect(profileSaveErrorMessage(new ApiError("failed", 500, null)))
      .toMatch(/changes are still on this screen/i);
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
