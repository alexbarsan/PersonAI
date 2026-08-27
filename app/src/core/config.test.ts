import { readAppConfig } from "@/core/config";

describe("app config", () => {
  it("prefers Expo public environment variables over app config extras", () => {
    const config = readAppConfig(
      {
        apiBaseUrl: "http://localhost:5000",
        mockApi: true,
        cognitoDomain: "",
        cognitoClientId: ""
      },
      {
        EXPO_PUBLIC_API_BASE_URL: "https://api.dev.dreamdna.world",
        EXPO_PUBLIC_MOCK_API: "false",
        EXPO_PUBLIC_COGNITO_DOMAIN: "https://dreamlens-dev.auth.us-east-1.amazoncognito.com",
        EXPO_PUBLIC_COGNITO_CLIENT_ID: "client-id"
      }
    );

    expect(config).toEqual({
      apiBaseUrl: "https://api.dev.dreamdna.world",
      mockApi: false,
      cognitoDomain: "https://dreamlens-dev.auth.us-east-1.amazoncognito.com",
      cognitoClientId: "client-id"
    });
  });
});
