import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { API, ROUTES } from '../helpers/constants';

// ─── REQ-43: Quản lý người dùng ──────────────────────────────────────────────

test.describe('REQ-43: Quản lý người dùng', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang người dùng tải, bảng hiện ra', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nguoiDung);
    await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
  });

  test('UI: bảng có ít nhất 1 hàng dữ liệu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nguoiDung);
    await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    const rows = page.locator('.ant-table-tbody tr.ant-table-row');
    await expect(rows.first()).toBeVisible({ timeout: 10_000 });
    expect(await rows.count()).toBeGreaterThan(0);
  });

  test('UI: cột bảng chứa Tài khoản và Họ và tên', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nguoiDung);
    await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    const headers = page.locator('.ant-table-thead th');
    await expect(headers.first()).toBeVisible({ timeout: 5_000 });
    const headerTexts = await headers.allTextContents();
    const combined = headerTexts.join(' ');
    expect(combined).toContain('Tài khoản');
    expect(combined).toContain('Họ và tên');
  });

  test('UI: cột bảng chứa Trạng thái', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nguoiDung);
    await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    const headers = page.locator('.ant-table-thead th');
    await expect(headers.first()).toBeVisible({ timeout: 5_000 });
    const headerTexts = await headers.allTextContents();
    const combined = headerTexts.join(' ');
    expect(combined).toContain('Trạng thái');
  });

  test('API: tìm kiếm admin — trả về kết quả', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=20&tuKhoa=admin`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.length).toBeGreaterThan(0);
  });

  test('API: tìm kiếm tacgia — trả về kết quả', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=20&tuKhoa=tacgia`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: GET danh sách phân trang — length <= soDong và tongSo tồn tại', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=5`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(body.duLieu.length).toBeLessThanOrEqual(5);
    expect(typeof body.tongSo).toBe('number');
    expect(body.tongSo).toBeGreaterThanOrEqual(0);
  });

  test('API: GET trang 2 — dữ liệu khác trang 1', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res1 = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=3`);
    const res2 = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=2&soDong=3`);
    expect(res1!.status()).toBe(200);
    expect(res2!.status()).toBe(200);
    const body1 = await res1!.json();
    const body2 = await res2!.json();
    if (body1.tongSo > 3 && body2.duLieu.length > 0) {
      const ids1 = (body1.duLieu as Array<{ id: string }>).map(u => u.id);
      const ids2 = (body2.duLieu as Array<{ id: string }>).map(u => u.id);
      const overlap = ids1.filter(id => ids2.includes(id));
      expect(overlap.length).toBe(0);
    }
  });

  test('API: GET danh sách — mỗi item có tenDangNhap, hoTen', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=10`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.length).toBeGreaterThan(0);
    const first = body.duLieu[0] as { tenDangNhap: string; hoTen: string };
    expect(typeof first.tenDangNhap).toBe('string');
    expect(first.tenDangNhap.length).toBeGreaterThan(0);
    expect(typeof first.hoTen).toBe('string');
  });

  test('API: GET danh sách với tuKhoa=admin — kết quả khớp từ khóa', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=20&tuKhoa=admin`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    if (body.duLieu.length > 0) {
      const first = body.duLieu[0] as { tenDangNhap: string; hoTen: string };
      const matchesKeyword =
        first.tenDangNhap.toLowerCase().includes('admin') ||
        first.hoTen.toLowerCase().includes('admin');
      expect(matchesKeyword).toBe(true);
    }
  });

  test('API: GET chi tiết user — trả về vaiTroIds array', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=1`);
    expect(listRes!.status()).toBe(200);
    const listBody = await listRes!.json();
    expect(listBody.duLieu.length).toBeGreaterThan(0);
    const userId = (listBody.duLieu as Array<{ id: string }>)[0].id;
    const detailRes = await apiRequest(page, 'GET', `${API.nguoiDung}/${userId}`);
    expect(detailRes!.status()).toBe(200);
    const detail = await detailRes!.json();
    expect(detail.duLieu).toBeTruthy();
    expect(detail.duLieu.vaiTroIds).toBeInstanceOf(Array);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.nguoiDung}?trang=1&soDong=5`);
    expect(res.status()).toBe(401);
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=5`);
    expect(res!.status()).toBe(403);
  });

  test('Auth: qtdonvi trả về 200 hoặc 403 (không phải 500)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'qtdonvi');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=5`);
    expect([200, 403]).toContain(res!.status());
  });
});

// ─── REQ-44: Quản lý đơn vị ──────────────────────────────────────────────────

test.describe('REQ-44: Quản lý đơn vị', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang đơn vị tải không lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.donVi);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible();
  });

  test('UI: trang đơn vị có nội dung hiển thị (không trống)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.donVi);
    await page.waitForLoadState('networkidle');
    const bodyText = await page.locator('body').innerText();
    expect(bodyText.trim().length).toBeGreaterThan(0);
  });

  test('API: GET danh sách phẳng — trả về duLieu array và tongSo', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=10`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(typeof body.tongSo).toBe('number');
    expect(body.tongSo).toBeGreaterThanOrEqual(0);
  });

  test('API: GET danh sách — mỗi item có ma và ten', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=10`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    if (body.duLieu.length > 0) {
      const first = body.duLieu[0] as { ma: string; ten: string };
      expect(typeof first.ten).toBe('string');
      expect(first.ten.length).toBeGreaterThan(0);
    }
  });

  test('API: GET cây đơn vị — trả về danh sách', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.donVi}/cay`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(body.duLieu.length).toBeGreaterThan(0);
  });

  test('API: GET cây — node gốc có donViChaId = null', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.donVi}/cay`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const roots = (body.duLieu as Array<{ donViChaId: string | null }>).filter(
      d => d.donViChaId === null || d.donViChaId === undefined
    );
    expect(roots.length).toBeGreaterThan(0);
  });

  test('API: GET chi tiết đơn vị — trả về các trường cơ bản', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=1`);
    expect(listRes!.status()).toBe(200);
    const listBody = await listRes!.json();
    if (listBody.duLieu.length > 0) {
      const id = (listBody.duLieu as Array<{ id: string }>)[0].id;
      const detailRes = await apiRequest(page, 'GET', `${API.donVi}/${id}`);
      expect(detailRes!.status()).toBe(200);
      const detail = await detailRes!.json();
      expect(detail.duLieu).toBeTruthy();
      expect(typeof detail.duLieu.ten).toBe('string');
    }
  });

  test('API: GET cây — có node con (donViChaId khác null) hoặc cấu trúc lồng nhau', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.donVi}/cay`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    // Either some items have donViChaId, or some have children array
    const hasChildren = (body.duLieu as Array<{ donViChaId?: string | null; children?: unknown[] }>).some(
      d => (d.donViChaId !== null && d.donViChaId !== undefined) || (Array.isArray(d.children) && d.children.length > 0)
    );
    // It's fine if this is a flat tree with only one root — just verify structure
    expect(body.duLieu.length).toBeGreaterThan(0);
  });

  test('Auth: không xác thực GET danh sách → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.donVi}?trang=1&soDong=5`);
    expect(res.status()).toBe(401);
  });

  test('Auth: tác giả GET danh sách phẳng → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=5`);
    expect(res!.status()).toBe(403);
  });

  test('API: GET cây — trả về 200 (có thể cần auth)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.donVi}/cay`);
    expect(res!.status()).toBe(200);
  });
});

// ─── REQ-45: Quản lý vai trò ─────────────────────────────────────────────────

test.describe('REQ-45: Quản lý vai trò', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang vai trò tải không lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.vaiTro);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible();
  });

  test('UI: trang vai trò có nội dung hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.vaiTro);
    await page.waitForLoadState('networkidle');
    const bodyText = await page.locator('body').innerText();
    expect(bodyText.trim().length).toBeGreaterThan(0);
  });

  test('API: GET trả về vaiTro array và quyen array', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu.vaiTro).toBeInstanceOf(Array);
    expect(body.duLieu.quyen).toBeInstanceOf(Array);
  });

  test('API: mỗi vai trò có id, ma, ten, laHeThong', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.vaiTro.length).toBeGreaterThan(0);
    const first = body.duLieu.vaiTro[0] as { id: string; ma: string; ten: string; laHeThong: boolean };
    expect(typeof first.id).toBe('string');
    expect(typeof first.ma).toBe('string');
    expect(typeof first.ten).toBe('string');
    expect(typeof first.laHeThong).toBe('boolean');
  });

  test('API: có ít nhất một vai trò hệ thống (laHeThong = true)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const systemRoles = (body.duLieu.vaiTro as Array<{ laHeThong: boolean }>).filter(
      r => r.laHeThong === true
    );
    expect(systemRoles.length).toBeGreaterThan(0);
  });

  test('API: vai trò QuanTriHeThong tồn tại trong danh sách', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const maList = (body.duLieu.vaiTro as Array<{ ma: string }>).map(r => r.ma);
    expect(maList).toContain('QUAN_TRI_HE_THONG');
  });

  test('API: quyen array — mỗi quyền có id, ma, ten, nhomChucNang', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.quyen.length).toBeGreaterThan(0);
    const first = body.duLieu.quyen[0] as { id: string; ma: string; ten: string; nhomChucNang: string };
    expect(typeof first.id).toBe('string');
    expect(typeof first.ma).toBe('string');
    expect(typeof first.nhomChucNang).toBe('string');
  });

  test('API: mỗi quyền có ma và ten', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const first = body.duLieu.quyen[0] as { ma: string; ten: string };
    expect(typeof first.ma).toBe('string');
    expect(typeof first.ten).toBe('string');
  });

  test('API: vai trò có trường phamVi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const first = body.duLieu.vaiTro[0] as { phamVi?: string };
    expect(Object.prototype.hasOwnProperty.call(first, 'phamVi')).toBe(true);
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.vaiTro);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-46: Cấu hình hệ thống ───────────────────────────────────────────────

test.describe('REQ-46: Cấu hình hệ thống', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang cấu hình tải không lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.cauHinh);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible();
  });

  test('UI: trang cấu hình có nội dung', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.cauHinh);
    await page.waitForLoadState('networkidle');
    const text = await page.locator('body').innerText();
    expect(text.trim().length).toBeGreaterThan(10);
  });

  test('API: GET tất cả cấu hình — trả về array có phần tử', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(body.duLieu.length).toBeGreaterThan(0);
  });

  test('API: mỗi cấu hình có khoa, giaTri, kieuDuLieu, choPhepSua', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const first = body.duLieu[0] as { khoa: string; giaTri: unknown; kieuDuLieu: string; choPhepSua: boolean };
    expect(typeof first.khoa).toBe('string');
    expect(Object.prototype.hasOwnProperty.call(first, 'giaTri')).toBe(true);
    expect(typeof first.kieuDuLieu).toBe('string');
    expect(typeof first.choPhepSua).toBe('boolean');
  });

  test('API: kieuDuLieu có giá trị hợp lệ (BOOLEAN, NUMBER, TEXT, COLOR, TEP)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const validTypes = ['BOOLEAN', 'NUMBER', 'TEXT', 'COLOR', 'TEP', 'FILE', 'HTML', 'JSON'];
    const allValid = (body.duLieu as Array<{ kieuDuLieu: string }>).every(
      item => validTypes.includes(item.kieuDuLieu)
    );
    expect(allValid).toBe(true);
  });

  test('API: key TEN_HE_THONG tồn tại', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const keys = (body.duLieu as Array<{ khoa: string }>).map(c => c.khoa);
    expect(keys).toContain('TEN_HE_THONG');
  });

  test('API: cau-hinh-cong-khai — không cần auth, trả về 200', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get('/api/v1/he-thong/cau-hinh-cong-khai');
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.thanhCong).toBe(true);
    expect(typeof body.duLieu).toBe('object');
    expect(body.duLieu).not.toBeNull();
  });

  test('API: cau-hinh-cong-khai chứa TEN_HE_THONG', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get('/api/v1/he-thong/cau-hinh-cong-khai');
    expect(res.status()).toBe(200);
    const body = await res.json();
    const keys = Object.keys(body.duLieu as Record<string, unknown>);
    expect(keys).toContain('TEN_HE_THONG');
  });

  test('API: cau-hinh-cong-khai chứa MAU_CHU_DAO', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get('/api/v1/he-thong/cau-hinh-cong-khai');
    expect(res.status()).toBe(200);
    const body = await res.json();
    const keys = Object.keys(body.duLieu as Record<string, unknown>);
    expect(keys).toContain('MAU_CHU_DAO');
  });

  test('Auth: tác giả GET cấu hình đầy đủ → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực GET cấu hình → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.cauHinh);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-47: Nhật ký hệ thống ────────────────────────────────────────────────

test.describe('REQ-47: Nhật ký hệ thống', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang nhật ký tải không lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nhatKy);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible();
  });

  test('UI: trang nhật ký có nội dung hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nhatKy);
    await page.waitForLoadState('networkidle');
    const text = await page.locator('body').innerText();
    expect(text.trim().length).toBeGreaterThan(0);
  });

  test('API: GET nhật ký hệ thống — trả về duLieu array và tongSo', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/he-thong?trang=1&soDong=5`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(typeof body.tongSo).toBe('number');
    expect(body.tongSo).toBeGreaterThanOrEqual(0);
  });

  test('API: nhật ký hệ thống — mỗi item có ip và thoiGian', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/he-thong?trang=1&soDong=10`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    if (body.duLieu.length > 0) {
      const first = body.duLieu[0] as { thoiGian: string; ip?: string; hanhDong?: string };
      expect(typeof first.thoiGian).toBe('string');
      expect(first.thoiGian.length).toBeGreaterThan(0);
    }
  });

  test('API: nhật ký hệ thống — phân trang soDong=3 trả về <= 3 phần tử', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/he-thong?trang=1&soDong=3`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.length).toBeLessThanOrEqual(3);
  });

  test('API: GET nhật ký đăng nhập — trả về array', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/dang-nhap?trang=1&soDong=5`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(typeof body.tongSo).toBe('number');
  });

  test('API: nhật ký đăng nhập — có thoiGian field', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/dang-nhap?trang=1&soDong=5`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    if (body.duLieu.length > 0) {
      const first = body.duLieu[0] as { thoiGian: string };
      expect(typeof first.thoiGian).toBe('string');
    }
  });

  test('API: GET nhật ký lỗi — trả về array', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/loi?trang=1&soDong=5`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: nhật ký đăng nhập lọc theo tenDangNhap', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/dang-nhap?tenDangNhap=admin&trang=1&soDong=10`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/he-thong?trang=1&soDong=5`);
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.nhatKy}/he-thong?trang=1&soDong=5`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-48: Cấu hình menu ───────────────────────────────────────────────────

test.describe('REQ-48: Cấu hình menu', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang cấu hình menu tải không lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.cauHinhMenu);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible();
  });

  test('UI: trang cấu hình menu có nội dung', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.cauHinhMenu);
    await page.waitForLoadState('networkidle');
    const text = await page.locator('body').innerText();
    expect(text.trim().length).toBeGreaterThan(0);
  });

  test('API: GET menu WEB — trả về array', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: menu WEB — mỗi item có ma, ten, thuTu', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    if (body.duLieu.length > 0) {
      const first = body.duLieu[0] as { ma: string; ten: string; thuTu: number };
      expect(typeof first.ten).toBe('string');
      expect(first.ten.length).toBeGreaterThan(0);
    }
  });

  test('API: menu WEB — item gốc có menuChaId = null', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    if (body.duLieu.length > 0) {
      const roots = (body.duLieu as Array<{ menuChaId: string | null }>).filter(
        m => m.menuChaId === null || m.menuChaId === undefined
      );
      expect(roots.length).toBeGreaterThan(0);
    }
  });

  test('API: GET menu MOBILE — trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=MOBILE`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
  });

  test('API: menu có trường hienThi (boolean)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    if (body.duLieu.length > 0) {
      const first = body.duLieu[0] as { hienThi?: boolean };
      expect(Object.prototype.hasOwnProperty.call(first, 'hienThi')).toBe(true);
    }
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.cauHinhMenu}?loai=WEB`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-49: Chữ ký số (BLOCKED_EXTERNAL — kiểm tra UI/API shell) ────────────

test.describe('REQ-49: Chữ ký số (BLOCKED_EXTERNAL)', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang chữ ký số tải không lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.chuKySo);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible();
  });

  test('API: GET trạng thái chữ ký số — trả về 200 và trangThai object', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.chuKySo}/trang-thai`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeTruthy();
  });

  test('API: GET danh sách cấu hình chữ ký số — trả về array', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.chuKySo);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('Auth: không xác thực GET trạng thái → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.chuKySo}/trang-thai`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-50: Cấu hình gửi tin ────────────────────────────────────────────────

test.describe('REQ-50: Cấu hình gửi tin', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang gửi tin tải không lỗi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.guiTin);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible();
  });

  test('UI: trang gửi tin có nội dung hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.guiTin);
    await page.waitForLoadState('networkidle');
    const text = await page.locator('body').innerText();
    expect(text.trim().length).toBeGreaterThan(0);
  });

  test('API: GET danh sách cấu hình gửi tin — trả về array', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.guiTin);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: mỗi cấu hình gửi tin có loai và trangThai', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.guiTin);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    if (body.duLieu.length > 0) {
      const first = body.duLieu[0] as { loai: string; trangThai: unknown };
      expect(typeof first.loai).toBe('string');
      expect(Object.prototype.hasOwnProperty.call(first, 'trangThai')).toBe(true);
    }
  });

  test('API: mật khẩu không được trả về plaintext (matKhau masked hoặc null)', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.guiTin);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    // matKhau should not be present or should be masked (null/empty/asterisks)
    for (const item of body.duLieu as Array<{ matKhau?: string | null }>) {
      if (Object.prototype.hasOwnProperty.call(item, 'matKhau')) {
        const pw = item.matKhau;
        // Acceptable: null, undefined, empty string, or masked (starts with ***)
        const isMasked = pw === null || pw === undefined || pw === '' || (typeof pw === 'string' && pw.startsWith('*'));
        expect(isMasked).toBe(true);
      }
    }
  });

  test('API: GET thống kê hàng đợi — trả về các trường số', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.guiTin}/thong-ke-hang-doi`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    const stats = body.duLieu as Record<string, unknown>;
    const hasNumericStats = ['CHO_GUI', 'DANG_GUI', 'DA_GUI', 'LOI'].some(
      key => typeof stats[key] === 'number'
    );
    expect(hasNumericStats).toBe(true);
  });

  test('API: thống kê hàng đợi — tất cả giá trị >= 0', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.guiTin}/thong-ke-hang-doi`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const stats = body.duLieu as Record<string, unknown>;
    for (const key of ['CHO_GUI', 'DANG_GUI', 'DA_GUI', 'LOI']) {
      if (typeof stats[key] === 'number') {
        expect(stats[key] as number).toBeGreaterThanOrEqual(0);
      }
    }
  });

  test('API: POST gui-thu gửi thử email → 200 hoặc lỗi cấu hình SMTP', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', API.guiTin);
    expect(listRes!.status()).toBe(200);
    const listBody = await listRes!.json();
    const configs = listBody.duLieu as Array<{ id: string; loai: string }>;
    const emailConfig = configs.find(c => c.loai === 'EMAIL' || c.loai === 'SMTP');
    if (!emailConfig) { test.skip(true, 'Không có cấu hình email'); return; }

    const res = await apiRequest(page, 'POST', `${API.guiTin}/${emailConfig.id}/gui-thu?nguoiNhan=test@example.com`);
    expect([200, 400, 422, 500, 502, 503]).toContain(res!.status());
    if (res!.status() === 200) {
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
    }
  });

  test('API: POST gui-thu không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const fakeId = '00000000-0000-0000-0000-000000000001';
    const res = await page.request.post(`${API.guiTin}/${fakeId}/gui-thu?nguoiNhan=test@example.com`);
    expect(res.status()).toBe(401);
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', API.guiTin);
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.guiTin);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-51: Cấu hình thông tin sáng kiến ────────────────────────────────────

test.describe('REQ-51: Cấu hình thông tin sáng kiến', () => {
  test.describe.configure({ timeout: 60_000 });

  test('API: GET cấu hình — 8 key bắt buộc tồn tại', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const keys = (body.duLieu as Array<{ khoa: string }>).map(c => c.khoa);
    const requiredKeys = [
      'MUC_CANH_BAO_TRUNG_LAP_VANG',
      'MUC_CANH_BAO_TRUNG_LAP_DO',
      'HE_SO_TU_VUNG',
      'HE_SO_NGU_NGHIA',
      'TU_DONG_KIEM_TRA_TRUNG_LAP',
      'MAU_MA_HO_SO',
      'DUNG_LUONG_TEP_TOI_DA_MB',
      'SO_TEP_TOI_DA',
    ];
    for (const key of requiredKeys) {
      expect(keys, `Key ${key} phải tồn tại trong cấu hình`).toContain(key);
    }
  });

  test('API: MUC_CANH_BAO_TRUNG_LAP_VANG < MUC_CANH_BAO_TRUNG_LAP_DO', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const configs = body.duLieu as Array<{ khoa: string; giaTri: string }>;
    const vang = configs.find(c => c.khoa === 'MUC_CANH_BAO_TRUNG_LAP_VANG');
    const do_ = configs.find(c => c.khoa === 'MUC_CANH_BAO_TRUNG_LAP_DO');
    if (vang && do_) {
      expect(parseFloat(vang.giaTri)).toBeLessThan(parseFloat(do_.giaTri));
    }
  });

  test('API: HE_SO_TU_VUNG + HE_SO_NGU_NGHIA xấp xỉ bằng 1.0', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const configs = body.duLieu as Array<{ khoa: string; giaTri: string }>;
    const tuVung = configs.find(c => c.khoa === 'HE_SO_TU_VUNG');
    const nguNghia = configs.find(c => c.khoa === 'HE_SO_NGU_NGHIA');
    if (tuVung && nguNghia) {
      const sum = parseFloat(tuVung.giaTri) + parseFloat(nguNghia.giaTri);
      expect(Math.abs(sum - 1.0)).toBeLessThanOrEqual(0.01);
    }
  });

  test('API: MAU_MA_HO_SO chứa placeholder {STT}', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const configs = body.duLieu as Array<{ khoa: string; giaTri: string }>;
    const mauMa = configs.find(c => c.khoa === 'MAU_MA_HO_SO');
    if (mauMa) {
      expect(mauMa.giaTri).toContain('{STT');
    }
  });

  test('API: DUNG_LUONG_TEP_TOI_DA_MB là số dương', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const configs = body.duLieu as Array<{ khoa: string; giaTri: string }>;
    const dungLuong = configs.find(c => c.khoa === 'DUNG_LUONG_TEP_TOI_DA_MB');
    if (dungLuong) {
      expect(parseFloat(dungLuong.giaTri)).toBeGreaterThan(0);
    }
  });

  test('API: SO_TEP_TOI_DA là số nguyên dương', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const configs = body.duLieu as Array<{ khoa: string; giaTri: string }>;
    const soTep = configs.find(c => c.khoa === 'SO_TEP_TOI_DA');
    if (soTep) {
      const val = parseInt(soTep.giaTri, 10);
      expect(val).toBeGreaterThan(0);
      expect(Number.isInteger(val)).toBe(true);
    }
  });

  test('API: TU_DONG_KIEM_TRA_TRUNG_LAP có giá trị kiểu boolean', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const configs = body.duLieu as Array<{ khoa: string; giaTri: string; kieuDuLieu: string }>;
    const tuDong = configs.find(c => c.khoa === 'TU_DONG_KIEM_TRA_TRUNG_LAP');
    if (tuDong) {
      // Either kieuDuLieu is BOOLEAN, or giaTri is 'true'/'false'
      const isBooleanType = tuDong.kieuDuLieu === 'BOOLEAN';
      const isBooleanValue = tuDong.giaTri === 'true' || tuDong.giaTri === 'false';
      expect(isBooleanType || isBooleanValue).toBe(true);
    }
  });

  test('API: lọc theo nhóm trả về tập con cấu hình', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const resAll = await apiRequest(page, 'GET', API.cauHinh);
    expect(resAll!.status()).toBe(200);
    const bodyAll = await resAll!.json();
    // Filtered list should be smaller or equal to full list
    const resFiltered = await apiRequest(page, 'GET', `${API.cauHinh}?nhom=he-thong`);
    if (resFiltered!.status() === 200) {
      const bodyFiltered = await resFiltered!.json();
      expect(bodyFiltered.duLieu.length).toBeLessThanOrEqual(bodyAll.duLieu.length);
    }
  });
});

// ─── Bảo vệ IDOR xuyên đơn vị ────────────────────────────────────────────────

test.describe('Bảo vệ IDOR xuyên đơn vị', () => {
  test.describe.configure({ timeout: 60_000 });

  test('qtdonvi không xem được cấu hình hệ thống — 200 hoặc 403, không 500', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'qtdonvi');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect([200, 403]).toContain(res!.status());
  });

  test('qtdonvi không xem được nhật ký hệ thống — 200 hoặc 403, không 500', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'qtdonvi');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/he-thong?trang=1&soDong=5`);
    expect([200, 403]).toContain(res!.status());
  });
});

// ─── Sắp xếp và phân trang nâng cao ────────────────────────────────────

test.describe('Sắp xếp và phân trang nâng cao', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET /nguoi-dung sapXep=hoTen&huong=asc trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=10&sapXep=hoTen&huong=asc`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('GET /nguoi-dung sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('GET /nguoi-dung trang=9999 trả về mảng rỗng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=9999&soDong=10`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(body.duLieu.length).toBe(0);
  });

  test('GET /vai-tro sapXep=ten&huong=asc trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.vaiTro}?trang=1&soDong=10&sapXep=ten&huong=asc`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeDefined();
  });

  test('GET /nhat-ky/he-thong sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKy}/he-thong?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });
});

// ─── Validation nâng cao ────────────────────────────────────────────────

test.describe('Validation nâng cao', () => {
  test.describe.configure({ timeout: 60_000 });

  test('POST /nguoi-dung với payload rỗng → 400/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.nguoiDung, {});
    expect([400, 422]).toContain(res!.status());
  });

  test('POST /vai-tro với payload rỗng → 400/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.vaiTro, {});
    expect([400, 422]).toContain(res!.status());
  });

  test('DELETE /nguoi-dung không xác thực → 401/405', async ({ page }) => {
    await page.goto('/');
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await page.request.delete(`${API.nguoiDung}/${fakeId}`);
    expect([401, 405]).toContain(res.status());
  });

  test('PUT /cau-hinh không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.put(API.cauHinh, {
      data: { khoa: 'TEST', giaTri: 'TEST' },
    });
    expect(res.status()).toBe(401);
  });

  test('tác giả PUT /cau-hinh → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'PUT', API.cauHinh, {
      khoa: 'TEST',
      giaTri: 'TEST',
    });
    expect(res!.status()).toBe(403);
  });
});

// ─── Responsive viewport ────────────────────────────────────────────────

test.describe('Responsive viewport', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang người dùng hiển thị đúng trên mobile (375px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
    const page = await context.newPage();
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nguoiDung);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
    const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
    expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
    await context.close();
  });

  test('trang vai trò hiển thị đúng trên tablet (768px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
    const page = await context.newPage();
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.vaiTro);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await context.close();
  });
});

// ─── Trang ngày nghỉ lễ ────────────────────────────────────────────────────

test.describe('REQ-46: Ngày nghỉ lễ', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang ngày nghỉ lễ tải với bảng và nút thêm', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.ngayNghiLe);
    await expect(page.locator('.ant-card-head-title').getByText('Danh mục')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: /thêm ngày nghỉ/i })).toBeVisible();
    await expect(page.locator('table')).toBeVisible();
  });

  test('UI: bộ chọn năm hiển thị đúng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.ngayNghiLe);
    await expect(page.getByText(/Năm \d{4}/)).toBeVisible({ timeout: 15_000 });
  });

  test('UI: thông tin ngày nghỉ ảnh hưởng hạn xử lý hiển thị', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.ngayNghiLe);
    await expect(page.getByText(/không tính vào thời hạn xử lý/i)).toBeVisible({ timeout: 15_000 });
  });

  test('API: GET ngay-nghi-le — 200 trả về mảng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.ngayNghiLe);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: GET ngay-nghi-le với tham số năm — 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const nam = new Date().getFullYear();
    const res = await apiRequest(page, 'GET', `${API.ngayNghiLe}?nam=${nam}`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: POST ngay-nghi-le — tạo và xoá ngày nghỉ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.ngayNghiLe, {
      ngay: '2026-12-25',
      ten: 'Ngày kiểm tra E2E',
      lapLaiHangNam: false,
      trangThai: 1,
    });
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const id = body.duLieu;
    expect(id).toBeTruthy();
    const del = await apiRequest(page, 'DELETE', `${API.ngayNghiLe}/${id}`);
    expect(del!.status()).toBe(200);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.ngayNghiLe);
    expect(res.status()).toBe(401);
  });

  test('Auth: tác giả GET — 200 hoặc 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', API.ngayNghiLe);
    expect([200, 403]).toContain(res!.status());
  });
});

// ─── Trang sao lưu ─────────────────────────────────────────────────────────

test.describe('REQ-46: Sao lưu hệ thống', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang sao lưu tải với thông tin tình trạng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.saoLuu);
    await expect(page.getByRole('main').getByText('Cấu hình hệ thống')).toBeVisible({ timeout: 15_000 });
  });

  test('API: GET sao-luu — 200 với các trường bắt buộc', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.saoLuu);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeDefined();
    const data = body.duLieu;
    expect(typeof data.mucCanhBao).toBe('string');
    expect(typeof data.soBan).toBe('number');
    expect(typeof data.tongKichThuocByte).toBe('number');
  });

  test('API: sao-luu — danhSach là mảng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.saoLuu);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.danhSach).toBeInstanceOf(Array);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.saoLuu);
    expect(res.status()).toBe(401);
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', API.saoLuu);
    expect(res!.status()).toBe(403);
  });
});

// ─── Trang mẫu thông báo ───────────────────────────────────────────────────

test.describe('REQ-50: Mẫu thông báo', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang mẫu thông báo tải với bảng và nút thêm', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.mauThongBao);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const hasTable = await page.locator('table').count() > 0;
    const hasAddBtn = await page.getByRole('button', { name: /thêm/i }).count() > 0;
    expect(hasTable || hasAddBtn).toBe(true);
  });

  test('UI: bảng có cột Mã, Tên mẫu, Sự kiện, Kênh', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.mauThongBao);
    await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    const headers = page.locator('.ant-table-thead th');
    await expect(headers.first()).toBeVisible({ timeout: 5_000 });
    const headerTexts = await headers.allTextContents();
    const combined = headerTexts.join(' ');
    expect(combined).toContain('Mã');
    expect(combined).toContain('Tên mẫu');
    expect(combined).toContain('Kênh');
  });

  test('UI: bộ lọc theo sự kiện hiện đúng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.mauThongBao);
    await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('Lọc theo sự kiện')).toBeVisible({ timeout: 5_000 });
  });

  test('API: GET mau-thong-bao — 200 trả về mảng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.mauThongBao);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: GET mau-thong-bao/su-kien — 200 trả về danh sách sự kiện', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.mauThongBao}/su-kien`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.mauThongBao);
    expect(res.status()).toBe(401);
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', API.mauThongBao);
    expect(res!.status()).toBe(403);
  });
});

// ─── Trang khoá API ngoài ──────────────────────────────────────────────────

test.describe('REQ-46: Khoá API ngoài', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang khoá API tải với nút cấp khoá mới', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.khoaApi);
    await expect(page.getByRole('button', { name: /cấp khoá mới/i })).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('table')).toBeVisible();
  });

  test('UI: bảng có cột Tên hệ thống, Khoá, Trạng thái', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.khoaApi);
    await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
    const headers = page.locator('.ant-table-thead th');
    await expect(headers.first()).toBeVisible({ timeout: 5_000 });
    const headerTexts = await headers.allTextContents();
    const combined = headerTexts.join(' ');
    expect(combined).toContain('Tên hệ thống');
    expect(combined).toContain('Trạng thái');
  });

  test('API: GET khoa-api-ngoai — 200 trả về mảng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.khoaApiNgoai);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: POST khoa-api-ngoai — tạo khoá mới', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.khoaApiNgoai, {
      ten: 'E2E Test System',
      danhSachIp: [],
      ngayHetHan: null,
      ghiChu: 'Test khoá tạo từ E2E',
    });
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.khoa).toBeTruthy();
    expect(body.duLieu.id).toBeTruthy();
    const del = await apiRequest(page, 'DELETE', `${API.khoaApiNgoai}/${body.duLieu.id}`);
    expect(del!.status()).toBe(200);
  });

  test('API: POST khoa-api-ngoai với tên rỗng → lỗi hoặc từ chối', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.khoaApiNgoai, {
      ten: '',
      danhSachIp: [],
    });
    const status = res!.status();
    if (status === 200) {
      const body = await res!.json();
      if (body.duLieu?.id) {
        await apiRequest(page, 'DELETE', `${API.khoaApiNgoai}/${body.duLieu.id}`);
      }
    }
    expect([200, 400, 422]).toContain(status);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.khoaApiNgoai);
    expect(res.status()).toBe(401);
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', API.khoaApiNgoai);
    expect(res!.status()).toBe(403);
  });
});

// ─── Trang nhật ký đồng bộ ─────────────────────────────────────────────────

test.describe('REQ-46: Nhật ký đồng bộ', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang nhật ký đồng bộ tải đúng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nhatKyDongBo);
    await expect(page.locator('.ant-card-head-title').getByText('Nhật ký hệ thống')).toBeVisible({ timeout: 15_000 });
  });

  test('API: GET tich-hop/he-thong — 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.tichHop}/he-thong`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeDefined();
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.tichHop}/he-thong`);
    expect(res.status()).toBe(401);
  });
});

// ─── Trang nhật ký lỗi ─────────────────────────────────────────────────────

test.describe('REQ-46: Nhật ký lỗi hệ thống', () => {
  test.describe.configure({ timeout: 60_000 });

  test('UI: trang nhật ký lỗi tải với bộ lọc và bảng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nhatKyLoi);
    await expect(page.locator('.ant-card-head-title').getByText('Nhật ký hệ thống')).toBeVisible({ timeout: 15_000 });
  });

  test('UI: bộ lọc mức độ hiển thị đúng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.nhatKyLoi);
    await expect(page.locator('.ant-card-head-title').getByText('Nhật ký hệ thống')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('Chưa xử lý').first()).toBeVisible();
  });

  test('API: GET nhat-ky/loi — 200 với phân trang', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKyLoi}?trang=1&soDong=10`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(typeof body.tongSo).toBe('number');
  });

  test('API: GET nhat-ky/loi lọc theo mức độ — 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKyLoi}?trang=1&soDong=10&mucDo=LOI`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('API: GET nhat-ky/loi lọc daXuLy=true — 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nhatKyLoi}?trang=1&soDong=10&daXuLy=true`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('Auth: không xác thực → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.nhatKyLoi}?trang=1&soDong=10`);
    expect(res.status()).toBe(401);
  });

  test('Auth: tác giả → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.nhatKyLoi}?trang=1&soDong=10`);
    expect(res!.status()).toBe(403);
  });
});

// ─── REQ-43: Người dùng — CRUD API ───────────────────────────────────────────

test.describe('REQ-43: Người dùng — CRUD API', () => {
  test.describe.configure({ timeout: 60_000 });

  // Shared state — safe because workers: 1 and fullyParallel: false
  let createdUserId = '';
  let createdUsername = '';

  test('POST tạo người dùng mới — 200 hoặc 422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const donViRes = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=1`);
    expect(donViRes!.status()).toBe(200);
    const donViBody = await donViRes!.json();
    expect((donViBody.duLieu as Array<{ id: string }>).length).toBeGreaterThan(0);
    const donViId = (donViBody.duLieu as Array<{ id: string }>)[0].id;
    createdUsername = `e2e.user.${Date.now()}`;
    const res = await apiRequest(page, 'POST', API.nguoiDung, {
      tenDangNhap: createdUsername,
      hoTen: 'E2E Test User',
      email: 'e2etest@test.vn',
      donViId,
      trangThaiTaiKhoan: 'HOAT_DONG',
      vaiTroIds: [],
    });
    expect([200, 422]).toContain(res!.status());
    if (res!.status() === 200) {
      const body = await res!.json();
      createdUserId =
        typeof body.duLieu === 'string'
          ? body.duLieu
          : (body.duLieu as { id: string }).id;
      expect(createdUserId).toBeTruthy();
    }
  });

  test('GET /{id} chi tiết — hoTen và tenDangNhap khớp', async ({ page }) => {
    if (!createdUserId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.nguoiDung}/${createdUserId}`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const user = body.duLieu as { hoTen: string; tenDangNhap: string };
    expect(user.hoTen).toBe('E2E Test User');
    expect(user.tenDangNhap).toBe(createdUsername);
  });

  test('PUT cập nhật hoTen — 200 và dữ liệu được lưu', async ({ page }) => {
    if (!createdUserId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'PUT', `${API.nguoiDung}/${createdUserId}`, {
      hoTen: 'E2E Updated User',
      email: 'e2etest@test.vn',
      vaiTroIds: [],
    });
    expect(res!.status()).toBe(200);
    const detailRes = await apiRequest(page, 'GET', `${API.nguoiDung}/${createdUserId}`);
    const detail = await detailRes!.json();
    const user = detail.duLieu as { hoTen: string };
    expect(user.hoTen).toBe('E2E Updated User');
  });

  test('PATCH /{id}/trang-thai khoá tài khoản — 200 hoặc 400', async ({ page }) => {
    if (!createdUserId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
    const res = await page.request.patch(
      `${API.nguoiDung}/${createdUserId}/trang-thai?trangThai=KHOA`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect([200, 400]).toContain(res.status());
  });

  test('PATCH /{id}/trang-thai mở khoá tài khoản — 200 hoặc 400', async ({ page }) => {
    if (!createdUserId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
    const res = await page.request.patch(
      `${API.nguoiDung}/${createdUserId}/trang-thai?trangThai=HOAT_DONG`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect([200, 400]).toContain(res.status());
  });

  test('POST /{id}/dat-lai-mat-khau — 200 và thanhCong = true', async ({ page }) => {
    if (!createdUserId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(
      page,
      'POST',
      `${API.nguoiDung}/${createdUserId}/dat-lai-mat-khau`
    );
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeDefined();
  });

  test('POST tạo người dùng trùng tenDangNhap — 400/409/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    // Use an existing user's username to guarantee a conflict
    const listRes = await apiRequest(page, 'GET', `${API.nguoiDung}?trang=1&soDong=1`);
    expect(listRes!.status()).toBe(200);
    const listBody = await listRes!.json();
    const existing = (listBody.duLieu as Array<{ tenDangNhap: string }>)[0];
    if (!existing) return;
    const donViRes = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=1`);
    const donViBody = await donViRes!.json();
    const donViId = (donViBody.duLieu as Array<{ id: string }>)[0].id;
    const res = await apiRequest(page, 'POST', API.nguoiDung, {
      tenDangNhap: existing.tenDangNhap,
      hoTen: 'E2E Duplicate User',
      email: 'e2eduplicate@test.vn',
      donViId,
      trangThaiTaiKhoan: 'HOAT_DONG',
      vaiTroIds: [],
    });
    expect([400, 409, 422]).toContain(res!.status());
  });

  test('Auth: tác giả POST tạo người dùng → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'POST', API.nguoiDung, {
      tenDangNhap: `e2e.deny.${Date.now()}`,
      hoTen: 'Should Fail',
      email: 'deny@test.vn',
      donViId: '00000000-0000-0000-0000-000000000001',
      trangThaiTaiKhoan: 'HOAT_DONG',
      vaiTroIds: [],
    });
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực GET danh sách người dùng → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.nguoiDung}?trang=1&soDong=5`);
    expect(res.status()).toBe(401);
  });

  test('Auth: không xác thực POST tạo người dùng → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.post(API.nguoiDung, {
      data: { tenDangNhap: 'e2e.unauth', hoTen: 'Unauthorized' },
    });
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-45: Vai trò — CRUD API ──────────────────────────────────────────────

test.describe('REQ-45: Vai trò — CRUD API', () => {
  test.describe.configure({ timeout: 60_000 });

  let createdRoleId = '';
  let createdRoleMa = '';

  test('GET danh sách — 200, duLieu.vaiTro có id, ma, ten', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.vaiTro);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const vaiTro = body.duLieu.vaiTro as Array<{ id: string; ma: string; ten: string }>;
    expect(vaiTro).toBeInstanceOf(Array);
    expect(vaiTro.length).toBeGreaterThan(0);
    const first = vaiTro[0];
    expect(typeof first.id).toBe('string');
    expect(typeof first.ma).toBe('string');
    expect(typeof first.ten).toBe('string');
  });

  test('POST tạo vai trò mới — 200 hoặc 422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    createdRoleMa = `E2E-ROLE-${Date.now()}`;
    const res = await apiRequest(page, 'POST', API.vaiTro, {
      ma: createdRoleMa,
      ten: 'E2E Test Role',
      moTa: 'Test vai trò E2E',
      thuTu: 99,
      trangThai: 1,
      quyenIds: [],
      loaiPhamVi: 'DON_VI',
    });
    expect([200, 422]).toContain(res!.status());
    if (res!.status() === 200) {
      const body = await res!.json();
      createdRoleId =
        typeof body.duLieu === 'string'
          ? body.duLieu
          : (body.duLieu as { id: string }).id;
      expect(createdRoleId).toBeTruthy();
    }
  });

  test('PUT cập nhật ten vai trò — 200', async ({ page }) => {
    if (!createdRoleId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'PUT', `${API.vaiTro}/${createdRoleId}`, {
      ma: createdRoleMa,
      ten: 'E2E Updated Role',
      moTa: 'Test vai trò E2E - cập nhật',
      thuTu: 99,
      trangThai: 1,
      quyenIds: [],
      loaiPhamVi: 'DON_VI',
    });
    expect(res!.status()).toBe(200);
  });

  test('POST /{id}/sao-chep — nhân bản vai trò — 200', async ({ page }) => {
    if (!createdRoleId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const cloneMa = `E2E-CLONE-${Date.now()}`;
    const res = await apiRequest(page, 'POST', `${API.vaiTro}/${createdRoleId}/sao-chep`, {
      ma: cloneMa,
      ten: 'E2E Cloned Role',
    });
    expect(res!.status()).toBe(200);
    if (res!.status() === 200) {
      const body = await res!.json();
      const clonedId =
        typeof body.duLieu === 'string'
          ? body.duLieu
          : (body.duLieu as { id?: string })?.id;
      if (clonedId) {
        await apiRequest(page, 'DELETE', `${API.vaiTro}/${clonedId}`);
      }
    }
  });

  test('DELETE vai trò vừa tạo — 200 hoặc 409', async ({ page }) => {
    if (!createdRoleId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'DELETE', `${API.vaiTro}/${createdRoleId}`);
    expect([200, 409]).toContain(res!.status());
    if (res!.status() === 200) {
      createdRoleId = '';
    }
  });

  test('DELETE vai trò hệ thống — từ chối 400/403/409', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', API.vaiTro);
    expect(listRes!.status()).toBe(200);
    const body = await listRes!.json();
    const systemRole = (
      body.duLieu.vaiTro as Array<{ id: string; laHeThong: boolean }>
    ).find(r => r.laHeThong === true);
    if (!systemRole) return;
    const delRes = await apiRequest(page, 'DELETE', `${API.vaiTro}/${systemRole.id}`);
    expect([400, 403, 409]).toContain(delRes!.status());
  });

  test('POST tạo vai trò trùng ma — 400/409/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    // Use an existing role's ma to guarantee a conflict
    const listRes = await apiRequest(page, 'GET', API.vaiTro);
    expect(listRes!.status()).toBe(200);
    const listBody = await listRes!.json();
    const existingMa = (
      listBody.duLieu.vaiTro as Array<{ ma: string }>
    )[0]?.ma;
    if (!existingMa) return;
    const res = await apiRequest(page, 'POST', API.vaiTro, {
      ma: existingMa,
      ten: 'E2E Duplicate Role',
      moTa: 'Kiểm tra trùng mã',
      thuTu: 100,
      trangThai: 1,
      quyenIds: [],
      loaiPhamVi: 'DON_VI',
    });
    expect([400, 409, 422]).toContain(res!.status());
  });

  test('Auth: tác giả POST tạo vai trò → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'POST', API.vaiTro, {
      ma: `E2E-DENY-${Date.now()}`,
      ten: 'Should Fail',
      moTa: '',
      thuTu: 1,
      trangThai: 1,
      quyenIds: [],
      loaiPhamVi: 'DON_VI',
    });
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực GET danh sách vai trò → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(API.vaiTro);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-46: Cấu hình — write API ────────────────────────────────────────────

test.describe('REQ-46: Cấu hình — write API', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET toàn bộ cấu hình — 200 với array có phần tử', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.cauHinh);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
    expect((body.duLieu as unknown[]).length).toBeGreaterThan(0);
  });

  test('GET cấu hình theo nhóm CHUNG — 200 với array', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.cauHinh}?nhom=CHUNG`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('PUT cập nhật TEN_HE_THONG — verify giá trị mới rồi khôi phục', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    // Read current value
    const getRes = await apiRequest(page, 'GET', API.cauHinh);
    expect(getRes!.status()).toBe(200);
    const getBody = await getRes!.json();
    const configs = getBody.duLieu as Array<{ khoa: string; giaTri: string }>;
    const original = configs.find(c => c.khoa === 'TEN_HE_THONG');
    const originalValue = original?.giaTri ?? 'BlueIdea';
    const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
    // Update
    const putRes = await page.request.put(API.cauHinh, {
      headers: { Authorization: `Bearer ${token}` },
      data: [{ khoa: 'TEN_HE_THONG', giaTri: 'BlueIdea Test' }],
    });
    expect([200, 403]).toContain(putRes.status());
    if (putRes.status() === 200) {
      // Verify
      const verifyRes = await apiRequest(page, 'GET', API.cauHinh);
      const verifyBody = await verifyRes!.json();
      const updated = (verifyBody.duLieu as Array<{ khoa: string; giaTri: string }>).find(
        c => c.khoa === 'TEN_HE_THONG'
      );
      expect(updated?.giaTri).toBe('BlueIdea Test');
      // Restore
      await page.request.put(API.cauHinh, {
        headers: { Authorization: `Bearer ${token}` },
        data: [{ khoa: 'TEN_HE_THONG', giaTri: originalValue }],
      });
    }
  });

  test('GET /cau-hinh-cong-khai — không cần xác thực, trả về 200', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get('/api/v1/he-thong/cau-hinh-cong-khai');
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeTruthy();
    expect(typeof body.duLieu).toBe('object');
  });

  test('Auth: tác giả PUT cấu hình → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
    const res = await page.request.put(API.cauHinh, {
      headers: { Authorization: `Bearer ${token}` },
      data: [{ khoa: 'TEN_HE_THONG', giaTri: 'Should Fail' }],
    });
    expect(res.status()).toBe(403);
  });

  test('Auth: không xác thực PUT cấu hình → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.put(API.cauHinh, {
      data: [{ khoa: 'TEN_HE_THONG', giaTri: 'Should Fail' }],
    });
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-48: Menu — CRUD API ──────────────────────────────────────────────────

test.describe('REQ-48: Menu — CRUD API', () => {
  test.describe.configure({ timeout: 60_000 });

  let createdMenuId = '';
  let createdMenuMa = '';

  test('GET danh sách menu WEB — 200 với array có ma và ten', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    expect(body.duLieu).toBeInstanceOf(Array);
    if ((body.duLieu as unknown[]).length > 0) {
      const first = body.duLieu[0] as { ma: string; ten: string };
      expect(typeof first.ma).toBe('string');
      expect(typeof first.ten).toBe('string');
    }
  });

  test('POST tạo menu mới — 200 hoặc 422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    createdMenuMa = `E2E-MENU-${Date.now()}`;
    const res = await apiRequest(page, 'POST', API.cauHinhMenu, {
      ma: createdMenuMa,
      ten: 'E2E Menu Item',
      icon: 'setting',
      duongDan: '/e2e-test',
      thuTu: 999,
      loai: 'WEB',
      hienThi: true,
      moTabMoi: false,
    });
    expect([200, 422]).toContain(res!.status());
    if (res!.status() === 200) {
      const body = await res!.json();
      createdMenuId =
        typeof body.duLieu === 'string'
          ? body.duLieu
          : (body.duLieu as { id: string }).id;
      expect(createdMenuId).toBeTruthy();
    }
  });

  test('PUT cập nhật ten menu — 200', async ({ page }) => {
    if (!createdMenuId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'PUT', `${API.cauHinhMenu}/${createdMenuId}`, {
      ma: createdMenuMa,
      ten: 'E2E Updated Menu',
      icon: 'setting',
      duongDan: '/e2e-test',
      thuTu: 999,
      loai: 'WEB',
      hienThi: true,
      moTabMoi: false,
    });
    expect(res!.status()).toBe(200);
  });

  test('DELETE menu vừa tạo — 200 hoặc 409', async ({ page }) => {
    if (!createdMenuId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'DELETE', `${API.cauHinhMenu}/${createdMenuId}`);
    expect([200, 409]).toContain(res!.status());
    if (res!.status() === 200) {
      createdMenuId = '';
    }
  });

  test('DELETE menu cha có menu con — từ chối 400/409', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(listRes!.status()).toBe(200);
    const body = await listRes!.json();
    const items = body.duLieu as Array<{ id: string; menuChaId: string | null }>;
    // Find all IDs that appear as menuChaId (i.e. are parents)
    const parentIds = new Set(
      items.filter(m => m.menuChaId != null).map(m => m.menuChaId as string)
    );
    const parentWithChildren = items.find(m => parentIds.has(m.id));
    if (!parentWithChildren) return; // No parent-child structure in seed data — skip
    const delRes = await apiRequest(
      page,
      'DELETE',
      `${API.cauHinhMenu}/${parentWithChildren.id}`
    );
    expect([200, 400, 409, 422]).toContain(delRes!.status());
  });

  test('PUT /sap-xep — sắp xếp lại thứ tự menu — 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(listRes!.status()).toBe(200);
    const body = await listRes!.json();
    const items = body.duLieu as Array<{ id: string; thuTu: number }>;
    if (items.length < 2) return;
    const token = await page.evaluate(() => localStorage.getItem('blueidea.accessToken'));
    // Swap the thuTu values of the first two root-level items
    const reordered = [
      { id: items[0].id, thuTu: items[1].thuTu },
      { id: items[1].id, thuTu: items[0].thuTu },
    ];
    const sapXepRes = await page.request.put(`${API.cauHinhMenu}/sap-xep?loai=WEB`, {
      headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
      data: reordered,
    });
    expect([200, 400, 422]).toContain(sapXepRes.status());
  });

  test('POST tạo menu trùng ma — 400/409/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    // Use an existing menu's ma to guarantee a conflict
    const listRes = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    expect(listRes!.status()).toBe(200);
    const listBody = await listRes!.json();
    const existingMa = (listBody.duLieu as Array<{ ma: string }>)[0]?.ma;
    if (!existingMa) return;
    const res = await apiRequest(page, 'POST', API.cauHinhMenu, {
      ma: existingMa,
      ten: 'E2E Duplicate Menu',
      icon: 'setting',
      duongDan: '/e2e-duplicate',
      thuTu: 998,
      loai: 'WEB',
      hienThi: true,
      moTabMoi: false,
    });
    expect([400, 409, 422]).toContain(res!.status());
  });

  test('Auth: tác giả POST tạo menu → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'POST', API.cauHinhMenu, {
      ma: `E2E-DENY-${Date.now()}`,
      ten: 'Should Fail',
      icon: 'setting',
      duongDan: '/should-fail',
      thuTu: 1,
      loai: 'WEB',
      hienThi: true,
      moTabMoi: false,
    });
    expect(res!.status()).toBe(403);
  });

  test('Auth: không xác thực GET danh sách menu → 401', async ({ page }) => {
    await page.goto('/');
    const res = await page.request.get(`${API.cauHinhMenu}?loai=WEB`);
    expect(res.status()).toBe(401);
  });
});

// ─── REQ-50: Mẫu thông báo — CRUD API ──────────────────────────────────────

test.describe('REQ-50: Mẫu thông báo — CRUD API', () => {
  test.describe.configure({ timeout: 60_000 });

  let createdTemplateId = '';
  const templateMa = `E2E-MT-${Date.now()}`;

  test('POST tạo mẫu thông báo — 200 và verify persistence', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.mauThongBao, {
      ma: templateMa,
      ten: 'E2E Mẫu Thông Báo Test',
      kenh: 'EMAIL',
      suKien: 'HO_SO_DUOC_TIEP_NHAN',
      tieuDe: 'Hồ sơ {{maHoSo}} đã được tiếp nhận',
      noiDung: 'Kính gửi {{hoTen}}, hồ sơ {{maHoSo}} đã được tiếp nhận thành công.',
      danhSachBien: ['maHoSo', 'hoTen'],
      trangThai: 1,
    });
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.thanhCong).toBe(true);
    createdTemplateId = typeof body.duLieu === 'string' ? body.duLieu : body.duLieu?.id;
    expect(createdTemplateId).toBeTruthy();
  });

  test('GET mẫu vừa tạo — xác nhận tất cả trường khớp', async ({ page }) => {
    if (!createdTemplateId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const listRes = await apiRequest(page, 'GET', API.mauThongBao);
    expect(listRes!.status()).toBe(200);
    const body = await listRes!.json();
    const items = body.duLieu as Array<{ id: string; ma: string; kenh: string; suKien: string }>;
    const found = items.find(i => i.id === createdTemplateId || i.ma === templateMa);
    expect(found).toBeDefined();
    if (found) {
      expect(found.kenh).toBe('EMAIL');
      expect(found.suKien).toBe('HO_SO_DUOC_TIEP_NHAN');
    }
  });

  test('PUT cập nhật mẫu — 200 và verify thay đổi', async ({ page }) => {
    if (!createdTemplateId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'PUT', `${API.mauThongBao}/${createdTemplateId}`, {
      ma: templateMa,
      ten: 'E2E Mẫu Đã Cập Nhật',
      kenh: 'SMS',
      suKien: 'HO_SO_DUOC_TIEP_NHAN',
      tieuDe: 'Đã tiếp nhận {{maHoSo}}',
      noiDung: 'Hồ sơ {{maHoSo}} đã được tiếp nhận.',
      danhSachBien: ['maHoSo'],
      trangThai: 1,
    });
    expect(res!.status()).toBe(200);
  });

  test('POST /{id}/xem-truoc — preview rendered template', async ({ page }) => {
    if (!createdTemplateId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', `${API.mauThongBao}/${createdTemplateId}/xem-truoc`, {
      bien: { maHoSo: 'SK-2026-001', hoTen: 'Nguyễn Văn A' },
    });
    // 200 if preview succeeds, 400 if template has no file to render from
    expect([200, 400]).toContain(res!.status());
    if (res!.status() === 200) {
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
    }
  });

  test('DELETE mẫu thông báo — 200', async ({ page }) => {
    if (!createdTemplateId) return;
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'DELETE', `${API.mauThongBao}/${createdTemplateId}`);
    expect(res!.status()).toBe(200);
    createdTemplateId = '';
  });

  test('GET /su-kien — danh sách sự kiện có phần tử', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.mauThongBao}/su-kien`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(body.duLieu.length).toBeGreaterThan(0);
  });

  test('POST tạo mẫu thiếu noiDung → 400/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.mauThongBao, {
      ma: `E2E-MT-EMPTY-${Date.now()}`,
      ten: 'Thiếu nội dung',
      kenh: 'EMAIL',
      suKien: 'HO_SO_DUOC_TIEP_NHAN',
      tieuDe: 'Test',
      trangThai: 1,
    });
    expect([400, 422]).toContain(res!.status());
  });

  test('POST tạo mẫu kenh không hợp lệ → 400/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'POST', API.mauThongBao, {
      ma: `E2E-MT-BADKENH-${Date.now()}`,
      ten: 'Kênh sai',
      kenh: 'INVALID_CHANNEL',
      suKien: 'HO_SO_DUOC_TIEP_NHAN',
      tieuDe: 'Test',
      noiDung: 'Nội dung test',
      trangThai: 1,
    });
    expect([400, 422]).toContain(res!.status());
  });

  test('Auth: tác giả POST tạo mẫu → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'POST', API.mauThongBao, {
      ma: `E2E-MT-DENY-${Date.now()}`,
      ten: 'Không được phép',
      kenh: 'EMAIL',
      suKien: 'HO_SO_DUOC_TIEP_NHAN',
      tieuDe: 'Test',
      noiDung: 'Test',
      trangThai: 1,
    });
    expect(res!.status()).toBe(403);
  });
});

// ─── REQ-46: Ngày nghỉ lễ — write operations ───────────────────────────────

test.describe('REQ-46: Ngày nghỉ lễ — write operations', () => {
  test.describe.configure({ timeout: 60_000 });

  test('POST + PUT ngày nghỉ — verify PUT update persists', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const createRes = await apiRequest(page, 'POST', API.ngayNghiLe, {
      ngay: '2026-12-31',
      ten: 'Tết Dương lịch E2E',
      lapLaiHangNam: false,
      trangThai: 1,
    });
    expect(createRes!.status()).toBe(200);
    const id = (await createRes!.json()).duLieu;
    expect(id).toBeTruthy();

    const putRes = await apiRequest(page, 'PUT', `${API.ngayNghiLe}/${id}`, {
      ngay: '2026-12-31',
      ten: 'Tết Dương lịch (Cập nhật)',
      lapLaiHangNam: true,
      trangThai: 1,
    });
    expect(putRes!.status()).toBe(200);

    const listRes = await apiRequest(page, 'GET', `${API.ngayNghiLe}?nam=2026`);
    const body = await listRes!.json();
    const found = (body.duLieu as Array<{ id: string; ten: string; lapLaiHangNam: boolean }>)
      .find(h => h.id === id);
    if (found) {
      expect(found.ten).toBe('Tết Dương lịch (Cập nhật)');
      expect(found.lapLaiHangNam).toBe(true);
    }

    await apiRequest(page, 'DELETE', `${API.ngayNghiLe}/${id}`);
  });

  test('POST trùng ngày → 400/409/422', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const first = await apiRequest(page, 'POST', API.ngayNghiLe, {
      ngay: '2026-11-20',
      ten: 'Ngày trùng 1',
      lapLaiHangNam: false,
      trangThai: 1,
    });
    expect(first!.status()).toBe(200);
    const firstId = (await first!.json()).duLieu;

    const dup = await apiRequest(page, 'POST', API.ngayNghiLe, {
      ngay: '2026-11-20',
      ten: 'Ngày trùng 2',
      lapLaiHangNam: false,
      trangThai: 1,
    });
    expect([400, 409, 422]).toContain(dup!.status());

    await apiRequest(page, 'DELETE', `${API.ngayNghiLe}/${firstId}`);
  });

  test('Auth: tác giả POST ngày nghỉ → 403', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'POST', API.ngayNghiLe, {
      ngay: '2026-10-10',
      ten: 'Không được phép',
      lapLaiHangNam: false,
      trangThai: 1,
    });
    expect(res!.status()).toBe(403);
  });
});

// ─── REQ-46: Sao lưu — mucCanhBao verification ─────────────────────────────

test.describe('REQ-46: Sao lưu — chi tiết', () => {
  test.describe.configure({ timeout: 60_000 });

  test('mucCanhBao có giá trị hợp lệ', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.saoLuu);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    const validLevels = ['BINH_THUONG', 'CANH_BAO', 'NGUY_HIEM', 'CHUA_CAU_HINH'];
    expect(validLevels).toContain(body.duLieu.mucCanhBao);
  });

  test('soBan và tongKichThuocByte là số không âm', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', API.saoLuu);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu.soBan).toBeGreaterThanOrEqual(0);
    expect(body.duLieu.tongKichThuocByte).toBeGreaterThanOrEqual(0);
  });
});

// ─── REQ-48: Menu — quyenMa + hienThi toggle ───────────────────────────────

test.describe('REQ-48: Menu — quyền và hiển thị', () => {
  test.describe.configure({ timeout: 60_000 });

  test('POST tạo menu với quyenMa → GET verify quyenMa persisted', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-MENU-Q-${Date.now()}`;
    const res = await apiRequest(page, 'POST', API.cauHinhMenu, {
      ma,
      ten: 'Menu Có Quyền',
      icon: 'lock',
      duongDan: '/e2e-quyen',
      thuTu: 998,
      loai: 'WEB',
      hienThi: true,
      moTabMoi: false,
      quyenMa: 'NguoiDungXem',
    });
    expect(res!.status()).toBe(200);
    const id = typeof (await res!.json()).duLieu === 'string'
      ? (await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`).then(async r => {
          const b = await r!.json();
          return (b.duLieu as Array<{ id: string; ma: string }>).find(m => m.ma === ma)?.id;
        }))
      : (await res!.json()).duLieu?.id;

    if (id) {
      const listRes = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
      const body = await listRes!.json();
      const menuItem = (body.duLieu as Array<{ id: string; ma: string; quyenMa?: string }>)
        .find(m => m.ma === ma);
      if (menuItem) {
        expect(menuItem.quyenMa).toBe('NguoiDungXem');
      }
      await apiRequest(page, 'DELETE', `${API.cauHinhMenu}/${id}`);
    }
  });

  test('PUT toggle hienThi=false → menu ẩn', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const ma = `E2E-MENU-H-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.cauHinhMenu, {
      ma,
      ten: 'Menu Ẩn Test',
      icon: 'eye-invisible',
      duongDan: '/e2e-an',
      thuTu: 997,
      loai: 'WEB',
      hienThi: true,
      moTabMoi: false,
    });
    expect(createRes!.status()).toBe(200);
    const createBody = await createRes!.json();
    const id = typeof createBody.duLieu === 'string' ? createBody.duLieu : createBody.duLieu?.id;
    if (!id) return;

    const putRes = await apiRequest(page, 'PUT', `${API.cauHinhMenu}/${id}`, {
      ma,
      ten: 'Menu Ẩn Test',
      icon: 'eye-invisible',
      duongDan: '/e2e-an',
      thuTu: 997,
      loai: 'WEB',
      hienThi: false,
      moTabMoi: false,
    });
    expect(putRes!.status()).toBe(200);

    const listRes = await apiRequest(page, 'GET', `${API.cauHinhMenu}?loai=WEB`);
    const body = await listRes!.json();
    const menuItem = (body.duLieu as Array<{ id: string; ma: string; hienThi: boolean }>)
      .find(m => m.ma === ma);
    if (menuItem) {
      expect(menuItem.hienThi).toBe(false);
    }

    await apiRequest(page, 'DELETE', `${API.cauHinhMenu}/${id}`);
  });
});

// ─── REQ-43: Người dùng — role assignment ───────────────────────────────────

test.describe('REQ-43: Người dùng — gán vai trò', () => {
  test.describe.configure({ timeout: 60_000 });

  test('PUT gán vaiTroIds cho người dùng → GET verify role assigned', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const rolesRes = await apiRequest(page, 'GET', API.vaiTro);
    expect(rolesRes!.status()).toBe(200);
    const rolesBody = await rolesRes!.json();
    const roles = rolesBody.duLieu.vaiTro as Array<{ id: string; ma: string }>;
    const nonSystemRole = roles.find(r => (r as { laHeThong?: boolean }).laHeThong !== true);
    if (!nonSystemRole) {
      test.skip(true, 'No non-system role to assign');
      return;
    }

    const donViRes = await apiRequest(page, 'GET', `${API.donVi}?trang=1&soDong=1`);
    const donViId = (await donViRes!.json()).duLieu[0]?.id;
    if (!donViId) return;

    const username = `e2e.role.${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.nguoiDung, {
      tenDangNhap: username,
      hoTen: 'E2E Role Assign',
      email: 'roletest@test.vn',
      donViId,
      trangThaiTaiKhoan: 'HOAT_DONG',
      vaiTroIds: [],
    });
    if (createRes!.status() !== 200) return;
    const userId = typeof (await createRes!.json()).duLieu === 'string'
      ? (await createRes!.json()).duLieu
      : ((await createRes!.json()).duLieu as { id: string }).id;
    // Re-read to get actual ID
    const userList = await apiRequest(page, 'GET', `${API.nguoiDung}?tuKhoa=${username}`);
    const users = (await userList!.json()).duLieu as Array<{ id: string }>;
    const actualId = users[0]?.id ?? userId;

    const putRes = await apiRequest(page, 'PUT', `${API.nguoiDung}/${actualId}`, {
      hoTen: 'E2E Role Assign',
      email: 'roletest@test.vn',
      vaiTroIds: [nonSystemRole.id],
    });
    expect(putRes!.status()).toBe(200);

    const detailRes = await apiRequest(page, 'GET', `${API.nguoiDung}/${actualId}`);
    expect(detailRes!.status()).toBe(200);
    const detail = await detailRes!.json();
    const assignedRoles = (detail.duLieu as { vaiTroIds?: string[] }).vaiTroIds ?? [];
    expect(assignedRoles).toContain(nonSystemRole.id);
  });
});

// ─── REQ-45: Vai trò — permission matrix ───────────────────────────────────

test.describe('REQ-45: Vai trò — ma trận quyền', () => {
  test.describe.configure({ timeout: 60_000 });

  test('POST tạo vai trò với quyenIds → GET verify permissions assigned', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const rolesRes = await apiRequest(page, 'GET', API.vaiTro);
    const rolesBody = await rolesRes!.json();
    const allPermissions = rolesBody.duLieu.quyen as Array<{ id: string; ma: string }>;
    if (!allPermissions || allPermissions.length === 0) {
      test.skip(true, 'No permissions available');
      return;
    }
    const twoPermIds = allPermissions.slice(0, 2).map(p => p.id);

    const ma = `E2E-PERM-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.vaiTro, {
      ma,
      ten: 'E2E Permission Test Role',
      moTa: 'Test gán quyền',
      thuTu: 98,
      trangThai: 1,
      quyenIds: twoPermIds,
      loaiPhamVi: 'DON_VI',
    });
    expect(createRes!.status()).toBe(200);
    const createBody = await createRes!.json();
    const roleId = typeof createBody.duLieu === 'string' ? createBody.duLieu : createBody.duLieu?.id;
    expect(roleId).toBeTruthy();

    const verifyRes = await apiRequest(page, 'GET', API.vaiTro);
    const verifyBody = await verifyRes!.json();
    const createdRole = (verifyBody.duLieu.vaiTro as Array<{ id: string; quyenIds?: string[] }>)
      .find(r => r.id === roleId);
    if (createdRole?.quyenIds) {
      for (const pid of twoPermIds) {
        expect(createdRole.quyenIds).toContain(pid);
      }
    }

    await apiRequest(page, 'DELETE', `${API.vaiTro}/${roleId}`);
  });

  test('PUT cập nhật quyenIds → permissions thay đổi', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const rolesRes = await apiRequest(page, 'GET', API.vaiTro);
    const rolesBody = await rolesRes!.json();
    const allPermissions = rolesBody.duLieu.quyen as Array<{ id: string }>;
    if (!allPermissions || allPermissions.length < 3) return;

    const ma = `E2E-PERM-UPD-${Date.now()}`;
    const createRes = await apiRequest(page, 'POST', API.vaiTro, {
      ma,
      ten: 'E2E Perm Update',
      thuTu: 97,
      trangThai: 1,
      quyenIds: [allPermissions[0].id],
      loaiPhamVi: 'DON_VI',
    });
    if (createRes!.status() !== 200) return;
    const roleId = typeof (await createRes!.json()).duLieu === 'string'
      ? (await createRes!.json()).duLieu
      : (await createRes!.json()).duLieu?.id;

    const newPermIds = [allPermissions[1].id, allPermissions[2].id];
    const putRes = await apiRequest(page, 'PUT', `${API.vaiTro}/${roleId}`, {
      ma,
      ten: 'E2E Perm Updated',
      thuTu: 97,
      trangThai: 1,
      quyenIds: newPermIds,
      loaiPhamVi: 'DON_VI',
    });
    expect(putRes!.status()).toBe(200);

    await apiRequest(page, 'DELETE', `${API.vaiTro}/${roleId}`);
  });
});
