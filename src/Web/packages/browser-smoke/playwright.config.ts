import { defineConfig, devices } from '@playwright/test';
import { chromiumArguments, discoverApps, findProjectRoot } from './src/apps.ts';

// A stack reached on another host than the loopback is no secure context, which would make Chromium refuse the headers the smoke checks.
const args = chromiumArguments(discoverApps(findProjectRoot(import.meta.dirname), process.env), process.env);

export default defineConfig({
  testDir: 'smoke',
  reporter: 'list',
  use: { ...devices['Desktop Chrome'], launchOptions: { args } },
  projects: [{ name: 'chromium' }],
});
