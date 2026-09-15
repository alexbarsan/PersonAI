import { expect, test } from "@playwright/test";

async function readyArtwork(page: import("@playwright/test").Page) {
  await expect.poll(() => page.locator("img").evaluateAll(images => images.length > 0 && images.every(image => (image as HTMLImageElement).complete && (image as HTMLImageElement).naturalWidth > 0)), { timeout: 30_000 }).toBe(true);
}

async function noHorizontalOverflow(page: import("@playwright/test").Page) {
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth + 1,
    ),
  ).toBe(true);
}

test("landing offers a working web entry, honest store placeholders and interactive examples", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto("/");
  await expect(
    page.getByRole("heading", { name: "Dream DNA", exact: true }),
  ).toBeVisible();
  await expect(page.getByTestId("mock-sign-in")).toBeVisible();
  await readyArtwork(page);
  await page.screenshot({
    path: "test-results/dream-dna-landing-desktop.png",
    fullPage: true,
  });
  await page.getByRole("tab", { name: "Dream map", exact: true }).click();
  await expect(
    page.getByText("Example data, not your journal statistics."),
  ).toBeVisible();
  await page.getByRole("tab", { name: "Visuals", exact: true }).click();
  await expect(
    page.getByLabel("Illustrative dream garden visual"),
  ).toBeVisible();
  await page.screenshot({ path: "test-results/dream-dna-landing-preview.png" });
  await expect(page.getByTestId("store-ios")).toHaveAttribute(
    "aria-disabled",
    "true",
  );
  await expect(page.getByTestId("store-android")).toHaveAttribute(
    "aria-disabled",
    "true",
  );
  await page
    .getByRole("button", {
      name: "Can I use Dream DNA without downloading anything?",
    })
    .click();
  await expect(
    page.getByText("Yes. Sign in and capture dreams", { exact: false }),
  ).toBeVisible();
  await noHorizontalOverflow(page);
  await page
    .getByRole("button", { name: "Sign In", exact: true })
    .first()
    .click();
  await expect(page.getByText("Today's dream", { exact: true })).toBeVisible();
  await expect(page.getByTestId("voice-record-toggle")).toBeVisible();
  await page.getByRole("radio", { name: "Joyful", exact: true }).click();
  await expect(page.getByLabel("Mood", { exact: true })).toHaveValue("joyful");
  await page
    .getByLabel("Dream text", { exact: true })
    .fill("I walked through a garden and met an old friend.");
  await page.getByRole("button", { name: "Save draft", exact: true }).click();
  await expect(page.getByTestId("auth-state")).toHaveText("Draft saved");
  await page.screenshot({
    path: "test-results/dream-dna-home-desktop.png",
    fullPage: true,
  });
  await page.getByLabel("Journal", { exact: true }).click();
  await page.screenshot({
    path: "test-results/dream-dna-journal-desktop.png",
    fullPage: true,
  });
  await page.getByRole("link", { name: "Map", exact: true }).click();
  await expect(
    page.getByText("Dreams recorded", { exact: true }),
  ).toBeVisible();
  await page.screenshot({
    path: "test-results/dream-dna-map-desktop.png",
    fullPage: true,
  });
  await page.getByRole("tab", { name: "Frequent emotions", exact: true }).click();
  await expect(page.getByText("water", { exact: true })).toHaveCount(0);
  await page.getByRole("tab", { name: "Everything", exact: true }).click();
  await expect(page.getByText("water", { exact: true })).toBeVisible();
  await noHorizontalOverflow(page);
});

test("the Dream DNA brand returns a signed-in visitor home", async ({ page }) => {
  await page.goto("/");
  await page.getByTestId("mock-sign-in").click();
  await expect(page.getByText("Today's dream", { exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Journal", exact: true }).click();
  await expect(page.getByText("Your dreams", { exact: true })).toBeVisible();

  await page.getByRole("link", { name: "Dream DNA home", exact: true }).first().click();
  await expect(page).toHaveURL(/\/$/);
  await expect(
    page.getByRole("heading", {
      name: "What followed you into today?",
      exact: true,
    }),
  ).toBeVisible();
});

for (const width of [375, 390, 768]) {
  test(`responsive experience at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 844 });
    await page.goto("/");
    await expect(
      page.getByRole("heading", { name: "Dream DNA", exact: true }),
    ).toBeVisible();
    await readyArtwork(page);
    await page.screenshot({
      path: `test-results/dream-dna-landing-${width}.png`,
      fullPage: true,
    });
    await noHorizontalOverflow(page);
    await page.getByTestId("store-ios").scrollIntoViewIfNeeded();
    await page.screenshot({ path: `test-results/dream-dna-downloads-${width}.png` });
    await page.getByTestId("mock-sign-in").click();
    await expect(
      page.getByText("Today's dream", { exact: true }),
    ).toBeVisible();
    await page.screenshot({
      path: `test-results/dream-dna-home-${width}.png`,
      fullPage: true,
    });
    await noHorizontalOverflow(page);
    const navigation = page.getByRole("link", {
      name: "Journal",
      exact: true,
    });
    const bounds = await navigation.boundingBox();
    expect(bounds!.y + bounds!.height).toBeLessThanOrEqual(844);
    await navigation.click();
    await expect(page.getByLabel("Search dreams")).toBeVisible();
    await page.getByLabel("Search dreams").fill("never-a-matching-dream");
    await expect(
      page.getByText("No matching dreams", { exact: true }),
    ).toBeVisible();
    await noHorizontalOverflow(page);
    await page.getByRole("button", { name: "Clear search", exact: true }).click();
    await expect(page.getByLabel("Search dreams")).toHaveValue("");
    await page.getByRole("link", { name: "Ask", exact: true }).click();
    await expect(page.getByText("Available with Premium", { exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Explore Premium", exact: true })).toBeVisible();
    await page.screenshot({
      path: `test-results/dream-dna-ask-${width}.png`,
      fullPage: true,
    });
  });
}
