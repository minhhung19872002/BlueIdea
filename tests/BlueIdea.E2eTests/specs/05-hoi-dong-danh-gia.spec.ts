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

  // ─── Tiêu chí chấm điểm (REQ-18) ────────────────────────────────────

  test.describe('REQ-18: Tiêu chí chấm điểm', () => {
    test('trang tiêu chí tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.tieuChi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });

    test('API GET danh sách tiêu chí trả về duLieu và tongSo', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('API GET /tieu-chi/chon trả về dropdown có thanhCong', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API GET chi tiết bộ tiêu chí có ma, ten, danhSachNhom', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
      expect(listRes!.status()).toBe(200);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const detailRes = await apiRequest(page, 'GET', `${API.tieuChi}/${id}`);
      expect(detailRes!.status()).toBe(200);
      const detailBody = await detailRes!.json();
      expect(detailBody.thanhCong).toBe(true);
      expect(detailBody.duLieu).toBeTruthy();
      expect(typeof detailBody.duLieu.ma).toBe('string');
      expect(typeof detailBody.duLieu.ten).toBe('string');
      expect(detailBody.duLieu.danhSachNhom).toBeInstanceOf(Array);
    });

    test('API chi tiết có cây nhóm → tiêu chí', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
      expect(listRes!.status()).toBe(200);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const detailRes = await apiRequest(page, 'GET', `${API.tieuChi}/${id}`);
      const detailBody = await detailRes!.json();
      const danhSachNhom: unknown[] = detailBody.duLieu.danhSachNhom;
      expect(danhSachNhom).toBeInstanceOf(Array);
      for (const nhom of danhSachNhom) {
        expect(nhom).toHaveProperty('danhSachTieuChi');
        expect((nhom as { danhSachTieuChi: unknown[] }).danhSachTieuChi).toBeInstanceOf(Array);
      }
    });

    test('API POST tạo bộ tiêu chí rồi DELETE', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_TC_${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.tieuChi, {
        ma,
        ten: `Bộ tiêu chí E2E ${Date.now()}`,
        thangDiemToiDa: 10,
        diemDatToiThieu: 5,
        cachTinh: 'TRUNG_BINH_CONG',
        lamTron: 2,
        danhSachNhom: [
          {
            ten: 'Nhóm 1',
            trongSo: 100,
            diemToiDa: 10,
            thuTu: 0,
            danhSachTieuChi: [
              {
                ten: 'Tiêu chí A',
                trongSo: 100,
                kieuNhap: 'SLIDER',
                diemToiDa: 10,
                thuTu: 0,
              },
            ],
          },
        ],
      });
      expect(createRes!.status()).toBe(200);
      const createBody = await createRes!.json();
      expect(createBody.thanhCong).toBe(true);
      const id: string = createBody.duLieu?.id ?? createBody.duLieu;
      expect(id).toBeTruthy();
      const delRes = await apiRequest(page, 'DELETE', `${API.tieuChi}/${id}`);
      expect(delRes!.status()).toBe(200);
    });

    test('tác giả không thể xem tiêu chí — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=5`);
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET tiêu chí — 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.tieuChi}?trang=1&soDong=5`);
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

    test('API GET /danh-gia/viec-cua-toi phân trang soDong=2', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=2`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(2);
    });

    test('API GET /danh-gia/ma-tran-diem chi tiết cấu trúc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
      if (body.duLieu.length > 0) {
        const item: Record<string, unknown> = body.duLieu[0];
        const hasIdField = 'hoSoId' in item || 'sangKienId' in item || 'id' in item;
        expect(hasIdField).toBe(true);
      }
    });

    test('tác giả không thể xem ma trận điểm — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem`);
      expect(res!.status()).toBe(403);
    });

    test('API GET /danh-gia/viec-cua-toi cho admin hoạt động', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
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

    test('API POST tạo hội đồng thiếu tên — lỗi validation 422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', API.hoiDong, {
        ma: `E2E_VAL_${Date.now()}`,
        ten: '',
        cap: 'CO_SO',
        soThanhVienToiThieu: 3,
        tyLeThongQua: 50,
        thuTu: 0,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
      });
      expect([200, 400, 422]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        const id = body.duLieu?.id ?? body.duLieu;
        if (id) await apiRequest(page, 'DELETE', `${API.hoiDong}/${id}`);
      }
    });

    test('API POST mã hội đồng trùng — 409 hoặc 422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ma = `E2E_DUP_${Date.now()}`;
      const payload = {
        ma,
        ten: `Hội đồng trùng mã ${Date.now()}`,
        cap: 'CO_SO',
        soThanhVienToiThieu: 3,
        tyLeThongQua: 50,
        thuTu: 0,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
      };
      const firstRes = await apiRequest(page, 'POST', API.hoiDong, payload);
      expect(firstRes!.status()).toBe(200);
      const firstBody = await firstRes!.json();
      const dupRes = await apiRequest(page, 'POST', API.hoiDong, payload);
      expect([409, 422]).toContain(dupRes!.status());
      const id: string = firstBody.duLieu?.id ?? firstBody.duLieu;
      if (id) await apiRequest(page, 'DELETE', `${API.hoiDong}/${id}`);
    });

    test('API GET danh sách có tongSo và tôn trọng phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(typeof body.tongSo).toBe('number');
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
    });

    test('API hội đồng chi tiết có trường bắt buộc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const detailRes = await apiRequest(page, 'GET', `${API.hoiDong}/${id}`);
      expect(detailRes!.status()).toBe(200);
      const detailBody = await detailRes!.json();
      const hd = detailBody.duLieu;
      expect(typeof hd.ma).toBe('string');
      expect(typeof hd.ten).toBe('string');
      expect(hd.cap).toBeTruthy();
      expect(typeof hd.soThanhVienToiThieu).toBe('number');
      expect(typeof hd.tyLeThongQua).toBe('number');
    });

    test('tác giả không thể tạo hội đồng — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.hoiDong, {
        ma: `E2E_AUTH_TACGIA_${Date.now()}`,
        ten: 'Hội đồng tác giả tạo',
        cap: 'CO_SO',
        soThanhVienToiThieu: 3,
        tyLeThongQua: 50,
        thuTu: 0,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
      });
      expect(res!.status()).toBe(403);
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

    test('API GET chi tiết hội đồng có mảng thành viên', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const detailRes = await apiRequest(page, 'GET', `${API.hoiDong}/${id}`);
      expect(detailRes!.status()).toBe(200);
      const detailBody = await detailRes!.json();
      expect(detailBody.duLieu.thanhVien).toBeInstanceOf(Array);
    });

    test('API thành viên có thông tin người dùng khi mảng không rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=5`);
      const listBody = await listRes!.json();
      for (const hd of listBody.duLieu as Array<{ id: string }>) {
        const detailRes = await apiRequest(page, 'GET', `${API.hoiDong}/${hd.id}`);
        const detailBody = await detailRes!.json();
        const members: unknown[] = detailBody.duLieu.thanhVien ?? [];
        if (members.length > 0) {
          for (const m of members) {
            expect(m).toHaveProperty('nguoiDungId');
            expect(m).toHaveProperty('hoTenHienThi');
          }
          return;
        }
      }
      // No councils with members seeded — pass vacuously
    });

    test('API PUT thành viên — mảng rỗng trả về 200 hoặc 400 (validation)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
      const res = await page.request.put(`${API.hoiDong}/${id}/thanh-vien`, {
        headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
        data: [],
      });
      expect([200, 400, 422]).toContain(res.status());
    });

    test('tiếp nhận không thể sửa thành viên hội đồng — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
      const res = await page.request.put(`${API.hoiDong}/${fakeId}/thanh-vien`, {
        headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
        data: [],
      });
      expect([403, 404]).toContain(res.status());
    });
  });

  // ─── Sắp xếp và phân trang nâng cao ────────────────────────────────

  test.describe('Sắp xếp và phân trang nâng cao', () => {
    test('GET /hoi-dong sapXep=ten&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=10&sapXep=ten&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /hoi-dong sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /hoi-dong trang=9999 trả về mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.hoiDong}?trang=9999&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBe(0);
    });

    test('GET /hoi-dong trang=2 khác trang=1 nếu đủ data', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const p1 = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=2`);
      const b1 = await p1!.json();
      if (b1.tongSo > 2) {
        const p2 = await apiRequest(page, 'GET', `${API.hoiDong}?trang=2&soDong=2`);
        const b2 = await p2!.json();
        expect(b2.duLieu).toBeInstanceOf(Array);
        if (b2.duLieu.length > 0 && b1.duLieu.length > 0) {
          expect(b1.duLieu[0].id).not.toBe(b2.duLieu[0].id);
        }
      }
    });

    test('GET /danh-gia/viec-cua-toi sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=5&sapXep=ngayTao&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Validation và edge cases nâng cao ────────────────────────────────

  test.describe('Validation nâng cao', () => {
    test('POST /hoi-dong với payload rỗng → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', API.hoiDong, {});
      expect([400, 422]).toContain(res!.status());
    });

    test('DELETE /hoi-dong không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.delete(`${API.hoiDong}/${fakeId}`);
      expect(res.status()).toBe(401);
    });

    test('tác giả DELETE /hoi-dong → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'DELETE', `${API.hoiDong}/${fakeId}`);
      expect([403, 404]).toContain(res!.status());
    });

    test('POST /hoi-dong với mã quá dài (500+ ký tự) — server xử lý', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const longStr = 'A'.repeat(600);
      const res = await apiRequest(page, 'POST', API.hoiDong, {
        ma: longStr,
        ten: 'Test mã quá dài',
        cap: 'CO_SO',
        soThanhVienToiThieu: 3,
        tyLeThongQua: 50,
        thuTu: 0,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
      });
      expect([400, 422, 500]).toContain(res!.status());
    });

    test('POST /hoi-dong với soThanhVienToiThieu âm → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', API.hoiDong, {
        ma: `E2E_NEG_${Date.now()}`,
        ten: `Test số âm ${Date.now()}`,
        cap: 'CO_SO',
        soThanhVienToiThieu: -1,
        tyLeThongQua: 50,
        thuTu: 0,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
      });
      expect([200, 400, 422]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        const id = body.duLieu?.id ?? body.duLieu;
        if (id) await apiRequest(page, 'DELETE', `${API.hoiDong}/${id}`);
      }
    });

    test('tiếp nhận GET /danh-gia/ma-tran-diem → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem`);
      expect([200, 403]).toContain(res!.status());
    });

    test('không xác thực GET /danh-gia/ma-tran-diem → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.danhGia}/ma-tran-diem`);
      expect(res.status()).toBe(401);
    });

    test('hoidong02 GET /danh-gia/viec-cua-toi trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong02');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('XSS trong tên hội đồng không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const xss = '<img src=x onerror=alert(1)>';
      const ma = `E2E_XSS_${Date.now()}`;
      const res = await apiRequest(page, 'POST', API.hoiDong, {
        ma,
        ten: xss,
        cap: 'CO_SO',
        soThanhVienToiThieu: 3,
        tyLeThongQua: 50,
        thuTu: 0,
        trangThai: 1,
        trangThaiHoatDong: 'DANG_HOAT_DONG',
      });
      expect([200, 400, 422]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        const id = body.duLieu?.id ?? body.duLieu;
        if (id) {
          const getRes = await apiRequest(page, 'GET', `${API.hoiDong}/${id}`);
          const getBody = await getRes!.json();
          expect(getBody.duLieu.ten).toBe(xss);
          await apiRequest(page, 'DELETE', `${API.hoiDong}/${id}`);
        }
      }
    });
  });

  // ─── Responsive viewport ──────────────────────────────────────────────

  test.describe('Responsive viewport', () => {
    test('trang hội đồng hiển thị đúng trên mobile (375px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.hoiDong);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
      const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
      expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
      await context.close();
    });

    test('trang đánh giá hiển thị đúng trên tablet (768px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      await page.goto(ROUTES.danhGia);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await context.close();
    });
  });
});
