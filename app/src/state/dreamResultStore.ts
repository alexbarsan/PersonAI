import { create } from "zustand";

import { DreamResponse } from "@/api/dto";

type DreamResultState = {
  dreamsById: Record<string, DreamResponse>;
  rememberDream: (dream: DreamResponse) => void;
  getDream: (id: string) => DreamResponse | null;
};

export const useDreamResultStore = create<DreamResultState>((set, get) => ({
  dreamsById: {},
  rememberDream: (dream) =>
    set((state) => ({
      dreamsById: {
        ...state.dreamsById,
        [dream.id]: dream
      }
    })),
  getDream: (id) => get().dreamsById[id] ?? null
}));
