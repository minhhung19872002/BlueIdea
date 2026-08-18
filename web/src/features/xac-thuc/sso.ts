import { apiSso } from '@/api/endpoints';

/** Khoá lưu tạm dữ liệu luồng SSO ở phía trình duyệt. */
export const KHOA_SSO = {
  /** Chống CSRF: so lại khi nhà cung cấp trả về. */
  state: 'blueidea.sso.state',
  /** PKCE code_verifier — chỉ trình duyệt này biết. */
  codeVerifier: 'blueidea.sso.codeVerifier',
  /** Đường dẫn người dùng muốn vào trước khi bị chuyển sang đăng nhập. */
  tiepTuc: 'blueidea.sso.tiepTuc',
  /** Đánh dấu phiên hiện tại đăng nhập bằng SSO — dùng cho single logout. */
  dangNhapBangSso: 'blueidea.sso.dangNhapBangSso',
};

/** Địa chỉ trang nhận mã trả về — phải trùng redirect_uri đã đăng ký với nhà cung cấp. */
export function duongDanTraVeSso() {
  return `${window.location.origin}/dang-nhap/sso`;
}

/**
 * Bắt đầu luồng SSO: xin state + PKCE từ máy chủ, giữ lại phía trình duyệt rồi chuyển
 * hướng sang nhà cung cấp.
 */
export async function batDauDangNhapSso(tiepTuc: string) {
  const ketQua = await apiSso.batDau(duongDanTraVeSso());

  sessionStorage.setItem(KHOA_SSO.state, ketQua.state);
  sessionStorage.setItem(KHOA_SSO.codeVerifier, ketQua.codeVerifier);
  sessionStorage.setItem(KHOA_SSO.tiepTuc, tiepTuc);

  window.location.href = ketQua.diaChi;
}

/**
 * Chức năng 41 — Single logout.
 *
 * Gọi TRƯỚC khi xoá token cục bộ vì endpoint này yêu cầu đăng nhập. Trả về `null` khi hệ thống
 * không dùng SSO hoặc nhà cung cấp không công bố `end_session_endpoint`; khi đó chỉ đăng xuất
 * cục bộ là đúng.
 */
export async function layDiaChiDangXuatSso(): Promise<string | null> {
  if (localStorage.getItem(KHOA_SSO.dangNhapBangSso) !== '1') return null;

  try {
    const ketQua = await apiSso.diaChiDangXuat({
      duongDanTraVe: `${window.location.origin}/dang-nhap`,
    });

    return ketQua.diaChi;
  } catch {
    // Không chặn đăng xuất chỉ vì không lấy được địa chỉ kết thúc phiên bên nhà cung cấp.
    return null;
  } finally {
    localStorage.removeItem(KHOA_SSO.dangNhapBangSso);
  }
}
