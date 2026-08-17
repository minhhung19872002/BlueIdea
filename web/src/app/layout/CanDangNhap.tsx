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

  return <>{children}</>;
}
