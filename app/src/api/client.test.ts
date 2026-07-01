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
});
