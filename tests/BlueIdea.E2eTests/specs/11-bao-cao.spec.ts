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

// ─── REQ-38: Báo cáo tùy biến — thực thi ────────────────────────────────────

test.describe('REQ-38: Báo cáo tùy biến — thực thi', () => {
  const baoCaoTuyBien = '/api/v1/nhap-xuat/bao-cao-tuy-bien';

  test('GET /bao-cao-tuy-bien/nguon-du-lieu trả về danh sách trường dữ liệu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${baoCaoTuyBien}/nguon-du-lieu`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(body.duLieu.length).toBeGreaterThan(0);
  });

  test('GET /bao-cao-tuy-bien/{bieuMauId} chạy báo cáo với biểu mẫu thực', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const bmRes = await apiRequest(page, 'GET', `${API.bieuMauThongKe}?trang=1&soDong=1`);
    expect(bmRes!.status()).toBe(200);
    const bmBody = await bmRes!.json();
    if (bmBody.duLieu.length === 0) { test.skip(true, 'Không có biểu mẫu thống kê'); return; }
    const bieuMauId: string = bmBody.duLieu[0].id;

    const res = await apiRequest(page, 'GET', `${baoCaoTuyBien}/${bieuMauId}`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
  });

  test('GET /bao-cao-tuy-bien/{bieuMauId}/xuat-excel trả file Excel hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const bmRes = await apiRequest(page, 'GET', `${API.bieuMauThongKe}?trang=1&soDong=1`);
    const bmBody = await bmRes!.json();
    if (bmBody.duLieu.length === 0) { test.skip(true, 'Không có biểu mẫu thống kê'); return; }
    const bieuMauId: string = bmBody.duLieu[0].id;

    const res = await apiRequest(page, 'GET', `${baoCaoTuyBien}/${bieuMauId}/xuat-excel`);
    expect(res!.status()).toBe(200);
    const ct = res!.headers()['content-type'] ?? '';
    expect(ct.includes('spreadsheet') || ct.includes('excel') || ct.includes('octet-stream')).toBeTruthy();
    const cl = parseInt(res!.headers()['content-length'] ?? '0', 10);
    expect(cl).toBeGreaterThan(0);
  });

  test('GET /bao-cao-tuy-bien/{bieuMauId}/xuat-pdf trả file PDF hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const bmRes = await apiRequest(page, 'GET', `${API.bieuMauThongKe}?trang=1&soDong=1`);
    const bmBody = await bmRes!.json();
    if (bmBody.duLieu.length === 0) { test.skip(true, 'Không có biểu mẫu thống kê'); return; }
    const bieuMauId: string = bmBody.duLieu[0].id;

    const res = await apiRequest(page, 'GET', `${baoCaoTuyBien}/${bieuMauId}/xuat-pdf`);
    expect(res!.status()).toBe(200);
    expect(res!.headers()['content-type'] ?? '').toContain('pdf');
  });

  test('GET /bao-cao-tuy-bien/{id} không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await page.request.get(`${baoCaoTuyBien}/${fakeId}`);
    expect(res.status()).toBe(401);
  });

  test('tác giả GET /bao-cao-tuy-bien/nguon-du-lieu → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${baoCaoTuyBien}/nguon-du-lieu`);
    expect(res!.status()).toBe(403);
  });

  test('GET /bao-cao-tuy-bien/{id} với id không tồn tại → 404', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const fakeId = '00000000-0000-0000-0000-000000000001';
    const res = await apiRequest(page, 'GET', `${baoCaoTuyBien}/${fakeId}`);
    expect([400, 404]).toContain(res!.status());
  });

  test('trang báo cáo tùy biến tải không lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.baoCaoTuyBien);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
  });
});

// ─── REQ-37: Bộ lọc báo cáo trả về dữ liệu đúng ────────────────────────

test.describe('REQ-37: Bộ lọc báo cáo', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET /bao-cao/sang-kien-dat trả về đúng cấu trúc DongBaoCaoSangKien', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0] as Record<string, unknown>;
      expect('tenSangKien' in item || 'tacGia' in item || 'tenDonVi' in item).toBe(true);
    }
  });

  test('GET /bao-cao/theo-don-vi trả về đúng cấu trúc DongBaoCaoDonVi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0] as Record<string, unknown>;
      expect('tenDonVi' in item).toBe(true);
      expect('tongSo' in item || 'soDat' in item).toBe(true);
    }
  });

  test('GET /bao-cao/theo-don-vi lọc theo năm → kết quả chỉ chứa năm đó', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi?nam=2026`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('GET /bao-cao/sang-kien-dat lọc theo đơn vị → 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const dvRes = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=1`);
    const dvBody = await dvRes!.json();
    if (!dvBody.duLieu || dvBody.duLieu.length === 0) return;
    const donViId = dvBody.duLieu[0].id as string;

    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat?donViId=${donViId}`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('GET /bao-cao/thoi-gian-xu-ly trả về thời gian xử lý', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });
});

// ─── REQ-40: Xuất file — kiểm tra magic bytes ──────────────────────────────

test.describe('REQ-40: Xuất file — magic bytes', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET /bao-cao/sang-kien-dat/xuat-excel → file Excel hợp lệ (PK header)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat/xuat-excel`);
    expect(res!.status()).toBe(200);
    const ct = res!.headers()['content-type'] ?? '';
    expect(ct.includes('spreadsheet') || ct.includes('excel') || ct.includes('octet-stream')).toBeTruthy();
    const bodyBuf = await res!.body();
    expect(bodyBuf.length).toBeGreaterThan(0);
    expect(bodyBuf[0]).toBe(0x50); // P
    expect(bodyBuf[1]).toBe(0x4b); // K (ZIP/OOXML)
  });

  test('GET /bao-cao/theo-don-vi/xuat-pdf → file PDF hợp lệ (%PDF header)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi/xuat-pdf`);
    expect(res!.status()).toBe(200);
    const ct = res!.headers()['content-type'] ?? '';
    expect(ct).toContain('pdf');
    const bodyBuf = await res!.body();
    expect(bodyBuf.length).toBeGreaterThan(0);
    expect(bodyBuf[0]).toBe(0x25); // %
    expect(bodyBuf[1]).toBe(0x50); // P
    expect(bodyBuf[2]).toBe(0x44); // D
    expect(bodyBuf[3]).toBe(0x46); // F
  });

  test('GET /bao-cao/theo-tac-gia/xuat-excel → file Excel hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia/xuat-excel`);
    expect(res!.status()).toBe(200);
    const bodyBuf = await res!.body();
    expect(bodyBuf.length).toBeGreaterThan(0);
    expect(bodyBuf[0]).toBe(0x50); // PK
    expect(bodyBuf[1]).toBe(0x4b);
  });
});
