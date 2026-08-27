import Constants from "expo-constants";

export type AppConfig = {
  apiBaseUrl: string;
  mockApi: boolean;
  cognitoDomain: string;
  cognitoClientId: string;
};

const extra = Constants.expoConfig?.extra ?? {};

export const appConfig: AppConfig = {
  apiBaseUrl: readString("EXPO_PUBLIC_API_BASE_URL", "apiBaseUrl", "http://localhost:5000"),
  mockApi: readBoolean("EXPO_PUBLIC_MOCK_API", "mockApi", true),
  cognitoDomain: readString("EXPO_PUBLIC_COGNITO_DOMAIN", "cognitoDomain", ""),
  cognitoClientId: readString("EXPO_PUBLIC_COGNITO_CLIENT_ID", "cognitoClientId", "")
};

function readString(envKey: string, extraKey: string, fallback: string) {
  const envValue = process.env[envKey];
  if (typeof envValue === "string" && envValue.trim().length > 0) {
    return envValue;
  }

  const extraValue = extra[extraKey];
  return typeof extraValue === "string" && extraValue.trim().length > 0 ? extraValue : fallback;
}

function readBoolean(envKey: string, extraKey: string, fallback: boolean) {
  const envValue = process.env[envKey];
  if (typeof envValue === "string" && envValue.trim().length > 0) {
    return envValue.trim().toLowerCase() === "true";
  }

  const extraValue = extra[extraKey];
  return typeof extraValue === "boolean" ? extraValue : fallback;
}
