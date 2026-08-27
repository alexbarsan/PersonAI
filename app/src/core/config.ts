import Constants from "expo-constants";

export type AppConfig = {
  apiBaseUrl: string;
  mockApi: boolean;
  cognitoDomain: string;
  cognitoClientId: string;
};

const extra = Constants.expoConfig?.extra ?? {};
const environment = {
  EXPO_PUBLIC_API_BASE_URL: process.env.EXPO_PUBLIC_API_BASE_URL,
  EXPO_PUBLIC_MOCK_API: process.env.EXPO_PUBLIC_MOCK_API,
  EXPO_PUBLIC_COGNITO_DOMAIN: process.env.EXPO_PUBLIC_COGNITO_DOMAIN,
  EXPO_PUBLIC_COGNITO_CLIENT_ID: process.env.EXPO_PUBLIC_COGNITO_CLIENT_ID
};

export const appConfig: AppConfig = readAppConfig(extra, environment);

export function readAppConfig(
  expoExtra: Record<string, unknown>,
  environment: Record<string, string | undefined>
): AppConfig {
  return {
    apiBaseUrl: readString(environment, expoExtra, "EXPO_PUBLIC_API_BASE_URL", "apiBaseUrl", "http://localhost:5000"),
    mockApi: readBoolean(environment, expoExtra, "EXPO_PUBLIC_MOCK_API", "mockApi", true),
    cognitoDomain: readString(environment, expoExtra, "EXPO_PUBLIC_COGNITO_DOMAIN", "cognitoDomain", ""),
    cognitoClientId: readString(environment, expoExtra, "EXPO_PUBLIC_COGNITO_CLIENT_ID", "cognitoClientId", "")
  };
}

function readString(
  environment: Record<string, string | undefined>,
  expoExtra: Record<string, unknown>,
  envKey: string,
  extraKey: string,
  fallback: string
) {
  const envValue = environment[envKey];
  if (typeof envValue === "string" && envValue.trim().length > 0) {
    return envValue;
  }

  const extraValue = expoExtra[extraKey];
  return typeof extraValue === "string" && extraValue.trim().length > 0 ? extraValue : fallback;
}

function readBoolean(
  environment: Record<string, string | undefined>,
  expoExtra: Record<string, unknown>,
  envKey: string,
  extraKey: string,
  fallback: boolean
) {
  const envValue = environment[envKey];
  if (typeof envValue === "string" && envValue.trim().length > 0) {
    return envValue.trim().toLowerCase() === "true";
  }

  const extraValue = expoExtra[extraKey];
  return typeof extraValue === "boolean" ? extraValue : fallback;
}
