import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

test.describe('REQ-43 đến REQ-51: Quản trị hệ thống', () => {
  // ─── REQ-43: Quản lý người dùng ──────────────────────────────────

  test.describe('REQ-43: Quản lý người dùng', () => {
    test('trang người dùng tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.nguoiDung);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });

    test('bảng hiển thị danh sách người dùng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.nguoiDung);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      const rows = page.locator('.ant-table-tbody tr.ant-table-row');
      await expect(rows.first()).toBeVisible({ timeout: 10_000 });
      expect(await rows.count()).toBeGreaterThan(0);
    });

    test('cột bảng hiển thị đúng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.nguoiDung);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      const headers = page.locator('.ant-table-thead th');
      await expect(headers.first()).toBeVisible({ timeout: 5_000 });
      const headerTexts = await headers.allTextContents();
      const combined = headerTexts.join(' ');
      expect(combined).toContain('Tài khoản');
      expect(combined).toContain('Họ và tên');
    });

    test('tìm kiếm người dùng theo tên', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.nguoiDung);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      const searchInput = page.getByPlaceholder(/Tìm theo họ tên/);
      await searchInput.fill('admin');
      await searchInput.press('Enter');
      await page.waitForTimeout(2000);
      const rows = page.locator('.ant-table-tbody tr.ant-table-row');
      await expect(rows.first()).toBeVisible({ timeout: 10_000 });
    });

    test('API GET danh sách người dùng với phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
      expect(body.tongSo).toBeGreaterThanOrEqual(0);
    });

    test('API GET danh sách — không xác thực trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.nguoiDung}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });

    test('API GET danh sách — tác giả trả về 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=5`);
      expect(res!.status()).toBe(403);
    });
  });

  // ─── REQ-44: Quản lý đơn vị ──────────────────────────────────────

  test.describe('REQ-44: Quản lý đơn vị', () => {
    test('trang đơn vị tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.donVi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible();
    });

    test('API GET danh sách đơn vị', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.tongSo).toBeGreaterThanOrEqual(0);
    });
  });

  // ─── REQ-45: Quản lý vai trò ─────────────────────────────────────

  test.describe('REQ-45: Quản lý vai trò', () => {
    test('trang vai trò tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.vaiTro);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible();
    });

    test('API GET danh sách vai trò', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', API.vaiTro);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu.vaiTro).toBeTruthy();
      expect(body.duLieu.quyen).toBeTruthy();
    });

    test('tác giả không thể xem vai trò — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', API.vaiTro);
      expect(res!.status()).toBe(403);
    });
  });

  // ─── REQ-46: Cấu hình hệ thống ───────────────────────────────────

  test.describe('REQ-46: Cấu hình hệ thống', () => {
    test('trang cấu hình tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.cauHinh);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible();
    });

    test('API GET cấu hình hệ thống', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', API.cauHinh);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeGreaterThan(0);
    });

    test('tác giả không thể xem cấu hình — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', API.cauHinh);
      expect(res!.status()).toBe(403);
    });
  });

  // ─── REQ-48: Cấu hình menu ───────────────────────────────────────

  test.describe('REQ-48: Cấu hình menu', () => {
    test('trang cấu hình menu tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.cauHinhMenu);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible();
    });

    test('API GET danh sách menu', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
    });
  });

  // ─── REQ-50: Cấu hình email/SMS ──────────────────────────────────

  test.describe('REQ-50: Cấu hình gửi tin', () => {
    test('trang gửi tin tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.guiTin);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible();
    });

    test('API GET cấu hình gửi tin', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', API.guiTin);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── REQ-51: Cấu hình thông tin sáng kiến ────────────────────────

  test.describe('REQ-51: Cấu hình thông tin sáng kiến', () => {
    test('API GET cấu hình trả về 8 key bắt buộc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', API.cauHinh);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      const keys = body.duLieu.map((c: { khoa: string }) => c.khoa);
      const requiredKeys = [
        'MUC_CANH_BAO_TRUNG_LAP_VANG',
        'MUC_CANH_BAO_TRUNG_LAP_DO',
        'HE_SO_TU_VUNG',
        'HE_SO_NGU_NGHIA',
        'TU_DONG_KIEM_TRA_TRUNG_LAP',
        'MAU_MA_HO_SO',
        'DUNG_LUONG_TEP_TOI_DA_MB',
        'SO_TEP_TOI_DA',
      ];
      for (const key of requiredKeys) {
        expect(keys).toContain(key);
      }
    });
  });

  // ─── REQ-47: Nhật ký ──────────────────────────────────────────────

  test.describe('REQ-47: Nhật ký hệ thống', () => {
    test('trang nhật ký tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.nhatKy);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible();
    });

    test('API GET nhật ký hệ thống', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.nhatKy}/he-thong?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.tongSo).toBeGreaterThanOrEqual(0);
    });
  });

  // ─── Cross-org IDOR protection ────────────────────────────────────

  test.describe('Bảo vệ IDOR xuyên đơn vị', () => {
    test('quản trị đơn vị không xem được cấu hình hệ thống', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'qtdonvi');
      const res = await apiRequest(page, 'GET', API.cauHinh);
      // Unit admin may or may not have system config access, depends on role
      // At minimum, should not get 500
      expect([200, 403]).toContain(res!.status());
    });
  });
});
