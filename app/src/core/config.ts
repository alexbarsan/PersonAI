import Constants from "expo-constants";

export type AppConfig = {
  apiBaseUrl: string;
  mockApi: boolean;
  cognitoDomain: string;
  cognitoClientId: string;
};

const extra = Constants.expoConfig?.extra ?? {};

export const appConfig: AppConfig = {
  apiBaseUrl: readString("apiBaseUrl", "http://localhost:5000"),
  mockApi: readBoolean("mockApi", true),
  cognitoDomain: readString("cognitoDomain", ""),
  cognitoClientId: readString("cognitoClientId", "")
};

function readString(key: string, fallback: string) {
  const value = extra[key];
  return typeof value === "string" && value.trim().length > 0 ? value : fallback;
}

function readBoolean(key: string, fallback: boolean) {
  const value = extra[key];
  return typeof value === "boolean" ? value : fallback;
}
