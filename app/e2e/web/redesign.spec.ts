import { devices, expect, test } from "@playwright/test";

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

test("completed voice recording controls stay inside the capture column", async ({ page }) => {
  await page.addInitScript(() => {
    class MockMediaRecorder {
      static isTypeSupported() {
        return true;
      }

      mimeType = "audio/webm";
      ondataavailable: ((event: { data: Blob }) => void) | null = null;
      onstop: (() => void) | null = null;
      state = "inactive";

      start() {
        this.state = "recording";
      }

      stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["recording"], { type: this.mimeType }) });
        this.onstop?.();
      }
    }

    Object.defineProperty(window, "MediaRecorder", { configurable: true, value: MockMediaRecorder });
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: async () => ({ getTracks: () => [{ stop: () => undefined }] }),
      },
    });
  });
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto("/");
  await page.getByTestId("mock-sign-in").click();

  const record = page.getByTestId("voice-record-toggle");
  await record.click();
  await expect(record).toContainText("Stop recording");
  await record.click();

  const transcribe = page.getByTestId("voice-transcribe");
  const recent = page.getByText("Recent memories", { exact: true });
  await expect(transcribe).toBeVisible();
  await expect(recent).toBeVisible();
  const transcribeBounds = await transcribe.boundingBox();
  const recentBounds = await recent.boundingBox();
  expect(transcribeBounds!.x + transcribeBounds!.width).toBeLessThanOrEqual(recentBounds!.x);
  await noHorizontalOverflow(page);
  await page.screenshot({ path: "test-results/dream-dna-voice-ready-desktop.png", fullPage: true });
});

test("Dream Map patterns open in a desktop drawer and support relationship navigation", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto("/");
  await page.getByTestId("mock-sign-in").click();
  await page.getByRole("link", { name: "Map", exact: true }).click();

  const waterPattern = page.getByRole("button", {
    name: "Explore water, appearing in 1 dream",
    exact: true,
  });
  await expect(waterPattern).toBeVisible();
  const waterSlice = page.locator('circle[aria-label="Explore water, appearing in 1 dream"]');
  await expect(waterSlice).toHaveCSS("cursor", "pointer");
  await waterSlice.hover({ position: { x: 50, y: 5 } });
  await expect(waterPattern.locator("svg")).toHaveCSS("opacity", "1");
  await waterPattern.hover();
  await expect(waterSlice).toHaveAttribute("stroke-width", "28");

  const drawer = page.getByLabel("water pattern details", { exact: true });
  await waterSlice.click({ position: { x: 50, y: 5 } });
  await expect(drawer).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(drawer).toBeHidden();

  await waterPattern.click();
  await expect(drawer).toBeVisible();
  const drawerBounds = await drawer.boundingBox();
  expect(drawerBounds!.width).toBeGreaterThanOrEqual(439);
  expect(drawerBounds!.width).toBeLessThanOrEqual(501);
  expect(drawerBounds!.x + drawerBounds!.width).toBeGreaterThanOrEqual(1439);
  await expect(page.getByText("Your DreamDNA interpretation", { exact: true })).toBeVisible();
  await expect(page.getByText(/Water appears with curiosity in this journal/)).toBeVisible();
  await expect(page.getByRole("link", { name: /Open source Continuity between waking activities/ })).toBeVisible();

  await page.getByRole("tab", { name: "Trends", exact: true }).click();
  await expect(page.getByLabel("Monthly pattern occurrences", { exact: true })).toBeVisible();
  await expect(page.getByText("More monthly history is needed before a direction can be estimated.", { exact: true })).toBeVisible();
  await page.screenshot({ path: "test-results/dream-map-pattern-trends-desktop.png" });

  await page.getByRole("tab", { name: "Related", exact: true }).click();
  await page.getByRole("button", { name: /Explore curiosity, connected in/ }).click();
  await expect(page.getByLabel("curiosity pattern details", { exact: true })).toBeVisible();
  await page.getByLabel("Back to previous pattern", { exact: true }).click();
  await expect(drawer).toBeVisible();
  await page.screenshot({ path: "test-results/dream-map-pattern-drawer-desktop.png" });

  await page.keyboard.press("Escape");
  await expect(drawer).toBeHidden();
  await expect(waterPattern).toBeFocused();
});

test("Dream Map patterns use a readable mobile legend and bottom sheet", async ({ browser }) => {
  const context = await browser.newContext(devices["Pixel 5"]);
  const page = await context.newPage();
  try {
    await page.goto("/");
    await page.getByTestId("mock-sign-in").click();
    await page.getByRole("link", { name: "Map", exact: true }).click();

    const chart = page.getByLabel("Recurring symbols distribution", { exact: true });
    const waterPattern = page.getByRole("button", {
      name: "Explore water, appearing in 1 dream",
      exact: true,
    });
    await chart.scrollIntoViewIfNeeded();
    const chartBounds = await chart.boundingBox();
    const legendBounds = await waterPattern.boundingBox();
    expect(legendBounds!.y).toBeGreaterThanOrEqual(chartBounds!.y + chartBounds!.height - 1);
    expect(legendBounds!.width).toBeGreaterThan(chartBounds!.width);

    await waterPattern.click();
    const sheet = page.getByLabel("water pattern details", { exact: true });
    await expect(sheet).toBeVisible();
    await expect.poll(() => sheet.evaluate((element) => {
      const bounds = element.getBoundingClientRect();
      const topmost = document.elementFromPoint(bounds.left + bounds.width / 2, bounds.top + 40);
      return topmost === element || element.contains(topmost);
    })).toBe(true);
    const sheetBounds = await sheet.boundingBox();
    const viewport = page.viewportSize()!;
    expect(sheetBounds!.height).toBeGreaterThanOrEqual(viewport.height * 0.79);
    expect(sheetBounds!.height).toBeLessThanOrEqual(viewport.height * 0.9);
    expect(sheetBounds!.y + sheetBounds!.height).toBeGreaterThanOrEqual(viewport.height - 1);
    await expect(page.getByRole("tab", { name: "Dreams", exact: true })).toBeVisible();
    await page.getByRole("tab", { name: "Trends", exact: true }).click();
    await expect(page.getByLabel("Monthly pattern occurrences", { exact: true })).toBeVisible();
    await page.screenshot({ path: "test-results/dream-map-pattern-sheet-mobile.png" });
    await noHorizontalOverflow(page);
  } finally {
    await context.close();
  }
});

for (const width of [375, 390, 768]) {
  test(`responsive experience at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 844 });
    await page.goto("/");
    await expect(
      page.getByRole("heading", { name: "Dream DNA", exact: true }),
    ).toBeVisible();
    await expect(page.getByText("Start today. Make room for a little wonder.", { exact: true })).toBeVisible();
    await readyArtwork(page);
    await page.screenshot({
      path: `test-results/dream-dna-landing-${width}.png`,
      fullPage: true,
    });
    await noHorizontalOverflow(page);
    await page.getByTestId("store-ios").scrollIntoViewIfNeeded();
    const downloadWidths = await Promise.all([
      page.getByTestId("download-web").evaluate(element => element.getBoundingClientRect().width),
      page.getByTestId("store-ios").evaluate(element => element.getBoundingClientRect().width),
      page.getByTestId("store-android").evaluate(element => element.getBoundingClientRect().width),
    ]);
    expect(Math.max(...downloadWidths) - Math.min(...downloadWidths)).toBeLessThanOrEqual(1);
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
