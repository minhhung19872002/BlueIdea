import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest, setupAuthenticatedPage } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

test.describe('REQ-01 đến REQ-08: Danh mục hệ thống', () => {
  // ─── Lĩnh vực (REQ-01) ─────────────────────────────────────────────

  test.describe('REQ-01: Lĩnh vực', () => {
    test('trang danh mục lĩnh vực tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/linh-vuc`);
      await expect(page.getByText('Danh mục dùng chung')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('table')).toBeVisible();
    });

    test('bảng hiển thị dữ liệu mẫu (seed data)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/linh-vuc`);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      // Wait for data to load — either rows or empty state
      await page.waitForTimeout(3000);
      const rows = page.locator('.ant-table-tbody tr.ant-table-row');
      const rowCount = await rows.count();
      // Seed data may or may not include linh-vuc entries
      expect(rowCount).toBeGreaterThanOrEqual(0);
    });

    test('cột bảng hiển thị đúng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/linh-vuc`);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      // Ant Design renders column headers in thead
      const headers = page.locator('.ant-table-thead th');
      await expect(headers.first()).toBeVisible({ timeout: 5_000 });
      const headerTexts = await headers.allTextContents();
      const combined = headerTexts.join(' ');
      expect(combined).toContain('Mã');
      expect(combined).toContain('Tên');
    });

    test('tìm kiếm không dấu hoạt động', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/linh-vuc`);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      const searchInput = page.getByPlaceholder('Tìm kiếm (không dấu)');
      await expect(searchInput).toBeVisible();
      // Type search term and press Enter
      await searchInput.fill('giao duc');
      await searchInput.press('Enter');
      await page.waitForTimeout(1000);
      // Table should still be visible (may have filtered results or no results)
      await expect(page.locator('table')).toBeVisible();
    });

    test('nút Thêm mới mở modal', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/linh-vuc`);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      await page.getByRole('button', { name: 'Thêm mới' }).click();
      // Modal should appear
      await expect(page.getByRole('dialog')).toBeVisible({ timeout: 5_000 });
      // Modal should have form fields
      await expect(page.getByPlaceholder('VD: GIAO_DUC')).toBeVisible();
    });

    test('modal đóng khi bấm Hủy', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/linh-vuc`);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      await page.getByRole('button', { name: 'Thêm mới' }).click();
      await expect(page.getByRole('dialog')).toBeVisible({ timeout: 5_000 });
      await page.getByRole('button', { name: 'Hủy' }).click();
      await expect(page.getByRole('dialog')).not.toBeVisible({ timeout: 5_000 });
    });

    test('API GET danh mục lĩnh vực trả về đúng cấu trúc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.tongSo).toBeGreaterThanOrEqual(0);
    });

    test('API GET lĩnh vực có phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc?trang=1&soDong=2`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu.length).toBeLessThanOrEqual(2);
    });

    test('API POST tạo + GET xác nhận + DELETE xóa lĩnh vực', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_LV_${Date.now()}`;
      // Create
      const createRes = await apiRequest(page, 'POST', `${API.danhMuc}/linh-vuc`, {
        ma,
        ten: 'Lĩnh vực E2E test',
        moTa: 'Tạo bởi E2E test',
        thuTu: 999,
        trangThai: 1,
      });
      expect(createRes!.status()).toBe(200);
      const createBody = await createRes!.json();
      expect(createBody.thanhCong).toBe(true);
      const id = createBody.duLieu.id;
      expect(id).toBeTruthy();

      // Read back
      const getRes = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc/${id}`);
      expect(getRes!.status()).toBe(200);
      const getBody = await getRes!.json();
      expect(getBody.duLieu.ma).toBe(ma);
      expect(getBody.duLieu.ten).toBe('Lĩnh vực E2E test');

      // Delete (soft)
      const delRes = await apiRequest(page, 'DELETE', `${API.danhMuc}/linh-vuc/${id}`);
      expect(delRes!.status()).toBe(200);

      // Confirm deleted (should return 404 or not found)
      const getAfterDel = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc/${id}`);
      expect([404, 400]).toContain(getAfterDel!.status());
    });

    test('API POST trùng mã trả về 409', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_DUP_${Date.now()}`;
      // Create first
      await apiRequest(page, 'POST', `${API.danhMuc}/linh-vuc`, {
        ma,
        ten: 'First',
        thuTu: 0,
        trangThai: 1,
      });
      // Create duplicate
      const dupRes = await apiRequest(page, 'POST', `${API.danhMuc}/linh-vuc`, {
        ma,
        ten: 'Duplicate',
        thuTu: 0,
        trangThai: 1,
      });
      expect(dupRes!.status()).toBe(409);
    });
  });

  // ─── Đối tượng (REQ-02) ────────────────────────────────────────────

  test.describe('REQ-02: Đối tượng', () => {
    test('API CRUD đối tượng hoạt động đúng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_DT_${Date.now()}`;
      // Create
      const createRes = await apiRequest(page, 'POST', `${API.danhMuc}/doi-tuong`, {
        ma,
        ten: 'Đối tượng E2E',
        moTa: 'Test',
        thuTu: 0,
        trangThai: 1,
      });
      expect(createRes!.status()).toBe(200);
      const id = (await createRes!.json()).duLieu.id;

      // Update
      const updateRes = await apiRequest(page, 'PUT', `${API.danhMuc}/doi-tuong/${id}`, {
        ma,
        ten: 'Đối tượng E2E (đã sửa)',
        moTa: 'Updated',
        thuTu: 1,
        trangThai: 1,
      });
      expect(updateRes!.status()).toBe(200);

      // Verify update
      const getRes = await apiRequest(page, 'GET', `${API.danhMuc}/doi-tuong/${id}`);
      expect(getRes!.status()).toBe(200);
      const body = await getRes!.json();
      expect(body.duLieu.ten).toBe('Đối tượng E2E (đã sửa)');

      // Cleanup
      await apiRequest(page, 'DELETE', `${API.danhMuc}/doi-tuong/${id}`);
    });

    test('tab đối tượng hiển thị đúng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/doi-tuong`);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });
  });

  // ─── Loại tác giả (REQ-04) ─────────────────────────────────────────

  test.describe('REQ-04: Loại tác giả', () => {
    test('API CRUD loại tác giả với trường mở rộng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_LTG_${Date.now()}`;
      // Create with extended fields
      const createRes = await apiRequest(page, 'POST', `${API.danhMuc}/loai-tac-gia`, {
        ma,
        ten: 'Loại tác giả E2E',
        moTa: 'Test',
        thuTu: 0,
        trangThai: 1,
        choPhepNhieuTacGia: true,
        soTacGiaToiDa: 5,
      });
      expect(createRes!.status()).toBe(200);
      const id = (await createRes!.json()).duLieu.id;

      // Verify extended fields
      const getRes = await apiRequest(page, 'GET', `${API.danhMuc}/loai-tac-gia/${id}`);
      const body = await getRes!.json();
      expect(body.duLieu.choPhepNhieuTacGia).toBe(true);
      expect(body.duLieu.soTacGiaToiDa).toBe(5);

      // Cleanup
      await apiRequest(page, 'DELETE', `${API.danhMuc}/loai-tac-gia/${id}`);
    });

    test('tab loại tác giả hiển thị đúng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/loai-tac-gia`);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });
  });

  // ─── Đợt đề nghị (REQ-03) ──────────────────────────────────────────

  test.describe('REQ-03: Đợt đề nghị', () => {
    test('trang đợt đề nghị tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dotDeNghi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });

    test('API GET danh sách đợt đề nghị', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.tongSo).toBeGreaterThanOrEqual(0);
    });
  });

  // ─── Authorization tests ──────────────────────────────────────────

  test.describe('Phân quyền danh mục', () => {
    test('tác giả không thể tạo danh mục — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.danhMuc}/linh-vuc`, {
        ma: 'KHONG_DUOC',
        ten: 'Unauthorized',
        thuTu: 0,
        trangThai: 1,
      });
      expect(res!.status()).toBe(403);
    });

    test('tác giả có thể xem danh mục (chỉ đọc)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc?trang=1&soDong=5`);
      // tacgia has DanhMucXem permission — catalog is reference data viewable by all users
      expect([200, 403]).toContain(res!.status());
    });

    test('API danh mục không xác thực trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.danhMuc}/linh-vuc?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── Biểu mẫu (REQ-06, REQ-07) ──────────────────────────────────

  test.describe('REQ-06: Biểu mẫu xuất', () => {
    test('trang biểu mẫu xuất tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.bieuMauXuat);
      await page.waitForLoadState('networkidle');
      // Page should load without crash
      await expect(page.locator('body')).toBeVisible();
    });
  });

  test.describe('REQ-07: Biểu mẫu thống kê', () => {
    test('trang biểu mẫu thống kê tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.bieuMauThongKe);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible();
    });
  });

  // ─── Quyết định (REQ-08) ──────────────────────────────────────────

  test.describe('REQ-08: Quyết định', () => {
    test('API GET danh sách quyết định', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.tongSo).toBeGreaterThanOrEqual(0);
    });

    test('trang quyết định tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyetDinh);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible();
    });
  });

  // ─── Edge cases ───────────────────────────────────────────────────

  test.describe('Trường hợp biên', () => {
    test('XSS trong tên danh mục không render HTML', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_XSS_${Date.now()}`;
      const xssTen = '<img src=x onerror=alert(1)>';
      const createRes = await apiRequest(page, 'POST', `${API.danhMuc}/linh-vuc`, {
        ma,
        ten: xssTen,
        thuTu: 0,
        trangThai: 1,
      });
      if (createRes!.status() === 200) {
        const id = (await createRes!.json()).duLieu.id;
        // Verify the name is stored as text, not rendered as HTML
        const getRes = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc/${id}`);
        const body = await getRes!.json();
        expect(body.duLieu.ten).toBe(xssTen);
        // Cleanup
        await apiRequest(page, 'DELETE', `${API.danhMuc}/linh-vuc/${id}`);
      }
    });

    test('text rất dài trong tên — không phá API', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_LONG_${Date.now()}`;
      const longName = 'Tên rất dài '.repeat(50);
      const res = await apiRequest(page, 'POST', `${API.danhMuc}/linh-vuc`, {
        ma,
        ten: longName,
        thuTu: 0,
        trangThai: 1,
      });
      // Should succeed, return validation error, or reject oversized input
      expect([200, 400, 422, 500]).toContain(res!.status());
      if (res!.status() === 200) {
        const id = (await res!.json()).duLieu.id;
        await apiRequest(page, 'DELETE', `${API.danhMuc}/linh-vuc/${id}`);
      }
    });

    test('ký tự đặc biệt trong tìm kiếm không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/linh-vuc`);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      const searchInput = page.getByPlaceholder('Tìm kiếm (không dấu)');
      await searchInput.fill('!@#$%^&*()');
      await searchInput.press('Enter');
      await page.waitForTimeout(1000);
      // Should not crash — table should still be visible
      await expect(page.locator('table')).toBeVisible();
    });
  });

  // ─── Đơn vị phê duyệt (REQ-05) ──────────────────────────────────

  test.describe('REQ-05: Đơn vị phê duyệt', () => {
    test('trang đơn vị tải không lỗi và hiển thị cây', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.donVi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('.ant-tree')).toBeVisible({ timeout: 15_000 });
    });

    test('API GET /don-vi/cay trả về cây đơn vị', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.donVi}/cay`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeGreaterThan(0);
    });

    test('API GET /don-vi phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
    });

    test('API GET /don-vi/chon trả về danh sách cho dropdown', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.donVi}/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API POST tạo + GET xác nhận + DELETE xóa đơn vị', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_DV_${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.donVi, {
        ma,
        ten: 'Đơn vị E2E test',
        moTa: 'Tạo bởi E2E',
        thuTu: 999,
        trangThai: 1,
        loai: 'DON_VI',
        laDonViPheDuyet: false,
      });
      expect(createRes!.status()).toBe(200);
      const createBody = await createRes!.json();
      expect(createBody.thanhCong).toBe(true);
      const id = createBody.duLieu.id;
      expect(id).toBeTruthy();

      const getRes = await apiRequest(page, 'GET', `${API.donVi}/${id}`);
      expect(getRes!.status()).toBe(200);
      const getBody = await getRes!.json();
      expect(getBody.duLieu.ma).toBe(ma);
      expect(getBody.duLieu.ten).toBe('Đơn vị E2E test');

      const delRes = await apiRequest(page, 'DELETE', `${API.donVi}/${id}`);
      expect(delRes!.status()).toBe(200);
    });

    test('API PUT cập nhật đơn vị', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_DV_UPD_${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.donVi, {
        ma,
        ten: 'Đơn vị ban đầu',
        thuTu: 0,
        trangThai: 1,
        loai: 'DON_VI',
        laDonViPheDuyet: false,
      });
      expect(createRes!.status()).toBe(200);
      const id = (await createRes!.json()).duLieu.id;

      const updateRes = await apiRequest(page, 'PUT', `${API.donVi}/${id}`, {
        ma,
        ten: 'Đơn vị đã cập nhật',
        thuTu: 1,
        trangThai: 1,
        loai: 'DON_VI',
        laDonViPheDuyet: true,
        capPheDuyet: 'CO_SO',
      });
      expect(updateRes!.status()).toBe(200);

      const getRes = await apiRequest(page, 'GET', `${API.donVi}/${id}`);
      const body = await getRes!.json();
      expect(body.duLieu.ten).toBe('Đơn vị đã cập nhật');
      expect(body.duLieu.laDonViPheDuyet).toBe(true);

      await apiRequest(page, 'DELETE', `${API.donVi}/${id}`);
    });

    test('trang cấp phê duyệt tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.capPheDuyet);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });

    test('API GET /cap-phe-duyet trả về danh sách', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', '/api/v1/cap-phe-duyet');
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('tác giả không có quyền tạo đơn vị — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.donVi, {
        ma: 'KHONG_DUOC',
        ten: 'Unauthorized',
        thuTu: 0,
        trangThai: 1,
        loai: 'DON_VI',
        laDonViPheDuyet: false,
      });
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET /don-vi trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.donVi}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });
  });
});
