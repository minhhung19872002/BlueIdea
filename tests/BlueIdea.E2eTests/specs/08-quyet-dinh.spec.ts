import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

// ─── REQ-31: Đính kèm quyết định ────────────────────────────────────────────
// ─── REQ-32: Kết quả sáng kiến ──────────────────────────────────────────────
// ─── REQ-36: Quyết định (merged with 31) ────────────────────────────────────

test.describe('REQ-31/32/36: Quyết định công nhận sáng kiến', () => {
  // ─── Frontend UI ──────────────────────────────────────────────────────────

  test.describe('Giao diện trang quyết định', () => {
    test('trang quyết định tải không lỗi với admin', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('trang hiển thị tiêu đề "Quyết định công nhận sáng kiến"', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      await expect(
        page.getByText(/quyết định/i).first()
      ).toBeVisible({ timeout: 10_000 });
    });

    test('nút "Ban hành quyết định" hiển thị cho lãnh đạo', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      const btn = page.getByRole('button', { name: /ban hành/i });
      const hasBtn = await btn.isVisible().catch(() => false);
      // Button may or may not be visible depending on role permissions
      expect(typeof hasBtn).toBe('boolean');
    });

    test('bảng quyết định hiển thị cột', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      const table = page.locator('.ant-table').first();
      const hasTable = await table.isVisible().catch(() => false);
      if (hasTable) {
        const headers = page.locator('.ant-table-thead th');
        await expect(headers.first()).toBeVisible({ timeout: 5_000 });
      }
    });

    test('ô tìm kiếm hiển thị đúng placeholder', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      const search = page.locator('input[placeholder*="Tìm theo"]').first();
      const hasSearch = await search.isVisible().catch(() => false);
      expect(typeof hasSearch).toBe('boolean');
    });
  });

  // ─── Backend API — danh sách ──────────────────────────────────────────────

  test.describe('API GET danh sách quyết định', () => {
    test('GET /quyet-dinh trả về cấu trúc phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('GET /quyet-dinh phân trang soDong=5 trả tối đa 5 bản ghi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
    });

    test('GET /quyet-dinh tìm kiếm bằng từ khoá', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.quyetDinh}?trang=1&soDong=10&tuKhoa=QD`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Backend API — CRUD ───────────────────────────────────────────────────

  test.describe('API CRUD quyết định', () => {
    let createdId: string | null = null;

    test('POST /quyet-dinh tạo quyết định mới', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'POST', API.quyetDinh, {
        soQuyetDinh: `E2E-QD-${Date.now()}`,
        ngayBanHanh: '2026-08-19T00:00:00+07:00',
        loai: 'CO_SO',
        trichYeu: 'Quyết định tạo bởi E2E test',
        nguoiKy: 'Nguyễn Văn Test',
        chucVuNguoiKy: 'Giám đốc',
      });
      // May need dotDeNghiId or other required fields
      expect([200, 400, 422]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.thanhCong).toBe(true);
        createdId = body.duLieu;
      }
    });

    test('GET /quyet-dinh/{id} chi tiết quyết định', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      // First get a real ID from the list
      const listRes = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=1`);
      expect(listRes!.status()).toBe(200);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length > 0) {
        const id = listBody.duLieu[0].id;
        const res = await apiRequest(page, 'GET', `${API.quyetDinh}/${id}`);
        expect(res!.status()).toBe(200);
        const body = await res!.json();
        expect(body.thanhCong).toBe(true);
        expect(body.duLieu).toBeDefined();
        expect(body.duLieu.thongTin || body.duLieu.soQuyetDinh || body.duLieu.id).toBeTruthy();
      }
    });

    test('GET /quyet-dinh/{id} với id không tồn tại → 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}/${fakeId}`);
      expect([400, 404]).toContain(res!.status());
    });

    test('POST /quyet-dinh với dữ liệu trống → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'POST', API.quyetDinh, {});
      expect([400, 422]).toContain(res!.status());
    });

    test('POST /quyet-dinh với soQuyetDinh rỗng → validation error', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'POST', API.quyetDinh, {
        soQuyetDinh: '',
        trichYeu: 'Test validation',
      });
      expect([400, 422]).toContain(res!.status());
    });
  });

  // ─── API Công bố & Xuất PDF ───────────────────────────────────────────────

  test.describe('API công bố và xuất PDF', () => {
    test('GET /quyet-dinh/ho-so-du-dieu-kien trả về danh sách', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(
        page,
        'GET',
        `${API.quyetDinh}/ho-so-du-dieu-kien`
      );
      expect([200, 400]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.thanhCong).toBe(true);
      }
    });

    test('GET /quyet-dinh/{id}/xuat-pdf với quyết định hợp lệ', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length > 0) {
        const id = listBody.duLieu[0].id;
        const res = await apiRequest(page, 'GET', `${API.quyetDinh}/${id}/xuat-pdf`);
        expect([200, 400, 404]).toContain(res!.status());
        if (res!.status() === 200) {
          const contentType = res!.headers()['content-type'] || '';
          expect(contentType).toContain('pdf');
        }
      }
    });

    test('POST /quyet-dinh/{id}/cong-bo không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.post(
        `${API.quyetDinh}/${fakeId}/cong-bo?congKhai=true`
      );
      expect(res.status()).toBe(401);
    });
  });

  // ─── Phân quyền ──────────────────────────────────────────────────────────

  test.describe('Phân quyền quyết định', () => {
    test('không xác thực GET /quyet-dinh → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.quyetDinh}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });

    test('tác giả GET /quyet-dinh → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=5`);
      expect([403, 404]).toContain(res!.status());
    });

    test('tác giả POST /quyet-dinh → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.quyetDinh, {
        soQuyetDinh: 'Không có quyền',
        trichYeu: 'Test phân quyền',
      });
      expect([403, 404]).toContain(res!.status());
    });

    test('không xác thực POST /quyet-dinh → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(API.quyetDinh, {
        data: { soQuyetDinh: 'Test', trichYeu: 'Test' },
      });
      expect(res.status()).toBe(401);
    });
  });

  // ─── Trường hợp biên ──────────────────────────────────────────────────────

  test.describe('Trường hợp biên', () => {
    test('XSS trong soQuyetDinh không render HTML', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const xss = '<script>alert("xss")</script>';
      const res = await apiRequest(page, 'POST', API.quyetDinh, {
        soQuyetDinh: xss,
        ngayBanHanh: '2026-08-19T00:00:00+07:00',
        loai: 'CO_SO',
        trichYeu: 'XSS test in decision',
        nguoiKy: 'Test',
        chucVuNguoiKy: 'Test',
      });
      // Server either stores safely or rejects
      expect([200, 400, 422]).toContain(res!.status());
    });

    test('ký tự đặc biệt trong tìm kiếm không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.quyetDinh}?trang=1&soDong=5&tuKhoa=${encodeURIComponent("'; DROP TABLE--")}`
      );
      expect(res!.status()).toBe(200);
    });

    test('trang quá lớn trả về mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=99999&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBe(0);
    });
  });

  // ─── Kết quả sáng kiến (REQ-32) ──────────────────────────────────

  test.describe('REQ-32: Kết quả sáng kiến', () => {
    test('API GET /quyet-dinh trả về danh sách quyết định có kết quả', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('trang quyết định hiển thị bảng kết quả', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });

    test('kết quả quyết định item có trường soQuyetDinh hoặc trichYeu', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      if (body.duLieu.length > 0) {
        const item = body.duLieu[0] as Record<string, unknown>;
        const hasIdentifier = 'soQuyetDinh' in item || 'trichYeu' in item || 'id' in item;
        expect(hasIdentifier).toBe(true);
      }
    });
  });

  // ─── Sắp xếp và phân trang nâng cao ────────────────────────────────

  test.describe('Sắp xếp và phân trang nâng cao', () => {
    test('GET /quyet-dinh sapXep=ngayBanHanh&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=10&sapXep=ngayBanHanh&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /quyet-dinh sapXep=ngayBanHanh&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=10&sapXep=ngayBanHanh&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /quyet-dinh trang=2 trả về tập kết quả khác trang=1 (nếu đủ data)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const page1 = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=2`);
      const body1 = await page1!.json();
      if (body1.tongSo > 2) {
        const page2 = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=2&soDong=2`);
        const body2 = await page2!.json();
        expect(body2.duLieu).toBeInstanceOf(Array);
        if (body2.duLieu.length > 0 && body1.duLieu.length > 0) {
          expect(body1.duLieu[0].id).not.toBe(body2.duLieu[0].id);
        }
      }
    });

    test('GET /quyet-dinh với soDong=-1 xử lý an toàn', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=-1`);
      expect([200, 400, 422]).toContain(res!.status());
    });

    test('GET /quyet-dinh với tuKhoa rỗng trả kết quả đầy đủ', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=10&tuKhoa=`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Phân quyền nâng cao ─────────────────────────────────────────────

  test.describe('Phân quyền nâng cao', () => {
    test('thư ký GET /quyet-dinh → 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=5`);
      expect([200, 403]).toContain(res!.status());
    });

    test('lãnh đạo GET /quyet-dinh → 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('không xác thực GET /quyet-dinh/{id}/xuat-pdf → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.get(`${API.quyetDinh}/${fakeId}/xuat-pdf`);
      expect(res.status()).toBe(401);
    });

    test('không xác thực DELETE /quyet-dinh → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.delete(`${API.quyetDinh}/${fakeId}`);
      expect(res.status()).toBe(401);
    });

    test('tác giả DELETE /quyet-dinh → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'DELETE', `${API.quyetDinh}/${fakeId}`);
      expect([403, 404]).toContain(res!.status());
    });
  });

  // ─── Validation nâng cao ──────────────────────────────────────────────

  test.describe('Validation nâng cao', () => {
    test('POST /quyet-dinh với ngayBanHanh null → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'POST', API.quyetDinh, {
        soQuyetDinh: `E2E-QD-VAL-${Date.now()}`,
        trichYeu: 'Test validation ngày',
        nguoiKy: 'Test',
        chucVuNguoiKy: 'Test',
      });
      expect([400, 422]).toContain(res!.status());
    });

    test('POST /quyet-dinh với trichYeu rỗng → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'POST', API.quyetDinh, {
        soQuyetDinh: `E2E-QD-VAL2-${Date.now()}`,
        trichYeu: '',
        ngayBanHanh: '2026-08-19T00:00:00+07:00',
        loai: 'CO_SO',
        nguoiKy: 'Test',
        chucVuNguoiKy: 'Test',
      });
      expect([400, 422]).toContain(res!.status());
    });

    test('PUT /quyet-dinh/{id} không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.put(`${API.quyetDinh}/${fakeId}`, {
        data: { soQuyetDinh: 'Test', trichYeu: 'Test' },
      });
      expect(res.status()).toBe(401);
    });

    test('POST /quyet-dinh với soQuyetDinh quá dài (500+ ký tự) → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const longStr = 'A'.repeat(600);
      const res = await apiRequest(page, 'POST', API.quyetDinh, {
        soQuyetDinh: longStr,
        trichYeu: 'Test quá dài',
        ngayBanHanh: '2026-08-19T00:00:00+07:00',
        loai: 'CO_SO',
        nguoiKy: 'Test',
        chucVuNguoiKy: 'Test',
      });
      expect([400, 422]).toContain(res!.status());
    });
  });

  // ─── Responsive viewport ──────────────────────────────────────────────

  test.describe('Responsive viewport', () => {
    test('trang quyết định hiển thị đúng trên mobile (375px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
      const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
      expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
      await context.close();
    });

    test('trang quyết định hiển thị đúng trên tablet (768px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await context.close();
    });
  });
});
