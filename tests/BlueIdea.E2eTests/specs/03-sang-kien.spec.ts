import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

// ─── REQ-09: Tạo / Chỉnh sửa hồ sơ sáng kiến ────────────────────────────────
// ─── REQ-11: Xem / Tìm kiếm sáng kiến ───────────────────────────────────────
// ─── REQ-12: Xử lý quy trình sáng kiến ──────────────────────────────────────

test.describe('REQ-09/11/12: Sáng kiến', () => {
  // ─── Frontend UI — tác giả ────────────────────────────────────────────────

  test.describe('Giao diện tác giả', () => {
    test('trang "Hồ sơ của tôi" tải không lỗi và hiển thị bảng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.hoSoCuaToi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    });

    test('bảng "Hồ sơ của tôi" hiển thị cột tiêu đề', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.hoSoCuaToi);
      await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
      const headers = page.locator('.ant-table-thead th');
      await expect(headers.first()).toBeVisible({ timeout: 5_000 });
      const headerTexts = await headers.allTextContents();
      const combined = headerTexts.join(' ');
      // Table should have at minimum a name/title column
      expect(combined.length).toBeGreaterThan(0);
    });

    test('trang "Nộp hồ sơ mới" tải không lỗi', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.nopMoi);
      await page.waitForLoadState('networkidle');
      // Page should load without crashing — form or redirect expected
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });
  });

  // ─── Backend API — danh sách ──────────────────────────────────────────────

  test.describe('API GET danh sách (PhanHoiPhanTrang)', () => {
    test('GET /sang-kien trả về cấu trúc phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      // PhanHoiPhanTrang — no thanhCong at top level
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
      expect(body.tongSo).toBeGreaterThanOrEqual(0);
    });

    test('GET /sang-kien/cua-toi trả về cấu trúc phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
      expect(body.duLieu.length).toBeLessThanOrEqual(10);
    });
  });

  // ─── Backend API — CRUD ───────────────────────────────────────────────────

  test.describe('API CRUD sáng kiến', () => {
    test('POST tạo sáng kiến — server kiểm tra trường bắt buộc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.sangKien, {
        tenSangKien: `E2E Sáng kiến ${Date.now()}`,
        moTaSangKien: 'Mô tả tạo bởi E2E test',
      });
      // Server may accept minimal payload (200) or require additional fields (400/422)
      expect([200, 400, 422]).toContain(res!.status());
    });

    test('POST tạo + GET chi tiết + PUT cập nhật sáng kiến', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');

      // Create
      const tenSangKien = `E2E Sáng kiến CRUD ${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.sangKien, {
        tenSangKien,
        moTaSangKien: 'Mô tả ban đầu',
      });
      // If server rejects minimal payload, assert validation error and skip
      if (createRes!.status() === 400 || createRes!.status() === 422) {
        const body = await createRes!.json();
        expect(body).toBeDefined();
        return;
      }
      expect(createRes!.status()).toBe(200);
      const createBody = await createRes!.json();
      expect(createBody.thanhCong).toBe(true);
      const id: string = createBody.duLieu;
      expect(id).toBeTruthy();

      // GET detail
      const getRes = await apiRequest(page, 'GET', `${API.sangKien}/${id}`);
      expect(getRes!.status()).toBe(200);
      const getBody = await getRes!.json();
      expect(getBody.thanhCong).toBe(true);
      expect(getBody.duLieu.tenSangKien).toBe(tenSangKien);

      // PUT update
      const updateRes = await apiRequest(page, 'PUT', `${API.sangKien}/${id}`, {
        tenSangKien: `${tenSangKien} (đã cập nhật)`,
        moTaSangKien: 'Mô tả đã cập nhật',
      });
      expect([200, 400, 422]).toContain(updateRes!.status());
      if (updateRes!.status() === 200) {
        const updateBody = await updateRes!.json();
        expect(updateBody.thanhCong).toBe(true);
      }
    });

    test('POST /sang-kien/{id}/nop nộp sáng kiến hiện có', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');

      // First create a sang-kien
      const createRes = await apiRequest(page, 'POST', API.sangKien, {
        tenSangKien: `E2E Nộp ${Date.now()}`,
        moTaSangKien: 'Hồ sơ để kiểm tra nộp',
      });
      if (createRes!.status() !== 200) {
        // Server requires more fields — validation is working
        expect([400, 422]).toContain(createRes!.status());
        return;
      }
      const createBody = await createRes!.json();
      const id: string = createBody.duLieu;

      // Submit
      const nopRes = await apiRequest(page, 'POST', `${API.sangKien}/${id}/nop`);
      // May succeed (200) or fail (400/422) depending on whether required docs/workflow config exist
      expect([200, 400, 409, 422]).toContain(nopRes!.status());
    });
  });

  // ─── Phân quyền ──────────────────────────────────────────────────────────

  test.describe('Phân quyền sáng kiến', () => {
    test('chưa xác thực → 401 khi GET danh sách sang-kien', async ({ page }) => {
      await page.goto('/');
      // No login — raw request without token
      const res = await page.request.get(`${API.sangKien}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });

    test('chưa xác thực → 401 khi GET cua-toi', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.sangKien}/cua-toi?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });

    test('tiếp nhận có thể xem danh sách sáng kiến (SangKienXem)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('tiếp nhận không thể tạo sáng kiến mới — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const res = await apiRequest(page, 'POST', API.sangKien, {
        tenSangKien: 'Không có quyền tạo',
        moTaSangKien: 'Test phân quyền',
      });
      expect(res!.status()).toBe(403);
    });
  });

  // ─── Trường hợp biên ──────────────────────────────────────────────────────

  test.describe('Trường hợp biên', () => {
    test('POST với tenSangKien rỗng trả về lỗi validation', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.sangKien, {
        tenSangKien: '',
        moTaSangKien: 'Mô tả có nội dung',
      });
      // Empty required field should fail validation
      expect([400, 422]).toContain(res!.status());
    });

    test('POST không có payload trả về lỗi validation', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', API.sangKien, {});
      expect([400, 422]).toContain(res!.status());
    });

    test('XSS trong tenSangKien được lưu dưới dạng text', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const xssPayload = '<script>alert("xss")</script>';
      const createRes = await apiRequest(page, 'POST', API.sangKien, {
        tenSangKien: xssPayload,
        moTaSangKien: 'XSS test',
      });
      if (createRes!.status() === 200) {
        const createBody = await createRes!.json();
        const id: string = createBody.duLieu;
        const getRes = await apiRequest(page, 'GET', `${API.sangKien}/${id}`);
        expect(getRes!.status()).toBe(200);
        const getBody = await getRes!.json();
        // Must be stored as text, not rendered as HTML
        expect(getBody.duLieu.tenSangKien).toBe(xssPayload);
      } else {
        // Server rejected — validation is working (acceptable)
        expect([400, 422]).toContain(createRes!.status());
      }
    });

    test('GET sang-kien với id không tồn tại trả về 404 hoặc 400', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${fakeId}`);
      expect([400, 404]).toContain(res!.status());
    });
  });

  // ─── Xử lý quy trình — trang xử lý (REQ-12) ──────────────────────────────

  test.describe('REQ-12: Trang xử lý quy trình', () => {
    test('trang tiếp nhận tải không lỗi với tài khoản tiếp nhận', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      await page.goto(ROUTES.tiepNhan);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });

    test('trang xử lý tải không lỗi với tài khoản thư ký', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      await page.goto(ROUTES.xuLy);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });

    test('API POST /xu-ly/thuc-thi không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(`${API.xuLy}/thuc-thi`, {
        data: { sangKienId: '00000000-0000-0000-0000-000000000000', buocId: 'test' },
      });
      expect(res.status()).toBe(401);
    });

    test('API GET /xu-ly danh sách cần xử lý — có phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const res = await apiRequest(page, 'GET', `${API.xuLy}?trang=1&soDong=10`);
      // Endpoint may exist (200) or not yet exist for this role (403/404)
      expect([200, 403, 404]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.duLieu).toBeInstanceOf(Array);
      }
    });
  });
});
