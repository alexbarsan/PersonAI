import { useCallback, useEffect, useMemo, useState } from "react";
import * as AuthSession from "expo-auth-session";
import * as WebBrowser from "expo-web-browser";

import { useAuthStore, type AuthUser } from "@/auth/authStore";
import { appConfig } from "@/core/config";

WebBrowser.maybeCompleteAuthSession();

export function useCognitoSignIn() {
  const setSession = useAuthStore((state) => state.setSession);
  const [error, setError] = useState<string | null>(null);
  const [isSigningIn, setIsSigningIn] = useState(false);
  const discovery = useMemo(
    () => (appConfig.mockApi ? null : createCognitoDiscovery(appConfig.cognitoDomain)),
    []
  );
  const requestConfig = useMemo(
    () => (appConfig.mockApi ? null : createCognitoAuthRequestConfig(appConfig.cognitoClientId)),
    []
  );
  const [request, response, promptAsync] = AuthSession.useAuthRequest(
    requestConfig ?? createDisabledAuthRequestConfig(),
    discovery
  );

  useEffect(() => {
    if (!response || !discovery || !requestConfig || !request) {
      return;
    }

    if (response.type !== "success") {
      if (response.type === "cancel" || response.type === "dismiss") {
        setIsSigningIn(false);
      }

      return;
    }

    const code = response.params.code;
    if (!code || !request.codeVerifier) {
      setIsSigningIn(false);
      setError("Cognito did not return a usable authorization code.");
      return;
    }

    let isActive = true;
    AuthSession.exchangeCodeAsync(
      {
        clientId: requestConfig.clientId,
        code,
        redirectUri: requestConfig.redirectUri,
        scopes: requestConfig.scopes,
        extraParams: {
          code_verifier: request.codeVerifier
        }
      },
      discovery
    )
      .then((tokenResponse) => {
        if (!isActive) {
          return;
        }

        const bearerToken = tokenResponse.idToken ?? tokenResponse.accessToken;
        setSession(bearerToken, createUserFromToken(bearerToken));
        setError(null);
      })
      .catch(() => {
        if (isActive) {
          setError("Sign-in failed. Please try again.");
        }
      })
      .finally(() => {
        if (isActive) {
          setIsSigningIn(false);
        }
      });

    return () => {
      isActive = false;
    };
  }, [discovery, request, requestConfig, response, setSession]);

  const signIn = useCallback(async () => {
    setError(null);

    if (!discovery || !requestConfig || !request) {
      setError("Cognito sign-in is not configured.");
      return;
    }

    setIsSigningIn(true);
    const result = await promptAsync();
    if (result.type !== "success") {
      setIsSigningIn(false);
    }
  }, [discovery, promptAsync, request, requestConfig]);

  return {
    enabled: Boolean(discovery && requestConfig && request),
    error,
    isSigningIn,
    signIn
  };
}

export function createCognitoDiscovery(cognitoDomain: string) {
  const baseUrl = normalizeCognitoDomain(cognitoDomain);
  if (!baseUrl) {
    return null;
  }

  return {
    authorizationEndpoint: `${baseUrl}/oauth2/authorize`,
    tokenEndpoint: `${baseUrl}/oauth2/token`,
    revocationEndpoint: `${baseUrl}/oauth2/revoke`,
    userInfoEndpoint: `${baseUrl}/oauth2/userInfo`
  };
}

export function createCognitoAuthRequestConfig(clientId: string) {
  if (!clientId) {
    return null;
  }

  const redirectUri = AuthSession.makeRedirectUri({ scheme: "dreamlens" });
  return {
    clientId,
    redirectUri,
    scopes: ["openid", "email", "profile"],
    responseType: AuthSession.ResponseType.Code,
    usePKCE: true
  };
}

export function createUserFromToken(token: string): AuthUser {
  const claims = decodeJwtPayload(token);
  const subject = readStringClaim(claims, "sub") ?? "cognito-user";
  const email = readStringClaim(claims, "email");
  const displayName = readStringClaim(claims, "name") ?? email;

  return {
    subject,
    email,
    displayName
  };
}

function createDisabledAuthRequestConfig() {
  return {
    clientId: "disabled",
    redirectUri: "dreamlens://auth",
    scopes: ["openid"],
    responseType: AuthSession.ResponseType.Code,
    usePKCE: true
  };
}

function normalizeCognitoDomain(cognitoDomain: string) {
  const trimmed = cognitoDomain.trim();
  if (!trimmed) {
    return null;
  }

  return trimmed.startsWith("https://") ? trimmed.replace(/\/+$/, "") : `https://${trimmed.replace(/\/+$/, "")}`;
}

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const [, payload] = token.split(".");
  if (!payload) {
    return null;
  }

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
