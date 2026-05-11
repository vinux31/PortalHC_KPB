import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  globalTeardown: require.resolve('./e2e/global.teardown.ts'), // Phase 315 — flush + RESTORE + Layer 4
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  retries: 0,
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: 'http://localhost:5277',
    // Phase 316 Plan 06 (GAP-316-2 defense-in-depth): bound retry untuk page.{check,click,fill}
    // actions ke 10s. Tanpa setting ini, default = test-level timeout (60s) → 1 hung action
    // bisa akumulate seluruh budget. Plus Plan 04 cascade catch sebagai 1st defense.
    // Tradeoff: 10s per action vs 60s per test. SignalR negotiate typically <2s acceptable.
    actionTimeout: 10_000,
    screenshot: 'on',
    trace: 'on-first-retry',
    viewport: { width: 1280, height: 720 },
  },
  projects: [
    {
      name: 'setup',
      testMatch: /global\.setup\.ts/,
    },
    {
      name: 'chromium',
      use: { browserName: 'chromium' },
      dependencies: ['setup'],
    },
  ],
});
