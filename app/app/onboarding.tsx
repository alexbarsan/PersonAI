import { Redirect } from "expo-router";

import { useAuthStore } from "@/auth/authStore";
import { ProfileForm } from "@/features/profile/ProfileForm";

export default function OnboardingRoute() {
  const user = useAuthStore((state) => state.user);

  if (!user) {
    return <Redirect href="/" />;
  }

  return <ProfileForm mode="onboarding" />;
}
