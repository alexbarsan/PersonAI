import { createCognitoDiscovery, createUserFromToken } from "@/auth/cognitoAuth";
import {
  confirmCognitoSignUp,
  finishCognitoPasswordReset,
  readCognitoSession,
  signInWithCognitoPassword,
  startCognitoSocialSignIn,
  signOutFromCognito,
  signUpWithCognitoPassword,
  startCognitoPasswordReset,
} from "@/auth/cognitoAmplify";
import { Amplify } from "aws-amplify";
import {
  confirmResetPassword,
  confirmSignUp,
  fetchAuthSession,
  resetPassword,
  signIn,
  signInWithRedirect,
  signOut,
  signUp,
} from "aws-amplify/auth";

jest.mock("@/core/config", () => ({
  appConfig: {
    cognitoDomain: "https://auth.example.com",
    cognitoClientId: "client-id",
    cognitoUserPoolId: "us-east-1_testpool",
  },
}));
jest.mock("aws-amplify", () => ({ Amplify: { configure: jest.fn() } }));
jest.mock("aws-amplify/auth", () => ({
  confirmResetPassword: jest.fn(),
  confirmSignIn: jest.fn(),
  confirmSignUp: jest.fn(),
  fetchAuthSession: jest.fn(),
  resetPassword: jest.fn(),
  signIn: jest.fn(),
  signInWithRedirect: jest.fn(),
  signOut: jest.fn(),
  signUp: jest.fn(),
}));
jest.mock("aws-amplify/auth/cognito", () => ({
  cognitoUserPoolsTokenProvider: { setKeyValueStorage: jest.fn() },
}));
jest.mock("expo-auth-session", () => ({
  makeRedirectUri: ({ scheme, path }: { scheme: string; path?: string }) => `${scheme}://${path ?? ""}`,
}));

describe("Cognito auth helpers", () => {
  beforeEach(() => jest.clearAllMocks());

  it("builds hosted-domain discovery endpoints from configured input", () => {
    expect(createCognitoDiscovery("auth.example.com/")).toEqual({
      authorizationEndpoint: "https://auth.example.com/oauth2/authorize",
      tokenEndpoint: "https://auth.example.com/oauth2/token",
      revocationEndpoint: "https://auth.example.com/oauth2/revoke",
      userInfoEndpoint: "https://auth.example.com/oauth2/userInfo",
    });
  });

  it("maps social users from the verified Cognito token claims", () => {
    const payload = btoa(JSON.stringify({ sub: "user-123", email: "dreamer@example.com", name: "Dreamer", "cognito:groups": ["dreamlens-metrics-admin"] }));
    expect(createUserFromToken(`header.${payload}.signature`)).toEqual({
      subject: "user-123",
      email: "dreamer@example.com",
      displayName: "Dreamer",
      groups: ["dreamlens-metrics-admin"],
    });
  });

  it("configures authorization code OAuth with the app callback", async () => {
    await startCognitoSocialSignIn("Google");
    const config = (Amplify.configure as jest.Mock).mock.calls[0][0];
    expect(config.Auth.Cognito.loginWith.oauth.responseType).toBe("code");
    expect(config.Auth.Cognito.loginWith.oauth.scopes).toEqual(["openid", "email", "profile"]);
    expect(config.Auth.Cognito.loginWith.oauth.redirectSignIn[0]).toContain("/auth/callback");
    expect(signInWithRedirect).toHaveBeenCalledWith({ provider: "Google" });
  });

  it("initiates Apple federation directly through Cognito", async () => {
    await startCognitoSocialSignIn("Apple");
    expect(signInWithRedirect).toHaveBeenCalledWith({ provider: { custom: "SignInWithApple" } });
  });

  it("preserves the existing Cognito email/password and recovery operations", async () => {
    await signInWithCognitoPassword("  user@example.com ", "Password1!");
    await signUpWithCognitoPassword("  user@example.com ", "Password1!");
    await confirmCognitoSignUp(" user@example.com ", " 123456 ");
    await startCognitoPasswordReset(" user@example.com ");
    await finishCognitoPasswordReset("user@example.com", "123456", "Password2!");

    expect(signIn).toHaveBeenCalledWith({ username: "user@example.com", password: "Password1!" });
    expect(signUp).toHaveBeenCalledWith(expect.objectContaining({ username: "user@example.com", options: expect.any(Object) }));
    expect(confirmSignUp).toHaveBeenCalledWith({ username: "user@example.com", confirmationCode: "123456" });
    expect(resetPassword).toHaveBeenCalledWith({ username: "user@example.com" });
    expect(confirmResetPassword).toHaveBeenCalledWith({ username: "user@example.com", confirmationCode: "123456", newPassword: "Password2!" });
  });

  it("restores Cognito ID tokens and signs out only the Cognito app session", async () => {
    (fetchAuthSession as jest.Mock).mockResolvedValue({ tokens: { idToken: { toString: () => "signed-id-token" } } });
    await expect(readCognitoSession()).resolves.toEqual({ idToken: "signed-id-token" });
    await signOutFromCognito();
    expect(signOut).toHaveBeenCalledWith({ global: false });
  });
});
