import { PropsWithChildren } from "react";
import { Redirect, useSegments } from "expo-router";
import { StyleSheet, View } from "react-native";

import { useAuthStore } from "@/auth/authStore";
import { Text } from "@/components/Text";
import { useTheme } from "@/theme/ThemeProvider";

export function AuthRouteGuard({ children }: PropsWithChildren) {
  const segments = useSegments();
  const user = useAuthStore((state) => state.user);
  const isRestoring = useAuthStore((state) => state.isRestoring);
  const theme = useTheme();
  const protectedRoute = requiresAuthentication(segments);

  if (protectedRoute && isRestoring) {
    return <View style={styles.loading}>
      <Text style={{ color: theme.colors.mutedText }}>
        Restoring your session
      </Text>
    </View>;
  }

  if (protectedRoute && !user) return <Redirect href="/" />;

  return children;
}

export function requiresAuthentication(segments: readonly string[]) {
  return segments.some((segment) => segment !== "index");
}

const styles = StyleSheet.create({
  loading: { alignItems: "center", flex: 1, justifyContent: "center", padding: 20 },
});
