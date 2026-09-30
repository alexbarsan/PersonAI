import { useEffect } from "react";

import { useAuthStore, type AuthUser } from "@/auth/authStore";
import { appConfig } from "@/core/config";

export function useCognitoSessionRestoration() {
  const setSession = useAuthStore((state) => state.setSession);
  const finishRestoring = useAuthStore((state) => state.finishRestoring);

  useEffect(() => {
    if (appConfig.mockApi) {
      finishRestoring();
      return;
    }

    let active = true;
    const restore = async () => {
      try {
        const { readCognitoSession } = await import("@/auth/cognitoAmplify");
        const session = await readCognitoSession();
        if (active && session) setSession(session.idToken, createUserFromToken(session.idToken));
      } catch {
        if (active) useAuthStore.getState().signOut();
      } finally {
        if (active) finishRestoring();
      }
    };

    void restore();
    const interval = setInterval(() => void restore(), 60_000);
    return () => {
      active = false;
      clearInterval(interval);
    };
  }, [finishRestoring, setSession]);
}

export function createUserFromToken(token: string): AuthUser {
  const claims = decodeJwtPayload(token);
  const subject = readStringClaim(claims, "sub") ?? "cognito-user";
  const email = readStringClaim(claims, "email");
  const displayName = readStringClaim(claims, "name") ?? email;
  const groups = readStringArrayClaim(claims, "cognito:groups");

  return { subject, email, displayName, groups };
}

export function createCognitoDiscovery(cognitoDomain: string) {
  const baseUrl = normalizeCognitoDomain(cognitoDomain);
  if (!baseUrl) return null;

  return {
    authorizationEndpoint: `${baseUrl}/oauth2/authorize`,
    tokenEndpoint: `${baseUrl}/oauth2/token`,
    revocationEndpoint: `${baseUrl}/oauth2/revoke`,
    userInfoEndpoint: `${baseUrl}/oauth2/userInfo`,
  };
}

function normalizeCognitoDomain(cognitoDomain: string) {
  const trimmed = cognitoDomain.trim();
  if (!trimmed) return null;
  return trimmed.startsWith("https://")
    ? trimmed.replace(/\/+$/, "")
    : `https://${trimmed.replace(/\/+$/, "")}`;
}

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const [, payload] = token.split(".");
  if (!payload) return null;

  try {
    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, "=");
    return JSON.parse(globalThis.atob(padded)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function readStringClaim(claims: Record<string, unknown> | null, key: string) {
  const value = claims?.[key];
  return typeof value === "string" && value.trim().length > 0 ? value : undefined;
}

function readStringArrayClaim(claims: Record<string, unknown> | null, key: string) {
  const value = claims?.[key];
  return Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
}
