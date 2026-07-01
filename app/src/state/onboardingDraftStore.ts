import { create } from "zustand";

import { defaultProfileFormValues, ProfileFormValues } from "@/features/profile/profileSchema";

type OnboardingDraftState = {
  values: ProfileFormValues;
  setValues: (values: ProfileFormValues) => void;
  reset: () => void;
};

export const useOnboardingDraftStore = create<OnboardingDraftState>((set) => ({
  values: defaultProfileFormValues,
  setValues: (values) => set({ values }),
  reset: () => set({ values: defaultProfileFormValues })
}));
