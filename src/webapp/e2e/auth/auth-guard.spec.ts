import { test, expect } from '@playwright/test';

test.describe('Auth Guard', () => {
  test('should redirect unauthenticated user to login', async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
  });

  test('should redirect from root to dashboard when authenticated', async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/');
    await expect(page).toHaveURL(/\/login|\/dashboard/);
  });
});