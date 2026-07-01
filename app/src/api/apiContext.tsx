import { PropsWithChildren, createContext, useContext } from "react";

import { ApiClient, createApiClient } from "@/api/client";
import { getAccessToken } from "@/auth/authStore";
import { appConfig } from "@/core/config";

const defaultApiClient = createApiClient({
  baseUrl: appConfig.apiBaseUrl,
  getAccessToken,
  mockMode: appConfig.mockApi
});

const ApiContext = createContext<ApiClient>(defaultApiClient);

export function ApiClientProvider({
  children,
  client = defaultApiClient
}: PropsWithChildren<{ client?: ApiClient }>) {
  return <ApiContext.Provider value={client}>{children}</ApiContext.Provider>;
}

export function useApiClient() {
  return useContext(ApiContext);
}
