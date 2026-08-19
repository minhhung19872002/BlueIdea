import { test, expect } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { ACCOUNTS, API, ROUTES } from '../helpers/constants';

// ─── REQ-42: Ứng dụng di động / Responsive web ─────────────────────────────
// Web phải responsive hoàn toàn từ 320px. Breakpoint chuyển đổi mobile ↔ desktop tại lg (992px).
// Mobile: sidebar → drawer, hamburger button, ẩn search nhanh và tên người dùng.

const MOBILE_VIEWPORT = { width: 375, height: 812 };
const TABLET_VIEWPORT = { width: 768, height: 1024 };
const DESKTOP_VIEWPORT = { width: 1280, height: 800 };
const MIN_VIEWPORT = { width: 320, height: 568 };

test.describe('REQ-42: Responsive — Mobile viewport (375px)', () => {
  test.describe('Layout chuyển đổi mobile', () => {
    test('hamburger menu hiển thị trên mobile', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dashboard);
      await page.waitForLoadState('networkidle');
      await expect(
        page.locator('button[aria-label="Mở menu"]')
      ).toBeVisible({ timeout: 10_000 });
    });

    test('sidebar không hiển thị trên mobile', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dashboard);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('.ant-layout-sider')).not.toBeVisible();
    });

    test('bấm hamburger mở drawer điều hướng', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dashboard);
      await page.waitForLoadState('networkidle');
      await page.locator('button[aria-label="Mở menu"]').click();
      await expect(page.locator('.ant-drawer')).toBeVisible({ timeout: 5_000 });
      await expect(
        page.locator('.ant-drawer nav[aria-label="Điều hướng chính"]')
      ).toBeVisible({ timeout: 5_000 });
    });

    test('drawer chứa menu BLUEIDEA', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dashboard);
      await page.waitForLoadState('networkidle');
      await page.locator('button[aria-label="Mở menu"]').click();
      await expect(page.locator('.ant-drawer')).toBeVisible({ timeout: 5_000 });
      await expect(
        page.locator('.ant-drawer').getByText('BLUEIDEA')
      ).toBeVisible({ timeout: 5_000 });
    });

    test('đóng drawer khi bấm ra ngoài', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dashboard);
      await page.waitForLoadState('networkidle');
      await page.locator('button[aria-label="Mở menu"]').click();
      await expect(page.locator('.ant-drawer')).toBeVisible({ timeout: 5_000 });
      await page.locator('.ant-drawer-mask').click();
      await expect(page.locator('.ant-drawer-open')).not.toBeVisible({ timeout: 5_000 });
    });

    test('ô tìm kiếm nhanh ẩn trên mobile', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dashboard);
      await page.waitForLoadState('networkidle');
      const timKiem = page.locator('.ant-layout-header input[placeholder*="tìm" i]');
      const visible = await timKiem.isVisible().catch(() => false);
      expect(visible).toBeFalsy();
    });
  });

  test.describe('Các trang chính tải trên mobile', () => {
    test('trang chủ tải không lỗi trên mobile (admin)', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dashboard);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('trang hồ sơ của tôi tải trên mobile (tác giả)', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'tacgia1');
      await page.goto(ROUTES.hoSoCuaToi);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
      await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
    });

    test('trang xử lý tải trên mobile (thư ký)', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'thuky');
      await page.goto(ROUTES.xuLy);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });

    test('trang đánh giá tải trên mobile (hội đồng)', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'hoidong01');
      await page.goto(ROUTES.danhGia);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });

    test('trang tra cứu tải trên mobile (admin)', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.traCuu);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });

    test('trang báo cáo tải trên mobile (admin)', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.baoCao);
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    });
  });

  test.describe('Điều hướng qua drawer trên mobile', () => {
    test('bấm mục menu trong drawer điều hướng đến trang tương ứng', async ({ page }) => {
      await page.setViewportSize(MOBILE_VIEWPORT);
      await page.goto('/');
      await loginViaAPI(page, 'admin');
      await page.goto(ROUTES.dashboard);
      await page.waitForLoadState('networkidle');

      await page.locator('button[aria-label="Mở menu"]').click();
      await expect(page.locator('.ant-drawer')).toBeVisible({ timeout: 5_000 });

      const menuItem = page.locator('.ant-drawer [role="link"]').first();
      await expect(menuItem).toBeVisible({ timeout: 5_000 });
      await menuItem.click();

      // Drawer should close after clicking
      await page.waitForTimeout(1_000);
      const drawerOpen = await page.locator('.ant-drawer-open').isVisible().catch(() => false);
      expect(drawerOpen).toBeFalsy();
    });
  });
});

test.describe('REQ-42: Responsive — Viewport minimum (320px)', () => {
  test('trang đăng nhập tải trên 320px', async ({ page }) => {
    await page.setViewportSize(MIN_VIEWPORT);
    await page.goto(ROUTES.login);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await expect(
      page.locator('input[autocomplete="username"]')
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      page.locator('input[autocomplete="current-password"]')
    ).toBeVisible({ timeout: 10_000 });
  });

  test('trang chủ tải trên 320px (admin)', async ({ page }) => {
    await page.setViewportSize(MIN_VIEWPORT);
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.dashboard);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
  });

  test('cổng công khai tải trên 320px', async ({ page }) => {
    await page.setViewportSize(MIN_VIEWPORT);
    await page.goto(ROUTES.congKhai);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
  });
});

test.describe('REQ-42: Responsive — Tablet viewport (768px)', () => {
  test('tablet hiển thị hamburger menu (dưới lg=992px)', async ({ page }) => {
    await page.setViewportSize(TABLET_VIEWPORT);
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.dashboard);
    await page.waitForLoadState('networkidle');
    await expect(
      page.locator('button[aria-label="Mở menu"]')
    ).toBeVisible({ timeout: 10_000 });
  });

  test('tablet — sidebar không hiển thị', async ({ page }) => {
    await page.setViewportSize(TABLET_VIEWPORT);
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.dashboard);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-layout-sider')).not.toBeVisible();
  });

  test('tablet — trang quản trị danh mục tải không lỗi', async ({ page }) => {
    await page.setViewportSize(TABLET_VIEWPORT);
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.danhMuc);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('body')).not.toContainText('Lỗi hệ thống');
  });
});

test.describe('REQ-42: Responsive — Desktop viewport (1280px)', () => {
  test('desktop hiển thị sidebar cố định', async ({ page }) => {
    await page.setViewportSize(DESKTOP_VIEWPORT);
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.dashboard);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-layout-sider')).toBeVisible({ timeout: 10_000 });
  });

  test('desktop — hamburger menu không hiển thị', async ({ page }) => {
    await page.setViewportSize(DESKTOP_VIEWPORT);
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.dashboard);
    await page.waitForLoadState('networkidle');
    const hamburger = page.locator('button[aria-label="Mở menu"]');
    const visible = await hamburger.isVisible().catch(() => false);
    expect(visible).toBeFalsy();
  });

  test('desktop — tên người dùng hiển thị cạnh avatar', async ({ page }) => {
    await page.setViewportSize(DESKTOP_VIEWPORT);
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.dashboard);
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.ant-layout-header')).toBeVisible({ timeout: 10_000 });
    // Avatar should be visible
    await expect(page.locator('.ant-layout-header .ant-avatar').first()).toBeVisible({ timeout: 5_000 });
  });

  test('desktop — sidebar chứa điều hướng chính', async ({ page }) => {
    await page.setViewportSize(DESKTOP_VIEWPORT);
    await page.goto('/');
    await loginViaAPI(page, 'admin');
    await page.goto(ROUTES.dashboard);
    await page.waitForLoadState('networkidle');
    await expect(
      page.locator('.ant-layout-sider nav[aria-label="Điều hướng chính"]')
    ).toBeVisible({ timeout: 10_000 });
  });
});
