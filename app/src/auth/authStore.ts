import { create } from "zustand";

export type AuthUser = {
  subject: string;
  email?: string;
  displayName?: string;
};

type AuthState = {
  accessToken: string | null;
  user: AuthUser | null;
  signInWithMockUser: () => void;
  signOut: () => void;
  setSession: (accessToken: string, user: AuthUser) => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  signInWithMockUser: () =>
    set({
      accessToken: "mock-access-token",
      user: {
        subject: "mock-user",
        email: "mock@dreamlens.local",
        displayName: "Mock Dreamer"
      }
    }),
  signOut: () => set({ accessToken: null, user: null }),
  setSession: (accessToken, user) => set({ accessToken, user })
}));

export function getAccessToken() {
  return useAuthStore.getState().accessToken;
}
