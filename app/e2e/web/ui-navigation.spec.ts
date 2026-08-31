import { devices, expect, test } from "@playwright/test";

test("signed-in navigation keeps core screens reachable", async ({ page }) => {
  await page.goto("/");
  await page.getByTestId("mock-sign-in").click();
  await expect(page.getByTestId("go-dream-capture")).toBeVisible();

  await page.getByLabel("Journal").click();
  await expect(page.getByText("Your dreams", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Search dreams")).toBeVisible();

  await page.getByLabel("Map").last().click();
  await expect(page.getByText("Your dream map", { exact: true })).toBeVisible();
  await expect(page.getByText("Dreams recorded", { exact: true })).toBeVisible();

  await page.getByLabel("Profile").last().click();
  await expect(page.getByTestId("profile-age")).toBeVisible();
  await expect(page.getByTestId("request-anonymization")).toBeVisible();
});

test("mobile UI keeps the capture workflow usable", async ({ browser }) => {
  const context = await browser.newContext(devices["Pixel 5"]);
  const page = await context.newPage();
  try {
    await page.goto("/");
    await page.getByTestId("mock-sign-in").click();
    await page.getByTestId("go-dream-capture").click();

    await expect(page.getByText("What stayed with you?", { exact: true })).toBeVisible();
    await expect(page.getByTestId("dream-text")).toBeVisible();
    await expect(page.getByTestId("submit-dream")).toBeVisible();
  } finally {
    await context.close();
  }
});
