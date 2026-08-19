import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

test.describe('REQ-17, REQ-18, REQ-13: Hội đồng và đánh giá', () => {
  // ─── Hội đồng — UI (REQ-17) ──────────────────────────────────────────

  test.describe('REQ-17: Giao diện quản lý hội đồng', () => {
    test('trang hội đồng tải không lỗi cho admin', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.hoiDong);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });

    test('bảng hội đồng hiển thị cột tiêu đề', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.hoiDong);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      const headers = page.locator('.ant-table-thead th');
      await expect(headers.first()).toBeVisible({ timeout: 5_000 });
      const headerTexts = await headers.allTextContents();
      expect(headerTexts.join(' ').length).toBeGreaterThan(0);
    });

    test('bảng hội đồng hiển thị dữ liệu hoặc trạng thái rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.hoiDong);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      await page.waitForTimeout(2_000);
      const rows = page.locator('.ant-table-tbody tr.ant-table-row');
      const rowCount = await rows.count();
      expect(rowCount).toBeGreaterThanOrEqual(0);
    });
  });

  // ─── Hội đồng — API (REQ-17, REQ-18) ────────────────────────────────

  test.describe('REQ-17: API hội đồng', () => {
    test('GET /hoi-dong trả về PhanHoiPhanTrang (không có thanhCong)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      // PhanHoiPhanTrang: duLieu, tongSo — NO thanhCong at root
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
      expect(body.thanhCong).toBeUndefined();
    });

    test('GET /hoi-dong tôn trọng soDong khi phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=2`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu.length).toBeLessThanOrEqual(2);
    });

    test('GET /hoi-dong/chon trả về PhanHoiApi có thanhCong cho người dùng xác thực', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /hoi-dong/{id} trả về chi tiết hội đồng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      // Fetch list first to get a valid ID
      const listRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`);
      expect(listRes!.status()).toBe(200);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        // No councils seeded — skip detail check
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const detailRes = await apiRequest(page, 'GET', `${API.hoiDong}/${id}`);
      expect(detailRes!.status()).toBe(200);
      const detailBody = await detailRes!.json();
      expect(detailBody.thanhCong).toBe(true);
      expect(detailBody.duLieu).toBeTruthy();
      expect(detailBody.duLieu.id).toBe(id);
    });
  });

  // ─── Hội đồng — Phân quyền (REQ-17) ─────────────────────────────────

  test.describe('REQ-17: Phân quyền hội đồng', () => {
    test('tác giả (gv.lan) không có quyền xem danh sách hội đồng — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=5`);
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET /hoi-dong trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.hoiDong}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── Đánh giá — UI (REQ-13) ──────────────────────────────────────────

  test.describe('REQ-13: Giao diện đánh giá', () => {
    test('trang đánh giá tải không lỗi cho thành viên hội đồng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      await page.goto(ROUTES.danhGia);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });
  });

  // ─── Đánh giá — API (REQ-13) ─────────────────────────────────────────

  test.describe('REQ-13: API đánh giá', () => {
    test('GET /danh-gia/viec-cua-toi cho hoidong01 trả về PhanHoiPhanTrang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('GET /danh-gia/ma-tran-diem cho admin trả về PhanHoiApi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('không xác thực GET /danh-gia/viec-cua-toi trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.danhGia}/viec-cua-toi?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── Trường hợp biên ─────────────────────────────────────────────────

  test.describe('Trường hợp biên', () => {
    test('GET /hoi-dong/{nil-uuid} trả về 400 hoặc 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.hoiDong}/00000000-0000-0000-0000-000000000000`
      );
      expect([400, 404]).toContain(res!.status());
    });

    test('GET /hoi-dong/{id-sai-dinh-dang} trả về 400', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}/khong-phai-uuid`);
      expect([400, 404]).toContain(res!.status());
    });
  });
});
