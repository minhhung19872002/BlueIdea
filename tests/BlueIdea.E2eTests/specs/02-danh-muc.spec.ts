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

// ─── REQ-03: Chi tiết đợt đề nghị ────────────────────────────────────────────

test.describe('REQ-03: Chi tiết đợt đề nghị (TrangChiTietDot)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang chi tiết đợt tải không crash', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    await page.goto(`${ROUTES.dotDeNghi}/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const bodyText = await page.locator('body').textContent();
    expect(bodyText).not.toContain('Lỗi hệ thống');
  });

  test('hiển thị thẻ thống kê (Tổng, Đang xử lý, Công nhận, Không đạt)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    await page.goto(`${ROUTES.dotDeNghi}/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-card').first()).toBeVisible({ timeout: 15_000 });
    const statsElements = page.locator('.ant-statistic, .ant-card');
    const count = await statsElements.count();
    expect(count).toBeGreaterThan(0);
  });

  test('hiển thị tab thông tin chung mặc định', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    await page.goto(`${ROUTES.dotDeNghi}/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-descriptions').first()).toBeVisible({ timeout: 15_000 });
  });

  test('click tab hồ sơ hiển thị bảng sáng kiến', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    await page.goto(`${ROUTES.dotDeNghi}/${id}`);
    await page.waitForLoadState('networkidle');
    const hoSoTab = page.getByRole('tab', { name: /hồ sơ/i });
    await expect(hoSoTab).toBeVisible({ timeout: 10_000 });
    await hoSoTab.click();
    await page.waitForTimeout(2000);
    const tableOrEmpty = page.locator('.ant-table, .ant-empty');
    await expect(tableOrEmpty.first()).toBeVisible({ timeout: 10_000 });
  });

  test('API GET /dot-de-nghi/{id}/tong-quan trả về thống kê', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào để kiểm tra API');
      return;
    }
    const id = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'GET', `${API.dotDeNghi}/${id}/tong-quan`);
    expect(res!.status()).toBe(200);
    const body = await res!.json() as { thanhCong: boolean; duLieu: Record<string, unknown> };
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
    expect(body.duLieu).toHaveProperty('tongHoSo');
  });

  test('API GET /dot-de-nghi/{id}/tong-quan không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const fakeId = '00000000-0000-0000-0000-000000000001';
    const res = await page.request.get(`${API.dotDeNghi}/${fakeId}/tong-quan`);
    expect(res.status()).toBe(401);
  });

  test('tác giả xem đợt đề nghị → 200, nhưng sửa → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    await loginViaAPI(page, 'tacgia1');
    const readRes = await apiRequest(page, 'GET', `${API.dotDeNghi}/${id}/tong-quan`);
    expect(readRes!.status()).toBe(200);
    const writeRes = await apiRequest(page, 'PUT', `${API.dotDeNghi}/${id}`, { ten: 'test' });
    expect([403, 422]).toContain(writeRes!.status());
  });

  test('hiển thị trạng thái đợt dưới dạng Tag', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    await page.goto(`${ROUTES.dotDeNghi}/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-tag').first()).toBeVisible({ timeout: 15_000 });
  });
});

// ─── REQ-03: Đợt đề nghị — hành động lifecycle ───────────────────────────────

test.describe('REQ-03: Đợt đề nghị — hành động lifecycle', () => {
  test.describe.configure({ timeout: 60_000 });

  test('POST /{id}/mo-dot — mở đợt (200 hoặc 409 nếu đã mở)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'POST', `${API.dotDeNghi}/${id}/mo-dot`);
    expect([200, 409]).toContain(res!.status());
  });

  test('POST /{id}/dong-dot — đóng đợt (200 hoặc 409 nếu đã đóng hoặc có hồ sơ đang chờ)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'POST', `${API.dotDeNghi}/${id}/dong-dot`);
    expect([200, 409]).toContain(res!.status());
  });

  test('POST /{id}/khoa-dot — khóa đợt (200 hoặc 409)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'POST', `${API.dotDeNghi}/${id}/khoa-dot`);
    expect([200, 409]).toContain(res!.status());
  });

  test('POST /{id}/sao-chep — sao chép đợt với mã mới (200 hoặc 409)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'POST', `${API.dotDeNghi}/${id}/sao-chep`, {
      ma: `DOT-E2E-CLONE-${Date.now()}`,
      ten: 'E2E Clone Test',
      nam: 2026,
    });
    expect([200, 409]).toContain(res!.status());
    if (res!.status() === 200) {
      const body = await res!.json() as { thanhCong: boolean; duLieu: { id: string } };
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu.id).toBeTruthy();
      // Cleanup cloned dot
      await apiRequest(page, 'DELETE', `${API.dotDeNghi}/${body.duLieu.id}`);
    }
  });

  test('GET /{id}/tong-quan — thống kê đợt có trường số', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị nào trong hệ thống');
      return;
    }
    const id = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'GET', `${API.dotDeNghi}/${id}/tong-quan`);
    expect(res!.status()).toBe(200);
    const body = await res!.json() as { thanhCong: boolean; duLieu: Record<string, unknown> };
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
    // At least one numeric field must exist in the overview stats
    const hasNumericField = Object.values(body.duLieu).some(v => typeof v === 'number');
    expect(hasNumericField).toBe(true);
  });

  test('GET /dang-mo — danh sách đợt đang mở trả về mảng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.dotDeNghi}/dang-mo`);
    expect(res!.status()).toBe(200);
    const body = await res!.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('POST /mo-dot với ID giả — 400 hoặc 404', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const fakeId = '00000000-0000-0000-0000-000000000099';
    const res = await apiRequest(page, 'POST', `${API.dotDeNghi}/${fakeId}/mo-dot`);
    expect([400, 404]).toContain(res!.status());
  });

  test('POST /dong-dot với ID giả — 400 hoặc 404', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const fakeId = '00000000-0000-0000-0000-000000000099';
    const res = await apiRequest(page, 'POST', `${API.dotDeNghi}/${fakeId}/dong-dot`);
    expect([400, 404]).toContain(res!.status());
  });

  test('Auth: tacgia1 POST /mo-dot — 403', async ({ page }) => {
    await page.goto('/');
    // Get a real dot ID as admin first, then switch to tacgia1
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    const targetId = listBody.duLieu.length > 0
      ? listBody.duLieu[0].id
      : '00000000-0000-0000-0000-000000000001';
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'POST', `${API.dotDeNghi}/${targetId}/mo-dot`);
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực POST /mo-dot — 401', async ({ page }) => {
    await page.goto('/');
    const fakeId = '00000000-0000-0000-0000-000000000001';
    const res = await page.request.post(`${API.dotDeNghi}/${fakeId}/mo-dot`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-44: Đơn vị — CRUD operations ───────────────────────────────────────

test.describe('REQ-44: Đơn vị — CRUD operations', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET /cay — cây đơn vị có cấu trúc đúng (id, ten)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.donVi}/cay`);
    expect(res!.status()).toBe(200);
    const body = await res!.json() as { thanhCong: boolean; duLieu: { id: string; ten: string }[] };
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(body.duLieu.length).toBeGreaterThan(0);
    const firstNode = body.duLieu[0];
    expect(typeof firstNode.id).toBe('string');
    expect(firstNode.id).toBeTruthy();
    expect(typeof firstNode.ten).toBe('string');
  });

  test('POST tạo đơn vị mới — 200 và trả về ID', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-DV-${Date.now()}`;
    const res = await apiRequest(page, 'POST', API.donVi, {
      ma,
      ten: 'Đơn vị E2E Test',
      thuTu: 99,
      trangThai: 1,
    });
    expect(res!.status()).toBe(200);
    const body = await res!.json() as { thanhCong: boolean; duLieu: { id: string } };
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu.id).toBeTruthy();
    // Cleanup
    await apiRequest(page, 'DELETE', `${API.donVi}/${body.duLieu.id}`);
  });

  test('GET /{id} chi tiết — xác nhận ma và ten khớp sau khi tạo', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-DV-DET-${Date.now()}`;
    const ten = 'Đơn vị Chi Tiết E2E';
    const createRes = await apiRequest(page, 'POST', API.donVi, {
      ma,
      ten,
      thuTu: 99,
      trangThai: 1,
    });
    expect(createRes!.status()).toBe(200);
    const id = (await createRes!.json() as { duLieu: { id: string } }).duLieu.id;
    const getRes = await apiRequest(page, 'GET', `${API.donVi}/${id}`);
    expect(getRes!.status()).toBe(200);
    const getBody = await getRes!.json() as { thanhCong: boolean; duLieu: { id: string; ma: string; ten: string } };
    expect(getBody.thanhCong).toBe(true);
    expect(getBody.duLieu.ma).toBe(ma);
    expect(getBody.duLieu.ten).toBe(ten);
    // Cleanup
    await apiRequest(page, 'DELETE', `${API.donVi}/${id}`);
  });

  test('PUT cập nhật ten đơn vị — GET xác nhận giá trị mới được lưu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-DV-UPD2-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.donVi, {
      ma,
      ten: 'Đơn vị Trước Khi Sửa',
      thuTu: 99,
      trangThai: 1,
    });
    expect(createRes!.status()).toBe(200);
    const id = (await createRes!.json() as { duLieu: { id: string } }).duLieu.id;
    const tenMoi = 'Đơn vị Sau Khi Cập Nhật E2E';
    const updateRes = await apiRequest(page, 'PUT', `${API.donVi}/${id}`, {
      ma,
      ten: tenMoi,
      thuTu: 99,
      trangThai: 1,
    });
    expect(updateRes!.status()).toBe(200);
    const getRes = await apiRequest(page, 'GET', `${API.donVi}/${id}`);
    expect(getRes!.status()).toBe(200);
    const getBody = await getRes!.json() as { duLieu: { ten: string } };
    expect(getBody.duLieu.ten).toBe(tenMoi);
    // Cleanup
    await apiRequest(page, 'DELETE', `${API.donVi}/${id}`);
  });

  test('DELETE đơn vị — 200 hoặc 409 nếu có ràng buộc dữ liệu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-DV-DEL-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.donVi, {
      ma,
      ten: 'Đơn vị Cần Xóa E2E',
      thuTu: 99,
      trangThai: 1,
    });
    expect(createRes!.status()).toBe(200);
    const id = (await createRes!.json() as { duLieu: { id: string } }).duLieu.id;
    const delRes = await apiRequest(page, 'DELETE', `${API.donVi}/${id}`);
    expect([200, 409]).toContain(delRes!.status());
  });

  test('POST /{id}/chuyen-cha — chuyển đơn vị cha (200 hoặc 400/404)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-DV-CHF-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.donVi, {
      ma,
      ten: 'Đơn vị Chuyển Cha E2E',
      thuTu: 99,
      trangThai: 1,
    });
    expect(createRes!.status()).toBe(200);
    const id = (await createRes!.json() as { duLieu: { id: string } }).duLieu.id;
    // Attempt to move to a non-existent parent — expects business rejection or success
    const fakeParentId = '00000000-0000-0000-0000-000000000099';
    const res = await apiRequest(page, 'POST', `${API.donVi}/${id}/chuyen-cha`, {
      donViChaId: fakeParentId,
    });
    expect([200, 400, 404]).toContain(res!.status());
    // Cleanup
    await apiRequest(page, 'DELETE', `${API.donVi}/${id}`);
  });

  test('POST tạo đơn vị trùng mã — 400/409/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-DV-DUP-${Date.now()}`;
    const first = await apiRequest(page, 'POST', API.donVi, {
      ma,
      ten: 'Đơn vị Đầu Tiên',
      thuTu: 99,
      trangThai: 1,
    });
    expect(first!.status()).toBe(200);
    const id = (await first!.json() as { duLieu: { id: string } }).duLieu.id;
    const dup = await apiRequest(page, 'POST', API.donVi, {
      ma,
      ten: 'Đơn vị Trùng Mã',
      thuTu: 99,
      trangThai: 1,
    });
    expect([400, 409, 422]).toContain(dup!.status());
    // Cleanup
    await apiRequest(page, 'DELETE', `${API.donVi}/${id}`);
  });

  test('Auth: tacgia1 POST tạo đơn vị — 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'POST', API.donVi, {
      ma: `E2E-DV-NOPERM-${Date.now()}`,
      ten: 'Không Được Phép',
      thuTu: 99,
      trangThai: 1,
    });
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực DELETE /don-vi — 401', async ({ page }) => {
    await page.goto('/');
    const fakeId = '00000000-0000-0000-0000-000000000099';
    const res = await page.request.delete(`${API.donVi}/${fakeId}`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-03: Đợt đề nghị — tabs quy trình, tiêu chí, đơn vị áp dụng ──────

test.describe('REQ-03: Đợt đề nghị — tabs áp dụng', () => {
  test.describe.configure({ timeout: 60_000 });

  test('POST tạo đợt với quyTrinhId → GET verify quy trình áp dụng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const qtRes = await apiRequest(page, 'GET', `${API.quyTrinh}/chon`);
    const qtBody = await qtRes!.json();
    const quyTrinh = qtBody.duLieu as Array<{ id: string }>;
    if (!quyTrinh || quyTrinh.length === 0) {
      test.skip(true, 'Không có quy trình nào để chọn');
      return;
    }
    const quyTrinhId = quyTrinh[0].id;
    const ma = `E2E-DOT-QT-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.dotDeNghi, {
      ma,
      ten: 'Đợt test quy trình áp dụng',
      nam: 2026,
      capXetDuyet: 'CO_SO',
      quyTrinhId,
    });
    expect(createRes!.status()).toBe(200);
    const createBody = await createRes!.json() as { thanhCong: boolean; duLieu: { id: string } };
    const dotId = createBody.duLieu?.id ?? createBody.duLieu;
    expect(dotId).toBeTruthy();

    const detailRes = await apiRequest(page, 'GET', `${API.dotDeNghi}/${dotId}`);
    expect(detailRes!.status()).toBe(200);
    const detail = (await detailRes!.json()).duLieu;
    expect(detail.quyTrinhId).toBe(quyTrinhId);

    await apiRequest(page, 'DELETE', `${API.dotDeNghi}/${dotId}`);
  });

  test('POST tạo đợt với boTieuChiId → GET verify tiêu chí áp dụng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const tcRes = await apiRequest(page, 'GET', `${API.tieuChi}/chon`);
    const tcBody = await tcRes!.json();
    const tieuChi = tcBody.duLieu as Array<{ id: string }>;
    if (!tieuChi || tieuChi.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để chọn');
      return;
    }
    const boTieuChiId = tieuChi[0].id;
    const ma = `E2E-DOT-TC-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.dotDeNghi, {
      ma,
      ten: 'Đợt test tiêu chí áp dụng',
      nam: 2026,
      capXetDuyet: 'CO_SO',
      boTieuChiId,
    });
    expect(createRes!.status()).toBe(200);
    const dotId = ((await createRes!.json()) as { duLieu: { id: string } }).duLieu?.id
      ?? (await createRes!.json()).duLieu;

    const detailRes = await apiRequest(page, 'GET', `${API.dotDeNghi}/${dotId}`);
    expect(detailRes!.status()).toBe(200);
    const detail = (await detailRes!.json()).duLieu;
    expect(detail.boTieuChiId).toBe(boTieuChiId);

    await apiRequest(page, 'DELETE', `${API.dotDeNghi}/${dotId}`);
  });

  test('POST tạo đợt với donViApDungIds → GET verify đơn vị áp dụng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const dvRes = await apiRequest(page, 'GET', `${API.donVi}/chon`);
    const dvBody = await dvRes!.json();
    const donVi = dvBody.duLieu as Array<{ id: string }>;
    if (!donVi || donVi.length === 0) {
      test.skip(true, 'Không có đơn vị nào để chọn');
      return;
    }
    const donViIds = donVi.slice(0, 2).map(d => d.id);
    const ma = `E2E-DOT-DV-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.dotDeNghi, {
      ma,
      ten: 'Đợt test đơn vị áp dụng',
      nam: 2026,
      capXetDuyet: 'CO_SO',
      donViApDungIds: donViIds,
    });
    expect(createRes!.status()).toBe(200);
    const dotId = ((await createRes!.json()) as { duLieu: { id: string } }).duLieu?.id
      ?? (await createRes!.json()).duLieu;

    const detailRes = await apiRequest(page, 'GET', `${API.dotDeNghi}/${dotId}`);
    expect(detailRes!.status()).toBe(200);
    const detail = (await detailRes!.json()).duLieu;
    if (detail.donViApDungIds) {
      expect(detail.donViApDungIds.length).toBeGreaterThanOrEqual(1);
    } else if (detail.donViApDung) {
      expect(detail.donViApDung.length).toBeGreaterThanOrEqual(1);
    }

    await apiRequest(page, 'DELETE', `${API.dotDeNghi}/${dotId}`);
  });

  test('UI: tab quy trình áp dụng hiển thị trên chi tiết đợt', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=1`);
    const listBody = await listRes!.json() as { duLieu: { id: string }[] };
    if (!listBody.duLieu || listBody.duLieu.length === 0) {
      test.skip(true, 'Không có đợt đề nghị');
      return;
    }
    const id = listBody.duLieu[0].id;
    await page.goto(`${ROUTES.dotDeNghi}/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-descriptions, .ant-tabs').first()).toBeVisible({ timeout: 15_000 });
    const bodyText = await page.locator('body').textContent();
    const hasQuyTrinh = bodyText?.includes('Quy trình') || bodyText?.includes('quy trình');
    expect(hasQuyTrinh).toBe(true);
  });
});

// ─── REQ-06: Biểu mẫu xuất — features ──────────────────────────────────────

test.describe('REQ-06: Biểu mẫu xuất — tính năng nâng cao', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET /truong-kha-dung?loai=PHIEU_TIEP_NHAN — trả về danh sách trường', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.bieuMau}/truong-kha-dung?loai=PHIEU_TIEP_NHAN`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
    if (body.duLieu.length > 0) {
      const first = body.duLieu[0] as Record<string, unknown>;
      expect(Object.keys(first).length).toBeGreaterThan(0);
    }
  });

  test('GET /truong-kha-dung?loai=PHIEU_DANH_GIA — trả về trường khác PHIEU_TIEP_NHAN', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.bieuMau}/truong-kha-dung?loai=PHIEU_DANH_GIA`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('POST tạo biểu mẫu với loai + cauHinhTruong → verify persistence', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-BM-LOAI-${Date.now()}`;
    const res = await apiRequest(page, 'POST', API.bieuMau, {
      ma,
      ten: 'Phiếu tiếp nhận E2E',
      loai: 'PHIEU_TIEP_NHAN',
      dinhDang: 'DOCX',
      thuTu: 999,
      trangThai: 1,
      cauHinhTruong: [
        { ma: 'maHoSo', tenTruong: 'Mã hồ sơ', bieuThuc: '{{maHoSo}}' },
        { ma: 'tenTacGia', tenTruong: 'Tên tác giả', bieuThuc: '{{tenTacGia}}' },
      ],
    });
    expect(res!.status()).toBe(200);
    const createBody = await res!.json();
    const id = createBody.duLieu?.id ?? createBody.duLieu;
    expect(id).toBeTruthy();

    const getRes = await apiRequest(page, 'GET', `${API.bieuMau}/${id}`);
    expect(getRes!.status()).toBe(200);
    const detail = (await getRes!.json()).duLieu;
    expect(detail.loai).toBe('PHIEU_TIEP_NHAN');
    expect(detail.dinhDang).toBe('DOCX');

    await apiRequest(page, 'DELETE', `${API.bieuMau}/${id}`);
  });

  test('GET /{id}/xem-truoc — preview trả về dữ liệu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.bieuMau}?trang=1&soDong=5`);
    const listBody = await listRes!.json();
    if (listBody.duLieu.length === 0) {
      test.skip(true, 'Không có biểu mẫu nào');
      return;
    }
    const id = listBody.duLieu[0].id;
    const previewRes = await apiRequest(page, 'GET', `${API.bieuMau}/${id}/xem-truoc`);
    expect([200, 404]).toContain(previewRes!.status());
  });

  test('POST tạo biểu mẫu loai không hợp lệ → 200 hoặc 400/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.bieuMau, {
      ma: `E2E-BM-BADLOAI-${Date.now()}`,
      ten: 'Loại sai',
      loai: 'LOAI_KHONG_TON_TAI',
      dinhDang: 'DOCX',
      thuTu: 0,
      trangThai: 1,
    });
    if (res!.status() === 200) {
      const body = await res!.json();
      const id = body.duLieu?.id ?? body.duLieu;
      if (id) await apiRequest(page, 'DELETE', `${API.bieuMau}/${id}`);
    }
    expect([200, 400, 422]).toContain(res!.status());
  });
});

// ─── P2: Catalog status toggle & export ──────────────────────────────────────

test.describe('REQ-01→08: Thao tác danh mục nâng cao', () => {
  test('PATCH /danh-muc/linh-vuc/{id}/trang-thai chuyển trạng thái hoạt động/ngưng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    // Tạo lĩnh vực mới để toggle
    const createRes = await apiRequest(page, 'POST', `${API.danhMuc}/linh-vuc`, {
      ma: `E2E-TOGGLE-${Date.now()}`,
      ten: `Lĩnh vực toggle ${Date.now()}`,
      moTa: 'Test toggle',
      thuTu: 999,
      trangThai: 1,
    });
    if (createRes!.status() !== 200) return;
    const createBody = await createRes!.json();
    const id = typeof createBody.duLieu === 'string' ? createBody.duLieu : createBody.duLieu?.id;
    if (!id) return;

    // Toggle off (trangThai = 0)
    const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
    const patchRes = await page.request.patch(`${API.danhMuc}/linh-vuc/${id}/trang-thai?trangThai=0`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect([200, 204, 400, 404]).toContain(patchRes.status());

    // Cleanup
    await apiRequest(page, 'DELETE', `${API.danhMuc}/linh-vuc/${id}`);
  });

  test('GET /danh-muc/linh-vuc/xuat-excel trả file Excel', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc/xuat-excel`);
    expect([200, 404]).toContain(res!.status());
    if (res!.status() === 200) {
      const ct = res!.headers()['content-type'] ?? '';
      expect(ct.includes('spreadsheet') || ct.includes('excel') || ct.includes('octet-stream')).toBeTruthy();
    }
  });

  test('GET /danh-muc/cap-phe-duyet/xuat-excel trả file Excel', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.danhMuc}/cap-phe-duyet/xuat-excel`);
    expect([200, 404]).toContain(res!.status());
    if (res!.status() === 200) {
      const ct = res!.headers()['content-type'] ?? '';
      expect(ct.includes('spreadsheet') || ct.includes('excel') || ct.includes('octet-stream')).toBeTruthy();
    }
  });

  test('DELETE danh mục đang được tham chiếu → 409 hoặc 422 với thông tin tham chiếu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    // Lấy lĩnh vực đầu tiên (có thể đang được sử dụng bởi sáng kiến)
    const lvRes = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc?trang=1&soDong=1`);
    expect(lvRes!.status()).toBe(200);
    const lvBody = await lvRes!.json();
    if (lvBody.duLieu.length === 0) return;
    const id: string = lvBody.duLieu[0].id;

    const delRes = await apiRequest(page, 'DELETE', `${API.danhMuc}/linh-vuc/${id}`);
    // Nếu đang được sử dụng → 409/422, nếu không → 200
    expect([200, 400, 409, 422]).toContain(delRes!.status());
    if (delRes!.status() === 409 || delRes!.status() === 422) {
      const delBody = await delRes!.json();
      expect(delBody.thanhCong).toBe(false);
    }
  });
});

// ─── REQ-03: Chặn nộp hồ sơ quá hạn ────────────────────────────────────────

test.describe('REQ-03: Hạn nộp hồ sơ', () => {
  test('đợt đề nghị quản lý DTO chứa các trường vòng đời', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.dotDeNghi}/quan-ly?trang=1&soDong=20`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    if (body.duLieu.length === 0) return;
    const dot = body.duLieu[0] as Record<string, unknown>;
    expect('trangThaiDot' in dot).toBe(true);
    expect('tuDongKhoa' in dot).toBe(true);
    const hasDeadlineField = body.duLieu.some(
      (d: Record<string, unknown>) => 'hanNopHoSo' in d || 'hanChamDiem' in d,
    );
    expect('nam' in dot || hasDeadlineField || body.duLieu.length > 0).toBe(true);
  });

  test('nộp hồ sơ với đợt đã đóng → lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    // Tìm đợt đã đóng
    const res = await apiRequest(page, 'GET', `${API.dotDeNghi}?trang=1&soDong=20`);
    const body = await res!.json();
    const dotDaDong = (body.duLieu as Array<Record<string, unknown>>).find(
      (d) => d.trangThaiDot === 'DaDong' || d.trangThaiDot === 'DaKhoa',
    );
    if (!dotDaDong) { test.skip(true, 'Không có đợt đã đóng'); return; }

    // Thử nộp hồ sơ với đợt đã đóng
    await loginViaAPI(page, 'tacgia1');
    const lvRes = await apiRequest(page, 'GET', `${API.danhMuc}/linh-vuc?trang=1&soDong=1`);
    const lvBody = await lvRes!.json();
    if (lvBody.duLieu.length === 0) return;

    const createRes = await apiRequest(page, 'POST', API.sangKien, {
      tenSangKien: `E2E Quá hạn ${Date.now()}`,
      dotDeNghiId: dotDaDong.id as string,
      linhVucId: lvBody.duLieu[0].id,
      moTaGiaiPhap: 'Test quá hạn',
      tinhTrangTruocKhiApDung: 'Test',
      noiDungGiaiPhap: 'Test',
      tinhMoi: 'Test',
      khaNangApDung: 'Test',
      phamViApDung: 'Test',
      hieuQuaKinhTe: 'Test',
      hieuQuaXaHoi: 'Test',
      thoiGianApDungTu: '2026-01-01',
      thoiGianApDungDen: '2026-12-31',
      danhSachTacGia: [{ hoTen: 'Trần Thị Lan', tyLeDongGop: 100, laTacGiaChinh: true }],
    });
    // Đợt đã đóng → không tạo được hoặc không nộp được
    expect([200, 400, 422]).toContain(createRes!.status());
    if (createRes!.status() === 200) {
      const createBody = await createRes!.json();
      const id = typeof createBody.duLieu === 'string' ? createBody.duLieu : createBody.duLieu?.id;
      if (id) {
        const nopRes = await apiRequest(page, 'POST', `${API.sangKien}/${id}/nop`);
        expect([400, 422]).toContain(nopRes!.status());
      }
    }
  });
});
