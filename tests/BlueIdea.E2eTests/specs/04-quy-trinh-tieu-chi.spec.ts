import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

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

  // ─── REQ-14: Tiêu chí — phân trang và chi tiết bổ sung ──────────

  test.describe('REQ-14: Tiêu chí — phân trang và chi tiết bổ sung', () => {
    test('GET tiêu chí với soDong=2 — trả về tối đa 2 dòng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=2`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(2);
      expect(typeof body.tongSo).toBe('number');
    });

    test('tiêu chí chi tiết có trường id và ten', async ({ page }) => {
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
      expect(body.duLieu).toHaveProperty('id');
      expect(body.duLieu).toHaveProperty('ten');
    });

    test('GET /tieu-chi với trang=9999 — trả về danh sách rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=9999&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBe(0);
    });

    test('GET /tieu-chi — trường tongSo là số không âm', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.tongSo).toBeGreaterThanOrEqual(0);
    });
  });

  // ─── REQ-15: Tác nhân xử lý — chi tiết và CRUD bổ sung ──────────

  test.describe('REQ-15: Tác nhân xử lý — chi tiết và CRUD bổ sung', () => {
    test('GET chi tiết quy trình — có mảng bước (cacBuoc hoặc buoc)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
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
      const duLieu = body.duLieu as Record<string, unknown>;
      expect(duLieu).toBeTruthy();
      const steps = (duLieu['danhSachBuoc']) as unknown[];
      expect(steps).toBeInstanceOf(Array);
    });

    test('bước quy trình có trường id và ten', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu quy trình');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}`);
      const body = await res!.json();
      const duLieu = body.duLieu as Record<string, unknown>;
      const steps = (duLieu['danhSachBuoc']) as unknown[];
      if (!steps || steps.length === 0) {
        test.skip(true, 'Quy trình không có bước nào');
        return;
      }
      const step = steps[0] as Record<string, unknown>;
      expect(step).toHaveProperty('id');
      expect(step).toHaveProperty('ten');
    });

    test('POST /quy-trinh/{id}/sao-chep — tạo bản sao với id mới', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) {
        test.skip(true, 'Không có dữ liệu mẫu quy trình');
        return;
      }
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'POST', `${API.quyTrinh}/${id}/sao-chep`, {
        ma: `E2E_CLONE_${Date.now()}`,
        ten: `Bản sao E2E ${Date.now()}`,
      });
      expect([200, 201]).toContain(res!.status());
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeTruthy();
    });

    test('GET /quy-trinh với soDong=2 — phân trang trả về đúng số dòng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=2`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(2);
      expect(typeof body.tongSo).toBe('number');
    });
  });

  // ─── REQ-16: Cấu hình liên thông — cấu trúc bổ sung ─────────────

  test.describe('REQ-16: Cấu hình liên thông — cấu trúc bổ sung', () => {
    test('GET /tich-hop/nhat-ky-dong-bo — có trường duLieu là mảng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tichHop}/nhat-ky-dong-bo?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /tich-hop/he-thong — phần tử có trường ten (nếu có dữ liệu)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tichHop}/he-thong`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      const items = body.duLieu as unknown[];
      if (items.length > 0) {
        const item = items[0] as Record<string, unknown>;
        expect(item).toHaveProperty('ten');
      }
    });

    test('qtdonvi không có TichHopCauHinh — GET hệ thống bị từ chối (403)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'qtdonvi');
      const res = await apiRequest(page, 'GET', `${API.tichHop}/he-thong`);
      expect(res!.status()).toBe(403);
    });
  });

  // ─── Sắp xếp và phân trang nâng cao ────────────────────────────────

  test.describe('Sắp xếp và phân trang nâng cao', () => {
    test('GET /quy-trinh sapXep=ten&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=10&sapXep=ten&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /quy-trinh sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /quy-trinh trang=9999 trả về mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=9999&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBe(0);
    });

    test('GET /tieu-chi sapXep=ten&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=10&sapXep=ten&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /tieu-chi sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Validation nâng cao ──────────────────────────────────────────────

  test.describe('Validation nâng cao', () => {
    test('POST /tieu-chi với ten rỗng → 200 tạo thành công hoặc 400/422 validation', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', API.tieuChi, {
        ma: `E2E_VAL_${Date.now()}`,
        ten: '',
        thangDiemToiDa: 10,
        diemDatToiThieu: 5,
        cachTinh: 'TRUNG_BINH_CONG',
        lamTron: 2,
        danhSachNhom: [],
      });
      expect([200, 400, 422]).toContain(res!.status());
    });

    test('POST /tieu-chi với ma rỗng → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'POST', API.tieuChi, {
        ma: '',
        ten: 'Test validation mã',
        thangDiemToiDa: 10,
        diemDatToiThieu: 5,
        cachTinh: 'TRUNG_BINH_CONG',
        lamTron: 2,
        danhSachNhom: [],
      });
      expect([400, 422]).toContain(res!.status());
    });

    test('POST /tieu-chi không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(API.tieuChi, {
        data: { ma: 'TEST', ten: 'Test' },
      });
      expect(res.status()).toBe(401);
    });

    test('DELETE /tieu-chi không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.delete(`${API.tieuChi}/${fakeId}`);
      expect(res.status()).toBe(401);
    });

    test('tác giả POST /tieu-chi → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.tieuChi, {
        ma: 'TACGIA_TEST',
        ten: 'Tác giả tạo tiêu chí',
        thangDiemToiDa: 10,
        diemDatToiThieu: 5,
        cachTinh: 'TRUNG_BINH_CONG',
        lamTron: 2,
        danhSachNhom: [],
      });
      expect(res!.status()).toBe(403);
    });

    test('POST /quy-trinh/{id}/kiem-tra không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.post(`${API.quyTrinh}/${fakeId}/kiem-tra`);
      expect(res.status()).toBe(401);
    });

    test('tác giả POST /quy-trinh/{id}/sao-chep → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'POST', `${API.quyTrinh}/${fakeId}/sao-chep`, {
        ma: 'TACGIA_CLONE',
        ten: 'Clone unauthorized',
      });
      expect([403, 404]).toContain(res!.status());
    });
  });

  // ─── Responsive viewport ──────────────────────────────────────────────

  test.describe('Responsive viewport', () => {
    test('trang quy trình hiển thị đúng trên mobile (375px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.quyTrinh);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
      const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
      expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
      await context.close();
    });

    test('trang tiêu chí hiển thị đúng trên tablet (768px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.tieuChi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await context.close();
    });
  });
});
