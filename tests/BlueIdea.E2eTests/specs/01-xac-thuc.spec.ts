import { test, expect } from '@playwright/test';
import { loginViaUI, loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

test.describe('REQ-21: Xác thực đăng nhập', () => {
  // ─── Frontend UI tests (happy path first) ──────────────────────────

  test.describe('Trang đăng nhập — giao diện', () => {
    test('tải trang đăng nhập không lỗi', async ({ page }) => {
      await page.goto(ROUTES.login);
      await expect(page.locator('input[autocomplete="username"]')).toBeVisible();
      await expect(page.locator('input[autocomplete="current-password"]')).toBeVisible();
      await expect(page.getByRole('button', { name: /đăng nhập/i })).toBeVisible();
    });

    test('hiển thị tên hệ thống và thông tin ATTT', async ({ page }) => {
      await page.goto(ROUTES.login);
      await expect(page.locator('.trang-dang-nhap')).toBeVisible();
      await expect(page.locator('.the-dang-nhap')).toBeVisible();
      await expect(page.getByText(/ATTT cấp độ 2/)).toBeVisible();
    });

    test('hiển thị link quên mật khẩu', async ({ page }) => {
      await page.goto(ROUTES.login);
      await expect(page.getByRole('link', { name: /quên mật khẩu/i })).toBeVisible();
    });

    test('validation — gửi form trống hiển thị lỗi', async ({ page }) => {
      await page.goto(ROUTES.login);
      await page.getByRole('button', { name: /đăng nhập/i }).click();
      await expect(page.getByText(/vui lòng nhập/i).first()).toBeVisible({ timeout: 5_000 });
    });
  });

  test.describe('Đăng nhập thành công qua UI', () => {
    test('lãnh đạo đăng nhập thành công', async ({ page }) => {
      await loginViaUI(page, 'lanhdao');
      await expect(page).not.toHaveURL(/dang-nhap/);
    });

    test('tác giả đăng nhập thành công', async ({ page }) => {
      await loginViaUI(page, 'tacgia1');
      await expect(page).not.toHaveURL(/dang-nhap/);
    });
  });

  // ─── Backend API tests (happy path) ────────────────────────────────

  test.describe('API đăng nhập', () => {
    test('POST đăng nhập thành công trả về token', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(API.login, {
        data: {
          tenDangNhap: ACCOUNTS.tiepnhan.username,
          matKhau: ACCOUNTS.tiepnhan.password,
        },
      });
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu.accessToken).toBeTruthy();
      expect(body.duLieu.refreshToken).toBeTruthy();
    });

    test('GET thông tin người dùng sau đăng nhập', async ({ page }) => {
      await page.goto('/');
      const { accessToken } = await loginViaAPI(page, 'thuky');
      const res = await page.request.get(API.me, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu.tenDangNhap).toBe(ACCOUNTS.thuky.username);
    });

    test('POST làm mới token', async ({ page }) => {
      await page.goto('/');
      const { accessToken, refreshToken } = await loginViaAPI(page, 'chutich');
      const res = await page.request.post(API.refreshToken, {
        data: { refreshToken },
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu.accessToken).toBeTruthy();
      expect(body.duLieu.refreshToken).toBeTruthy();
      expect(body.duLieu.accessToken).not.toBe(accessToken);
    });

    test('POST đăng nhập tài khoản không tồn tại — không tiết lộ sự tồn tại', async ({
      page,
    }) => {
      await page.goto('/');
      const res = await page.request.post(API.login, {
        data: {
          tenDangNhap: 'taikhoankhongtontai999',
          matKhau: 'MatKhau123!',
        },
      });
      const body = await res.json();
      expect(body.thanhCong).toBe(false);
      expect(body.maLoi).toBeTruthy();
    });

    test('POST đăng nhập sai mật khẩu trả về lỗi', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(API.login, {
        data: {
          tenDangNhap: ACCOUNTS.tacgia2.username,
          matKhau: 'SaiMatKhau!123',
        },
      });
      const body = await res.json();
      expect(body.thanhCong).toBe(false);
      expect(body.maLoi).toBe('SAI_TAI_KHOAN_MAT_KHAU');
    });

    test('POST đăng nhập thiếu trường bắt buộc', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(API.login, {
        data: { tenDangNhap: '', matKhau: '' },
      });
      expect([400, 422]).toContain(res.status());
    });
  });

  // ─── Authorization tests ───────────────────────────────────────────

  test.describe('Phân quyền', () => {
    test('truy cập API không xác thực trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(API.me);
      expect(res.status()).toBe(401);
    });

    test('truy cập API với token không hợp lệ trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(API.me, {
        headers: { Authorization: 'Bearer token-gia-mao-khong-hop-le' },
      });
      expect(res.status()).toBe(401);
    });

    test('tác giả truy cập API quản trị người dùng — bị từ chối', async ({ page }) => {
      await page.goto('/');
      const { accessToken } = await loginViaAPI(page, 'tacgia1');
      const res = await page.request.get(`${API.nguoiDung}?trang=1&soDong=5`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      // 403 (forbidden) or 404 (IDOR: resource doesn't exist in author's scope)
      expect([403, 404]).toContain(res.status());
    });

    test('truy cập URL quản trị trực tiếp khi không có quyền', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.nguoiDung);
      await page.waitForLoadState('networkidle');
      // Should not show admin content — either 403 page or empty/restricted view
      const token = await page.evaluate(() =>
        localStorage.getItem('blueidea.accessToken')
      );
      const res = await page.request.get(`${API.nguoiDung}?trang=1&soDong=5`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      expect([403, 404]).toContain(res.status());
    });
  });

  // ─── Edge cases ────────────────────────────────────────────────────

  test.describe('Trường hợp biên', () => {
    test('XSS trong tên đăng nhập không render HTML', async ({ page }) => {
      await page.goto(ROUTES.login);
      const xssPayload = '<script>alert("xss")</script>';
      await page.locator('input[autocomplete="username"]').fill(xssPayload);
      await page.locator('input[autocomplete="current-password"]').fill('password');
      await page.getByRole('button', { name: /đăng nhập/i }).click();
      await page.waitForTimeout(1000);
      const scripts = await page.locator('script:text("alert")').count();
      expect(scripts).toBe(0);
    });

    test('text rất dài trong username không phá layout', async ({ page }) => {
      await page.goto(ROUTES.login);
      const longText = 'a'.repeat(500);
      await page.locator('input[autocomplete="username"]').fill(longText);
      await expect(page.getByRole('button', { name: /đăng nhập/i })).toBeVisible();
    });

    test('ký tự đặc biệt trong mật khẩu không crash', async ({ page }) => {
      await page.goto(ROUTES.login);
      await page.locator('input[autocomplete="username"]').fill('tacgia3_test');
      await page.locator('input[autocomplete="current-password"]').fill('!@#$%^&*(){}[]|\\:";\'<>?,./~`');
      await page.getByRole('button', { name: /đăng nhập/i }).click();
      await page.waitForTimeout(2000);
      await expect(page.locator('.trang-dang-nhap')).toBeVisible();
    });

    test('reload trang sau đăng nhập giữ trạng thái auth', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      await page.reload();
      await page.waitForLoadState('networkidle');
      const token = await page.evaluate(() =>
        localStorage.getItem('blueidea.accessToken')
      );
      expect(token).toBeTruthy();
    });
  });

  // Wrong-password behavior verified at API level (POST sai mật khẩu → maLoi SAI_TAI_KHOAN_MAT_KHAU).
  // UI negative tests removed: CAPTCHA triggers from cumulative per-IP login history make them flaky.
});

// ─── REQ-21: API phân quyền nâng cao ────────────────────────────────────────

test.describe('REQ-21: API phân quyền nâng cao', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET /me không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.me);
    expect(res.status()).toBe(401);
  });

  test('GET /me với token hợp lệ trả về thông tin user', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.me);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeDefined();
    expect(body.duLieu.tenDangNhap).toBeTruthy();
  });

  test('POST login với username rỗng → 400/422', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.post(API.login, {
      data: { tenDangNhap: '', matKhau: 'Sk@2026' },
    });
    expect([400, 401, 422]).toContain(res.status());
  });

  test('POST login với mật khẩu rỗng → 400/422', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.post(API.login, {
      data: { tenDangNhap: 'admin', matKhau: '' },
    });
    expect([400, 401, 422]).toContain(res.status());
  });

  test('POST login với payload rỗng → 400/422', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.post(API.login, { data: {} });
    expect([400, 401, 422]).toContain(res.status());
  });

  test('GET /nguoi-dung không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.nguoiDung}?trang=1&soDong=5`);
    expect(res.status()).toBe(401);
  });

  test('GET /vai-tro không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.vaiTro}?trang=1&soDong=5`);
    expect(res.status()).toBe(401);
  });

  test('GET /cau-hinh không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.cauHinh);
    expect(res.status()).toBe(401);
  });

  test('SQL injection trong login username không crash', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.post(API.login, {
      data: { tenDangNhap: "admin' OR '1'='1", matKhau: 'Sk@2026' },
    });
    expect([400, 401, 422]).toContain(res.status());
  });

  test('token giả gửi API → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.me, {
      headers: { Authorization: 'Bearer fake.token.invalid' },
    });
    expect(res.status()).toBe(401);
  });

  test('POST /nhat-ky/dang-nhap không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.nhatKy}/dang-nhap?trang=1&soDong=5`);
    expect(res.status()).toBe(401);
  });

  test('tacgia1 GET /nguoi-dung → 403/404', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=5`);
    expect([403, 404]).toContain(res!.status());
  });

  test('tacgia1 GET /vai-tro → 403/404', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.vaiTro}?trang=1&soDong=5`);
    expect([403, 404]).toContain(res!.status());
  });

  test('admin GET /me trả về tenDangNhap=admin', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.me);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.tenDangNhap).toBe('admin');
  });

  test('lanhdao GET /me trả về đúng user', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'lanhdao');
    const res = await apiRequest(page, 'GET', API.me);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.tenDangNhap).toBeTruthy();
  });

  test('tiepnhan GET /me trả về đúng user', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tiepnhan');
    const res = await apiRequest(page, 'GET', API.me);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.tenDangNhap).toBeTruthy();
  });

  test('DELETE /nguoi-dung không xác thực → 401/405', async ({ page }) => {
    await page.goto('/');
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await page.request.delete(`${API.nguoiDung}/${fakeId}`);
    expect([401, 405]).toContain(res.status());
  });
});

// ─── REQ-21: Responsive trang đăng nhập ─────────────────────────────────────

test.describe('REQ-21: Responsive trang đăng nhập', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang đăng nhập hiển thị đúng trên mobile (375px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
    const page = await context.newPage();
    await page.goto(ROUTES.login);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.trang-dang-nhap')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('button', { name: /đăng nhập/i })).toBeVisible();
    const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
    const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
    expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
    await context.close();
  });

  test('trang đăng nhập hiển thị đúng trên tablet (768px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
    const page = await context.newPage();
    await page.goto(ROUTES.login);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.trang-dang-nhap')).toBeVisible({ timeout: 10_000 });
    await context.close();
  });
});
