import { setupServer } from "msw/node";

import { createApiClient } from "@/api/client";
import { handlers } from "@/mocks/handlers";

const server = setupServer(...handlers);

describe("api client", () => {
  beforeAll(() => server.listen());
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it("calls the MSW mock endpoint with the auth token", async () => {
    const client = createApiClient({
      baseUrl: "http://localhost",
      getAccessToken: () => "test-token",
      mockMode: false
    });

    const me = await client.getMe();

    expect(me.subject).toBe("mock-user");
    expect(me.displayName).toBe("Mock Dreamer");
  });

  it("sends profile updates to the API", async () => {
    const client = createApiClient({
      baseUrl: "http://localhost",
      getAccessToken: () => "test-token",
      mockMode: false
    });

    const saved = await client.updateProfile({
      age: 33,
      sex: null,
      genderIdentity: null,
      language: "en",
      timezone: "America/New_York",
      traits: {
        fears: ["heights"],
        allergies: [],
        interests: ["journaling"],
        occupation: null,
        relationshipStatus: null,
        culturalBackground: null,
        sleepPattern: null,
        stressLevel: null,
        recentLifeEvents: []
      },
      consent: {
        aiProcessing: true,
        sensitiveTraits: true,
        historyUse: true
      }
    });

    expect(saved.age).toBe(33);
    expect(saved.traits.fears).toEqual(["heights"]);
  });
});
