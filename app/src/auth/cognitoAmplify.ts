import { Amplify } from "aws-amplify";
import {
  confirmResetPassword,
  confirmSignIn,
  confirmSignUp,
  fetchAuthSession,
  resetPassword,
  signIn,
  signInWithRedirect,
  signOut,
  signUp,
} from "aws-amplify/auth";
import { cognitoUserPoolsTokenProvider } from "aws-amplify/auth/cognito";
import { Platform } from "react-native";
import * as AuthSession from "expo-auth-session";
import * as SecureStore from "expo-secure-store";

import { appConfig } from "@/core/config";

let configured = false;
let nativeStorageIndexQueue: Promise<void> = Promise.resolve();
const nativeStorageKeyIndex = "dreamdna.amplify.storage.keys.v1";

export type SocialProvider = "Google" | "Apple";

export function cognitoProviderName(provider: SocialProvider) {
  return provider === "Google" ? "Google" : "SignInWithApple";
}

export function configureCognitoAuth() {
  if (configured) return true;
  if (!appConfig.cognitoUserPoolId || !appConfig.cognitoClientId || !appConfig.cognitoDomain) return false;

  const webOrigin = typeof window === "undefined" ? "" : window.location?.origin || "http://localhost:8081";
  const callback = Platform.OS === "web"
    ? `${webOrigin}/auth/callback`
    : AuthSession.makeRedirectUri({ scheme: "dreamlens", path: "auth/callback" });
  const logout = Platform.OS === "web"
    ? webOrigin
    : AuthSession.makeRedirectUri({ scheme: "dreamlens" });

  Amplify.configure({
    Auth: {
      Cognito: {
        userPoolId: appConfig.cognitoUserPoolId,
        userPoolClientId: appConfig.cognitoClientId,
        loginWith: {
          email: true,
          oauth: {
            domain: appConfig.cognitoDomain.replace(/^https?:\/\//, "").replace(/\/+$/, ""),
            scopes: ["openid", "email", "profile"],
            redirectSignIn: [callback],
            redirectSignOut: [logout],
            responseType: "code",
          },
        },
        signUpVerificationMethod: "code",
      },
    },
  });

  cognitoUserPoolsTokenProvider.setKeyValueStorage(authTokenStorage);
  configured = true;
  return true;
}

export async function signInWithCognitoPassword(email: string, password: string) {
  ensureConfigured();
  return signIn({ username: email.trim(), password });
}

export async function confirmCognitoSignIn(code: string) {
  ensureConfigured();
  return confirmSignIn({ challengeResponse: code });
}

export async function signUpWithCognitoPassword(email: string, password: string) {
  ensureConfigured();
  return signUp({
    username: email.trim(),
    password,
    options: { userAttributes: { email: email.trim() } },
  });
}

export async function confirmCognitoSignUp(email: string, code: string) {
  ensureConfigured();
  return confirmSignUp({ username: email.trim(), confirmationCode: code.trim() });
}

export async function startCognitoPasswordReset(email: string) {
  ensureConfigured();
  return resetPassword({ username: email.trim() });
}

export async function finishCognitoPasswordReset(email: string, code: string, password: string) {
  ensureConfigured();
  return confirmResetPassword({ username: email.trim(), confirmationCode: code.trim(), newPassword: password });
}

export async function startCognitoSocialSignIn(provider: SocialProvider) {
  ensureConfigured();
  if (provider === "Google") {
    await signInWithRedirect({ provider: "Google" });
    return;
  }

  await signInWithRedirect({ provider: { custom: cognitoProviderName(provider) } });
}

export async function readCognitoSession() {
  if (!configureCognitoAuth()) return null;
  const session = await fetchAuthSession();
  const idToken = session.tokens?.idToken?.toString();
  return idToken ? { idToken } : null;
}

export async function signOutFromCognito() {
  if (!configureCognitoAuth()) return;
  await signOut({ global: false });
}

function ensureConfigured() {
  if (!configureCognitoAuth()) throw new Error("Cognito sign-in is not configured.");
}

const authTokenStorage = {
  async getItem(key: string) {
    try {
      return Platform.OS === "web"
        ? globalThis.sessionStorage?.getItem(key) ?? null
        : await SecureStore.getItemAsync(key);
    } catch {
      return null;
    }
  },
  async setItem(key: string, value: string) {
    if (Platform.OS === "web") {
      globalThis.sessionStorage?.setItem(key, value);
      return;
    }
    await SecureStore.setItemAsync(key, value);
    if (key !== nativeStorageKeyIndex) {
      await updateNativeStorageKeys((keys) => keys.includes(key) ? keys : [...keys, key]);
    }
  },
  async removeItem(key: string) {
    if (Platform.OS === "web") {
      globalThis.sessionStorage?.removeItem(key);
      return;
    }
    await SecureStore.deleteItemAsync(key);
    if (key !== nativeStorageKeyIndex) {
      await updateNativeStorageKeys((keys) => keys.filter((storedKey) => storedKey !== key));
    }
  },
  async clear() {
    if (Platform.OS === "web") {
      const storage = globalThis.sessionStorage;
      for (let index = (storage?.length ?? 0) - 1; index >= 0; index -= 1) {
        const key = storage?.key(index);
        if (key?.startsWith("CognitoIdentityServiceProvider.")) storage?.removeItem(key);
      }
      return;
    }
    await nativeStorageIndexQueue;
    const keys = await readNativeStorageKeys();
    await Promise.all(keys.map((key) => SecureStore.deleteItemAsync(key)));
    await SecureStore.deleteItemAsync(nativeStorageKeyIndex);
  },
};

async function readNativeStorageKeys() {
  try {
    const serialized = await SecureStore.getItemAsync(nativeStorageKeyIndex);
    const value: unknown = serialized ? JSON.parse(serialized) : [];
    return Array.isArray(value) ? value.filter((key): key is string => typeof key === "string") : [];
  } catch {
    return [];
  }
}

function updateNativeStorageKeys(update: (keys: string[]) => string[]) {
  const operation = nativeStorageIndexQueue.then(async () => {
    const updatedKeys = update(await readNativeStorageKeys());
    await SecureStore.setItemAsync(nativeStorageKeyIndex, JSON.stringify(updatedKeys));
  });
  nativeStorageIndexQueue = operation.catch(() => undefined);
  return operation;
}
