import * as AuthSession from "expo-auth-session";

import { appConfig } from "@/core/config";

export function createCognitoAuthRequest() {
  if (!appConfig.cognitoDomain || !appConfig.cognitoClientId) {
    return null;
  }

  const redirectUri = AuthSession.makeRedirectUri({ scheme: "dreamlens" });

  return {
    clientId: appConfig.cognitoClientId,
    redirectUri,
    scopes: ["openid", "email", "profile"],
    responseType: AuthSession.ResponseType.Code,
    usePKCE: true
  };
}
