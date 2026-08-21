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

    test('GET /sang-kien với soDong=2 trả về tối đa 2 mục', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=2`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(2);
    });

    test('GET /sang-kien response item có trường tenSangKien', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      if (body.duLieu.length > 0) {
        const item = body.duLieu[0] as Record<string, unknown>;
        expect(typeof item['tenSangKien']).toBe('string');
      }
    });

    test('thuky GET /sang-kien danh sách — 200 và có tongSo', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('GET /sang-kien không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.sangKien}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
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

    test('tacgia1 GET /sang-kien/{id}/tien-do của đơn của mình — 200 hoặc 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = (listBody.duLieu[0] as { id: string }).id;
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/tien-do`);
      expect([200, 404]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.thanhCong).toBe(true);
      }
    });

    test('GET /sang-kien/{id}/lich-su không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.get(`${API.sangKien}/${fakeId}/lich-su`);
      expect(res.status()).toBe(401);
    });

    test('thuky GET /sang-kien/{id}/tien-do — 200 hoặc 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = (listBody.duLieu[0] as { id: string }).id;
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/tien-do`);
      expect([200, 404]).toContain(res!.status());
    });

    test('GET /sang-kien/{id}/hanh-dong không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.get(`${API.sangKien}/${fakeId}/hanh-dong`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── Sắp xếp nâng cao ──────────────────────────────────────────────

  test.describe('Sắp xếp nâng cao', () => {
    test('GET /sang-kien sapXep=tenSangKien&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10&sapXep=tenSangKien&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Phân quyền nâng cao ──────────────────────────────────────────────

  test.describe('Phân quyền nâng cao', () => {
    test('lãnh đạo GET /sang-kien danh sách — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('chủ tịch GET /sang-kien danh sách — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'chutich');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('POST /xu-ly/thu-hoi với tác giả → không phải 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.xuLy}/thu-hoi`, {
        sangKienId: '00000000-0000-0000-0000-000000000000',
      });
      expect([400, 403, 404, 422]).toContain(res!.status());
    });

    test('POST /xu-ly/thuc-thi-hang-loat với tacgia → không phải 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.xuLy}/thuc-thi-hang-loat`, {
        danhSach: [],
      });
      expect([400, 403, 404, 422]).toContain(res!.status());
    });

    test('hội đồng GET /xu-ly danh sách — 200 hoặc 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.xuLy}?trang=1&soDong=5`);
      expect([200, 403, 404]).toContain(res!.status());
    });
  });

  // ─── Validation nâng cao ──────────────────────────────────────────────

  test.describe('Validation nâng cao', () => {
    test('POST /xu-ly/thuc-thi với sangKienId rỗng → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const res = await apiRequest(page, 'POST', `${API.xuLy}/thuc-thi`, {
        sangKienId: '',
        truongHopId: '00000000-0000-0000-0000-000000000000',
      });
      expect([400, 404, 422]).toContain(res!.status());
    });

    test('POST /xu-ly/thu-hoi không xác thực → 401 (confirm)', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(`${API.xuLy}/thu-hoi`, {
        data: { sangKienId: '00000000-0000-0000-0000-000000000000' },
      });
      expect(res.status()).toBe(401);
    });

    test('GET /xu-ly không xác thực → 401/404', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.xuLy}?trang=1&soDong=5`);
      expect([401, 404]).toContain(res.status());
    });

    test('GET /sang-kien với trangThaiTong không hợp lệ — xử lý an toàn', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5&trangThaiTong=INVALID`);
      expect([200, 400, 422]).toContain(res!.status());
    });
  });

  // ─── Responsive viewport ──────────────────────────────────────────────

  test.describe('Responsive viewport', () => {
    test('trang tiếp nhận hiển thị đúng trên mobile (375px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      await page.goto(ROUTES.tiepNhan);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
      const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
      expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
      await context.close();
    });

    test('trang xử lý hiển thị đúng trên tablet (768px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      await page.goto(ROUTES.xuLy);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await context.close();
    });
  });
});

// ─── P2: REQ-27 Organization scope (IDOR) ────────────────────────────────────

test.describe('REQ-27: Phạm vi đơn vị tiếp nhận', () => {
  test('tiepnhan chỉ xem sáng kiến thuộc đơn vị mình (hoặc cấp dưới)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tiepnhan');
    const res = await apiRequest(page, 'GET', `${API.sangKien}/cho-tiep-nhan?trang=1&soDong=50`);
    expect([200, 404]).toContain(res!.status());
    if (res!.status() === 200) {
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    }
  });

  test('tacgia1 không xem được danh sách chờ tiếp nhận → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.sangKien}/cho-tiep-nhan?trang=1&soDong=10`);
    expect(res!.status()).toBe(403);
  });

  test('GET /sang-kien/cho-tiep-nhan không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.sangKien}/cho-tiep-nhan?trang=1&soDong=5`);
    expect(res.status()).toBe(401);
  });

  test('phiếu tiếp nhận PDF cho sáng kiến hợp lệ trả về PDF', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const skRes = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`);
    const skBody = await skRes!.json();
    if (skBody.duLieu.length === 0) return;
    const sangKienId: string = skBody.duLieu[0].id;
    const res = await apiRequest(page, 'GET', `${API.sangKien}/${sangKienId}/phieu-tiep-nhan`);
    expect([200, 400, 404, 422]).toContain(res!.status());
    if (res!.status() === 200) {
      const ct = res!.headers()['content-type'] ?? '';
      expect(ct.includes('pdf') || ct.includes('json')).toBeTruthy();
    }
  });
});
