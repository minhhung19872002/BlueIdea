import { type Page, type BrowserContext, expect } from '@playwright/test';
import { ACCOUNTS, API, ROUTES } from './constants';

type AccountKey = keyof typeof ACCOUNTS;

export async function loginViaUI(page: Page, account: AccountKey): Promise<void> {
  const { username, password } = ACCOUNTS[account];
  await page.goto(ROUTES.login);
  await page.locator('input[autocomplete="username"]').fill(username);
  await page.locator('input[autocomplete="current-password"]').fill(password);
  await page.getByRole('button', { name: /đăng nhập/i }).click();
  await page.waitForURL(url => !url.pathname.includes('dang-nhap'), { timeout: 15_000 });
}

export async function loginViaAPI(
  page: Page,
  account: AccountKey
): Promise<{ accessToken: string; refreshToken: string }> {
  const { username, password } = ACCOUNTS[account];
  const response = await page.request.post(API.login, {
    data: { tenDangNhap: username, matKhau: password },
  });
  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  const { accessToken, refreshToken } = body.duLieu;

  await page.evaluate(
    ({ accessToken, refreshToken }) => {
      localStorage.setItem('blueidea.accessToken', accessToken);
      localStorage.setItem('blueidea.refreshToken', refreshToken);
    },
    { accessToken, refreshToken }
  );

  return { accessToken, refreshToken };
}

export async function setupAuthenticatedPage(
  context: BrowserContext,
  account: AccountKey
): Promise<Page> {
  const page = await context.newPage();
  await page.goto('/');
  await loginViaAPI(page, account);
  return page;
}

export async function apiRequest(
  page: Page,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  url: string,
  data?: Record<string, unknown>
) {
  const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
  const options: Record<string, unknown> = {
    headers: { Authorization: `Bearer ${token}` },
  };
  if (data) options.data = data;

  switch (method) {
    case 'GET':
      return page.request.get(url, options);
    case 'POST':
      return page.request.post(url, options);
    case 'PUT':
      return page.request.put(url, options);
    case 'DELETE':
      return page.request.delete(url, options);
  }
}
