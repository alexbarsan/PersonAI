import { expect, test } from "@playwright/test";

test("happy path: onboarding, submit dream, view result", async ({ page }) => {
  await page.goto("/onboarding");
  await page.getByTestId("profile-age").fill("42");
  await page.getByTestId("profile-fears").fill("heights, dark water");
  await page.getByTestId("profile-interests").fill("journaling");
  await page.getByTestId("save-profile").click();

  await expect(page.getByText("Dream DNA")).toBeVisible();

  await page.getByTestId("mock-sign-in").click();
  await page.getByTestId("go-dream-capture").click();
  await page.getByTestId("dream-text").fill("I was walking through a quiet station while holding a blue notebook.");
  await page.getByTestId("dream-mood").fill("curious");
  await page.getByTestId("submit-dream").click();

  await expect(page.getByText("Dream result")).toBeVisible();
  await expect(page.getByText("The dream points to uncertainty and a wish for steadier ground.")).toBeVisible();
  await expect(page.getByText("Guidance")).toBeVisible();
});

test("error path: provider failure shows a safe message", async ({ page }) => {
  await page.addInitScript(() => {
    window.localStorage.setItem("dreamlens.mockSubmitMode", "provider-failure");
  });
  await page.goto("/");
  await page.getByTestId("mock-sign-in").click();
  await page.getByTestId("go-dream-capture").click();
  await page.getByTestId("dream-text").fill("I was walking through a quiet station while holding a blue notebook.");
  await page.getByTestId("submit-dream").click();

  await expect(page.getByText("The interpretation service is temporarily unavailable. Please try again.")).toBeVisible();
});
