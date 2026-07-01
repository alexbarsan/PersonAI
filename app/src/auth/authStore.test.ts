import { useAuthStore } from "@/auth/authStore";

describe("auth store", () => {
  beforeEach(() => {
    useAuthStore.getState().signOut();
  });

  it("handles signed-out and signed-in states", () => {
    expect(useAuthStore.getState().user).toBeNull();
    expect(useAuthStore.getState().accessToken).toBeNull();

    useAuthStore.getState().signInWithMockUser();

    expect(useAuthStore.getState().user?.subject).toBe("mock-user");
    expect(useAuthStore.getState().accessToken).toBe("mock-access-token");

    useAuthStore.getState().signOut();

    expect(useAuthStore.getState().user).toBeNull();
  });
});
