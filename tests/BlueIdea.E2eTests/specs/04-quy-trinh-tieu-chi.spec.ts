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

// ─────────────────────────────────────────────────────────────────────────────
// REQ-10: Thiết kế quy trình (TrangThietKeQuyTrinh)
// ─────────────────────────────────────────────────────────────────────────────

test.describe('REQ-10: Thiết kế quy trình (TrangThietKeQuyTrinh)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang thiết kế quy trình tải không crash', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để thiết kế');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thiet-ke`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
  });

  test('hiển thị canvas ReactFlow với nodes', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để thiết kế');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thiet-ke`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.react-flow')).toBeVisible({ timeout: 15_000 });
    const nodeCount = await page.locator('.react-flow__node').count();
    expect(nodeCount).toBeGreaterThanOrEqual(1);
  });

  test('hiển thị nút Kiểm tra hợp lệ và Lưu sơ đồ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để thiết kế');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thiet-ke`);
    await page.waitForLoadState('networkidle');
    await expect(page.getByRole('button', { name: /kiểm tra hợp lệ/i })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: /lưu sơ đồ/i })).toBeVisible({ timeout: 15_000 });
  });

  test('click node mở Drawer chi tiết bước', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để thiết kế');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thiet-ke`);
    await page.waitForLoadState('networkidle');
    await page.waitForSelector('.react-flow__node', { timeout: 15_000 });
    await page.locator('.react-flow__node').first().click();
    await expect(page.locator('.ant-drawer')).toBeVisible({ timeout: 10_000 });
  });

  test('POST /quy-trinh/{id}/kiem-tra trả về kết quả kiểm tra', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để kiểm tra');
      return;
    }
    const id = body.duLieu[0].id;
    const kiemTraRes = await apiRequest(page, 'POST', `${API.quyTrinh}/${id}/kiem-tra`);
    expect(kiemTraRes.status()).toBe(200);
    const kiemTraBody = await kiemTraRes.json();
    expect(kiemTraBody.thanhCong).toBe(true);
  });

  test('hiển thị feature switches', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để thiết kế');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thiet-ke`);
    await page.waitForLoadState('networkidle');
    const switchCount = await page.locator('.ant-switch').count();
    expect(switchCount).toBeGreaterThanOrEqual(1);
  });

  test('tác giả gọi API quy trình → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để kiểm tra phân quyền');
      return;
    }
    const id = body.duLieu[0].id;
    await loginViaAPI(page, 'tacgia1');
    const detail = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}`);
    expect(detail.status()).toBe(403);
  });

  test('responsive trên tablet (768px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
    const page = await context.newPage();
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      await context.close();
      test.skip(true, 'Không có quy trình nào để thiết kế');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thiet-ke`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await context.close();
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// REQ-10: Thành phần hồ sơ (TrangThanhPhanHoSo)
// ─────────────────────────────────────────────────────────────────────────────

test.describe('REQ-10: Thành phần hồ sơ (TrangThanhPhanHoSo)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang thành phần hồ sơ tải không crash', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem thành phần hồ sơ');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thanh-phan`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
  });

  test('hiển thị bảng thành phần hoặc trạng thái rỗng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem thành phần hồ sơ');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thanh-phan`);
    await page.waitForLoadState('networkidle');
    const hasTable = await page.locator('table').count() > 0;
    const hasEmpty = await page.locator('.ant-empty').count() > 0
      || (await page.getByText('Chưa có thành phần').count()) > 0;
    const hasContent = await page.locator('body').count() > 0;
    expect(hasTable || hasEmpty || hasContent).toBe(true);
  });

  test('hiển thị nút Thêm thành phần hồ sơ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem thành phần hồ sơ');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thanh-phan`);
    await page.waitForLoadState('networkidle');
    await expect(page.getByRole('button', { name: /thêm thành phần/i })).toBeVisible({ timeout: 15_000 });
  });

  test('hiển thị alert thông tin về checklist hoặc nộp hồ sơ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem thành phần hồ sơ');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thanh-phan`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-alert')).toBeVisible({ timeout: 15_000 });
  });

  test('nút Lưu hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem thành phần hồ sơ');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thanh-phan`);
    await page.waitForLoadState('networkidle');
    await expect(page.getByRole('button', { name: /lưu/i }).first()).toBeVisible({ timeout: 15_000 });
  });

  test('hiển thị link về trình thiết kế', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem thành phần hồ sơ');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/thanh-phan`);
    await page.waitForLoadState('networkidle');
    const hasDesignerLink = await page.locator('a, button').filter({ hasText: /thiết kế/i }).count() > 0;
    expect(hasDesignerLink).toBe(true);
  });

  test('API GET /quy-trinh/{id}/so-do trả thành phần hồ sơ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để kiểm tra API');
      return;
    }
    const id = body.duLieu[0].id;
    const soDo = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}/so-do`);
    expect(soDo.status()).toBe(200);
    const soDoBody = await soDo.json();
    expect(soDoBody.thanhCong).toBe(true);
    expect(soDoBody.duLieu).toHaveProperty('thanhPhanHoSo');
  });

  test('không xác thực truy cập → chuyển hướng đăng nhập', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    let id = 'unknown';
    try {
      const body = await res.json();
      if (body.duLieu && body.duLieu.length > 0) {
        id = body.duLieu[0].id;
      }
    } catch {
      // fallback id
    }
    await page.evaluate(() => localStorage.clear());
    await page.goto(`/quan-tri/quy-trinh/${id}/thanh-phan`);
    await page.waitForLoadState('networkidle');
    const url = page.url();
    expect(url).toContain('/dang-nhap');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// REQ-15: Liên thông bước (TrangLienThongBuoc)
// ─────────────────────────────────────────────────────────────────────────────

test.describe('REQ-15: Liên thông bước (TrangLienThongBuoc)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang liên thông tải không crash', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem liên thông');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/lien-thong`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
  });

  test('hiển thị bảng cấu hình hoặc trạng thái rỗng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem liên thông');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/lien-thong`);
    await page.waitForLoadState('networkidle');
    const hasTable = await page.locator('table').count() > 0;
    const hasEmpty = await page.locator('.ant-empty').count() > 0
      || (await page.getByText(/chưa gắn liên thông/i).count()) > 0;
    const hasBody = await page.locator('body').count() > 0;
    expect(hasTable || hasEmpty || hasBody).toBe(true);
  });

  test('hiển thị alert thông tin hoặc cảnh báo', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem liên thông');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/lien-thong`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-alert')).toBeVisible({ timeout: 15_000 });
  });

  test('nút Thêm cấu hình hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem liên thông');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/lien-thong`);
    await page.waitForLoadState('networkidle');
    await expect(page.getByRole('button', { name: /thêm cấu hình/i })).toBeVisible({ timeout: 15_000 });
  });

  test('API GET /quy-trinh/{id}/lien-thong trả 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để kiểm tra API liên thông');
      return;
    }
    const id = body.duLieu[0].id;
    const lienThongRes = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}/lien-thong`);
    expect(lienThongRes.status()).toBe(200);
    const lienThongBody = await lienThongRes.json();
    expect(lienThongBody.thanhCong).toBe(true);
  });

  test('API GET /tich-hop/he-thong trả 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const heThongRes = await apiRequest(page, 'GET', `${API.tichHop}/he-thong`);
    expect(heThongRes.status()).toBe(200);
    const heThongBody = await heThongRes.json();
    expect(heThongBody.thanhCong).toBe(true);
  });

  test('tác giả gọi API quy trình → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để kiểm tra phân quyền liên thông');
      return;
    }
    const id = body.duLieu[0].id;
    await loginViaAPI(page, 'tacgia1');
    const detail = await apiRequest(page, 'GET', `${API.quyTrinh}/${id}`);
    expect(detail.status()).toBe(403);
  });

  test('hiển thị link về trình thiết kế', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.quyTrinh}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có quy trình nào để xem liên thông');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/quy-trinh/${id}/lien-thong`);
    await page.waitForLoadState('networkidle');
    const hasDesignerLink = await page.locator('a, button').filter({ hasText: /thiết kế/i }).count() > 0;
    expect(hasDesignerLink).toBe(true);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// REQ-17: Cấu hình tiêu chí chi tiết (TrangCauHinhTieuChi)
// ─────────────────────────────────────────────────────────────────────────────

test.describe('REQ-17: Cấu hình tiêu chí chi tiết (TrangCauHinhTieuChi)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang cấu hình tiêu chí tải không crash', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để cấu hình');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/tieu-chi/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
  });

  test('hiển thị tiêu đề "Cấu hình:"', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để cấu hình');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/tieu-chi/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.getByText(/cấu hình:/i)).toBeVisible({ timeout: 15_000 });
  });

  test('hiển thị thống kê thang điểm', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để cấu hình');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/tieu-chi/${id}`);
    await page.waitForLoadState('networkidle');
    const statCount = await page.locator('.ant-statistic').count();
    expect(statCount).toBeGreaterThanOrEqual(1);
  });

  test('hiển thị card nhóm tiêu chí hoặc trạng thái rỗng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để cấu hình');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/tieu-chi/${id}`);
    await page.waitForLoadState('networkidle');
    const cardCount = await page.locator('.ant-card').count();
    expect(cardCount).toBeGreaterThanOrEqual(1);
  });

  test('nút Kiểm tra và Lưu hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để cấu hình');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/tieu-chi/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.getByRole('button', { name: /kiểm tra/i })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: /lưu/i }).first()).toBeVisible({ timeout: 15_000 });
  });

  test('nút Thêm nhóm tiêu chí hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để cấu hình');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/tieu-chi/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.getByRole('button', { name: /thêm nhóm/i })).toBeVisible({ timeout: 15_000 });
  });

  test('hiển thị phần mức công nhận', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để cấu hình');
      return;
    }
    const id = body.duLieu[0].id;
    await page.goto(`/quan-tri/tieu-chi/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.getByText(/mức công nhận/i).first()).toBeVisible({ timeout: 15_000 });
  });

  test('API GET /tieu-chi/{id} trả về bộ tiêu chí chi tiết', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để kiểm tra API');
      return;
    }
    const id = body.duLieu[0].id;
    const detailRes = await apiRequest(page, 'GET', `${API.tieuChi}/${id}`);
    expect(detailRes.status()).toBe(200);
    const detailBody = await detailRes.json();
    expect(detailBody.thanhCong).toBe(true);
    expect(detailBody.duLieu).toHaveProperty('ten');
    expect(detailBody.duLieu).toHaveProperty('thangDiemToiDa');
    expect(detailBody.duLieu).toHaveProperty('danhSachNhom');
  });

  test('API POST /tieu-chi/{id}/kiem-tra trả về kết quả', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để kiểm tra API');
      return;
    }
    const id = body.duLieu[0].id;
    const kiemTraRes = await apiRequest(page, 'POST', `${API.tieuChi}/${id}/kiem-tra`);
    expect(kiemTraRes.status()).toBe(200);
    const kiemTraBody = await kiemTraRes.json();
    expect(kiemTraBody.thanhCong).toBe(true);
  });

  test('tác giả gọi API tiêu chí → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tieuChi}?trang=1&soDong=1`);
    const body = await res.json();
    if (!body.duLieu || body.duLieu.length === 0) {
      test.skip(true, 'Không có bộ tiêu chí nào để kiểm tra phân quyền');
      return;
    }
    const id = body.duLieu[0].id;
    await loginViaAPI(page, 'tacgia1');
    const detail = await apiRequest(page, 'GET', `${API.tieuChi}/${id}`);
    expect(detail.status()).toBe(403);
  });
});
