import { expect, test } from "@playwright/test";

test("happy path: onboarding, submit dream, view result", async ({ page }) => {
  await page.goto("/onboarding");
  await expect(page.getByTestId("mock-sign-in")).toBeVisible();
  await expect(page.getByTestId("profile-age")).not.toBeVisible();

  await page.getByTestId("mock-sign-in").click();
  await page.getByTestId("go-onboarding").click();
  await expect(page.getByTestId("profile-age")).toBeVisible();
  await page.getByTestId("profile-age").fill("42");
  await page.getByTestId("profile-fears-input").fill("heights, dark water");
  await page.getByTestId("profile-fears-add").click();
  await page.getByTestId("profile-interests-input").fill("journaling");
  await page.getByTestId("profile-interests-add").click();
  await page.getByTestId("save-profile").click();

  await expect(page.getByTestId("go-dream-capture").last()).toBeVisible();

  await page.getByTestId("go-dream-capture").last().click();
  await page.getByTestId("dream-text").fill("I was walking through a quiet station while holding a blue notebook.");
  await page.getByTestId("dream-mood-curious").click();
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
