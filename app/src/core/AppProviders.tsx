import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PropsWithChildren, useState } from "react";
import { useFonts } from "expo-font";
import { Nunito_400Regular } from "@expo-google-fonts/nunito/400Regular";
import { Nunito_700Bold } from "@expo-google-fonts/nunito/700Bold";
import { SafeAreaProvider } from "react-native-safe-area-context";

import { ApiClientProvider } from "@/api/apiContext";
import { ThemeProvider } from "@/theme/ThemeProvider";
import { useCognitoSessionRestoration } from "@/auth/cognitoAuth";

export function AppProviders({ children }: PropsWithChildren) {
  useFonts({ Nunito_400Regular, Nunito_700Bold });
  useCognitoSessionRestoration();
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            retry: 1,
            staleTime: 30_000
          }
        }
      })
  );

  return (
    <SafeAreaProvider><ThemeProvider>
      <ApiClientProvider>
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      </ApiClientProvider>
    </ThemeProvider></SafeAreaProvider>
  );
}
