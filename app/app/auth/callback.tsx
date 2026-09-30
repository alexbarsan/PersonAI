import { useEffect } from "react";
import { router, useLocalSearchParams } from "expo-router";
import { StyleSheet, View } from "react-native";
import { useAuthStore } from "@/auth/authStore";
import { readCognitoSession } from "@/auth/cognitoAmplify";
import { createUserFromToken } from "@/auth/cognitoAuth";
import { hasOAuthCallbackError, waitForOAuthSession } from "@/auth/oauthCallback";
import { Text } from "@/components/Text";
import { useTheme } from "@/theme/ThemeProvider";

export default function AuthCallbackRoute() {
  const params = useLocalSearchParams<{ error?: string }>();
  const user = useAuthStore((state) => state.user);
  const isRestoring = useAuthStore((state) => state.isRestoring);
  const setSession = useAuthStore((state) => state.setSession);
  const theme = useTheme();

  useEffect(() => {
    let active = true;

    if (hasOAuthCallbackError(params.error)) {
      router.replace("/sign-in?error=oauth");
    } else if (!isRestoring && user) {
      router.replace("/");
    } else {
      void waitForOAuthSession(readCognitoSession).then((session) => {
        if (!active) return;
        if (!session) {
          router.replace("/sign-in?error=oauth");
          return;
        }
        setSession(session.idToken, createUserFromToken(session.idToken));
        router.replace("/");
      });
    }

    return () => {
      active = false;
    };
  }, [isRestoring, params.error, setSession, user]);

  return (
    <View style={[styles.page, { backgroundColor: theme.colors.background }]}>
      <Text style={[styles.status, { color: theme.colors.mutedText }]}>Finishing sign-in...</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  page: { flex: 1, alignItems: "center", justifyContent: "center", padding: 24 },
  status: { fontSize: 14 },
});
