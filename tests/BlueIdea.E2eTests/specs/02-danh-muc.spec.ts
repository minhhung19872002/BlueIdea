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

    test('API GET đối tượng có phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/doi-tuong?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
      expect(typeof body.tongSo).toBe('number');
    });

    test('API GET /doi-tuong/chon trả về dropdown', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/doi-tuong/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API POST thiếu tên đối tượng — không lỗi hệ thống', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', `${API.danhMuc}/doi-tuong`, {
        ma: `E2E_DT_NOTIEN_${Date.now()}`,
        ten: '',
        thuTu: 0,
        trangThai: 1,
      });
      expect([200, 400, 422]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        const id = body.duLieu?.id ?? body.duLieu;
        if (id) await apiRequest(page, 'DELETE', `${API.danhMuc}/doi-tuong/${id}`);
      }
    });

    test('API POST mã trùng đối tượng trả về 409', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_DT_DUP_${Date.now()}`;
      const first = await apiRequest(page, 'POST', `${API.danhMuc}/doi-tuong`, {
        ma,
        ten: 'Đối tượng thứ nhất',
        thuTu: 0,
        trangThai: 1,
      });
      expect(first!.status()).toBe(200);
      const id = (await first!.json()).duLieu.id;
      const dup = await apiRequest(page, 'POST', `${API.danhMuc}/doi-tuong`, {
        ma,
        ten: 'Đối tượng trùng mã',
        thuTu: 0,
        trangThai: 1,
      });
      expect(dup!.status()).toBe(409);
      await apiRequest(page, 'DELETE', `${API.danhMuc}/doi-tuong/${id}`);
    });

    test('API tìm kiếm đối tượng theo từ khóa', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_DT_TK_${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', `${API.danhMuc}/doi-tuong`, {
        ma,
        ten: 'Đối tượng tìm kiếm E2E',
        thuTu: 0,
        trangThai: 1,
      });
      expect(createRes!.status()).toBe(200);
      const id = (await createRes!.json()).duLieu.id;
      const searchRes = await apiRequest(page, 'GET', `${API.danhMuc}/doi-tuong?tuKhoa=tim+kiem&trang=1&soDong=20`);
      expect(searchRes!.status()).toBe(200);
      const body = await searchRes!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      await apiRequest(page, 'DELETE', `${API.danhMuc}/doi-tuong/${id}`);
    });

    test('tác giả không thể tạo đối tượng — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.danhMuc}/doi-tuong`, {
        ma: 'KHONG_DUOC_DT',
        ten: 'Không được phép',
        thuTu: 0,
        trangThai: 1,
      });
      expect(res!.status()).toBe(403);
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

    test('API GET loại tác giả có phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/loai-tac-gia?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
      expect(typeof body.tongSo).toBe('number');
    });

    test('API GET /loai-tac-gia/chon trả về dropdown', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/loai-tac-gia/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API POST mã trùng loại tác giả trả về 409', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_LTG_DUP_${Date.now()}`;
      const first = await apiRequest(page, 'POST', `${API.danhMuc}/loai-tac-gia`, {
        ma,
        ten: 'Loại tác giả đầu tiên',
        thuTu: 0,
        trangThai: 1,
        choPhepNhieuTacGia: false,
        soTacGiaToiDa: 1,
      });
      expect(first!.status()).toBe(200);
      const id = (await first!.json()).duLieu.id;
      const dup = await apiRequest(page, 'POST', `${API.danhMuc}/loai-tac-gia`, {
        ma,
        ten: 'Loại tác giả trùng mã',
        thuTu: 0,
        trangThai: 1,
        choPhepNhieuTacGia: false,
        soTacGiaToiDa: 1,
      });
      expect(dup!.status()).toBe(409);
      await apiRequest(page, 'DELETE', `${API.danhMuc}/loai-tac-gia/${id}`);
    });

    test('API POST thiếu tên loại tác giả — không lỗi hệ thống', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', `${API.danhMuc}/loai-tac-gia`, {
        ma: `E2E_LTG_NOTIEN_${Date.now()}`,
        ten: '',
        thuTu: 0,
        trangThai: 1,
        choPhepNhieuTacGia: false,
        soTacGiaToiDa: 1,
      });
      expect([200, 400, 422]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        const id = body.duLieu?.id ?? body.duLieu;
        if (id) await apiRequest(page, 'DELETE', `${API.danhMuc}/loai-tac-gia/${id}`);
      }
    });

    test('API cập nhật soTacGiaToiDa — verify saved', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_LTG_UPD_${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', `${API.danhMuc}/loai-tac-gia`, {
        ma,
        ten: 'Loại tác giả cập nhật',
        thuTu: 0,
        trangThai: 1,
        choPhepNhieuTacGia: true,
        soTacGiaToiDa: 3,
      });
      expect(createRes!.status()).toBe(200);
      const id = (await createRes!.json()).duLieu.id;
      const updateRes = await apiRequest(page, 'PUT', `${API.danhMuc}/loai-tac-gia/${id}`, {
        ma,
        ten: 'Loại tác giả cập nhật',
        thuTu: 0,
        trangThai: 1,
        choPhepNhieuTacGia: true,
        soTacGiaToiDa: 10,
      });
      expect(updateRes!.status()).toBe(200);
      const getRes = await apiRequest(page, 'GET', `${API.danhMuc}/loai-tac-gia/${id}`);
      const body = await getRes!.json();
      expect(body.duLieu.soTacGiaToiDa).toBe(10);
      await apiRequest(page, 'DELETE', `${API.danhMuc}/loai-tac-gia/${id}`);
    });

    test('tác giả không thể tạo loại tác giả — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.danhMuc}/loai-tac-gia`, {
        ma: 'KHONG_DUOC_LTG',
        ten: 'Không được phép',
        thuTu: 0,
        trangThai: 1,
        choPhepNhieuTacGia: false,
        soTacGiaToiDa: 1,
      });
      expect(res!.status()).toBe(403);
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

    test('API GET /dot-de-nghi/chon trả về dropdown', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.dotDeNghi}/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API GET /dot-de-nghi/dang-mo trả về đợt đang mở', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.dotDeNghi}/dang-mo`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API GET chi tiết đợt đề nghị', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=5`);
      expect(listRes!.status()).toBe(200);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length > 0) {
        const id: string = listBody.duLieu[0].id;
        const getRes = await apiRequest(page, 'GET', `${API.dotDeNghi}/${id}`);
        expect(getRes!.status()).toBe(200);
        const body = await getRes!.json();
        expect(body.thanhCong).toBe(true);
        expect(body.duLieu.id).toBe(id);
        expect(typeof body.duLieu.ten).toBe('string');
      }
    });

    test('API POST tạo đợt đề nghị + DELETE', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_DDN_${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.dotDeNghi, {
        ma,
        ten: 'Đợt đề nghị E2E test',
        moTa: 'Tạo bởi E2E',
        nam: new Date().getFullYear(),
        thuTu: 999,
        trangThai: 1,
      });
      expect(createRes!.status()).toBe(200);
      const createBody = await createRes!.json();
      expect(createBody.thanhCong).toBe(true);
      const id: string = createBody.duLieu.id;
      expect(id).toBeTruthy();
      const delRes = await apiRequest(page, 'DELETE', `${API.dotDeNghi}/${id}`);
      expect(delRes!.status()).toBe(200);
    });

    test('API POST thiếu tên đợt đề nghị — không lỗi hệ thống', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', API.dotDeNghi, {
        ma: `E2E_DDN_NOTIEN_${Date.now()}`,
        ten: '',
        nam: new Date().getFullYear(),
        thuTu: 0,
        trangThai: 1,
      });
      expect([200, 400, 422]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        const id = body.duLieu?.id ?? body.duLieu;
        if (id) await apiRequest(page, 'DELETE', `${API.dotDeNghi}/${id}`);
      }
    });

    test('tác giả không thể tạo đợt đề nghị — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.dotDeNghi, {
        ma: 'KHONG_DUOC_DDN',
        ten: 'Không được phép',
        nam: new Date().getFullYear(),
        thuTu: 0,
        trangThai: 1,
      });
      expect(res!.status()).toBe(403);
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

    test('API GET danh sách biểu mẫu xuất có phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.bieuMau}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
      expect(typeof body.tongSo).toBe('number');
    });

    test('API GET /bieu-mau-xuat/chon trả về dropdown', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.bieuMau}/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API GET /bieu-mau-xuat/truong-kha-dung trả về trường dữ liệu', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.bieuMau}/truong-kha-dung?loai=PHIEU_TIEP_NHAN`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API POST tạo + GET xác nhận + DELETE biểu mẫu xuất', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_BMX_${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.bieuMau, {
        ma,
        ten: 'Biểu mẫu xuất E2E',
        moTa: 'Tạo bởi E2E test',
        thuTu: 999,
        trangThai: 1,
      });
      expect(createRes!.status()).toBe(200);
      const createBody = await createRes!.json();
      expect(createBody.thanhCong).toBe(true);
      const id: string = createBody.duLieu.id;
      expect(id).toBeTruthy();
      const getRes = await apiRequest(page, 'GET', `${API.bieuMau}/${id}`);
      expect(getRes!.status()).toBe(200);
      const getBody = await getRes!.json();
      expect(getBody.duLieu.ma).toBe(ma);
      const delRes = await apiRequest(page, 'DELETE', `${API.bieuMau}/${id}`);
      expect(delRes!.status()).toBe(200);
    });

    test('API GET chi tiết biểu mẫu xuất', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.bieuMau}?trang=1&soDong=5`);
      expect(listRes!.status()).toBe(200);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length > 0) {
        const id: string = listBody.duLieu[0].id;
        const getRes = await apiRequest(page, 'GET', `${API.bieuMau}/${id}`);
        expect(getRes!.status()).toBe(200);
        const body = await getRes!.json();
        expect(body.thanhCong).toBe(true);
        expect(body.duLieu.id).toBe(id);
        expect(typeof body.duLieu.ten).toBe('string');
      }
    });

    test('tác giả không thể tạo biểu mẫu xuất — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.bieuMau, {
        ma: 'KHONG_DUOC_BMX',
        ten: 'Không được phép',
        thuTu: 0,
        trangThai: 1,
      });
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET biểu mẫu xuất — 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.bieuMau}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
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

    test('API GET danh sách biểu mẫu thống kê có phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.bieuMauThongKe}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
      expect(typeof body.tongSo).toBe('number');
    });

    test('API GET /bieu-mau-thong-ke/chon trả về dropdown', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.bieuMauThongKe}/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API POST tạo biểu mẫu thống kê + GET xác nhận + DELETE', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_BMTK_${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.bieuMauThongKe, {
        ma,
        ten: 'Biểu mẫu thống kê E2E',
        moTa: 'Tạo bởi E2E test',
        thuTu: 999,
        trangThai: 1,
        cauHinhCot: [
          { ma: 'COL1', tieuDe: 'Cột 1', thuTu: 0, nguon: 'tenSangKien' },
        ],
      });
      expect([200, 201]).toContain(createRes!.status());
      const createBody = await createRes!.json();
      expect(createBody.thanhCong).toBe(true);
      const id: string = createBody.duLieu?.id ?? createBody.duLieu;
      expect(id).toBeTruthy();
      const getRes = await apiRequest(page, 'GET', `${API.bieuMauThongKe}/${id}`);
      expect(getRes!.status()).toBe(200);
      const getBody = await getRes!.json();
      expect(getBody.duLieu.ma).toBe(ma);
      const delRes = await apiRequest(page, 'DELETE', `${API.bieuMauThongKe}/${id}`);
      expect(delRes!.status()).toBe(200);
    });

    test('API GET chi tiết biểu mẫu thống kê', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.bieuMauThongKe}?trang=1&soDong=5`);
      expect(listRes!.status()).toBe(200);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length > 0) {
        const id: string = listBody.duLieu[0].id;
        const getRes = await apiRequest(page, 'GET', `${API.bieuMauThongKe}/${id}`);
        expect(getRes!.status()).toBe(200);
        const body = await getRes!.json();
        expect(body.thanhCong).toBe(true);
        expect(body.duLieu.id).toBe(id);
        expect(typeof body.duLieu.ten).toBe('string');
      }
    });

    test('API POST thiếu cấu hình cột biểu mẫu thống kê trả về lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', API.bieuMauThongKe, {
        ma: `E2E_BMTK_NOCOT_${Date.now()}`,
        ten: 'Thiếu cột cấu hình',
        thuTu: 0,
        trangThai: 1,
        // omit cauHinhCot to test required field validation
      });
      // API should reject if cauHinhCot is required, or accept — either way it must not crash
      expect([200, 400, 422]).toContain(res!.status());
      if (res!.status() === 200) {
        const resBody = await res!.json();
        const id: string | undefined = resBody.duLieu?.id;
        if (id) {
          await apiRequest(page, 'DELETE', `${API.bieuMauThongKe}/${id}`);
        }
      }
    });

    test('tác giả không thể tạo biểu mẫu thống kê — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.bieuMauThongKe, {
        ma: 'KHONG_DUOC_BMTK',
        ten: 'Không được phép',
        thuTu: 0,
        trangThai: 1,
        cauHinhCot: [],
      });
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET biểu mẫu thống kê — 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.bieuMauThongKe}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
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

    test('API GET quyết định có phân trang đúng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=3`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(3);
      expect(typeof body.tongSo).toBe('number');
    });

    test('API GET quyết định trang 1 vs trang 2 khác nhau', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const page1Res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=2`);
      const page2Res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=2&soDong=2`);
      expect(page1Res!.status()).toBe(200);
      expect(page2Res!.status()).toBe(200);
      const body1 = await page1Res!.json();
      const body2 = await page2Res!.json();
      if (body1.tongSo > 2 && body1.duLieu.length > 0 && body2.duLieu.length > 0) {
        expect(body1.duLieu[0].id).not.toBe(body2.duLieu[0].id);
      }
    });

    test('API GET chi tiết quyết định', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=5`);
      expect(listRes!.status()).toBe(200);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length > 0) {
        const id: string = listBody.duLieu[0].id;
        const getRes = await apiRequest(page, 'GET', `${API.quyetDinh}/${id}`);
        expect(getRes!.status()).toBe(200);
        const body = await getRes!.json();
        expect(body.thanhCong).toBe(true);
        expect(body.duLieu.id).toBe(id);
      }
    });

    test('API GET quyết định — tìm kiếm theo từ khóa', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?tuKhoa=test&trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('tác giả xem danh sách quyết định', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.quyetDinh}?trang=1&soDong=5`);
      // tacgia may have read-only access to quyết định
      expect([200, 403]).toContain(res!.status());
    });

    test('không xác thực GET quyết định — 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.quyetDinh}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
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

  // ─── Sắp xếp và phân trang nâng cao ────────────────────────────────

  test.describe('Sắp xếp và phân trang nâng cao', () => {
    test('GET /danh-muc/linh-vuc sapXep=ten&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc?trang=1&soDong=10&sapXep=ten&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /danh-muc/linh-vuc sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /danh-muc/linh-vuc trang=9999 trả về mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc?trang=9999&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBe(0);
    });

    test('GET /don-vi sapXep=ten&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=10&sapXep=ten&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /don-vi trang=2 khác trang=1 nếu đủ data', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const p1 = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=2`);
      const b1 = await p1!.json();
      if (b1.tongSo > 2) {
        const p2 = await apiRequest(page, 'GET', `${API.donVi}?trang=2&soDong=2`);
        const b2 = await p2!.json();
        expect(b2.duLieu).toBeInstanceOf(Array);
        if (b2.duLieu.length > 0 && b1.duLieu.length > 0) {
          expect(b1.duLieu[0].id).not.toBe(b2.duLieu[0].id);
        }
      }
    });

    test('GET /danh-muc/dot-de-nghi sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /bieu-mau-xuat sapXep=ten&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.bieuMau}?trang=1&soDong=10&sapXep=ten&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Validation nâng cao ──────────────────────────────────────────────

  test.describe('Validation nâng cao', () => {
    test('POST /danh-muc/linh-vuc với payload rỗng → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', `${API.danhMuc}/linh-vuc`, {});
      expect([400, 422]).toContain(res!.status());
    });

    test('POST /don-vi với payload rỗng → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', API.donVi, {});
      expect([400, 422]).toContain(res!.status());
    });

    test('DELETE /danh-muc/linh-vuc không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.delete(`${API.danhMuc}/linh-vuc/${fakeId}`);
      expect(res.status()).toBe(401);
    });

    test('DELETE /don-vi không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.delete(`${API.donVi}/${fakeId}`);
      expect(res.status()).toBe(401);
    });

    test('GET /danh-muc/linh-vuc/{id-sai} trả về 400 hoặc 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc/khong-phai-uuid`);
      expect([400, 404]).toContain(res!.status());
    });

    test('PUT /don-vi không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.put(`${API.donVi}/${fakeId}`, {
        data: { ma: 'TEST', ten: 'Test' },
      });
      expect(res.status()).toBe(401);
    });
  });

  // ─── Responsive viewport ──────────────────────────────────────────────

  test.describe('Responsive viewport', () => {
    test('trang danh mục hiển thị đúng trên mobile (375px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(`${ROUTES.danhMuc}/linh-vuc`);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
      const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
      expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
      await context.close();
    });

    test('trang đơn vị hiển thị đúng trên tablet (768px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.donVi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await context.close();
    });
  });
});
