import { create } from "zustand";
import { clearStoredAuthSession } from "@/auth/authSessionStorage";
import { appConfig } from "@/core/config";

export type AuthUser = {
  subject: string;
  email?: string;
  displayName?: string;
  groups?: string[];
};

type AuthState = {
  accessToken: string | null;
  user: AuthUser | null;
  isRestoring: boolean;
  signInWithMockUser: () => void;
  signOut: () => void;
  setSession: (accessToken: string, user: AuthUser) => void;
  finishRestoring: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  isRestoring: !appConfig.mockApi,
  signInWithMockUser: () =>
    set({
      accessToken: "mock-access-token",
      user: {
        subject: "mock-user",
        email: "mock@dreamlens.local",
        displayName: "Mock Dreamer",
        groups: ["dreamlens-metrics-admin", "dreamlens-admin"]
      },
      isRestoring: false
    }),
  signOut: () => {
    void clearStoredAuthSession();
    set({ accessToken: null, user: null, isRestoring: false });
  },
  setSession: (accessToken, user) => set({ accessToken, user, isRestoring: false }),
  finishRestoring: () => set({ isRestoring: false })
}));

export function getAccessToken() {
  return useAuthStore.getState().accessToken;
}
