# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: di\import-coa-dialog.spec.ts >> Import COA Dialog — DI module >> TC-01: clicking Nhập COA chuẩn opens dialog with step 1
- Location: e2e\di\import-coa-dialog.spec.ts:13:7

# Error details

```
Test timeout of 30000ms exceeded while running "beforeEach" hook.
```

```
Error: locator.fill: Test timeout of 30000ms exceeded.
Call log:
  - waiting for locator('#email')

```

# Page snapshot

```yaml
- generic [ref=e5]:
  - generic [ref=e6]:
    - generic [ref=e7]: 
    - heading "Quản lý Mạ kẽm" [level=1] [ref=e8]
    - paragraph [ref=e9]: Đăng nhập để tiếp tục
  - generic [ref=e10]:
    - generic [ref=e11]:
      - generic [ref=e12]: Cơ sở dữ liệu
      - generic [ref=e14] [cursor=pointer]:
        - combobox "ZincPlating Dev" [ref=e15]
        - button "dropdown trigger" [ref=e16]:
          - img [ref=e18]
    - generic [ref=e20]:
      - generic [ref=e21]: Tên đăng nhập
      - textbox "Tên đăng nhập" [ref=e22]:
        - /placeholder: Nhập tên đăng nhập
    - generic [ref=e23]:
      - generic [ref=e24]: Mật khẩu
      - generic [ref=e26]:
        - textbox "Nhập mật khẩu" [ref=e27]
        - img [ref=e29] [cursor=pointer]
    - button "Đăng nhập" [ref=e32] [cursor=pointer]:
      - generic [ref=e33]: Đăng nhập
  - link " Hướng dẫn sử dụng" [ref=e35] [cursor=pointer]:
    - /url: /user-guide
    - generic [ref=e36]: 
    - text: Hướng dẫn sử dụng
```

# Test source

```ts
  1  | import type { Page } from '@playwright/test';
  2  | 
  3  | export async function loginAsAdmin(page: Page): Promise<void> {
  4  |   await page.goto('/login');
> 5  |   await page.locator('#email').fill('superadmin@system.local');
     |                                ^ Error: locator.fill: Test timeout of 30000ms exceeded.
  6  |   await page.locator('input[type="password"]').fill('Admin@123456');
  7  |   await page.locator('button[type="submit"]').click();
  8  |   await page.waitForURL(/\/dashboard/, { timeout: 10000 });
  9  | }
  10 | 
  11 | export async function navigateClientSide(page: Page, path: string): Promise<void> {
  12 |   await page.evaluate((p) => {
  13 |     window.history.pushState({}, '', p);
  14 |     window.dispatchEvent(new PopStateEvent('popstate'));
  15 |   }, path);
  16 |   await page.waitForTimeout(300);
  17 | }
```