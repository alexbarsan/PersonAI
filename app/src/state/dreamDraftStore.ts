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
  ownerSubject: string | null;
  savedAt: string | null;
  hasHydrated: boolean;
  setText: (text: string) => void;
  setMood: (mood: string) => void;
  setFields: (fields: Partial<DreamDraftFields>) => void;
  adoptForUser: (subject: string) => void;
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
      ownerSubject: null,
      savedAt: null,
      hasHydrated: false,
      setText: (text) => set({ text, savedAt: new Date().toISOString() }),
      setMood: (mood) => set({ mood, savedAt: new Date().toISOString() }),
      setFields: (fields) => set({ ...fields, savedAt: new Date().toISOString() }),
      adoptForUser: (subject) => set((state) => state.ownerSubject === subject
        ? state
        : { ...emptyDraft, ownerSubject: subject, savedAt: null }),
      saveDraft: () => set({ savedAt: new Date().toISOString() }),
      clearDraft: () => set((state) => ({ ...emptyDraft, ownerSubject: state.ownerSubject, savedAt: null })),
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
        ownerSubject: state.ownerSubject,
        savedAt: state.savedAt
      }),
      onRehydrateStorage: () => (state) => state?.setHydrated(true)
    }
  )
);
