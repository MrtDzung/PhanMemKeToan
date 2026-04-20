import { test, expect } from '@playwright/test';
import { loginAsAdmin, navigateClientSide } from '../helpers/auth.helper';

test.describe('Import COA Dialog — DI module', () => {
  test.beforeEach(async ({ page }) => {
    test.skip(process.env['CI'] === 'true' && !process.env['E2E_BACKEND'], 'No backend in CI');
    await loginAsAdmin(page);
    await navigateClientSide(page, '/di/accounts');
    await page.waitForSelector('app-account-tree-page, [data-testid="account-tree-page"]', { timeout: 10000 });
  });

  // ─── TC-01: Open dialog ──────────────────────────────────────────────────────
  test('TC-01: clicking Nhập COA chuẩn opens dialog with step 1', async ({ page }) => {
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();

    const dialog = page.locator('p-dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });
    await expect(dialog.locator('p-steps')).toBeVisible();

    // Both COA cards visible
    await expect(dialog.locator('text=TT99')).toBeVisible();
    await expect(dialog.locator('text=TT133')).toBeVisible();

    // "Tiếp theo" disabled until selection made
    await expect(dialog.getByRole('button', { name: /Tiếp theo/i })).toBeDisabled();
  });

  // ─── TC-02: Step 1 — select TT99 enables Tiếp theo ──────────────────────────
  test('TC-02: selecting TT99 enables Tiếp theo button', async ({ page }) => {
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();
    const dialog = page.locator('p-dialog');

    await dialog.locator('.coa-card', { hasText: 'TT99' }).click();
    await expect(dialog.getByRole('button', { name: /Tiếp theo/i })).toBeEnabled();
  });

  // ─── TC-03: Navigate to step 2 ───────────────────────────────────────────────
  test('TC-03: clicking Tiếp theo advances to step 2', async ({ page }) => {
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();
    const dialog = page.locator('p-dialog');

    await dialog.locator('.coa-card', { hasText: 'TT99' }).click();
    await dialog.getByRole('button', { name: /Tiếp theo/i }).click();

    // Step 2 content: conflict resolution options
    await expect(dialog.locator('text=Bỏ qua')).toBeVisible();
    await expect(dialog.locator('text=Ghi đè')).toBeVisible();
    await expect(dialog.locator('text=Nhập')).toBeVisible();

    // "Quay lại" button visible
    await expect(dialog.getByRole('button', { name: /Quay lại/i })).toBeVisible();
  });

  // ─── TC-04: Overwrite warning message ────────────────────────────────────────
  test('TC-04: selecting Ghi đè shows overwrite warning', async ({ page }) => {
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();
    const dialog = page.locator('p-dialog');

    await dialog.locator('.coa-card', { hasText: 'TT133' }).click();
    await dialog.getByRole('button', { name: /Tiếp theo/i }).click();

    // Click Ghi đè option
    await dialog.locator('.resolution-option', { hasText: 'Ghi đè' }).click();

    // Warning message from spec FR-03
    await expect(dialog.locator('p-message[severity="warn"]')).toBeVisible();
    await expect(dialog.locator('text=ghi đè các tài khoản hiện có')).toBeVisible();
  });

  // ─── TC-05: Quay lại goes back to step 1 ─────────────────────────────────────
  test('TC-05: Quay lại returns to step 1', async ({ page }) => {
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();
    const dialog = page.locator('p-dialog');

    await dialog.locator('.coa-card', { hasText: 'TT99' }).click();
    await dialog.getByRole('button', { name: /Tiếp theo/i }).click();
    await dialog.getByRole('button', { name: /Quay lại/i }).click();

    // Back to step 1 — COA cards visible
    await expect(dialog.locator('.coa-card')).toHaveCount(2);
    await expect(dialog.getByRole('button', { name: /Tiếp theo/i })).toBeVisible();
  });

  // ─── TC-06: Hủy closes dialog ────────────────────────────────────────────────
  test('TC-06: clicking Hủy closes the dialog', async ({ page }) => {
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();
    const dialog = page.locator('p-dialog');
    await expect(dialog).toBeVisible();

    await dialog.getByRole('button', { name: /Hủy/i }).click();
    await expect(dialog).not.toBeVisible({ timeout: 3000 });
  });

  // ─── TC-07: Full happy path — import TT99 with skip ──────────────────────────
  test('TC-07: full import flow TT99 skip → shows result stats', async ({ page }) => {
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();
    const dialog = page.locator('p-dialog');

    // Step 1: select TT99
    await dialog.locator('.coa-card', { hasText: 'TT99' }).click();
    await dialog.getByRole('button', { name: /Tiếp theo/i }).click();

    // Step 2: keep default "Bỏ qua", click Nhập
    await dialog.getByRole('button', { name: /^Nhập$/i }).click();

    // Loading state — spinner visible briefly
    // (may be too fast to catch, just check result panel)

    // Wait for result panel
    await expect(dialog.locator('.import-result')).toBeVisible({ timeout: 15000 });

    // Stat cards visible
    await expect(dialog.locator('.stat-card--imported')).toBeVisible();
    await expect(dialog.locator('.stat-card--skipped')).toBeVisible();
    await expect(dialog.locator('.stat-card--overwritten')).toBeVisible();

    // "Đóng" button visible
    await expect(dialog.getByRole('button', { name: /Đóng/i })).toBeVisible();
  });

  // ─── TC-08: Dialog resets on reopen ──────────────────────────────────────────
  test('TC-08: reopening dialog resets to step 1 with no selection', async ({ page }) => {
    // Open → select TT133 → navigate to step 2 → close
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();
    const dialog = page.locator('p-dialog');

    await dialog.locator('.coa-card', { hasText: 'TT133' }).click();
    await dialog.getByRole('button', { name: /Tiếp theo/i }).click();
    await dialog.getByRole('button', { name: /Hủy/i }).first().click();
    await expect(dialog).not.toBeVisible();

    // Reopen
    await page.getByRole('button', { name: /Nhập COA chuẩn/i }).click();
    await expect(dialog).toBeVisible();

    // Should be back on step 1 with nothing selected
    await expect(dialog.getByRole('button', { name: /Tiếp theo/i })).toBeDisabled();
    await expect(dialog.locator('.coa-card--selected')).toHaveCount(0);
  });
});
