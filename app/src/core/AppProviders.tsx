import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PropsWithChildren, useState } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { ThemeProvider } from "@/theme/ThemeProvider";
import { useCognitoSessionRestoration } from "@/auth/cognitoAuth";

export function AppProviders({ children }: PropsWithChildren) {
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
    <ThemeProvider>
      <ApiClientProvider>
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      </ApiClientProvider>
    </ThemeProvider>
  );
}
