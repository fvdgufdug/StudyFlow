import { defineConfig } from '@playwright/test';
import path from 'node:path';

export default defineConfig({
  testDir: '.',
  testMatch: '*.spec.js',
  fullyParallel: false,
  workers: 1,
  timeout: 30000,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: { baseURL: 'http://127.0.0.1:5081', trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  projects: [{ name: 'chromium', use: { browserName: 'chromium' } }],
  webServer: {
    command: 'dotnet run --no-build --project ../../src/StudyFlow.Web --urls http://127.0.0.1:5081',
    url: 'http://127.0.0.1:5081',
    reuseExistingServer: false,
    env: { StudyFlow__DatabasePath: path.resolve('test-results', 'e2e.db') },
    timeout: 60000
  }
});
