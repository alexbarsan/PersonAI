import { defineConfig, devices } from "@playwright/test";

const webCommand = process.platform === "win32" ? "npm.cmd run web -- --port 8081" : "npm run web -- --port 8081";

export default defineConfig({
  testDir: "./e2e/web",
  timeout: 30_000,
  expect: {
    timeout: 10_000
  },
  use: {
    baseURL: "http://127.0.0.1:8081",
    trace: "on-first-retry"
  },
  webServer: {
    command: webCommand,
    env: {
      CI: "1"
    },
    reuseExistingServer: true,
    timeout: 120_000,
    url: "http://127.0.0.1:8081"
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] }
    }
  ]
});
