import { expect, test } from "@playwright/test";

test("keyboard navigation exposes stable links, tabs, and journal destinations", async ({ page }) => {
  await page.goto("/");
  await page.getByTestId("mock-sign-in").click();

  const journal = page.getByRole("link", { name: "Journal", exact: true });
  await journal.focus();
  await expect(journal).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.getByText("Your dreams", { exact: true })).toBeVisible();

  const dream = page.getByRole("link", { name: "Open dream: The Quiet Shoreline", exact: true });
  await dream.focus();
  await expect(dream).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.getByRole("heading", { name: "The Quiet Shoreline", exact: true })).toBeVisible();

  const symbol = page.getByRole("tab", { name: "symbols: water", exact: true });
  await symbol.focus();
  await expect(symbol).toBeFocused();
  await page.keyboard.press("Space");
  await expect(symbol).toHaveAttribute("aria-selected", "true");

  const map = page.getByRole("link", { name: "Map", exact: true });
  await map.focus();
  await expect(map).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.getByText("Your dream map", { exact: true })).toBeVisible();
  await expect(page.getByRole("tablist")).toBeVisible();
});
