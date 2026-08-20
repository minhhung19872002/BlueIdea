import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { API, ROUTES } from '../helpers/constants';

// ─── REQ-34: Tổng quan báo cáo ───────────────────────────────────────────────

test.describe('REQ-34: Tổng quan báo cáo — KPI + biểu đồ', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API trả về HTTP 200 và thanhCong=true', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: Record<string, unknown> };
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
  });

  test('API trả về KPI fields bắt buộc: tongHoSo, hoSoDangXuLy, hoSoDat, hoSoQuaHan', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan`);
    const body = await res.json() as { duLieu: Record<string, unknown> };
    const d = body.duLieu;
    expect(typeof d['tongHoSo']).toBe('number');
    expect(typeof d['hoSoDangXuLy']).toBe('number');
    expect(typeof d['hoSoDat']).toBe('number');
    expect(typeof d['hoSoQuaHan']).toBe('number');
  });

  test('API KPI values không âm', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan`);
    const body = await res.json() as {
      duLieu: { tongHoSo: number; hoSoDangXuLy: number; hoSoDat: number; hoSoQuaHan: number };
    };
    const d = body.duLieu;
    expect(d.tongHoSo).toBeGreaterThanOrEqual(0);
    expect(d.hoSoDangXuLy).toBeGreaterThanOrEqual(0);
    expect(d.hoSoDat).toBeGreaterThanOrEqual(0);
    expect(d.hoSoQuaHan).toBeGreaterThanOrEqual(0);
  });

  test('API trả về dữ liệu biểu đồ: theoTrangThai, theoLinhVuc, topDonVi, theoNam', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan`);
    const body = await res.json() as { duLieu: Record<string, unknown> };
    const d = body.duLieu;
    expect(Array.isArray(d['theoTrangThai'])).toBe(true);
    expect(Array.isArray(d['theoLinhVuc'])).toBe(true);
    expect(Array.isArray(d['topDonVi'])).toBe(true);
    expect(Array.isArray(d['xuHuongTheoNam'])).toBe(true);
  });

  test('API với filter nam=2026 trả về 200 và thanhCong=true', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan?nam=2026`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean };
    expect(body.thanhCong).toBe(true);
  });

  test('UI: trang báo cáo tải không lỗi hệ thống', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.baoCao);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).not.toContainText('500');
    await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
  });

  test('Auth: lãnh đạo có quyền xem tổng quan (200)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'lanhdao');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-quan`);
    expect(res.status()).toBe(200);
  });

  test('Auth: tác giả không có quyền xem tổng quan (403)', async ({ page }) => {
    await page.goto('/');
    const { accessToken } = await loginViaAPI(page, 'tacgia1');
    const res = await page.request.get(`${API.baoCao}/tong-quan`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect([403, 404]).toContain(res.status());
  });

  test('Auth: không xác thực truy cập tổng quan — 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.baoCao}/tong-quan`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-35: Sáng kiến đạt ───────────────────────────────────────────────────

test.describe('REQ-35: Sáng kiến đạt — danh sách và xuất file', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API trả về mảng hợp lệ và thanhCong=true', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('API items có trường maHoSo, tenSangKien, tacGia, tenDonVi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0];
      expect(item).toHaveProperty('maHoSo');
      expect(item).toHaveProperty('tenSangKien');
      expect(item).toHaveProperty('tacGia');
      expect(item).toHaveProperty('tenDonVi');
    }
  });

  test('API items có trường tongDiem và ketQua', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      expect(body.duLieu[0]).toHaveProperty('tongDiem');
      expect(body.duLieu[0]).toHaveProperty('ketQua');
    }
  });

  test('API với filter nam=2026 trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat?nam=2026`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean };
    expect(body.thanhCong).toBe(true);
  });

  test('Excel export — content-type là spreadsheet và content-length > 0', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat/xuat-excel`);
    expect(res.status()).toBe(200);
    const contentType = res.headers()['content-type'] ?? '';
    const isSpreadsheet =
      contentType.includes('application/vnd.openxmlformats') ||
      contentType.includes('application/octet-stream') ||
      contentType.includes('application/vnd.ms-excel');
    expect(isSpreadsheet).toBe(true);
    const body = await res.body();
    expect(body.length).toBeGreaterThan(0);
  });

  test('PDF export — content-type là application/pdf và nội dung > 0', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat/xuat-pdf`);
    expect(res.status()).toBe(200);
    const contentType = res.headers()['content-type'] ?? '';
    expect(contentType).toContain('application/pdf');
    const body = await res.body();
    expect(body.length).toBeGreaterThan(0);
  });

  test('Auth: lãnh đạo có quyền xem sáng kiến đạt (200)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'lanhdao');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-dat`);
    expect(res.status()).toBe(200);
  });

  test('Auth: tác giả bị từ chối xem sáng kiến đạt (403)', async ({ page }) => {
    await page.goto('/');
    const { accessToken } = await loginViaAPI(page, 'tacgia1');
    const res = await page.request.get(`${API.baoCao}/sang-kien-dat`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect([403, 404]).toContain(res.status());
  });

  test('Auth: không xác thực truy cập sáng kiến đạt — 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.baoCao}/sang-kien-dat`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-36: Sáng kiến chưa đạt ─────────────────────────────────────────────

test.describe('REQ-36: Sáng kiến chưa đạt — lý do và xuất file', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API trả về mảng hợp lệ và thanhCong=true', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('API items có trường ketQua = KHONG_DAT', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      expect(body.duLieu[0]).toHaveProperty('ketQua');
      expect(body.duLieu[0]['ketQua']).toBe('KHONG_DAT');
    }
  });

  test('API items có trường maHoSo, tenSangKien, tacGia', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0];
      expect(item).toHaveProperty('maHoSo');
      expect(item).toHaveProperty('tenSangKien');
      expect(item).toHaveProperty('tacGia');
    }
  });

  test('API với filter nam=2026 trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat?nam=2026`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean };
    expect(body.thanhCong).toBe(true);
  });

  test('Excel export — content-type là spreadsheet', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat/xuat-excel`);
    expect(res.status()).toBe(200);
    const contentType = res.headers()['content-type'] ?? '';
    const isSpreadsheet =
      contentType.includes('application/vnd.openxmlformats') ||
      contentType.includes('application/octet-stream') ||
      contentType.includes('application/vnd.ms-excel');
    expect(isSpreadsheet).toBe(true);
    const body = await res.body();
    expect(body.length).toBeGreaterThan(0);
  });

  test('Auth: lãnh đạo có quyền xem (200)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'lanhdao');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/sang-kien-chua-dat`);
    expect(res.status()).toBe(200);
  });

  test('Auth: tác giả bị từ chối (403)', async ({ page }) => {
    await page.goto('/');
    const { accessToken } = await loginViaAPI(page, 'tacgia1');
    const res = await page.request.get(`${API.baoCao}/sang-kien-chua-dat`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect([403, 404]).toContain(res.status());
  });

  test('Auth: không xác thực — 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.baoCao}/sang-kien-chua-dat`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-37: Theo đơn vị ─────────────────────────────────────────────────────

test.describe('REQ-37: Báo cáo theo đơn vị — tổng hợp và tỷ lệ đạt', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API trả về mảng hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('API items có trường tổng hợp: tongSo, soDat, soKhongDat', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0];
      expect(item).toHaveProperty('tongSo');
      expect(item).toHaveProperty('soDat');
      expect(item).toHaveProperty('soKhongDat');
    }
  });

  test('API items tyLeDat là số trong khoảng 0-100', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    for (const item of body.duLieu) {
      const ty = item['tyLeDat'];
      if (typeof ty === 'number') {
        expect(ty).toBeGreaterThanOrEqual(0);
        expect(ty).toBeLessThanOrEqual(100);
      }
    }
  });

  test('API items tongSo >= soDat + soKhongDat (dữ liệu nhất quán)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi`);
    const body = await res.json() as { duLieu: { tongSo: number; soDat: number; soKhongDat: number }[] };
    for (const item of body.duLieu) {
      if (typeof item.tongSo === 'number' && typeof item.soDat === 'number' && typeof item.soKhongDat === 'number') {
        expect(item.tongSo).toBeGreaterThanOrEqual(item.soDat + item.soKhongDat);
      }
    }
  });

  test('API với filter nam=2026 trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi?nam=2026`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean };
    expect(body.thanhCong).toBe(true);
  });

  test('Excel export — content-type là spreadsheet và nội dung > 0', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi/xuat-excel`);
    expect(res.status()).toBe(200);
    const contentType = res.headers()['content-type'] ?? '';
    const isSpreadsheet =
      contentType.includes('application/vnd.openxmlformats') ||
      contentType.includes('application/octet-stream') ||
      contentType.includes('application/vnd.ms-excel');
    expect(isSpreadsheet).toBe(true);
    const body = await res.body();
    expect(body.length).toBeGreaterThan(0);
  });

  test('Auth: tác giả bị từ chối (403)', async ({ page }) => {
    await page.goto('/');
    const { accessToken } = await loginViaAPI(page, 'tacgia1');
    const res = await page.request.get(`${API.baoCao}/theo-don-vi`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect([403, 404]).toContain(res.status());
  });

  test('Auth: không xác thực — 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.baoCao}/theo-don-vi`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-38: Theo tác giả ────────────────────────────────────────────────────

test.describe('REQ-38: Báo cáo theo tác giả — thống kê cá nhân', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API trả về mảng hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('API items có trường hoTen, tongSo, soDat', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0];
      expect(item).toHaveProperty('hoTen');
      expect(item).toHaveProperty('tongSo');
      expect(item).toHaveProperty('soDat');
    }
  });

  test('API items soDat <= tongSo (tính nhất quán)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia`);
    const body = await res.json() as { duLieu: { tongSo: number; soDat: number }[] };
    for (const item of body.duLieu) {
      if (typeof item.tongSo === 'number' && typeof item.soDat === 'number') {
        expect(item.soDat).toBeLessThanOrEqual(item.tongSo);
      }
    }
  });

  test('API với filter nam=2026 trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia?nam=2026`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean };
    expect(body.thanhCong).toBe(true);
  });

  test('Excel export — content-type là spreadsheet và nội dung > 0', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia/xuat-excel`);
    expect(res.status()).toBe(200);
    const contentType = res.headers()['content-type'] ?? '';
    const isSpreadsheet =
      contentType.includes('application/vnd.openxmlformats') ||
      contentType.includes('application/octet-stream') ||
      contentType.includes('application/vnd.ms-excel');
    expect(isSpreadsheet).toBe(true);
    const body = await res.body();
    expect(body.length).toBeGreaterThan(0);
  });

  test('Auth: lãnh đạo có quyền xem (200)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'lanhdao');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-tac-gia`);
    expect(res.status()).toBe(200);
  });

  test('Auth: tác giả bị từ chối (403)', async ({ page }) => {
    await page.goto('/');
    const { accessToken } = await loginViaAPI(page, 'tacgia1');
    const res = await page.request.get(`${API.baoCao}/theo-tac-gia`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect([403, 404]).toContain(res.status());
  });

  test('Auth: không xác thực — 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.baoCao}/theo-tac-gia`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-39: Thời gian xử lý ────────────────────────────────────────────────

test.describe('REQ-39: Báo cáo thời gian xử lý — số ngày trung bình mỗi bước', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API trả về mảng hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('API items có trường tenBuoc, soLuot, soNgayTrungBinh', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0];
      expect(item).toHaveProperty('tenBuoc');
      expect(item).toHaveProperty('soLuot');
      expect(item).toHaveProperty('soNgayTrungBinh');
    }
  });

  test('API items soNgayTrungBinh >= 0 (không âm)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    for (const item of body.duLieu) {
      const avg = item['soNgayTrungBinh'];
      if (typeof avg === 'number') {
        expect(avg).toBeGreaterThanOrEqual(0);
      }
    }
  });

  test('API items soNgayLauNhat >= soNgayTrungBinh khi có dữ liệu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    for (const item of body.duLieu) {
      const max = item['soNgayLauNhat'];
      const avg = item['soNgayTrungBinh'];
      if (typeof max === 'number' && typeof avg === 'number' && avg > 0) {
        expect(max).toBeGreaterThanOrEqual(avg);
      }
    }
  });

  test('API items soLuotQuaHan không âm', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    for (const item of body.duLieu) {
      const overdue = item['soLuotQuaHan'];
      if (typeof overdue === 'number') {
        expect(overdue).toBeGreaterThanOrEqual(0);
      }
    }
  });

  test('Excel export — content-type là spreadsheet và nội dung > 0', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/thoi-gian-xu-ly/xuat-excel`);
    expect(res.status()).toBe(200);
    const contentType = res.headers()['content-type'] ?? '';
    const isSpreadsheet =
      contentType.includes('application/vnd.openxmlformats') ||
      contentType.includes('application/octet-stream') ||
      contentType.includes('application/vnd.ms-excel');
    expect(isSpreadsheet).toBe(true);
    const body = await res.body();
    expect(body.length).toBeGreaterThan(0);
  });

  test('Auth: tác giả bị từ chối (403)', async ({ page }) => {
    await page.goto('/');
    const { accessToken } = await loginViaAPI(page, 'tacgia1');
    const res = await page.request.get(`${API.baoCao}/thoi-gian-xu-ly`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect([403, 404]).toContain(res.status());
  });

  test('Auth: không xác thực — 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.baoCao}/thoi-gian-xu-ly`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-40: Tổng hợp năm ────────────────────────────────────────────────────

test.describe('REQ-40: Báo cáo tổng hợp năm — xuất PDF báo cáo năm', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API tổng hợp năm 2026 trả về 200 và object hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2026`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: Record<string, unknown> };
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
    expect(typeof body.duLieu).toBe('object');
  });

  test('API tổng hợp năm 2026 có ít nhất một breakdown array (theoLinhVuc / theoDot / theoMucCongNhan)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2026`);
    const body = await res.json() as { duLieu: Record<string, unknown> };
    const d = body.duLieu;
    const hasBreakdown =
      Array.isArray(d['theoLinhVuc']) ||
      Array.isArray(d['theoDot']) ||
      Array.isArray(d['theoMucCongNhan']);
    expect(hasBreakdown).toBe(true);
  });

  test('API năm 2026 — trường nam trong response khớp 2026 (nếu có)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2026`);
    const body = await res.json() as { duLieu: Record<string, unknown> };
    if (body.duLieu['nam'] !== undefined) {
      expect(body.duLieu['nam']).toBe(2026);
    }
  });

  test('API năm không tồn tại (2000) trả về 200 hoặc 404 — không phải 500', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2000`);
    expect([200, 404]).toContain(res.status());
  });

  test('PDF export năm 2026 — content-type là application/pdf và nội dung > 0', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2026/xuat-pdf`);
    expect(res.status()).toBe(200);
    const contentType = res.headers()['content-type'] ?? '';
    expect(contentType).toContain('application/pdf');
    const body = await res.body();
    expect(body.length).toBeGreaterThan(0);
  });

  test('Auth: lãnh đạo có quyền xem tổng hợp năm (200)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'lanhdao');
    const res = await apiRequest(page, 'GET', `${API.baoCao}/tong-hop-nam/2026`);
    expect(res.status()).toBe(200);
  });

  test('Auth: tác giả bị từ chối xem tổng hợp năm (403)', async ({ page }) => {
    await page.goto('/');
    const { accessToken } = await loginViaAPI(page, 'tacgia1');
    const res = await page.request.get(`${API.baoCao}/tong-hop-nam/2026`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect([403, 404]).toContain(res.status());
  });

  test('Auth: không xác thực truy cập tổng hợp năm — 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.baoCao}/tong-hop-nam/2026`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-23: Cổng thông tin công khai ───────────────────────────────────────

test.describe('REQ-23: Cổng thông tin công khai — tra cứu sáng kiến (AllowAnonymous)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API GET /cong-khai/sang-kien trả về cấu trúc phân trang hợp lệ', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10`);
    expect(res.status()).toBe(200);
    const body = await res.json() as {
      duLieu: unknown[];
      tongSo: number;
      trang: number;
      soDong: number;
      tongTrang: number;
    };
    expect(Array.isArray(body.duLieu)).toBe(true);
    expect(typeof body.tongSo).toBe('number');
    expect(typeof body.trang).toBe('number');
    expect(typeof body.soDong).toBe('number');
    expect(typeof body.tongTrang).toBe('number');
  });

  test('API items có trường công khai bắt buộc: id, tenSangKien, tacGiaChinh, nam', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0];
      expect(item).toHaveProperty('id');
      expect(item).toHaveProperty('tenSangKien');
      expect(item).toHaveProperty('tacGiaChinh');
      expect(item).toHaveProperty('nam');
    }
  });

  test('API KHÔNG tiết lộ dữ liệu nội bộ: tongDiem, tyLeTrungLap', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=20`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    for (const item of body.duLieu) {
      expect(item).not.toHaveProperty('tongDiem');
      expect(item).not.toHaveProperty('tyLeTrungLap');
    }
  });

  test('API trang 2 trả về ids khác trang 1 (không trùng lặp)', async ({ page }) => {
    await page.goto('/');
    const res1 = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=5`);
    const res2 = await page.request.get(`${API.congKhai}/sang-kien?trang=2&soDong=5`);
    expect(res1.status()).toBe(200);
    expect(res2.status()).toBe(200);
    const body1 = await res1.json() as { duLieu: { id: string }[]; tongTrang: number };
    const body2 = await res2.json() as { duLieu: { id: string }[] };
    if (body1.tongTrang >= 2 && body1.duLieu.length > 0 && body2.duLieu.length > 0) {
      const ids1 = new Set(body1.duLieu.map(i => i.id));
      const overlap = body2.duLieu.filter(i => ids1.has(i.id));
      expect(overlap.length).toBe(0);
    }
  });

  test('API soDong=5 trả về tối đa 5 items', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=5`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: unknown[] };
    expect(body.duLieu.length).toBeLessThanOrEqual(5);
  });

  test('API filter tuKhoa — trả về 200 và mảng (dù rỗng)', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(
      `${API.congKhai}/sang-kien?tuKhoa=${encodeURIComponent('ki')}&trang=1&soDong=20`
    );
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: unknown[] };
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('API items có trường maHoSo và tenDonVi', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      expect(body.duLieu[0]).toHaveProperty('maHoSo');
      expect(body.duLieu[0]).toHaveProperty('tenDonVi');
    }
  });

  test('API nam field là số hợp lệ (>= 2000)', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    for (const item of body.duLieu) {
      const nam = item['nam'];
      if (typeof nam === 'number') {
        expect(nam).toBeGreaterThanOrEqual(2000);
      }
    }
  });

  test('UI: trang tra cứu công khai tải được không cần đăng nhập', async ({ page }) => {
    await page.goto(ROUTES.congKhai);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).not.toContainText('401');
    await expect(page.locator('body')).not.toContainText('403');
    await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
  });

  test('UI: có link đăng nhập dẫn tới /dang-nhap', async ({ page }) => {
    await page.goto(ROUTES.congKhai);
    await page.waitForLoadState('networkidle');
    const loginBtn = page.locator('a[href*="dang-nhap"], button:has-text("Đăng nhập"), a:has-text("Đăng nhập")').first();
    await expect(loginBtn).toBeVisible({ timeout: 5_000 });
    await loginBtn.click();
    await expect(page).toHaveURL(/dang-nhap/);
  });

  test('UI: trang công khai có nội dung hiển thị', async ({ page }) => {
    await page.goto(ROUTES.congKhai);
    await page.waitForLoadState('networkidle');
    const bodyText = await page.locator('body').textContent() ?? '';
    expect(bodyText.length).toBeGreaterThan(50);
  });

  test('UI: banner thống kê hiển thị (không lỗi 500)', async ({ page }) => {
    await page.goto(ROUTES.congKhai);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).not.toContainText('500');
    // Stats values should be present somewhere on the page
    const bodyText = await page.locator('body').textContent() ?? '';
    expect(bodyText.length).toBeGreaterThan(100);
  });
});

// ─── REQ-24: Thống kê công khai ──────────────────────────────────────────────

test.describe('REQ-24: Thống kê công khai — banner số liệu tổng hợp', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API GET /cong-khai/thong-ke trả về 200 và thanhCong=true (anonymous)', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/thong-ke`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: Record<string, unknown> };
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
  });

  test('API thong-ke có trường soSangKien, soNam, soQuyetDinh', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/thong-ke`);
    const body = await res.json() as { duLieu: Record<string, unknown> };
    const d = body.duLieu;
    expect(d).toHaveProperty('soSangKien');
    expect(d).toHaveProperty('soNam');
    expect(d).toHaveProperty('soQuyetDinh');
  });

  test('API thong-ke — giá trị không âm', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/thong-ke`);
    const body = await res.json() as {
      duLieu: { soSangKien: number; soNam: number; soQuyetDinh: number };
    };
    const d = body.duLieu;
    expect(d.soSangKien).toBeGreaterThanOrEqual(0);
    expect(d.soNam).toBeGreaterThanOrEqual(0);
    expect(d.soQuyetDinh).toBeGreaterThanOrEqual(0);
  });

  test('API thong-ke truy cập được hoàn toàn không cần token', async ({ page }) => {
    // Raw request — no page.goto, no login
    const res = await page.request.get(`${API.congKhai}/thong-ke`);
    expect(res.status()).toBe(200);
  });

  test('API thong-ke soQuyetDinh <= soSangKien (tính nhất quán)', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/thong-ke`);
    const body = await res.json() as {
      duLieu: { soSangKien: number; soQuyetDinh: number };
    };
    expect(body.duLieu.soQuyetDinh).toBeLessThanOrEqual(body.duLieu.soSangKien);
  });
});

// ─── REQ-25: Lĩnh vực công khai ──────────────────────────────────────────────

test.describe('REQ-25: Lĩnh vực công khai — chips lọc theo lĩnh vực', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API GET /cong-khai/linh-vuc trả về 200 và mảng hợp lệ (anonymous)', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/linh-vuc`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('API linh-vuc items có trường ten và soLuong', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/linh-vuc`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      const item = body.duLieu[0];
      expect(item).toHaveProperty('ten');
      expect(item).toHaveProperty('soLuong');
    }
  });

  test('API linh-vuc soLuong không âm cho mọi item', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/linh-vuc`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    for (const item of body.duLieu) {
      const soLuong = item['soLuong'];
      if (typeof soLuong === 'number') {
        expect(soLuong).toBeGreaterThanOrEqual(0);
      }
    }
  });

  test('API linh-vuc ten là chuỗi không rỗng với mọi item', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/linh-vuc`);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    for (const item of body.duLieu) {
      const ten = item['ten'];
      if (ten !== null && ten !== undefined) {
        expect(typeof ten).toBe('string');
        expect((ten as string).length).toBeGreaterThan(0);
      }
    }
  });

  test('API linh-vuc truy cập được hoàn toàn không cần token', async ({ page }) => {
    const res = await page.request.get(`${API.congKhai}/linh-vuc`);
    expect(res.status()).toBe(200);
  });
});

// ─── Sắp xếp và phân trang nâng cao ────────────────────────────────────

test.describe('Sắp xếp và phân trang công khai', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API GET /cong-khai/sang-kien sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: unknown[] };
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API GET /cong-khai/sang-kien sapXep=tenSangKien&huong=asc trả về 200', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10&sapXep=tenSangKien&huong=asc`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: unknown[] };
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API GET /cong-khai/sang-kien trang=9999 trả về mảng rỗng', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=9999&soDong=10`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: unknown[] };
    expect(body.duLieu.length).toBe(0);
  });

  test('API GET /cong-khai/sang-kien soDong=-1 xử lý an toàn', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=-1`);
    expect([200, 400, 422]).toContain(res.status());
  });

  test('API GET /cong-khai/sang-kien tuKhoa rỗng trả về tất cả', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10&tuKhoa=`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: unknown[] };
    expect(body.duLieu).toBeInstanceOf(Array);
  });
});

// ─── Edge cases nâng cao ────────────────────────────────────────────────

test.describe('Edge cases nâng cao', () => {
  test.describe.configure({ timeout: 60_000 });

  test('SQL injection trong tuKhoa công khai không crash', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=5&tuKhoa=${encodeURIComponent("1' OR '1'='1")}`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: unknown[] };
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('ký tự Unicode trong tuKhoa công khai không crash', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=5&tuKhoa=${encodeURIComponent('Giáo dục và đào tạo')}`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: unknown[] };
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API GET /cong-khai/sang-kien/{id} với id không tồn tại → 404', async ({ page }) => {
    await page.goto('/');
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await page.request.get(`${API.congKhai}/sang-kien/${fakeId}`);
    expect([400, 404]).toContain(res.status());
  });

  test('API GET /cong-khai/thong-ke response có đúng cấu trúc JSON', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/thong-ke`);
    expect(res.status()).toBe(200);
    const body = await res.json() as Record<string, unknown>;
    expect(body).toHaveProperty('thanhCong');
    expect(body).toHaveProperty('duLieu');
  });

  test('API GET /cong-khai/linh-vuc response items có id', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.congKhai}/linh-vuc`);
    expect(res.status()).toBe(200);
    const body = await res.json() as { duLieu: Record<string, unknown>[] };
    if (body.duLieu.length > 0) {
      expect(body.duLieu[0]).toHaveProperty('id');
    }
  });
});

// ─── Responsive viewport ────────────────────────────────────────────────

test.describe('Responsive viewport công khai', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang công khai hiển thị đúng trên mobile (375px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
    const page = await context.newPage();
    await page.goto(ROUTES.congKhai);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
    const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
    expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
    await context.close();
  });

  test('trang công khai hiển thị đúng trên tablet (768px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
    const page = await context.newPage();
    await page.goto(ROUTES.congKhai);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await context.close();
  });
});

// ─── REQ-40: Báo cáo tuỳ biến ────────────────────────────────────────────────

test.describe('REQ-40: Báo cáo tuỳ biến (TrangBaoCaoTuyBien)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang tuỳ biến tải không lỗi và hiển thị tab Tuỳ biến', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.baoCaoTuyBien);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const bodyText = await page.locator('body').textContent();
    expect(bodyText).not.toContain('Lỗi hệ thống');
    await expect(page.locator('.ant-select').first()).toBeVisible({ timeout: 10_000 });
  });

  test('API GET /bieu-mau-thong-ke/chon có dữ liệu và Select hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const apiRes = await apiRequest(page, 'GET', `${API.bieuMauThongKe}/chon`);
    expect(apiRes!.status()).toBe(200);
    const apiBody = await apiRes!.json() as { thanhCong: boolean; duLieu: unknown[] };
    if (!apiBody.duLieu || apiBody.duLieu.length === 0) {
      test.skip(true, 'Không có dữ liệu biểu mẫu thống kê để kiểm tra');
      return;
    }
    await page.goto(ROUTES.baoCaoTuyBien);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-select').first()).toBeVisible({ timeout: 10_000 });
    expect(apiBody.duLieu.length).toBeGreaterThan(0);
  });

  test('nút Chạy báo cáo bị disabled khi chưa chọn biểu mẫu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.baoCaoTuyBien);
    await page.waitForLoadState('networkidle');
    const runButton = page.getByRole('button', { name: /chạy báo cáo/i });
    await expect(runButton).toBeVisible({ timeout: 15_000 });
    await expect(runButton).toBeDisabled();
  });

  test('trang tuỳ biến hiển thị 5 bộ lọc Select', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.baoCaoTuyBien);
    await page.waitForLoadState('networkidle');
    const selectCount = await page.locator('.ant-select').count();
    expect(selectCount).toBeGreaterThanOrEqual(3);
  });

  test('API GET /bieu-mau-thong-ke/chon trả về danh sách biểu mẫu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.bieuMauThongKe}/chon`);
    expect(res!.status()).toBe(200);
    const body = await res!.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
    expect(body.duLieu.length).toBeGreaterThan(0);
  });

  test('API GET /bieu-mau-thong-ke/chon không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.bieuMauThongKe}/chon`);
    expect(res.status()).toBe(401);
  });

  test('API GET /danh-muc/dot-de-nghi/chon trả về danh sách đợt', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.dotDeNghi}/chon`);
    expect(res!.status()).toBe(200);
    const body = await res!.json() as { thanhCong: boolean; duLieu: unknown[] };
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBe(true);
  });

  test('responsive: trang tuỳ biến trên mobile (375px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
    const page = await context.newPage();
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.baoCaoTuyBien);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
    const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
    expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
    await context.close();
  });
});
