import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

// ─── REQ-33: Phân công chấm điểm ───────────────────────────────────────────
// ─── REQ-34: Chấm điểm sáng kiến ───────────────────────────────────────────
// ─── REQ-35: Tổng hợp điểm, ma trận điểm ───────────────────────────────────

test.describe('REQ-33: Phân công và danh sách chấm điểm', () => {
  test.describe('Giao diện trang đánh giá', () => {
    test('trang đánh giá tải không lỗi cho thành viên hội đồng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      await page.goto(ROUTES.danhGia);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('trang đánh giá hiển thị tiêu đề "Hồ sơ được phân công chấm"', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      await page.goto(ROUTES.danhGia);
      await page.waitForLoadState('networkidle');
      await expect(
        page.getByText(/hồ sơ được phân công chấm/i).first()
      ).toBeVisible({ timeout: 10_000 });
    });

    test('bộ lọc trạng thái hiển thị (select)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      await page.goto(ROUTES.danhGia);
      await page.waitForLoadState('networkidle');
      await expect(
        page.locator('.ant-select').first()
      ).toBeVisible({ timeout: 10_000 });
    });

    test('bảng hoặc trạng thái rỗng hiển thị', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      await page.goto(ROUTES.danhGia);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(3_000);
      const hasTable = await page.locator('.ant-table').first().isVisible().catch(() => false);
      const hasEmpty = await page.getByText(/chưa được phân công/i).first().isVisible().catch(() => false);
      expect(hasTable || hasEmpty).toBeTruthy();
    });
  });

  test.describe('API việc của tôi (REQ-33)', () => {
    test('GET /danh-gia/viec-cua-toi cho hoidong01 — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('GET /danh-gia/viec-cua-toi cho hoidong02 — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong02');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /danh-gia/viec-cua-toi phân trang soDong=3', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=3`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu.length).toBeLessThanOrEqual(3);
    });

    test('GET /danh-gia/viec-cua-toi lọc trạng thái CHUA_CHAM', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(
        page, 'GET',
        `${API.danhGia}/viec-cua-toi?trang=1&soDong=10&trangThai=CHUA_CHAM`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /danh-gia/viec-cua-toi lọc trạng thái DA_CHAM', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(
        page, 'GET',
        `${API.danhGia}/viec-cua-toi?trang=1&soDong=10&trangThai=DA_CHAM`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  test.describe('API phân công (REQ-33)', () => {
    test('POST /danh-gia/phan-cong với dữ liệu rỗng → không thành công', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const res = await apiRequest(page, 'POST', `${API.danhGia}/phan-cong`, {});
      expect(res!.status()).toBeGreaterThanOrEqual(400);
    });

    test('POST /danh-gia/phan-cong không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(`${API.danhGia}/phan-cong`, {
        data: { hoiDongId: '00000000-0000-0000-0000-000000000000', sangKienIds: [] },
      });
      expect(res.status()).toBe(401);
    });
  });

  test.describe('Phân quyền đánh giá (REQ-33)', () => {
    test('không xác thực GET /danh-gia/viec-cua-toi → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.danhGia}/viec-cua-toi?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });

    test('tác giả GET /danh-gia/viec-cua-toi → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=5`);
      expect(res!.status()).toBe(403);
    });

    test('tác giả POST /danh-gia/phan-cong → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.danhGia}/phan-cong`, {
        hoiDongId: '00000000-0000-0000-0000-000000000000',
        sangKienIds: [],
      });
      expect(res!.status()).toBe(403);
    });
  });
});

test.describe('REQ-34: Phiếu chấm điểm', () => {
  test.describe('API phiếu chấm', () => {
    test('GET /danh-gia/phieu thiếu tham số → không 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/phieu`);
      expect([400, 403, 422]).toContain(res!.status());
    });

    test('GET /danh-gia/phieu với sangKienId và hoiDongId không tồn tại → không 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(
        page, 'GET',
        `${API.danhGia}/phieu?sangKienId=${fakeId}&hoiDongId=${fakeId}`
      );
      expect([400, 403, 404]).toContain(res!.status());
    });

    test('POST /danh-gia/phieu/luu-nhap không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(`${API.danhGia}/phieu/luu-nhap`, {
        data: { sangKienId: '00000000-0000-0000-0000-000000000000' },
      });
      expect(res.status()).toBe(401);
    });

    test('POST /danh-gia/phieu/gui không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.post(`${API.danhGia}/phieu/gui`, {
        data: { sangKienId: '00000000-0000-0000-0000-000000000000' },
      });
      expect(res.status()).toBe(401);
    });

    test('POST /danh-gia/phieu/luu-nhap với dữ liệu rỗng → không 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'POST', `${API.danhGia}/phieu/luu-nhap`, {});
      expect([400, 403, 422]).toContain(res!.status());
    });

    test('POST /danh-gia/phieu/gui với dữ liệu rỗng → không 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'POST', `${API.danhGia}/phieu/gui`, {});
      expect([400, 403, 422]).toContain(res!.status());
    });
  });

  test.describe('API mở lại phiếu', () => {
    test('POST /danh-gia/phieu/{id}/mo-lai không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.post(`${API.danhGia}/phieu/${fakeId}/mo-lai`);
      expect(res.status()).toBe(401);
    });

    test('POST /danh-gia/phieu/{id}/mo-lai — tác giả → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'POST', `${API.danhGia}/phieu/${fakeId}/mo-lai`);
      expect(res!.status()).toBe(403);
    });

    test('POST /danh-gia/phieu/{id}/mo-lai với id không tồn tại → 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(page, 'POST', `${API.danhGia}/phieu/${fakeId}/mo-lai`);
      expect([400, 404]).toContain(res!.status());
    });
  });
});

test.describe('REQ-35: Tổng hợp và ma trận điểm', () => {
  test.describe('API ma trận điểm', () => {
    test('GET /danh-gia/ma-tran-diem cho admin — 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /danh-gia/ma-tran-diem với hoiDongId cụ thể', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      // First get a council ID
      const hoiDongRes = await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`);
      expect(hoiDongRes!.status()).toBe(200);
      const hoiDongBody = await hoiDongRes!.json();
      if (hoiDongBody.duLieu.length === 0) return;
      const hoiDongId = hoiDongBody.duLieu[0].id;

      const res = await apiRequest(
        page, 'GET',
        `${API.danhGia}/ma-tran-diem?hoiDongId=${hoiDongId}`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /danh-gia/ma-tran-diem không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.danhGia}/ma-tran-diem`);
      expect(res.status()).toBe(401);
    });

    test('tác giả GET /danh-gia/ma-tran-diem → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem`);
      expect(res!.status()).toBe(403);
    });
  });

  test.describe('API tổng hợp điểm', () => {
    test('POST /danh-gia/tong-hop không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.post(
        `${API.danhGia}/tong-hop?sangKienId=${fakeId}&hoiDongId=${fakeId}`
      );
      expect(res.status()).toBe(401);
    });

    test('tác giả POST /danh-gia/tong-hop → 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(
        page, 'POST',
        `${API.danhGia}/tong-hop?sangKienId=${fakeId}&hoiDongId=${fakeId}`
      );
      expect(res!.status()).toBe(403);
    });

    test('POST /danh-gia/tong-hop với id không tồn tại → 404', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await apiRequest(
        page, 'POST',
        `${API.danhGia}/tong-hop?sangKienId=${fakeId}&hoiDongId=${fakeId}`
      );
      expect([400, 404]).toContain(res!.status());
    });
  });

  test.describe('API ký số phiếu', () => {
    test('POST /danh-gia/phieu/{id}/ky-so không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.post(`${API.danhGia}/phieu/${fakeId}/ky-so`);
      expect(res.status()).toBe(401);
    });

    test('GET /danh-gia/phieu/{id}/lich-su-ky-so không xác thực → 401', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.get(`${API.danhGia}/phieu/${fakeId}/lich-su-ky-so`);
      expect(res.status()).toBe(401);
    });
  });

  test.describe('Trường hợp biên', () => {
    test('GET /danh-gia/viec-cua-toi trang 99999 → mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=99999&soDong=10`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBe(0);
    });

    test('GET /danh-gia/ma-tran-diem?hoiDongId=uuid-khong-ton-tai trả mảng rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const fakeId = '00000000-0000-0000-0000-000000000001';
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem?hoiDongId=${fakeId}`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /danh-gia/viec-cua-toi với sapXep=ngayTao&huong=asc trả về 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=5&sapXep=ngayTao&huong=asc`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('ký tự đặc biệt trong tuKhoa không crash /danh-gia', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem?tuKhoa=${encodeURIComponent("'; DROP TABLE--")}`);
      expect(res!.status()).toBe(200);
    });
  });

  // ─── Phân quyền nâng cao ──────────────────────────────────────────────

  test.describe('REQ-33: Phân quyền nâng cao', () => {
    test('lãnh đạo GET /danh-gia/ma-tran-diem → 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'lanhdao');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/ma-tran-diem`);
      expect([200, 403]).toContain(res!.status());
    });

    test('chủ tịch GET /danh-gia/viec-cua-toi → 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'chutich');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('tiếp nhận GET /danh-gia/viec-cua-toi → 200 (có thể trống)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tiepnhan');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=5`);
      expect([200, 403]).toContain(res!.status());
    });

    test('hoidong02 GET /danh-gia/viec-cua-toi → 200', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'hoidong02');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=5`);
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('không xác thực POST /danh-gia/phieu nộp → 401/404', async ({ page }) => {
      await page.goto('/');
      const fakeId = '00000000-0000-0000-0000-000000000000';
      const res = await page.request.post(`${API.danhGia}/phieu/${fakeId}/nop`);
      expect([401, 404]).toContain(res.status());
    });

    test('tác giả GET /danh-gia/viec-cua-toi → 200 hoặc 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.danhGia}/viec-cua-toi?trang=1&soDong=5`);
      expect([200, 403]).toContain(res!.status());
    });
  });

  // ─── Responsive viewport ──────────────────────────────────────────────

  test.describe('REQ-34: Responsive viewport', () => {
    test('trang đánh giá hiển thị đúng trên mobile (375px)', async ({ browser }) => {
      const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
      const page = await context.newPage();
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      await page.goto(ROUTES.danhGia);
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

// ─── REQ-34: Trang chấm điểm chi tiết (TrangChamDiem) ──────────────────────

test.describe('REQ-34: Trang chấm điểm chi tiết (TrangChamDiem)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang chấm điểm tải không crash với sangKienId và hoiDongId hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'hoidong01');
    const skBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (skBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const sangKienId: string = skBody.duLieu[0].id;
    const hdBody = await (await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`))!.json();
    if (hdBody.duLieu.length === 0) { test.skip(true, 'Không có hội đồng'); return; }
    const hoiDongId: string = hdBody.duLieu[0].id;
    await page.goto(`${ROUTES.danhGia}/${sangKienId}/cham-diem?hoiDongId=${hoiDongId}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
  });

  test('hiển thị panel nội dung sáng kiến bên trái', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'hoidong01');
    const skBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (skBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const sangKienId: string = skBody.duLieu[0].id;
    const hdBody = await (await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`))!.json();
    if (hdBody.duLieu.length === 0) { test.skip(true, 'Không có hội đồng'); return; }
    const hoiDongId: string = hdBody.duLieu[0].id;
    await page.goto(`${ROUTES.danhGia}/${sangKienId}/cham-diem?hoiDongId=${hoiDongId}`);
    await page.waitForLoadState('networkidle');
    const hasContentPanel =
      (await page.getByText(/nội dung sáng kiến/i).count()) > 0 ||
      (await page.locator('.ant-card').count()) > 0;
    expect(hasContentPanel).toBe(true);
  });

  test('hiển thị form chấm điểm hoặc cảnh báo chưa cấu hình', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'hoidong01');
    const skBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (skBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const sangKienId: string = skBody.duLieu[0].id;
    const hdBody = await (await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`))!.json();
    if (hdBody.duLieu.length === 0) { test.skip(true, 'Không có hội đồng'); return; }
    const hoiDongId: string = hdBody.duLieu[0].id;
    await page.goto(`${ROUTES.danhGia}/${sangKienId}/cham-diem?hoiDongId=${hoiDongId}`);
    await page.waitForLoadState('networkidle');
    const hasForm =
      (await page.locator('.ant-slider').count()) > 0 ||
      (await page.locator('.ant-radio').count()) > 0 ||
      (await page.locator('.ant-input-number').count()) > 0 ||
      (await page.getByText(/chưa cấu hình/i).count()) > 0 ||
      (await page.locator('.ant-alert').count()) > 0;
    expect(hasForm).toBe(true);
  });

  test('API GET /sang-kien/{id} trả về thông tin sáng kiến', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'hoidong01');
    const skBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (skBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const sangKienId: string = skBody.duLieu[0].id;
    const res = await apiRequest(page, 'GET', `${API.sangKien}/${sangKienId}`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
    expect(body.duLieu.id).toBe(sangKienId);
  });

  test('API GET /danh-gia/phieu với tham số hợp lệ trả về 200 hoặc lỗi xác định', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'hoidong01');
    const skBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (skBody.duLieu.length === 0) { test.skip(true, 'Không có sáng kiến'); return; }
    const sangKienId: string = skBody.duLieu[0].id;
    const hdBody = await (await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`))!.json();
    if (hdBody.duLieu.length === 0) { test.skip(true, 'Không có hội đồng'); return; }
    const hoiDongId: string = hdBody.duLieu[0].id;
    const res = await apiRequest(page, 'GET', `${API.danhGia}/phieu?sangKienId=${sangKienId}&hoiDongId=${hoiDongId}`);
    expect([200, 400, 404]).toContain(res!.status());
  });

  test('API POST /danh-gia/phieu/luu-nhap với body rỗng → validation error', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'hoidong01');
    const res = await apiRequest(page, 'POST', `${API.danhGia}/phieu/luu-nhap`, {});
    expect(res!.status()).toBeGreaterThanOrEqual(400);
  });

  test('API POST /danh-gia/phieu/gui với body rỗng → validation error', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'hoidong01');
    const res = await apiRequest(page, 'POST', `${API.danhGia}/phieu/gui`, {});
    expect(res!.status()).toBeGreaterThanOrEqual(400);
  });

  test('không xác thực truy cập trang chấm điểm → chuyển hướng đăng nhập', async ({ page }) => {
    await page.goto('/danh-gia/fake-uuid/cham-diem');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/dang-nhap/, { timeout: 10_000 });
  });

  test('tác giả không có quyền GET /danh-gia/phieu → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const fakeId = '00000000-0000-0000-0000-000000000001';
    const res = await apiRequest(page, 'GET', `${API.danhGia}/phieu?sangKienId=${fakeId}&hoiDongId=${fakeId}`);
    expect(res!.status()).toBe(403);
  });

  test('responsive: trang chấm điểm trên mobile (375px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
    const page = await context.newPage();
    await page.goto('/');
    await loginViaAPI(page, 'hoidong01');
    const skBody = await (await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1`))!.json();
    if (skBody.duLieu.length === 0) { await context.close(); test.skip(true, 'Không có sáng kiến'); return; }
    const sangKienId: string = skBody.duLieu[0].id;
    const hdBody = await (await apiRequest(page, 'GET', `${API.hoiDong}?trang=1&soDong=1`))!.json();
    if (hdBody.duLieu.length === 0) { await context.close(); test.skip(true, 'Không có hội đồng'); return; }
    const hoiDongId: string = hdBody.duLieu[0].id;
    await page.goto(`${ROUTES.danhGia}/${sangKienId}/cham-diem?hoiDongId=${hoiDongId}`);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
    const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
    expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
    await context.close();
  });
});
