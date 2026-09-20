import { requiresAuthentication } from "@/auth/AuthRouteGuard";

describe("auth route guard", () => {
  it("leaves only the public landing route available without authentication", () => {
    expect(requiresAuthentication([])).toBe(false);
    expect(requiresAuthentication(["index"])).toBe(false);
    expect(requiresAuthentication(["insights"])).toBe(true);
    expect(requiresAuthentication(["dreams", "capture"])).toBe(true);
    expect(requiresAuthentication(["journal", "dream-id"])).toBe(true);
    expect(requiresAuthentication(["admin", "operations"])).toBe(true);
  });
});
