import { test, expect } from '@playwright/test';

test.describe('Login Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
  });

  test('should display login form', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Phần Mềm Kế Toán');
    await expect(page.locator('#email')).toBeVisible();
    await expect(page.locator('#password')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test('should show validation errors on empty submit', async ({ page }) => {
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL('/login');
  });

  test('should show error on invalid credentials', async ({ page }) => {
    await page.locator('#email').fill('wrong@example.com');
    await page.locator('input[type="password"]').fill('WrongPassword!');
    await page.locator('button[type="submit"]').click();
    await expect(page.locator('.login-error, [class*="error"]')).toBeVisible({ timeout: 5000 });
  });

  test('should redirect to dashboard on valid login', async ({ page }) => {
    test.skip(process.env['CI'] === 'true' && !process.env['E2E_BACKEND'], 'No backend in CI');

    await page.locator('#email').fill('superadmin@system.local');
    await page.locator('input[type="password"]').fill('Admin@123456');
    await page.locator('button[type="submit"]').click();

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });
    await expect(page.locator('h1')).toContainText('Trang chủ');
  });
});