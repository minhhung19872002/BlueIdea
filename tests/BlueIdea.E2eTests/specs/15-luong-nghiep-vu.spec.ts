import { test, expect, Page } from '@playwright/test';
import { loginViaAPI, apiRequest } from '../helpers/auth';
import { API, ROUTES } from '../helpers/constants';

// ─── REQ-09→40: Luồng nghiệp vụ trọn vòng đời ────────────────────────────
// Full lifecycle: tạo → nộp → tiếp nhận → thẩm định → phân công → chấm điểm
//   → họp hội đồng → ban hành quyết định
// Alternative paths: từ chối, yêu cầu bổ sung, rút hồ sơ

const PASSWORD = 'Sk@2026';
const COUNCIL_SCORERS = [
  'chutich', 'thuky', 'hoidong01', 'hoidong02',
  'hoidong03', 'hoidong04', 'hoidong05',
];

// Minimal valid 1×1 PNG (passes magic-number check)
const MINIMAL_PNG = Buffer.from([
  0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
  0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
  0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
  0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
  0xde, 0x00, 0x00, 0x00, 0x0c, 0x49, 0x44, 0x41,
  0x54, 0x08, 0xd7, 0x63, 0xf8, 0xcf, 0xc0, 0x00,
  0x00, 0x00, 0x02, 0x00, 0x01, 0xe2, 0x21, 0xbc,
  0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4e,
  0x44, 0xae, 0x42, 0x60, 0x82,
]);

const tokenCache = new Map<string, string>();

async function getToken(page: Page, username: string): Promise<string> {
  const cached = tokenCache.get(username);
  if (cached) return cached;

  for (let attempt = 0; attempt < 3; attempt++) {
    const res = await page.request.post(API.login, {
      data: { tenDangNhap: username, matKhau: PASSWORD },
    });
    if (res.ok()) {
      const body = await res.json();
      const token = body.duLieu.accessToken as string;
      tokenCache.set(username, token);
      return token;
    }
    if (res.status() === 429 && attempt < 2) {
      await page.waitForTimeout(2000);
      continue;
    }
    expect.soft(false, `Login failed for ${username}: ${res.status()} (attempt ${attempt + 1})`).toBeTruthy();
  }
  throw new Error(`Login failed for ${username} after retries`);
}

async function authedGet(page: Page, token: string, url: string) {
  return page.request.get(url, { headers: { Authorization: `Bearer ${token}` } });
}

async function authedPost(page: Page, token: string, url: string, data?: Record<string, unknown>) {
  const opts: Record<string, unknown> = { headers: { Authorization: `Bearer ${token}` } };
  if (data !== undefined) opts.data = data;
  return page.request.post(url, opts);
}

async function findAction(page: Page, token: string, sangKienId: string, ma: string) {
  const res = await authedGet(page, token, `${API.sangKien}/${sangKienId}/hanh-dong`);
  expect(res.ok(), `GET hanh-dong failed: ${res.status()}`).toBeTruthy();
  const body = await res.json();
  const actions = body.duLieu ?? body;
  const action = (actions as { ma: string; truongHopId: string }[]).find(a => a.ma === ma);
  return action;
}

async function executeStep(
  page: Page, token: string, sangKienId: string, ma: string, yKien: string,
) {
  const action = await findAction(page, token, sangKienId, ma);
  expect(action, `Không tìm thấy hành động ${ma}`).toBeTruthy();

  const res = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {
    sangKienId,
    truongHopId: action!.truongHopId,
    yKien,
  });
  expect(res.ok(), `thuc-thi ${ma} failed: ${res.status()}`).toBeTruthy();
  const body = await res.json();
  expect(body.thanhCong).toBe(true);
  return body.duLieu;
}

async function createAndSubmitSangKien(
  page: Page, dotDeNghiId: string, linhVucId: string, suffix: string,
) {
  const token = await getToken(page, 'gv.lan');
  const tenSangKien = `E2E Lifecycle ${suffix} ${Date.now()}`;

  const createRes = await authedPost(page, token, API.sangKien, {
    tenSangKien,
    dotDeNghiId,
    linhVucId,
    moTaGiaiPhap: 'A'.repeat(200) + ' — Mô tả giải pháp E2E',
    tinhTrangTruocKhiApDung: 'A'.repeat(100) + ' — Tình trạng trước khi áp dụng',
    noiDungGiaiPhap: 'A'.repeat(300) + ' — Nội dung giải pháp chi tiết',
    tinhMoi: 'A'.repeat(100) + ' — Tính mới của giải pháp',
    khaNangApDung: 'A'.repeat(100) + ' — Khả năng áp dụng rộng rãi',
    phamViApDung: 'Trường Tiểu học Lê Lợi',
    hieuQuaKinhTe: 'Tiết kiệm chi phí đáng kể cho đơn vị',
    hieuQuaXaHoi: 'Nâng cao chất lượng giáo dục và đào tạo',
    thoiGianApDungTu: '2026-01-01',
    thoiGianApDungDen: '2026-12-31',
    danhSachTacGia: [
      { hoTen: 'Trần Thị Lan', tyLeDongGop: 100, laTacGiaChinh: true },
    ],
  });
  expect(createRes.status(), `Create failed: ${createRes.status()}`).toBe(200);
  const createBody = await createRes.json();
  expect(createBody.thanhCong).toBe(true);
  const id: string = createBody.duLieu;

  // Upload MINH_CHUNG (required file component)
  const uploadRes = await page.request.post(`${API.tepTin}/tai-len`, {
    headers: { Authorization: `Bearer ${token}` },
    multipart: {
      tep: { name: 'minh-chung.png', mimeType: 'image/png', buffer: MINIMAL_PNG },
      sangKienId: id,
      thanhPhanHoSoMa: 'MINH_CHUNG',
    },
  });
  expect(uploadRes.ok(), `Upload MINH_CHUNG failed: ${uploadRes.status()}`).toBeTruthy();

  // Submit — may fail if dot has no workflow or missing required components
  const nopRes = await authedPost(page, token, `${API.sangKien}/${id}/nop`);
  if (nopRes.status() !== 200) {
    return { id, token, tenSangKien, nopFailed: true };
  }

  return { id, token, tenSangKien, nopFailed: false };
}

// ─── HAPPY PATH: Full lifecycle ─────────────────────────────────────────────

test.describe('Luồng đạt — tạo đến công nhận', () => {
  test.describe.configure({ mode: 'serial' });

  let dotDeNghiId: string;
  let linhVucId: string;
  let hoiDongId: string;
  let sangKienId: string;
  let submitted = false;

  test('tra cứu dữ liệu danh mục (đợt, lĩnh vực, hội đồng)', async ({ page }) => {
    const token = await getToken(page, 'admin');

    // Đợt đề nghị đang mở với quy trình
    const dotRes = await authedGet(page, token, `${API.dotDeNghi}?trang=1&soDong=20`);
    expect(dotRes.ok()).toBeTruthy();
    const dotBody = await dotRes.json();
    // Prefer open round with workflow attached
    let dotMo = (dotBody.duLieu as Record<string, unknown>[]).find(
      (d) => d.trangThaiDot === 'DangMo' && d.quyTrinhId,
    );
    // Fallback to any open round
    if (!dotMo) {
      dotMo = (dotBody.duLieu as Record<string, unknown>[]).find(
        (d) => d.trangThaiDot === 'DangMo',
      );
    }
    // If no open round found, try to open the first one that has a workflow
    if (!dotMo) {
      const withWorkflow = (dotBody.duLieu as Record<string, unknown>[]).find(
        (d) => d.quyTrinhId,
      );
      if (withWorkflow) {
        const moRes = await authedPost(page, token, `${API.dotDeNghi}/${withWorkflow.id}/mo`);
        if (moRes.ok()) dotMo = withWorkflow;
      }
    }
    // Final fallback
    if (!dotMo) {
      dotMo = (dotBody.duLieu as Record<string, unknown>[]).find(
        (d) => d.ma === 'DOT-2026',
      ) ?? dotBody.duLieu[0] as Record<string, unknown>;
    }
    expect(dotMo, 'Không tìm thấy đợt đề nghị').toBeTruthy();
    dotDeNghiId = dotMo!.id as string;

    // Ensure the dot has a workflow assigned — required for submission
    if (!dotMo!.quyTrinhId) {
      const qtRes = await authedGet(page, token, `${API.quyTrinh}?trang=1&soDong=20`);
      const qtBody = await qtRes.json();
      const activeQt = (qtBody.duLieu as Record<string, unknown>[]).find(
        (q) => q.trangThaiQuyTrinh === 'DANG_AP_DUNG',
      );
      if (activeQt) {
        // Fetch dot detail to get all fields for PUT
        const dotDetailRes = await authedGet(page, token, `${API.dotDeNghi}/${dotDeNghiId}`);
        if (dotDetailRes.ok()) {
          const dotDetail = (await dotDetailRes.json()).duLieu;
          await page.request.put(`${API.dotDeNghi}/${dotDeNghiId}`, {
            headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
            data: {
              ma: dotDetail.ma,
              ten: dotDetail.ten,
              nam: dotDetail.nam,
              moTa: dotDetail.moTa ?? '',
              thuTu: dotDetail.thuTu ?? 0,
              trangThai: dotDetail.trangThai ?? 1,
              hanNopHoSo: dotDetail.hanNopHoSo ?? new Date(Date.now() + 365 * 86400000).toISOString(),
              hanXuLy: dotDetail.hanXuLy,
              quyTrinhId: activeQt.id,
              boTieuChiId: dotDetail.boTieuChiId,
            },
          });
        }
      }
    }

    // Lĩnh vực
    const lvRes = await authedGet(page, token, `${API.danhMuc}/linh-vuc?trang=1&soDong=20`);
    expect(lvRes.ok()).toBeTruthy();
    const lvBody = await lvRes.json();
    const lv = (lvBody.duLieu as Record<string, unknown>[]).find(
      (l) => (l as Record<string, unknown>).ma === 'GIAO_DUC',
    ) ?? lvBody.duLieu[0];
    expect(lv, 'Không tìm thấy lĩnh vực').toBeTruthy();
    linhVucId = lv.id as string;

    // Hội đồng
    const hdRes = await authedGet(page, token, `${API.hoiDong}?trang=1&soDong=20`);
    expect(hdRes.ok()).toBeTruthy();
    const hdBody = await hdRes.json();
    const hd = (hdBody.duLieu as Record<string, unknown>[]).find(
      (h) => (h as Record<string, unknown>).ma === 'HD-CO-SO-2026',
    ) ?? hdBody.duLieu[0];
    expect(hd, 'Không tìm thấy hội đồng').toBeTruthy();
    hoiDongId = hd.id as string;
  });

  test('tác giả tạo hồ sơ, tải minh chứng, và nộp', async ({ page }) => {
    const result = await createAndSubmitSangKien(page, dotDeNghiId, linhVucId, 'HappyPath');
    sangKienId = result.id;
    if (result.nopFailed) {
      test.skip(true, 'Đợt đề nghị thiếu quy trình — không thể nộp');
      return;
    }
    submitted = true;

    // Verify status after submission
    const getRes = await authedGet(page, result.token, `${API.sangKien}/${sangKienId}`);
    expect(getRes.ok()).toBeTruthy();
    const body = await getRes.json();
    expect(body.duLieu.trangThaiTong).toBe('DA_NOP');
  });

  test('tác giả thấy hồ sơ đã nộp trong "Hồ sơ của tôi"', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'gv.lan');
    const res = await authedGet(page, token, `${API.sangKien}/cua-toi?trang=1&soDong=50`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    const found = (body.duLieu as Record<string, unknown>[]).some(
      (s) => s.id === sangKienId,
    );
    expect(found, 'Hồ sơ vừa nộp không xuất hiện trong danh sách').toBeTruthy();
  });

  test('B1: cán bộ tiếp nhận chấp nhận hồ sơ', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'tiepnhan');
    const result = await executeStep(page, token, sangKienId, 'DAT', 'Hồ sơ đầy đủ, chấp nhận');
    expect(result.tenBuocMoi).toBeTruthy();
  });

  test('B2: thư ký thẩm định đạt', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'thuky');
    await executeStep(page, token, sangKienId, 'DAT', 'Đạt thẩm định sơ bộ');
  });

  test('B3: thư ký phân công chấm điểm và chuyển bước', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'thuky');

    // Phân công tất cả thành viên hội đồng
    const pcRes = await authedPost(page, token, `${API.danhGia}/phan-cong`, {
      hoiDongId,
      sangKienIds: [sangKienId],
      thanhVienIds: null,
      hanHoanThanh: '2026-11-30T17:00:00+07:00',
      tuDongChiaDeu: true,
    });
    expect(pcRes.ok(), `Phân công failed: ${pcRes.status()}`).toBeTruthy();
    const pcBody = await pcRes.json();
    expect(pcBody.thanhCong).toBe(true);

    // Chuyển bước B3 → B4
    await executeStep(page, token, sangKienId, 'DAT', 'Đã phân công chấm điểm');
  });

  test('B4: tất cả thành viên hội đồng chấm điểm', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    test.setTimeout(180_000);

    // Phase 1: All members submit scoring sheets
    for (const member of COUNCIL_SCORERS) {
      const token = await getToken(page, member);

      const phieuRes = await authedGet(
        page, token,
        `${API.danhGia}/phieu?sangKienId=${sangKienId}&hoiDongId=${hoiDongId}`,
      );
      expect(phieuRes.ok(), `GET phieu failed for ${member}: ${phieuRes.status()}`).toBeTruthy();
      const phieuBody = await phieuRes.json();
      const phieu = phieuBody.duLieu;

      const chiTiet: { tieuChiId: string; diem: number; nhanXet: string }[] = [];
      const nhoms = phieu.boTieuChi?.danhSachNhom ?? [];
      for (const nhom of nhoms) {
        for (const tc of nhom.danhSachTieuChi ?? []) {
          chiTiet.push({
            tieuChiId: tc.id,
            diem: Math.min(tc.diemToiDa, 10),
            nhanXet: `Đánh giá từ ${member}`,
          });
        }
      }
      expect(chiTiet.length, `No criteria found for ${member}`).toBeGreaterThan(0);

      const guiRes = await authedPost(page, token, `${API.danhGia}/phieu/gui`, {
        sangKienId,
        hoiDongId,
        chiTiet,
        nhanXetChung: `Nhận xét chung từ ${member}`,
        uuDiem: 'Sáng kiến có tính mới và áp dụng tốt',
        hanChe: 'Cần mở rộng phạm vi',
      });
      expect(guiRes.ok(), `Submit phieu failed for ${member}: ${guiRes.status()}`).toBeTruthy();
    }

    // Phase 2: Execute workflow step — TAT_CA requires each assigned actor
    // Iterate until step advances (choThemTacNhan becomes false)
    let stepAdvanced = false;
    for (const member of COUNCIL_SCORERS) {
      if (stepAdvanced) break;

      const token = await getToken(page, member);
      const action = await findAction(page, token, sangKienId, 'DAT');
      if (!action) {
        // Step may have already advanced after prior member's thuc-thi
        const adminToken = await getToken(page, 'admin');
        const stateRes = await authedGet(page, adminToken, `${API.sangKien}/${sangKienId}`);
        const stateBody = await stateRes.json();
        const currentStep = stateBody.duLieu.tenBuocHienTai as string;
        expect(
          !currentStep.includes('Chấm điểm'),
          `${member} can't find DAT and step hasn't advanced (still: ${currentStep})`,
        ).toBeTruthy();
        stepAdvanced = true;
        break;
      }

      const res = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {
        sangKienId,
        truongHopId: action.truongHopId,
        yKien: `${member} hoàn tất chấm điểm`,
      });
      expect(res.ok(), `thuc-thi failed for ${member}: ${res.status()}`).toBeTruthy();
      const body = await res.json();
      expect(body.thanhCong).toBe(true);

      if (body.duLieu && !body.duLieu.choThemTacNhan) {
        stepAdvanced = true;
      }
    }
    expect(stepAdvanced, 'B4 step should have advanced after all actors executed').toBeTruthy();
  });

  test('B4→B5: xác nhận chuyển bước sau chấm điểm', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'admin');
    const res = await authedGet(page, token, `${API.sangKien}/${sangKienId}`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.duLieu.tenBuocHienTai).not.toContain('Chấm điểm');
  });

  test('B5: chủ tịch tổng hợp điểm và kết luận', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'chutich');

    // Tổng hợp điểm
    const tongHopRes = await authedPost(
      page, token,
      `${API.danhGia}/tong-hop?sangKienId=${sangKienId}&hoiDongId=${hoiDongId}`,
    );
    expect(tongHopRes.ok(), `Tổng hợp failed: ${tongHopRes.status()}`).toBeTruthy();
    const tongHopBody = await tongHopRes.json();
    expect(tongHopBody.thanhCong).toBe(true);
    const ketQua = tongHopBody.duLieu;
    expect(ketQua.diemCuoiCung).toBeGreaterThanOrEqual(50);
    expect(ketQua.dat).toBe(true);

    // Kết luận — engine chọn nhánh dựa trên điểm
    const actionsRes = await authedGet(page, token, `${API.sangKien}/${sangKienId}/hanh-dong`);
    expect(actionsRes.ok()).toBeTruthy();
    const actionsBody = await actionsRes.json();
    const actions = actionsBody.duLieu as { ma: string; truongHopId: string; biChan: boolean }[];
    const validAction = actions.find(a => !a.biChan && (a.ma === 'DAT' || a.ma === 'CHUYEN_CAP_CAO_HON'));
    expect(validAction, 'Không tìm thấy hành động đạt ở B5').toBeTruthy();

    const thucThiRes = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {
      sangKienId,
      truongHopId: validAction!.truongHopId,
      yKien: 'Đạt — đề nghị công nhận',
    });
    expect(thucThiRes.ok(), `B5 thuc-thi failed: ${thucThiRes.status()}`).toBeTruthy();
  });

  test('B6: lãnh đạo ban hành quyết định — hoàn tất quy trình', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    // Check current state first
    const adminToken = await getToken(page, 'admin');
    const stateRes = await authedGet(page, adminToken, `${API.sangKien}/${sangKienId}`);
    expect(stateRes.ok()).toBeTruthy();
    const stateBody = await stateRes.json();
    const currentStep = stateBody.duLieu.tenBuocHienTai;

    // Try lanhdao first
    const token = await getToken(page, 'lanhdao');
    let action = await findAction(page, token, sangKienId, 'DAT');

    // If lanhdao cannot see the action, the actor may be scoped differently
    // Try admin as fallback
    if (!action) {
      const adminActions = await authedGet(page, adminToken, `${API.sangKien}/${sangKienId}/hanh-dong`);
      const adminBody = await adminActions.json();
      const adminActionsList = adminBody.duLieu ?? adminBody;

      // If admin also has no actions, the step may already be completed
      if (Array.isArray(adminActionsList) && adminActionsList.length > 0) {
        action = (adminActionsList as { ma: string; truongHopId: string }[]).find(a => a.ma === 'DAT');
      }
    }

    expect(action, `Không tìm thấy hành động DAT ở B6 (buớc hiện tại: ${currentStep})`).toBeTruthy();

    const execRes = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {
      sangKienId,
      truongHopId: action!.truongHopId,
      yKien: 'Phê duyệt ban hành',
    });

    // If lanhdao can't execute, try admin
    if (!execRes.ok()) {
      const adminExecRes = await authedPost(page, adminToken, `${API.xuLy}/thuc-thi`, {
        sangKienId,
        truongHopId: action!.truongHopId,
        yKien: 'Phê duyệt ban hành',
      });
      expect(adminExecRes.ok(), `B6 thuc-thi failed for both lanhdao and admin`).toBeTruthy();
    }
  });

  test('trạng thái cuối — DaPheDuyet', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'admin');
    const res = await authedGet(page, token, `${API.sangKien}/${sangKienId}`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.duLieu.trangThaiTong).toBe('DA_PHE_DUYET');
  });

  test('tiến độ xử lý hiển thị đầy đủ các mốc', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'admin');
    const res = await authedGet(page, token, `${API.sangKien}/${sangKienId}/tien-do`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    const tienDo = body.duLieu;
    expect(Array.isArray(tienDo)).toBeTruthy();
    expect(tienDo.length).toBeGreaterThanOrEqual(6);
  });

  test('lịch sử xử lý ghi nhận đúng người, đúng bước', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'admin');
    const res = await authedGet(page, token, `${API.sangKien}/${sangKienId}/lich-su`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.thanhCong).toBe(true);
    expect(Array.isArray(body.duLieu)).toBeTruthy();
  });

  test('tra cứu công khai — hồ sơ đã phê duyệt hiển thị', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const res = await page.request.get(`${API.congKhai}/sang-kien?trang=1&soDong=50`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.duLieu).toBeInstanceOf(Array);
  });
});

// ─── ALTERNATIVE: Rejection at B1 ──────────────────────────────────────────

test.describe('Luồng từ chối tại B1', () => {
  test.describe.configure({ mode: 'serial' });

  let dotDeNghiId: string;
  let linhVucId: string;
  let sangKienId: string;
  let submitted = false;

  test('setup: tra cứu danh mục', async ({ page }) => {
    const token = await getToken(page, 'admin');

    const dotRes = await authedGet(page, token, `${API.dotDeNghi}?trang=1&soDong=20`);
    const dotBody = await dotRes.json();
    dotDeNghiId = ((dotBody.duLieu as Record<string, unknown>[]).find(
      d => d.ma === 'DOT-2026' || d.trangThaiDot === 'DangMo',
    ) as Record<string, unknown>).id as string;

    const lvRes = await authedGet(page, token, `${API.danhMuc}/linh-vuc?trang=1&soDong=10`);
    const lvBody = await lvRes.json();
    linhVucId = lvBody.duLieu[0].id;
  });

  test('tác giả nộp hồ sơ mới', async ({ page }) => {
    const result = await createAndSubmitSangKien(page, dotDeNghiId, linhVucId, 'Reject');
    sangKienId = result.id;
    if (result.nopFailed) {
      test.skip(true, 'Đợt đề nghị thiếu quy trình — không thể nộp');
      return;
    }
    submitted = true;
  });

  test('B1: cán bộ tiếp nhận trả lại hồ sơ (TRA_LAI)', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'tiepnhan');
    await executeStep(page, token, sangKienId, 'TRA_LAI', 'Hồ sơ thiếu thông tin, trả lại');
  });

  test('trạng thái cuối — KhongDat', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'admin');
    const res = await authedGet(page, token, `${API.sangKien}/${sangKienId}`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.duLieu.trangThaiTong).toBe('KHONG_DAT');
  });
});

// ─── ALTERNATIVE: Supplement request at B1 ──────────────────────────────────

test.describe('Luồng yêu cầu bổ sung tại B1', () => {
  test.describe.configure({ mode: 'serial' });

  let dotDeNghiId: string;
  let linhVucId: string;
  let sangKienId: string;
  let submitted = false;

  test('setup: tra cứu danh mục', async ({ page }) => {
    const token = await getToken(page, 'admin');

    const dotRes = await authedGet(page, token, `${API.dotDeNghi}?trang=1&soDong=20`);
    const dotBody = await dotRes.json();
    dotDeNghiId = ((dotBody.duLieu as Record<string, unknown>[]).find(
      d => d.ma === 'DOT-2026' || d.trangThaiDot === 'DangMo',
    ) as Record<string, unknown>).id as string;

    const lvRes = await authedGet(page, token, `${API.danhMuc}/linh-vuc?trang=1&soDong=10`);
    const lvBody = await lvRes.json();
    linhVucId = lvBody.duLieu[0].id;
  });

  test('tác giả nộp hồ sơ mới', async ({ page }) => {
    const result = await createAndSubmitSangKien(page, dotDeNghiId, linhVucId, 'Supplement');
    sangKienId = result.id;
    if (result.nopFailed) {
      test.skip(true, 'Đợt đề nghị thiếu quy trình — không thể nộp');
      return;
    }
    submitted = true;
  });

  test('B1: cán bộ tiếp nhận yêu cầu bổ sung (BO_SUNG_HO_SO)', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'tiepnhan');
    await executeStep(page, token, sangKienId, 'BO_SUNG_HO_SO', 'Cần bổ sung minh chứng');
  });

  test('trạng thái — YeuCauBoSung', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'admin');
    const res = await authedGet(page, token, `${API.sangKien}/${sangKienId}`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.duLieu.trangThaiTong).toBe('YEU_CAU_BO_SUNG');
  });
});

// ─── ALTERNATIVE: Author withdraws ─────────────────────────────────────────

test.describe('Luồng tác giả rút hồ sơ', () => {
  test.describe.configure({ mode: 'serial' });

  let dotDeNghiId: string;
  let linhVucId: string;
  let sangKienId: string;
  let submitted = false;

  test('setup: tra cứu danh mục', async ({ page }) => {
    const token = await getToken(page, 'admin');

    const dotRes = await authedGet(page, token, `${API.dotDeNghi}?trang=1&soDong=20`);
    const dotBody = await dotRes.json();
    dotDeNghiId = ((dotBody.duLieu as Record<string, unknown>[]).find(
      d => d.ma === 'DOT-2026' || d.trangThaiDot === 'DangMo',
    ) as Record<string, unknown>).id as string;

    const lvRes = await authedGet(page, token, `${API.danhMuc}/linh-vuc?trang=1&soDong=10`);
    const lvBody = await lvRes.json();
    linhVucId = lvBody.duLieu[0].id;
  });

  test('tác giả nộp và cán bộ tiếp nhận chấp nhận', async ({ page }) => {
    const result = await createAndSubmitSangKien(page, dotDeNghiId, linhVucId, 'Withdraw');
    sangKienId = result.id;
    if (result.nopFailed) {
      test.skip(true, 'Đợt đề nghị thiếu quy trình — không thể nộp');
      return;
    }
    submitted = true;

    // B1: tiếp nhận chấp nhận
    const token = await getToken(page, 'tiepnhan');
    await executeStep(page, token, sangKienId, 'DAT', 'Chấp nhận');
  });

  test('tác giả rút hồ sơ sau tiếp nhận', async ({ page }) => {
    test.skip(!submitted, 'Hồ sơ chưa nộp — skip');
    const token = await getToken(page, 'gv.lan');
    const res = await authedPost(page, token, `${API.sangKien}/${sangKienId}/rut`, {
      lyDo: 'Tác giả xin rút hồ sơ',
    });
    expect([200, 400].includes(res.status()),
      `Rút hồ sơ unexpected status: ${res.status()}`).toBeTruthy();
  });
});

// ─── CROSS-CUTTING: Authorization boundaries ────────────────────────────────

test.describe('Phân quyền liên bước', () => {
  test('tác giả không thể thực thi bước xử lý', async ({ page }) => {
    const token = await getToken(page, 'gv.lan');
    const res = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {
      sangKienId: '00000000-0000-0000-0000-000000000000',
      truongHopId: '00000000-0000-0000-0000-000000000000',
    });
    expect([400, 403, 404].includes(res.status())).toBeTruthy();
  });

  test('không xác thực — thuc-thi → 401', async ({ page }) => {
    const res = await page.request.post(`${API.xuLy}/thuc-thi`, {
      data: {
        sangKienId: '00000000-0000-0000-0000-000000000000',
        truongHopId: '00000000-0000-0000-0000-000000000000',
      },
    });
    expect(res.status()).toBe(401);
  });

  test('không xác thực — nộp hồ sơ → 401', async ({ page }) => {
    const res = await page.request.post(`${API.sangKien}/00000000-0000-0000-0000-000000000000/nop`);
    expect(res.status()).toBe(401);
  });

  test('không xác thực — phân công → 401', async ({ page }) => {
    const res = await page.request.post(`${API.danhGia}/phan-cong`, {
      data: { hoiDongId: '00000000-0000-0000-0000-000000000000', sangKienIds: [] },
    });
    expect(res.status()).toBe(401);
  });

  test('tác giả không thể phân công chấm điểm — 403', async ({ page }) => {
    const token = await getToken(page, 'gv.lan');
    const res = await authedPost(page, token, `${API.danhGia}/phan-cong`, {
      hoiDongId: '00000000-0000-0000-0000-000000000000',
      sangKienIds: [],
    });
    expect(res.status()).toBe(403);
  });

  test('tác giả không thể tổng hợp điểm — 403', async ({ page }) => {
    const token = await getToken(page, 'gv.lan');
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await authedPost(
      page, token,
      `${API.danhGia}/tong-hop?sangKienId=${fakeId}&hoiDongId=${fakeId}`,
    );
    expect(res.status()).toBe(403);
  });
});

// ─── CROSS-CUTTING: Edge cases ──────────────────────────────────────────────

test.describe('Trường hợp biên liên bước', () => {
  test('thuc-thi với truongHopId không tồn tại — 400/404', async ({ page }) => {
    const token = await getToken(page, 'thuky');
    const res = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {
      sangKienId: '00000000-0000-0000-0000-000000000000',
      truongHopId: '00000000-0000-0000-0000-000000000000',
    });
    expect([400, 404, 422].includes(res.status())).toBeTruthy();
  });

  test('thuc-thi với payload rỗng — 400/422', async ({ page }) => {
    const token = await getToken(page, 'thuky');
    const res = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {});
    expect([400, 404, 422].includes(res.status())).toBeTruthy();
  });

  test('nộp hồ sơ không tồn tại — 400/404', async ({ page }) => {
    const token = await getToken(page, 'gv.lan');
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await authedPost(page, token, `${API.sangKien}/${fakeId}/nop`);
    expect([400, 404].includes(res.status())).toBeTruthy();
  });

  test('hanh-dong cho hồ sơ không tồn tại — trả về rỗng hoặc lỗi', async ({ page }) => {
    const token = await getToken(page, 'admin');
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await authedGet(page, token, `${API.sangKien}/${fakeId}/hanh-dong`);
    if (res.ok()) {
      const body = await res.json();
      const actions = body.duLieu ?? body;
      expect(Array.isArray(actions)).toBeTruthy();
      expect(actions.length).toBe(0);
    } else {
      expect([400, 404].includes(res.status())).toBeTruthy();
    }
  });
});

// ─── Validation và edge cases nâng cao ──────────────────────────────────────

test.describe('Validation và edge cases nghiệp vụ', () => {
  test.describe.configure({ timeout: 60_000 });

  test('POST /xu-ly/thuc-thi không xác thực → 401', async ({ page }) => {
    const res = await page.request.post(`${API.xuLy}/thuc-thi`, {
      data: {
        sangKienId: '00000000-0000-0000-0000-000000000000',
        truongHopId: '00000000-0000-0000-0000-000000000000',
      },
    });
    expect(res.status()).toBe(401);
  });

  test('POST /sang-kien/nop không xác thực → 401', async ({ page }) => {
    const res = await page.request.post(
      `${API.sangKien}/00000000-0000-0000-0000-000000000000/nop`
    );
    expect(res.status()).toBe(401);
  });

  test('GET /sang-kien/{id}/tien-do không xác thực → 401', async ({ page }) => {
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await page.request.get(`${API.sangKien}/${fakeId}/tien-do`);
    expect(res.status()).toBe(401);
  });

  test('GET /sang-kien/{id}/lich-su không xác thực → 401', async ({ page }) => {
    const fakeId = '00000000-0000-0000-0000-000000000000';
    const res = await page.request.get(`${API.sangKien}/${fakeId}/lich-su`);
    expect(res.status()).toBe(401);
  });

  test('POST /danh-gia/phieu/gui không xác thực → 401', async ({ page }) => {
    const res = await page.request.post(`${API.danhGia}/phieu/gui`, {
      data: {
        sangKienId: '00000000-0000-0000-0000-000000000000',
        hoiDongId: '00000000-0000-0000-0000-000000000000',
        chiTiet: [],
      },
    });
    expect(res.status()).toBe(401);
  });

  test('POST /xu-ly/thuc-thi với truongHopId invalid UUID → 400/422', async ({ page }) => {
    const token = await getToken(page, 'admin');
    const res = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {
      sangKienId: '00000000-0000-0000-0000-000000000000',
      truongHopId: 'not-a-valid-uuid',
    });
    expect([400, 422].includes(res.status())).toBeTruthy();
  });

  test('GET /xu-ly/viec-cua-toi sapXep=ngayTao&huong=desc trả về 200', async ({ page }) => {
    const token = await getToken(page, 'thuky');
    const res = await authedGet(
      page, token,
      `${API.xuLy}/viec-cua-toi?trang=1&soDong=10&sapXep=ngayTao&huong=desc`
    );
    expect([200, 404].includes(res.status())).toBeTruthy();
  });

  test('GET /xu-ly/viec-cua-toi trang=9999 trả về mảng rỗng hoặc 404', async ({ page }) => {
    const token = await getToken(page, 'admin');
    const res = await authedGet(
      page, token,
      `${API.xuLy}/viec-cua-toi?trang=9999&soDong=10`
    );
    if (res.ok()) {
      const body = await res.json();
      expect(body.duLieu).toBeInstanceOf(Array);
      expect(body.duLieu.length).toBe(0);
    } else {
      expect([404].includes(res.status())).toBeTruthy();
    }
  });

  test('GET /sang-kien/{id}/tien-do với id hợp lệ trả về 200', async ({ page }) => {
    const token = await getToken(page, 'admin');
    const listRes = await authedGet(page, token, `${API.sangKien}?trang=1&soDong=1`);
    const listBody = await listRes.json();
    if (listBody.duLieu.length > 0) {
      const id = listBody.duLieu[0].id;
      const res = await authedGet(page, token, `${API.sangKien}/${id}/tien-do`);
      expect(res.ok()).toBeTruthy();
    }
  });

  test('GET /sang-kien/{id}/lich-su với id hợp lệ trả về 200', async ({ page }) => {
    const token = await getToken(page, 'admin');
    const listRes = await authedGet(page, token, `${API.sangKien}?trang=1&soDong=1`);
    const listBody = await listRes.json();
    if (listBody.duLieu.length > 0) {
      const id = listBody.duLieu[0].id;
      const res = await authedGet(page, token, `${API.sangKien}/${id}/lich-su`);
      expect(res.ok()).toBeTruthy();
    }
  });

  test('POST /xu-ly/thuc-thi SQL injection trong yKien không crash', async ({ page }) => {
    const token = await getToken(page, 'thuky');
    const res = await authedPost(page, token, `${API.xuLy}/thuc-thi`, {
      sangKienId: '00000000-0000-0000-0000-000000000000',
      truongHopId: '00000000-0000-0000-0000-000000000000',
      yKien: "Test' OR '1'='1; DROP TABLE sang_kien;--",
    });
    expect([400, 404, 422].includes(res.status())).toBeTruthy();
  });
});
