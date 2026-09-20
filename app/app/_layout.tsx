import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";

import { AppProviders } from "@/core/AppProviders";
import { AuthRouteGuard } from "@/auth/AuthRouteGuard";

export default function RootLayout() {
  return (
    <AppProviders>
      <AuthRouteGuard>
        <Stack screenOptions={{ headerShown: false }} />
      </AuthRouteGuard>
      <StatusBar style="dark" />
    </AppProviders>
  );
}
