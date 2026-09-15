import { Redirect } from "expo-router";
import { StyleSheet, View } from "react-native";

import { useAuthStore } from "@/auth/authStore";
import { Text } from "@/components/Text";
import { ProfileForm } from "@/features/profile/ProfileForm";
import { useTheme } from "@/theme/ThemeProvider";

export default function OnboardingRoute() {
  const user = useAuthStore((state) => state.user);
  const isRestoring = useAuthStore((state) => state.isRestoring);
  const theme = useTheme();

  if (isRestoring) {
    return <View style={styles.loading}><Text style={{ color: theme.colors.mutedText }}>Restoring your session</Text></View>;
  }

  if (!user) {
    return <Redirect href="/" />;
  }

  return <ProfileForm mode="onboarding" />;
}

const styles = StyleSheet.create({ loading: { alignItems: "center", flex: 1, justifyContent: "center", padding: 20 } });
