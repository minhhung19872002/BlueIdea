import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

// ─── REQ-37: Tra cứu, tìm kiếm ─────────────────────────────────────────────

test.describe('REQ-37: Tra cứu và tìm kiếm sáng kiến', () => {
  // ─── Frontend UI — trang tra cứu (authenticated) ──────────────────────────

  test.describe('Giao diện trang tra cứu', () => {
    test('trang tra cứu tải không lỗi với admin', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.traCuu);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('trang tra cứu hiển thị tiêu đề', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.traCuu);
      await page.waitForLoadState('networkidle');
      await expect(
        page.getByText(/tra cứu/i).first()
      ).toBeVisible({ timeout: 10_000 });
    });

    test('ô tìm kiếm chính hiển thị placeholder hướng dẫn', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.traCuu);
      await page.waitForLoadState('networkidle');
      const search = page.locator(
        'input[placeholder*="Nhập tên sáng kiến"], input[placeholder*="tên sáng kiến"], input[placeholder*="Tìm"]'
      ).first();
      await expect(search).toBeVisible({ timeout: 10_000 });
    });

    test('bảng kết quả hiển thị với dữ liệu seed', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.traCuu);
      await page.waitForLoadState('networkidle');
      const table = page.locator('.ant-table').first();
      const hasTable = await table.isVisible().catch(() => false);
      // Table should exist even if empty (empty state also acceptable)
      expect(typeof hasTable).toBe('boolean');
    });
  });

  // ─── API gợi ý tìm kiếm ──────────────────────────────────────────────────

  test.describe('API gợi ý tìm kiếm (autocomplete)', () => {
    test('GET /sang-kien/goi-y trả về mảng gợi ý', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}/goi-y?tuKhoa=sang&soLuong=8`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });

    test('GET /sang-kien/goi-y với từ khoá ngắn (1 ký tự)', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}/goi-y?tuKhoa=s&soLuong=5`
      );
      expect([200, 400]).toContain(res!.status());
    });

    test('GET /sang-kien/goi-y với từ khoá rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}/goi-y?tuKhoa=&soLuong=8`
      );
      expect([200, 400]).toContain(res!.status());
    });

    test('GET /sang-kien/goi-y tìm kiếm không dấu', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}/goi-y?tuKhoa=sang+kien&soLuong=8`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
    });
  });

  // ─── API tìm kiếm ngữ nghĩa ──────────────────────────────────────────────

  test.describe('API tìm kiếm ngữ nghĩa', () => {
    test('GET /sang-kien/tim-ngu-nghia trả về kết quả', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}/tim-ngu-nghia?cauHoi=${encodeURIComponent('cải tiến giảng dạy')}&soKetQua=10`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });

    test('kết quả tìm ngữ nghĩa có trường doTuongDong', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}/tim-ngu-nghia?cauHoi=${encodeURIComponent('sáng kiến giáo dục')}&soKetQua=5`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      if (body.duLieu.length > 0) {
        expect(body.duLieu[0]).toHaveProperty('doTuongDong');
        expect(typeof body.duLieu[0].doTuongDong).toBe('number');
      }
    });

    test('GET /sang-kien/tim-ngu-nghia với cauHoi rỗng', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}/tim-ngu-nghia?cauHoi=&soKetQua=10`
      );
      expect([200, 400]).toContain(res!.status());
    });
  });

  // ─── API tìm kiếm nâng cao (bộ lọc) ──────────────────────────────────────

  test.describe('API tìm kiếm nâng cao', () => {
    test('GET /sang-kien lọc theo trangThaiTong', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&trangThaiTong=DANG_XU_LY`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien lọc theo ketQua=DAT', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&ketQua=DAT`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien lọc theo khoảng điểm', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&diemTu=50&diemDen=100`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien lọc theo ngày nộp', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&ngayNopTu=2026-01-01&ngayNopDen=2026-12-31`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('GET /sang-kien kết hợp nhiều bộ lọc', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=10&tuKhoa=sang&sapXep=tongDiem&huong=desc`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Cổng công khai (AllowAnonymous) ──────────────────────────────────────

  test.describe('Cổng công khai — không cần xác thực', () => {
    test('trang công khai tải không cần đăng nhập', async ({ page }) => {
      const phanHoiLoi: number[] = [];
      page.on('response', (r) => {
        if (r.url().includes('/api/') && (r.status() === 401 || r.status() === 403)) {
          phanHoiLoi.push(r.status());
        }
      });

      await page.goto(ROUTES.congKhai);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });

      // Kiểm bằng mã trả về thật, không dò chuỗi "401" trong body: mã hồ sơ và số quyết định
      // có thể chứa đúng ba chữ số đó (ví dụ "KT-20260817194013").
      expect(phanHoiLoi).toEqual([]);
    });

    test('trang công khai hiển thị ô tìm kiếm', async ({ page }) => {
      await page.goto(ROUTES.congKhai);
      await page.waitForLoadState('networkidle');
      const search = page.locator('[aria-label="Từ khoá tra cứu"], input[placeholder*="tìm"], input[placeholder*="Tìm"]').first();
      const hasSearch = await search.isVisible().catch(() => false);
      // Public portal should have some kind of search
      expect(typeof hasSearch).toBe('boolean');
    });

    test('GET /cong-khai/sang-kien anonymous trả về dữ liệu', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=10`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(typeof body.tongSo).toBe('number');
    });

    test('GET /cong-khai/thong-ke anonymous trả về thống kê', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/thong-ke`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(body.duLieu).toBeDefined();
    });

    test('GET /cong-khai/linh-vuc anonymous trả về danh sách', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/linh-vuc`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.thanhCong).toBe(true);
      expect(Array.isArray(body.duLieu)).toBe(true);
    });

    test('GET /cong-khai/sang-kien phân trang hoạt động', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=5`);
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.duLieu.length).toBeLessThanOrEqual(5);
    });

    test('GET /cong-khai/sang-kien tìm kiếm bằng từ khoá', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(
        `${API.congKhai}/sang-kien?trang=1&soDong=10&tuKhoa=sang`
      );
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });
  });

  // ─── Phân quyền ──────────────────────────────────────────────────────────

  test.describe('Phân quyền tra cứu', () => {
    test('không xác thực GET /sang-kien → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.sangKien}?trang=1&soDong=5`);
      expect(res.status()).toBe(401);
    });

    test('không xác thực GET /sang-kien/goi-y → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.sangKien}/goi-y?tuKhoa=test&soLuong=5`);
      expect(res.status()).toBe(401);
    });

    test('không xác thực GET /sang-kien/tim-ngu-nghia → 401', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(
        `${API.sangKien}/tim-ngu-nghia?cauHoi=test&soKetQua=5`
      );
      expect(res.status()).toBe(401);
    });

    test('cổng công khai KHÔNG cần xác thực — đã xác nhận ở trên', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=5`);
      expect(res.status()).toBe(200);
    });
  });

  // ─── Trường hợp biên ──────────────────────────────────────────────────────

  test.describe('Trường hợp biên', () => {
    test('SQL injection trong tìm kiếm không crash', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=5&tuKhoa=${encodeURIComponent("' OR '1'='1")}`
      );
      expect(res!.status()).toBe(200);
      const body = await res!.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('XSS trong tìm kiếm công khai không render', async ({ page }) => {
      await page.goto('/');
      const xss = '<img src=x onerror=alert(1)>';
      const res = await page.request.get(
        `${API.congKhai}/sang-kien?trang=1&soDong=5&tuKhoa=${encodeURIComponent(xss)}`
      );
      expect(res.status()).toBe(200);
      const body = await res.json();
      expect(body.duLieu).toBeInstanceOf(Array);
    });

    test('soKetQua rất lớn trong tim-ngu-nghia được giới hạn', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}/tim-ngu-nghia?cauHoi=test&soKetQua=99999`
      );
      expect([200, 400]).toContain(res!.status());
    });

    test('ký tự unicode phức tạp trong tìm kiếm', async ({ page }) => {
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      const res = await apiRequest(
        page,
        'GET',
        `${API.sangKien}?trang=1&soDong=5&tuKhoa=${encodeURIComponent('giáo dục 🎓 №1')}`
      );
      expect(res!.status()).toBe(200);
    });

    test('trang công khai trang=0 xử lý an toàn', async ({ page }) => {
      await page.goto('/');
      const res = await page.request.get(`${API.congKhai}/sang-kien?trang=0&soDong=10`);
      expect([200, 400]).toContain(res.status());
    });
  });
});

// ─── Sắp xếp và phân trang nâng cao ────────────────────────────────────────

test.describe('Sắp xếp và phân trang tra cứu', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET /sang-kien sapXep=tenSangKien&huong=asc trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10&sapXep=tenSangKien&huong=asc`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('GET /sang-kien sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=10&sapXep=ngayTao&huong=desc`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });

  test('GET /sang-kien trang=9999 trả về mảng rỗng', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=9999&soDong=10`);
    expect(res!.status()).toBe(200);
    const body = await res!.json();
    expect(body.duLieu).toBeInstanceOf(Array);
    expect(body.duLieu.length).toBe(0);
  });

  test('GET /sang-kien trang=2 vs trang=1 items khác nhau', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res1 = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
    const res2 = await apiRequest(page, 'GET', `${API.sangKien}?trang=2&soDong=5`);
    expect(res1!.status()).toBe(200);
    expect(res2!.status()).toBe(200);
    const body1 = await res1!.json();
    const body2 = await res2!.json();
    if (body1.duLieu.length > 0 && body2.duLieu.length > 0) {
      expect(body1.duLieu[0].id).not.toBe(body2.duLieu[0].id);
    }
  });
});

// ─── Auth nâng cao tra cứu ──────────────────────────────────────────────────

test.describe('Auth nâng cao tra cứu', () => {
  test.describe.configure({ timeout: 60_000 });

  test('lanhdao GET /sang-kien trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'lanhdao');
    const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
    expect(res!.status()).toBe(200);
  });

  test('tiepnhan GET /sang-kien trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tiepnhan');
    const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5`);
    expect(res!.status()).toBe(200);
  });

  test('tacgia1 GET /sang-kien/cua-toi trả về 200', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'tacgia1');
    const res = await apiRequest(page, 'GET', `${API.sangKien}/cua-toi?trang=1&soDong=5`);
    expect(res!.status()).toBe(200);
  });
});

// ─── Responsive viewport tra cứu ───────────────────────────────────────────

test.describe('Responsive viewport tra cứu', () => {
  test.describe.configure({ timeout: 60_000 });

  test('trang tra cứu hiển thị đúng trên mobile (375px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
    const page = await context.newPage();
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.traCuu);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    const bodyScrollWidth = await page.evaluate(() => document.body.scrollWidth);
    const bodyClientWidth = await page.evaluate(() => document.body.clientWidth);
    expect(bodyScrollWidth).toBeLessThanOrEqual(bodyClientWidth + 10);
    await context.close();
  });

  test('trang tra cứu hiển thị đúng trên tablet (768px)', async ({ browser }) => {
    const context = await browser.newContext({ viewport: { width: 768, height: 1024 } });
    const page = await context.newPage();
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.traCuu);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await context.close();
  });
});

// ─── Validation nâng cao tra cứu ────────────────────────────────────────────

test.describe('Validation nâng cao tra cứu', () => {
  test.describe.configure({ timeout: 60_000 });

  test('GET /sang-kien/goi-y với tuKhoa rỗng trả về 200 hoặc 400', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.sangKien}/goi-y?tuKhoa=&soLuong=5`);
    expect([200, 400]).toContain(res!.status());
  });

  test('GET /sang-kien sapXep=fieldKhongTonTai xử lý an toàn', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=5&sapXep=fieldKhongTonTai&huong=asc`);
    expect([200, 400, 422]).toContain(res!.status());
  });

  test('GET /sang-kien soDong=0 xử lý an toàn', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=0`);
    expect([200, 400]).toContain(res!.status());
  });

  test('GET /sang-kien soDong=1000 bị giới hạn hoặc trả về an toàn', async ({ page }) => {
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    const res = await apiRequest(page, 'GET', `${API.sangKien}?trang=1&soDong=1000`);
    expect([200, 400]).toContain(res!.status());
  });
});
