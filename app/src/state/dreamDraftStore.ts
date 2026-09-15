import { Platform } from "react-native";
import * as SecureStore from "expo-secure-store";
import { create } from "zustand";
import { createJSONStorage, persist, StateStorage } from "zustand/middleware";

const storageKey = "dreamdna.dream-draft.v2";

type DreamDraftFields = {
  text: string;
  mood: string;
  sleepQuality: string;
  tags: string;
  occurredAt: string;
};

type DreamDraftState = DreamDraftFields & {
  savedAt: string | null;
  hasHydrated: boolean;
  setText: (text: string) => void;
  setMood: (mood: string) => void;
  setFields: (fields: Partial<DreamDraftFields>) => void;
  saveDraft: () => void;
  clearDraft: () => void;
  setHydrated: (hasHydrated: boolean) => void;
};

const emptyDraft: DreamDraftFields = {
  text: "",
  mood: "",
  sleepQuality: "",
  tags: "",
  occurredAt: ""
};

const draftStorage: StateStorage = {
  getItem: async (name) => {
    if (Platform.OS === "web") {
      return globalThis.sessionStorage?.getItem(name) ?? null;
    }

    return SecureStore.getItemAsync(name);
  },
  setItem: async (name, value) => {
    if (Platform.OS === "web") {
      globalThis.sessionStorage?.setItem(name, value);
      return;
    }

    await SecureStore.setItemAsync(name, value);
  },
  removeItem: async (name) => {
    if (Platform.OS === "web") {
      globalThis.sessionStorage?.removeItem(name);
      return;
    }

    await SecureStore.deleteItemAsync(name);
  }
};

export const useDreamDraftStore = create<DreamDraftState>()(
  persist(
    (set) => ({
      ...emptyDraft,
      savedAt: null,
      hasHydrated: false,
      setText: (text) => set({ text }),
      setMood: (mood) => set({ mood }),
      setFields: (fields) => set(fields),
      saveDraft: () => set({ savedAt: new Date().toISOString() }),
      clearDraft: () => set({ ...emptyDraft, savedAt: null }),
      setHydrated: (hasHydrated) => set({ hasHydrated })
    }),
    {
      name: storageKey,
      storage: createJSONStorage(() => draftStorage),
      partialize: (state) => ({
        text: state.text,
        mood: state.mood,
        sleepQuality: state.sleepQuality,
        tags: state.tags,
        occurredAt: state.occurredAt,
        savedAt: state.savedAt
      }),
      onRehydrateStorage: () => (state) => state?.setHydrated(true)
    }
  )
);
