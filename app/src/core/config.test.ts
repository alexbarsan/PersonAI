import { readAppConfig } from "@/core/config";

describe("app config", () => {
  it("prefers Expo public environment variables over app config extras", () => {
    const config = readAppConfig(
      {
        apiBaseUrl: "http://localhost:5000",
        mockApi: true,
        cognitoDomain: "",
        cognitoClientId: "",
        cognitoUserPoolId: "",
        cognitoGoogleEnabled: false,
        cognitoAppleEnabled: false,
        revenueCatWebApiKey: ""
      },
      {
        EXPO_PUBLIC_API_BASE_URL: "https://api.dev.dreamdna.world",
        EXPO_PUBLIC_MOCK_API: "false",
        EXPO_PUBLIC_COGNITO_DOMAIN: "https://dreamlens-dev.auth.us-east-1.amazoncognito.com",
        EXPO_PUBLIC_COGNITO_CLIENT_ID: "client-id",
        EXPO_PUBLIC_COGNITO_USER_POOL_ID: "us-east-1_pool",
        EXPO_PUBLIC_COGNITO_GOOGLE_ENABLED: "true",
        EXPO_PUBLIC_COGNITO_APPLE_ENABLED: "false",
        EXPO_PUBLIC_REVENUECAT_WEB_API_KEY: "web-api-key"
      }
    );

    expect(config).toEqual({
      apiBaseUrl: "https://api.dev.dreamdna.world",
      mockApi: false,
      cognitoDomain: "https://dreamlens-dev.auth.us-east-1.amazoncognito.com",
      cognitoClientId: "client-id",
      cognitoUserPoolId: "us-east-1_pool",
      cognitoGoogleEnabled: true,
      cognitoAppleEnabled: false,
      revenueCatWebApiKey: "web-api-key"
    });
  });
});
