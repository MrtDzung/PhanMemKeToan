import { test, expect, Page } from '@playwright/test';

async function loginAsAdmin(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('#email').fill('superadmin@system.local');
  await page.locator('input[type="password"]').fill('Admin@123456');
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL('/dashboard', { timeout: 10000 });
}

test.describe('Users Page (smoke)', () => {
  test.skip(process.env['CI'] === 'true' && !process.env['E2E_BACKEND'], 'No backend in CI');

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('should navigate to users page', async ({ page }) => {
    await page.locator('a[href="/system/users"]').click();
    await expect(page).toHaveURL('/system/users');
    await expect(page.locator('h1')).toContainText('Quản lý người dùng');
  });

  test('users page should show table', async ({ page }) => {
    await page.evaluate(() => {
      window.history.pushState({}, '', '/system/users');
      window.dispatchEvent(new PopStateEvent('popstate'));
    });
    await expect(page.locator('p-table, .p-datatable')).toBeVisible({ timeout: 5000 });
  });
});