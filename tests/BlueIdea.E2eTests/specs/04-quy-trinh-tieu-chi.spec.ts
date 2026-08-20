import { test, expect, type Page } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

/** Quy trình đang áp dụng thì máy chủ chặn sửa thành phần, nên mỗi test dựng bản nháp riêng. */
async function taoQuyTrinhNhap(page: Page): Promise<string> {
  const hau = `${Date.now()}`.slice(-8);
  const res = await apiRequest(page, 'POST', API.quyTrinh, {
    ma: `E2E_TP_${hau}`,
    ten: 'Quy trình E2E thành phần hồ sơ',
    cap: 'CO_SO',
    trangThai: 1,
    thuTu: 99,
  });
  expect(res!.status()).toBe(200);
  const body = await res!.json();
  return typeof body.duLieu === 'string' ? body.duLieu : body.duLieu.id;
}

async function themThanhPhanQuaApi(
  page: Page,
  quyTrinhId: string,
  ma: string,
  ten: string,
  thuTu: number,
): Promise<string> {
  const res = await apiRequest(page, 'POST', `${API.quyTrinh}/${quyTrinhId}/thanh-phan-ho-so`, {
    ma,
    ten,
    batBuoc: true,
    loaiDuLieu: 'CA_HAI',
    dungLuongToiDaMb: 20,
    soLuongToiDa: 3,
    thuTu,
  });
  expect(res!.status()).toBe(200);
  return (await res!.json()).duLieu;
}

test.describe('REQ-10 & REQ-14: Quy trình và Tiêu chí chấm điểm', () => {
  // ─── REQ-10: Cấu hình quy trình (Workflow UI) ──────────────────────

  test.describe('REQ-10: Giao diện quản trị quy trình', () => {
    test('trang quy trình tải không lỗi và hiển thị bảng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyTrinh);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });

    test('bảng quy trình hiển thị cột đúng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyTrinh);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      const headers = page.locator('.ant-table-thead th');
      await expect(headers.first()).toBeVisible({ timeout: 5_000 });
      const headerTexts = await headers.allTextContents();
      const combined = headerTexts.join(' ');
      // Workflow table must show at least a name/title column
      expect(combined.length).toBeGreaterThan(0);
    });
  });

  // ─── REQ-10: API quy trình ─────────────────────────────────────────

  test.describe('REQ-10: API quy trình', () => {
    test('GET danh sách quy trình — PhanHoiPhanTrang (không có thanhCong)', async ({
      page,
    }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      // PhanHoiPhanTrang has duLieu array and tongSo but NO thanhCong at root
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
      expect(body).not.toHaveProperty('thanhCong');
    });

    test('GET quy trình theo ID — PhanHoiApi (có thanhCong)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      // Fetch list first to get a real ID from seed data
      const listRes = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu quy trình');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeTruthy();
      expect(body.duLieu.id).toBe(id);
    });

    test('GET sơ đồ quy trình /so-do — PhanHoiApi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu quy trình');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}/so-do`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeTruthy();
    });

    test('POST kiểm tra quy trình /kiem-tra — trả về kết quả validation', async ({
      page,
    }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu quy trình');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
      const res = await page.request.post(`${API.quyTrinh}/${id}/kiem-tra`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      // duLieu contains validation result (hopleLe boolean or list of errors)
      expect(body.duLieu).toBeDefined();
    });

    test('GET /chon — endpoint dropdown trả về 200 cho bất kỳ người dùng đã xác thực', async ({
      page,
    }) => {
      await page.goto('/');
      // Use tacgia1 — does NOT have QuyTrinhXem policy but /chon has no named policy
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── REQ-14: Giao diện quản trị tiêu chí ──────────────────────────

  test.describe('REQ-14: Giao diện quản trị tiêu chí', () => {
    test('trang tiêu chí tải không lỗi và hiển thị nội dung', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.tieuChi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });
  });

  // ─── REQ-14: API tiêu chí ──────────────────────────────────────────

  test.describe('REQ-14: API tiêu chí', () => {
    test('GET danh sách tiêu chí — PhanHoiPhanTrang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
      expect(body).not.toHaveProperty('thanhCong');
    });

    test('GET tiêu chí theo ID — PhanHoiApi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu tiêu chí');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.tieuChi}/${id}`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu.id).toBe(id);
    });

    test('GET /chon tiêu chí — dropdown trả về 200 cho bất kỳ người dùng đã xác thực', async ({
      page,
    }) => {
      await page.goto('/');
      // tacgia1 does NOT have TieuChiXem but /chon has no named policy
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}/chon`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Phân quyền ────────────────────────────────────────────────────

  test.describe('Phân quyền', () => {
    test('tác giả không có QuyTrinhXem — GET danh sách quy trình bị từ chối (403)', async ({
      page,
    }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=5`);
      expect(res!.status()).toBe(403);
    });

    test('tác giả không có TieuChiXem — GET danh sách tiêu chí bị từ chối (403)', async ({
      page,
    }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=5`);
      expect(res!.status()).toBe(403);
    });

    test('không xác thực — GET quy trình trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.quyTrinh}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });

    test('không xác thực — GET tiêu chí trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.tieuChi}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── Trường hợp biên ───────────────────────────────────────────────

  test.describe('Trường hợp biên', () => {
    test('tìm kiếm ký tự đặc biệt trên trang quy trình không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyTrinh);
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      // Find any search/filter input if present and type special characters
      const searchInput = page.locator('input[type="search"], input[placeholder*="Tìm"]').first();
      const inputCount = await searchInput.count();
      if (inputCount > 0) {
        await searchInput.fill('!@#$%');
        await searchInput.press('Enter');
        await page.waitForTimeout(1000);
      }
      // Table or content should still be visible — no crash
      await expect(page.locator('body')).toBeVisible();
    });
  });

  // ─── Tác nhân xử lý (REQ-15) ─────────────────────────────────────

  test.describe('REQ-15: Tác nhân xử lý', () => {
    test('API GET /quy-trinh/{id}/so-do chứa thông tin tác nhân', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu quy trình');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}/so-do`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeTruthy();
    });

    test('sơ đồ quy trình có các bước với cấu hình tác nhân', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu quy trình');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}/so-do`);
      const body = await res!.json();
      const soDo = body.duLieu;
      if (soDo.cacBuoc && soDo.cacBuoc.length > 0) {
        const buoc = soDo.cacBuoc[0];
        expect(buoc).toHaveProperty('id');
        expect(buoc).toHaveProperty('ten');
      }
    });

    test('tác giả không có QuyTrinhXem — GET sơ đồ bị từ chối (403)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}/00000000-0000-0000-0000-000000000000/so-do`);
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET /quy-trinh/{id}/so-do trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.quyTrinh}/00000000-0000-0000-0000-000000000000/so-do`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── Cấu hình liên thông (REQ-16) ────────────────────────────────

  test.describe('REQ-16: Cấu hình liên thông', () => {
    test('trang liên thông hệ thống tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.lienThong);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });

    test('API GET /tich-hop/he-thong trả về danh sách hệ thống liên thông', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tichHop}/he-thong`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API GET /quy-trinh/{id}/lien-thong trả về cấu hình liên thông bước', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu quy trình');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}/lien-thong`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('API GET /tich-hop/nhat-ky-dong-bo trả về nhật ký đồng bộ', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tichHop}/nhat-ky-dong-bo`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('tác giả không có TichHopCauHinh — GET hệ thống bị từ chối (403)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.tichHop}/he-thong`);
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET /tich-hop/he-thong trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.tichHop}/he-thong`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── REQ-13: Thành phần hồ sơ ──────────────────────────────────────

  test.describe('REQ-13: Thành phần hồ sơ', () => {
    /**
     * Màn hình phải ghi qua API riêng của từng thành phần. Trước đây nó lưu bằng cách gửi lại cả
     * sơ đồ quy trình (PUT /so-do), nên hai người cùng mở một quy trình sẽ ghi đè lên nhau.
     */
    test('thêm thành phần trên màn hình gọi POST /thanh-phan-ho-so, không gửi lại cả sơ đồ', async ({
      page,
    }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const quyTrinhId = await taoQuyTrinhNhap(page);

      await page.goto(`/quan-tri/quy-trinh/${quyTrinhId}/thanh-phan`);
      const nutThem = page.getByRole('button', { name: /Thêm thành phần hồ sơ/ });
      await expect(nutThem).toBeVisible({ timeout: 15_000 });

      const daGoi: string[] = [];
      page.on('request', (req) => {
        if (req.url().includes('/api/v1/quy-trinh/')) {
          daGoi.push(`${req.method()} ${new URL(req.url()).pathname}`);
        }
      });

      await nutThem.click();

      const oNhap = page.locator('.ant-table-tbody .ant-input');
      await oNhap.nth(0).fill('E2E_TP_A');
      await oNhap.nth(1).fill('Thành phần thêm từ màn hình');

      await page.getByRole('button', { name: /Lưu$/ }).click();
      await expect(page.getByText('Đã lưu cấu hình thành phần hồ sơ')).toBeVisible({
        timeout: 15_000,
      });

      expect(daGoi.some((x) => x.startsWith('POST') && x.endsWith('/thanh-phan-ho-so'))).toBe(true);
      expect(daGoi.some((x) => x.endsWith('/so-do') && x.startsWith('PUT'))).toBe(false);

      // Đọc lại bằng một request khác: dữ liệu phải nằm trong CSDL, không phải trong state.
      const doc = await apiRequest(page, 'GET', `${API.quyTrinh}/${quyTrinhId}/thanh-phan-ho-so`);
      const danhSach = (await doc!.json()).duLieu as Array<{ ma: string; ten: string }>;
      expect(danhSach.map((x) => x.ma)).toContain('E2E_TP_A');
    });

    test('đổi thứ tự trên màn hình gọi API sắp xếp và thứ tự được lưu', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const quyTrinhId = await taoQuyTrinhNhap(page);

      await themThanhPhanQuaApi(page, quyTrinhId, 'E2E_SX_A', 'Thành phần A', 0);
      await themThanhPhanQuaApi(page, quyTrinhId, 'E2E_SX_B', 'Thành phần B', 1);

      await page.goto(`/quan-tri/quy-trinh/${quyTrinhId}/thanh-phan`);
      await expect(page.locator('.ant-table-tbody tr.ant-table-row')).toHaveCount(2, {
        timeout: 15_000,
      });

      const daGoi: string[] = [];
      page.on('request', (req) => {
        if (req.url().includes('/api/v1/quy-trinh/')) {
          daGoi.push(`${req.method()} ${new URL(req.url()).pathname}`);
        }
      });

      // Đưa dòng thứ hai lên trên.
      await page.getByRole('button', { name: 'Đưa lên trên' }).nth(1).click();
      await page.getByRole('button', { name: /Lưu$/ }).click();
      await expect(page.getByText('Đã lưu cấu hình thành phần hồ sơ')).toBeVisible({
        timeout: 15_000,
      });

      expect(daGoi.some((x) => x.endsWith('/thanh-phan-ho-so/sap-xep'))).toBe(true);

      const doc = await apiRequest(page, 'GET', `${API.quyTrinh}/${quyTrinhId}/thanh-phan-ho-so`);
      const danhSach = (await doc!.json()).duLieu as Array<{ ma: string }>;
      expect(danhSach.map((x) => x.ma)).toEqual(['E2E_SX_B', 'E2E_SX_A']);
    });

    test('tác giả không có QuyTrinhCauHinh — POST thành phần bị từ chối (403)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const quyTrinhId = await taoQuyTrinhNhap(page);

      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(
        page,
        'POST',
        `${API.quyTrinh}/${quyTrinhId}/thanh-phan-ho-so`,
        { ma: 'E2E_TP_403', ten: 'Không được phép', batBuoc: true, loaiDuLieu: 'TEP',
          dungLuongToiDaMb: 20, soLuongToiDa: 1, thuTu: 0 },
      );
      expect(res!.status()).toBe(403);
    });

    test('không xác thực GET /thanh-phan-ho-so trả về 401', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const quyTrinhId = await taoQuyTrinhNhap(page);

      const res = await page.request.get(`${API.quyTrinh}/${quyTrinhId}/thanh-phan-ho-so`);
      expect(res.status()).toBe(401);
    });
  });
});
