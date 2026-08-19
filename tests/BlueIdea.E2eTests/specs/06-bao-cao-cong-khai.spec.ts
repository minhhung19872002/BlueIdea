import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

test.describe('REQ-34 đến REQ-40: Báo cáo thống kê', () => {
  // ─── Giao diện trang báo cáo ──────────────────────────────────────

  test.describe('Trang báo cáo — giao diện', () => {
    test('admin tải trang báo cáo không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.baoCao);
      await page.waitForLoadState('networkidle');
      // Page should load without error — either report content or empty state
      await expect(page.locator('body')).not.toContainText('500');
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('lãnh đạo tải trang báo cáo không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      await page.goto(ROUTES.baoCao);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).not.toContainText('500');
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });
  });

  // ─── API báo cáo (REQ-34 đến REQ-40) ────────────────────────────

  test.describe('API báo cáo — admin', () => {
    test('GET tổng quan trả về thống kê hợp lệ (REQ-34)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeDefined();
      expect(typeof body.duLieu).toBe('object');
    });

    test('GET sáng kiến đạt trả về mảng (REQ-35)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });

    test('GET sáng kiến chưa đạt trả về mảng (REQ-36)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });

    test('GET theo đơn vị trả về mảng (REQ-37)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });

    test('GET theo tác giả trả về mảng (REQ-38)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });

    test('GET thời gian xử lý trả về mảng (REQ-39)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });

    test('GET tổng hợp năm 2026 trả về dữ liệu báo cáo (REQ-40)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2026`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeDefined();
      expect(typeof body.duLieu).toBe('object');
    });
  });

  // ─── Phân quyền báo cáo ──────────────────────────────────────────

  test.describe('Phân quyền báo cáo', () => {
    test('tác giả không có quyền xem báo cáo — bị từ chối (403)', async ({ page }) => {
      await page.goto('/');
      const { accessToken } = await loginViaAPI(page, 'tacgia1');
      const res = await page.request.get(`${API.baoCao}/tong-quan`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      expect([403, 404]).toContain(res.status());
    });

    test('không xác thực truy cập API báo cáo — trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/tong-quan`);
      expect(res.status()).toBe(401);
    });
  });
});

test.describe('REQ-23 đến REQ-25: Cổng thông tin công khai & Tra cứu', () => {
  // ─── Giao diện cổng công khai ─────────────────────────────────────

  test.describe('Trang công khai — giao diện', () => {
    test('trang tra cứu công khai tải được mà không cần đăng nhập', async ({ page }) => {
      await page.goto(ROUTES.congKhai);
      await page.waitForLoadState('networkidle');
      // Public portal must load without authentication
      await expect(page.locator('body')).not.toContainText('401');
      await expect(page.locator('body')).not.toContainText('403');
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });
  });

  // ─── API công khai (AllowAnonymous) ───────────────────────────────

  test.describe('API công khai — không cần xác thực (REQ-23)', () => {
    test('GET cong-khai/sang-kien trả về dữ liệu phân trang (anonymous)', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      // PhanHoiPhanTrang: no thanhCong field, has tongSo/trang/soDong/tongTrang
      expect(body.duLieu).toBeDefined();
      expect(Array.isArray(body.duLieu)).toBe(true);
      expect(typeof body.tongSo).toBe('number');
      expect(typeof body.trang).toBe('number');
      expect(typeof body.soDong).toBe('number');
      expect(typeof body.tongTrang).toBe('number');
    });

    test('GET cong-khai/thong-ke trả về thống kê (anonymous, REQ-24)', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/thong-ke`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeDefined();
      expect(typeof body.duLieu).toBe('object');
    });

    test('GET cong-khai/linh-vuc trả về danh sách lĩnh vực (anonymous, REQ-25)', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/linh-vuc`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });
  });
});
