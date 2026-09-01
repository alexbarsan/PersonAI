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

test("web home makes voice capture immediately available", async ({ page }) => {
  await page.goto("/");
  await page.getByTestId("mock-sign-in").click();

  await expect(page.getByTestId("voice-capture-panel")).toBeVisible();
  await expect(page.getByTestId("voice-record-toggle")).toBeVisible();
  await expect(page.getByText("Capture by voice", { exact: true })).toBeVisible();
  await page.screenshot({ path: "test-results/home-voice-capture-desktop.png", fullPage: true });
});

test("onboarding uses structured choices, scales, and tags", async ({ page }) => {
  await page.goto("/");
  await page.getByTestId("mock-sign-in").click();
  await page.getByTestId("go-onboarding").click();

  await expect(page.getByTestId("profile-sex")).toBeVisible();
  await page.getByTestId("profile-sex-female").click();
  await expect(page.getByTestId("profile-stressLevel")).toBeVisible();
  await page.getByTestId("profile-stressLevel-4").click();
  await page.getByTestId("profile-fears-input").fill("deep water");
  await page.getByTestId("profile-fears-add").click();
  await expect(page.getByText("deep water", { exact: true })).toBeVisible();

  await page.screenshot({ path: "test-results/profile-controls-desktop.png", fullPage: true });
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
    await expect(page.getByTestId("dream-mood")).toBeVisible();
    await expect(page.getByTestId("dream-sleepQuality")).toBeVisible();
    await expect(page.getByTestId("dream-tags")).toBeVisible();
    await expect(page.getByTestId("submit-dream")).toBeVisible();

    await page.getByTestId("dream-mood").scrollIntoViewIfNeeded();
    await page.screenshot({ path: "test-results/dream-capture-mobile-controls.png", fullPage: false });
  } finally {
    await context.close();
  }
});
