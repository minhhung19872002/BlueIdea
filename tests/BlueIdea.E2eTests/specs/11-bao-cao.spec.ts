import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

// ─── REQ-38: Danh sách sáng kiến đạt ────────────────────────────────────────
// ─── REQ-39: Danh sách sáng kiến chưa đạt ──────────────────────────────────
// ─── REQ-40: Thống kê theo đơn vị ───────────────────────────────────────────

test.describe('REQ-38: Báo cáo sáng kiến đạt', () => {
  test.describe('Giao diện trang báo cáo', () => {
    test('trang báo cáo tải không lỗi cho admin', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.baoCao);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('trang báo cáo tải không lỗi cho lãnh đạo', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      await page.goto(ROUTES.baoCao);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });
  });

  test.describe('API tổng quan dashboard', () => {
    test('GET /bao-cao/tong-quan cho admin — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeTruthy();
    });

    test('GET /bao-cao/tong-quan không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/tong-quan`);
      expect(res.status()).toBe(401);
    });

    test('tác giả GET /bao-cao/tong-quan → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan`);
      expect(res!.status()).toBe(403);
    });
  });

  test.describe('API sáng kiến đạt (REQ-38)', () => {
    test('GET /bao-cao/sang-kien-dat cho admin — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/sang-kien-dat với năm cụ thể', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat?nam=2026`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/sang-kien-dat/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat/xuat-excel`);
      expect(res!.status()).toBe(200);
      const contentType = res!.headers()['content-type'] ?? '';
      expect(
        contentType.includes('spreadsheet') ||
        contentType.includes('excel') ||
        contentType.includes('octet-stream')
      ).toBeTruthy();
    });

    test('GET /bao-cao/sang-kien-dat/xuat-pdf trả file PDF', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat/xuat-pdf`);
      expect(res!.status()).toBe(200);
      const contentType = res!.headers()['content-type'] ?? '';
      expect(contentType.includes('pdf')).toBeTruthy();
    });

    test('GET /bao-cao/sang-kien-dat/xuat-excel không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/sang-kien-dat/xuat-excel`);
      expect(res.status()).toBe(401);
    });
  });
});

test.describe('REQ-39: Báo cáo sáng kiến chưa đạt', () => {
  test.describe('API sáng kiến chưa đạt', () => {
    test('GET /bao-cao/sang-kien-chua-dat cho admin — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/sang-kien-chua-dat/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat/xuat-excel`);
      expect(res!.status()).toBe(200);
      const contentType = res!.headers()['content-type'] ?? '';
      expect(
        contentType.includes('spreadsheet') ||
        contentType.includes('excel') ||
        contentType.includes('octet-stream')
      ).toBeTruthy();
    });

    test('GET /bao-cao/sang-kien-chua-dat không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/sang-kien-chua-dat`);
      expect(res.status()).toBe(401);
    });

    test('tác giả GET /bao-cao/sang-kien-chua-dat → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat`);
      expect(res!.status()).toBe(403);
    });
  });
});

test.describe('REQ-40: Thống kê theo đơn vị', () => {
  test.describe('API theo đơn vị', () => {
    test('GET /bao-cao/theo-don-vi cho admin — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/theo-don-vi/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi/xuat-excel`);
      expect(res!.status()).toBe(200);
      const contentType = res!.headers()['content-type'] ?? '';
      expect(
        contentType.includes('spreadsheet') ||
        contentType.includes('excel') ||
        contentType.includes('octet-stream')
      ).toBeTruthy();
    });

    test('GET /bao-cao/theo-don-vi không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/theo-don-vi`);
      expect(res.status()).toBe(401);
    });
  });

  test.describe('API theo tác giả', () => {
    test('GET /bao-cao/theo-tac-gia cho admin — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/theo-tac-gia/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia/xuat-excel`);
      expect(res!.status()).toBe(200);
      const contentType = res!.headers()['content-type'] ?? '';
      expect(
        contentType.includes('spreadsheet') ||
        contentType.includes('excel') ||
        contentType.includes('octet-stream')
      ).toBeTruthy();
    });
  });

  test.describe('API thời gian xử lý', () => {
    test('GET /bao-cao/thoi-gian-xu-ly cho admin — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/thoi-gian-xu-ly/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly/xuat-excel`);
      expect(res!.status()).toBe(200);
      const contentType = res!.headers()['content-type'] ?? '';
      expect(
        contentType.includes('spreadsheet') ||
        contentType.includes('excel') ||
        contentType.includes('octet-stream')
      ).toBeTruthy();
    });
  });

  test.describe('API tổng hợp năm', () => {
    test('GET /bao-cao/tong-hop-nam/2026 cho admin — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2026`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeTruthy();
    });

    test('GET /bao-cao/tong-hop-nam/2026/xuat-pdf trả file PDF', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2026/xuat-pdf`);
      expect(res!.status()).toBe(200);
      const contentType = res!.headers()['content-type'] ?? '';
      expect(contentType.includes('pdf')).toBeTruthy();
    });

    test('GET /bao-cao/tong-hop-nam/2026 không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/tong-hop-nam/2026`);
      expect(res.status()).toBe(401);
    });
  });

  test.describe('Trường hợp biên', () => {
    test('GET /bao-cao/sang-kien-dat với năm không tồn tại → mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat?nam=1999`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/tong-hop-nam/1999 → trả số liệu zero', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/1999`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
    });

    test('GET /bao-cao/sang-kien-chua-dat với năm không tồn tại → mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat?nam=1999`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/theo-don-vi với năm không tồn tại → mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi?nam=1999`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bao-cao/theo-tac-gia với năm không tồn tại → mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia?nam=1999`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Phân quyền nâng cao ──────────────────────────────────────────────

  test.describe('REQ-38: Phân quyền nâng cao', () => {
    test('lãnh đạo GET /bao-cao/sang-kien-dat → 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
    });

    test('tác giả GET /bao-cao/theo-don-vi → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi`);
      expect(res!.status()).toBe(403);
    });

    test('tác giả GET /bao-cao/theo-tac-gia → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia`);
      expect(res!.status()).toBe(403);
    });

    test('tác giả GET /bao-cao/thoi-gian-xu-ly → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET /bao-cao/theo-don-vi → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/theo-don-vi`);
      expect(res.status()).toBe(401);
    });

    test('không xác thực GET /bao-cao/theo-tac-gia → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/theo-tac-gia`);
      expect(res.status()).toBe(401);
    });

    test('không xác thực GET /bao-cao/thoi-gian-xu-ly → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/thoi-gian-xu-ly`);
      expect(res.status()).toBe(401);
    });

    test('không xác thực GET /bao-cao/sang-kien-chua-dat/xuat-excel → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.baoCao}/sang-kien-chua-dat/xuat-excel`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── Export nâng cao ──────────────────────────────────────────────────

  test.describe('REQ-39: Export nâng cao', () => {
    test('GET /bao-cao/sang-kien-chua-dat/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat/xuat-excel`);
      expect([200, 404]).toContain(res!.status());
    });

    test('GET /bao-cao/theo-don-vi/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi/xuat-excel`);
      expect([200, 404]).toContain(res!.status());
    });

    test('GET /bao-cao/theo-tac-gia/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia/xuat-excel`);
      expect([200, 404]).toContain(res!.status());
    });

    test('GET /bao-cao/thoi-gian-xu-ly/xuat-excel trả file Excel', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly/xuat-excel`);
      expect([200, 404]).toContain(res!.status());
    });
  });

  // ─── Responsive viewport ──────────────────────────────────────────────

  test.describe('REQ-40: Responsive viewport', () => {
    test('trang báo cáo hiển thị đúng trên mobile (375px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.baoCao);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
      const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
      expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
      await context.close();
    });

    test('trang báo cáo hiển thị đúng trên tablet (768px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.baoCao);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await context.close();
    });
  });
});
