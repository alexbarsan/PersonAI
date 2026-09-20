import { PropsWithChildren, useEffect } from "react";
import { useRouter, useSegments } from "expo-router";
import { StyleSheet, View } from "react-native";

import { useAuthStore } from "@/auth/authStore";
import { Text } from "@/components/Text";
import { useTheme } from "@/theme/ThemeProvider";

export function AuthRouteGuard({ children }: PropsWithChildren) {
  const router = useRouter();
  const segments = useSegments();
  const user = useAuthStore((state) => state.user);
  const isRestoring = useAuthStore((state) => state.isRestoring);
  const theme = useTheme();
  const protectedRoute = requiresAuthentication(segments);

  useEffect(() => {
    if (protectedRoute && !isRestoring && !user) {
      router.replace("/");
    }
  }, [isRestoring, protectedRoute, router, user]);

  if (protectedRoute && (isRestoring || !user)) {
    return <View style={styles.loading}>
      <Text style={{ color: theme.colors.mutedText }}>
        {isRestoring ? "Restoring your session" : "Redirecting to sign in"}
      </Text>
    </View>;
  }

  return children;
}

export function requiresAuthentication(segments: readonly string[]) {
  return segments.some((segment) => segment !== "index");
}

const styles = StyleSheet.create({
  loading: { alignItems: "center", flex: 1, justifyContent: "center", padding: 20 },
});
