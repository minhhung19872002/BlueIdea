import { defineConfig, devices } from '@playwright/test';

const API_PORT = Number(process.env.API_PORT) || 8080;
const WEB_PORT = Number(process.env.WEB_PORT) || 5173;
const PG_PORT = process.env.PG_PORT || '5432';
const BASE_URL = `http://localhost:${WEB_PORT}`;

export default defineConfig({
  testDir: './specs',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [
    ['html', { open: 'never' }],
    ['list'],
  ],
  timeout: 60_000,
  expect: { timeout: 10_000 },

  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    locale: 'vi-VN',
    timezoneId: 'Asia/Ho_Chi_Minh',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: [
    {
      command: 'dotnet run --project ../../src/BlueIdea.Api -c Release',
      port: API_PORT,
      timeout: 120_000,
      reuseExistingServer: !process.env.CI,
      env: {
        ASPNETCORE_URLS: `http://0.0.0.0:${API_PORT}`,
        ASPNETCORE_ENVIRONMENT: 'Development',
        KhoiTao__TuDongMigrate: 'true',
        KhoiTao__NapDuLieuMau: 'true',
        ConnectionStrings__Postgres: `Host=localhost;Port=${PG_PORT};Database=blueidea;Username=blueidea;Password=blueidea;Include Error Detail=true`,
        GioiHanTruyCap__SoRequestMoiPhut: '50000',
        GioiHanTruyCap__SoLanDangNhapMoiPhut: '10000',
        CongViecNen__BatHangfire: 'false',
        QuetVirus__Bat: 'false',
      },
    },
    {
      command: 'npm run dev',
      cwd: '../../web',
      port: WEB_PORT,
      timeout: 30_000,
      reuseExistingServer: !process.env.CI,
      env: {
        // Máy dev có thể đã chiếm cổng 8080/5173 bởi dự án khác; hai biến này cho phép chạy bộ
        // kiểm thử vào đúng API và cổng web của BlueIdea mà không sửa cấu hình dự án.
        PROXY_API_URL: `http://127.0.0.1:${API_PORT}`,
        WEB_PORT: String(WEB_PORT),
      },
    },
  ],
});
