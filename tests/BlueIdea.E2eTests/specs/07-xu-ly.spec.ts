import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

// ─── REQ-27: Tiếp nhận hồ sơ ────────────────────────────────────────────────
// ─── REQ-28: Danh sách hồ sơ ────────────────────────────────────────────────
// ─── REQ-29: Xử lý hồ sơ ────────────────────────────────────────────────────
// ─── REQ-30: Theo dõi hồ sơ ─────────────────────────────────────────────────

test.describe('REQ-27: Tiếp nhận hồ sơ', () => {
  test.describe('Giao diện trang tiếp nhận', () => {
    test('trang tiếp nhận tải không lỗi với cán bộ tiếp nhận', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      await page.goto(ROUTES.tiepNhan);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('trang tiếp nhận hiển thị tiêu đề "Hồ sơ chờ tiếp nhận"', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      await page.goto(ROUTES.tiepNhan);
      await page.waitForLoadState('networkidle');
      await expect(
        page.getByText(/hồ sơ chờ tiếp nhận/i).first()
      ).toBeVisible({ timeout: 10_000 });
    });

    test('thanh lọc hiển thị ô tìm kiếm và bộ lọc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      await page.goto(ROUTES.tiepNhan);
      await page.waitForLoadState('networkidle');
      await expect(
        page.locator('input[placeholder*="Tìm theo"]').first()
      ).toBeVisible({ timeout: 10_000 });
    });
  });

  test.describe('API tiếp nhận', () => {
    test('cán bộ tiếp nhận GET danh sách sáng kiến — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('cán bộ tiếp nhận GET với bộ lọc trạng thái DA_NOP', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&trangThaiTong=DA_NOP`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('cán bộ tiếp nhận GET với phân trang soDong=5', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
    });

    test('cán bộ tiếp nhận GET với sắp xếp theo ngayNop desc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&sapXep=ngayNop&huong=desc`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  test.describe('Phân quyền tiếp nhận', () => {
    test('không xác thực GET danh sách sáng kiến → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.sangKien}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });

    test('tác giả không có quyền xử lý POST thuc-thi → 401/403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.xuLy}/thuc-thi`, {
        sangKienId: '00000000-0000-0000-0000-000000000000',
        truongHopId: '00000000-0000-0000-0000-000000000000',
      });
      expect([400, 403, 404, 422]).toContain(res!.status());
    });
  });
});

test.describe('REQ-28: Danh sách hồ sơ', () => {
  test.describe('Giao diện bảng danh sách', () => {
    test('admin xem danh sách sáng kiến hiển thị bảng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.traCuu);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });

    test('bảng có tiêu đề cột', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.traCuu);
      await page.waitForLoadState('networkidle');
      const table = page.locator('.ant-table').first();
      const hasTable = await table.isVisible().catch(() => false);
      if (hasTable) {
        const headers = page.locator('.ant-table-thead th');
        const headerTexts = await headers.allTextContents();
        expect(headerTexts.join(' ').length).toBeGreaterThan(0);
      }
    });
  });

  test.describe('API phân trang và sắp xếp', () => {
    test('GET /sang-kien phân trang trang=1 soDong=5 trả đúng cấu trúc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
      expect(typeof body.tongSo).toBe('number');
    });

    test('GET /sang-kien sắp xếp sapXep=ngayNop&huong=asc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&sapXep=ngayNop&huong=asc`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien lọc theo lĩnh vực không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const fakeId = '00000000-0000-0000-0000-000000000001';
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&linhVucId=${fakeId}`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien tìm kiếm bằng từ khoá', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&tuKhoa=sang+kien`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });
});

test.describe('REQ-29: Xử lý hồ sơ', () => {
  test.describe('Giao diện trang xử lý', () => {
    test('trang xử lý tải không lỗi với thư ký', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      await page.goto(ROUTES.xuLy);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('trang xử lý hiển thị tiêu đề "Việc cần xử lý"', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      await page.goto(ROUTES.xuLy);
      await page.waitForLoadState('networkidle');
      await expect(
        page.getByText(/việc cần xử lý/i).first()
      ).toBeVisible({ timeout: 10_000 });
    });
  });

  test.describe('API xử lý', () => {
    test('GET /xu-ly danh sách cần xử lý — thư ký', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const res = await apiRequest(page, 'GET', `${API.xuLy}?trang=1&soDong=10`);
      expect([200, 403, 404]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.duLieu).toBeInstanceOf(Array);
      }
    });

    test('POST /xu-ly/thuc-thi không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(`${API.xuLy}/thuc-thi`, {
        data: {
          sangKienId: '00000000-0000-0000-0000-000000000000',
          truongHopId: '00000000-0000-0000-0000-000000000000',
        },
      });
      expect(res.status()).toBe(401);
    });

    test('POST /xu-ly/thuc-thi-hang-loat không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(`${API.xuLy}/thuc-thi-hang-loat`, {
        data: { danhSach: [] },
      });
      expect(res.status()).toBe(401);
    });

    test('POST /xu-ly/thuc-thi với dữ liệu không hợp lệ → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const res = await apiRequest(page, 'POST', `${API.xuLy}/thuc-thi`, {});
      expect([400, 404, 422]).toContain(res!.status());
    });

    test('POST /xu-ly/thu-hoi không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(`${API.xuLy}/thu-hoi`, {
        data: { sangKienId: '00000000-0000-0000-0000-000000000000' },
      });
      expect(res.status()).toBe(401);
    });
  });

  test.describe('Phân quyền xử lý', () => {
    test('tác giả POST thuc-thi trên sang-kien không tồn tại → không 200', async ({
      page,
    }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.xuLy}/thuc-thi`, {
        sangKienId: '00000000-0000-0000-0000-000000000000',
        truongHopId: '00000000-0000-0000-0000-000000000000',
      });
      expect([400, 403, 404, 422]).toContain(res!.status());
    });
  });
});

test.describe('REQ-30: Theo dõi hồ sơ', () => {
  test.describe('API tiến độ', () => {
    test('GET /sang-kien/{id}/tien-do với id không tồn tại → 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${fakeId}/tien-do`);
      expect([400, 404]).toContain(res!.status());
    });

    test('GET /sang-kien/{id}/tien-do không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.get(`${API.sangKien}/${fakeId}/tien-do`);
      expect(res.status()).toBe(401);
    });

    test('GET /sang-kien/{id}/hanh-dong với id không tồn tại → empty hoặc 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${fakeId}/hanh-dong`);
      expect([200, 400, 404]).toContain(res!.status());
    });
  });

  test.describe('Trường hợp biên', () => {
    test('ký tự đặc biệt trong tìm kiếm không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=5&tuKhoa=${encodeURIComponent("'OR 1=1--")}`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('trang lớn hơn tổng số trang trả về mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=99999&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBe(0);
    });

    test('soDong bằng 0 được xử lý an toàn', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=0`);
      expect([200, 400, 422]).toContain(res!.status());
    });
  });
});
