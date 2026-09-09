import { Platform } from "react-native";
import * as SecureStore from "expo-secure-store";

const storageKey = "dreamdna.auth.session.v1";

export type StoredAuthSession = {
  bearerToken: string;
  refreshToken: string;
  expiresAt: number;
};

export async function readStoredAuthSession(): Promise<StoredAuthSession | null> {
  try {
    const value = Platform.OS === "web"
      ? globalThis.sessionStorage?.getItem(storageKey) ?? null
      : await SecureStore.getItemAsync(storageKey);
    return value ? JSON.parse(value) as StoredAuthSession : null;
  } catch {
    return null;
  }
}

export async function writeStoredAuthSession(session: StoredAuthSession) {
  const value = JSON.stringify(session);
  if (Platform.OS === "web") {
    globalThis.sessionStorage?.setItem(storageKey, value);
    return;
  }

  await SecureStore.setItemAsync(storageKey, value);
}

export async function clearStoredAuthSession() {
  if (Platform.OS === "web") {
    globalThis.sessionStorage?.removeItem(storageKey);
    return;
  }

  await SecureStore.deleteItemAsync(storageKey);
}
