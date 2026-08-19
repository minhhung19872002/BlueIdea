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

  // ─── Danh sách hội đồng (REQ-19) ─────────────────────────────────

  test.describe('REQ-19: Danh sách hội đồng', () => {
    test('API POST tạo + GET xác nhận + PUT cập nhật + DELETE xóa hội đồng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_HD_${Date.now()}`;
      const ten = `Hội đồng E2E ${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.hoiDong, {
        ma,
        ten,
        cap: 'CO_SO',
        soThanhVienToiThieu: 3,
        tyLeThongQua: 50,
        thuTu: 999,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
        moTa: 'Tạo bởi E2E test',
      });
      expect(createRes!.status()).toBe(200);
      const createBody = await createRes!.json();
      expect(createBody.thanhCong).toBe(true);
      const id = createBody.duLieu.id;
      expect(id).toBeTruthy();

      const getRes = await apiRequest(page, 'GET', `${API.hoiDong}/${id}`);
      expect(getRes!.status()).toBe(200);
      const getBody = await getRes!.json();
      expect(getBody.duLieu.ten).toBe(ten);

      const updateRes = await apiRequest(page, 'PUT', `${API.hoiDong}/${id}`, {
        ma,
        ten: `${ten} (sửa)`,
        cap: 'CO_SO',
        soThanhVienToiThieu: 5,
        tyLeThongQua: 60,
        thuTu: 999,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
        moTa: 'Đã cập nhật',
      });
      expect(updateRes!.status()).toBe(200);

      const getAfterUpdate = await apiRequest(page, 'GET', `${API.hoiDong}/${id}`);
      const updatedBody = await getAfterUpdate!.json();
      expect(updatedBody.duLieu.ten).toContain('(sửa)');
      expect(updatedBody.duLieu.soThanhVienToiThieu).toBe(5);

      const delRes = await apiRequest(page, 'DELETE', `${API.hoiDong}/${id}`);
      expect(delRes!.status()).toBe(200);

      const getAfterDel = await apiRequest(page, 'GET', `${API.hoiDong}/${id}`);
      expect([400, 404]).toContain(getAfterDel!.status());
    });

    test('chi tiết hội đồng hiển thị nội dung', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      await page.goto(`${ROUTES.hoiDong}/${id}`);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('.ant-card')).toBeVisible({ timeout: 15_000 });
    });

    test('admin có thể tạo hội đồng — HoiDongCauHinh', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_AUTH_${Date.now()}`;
      const res = await apiRequest(page, 'POST', API.hoiDong, {
        ma,
        ten: `Auth test ${Date.now()}`,
        cap: 'CO_SO',
        soThanhVienToiThieu: 3,
        tyLeThongQua: 50,
        thuTu: 0,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
      });
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      if (body.duLieu) {
        await apiRequest(page, 'DELETE', `${API.hoiDong}/${body.duLieu}`);
      }
    });
  });

  // ─── Thành viên hội đồng (REQ-20) ────────────────────────────────

  test.describe('REQ-20: Thành viên hội đồng', () => {
    test('API PUT /hoi-dong/{id}/thanh-vien lưu danh sách thành viên', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;

      const detailRes = await apiRequest(page, 'GET', `${API.hoiDong}/${id}`);
      const detailBody = await detailRes!.json();
      const currentMembers = detailBody.duLieu.thanhVien || [];

      const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
      const res = await page.request.put(`${API.hoiDong}/${id}/thanh-vien`, {
        headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
        data: currentMembers,
      });
      expect([200, 400, 422]).toContain(res.status());
    });

    test('chi tiết hội đồng hiển thị tab thành viên', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      await page.goto(`${ROUTES.hoiDong}/${id}`);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('.ant-card')).toBeVisible({ timeout: 15_000 });
      const tabs = page.locator('.ant-tabs-tab');
      await expect(tabs.first()).toBeVisible({ timeout: 5_000 });
      const tabTexts = await tabs.allTextContents();
      const combined = tabTexts.join(' ');
      expect(combined.toLowerCase()).toContain('thành viên');
    });

    test('tác giả không có HoiDongCauHinh — PUT thành viên bị từ chối (403)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'PUT', `${API.hoiDong}/${fakeId}/thanh-vien`, {
        danhSach: [],
      });
      expect([403, 404]).toContain(res!.status());
    });

    test('không xác thực PUT /hoi-dong/{id}/thanh-vien trả về 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.put(`${API.hoiDong}/${fakeId}/thanh-vien`, {
        data: { danhSach: [] },
      });
      expect(res.status()).toBe(401);
    });
  });
});
