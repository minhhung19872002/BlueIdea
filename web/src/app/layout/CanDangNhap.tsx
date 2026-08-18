import { useEffect } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { Spin } from 'antd';

import { useAuthStore } from '@/app/store/authStore';
import { useCauHinhStore } from '@/app/store/cauHinhStore';

/** Chặn truy cập khi chưa đăng nhập và ép đổi mật khẩu lần đầu. */
export function CanDangNhap({ children }: { children: React.ReactNode }) {
  const viTri = useLocation();
  const { nguoiDung, daKhoiTao, buocDoiMatKhau, napLaiThongTin } = useAuthStore();
  const napCauHinh = useCauHinhStore((s) => s.napCauHinhCongKhai);
  const napMenu = useCauHinhStore((s) => s.napMenu);

  useEffect(() => {
    if (!daKhoiTao) {
      void napLaiThongTin();
    }
  }, [daKhoiTao, napLaiThongTin]);

  useEffect(() => {
    if (nguoiDung) {
      void napCauHinh();
      void napMenu();
    }
  }, [nguoiDung, napCauHinh, napMenu]);

  if (!daKhoiTao) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', paddingTop: 120 }}>
        <Spin size="large" tip="Đang kiểm tra phiên đăng nhập..." />
      </div>
    );
  }

  if (!nguoiDung) {
    return <Navigate to={`/dang-nhap?tiepTuc=${encodeURIComponent(viTri.pathname)}`} replace />;
  }

  // Tài khoản mới bắt buộc đổi mật khẩu trước khi dùng các chức năng khác.
  if (buocDoiMatKhau && viTri.pathname !== '/doi-mat-khau') {
    return <Navigate to="/doi-mat-khau" replace />;
  }

  /*
   * Quản trị viên bắt buộc bật xác thực hai lớp (Mục 6 đặc tả, bật/tắt trong cấu hình).
   *
   * Đưa sang màn hình cài đặt chứ không chặn đăng nhập: chặn đăng nhập thì họ không còn đường nào
   * vào để bật, tức là khoá cứng cả hệ thống. Vẫn cho vào trang đổi mật khẩu vì hai ràng buộc này
   * có thể cùng lúc áp lên một tài khoản mới.
   */
  const duocVaoKhiThieuMfa = ['/bao-mat-tai-khoan', '/doi-mat-khau'];

  if (nguoiDung.buocBatMfa && !duocVaoKhiThieuMfa.includes(viTri.pathname)) {
    return <Navigate to="/bao-mat-tai-khoan" replace />;
  }

  return <>{children}</>;
}
