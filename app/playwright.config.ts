import { defineConfig, devices } from "@playwright/test";

const port = process.env.E2E_PORT ?? "8081";
const baseURL = `http://127.0.0.1:${port}`;
const webCommand = `${process.platform === "win32" ? "npm.cmd" : "npm"} run web -- --port ${port}`;

export default defineConfig({
  testDir: "./e2e/web",
  timeout: 90_000,
  expect: {
    timeout: 10_000
  },
  use: {
    baseURL,
    trace: "on-first-retry"
  },
  webServer: {
    command: webCommand,
    env: {
      CI: "1",
      EXPO_OFFLINE: "1",
      EXPO_PUBLIC_MOCK_API: "true"
    },
    reuseExistingServer: true,
    timeout: 120_000,
    url: baseURL
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] }
    }
  ]
});
