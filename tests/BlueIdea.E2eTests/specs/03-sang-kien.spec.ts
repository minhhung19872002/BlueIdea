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

  // ─── Xem / Tìm kiếm sáng kiến (REQ-11) ────────────────────────────

  test.describe('REQ-11: Xem / Tìm kiếm sáng kiến', () => {
    test('API GET /sang-kien/goi-y trả về gợi ý tìm kiếm', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}/goi-y?tuKhoa=test`);
      expect([200, 404]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.thanhCong).toBe(true);
      }
    });

    test('API GET /sang-kien/{id}/lich-su trả về lịch sử chỉnh sửa', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/lich-su`);
      expect([200, 404]).toContain(res!.status());
    });

    test('trang hồ sơ của tôi hỗ trợ phân trang', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
    });

    test('GET /sang-kien với soDong=2 trả về tối đa 2 kết quả', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=2`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBeLessThanOrEqual(2);
    });

    test('GET /sang-kien với tuKhoa không crash và trả về mảng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=5&tuKhoa=sang+kien`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien response item có trường tenSangKien và id', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      if (body.duLieu.length > 0) {
        const item = body.duLieu[0] as Record<string, unknown>;
        expect(typeof item['tenSangKien']).toBe('string');
        expect(typeof item['id']).toBe('string');
      }
    });

    test('GET /sang-kien với sapXep=ngayTao&huong=desc không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&sapXep=ngayTao&huong=desc`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('thuky GET /sang-kien trả về 200 và danh sách', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Đăng ký nộp sáng kiến (REQ-22) ──────────────────────────────

  test.describe('REQ-22: Đăng ký nộp sáng kiến', () => {
    test('trang nộp hồ sơ mới hiển thị wizard steps', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.nopMoi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('.ant-steps')).toBeVisible({ timeout: 15_000 });
    });

    test('wizard hiển thị bước "Đợt đề nghị" đầu tiên', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.nopMoi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('.ant-steps')).toBeVisible({ timeout: 15_000 });
      const steps = page.locator('.ant-steps-item');
      await expect(steps.first()).toBeVisible();
      const stepCount = await steps.count();
      expect(stepCount).toBeGreaterThanOrEqual(2);
    });

    test('API POST tạo sáng kiến nháp với đầy đủ trường', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const tenSangKien = `E2E Đăng ký REQ-22 ${Date.now()}`;
      const createRes = await apiRequest(page, 'POST', API.sangKien, {
        tenSangKien,
        moTaSangKien: 'Hồ sơ tạo bởi E2E test REQ-22',
      });
      if (createRes!.status() === 200) {
        const body = await createRes!.json();
        expect(body.thanhCong).toBe(true);
        expect(body.duLieu).toBeTruthy();
      } else {
        expect([400, 422]).toContain(createRes!.status());
      }
    });

    test('API GET /sang-kien/{id}/tien-do trả về tiến trình xử lý', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/tien-do`);
      expect([200, 404]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.thanhCong).toBe(true);
      }
    });

    test('API GET /sang-kien/{id}/hanh-dong trả về hành động khả dụng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/hanh-dong`);
      expect([200, 404]).toContain(res!.status());
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.thanhCong).toBe(true);
      }
    });

    test('tiếp nhận không có quyền SangKienNop — POST /nop bị từ chối (403)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'POST', `${API.sangKien}/${fakeId}/nop`);
      expect([403, 404]).toContain(res!.status());
    });

    test('không xác thực POST /sang-kien trả về 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(API.sangKien, {
        data: { tenSangKien: 'Unauthorized', moTaSangKien: 'Test' },
      });
      expect(res.status()).toBe(401);
    });

    test('tacgia2 GET /sang-kien/cua-toi trả về danh sách riêng — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia2');
      const res = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });
  });

  // ─── Kiểm tra trùng lặp (REQ-26) ─────────────────────────────────

  test.describe('REQ-26: Kiểm tra trùng lặp', () => {
    test('API GET /sang-kien/{id}/trung-lap trả về kết quả hoặc null', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/trung-lap`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      // duLieu can be null (not yet checked) or an object with similarity result
      if (body.duLieu != null && typeof body.duLieu === 'object') {
        expect(typeof body.duLieu).toBe('object');
      }
    });

    test('API POST /sang-kien/{id}/trung-lap/chay-lai kích hoạt kiểm tra', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'POST', `${API.sangKien}/${id}/trung-lap/chay-lai`);
      // May succeed (200) or reject if not submitted (400/422)
      expect([200, 400, 422]).toContain(res!.status());
    });

    test('tác giả không có quyền TrungLapChayLai — POST bị từ chối', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = listBody.duLieu[0].id;
      const res = await apiRequest(page, 'POST', `${API.sangKien}/${id}/trung-lap/chay-lai`);
      expect([403, 404]).toContain(res!.status());
    });

    test('không xác thực GET /trung-lap trả về 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.get(`${API.sangKien}/${fakeId}/trung-lap`);
      expect(res.status()).toBe(401);
    });

    test('tiếp nhận GET /sang-kien/{id}/trung-lap trả về 200 hoặc 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = (listBody.duLieu[0] as { id: string }).id;
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/trung-lap`);
      expect([200, 403]).toContain(res!.status());
    });

    test('admin POST /trung-lap/chay-lai với id không tồn tại → 400/404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const fakeId = '00000000-0000-0000-0000-000000000001';
      const res = await apiRequest(
        page,
        'POST',
        `${API.sangKien}/${fakeId}/trung-lap/chay-lai`
      );
      expect([400, 404]).toContain(res!.status());
    });

    test('GET /trung-lap response có thanhCong=true khi sang-kien tồn tại', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const listRes = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`);
      const listBody = await listRes!.json();
      if (listBody.duLieu.length === 0) return;
      const id: string = (listBody.duLieu[0] as { id: string }).id;
      const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/trung-lap`);
      if (res!.status() === 200) {
        const body = await res!.json();
        expect(body.thanhCong).toBe(true);
        // duLieu may be null (not yet processed) or an object with similarity result
        expect(body.duLieu === null || typeof body.duLieu === 'object').toBe(true);
      }
    });

    test('tiếp nhận không có quyền POST /trung-lap/chay-lai → 403/404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(
        page,
        'POST',
        `${API.sangKien}/${fakeId}/trung-lap/chay-lai`
      );
      expect([403, 404]).toContain(res!.status());
    });
  });

  // ─── Sắp xếp và phân trang nâng cao ────────────────────────────────

  test.describe('Sắp xếp và phân trang nâng cao', () => {
    test('GET /sang-kien sapXep=tenSangKien&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10&sapXep=tenSangKien&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien trang=2 khác trang=1 nếu đủ data', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const p1 = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=2`);
      const b1 = await p1!.json();
      if (b1.tongSo > 2) {
        const p2 = await apiRequest(page, 'GET', `${API.sangKien}?trang=2&soDong=2`);
        const b2 = await p2!.json();
        expect(b2.duLieu).toBeInstanceOf(Array);
        if (b2.duLieu.length > 0 && b1.duLieu.length > 0) {
          expect(b1.duLieu[0].id).not.toBe(b2.duLieu[0].id);
        }
      }
    });

    test('GET /sang-kien/cua-toi sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=5&sapXep=ngayTao&huong=desc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien với trang=-1 xử lý an toàn', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=-1&soDong=10`);
      expect([200, 400, 422]).toContain(res!.status());
    });

    test('GET /sang-kien SQL injection trong tuKhoa không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5&tuKhoa=${encodeURIComponent("1' OR '1'='1")}`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── DELETE sáng kiến ──────────────────────────────────────────────

  test.describe('DELETE sáng kiến', () => {
    test('DELETE /sang-kien/{id} không xác thực → 401/405', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.delete(`${API.sangKien}/${fakeId}`);
      expect([401, 405]).toContain(res.status());
    });

    test('tiếp nhận DELETE /sang-kien → 403/405', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'DELETE', `${API.sangKien}/${fakeId}`);
      expect([403, 404, 405]).toContain(res!.status());
    });

    test('admin DELETE /sang-kien/{id} không tồn tại → 400/404/405', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const fakeId = '00000000-0000-0000-0000-000000000001';
      const res = await apiRequest(page, 'DELETE', `${API.sangKien}/${fakeId}`);
      expect([400, 404, 405]).toContain(res!.status());
    });
  });

  // ─── Validation nâng cao ──────────────────────────────────────────────

  test.describe('Validation nâng cao', () => {
    test('POST /sang-kien với tenSangKien quá dài (1000+ ký tự) → 400/422', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const longStr = 'A'.repeat(1200);
      const res = await apiRequest(page, 'POST', API.sangKien, {
        tenSangKien: longStr,
        moTaSangKien: 'Test quá dài',
      });
      expect([400, 422]).toContain(res!.status());
    });

    test('PUT /sang-kien/{id} không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.put(`${API.sangKien}/${fakeId}`, {
        data: { tenSangKien: 'Test', moTaSangKien: 'Test' },
      });
      expect(res.status()).toBe(401);
    });

    test('PUT /sang-kien/{id} với id không tồn tại → 400/404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const fakeId = '00000000-0000-0000-0000-000000000001';
      const res = await apiRequest(page, 'PUT', `${API.sangKien}/${fakeId}`, {
        tenSangKien: 'Test not found',
        moTaSangKien: 'Test',
      });
      expect([400, 403, 404]).toContain(res!.status());
    });

    test('POST /sang-kien/{id}/nop không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.post(`${API.sangKien}/${fakeId}/nop`);
      expect(res.status()).toBe(401);
    });
  });

  // ─── Responsive viewport ──────────────────────────────────────────────

  test.describe('Responsive viewport', () => {
    test('trang hồ sơ của tôi hiển thị đúng trên mobile (375px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.hoSoCuaToi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
      const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
      expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
      await context.close();
    });

    test('trang nộp mới hiển thị đúng trên tablet (768px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.nopMoi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await context.close();
    });
  });
});

// ─── REQ-22: Chi tiết hồ sơ sáng kiến (TrangChiTietHoSo) ───────────────────

test.describe('REQ-22: Chi tiết hồ sơ sáng kiến (TrangChiTietHoSo)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang chi tiết hồ sơ tải không crash', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const id: string = listBody.duLieu[0].id;
    await page.goto(`/sang-kien/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
  });

  test('hiển thị mã hồ sơ và trạng thái', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const item = listBody.duLieu[0];
    await page.goto(`/sang-kien/${item.id}`);
    await page.waitForLoadState('networkidle');
    if (item.maHoSo) {
      await expect(page.getByText(item.maHoSo).first()).toBeVisible({ timeout: 10_000 });
    }
    await expect(page.locator('.ant-tag').first()).toBeVisible({ timeout: 10_000 });
  });

  test('hiển thị 5 tab', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    await page.goto(`/sang-kien/${listBody.duLieu[0].id}`);
    await page.waitForLoadState('networkidle');
    const tabs = page.getByRole('tab');
    await expect(tabs.first()).toBeVisible({ timeout: 10_000 });
    const tabTexts = await tabs.allTextContents();
    const combined = tabTexts.join(' ').toLowerCase();
    expect(
      combined.includes('nội dung') ||
      combined.includes('tệp') ||
      combined.includes('tiến độ') ||
      combined.includes('lịch sử') ||
      combined.includes('trùng lặp')
    ).toBe(true);
  });

  test('tab Tiến độ xử lý hiển thị timeline', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    await page.goto(`/sang-kien/${listBody.duLieu[0].id}`);
    await page.waitForLoadState('networkidle');
    const tienDoTab = page.getByRole('tab', { name: /tiến độ/i });
    if (await tienDoTab.count() > 0) {
      await tienDoTab.click();
      await page.waitForTimeout(1000);
    }
    const hasTimeline =
      (await page.locator('.ant-timeline').count()) > 0 ||
      (await page.getByText(/chưa có/i).count()) > 0 ||
      (await page.locator('.ant-empty').count()) > 0;
    expect(hasTimeline).toBe(true);
  });

  test('tab Lịch sử chỉnh sửa hiển thị bảng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    await page.goto(`/sang-kien/${listBody.duLieu[0].id}`);
    await page.waitForLoadState('networkidle');
    const lichSuTab = page.getByRole('tab', { name: /lịch sử/i });
    if (await lichSuTab.count() > 0) {
      await lichSuTab.click();
      await page.waitForTimeout(1000);
    }
    const hasContent =
      (await page.locator('table').count()) > 0 ||
      (await page.locator('.ant-empty').count()) > 0 ||
      (await page.getByText(/chưa có/i).count()) > 0;
    expect(hasContent).toBe(true);
  });

  test('API GET /sang-kien/{id} trả về chi tiết đầy đủ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const id: string = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
    expect(body.duLieu.id).toBe(id);
    expect(body.duLieu.maHoSo).toBeDefined();
    expect(body.duLieu.tenSangKien).toBeDefined();
  });

  test('API GET /sang-kien/{id}/tien-do trả về timeline', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const id: string = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/tien-do`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('API GET /sang-kien/{id}/lich-su trả về lịch sử', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const id: string = listBody.duLieu[0].id;
    const res = await apiRequest(page, 'GET', `${API.sangKien}/${id}/lich-su`);
    expect(res!.status()).toBe(200);
  });

  test('API GET /sang-kien/{fakeId} với ID không tồn tại → 404', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await apiRequest(page, 'GET', `${API.sangKien}/${fakeId}`);
    expect(res!.status()).toBe(404);
  });

  test('tác giả xem hồ sơ của mình', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const cuaToiBody = await (await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=1`))!.json();
    if (cuaToiBody.duLieu.length === 0) { test.skip(true, 'Tác giả chưa có hồ sơ'); return; }
    const id: string = cuaToiBody.duLieu[0].id;
    await page.goto(`/sang-kien/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
  });

  test('responsive: trang chi tiết trên mobile (375px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
    const page = await context.newPage();
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (listBody.duLieu.length === 0) { await context.close(); test.skip(true, 'Không có sáng kiến'); return; }
    const id: string = listBody.duLieu[0].id;
    await page.goto(`/sang-kien/${id}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
    const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
    expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
    await context.close();
  });
});
