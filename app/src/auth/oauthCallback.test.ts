import { hasOAuthCallbackError, waitForOAuthSession } from "@/auth/oauthCallback";

describe("OAuth callback handling", () => {
  it("accepts an exchanged Cognito session after an initial pending response", async () => {
    const readSession = jest.fn()
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce({ idToken: "cognito-id-token" });

    await expect(waitForOAuthSession(readSession, 3, 0, async () => undefined))
      .resolves.toEqual({ idToken: "cognito-id-token" });
    expect(readSession).toHaveBeenCalledTimes(2);
  });

  it("retries temporary exchange errors and fails closed for an invalid callback", async () => {
    const readSession = jest.fn()
      .mockRejectedValueOnce(new Error("invalid or expired code"))
      .mockResolvedValue(null);

    await expect(waitForOAuthSession(readSession, 2, 0, async () => undefined)).resolves.toBeNull();
  });

  it("recognizes provider cancellation and error callback parameters", () => {
    expect(hasOAuthCallbackError("access_denied")).toBe(true);
    expect(hasOAuthCallbackError(undefined)).toBe(false);
    expect(hasOAuthCallbackError(["access_denied"])).toBe(true);
  });
});
