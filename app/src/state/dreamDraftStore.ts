import { create } from "zustand";

type DreamDraftState = {
  text: string;
  mood: string;
  savedAt: string | null;
  setText: (text: string) => void;
  setMood: (mood: string) => void;
  saveDraft: () => void;
  clearDraft: () => void;
};

export const useDreamDraftStore = create<DreamDraftState>((set) => ({
  text: "",
  mood: "",
  savedAt: null,
  setText: (text) => set({ text }),
  setMood: (mood) => set({ mood }),
  saveDraft: () => set({ savedAt: new Date().toISOString() }),
  clearDraft: () => set({ text: "", mood: "", savedAt: null })
}));
