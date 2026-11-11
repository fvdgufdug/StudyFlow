import { test, expect } from '@playwright/test';

async function ready(page, route) {
  await page.goto(route);
  await page.getByText('Planner refreshed.', { exact: true }).waitFor();
  await page.getByRole('button', { name: 'Refresh', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Refresh', exact: true })).toBeEnabled();
}

test('course and assignment lifecycle, filters, archive protection and export', async ({ page }) => {
  await ready(page, '/courses');
  await page.getByLabel('Course name').fill('E2E Functional Programming');
  await page.getByRole('button', { name: 'Add course', exact: true }).click();
  await expect(page.getByText('Course saved.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Edit E2E Functional Programming', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Edit course', exact: true })).toBeVisible();
  await page.getByLabel('Course name').fill('E2E FP');
  await page.getByRole('button', { name: 'Save course', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Edit E2E FP', exact: true })).toBeVisible();
  await ready(page, '/assignments');
  await page.locator('#title').fill('E2E final');
  await page.locator('#course').selectOption({ label: 'E2E FP' });
  await page.locator('#due').fill('2026-11-15');
  await page.getByRole('button', { name: 'Add task', exact: true }).click();
  await expect(page.getByText('Assignment saved.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Edit E2E final', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Edit assignment', exact: true })).toBeVisible();
  await page.locator('#title').fill('E2E defense');
  await page.locator('#minutes').fill('120');
  await page.getByRole('button', { name: 'Save task', exact: true }).click();
  await page.getByRole('button', { name: 'Complete E2E defense', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Reopen E2E defense', exact: true })).toBeVisible();
  await page.locator('#filter-status').selectOption('active');
  await expect(page.getByRole('button', { name: 'Reopen E2E defense', exact: true })).toHaveCount(0);
  await page.locator('#filter-status').selectOption('done');
  await page.getByRole('button', { name: 'Reopen E2E defense', exact: true }).click();
  await page.locator('#filter-status').selectOption('all');
  await page.locator('#search').fill('no-match');
  await expect(page.getByText('No assignments match.', { exact: false })).toBeVisible();
  await page.locator('#search').fill('E2E defense');
  await expect(page.getByRole('button', { name: 'Edit E2E defense', exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByRole('button', { name: 'Complete E2E defense', exact: true })).toBeVisible();
  const exported = await (await page.request.get('/export')).json();
  expect(exported.Tasks.some(t => t.Title === 'E2E defense' && t.EstimatedMinutes === 120)).toBeTruthy();
  await ready(page, '/courses');
  await page.getByRole('button', { name: 'Delete E2E FP', exact: true }).click();
  await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('This course has assignments');
  await page.getByRole('button', { name: 'Archive', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Restore', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Restore', exact: true }).click();
  await ready(page, '/assignments');
  await page.getByRole('button', { name: 'Delete E2E defense', exact: true }).click();
  await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Edit E2E defense', exact: true })).toHaveCount(0);
  await ready(page, '/courses');
  await page.getByRole('button', { name: 'Delete E2E FP', exact: true }).click();
  await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Edit E2E FP', exact: true })).toHaveCount(0);
});

test('calendar rejects overlapping windows and removes availability', async ({ page }) => {
  await page.goto('/calendar');
  await page.getByRole('button', { name: 'Add study window', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Remove study window', exact: false })).toHaveCount(1);
  await page.getByRole('button', { name: 'Add study window', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('cannot overlap');
  await page.getByRole('button', { name: 'Remove study window', exact: false }).click();
  await expect(page.getByRole('button', { name: 'Remove study window', exact: false })).toHaveCount(0);
});

for (const width of [360, 768, 1280]) {
  test(`all routes fit ${width}px and have working static assets`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    for (const route of ['/', '/courses', '/assignments', '/calendar', '/analytics']) {
      await page.goto(route);
      await expect(page.locator('h1')).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
      await expect(page.locator('#blazor-error-ui')).not.toBeVisible();
    }
    expect((await page.request.get('/_framework/blazor.web.js')).status()).toBe(200);
    expect((await page.request.get('/app.css')).status()).toBe(200);
    await page.screenshot({ path: `../../artifacts/screenshots/analytics-${width}.png`, fullPage: true });
  });
}

test('validated import and saved schedule survive reload without partial conflict imports', async ({ page }) => {
  const courseId = 'dddddddd-1111-2222-3333-444444444444';
  const taskId = 'eeeeeeee-1111-2222-3333-444444444444';
  const backup = {
    SchemaVersion: 1,
    Courses: [{ Id: courseId, Name: 'Import course', ColorHex: '#123abc', Archived: false, Revision: 0 }],
    Tasks: [{ Id: taskId, CourseId: courseId, Title: 'Imported assignment', DueDate: '2099-12-31', Importance: 3, EstimatedMinutes: 60, Status: 0, Revision: 0 }],
    Windows: [], Sessions: []
  };
  const upload = async value => page.locator('#backup-file').setInputFiles({ name: 'backup.json', mimeType: 'application/json', buffer: Buffer.from(typeof value === 'string' ? value : JSON.stringify(value)) });
  await page.goto('/analytics');
  await upload('{broken');
  await expect(page.getByRole('alert')).toContainText('Invalid JSON backup');
  await expect(page.getByRole('button', { name: 'Import validated backup' })).toHaveCount(0);
  await upload(backup);
  await expect(page.getByText('Validated backup:', { exact: false })).toContainText('1 courses, 1 assignments');
  await page.getByRole('button', { name: 'Import validated backup' }).click();
  await expect(page.getByText('Backup imported.', { exact: false })).toBeVisible();
  await upload(backup);
  await page.getByRole('button', { name: 'Import validated backup' }).click();
  await expect(page.getByText('Validated backup:', { exact: false })).toHaveCount(0);
  const conflict = structuredClone(backup);
  conflict.Courses[0].Name = 'Do not overwrite';
  conflict.Courses.push({ ...backup.Courses[0], Id: 'ffffffff-1111-2222-3333-444444444444', Name: 'Must not be imported' });
  await upload(conflict);
  await page.getByRole('button', { name: 'Import validated backup' }).click();
  await expect(page.getByRole('alert')).toContainText('Import conflicts');
  const unchanged = await (await page.request.get('/export')).json();
  expect(unchanged.Courses).toHaveLength(1);
  expect(unchanged.Courses[0].Name).toBe('Import course');
  await page.goto('/calendar');
  await page.locator('#slot-date').fill('2099-11-20');
  await page.locator('#slot-time').fill('10:00');
  await page.getByRole('button', { name: 'Add study window', exact: true }).click();
  await expect(page.getByText('Study window saved.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Save current proposal' }).click();
  await expect(page.getByText('Study plan saved.', { exact: true })).toBeVisible();
  await page.reload();
  const savedSection = page.locator('section').filter({ has: page.getByRole('heading', { name: 'Saved study plan' }) });
  await expect(savedSection.getByText('Imported assignment', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Save current proposal' })).toBeDisabled();
  const exported = await (await page.request.get('/export')).json();
  expect(exported.SchemaVersion).toBe(1);
  expect(exported.Windows).toHaveLength(1);
  expect(exported.Sessions).toHaveLength(1);
  expect(exported.Sessions[0].TaskId).toBe(taskId);
  await page.goto('/analytics');
  await upload(exported);
  await page.getByRole('button', { name: 'Import validated backup' }).click();
  await expect(page.getByText('Backup imported.', { exact: false })).toBeVisible();
  await page.goto('/calendar');
  await page.getByRole('button', { name: 'Remove study window', exact: false }).click();
  await expect(page.getByText('No saved sessions yet.', { exact: true })).toBeVisible();
  await ready(page, '/assignments');
  await page.getByRole('button', { name: 'Delete Imported assignment', exact: true }).click();
  await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page.getByText('Assignment deleted.', { exact: true })).toBeVisible();
  await ready(page, '/courses');
  await page.getByRole('button', { name: 'Delete Import course', exact: true }).click();
  await page.getByRole('button', { name: 'Confirm delete', exact: true }).click();
  await expect(page.getByText('Course deleted.', { exact: true })).toBeVisible();
});
