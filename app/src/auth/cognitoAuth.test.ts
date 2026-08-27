import { createCognitoAuthRequestConfig, createCognitoDiscovery, createUserFromToken } from "@/auth/cognitoAuth";

describe("cognito auth helpers", () => {
  it("builds hosted UI discovery endpoints from a Cognito domain", () => {
    const discovery = createCognitoDiscovery("dreamlens-dev.auth.us-east-1.amazoncognito.com/");

    expect(discovery).toEqual({
      authorizationEndpoint: "https://dreamlens-dev.auth.us-east-1.amazoncognito.com/oauth2/authorize",
      tokenEndpoint: "https://dreamlens-dev.auth.us-east-1.amazoncognito.com/oauth2/token",
      revocationEndpoint: "https://dreamlens-dev.auth.us-east-1.amazoncognito.com/oauth2/revoke",
      userInfoEndpoint: "https://dreamlens-dev.auth.us-east-1.amazoncognito.com/oauth2/userInfo"
    });
  });

  it("returns null auth config when no client id is configured", () => {
    expect(createCognitoAuthRequestConfig("")).toBeNull();
  });

  it("creates a user snapshot from JWT claims", () => {
    const payload = btoa(JSON.stringify({ sub: "user-123", email: "dreamer@example.com", name: "Dreamer" }));
    const token = `header.${payload}.signature`;

    expect(createUserFromToken(token)).toEqual({
      subject: "user-123",
      email: "dreamer@example.com",
      displayName: "Dreamer"
    });
  });
});
