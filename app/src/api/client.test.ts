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

  it("deletes dreams through the API", async () => {
    const client = createApiClient({
      baseUrl: "http://localhost",
      getAccessToken: () => "test-token",
      mockMode: false
    });

    await expect(client.deleteDream("dream_mock_1")).resolves.toBeNull();
  });

  it("reads entitlement state from the API", async () => {
    const client = createApiClient({
      baseUrl: "http://localhost",
      getAccessToken: () => "test-token",
      mockMode: false
    });

    const entitlement = await client.getEntitlements();

    expect(entitlement.tier).toBe("free");
    expect(entitlement.dailyDreamLimit).toBe(3);
  });

  it("requests and reads a generated dream image", async () => {
    const client = createApiClient({
      baseUrl: "http://localhost",
      getAccessToken: () => "test-token",
      mockMode: false
    });

    const requested = await client.requestDreamImage("dream_mock_1", { style: "SOFT_DIGITAL_PAINTING" });
    const image = await client.getDreamImage("dream_mock_1");

    expect(requested.status).toBe("completed");
    expect(image.downloadUrl).toContain("data:image/png");
  });

  it("updates journal metadata and prepares privacy actions", async () => {
    const client = createApiClient({
      baseUrl: "http://localhost",
      getAccessToken: () => "test-token",
      mockMode: false
    });

    const updated = await client.updateDreamJournal("dream_mock_1", { journalNote: "Remember this." });
    const exported = await client.exportUserData();
    const anonymization = await client.requestAnonymization();

    expect(updated.id).toBe("dream_mock_1");
    expect(exported.dreams).toHaveLength(1);
    expect(anonymization.status).toBe("pending");
  });
});
