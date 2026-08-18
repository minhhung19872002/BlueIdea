import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button, Card, Result, Spin } from 'antd';

import { boNhoToken, LoiApi } from '@/api/client';
import { apiSso } from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { KHOA_SSO } from '@/features/xac-thuc/sso';

/**
 * Chức năng 21, 41 — Trang nhận mã trả về từ nhà cung cấp SSO.
 *
 * `state` và `codeVerifier` do CHÍNH trang này sinh ra ở bước bắt đầu và giữ trong
 * sessionStorage; máy chủ không lưu phiên tạm nên chạy được sau cân bằng tải.
 */
export default function TrangSsoTraVe() {
  const [thamSo] = useSearchParams();
  const dieuHuong = useNavigate();
  const napLaiThongTin = useAuthStore((s) => s.napLaiThongTin);

  const [loi, setLoi] = useState<string | null>(null);
  const daChay = useRef(false);

  useEffect(() => {
    // React 18 StrictMode gọi effect hai lần — authorization code chỉ đổi được MỘT lần,
    // lần thứ hai nhà cung cấp sẽ từ chối nên phải tự chặn.
    if (daChay.current) return;
    daChay.current = true;

    const loiSso = thamSo.get('error');
    const code = thamSo.get('code');
    const state = thamSo.get('state');

    const stateDaLuu = sessionStorage.getItem(KHOA_SSO.state);
    const codeVerifier = sessionStorage.getItem(KHOA_SSO.codeVerifier);
    const tiepTuc = sessionStorage.getItem(KHOA_SSO.tiepTuc) ?? '/';

    if (loiSso) {
      setLoi(`Nhà cung cấp SSO từ chối đăng nhập: ${loiSso}`);
      return;
    }

    if (!code || !state || !stateDaLuu || !codeVerifier) {
      setLoi('Thiếu dữ liệu phiên đăng nhập SSO. Vui lòng đăng nhập lại từ đầu.');
      return;
    }

    if (state !== stateDaLuu) {
      // Khác state = phản hồi không thuộc phiên này (dấu hiệu tấn công CSRF).
      setLoi('Phiên đăng nhập SSO không khớp. Vui lòng thử lại.');
      return;
    }

    void (async () => {
      try {
        const ketQua = await apiSso.doiMa({
          code,
          codeVerifier,
          duongDanTraVe: `${window.location.origin}/dang-nhap/sso`,
          state,
        });

        boNhoToken.luu(ketQua.accessToken, ketQua.refreshToken);
        localStorage.setItem(KHOA_SSO.dangNhapBangSso, '1');
        await napLaiThongTin();

        dieuHuong(tiepTuc, { replace: true });
      } catch (e) {
        setLoi(e instanceof LoiApi ? e.message : 'Không hoàn tất được đăng nhập SSO.');
      } finally {
        sessionStorage.removeItem(KHOA_SSO.state);
        sessionStorage.removeItem(KHOA_SSO.codeVerifier);
        sessionStorage.removeItem(KHOA_SSO.tiepTuc);
      }
    })();
  }, [thamSo, dieuHuong, napLaiThongTin]);

  return (
    <div className="trang-dang-nhap">
      <Card style={{ maxWidth: 460, width: '100%' }}>
        {loi ? (
          <Result
            status="error"
            title="Đăng nhập một lần không thành công"
            subTitle={loi}
            extra={
              <Button type="primary" onClick={() => dieuHuong('/dang-nhap', { replace: true })}>
                Về trang đăng nhập
              </Button>
            }
          />
        ) : (
          <div style={{ textAlign: 'center', padding: 32 }}>
            <Spin size="large" />
            <div style={{ marginTop: 16 }}>Đang hoàn tất đăng nhập một lần...</div>
          </div>
        )}
      </Card>
    </div>
  );
}
