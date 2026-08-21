import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { API } from '../helpers/constants';

/**
 * Các luồng bổ sung sau đợt rà soát 51 chức năng:
 *  - REQ-26: hội đồng ghi ý kiến xem xét cảnh báo trùng lặp + xuất báo cáo PDF
 *  - REQ-39/40: xuất PDF cho báo cáo chưa đạt / theo đơn vị / theo tác giả / thời gian xử lý
 *  - REQ-21/43: người dùng tự sửa thông tin cá nhân, lối vào trang bảo mật tài khoản
 *  - REQ-35: xuất phiếu chấm bản Word
 *  - REQ-15/29: uỷ quyền xử lý bước
 *  - Nhóm X: đặt lệnh xuất báo cáo chạy nền
 */
test.describe('Luồng bổ sung sau rà soát', () => {
  // ─── REQ-21/43: hồ sơ cá nhân ────────────────────────────────────

  test.describe('REQ-21/43: Hồ sơ cá nhân', () => {
    test('menu người dùng có lối vào Thông tin cá nhân và Bảo mật tài khoản', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto('/');

      // Mở dropdown ở góc phải thanh tiêu đề.
      await page.locator('.ant-layout-header .ant-dropdown-trigger').first().click();

      await expect(page.getByRole('link', { name: 'Thông tin cá nhân', exact: true })).toBeVisible({
        timeout: 10_000,
      });

      // `exact` vì menu bên trái đã có sẵn mục "🔐 Bảo mật tài khoản"; lối tắt trong menu người
      // dùng là mục thứ hai trỏ cùng đường dẫn.
      await expect(
        page.getByRole('link', { name: 'Bảo mật tài khoản', exact: true }),
      ).toBeVisible();
    });

    test('trang hồ sơ cá nhân tải được và hiển thị đơn vị chỉ đọc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto('/ho-so-ca-nhan');

      await expect(page.getByText('Thông tin cá nhân').first()).toBeVisible({ timeout: 15_000 });
      await expect(page.getByText('Đơn vị và vai trò do quản trị viên gán')).toBeVisible();
    });

    test('API cập nhật thông tin cá nhân rồi đọc lại đúng giá trị mới', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia3');

      const chucVuMoi = `Giáo viên ${Date.now() % 100000}`;

      const capNhat = await apiRequest(page, 'PUT', API.me, {
        hoTen: 'Phạm Thị Thuý',
        chucVu: chucVuMoi,
        dienThoai: '0900112233',
      });
      expect(capNhat!.status()).toBe(200);

      const doc = await apiRequest(page, 'GET', API.me);
      const body = await doc!.json();
      expect(body.duLieu.chucVu).toBe(chucVuMoi);
      expect(body.duLieu.dienThoai).toBe('0900112233');
    });

    test('API cập nhật thông tin cá nhân chặn dữ liệu không hợp lệ', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');

      const res = await apiRequest(page, 'PUT', API.me, {
        hoTen: '',
        dienThoai: 'khong-phai-so',
      });

      expect(res!.status()).toBe(422);
    });

    test('không đăng nhập thì không cập nhật được thông tin cá nhân', async ({ page }) => {
      const res = await page.request.put(API.me, { data: { hoTen: 'Ai đó' } });
      expect(res.status()).toBe(401);
    });
  });

  // ─── REQ-26: ý kiến hội đồng về trùng lặp ────────────────────────

  test.describe('REQ-26: Xem xét cảnh báo trùng lặp', () => {
    test('admin ghi được ý kiến xem xét và đọc lại thấy đã xem xét', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');

      const ds = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=20`);
      const body = await ds!.json();
      const hoSo = (body.duLieu as Array<Record<string, string>>).find(
        (x) => x.trangThaiTong !== 'NHAP',
      );
      expect(hoSo, 'dữ liệu mẫu phải có hồ sơ đã nộp').toBeTruthy();

      await apiRequest(page, 'POST', `${API.sangKien}/${hoSo!.id}/trung-lap/chay-lai`);

      const ghi = await apiRequest(page, 'POST', `${API.sangKien}/${hoSo!.id}/trung-lap/xem-xet`, {
        yKienHoiDong: 'E2E: hội đồng đã đối chiếu.',
      });
      expect(ghi!.status()).toBe(200);

      const doc = await apiRequest(page, 'GET', `${API.sangKien}/${hoSo!.id}/trung-lap`);
      const ketQua = await doc!.json();
      expect(ketQua.duLieu.daXemXet).toBe(true);
      expect(ketQua.duLieu.yKienHoiDong).toContain('E2E');
    });

    test('tác giả không ghi được ý kiến xem xét — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ds = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=20`);
      const body = await ds!.json();
      const hoSo = (body.duLieu as Array<Record<string, string>>).find(
        (x) => x.trangThaiTong !== 'NHAP',
      );

      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'POST', `${API.sangKien}/${hoSo!.id}/trung-lap/xem-xet`, {
        yKienHoiDong: 'Tôi tự kết luận',
      });

      expect(res!.status()).toBe(403);
    });

    test('xuất báo cáo trùng lặp ra PDF', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');

      const ds = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=20`);
      const body = await ds!.json();
      const hoSo = (body.duLieu as Array<Record<string, string>>).find(
        (x) => x.trangThaiTong !== 'NHAP',
      );

      await apiRequest(page, 'POST', `${API.sangKien}/${hoSo!.id}/trung-lap/chay-lai`);

      const res = await apiRequest(page, 'GET', `${API.sangKien}/${hoSo!.id}/trung-lap/xuat-pdf`);
      expect(res!.status()).toBe(200);
      expect(res!.headers()['content-type']).toContain('application/pdf');
    });
  });

  // ─── REQ-39/40: xuất PDF các báo cáo còn thiếu ───────────────────

  test.describe('REQ-39/40: Xuất PDF báo cáo', () => {
    for (const loai of ['sang-kien-chua-dat', 'theo-don-vi', 'theo-tac-gia', 'thoi-gian-xu-ly']) {
      test(`báo cáo ${loai} xuất được PDF`, async ({ page }) => {
        await page.goto('/');
        await loginViaAPI(page, 'admin');

        const res = await apiRequest(page, 'GET', `${API.baoCao}/${loai}/xuat-pdf`);
        expect(res!.status()).toBe(200);
        expect(res!.headers()['content-type']).toContain('application/pdf');
      });
    }

    test('tác giả không xuất được PDF báo cáo — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');

      const res = await apiRequest(page, 'GET', `${API.baoCao}/theo-don-vi/xuat-pdf`);
      expect(res!.status()).toBe(403);
    });

    test('nút Xuất PDF hiện trên trang báo cáo chưa đạt', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto('/bao-cao/sang-kien-chua-dat');

      await expect(page.getByRole('button', { name: /Xuất PDF/ })).toBeVisible({ timeout: 15_000 });
    });
  });

  // ─── Nhóm X: xuất báo cáo chạy nền ───────────────────────────────

  test.describe('Xuất báo cáo chạy nền', () => {
    test('đặt lệnh xuất nền trả về 202', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');

      const res = await apiRequest(page, 'POST', `${API.baoCao}/theo-don-vi/xuat-nen`);
      expect(res!.status()).toBe(202);
    });

    test('loại báo cáo lạ bị chặn — 400', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');

      const res = await apiRequest(page, 'POST', `${API.baoCao}/khong-ton-tai/xuat-nen`);
      expect(res!.status()).toBe(400);
    });

    test('tác giả không đặt được lệnh xuất nền — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');

      const res = await apiRequest(page, 'POST', `${API.baoCao}/theo-don-vi/xuat-nen`);
      expect(res!.status()).toBe(403);
    });

    test('nút Xuất nền hiện trên trang báo cáo', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto('/bao-cao/theo-don-vi');

      await expect(page.getByRole('button', { name: /Xuất nền/ })).toBeVisible({ timeout: 15_000 });
    });
  });

  // ─── REQ-15/29: uỷ quyền xử lý ───────────────────────────────────

  test.describe('REQ-15/29: Uỷ quyền xử lý bước', () => {
    test('API trả về danh sách tác nhân của bước hiện tại', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');

      const ds = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=20`);
      const body = await ds!.json();
      const hoSo = (body.duLieu as Array<Record<string, string>>).find(
        (x) => x.trangThaiTong !== 'NHAP',
      );

      const res = await apiRequest(page, 'GET', `${API.xuLy}/tac-nhan-buoc/${hoSo!.id}`);
      expect(res!.status()).toBe(200);

      const ketQua = await res!.json();
      expect(Array.isArray(ketQua.duLieu)).toBe(true);
    });

    test('tác giả không đọc được danh sách tác nhân — 403', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const ds = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=20`);
      const body = await ds!.json();
      const hoSo = (body.duLieu as Array<Record<string, string>>).find(
        (x) => x.trangThaiTong !== 'NHAP',
      );

      await loginViaAPI(page, 'tacgia1');
      const res = await apiRequest(page, 'GET', `${API.xuLy}/tac-nhan-buoc/${hoSo!.id}`);
      expect(res!.status()).toBe(403);
    });
  });

  // ─── REQ-35: phiếu chấm bản Word ─────────────────────────────────

  test.describe('REQ-35: Xuất phiếu chấm bản Word', () => {
    test('API xuất phiếu chấm định dạng DOCX', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');

      const ds = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=50`);
      const body = await ds!.json();
      const coDiem = (body.duLieu as Array<Record<string, unknown>>).find(
        (x) => x.tongDiem !== null && x.tongDiem !== undefined,
      );

      if (!coDiem) {
        test.skip(true, 'dữ liệu mẫu chưa có hồ sơ đã chấm điểm');
        return;
      }

      const res = await apiRequest(
        page,
        'GET',
        `/api/v1/nhap-xuat/phieu-cham/ho-so/${coDiem.id}?dinhDang=DOCX`,
      );

      expect(res!.status()).toBe(200);
      expect(res!.headers()['content-type']).toContain('wordprocessingml.document');
    });
  });
});
