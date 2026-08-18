import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom';
import { Result, Spin } from 'antd';

import { BoCucChinh } from '@/app/layout/BoCucChinh';
import { CanDangNhap } from '@/app/layout/CanDangNhap';
import { TrangLoiRoute } from '@/app/layout/TrangLoiRoute';

/** Bọc lazy route bằng skeleton để tránh nháy màn hình trắng. */
function ChoTai({ children }: { children: React.ReactNode }) {
  return (
    <Suspense
      fallback={
        <div style={{ display: 'flex', justifyContent: 'center', padding: 64 }}>
          <Spin size="large" tip="Đang tải..." />
        </div>
      }
    >
      {children}
    </Suspense>
  );
}

const TrangDangNhap = lazy(() => import('@/features/xac-thuc/TrangDangNhap'));
const TrangDoiMatKhau = lazy(() => import('@/features/xac-thuc/TrangDoiMatKhau'));
const TrangDashboard = lazy(() => import('@/features/dashboard/TrangDashboard'));
const TrangHoSoCuaToi = lazy(() => import('@/features/sang-kien/TrangHoSoCuaToi'));
const TrangNopHoSo = lazy(() => import('@/features/sang-kien/TrangNopHoSo'));
const TrangChiTietHoSo = lazy(() => import('@/features/sang-kien/TrangChiTietHoSo'));
const TrangDanhSachXuLy = lazy(() => import('@/features/xu-ly/TrangDanhSachXuLy'));
const TrangViecDanhGia = lazy(() => import('@/features/danh-gia/TrangViecDanhGia'));
const TrangChamDiem = lazy(() => import('@/features/danh-gia/TrangChamDiem'));
const TrangTraCuu = lazy(() => import('@/features/tra-cuu/TrangTraCuu'));
const TrangBaoCao = lazy(() => import('@/features/bao-cao/TrangBaoCao'));
const TrangDanhMuc = lazy(() => import('@/features/quan-tri/TrangDanhMuc'));
const TrangQuyTrinh = lazy(() => import('@/features/quy-trinh/TrangQuyTrinh'));
const TrangThietKeQuyTrinh = lazy(() => import('@/features/quy-trinh/TrangThietKeQuyTrinh'));
const TrangTieuChi = lazy(() => import('@/features/tieu-chi/TrangTieuChi'));
const TrangCauHinhTieuChi = lazy(() => import('@/features/tieu-chi/TrangCauHinhTieuChi'));
const TrangNguoiDung = lazy(() => import('@/features/quan-tri/TrangNguoiDung'));
const TrangDonVi = lazy(() => import('@/features/quan-tri/TrangDonVi'));
const TrangVaiTro = lazy(() => import('@/features/quan-tri/TrangVaiTro'));
const TrangCauHinhHeThong = lazy(() => import('@/features/quan-tri/TrangCauHinhHeThong'));
const TrangNhatKy = lazy(() => import('@/features/quan-tri/TrangNhatKy'));
const TrangQuyetDinh = lazy(() => import('@/features/quyet-dinh/TrangQuyetDinh'));
const TrangBaoCaoTuyBien = lazy(() => import('@/features/bao-cao/TrangBaoCaoTuyBien'));
const TrangCauHinhGuiTin = lazy(() => import('@/features/quan-tri/TrangCauHinhGuiTin'));
const TrangCongKhai = lazy(() => import('@/features/cong-khai/TrangCongKhai'));
const TrangQuenMatKhau = lazy(() => import('@/features/xac-thuc/TrangQuenMatKhau'));
const TrangBaoMatTaiKhoan = lazy(() => import('@/features/xac-thuc/TrangBaoMatTaiKhoan'));
const TrangKhoaApiNgoai = lazy(() => import('@/features/quan-tri/TrangKhoaApiNgoai'));
const TrangHoiDong = lazy(() => import('@/features/hoi-dong/TrangHoiDong'));
const TrangChiTietHoiDong = lazy(() => import('@/features/hoi-dong/TrangChiTietHoiDong'));
const TrangLienThong = lazy(() => import('@/features/quan-tri/TrangLienThong'));
const TrangBieuMauXuat = lazy(() => import('@/features/quan-tri/TrangBieuMauXuat'));
const TrangDotDeNghi = lazy(() => import('@/features/quan-tri/TrangDotDeNghi'));
const TrangChiTietDot = lazy(() => import('@/features/quan-tri/TrangChiTietDot'));
const TrangCauHinhMenu = lazy(() => import('@/features/quan-tri/TrangCauHinhMenu'));
const TrangMauThongBao = lazy(() => import('@/features/quan-tri/TrangMauThongBao'));
const TrangNgayNghiLe = lazy(() => import('@/features/quan-tri/TrangNgayNghiLe'));
const TrangChuKySo = lazy(() => import('@/features/quan-tri/TrangChuKySo'));
const TrangThanhPhanHoSo = lazy(() => import('@/features/quy-trinh/TrangThanhPhanHoSo'));
const TrangLienThongBuoc = lazy(() => import('@/features/quy-trinh/TrangLienThongBuoc'));
const TrangCapPheDuyet = lazy(() => import('@/features/quan-tri/TrangCapPheDuyet'));
const TrangNhatKyLoi = lazy(() => import('@/features/quan-tri/TrangNhatKyLoi'));
const TrangSsoTraVe = lazy(() => import('@/features/xac-thuc/TrangSsoTraVe'));

export const router = createBrowserRouter([
  {
    path: '/dang-nhap',
    element: (
      <ChoTai>
        <TrangDangNhap />
      </ChoTai>
    ),
  },
  {
    path: '/dang-nhap/sso',
    element: (
      <ChoTai>
        <TrangSsoTraVe />
      </ChoTai>
    ),
  },
  {
    path: '/quen-mat-khau',
    element: (
      <ChoTai>
        <TrangQuenMatKhau />
      </ChoTai>
    ),
  },
  {
    path: '/cong-khai/tra-cuu',
    element: (
      <ChoTai>
        <TrangCongKhai />
      </ChoTai>
    ),
  },
  {
    element: (
      <CanDangNhap>
        <BoCucChinh />
      </CanDangNhap>
    ),
    // Lỗi hiển thị ở bất kỳ trang con nào cũng được bắt tại đây, không để lộ stack trace.
    errorElement: <TrangLoiRoute />,
    children: [
      { index: true, element: <ChoTai><TrangDashboard /></ChoTai> },
      { path: 'doi-mat-khau', element: <ChoTai><TrangDoiMatKhau /></ChoTai> },
      { path: 'bao-mat-tai-khoan', element: <ChoTai><TrangBaoMatTaiKhoan /></ChoTai> },
      { path: 'quan-tri/khoa-api', element: <ChoTai><TrangKhoaApiNgoai /></ChoTai> },

      { path: 'sang-kien/cua-toi', element: <ChoTai><TrangHoSoCuaToi /></ChoTai> },
      { path: 'sang-kien/nop-moi', element: <ChoTai><TrangNopHoSo /></ChoTai> },
      { path: 'sang-kien/:id/sua', element: <ChoTai><TrangNopHoSo /></ChoTai> },
      { path: 'sang-kien/:id', element: <ChoTai><TrangChiTietHoSo /></ChoTai> },

      { path: 'tiep-nhan', element: <ChoTai><TrangDanhSachXuLy /></ChoTai> },
      { path: 'xu-ly', element: <ChoTai><TrangDanhSachXuLy /></ChoTai> },

      { path: 'danh-gia', element: <ChoTai><TrangViecDanhGia /></ChoTai> },
      { path: 'danh-gia/:id/cham-diem', element: <ChoTai><TrangChamDiem /></ChoTai> },

      { path: 'hoi-dong', element: <ChoTai><TrangHoiDong /></ChoTai> },
      { path: 'hoi-dong/:id', element: <ChoTai><TrangChiTietHoiDong /></ChoTai> },

      { path: 'quyet-dinh', element: <ChoTai><TrangQuyetDinh /></ChoTai> },

      { path: 'tra-cuu', element: <ChoTai><TrangTraCuu /></ChoTai> },
      { path: 'bao-cao/tuy-bien', element: <ChoTai><TrangBaoCaoTuyBien /></ChoTai> },
      { path: 'bao-cao/:loai', element: <ChoTai><TrangBaoCao /></ChoTai> },

      { path: 'quan-tri/danh-muc/bieu-mau-xuat', element: <ChoTai><TrangBieuMauXuat /></ChoTai> },
      { path: 'quan-tri/danh-muc/dot-de-nghi', element: <ChoTai><TrangDotDeNghi /></ChoTai> },
      { path: 'quan-tri/danh-muc/:ma', element: <ChoTai><TrangDanhMuc /></ChoTai> },
      { path: 'quan-tri/lien-thong', element: <ChoTai><TrangLienThong /></ChoTai> },
      { path: 'quan-tri/chu-ky-so', element: <ChoTai><TrangChuKySo /></ChoTai> },
      { path: 'quan-tri/mau-thong-bao', element: <ChoTai><TrangMauThongBao /></ChoTai> },
      { path: 'quan-tri/danh-muc/dot-de-nghi/:id', element: <ChoTai><TrangChiTietDot /></ChoTai> },
      { path: 'quan-tri/cau-hinh/menu', element: <ChoTai><TrangCauHinhMenu /></ChoTai> },
      { path: 'quan-tri/danh-muc/ngay-nghi-le', element: <ChoTai><TrangNgayNghiLe /></ChoTai> },
      { path: 'quan-tri/danh-muc/cap-phe-duyet', element: <ChoTai><TrangCapPheDuyet /></ChoTai> },
      { path: 'quan-tri/nhat-ky-loi', element: <ChoTai><TrangNhatKyLoi /></ChoTai> },
      { path: 'quan-tri/quy-trinh', element: <ChoTai><TrangQuyTrinh /></ChoTai> },
      { path: 'quan-tri/quy-trinh/:id/thiet-ke', element: <ChoTai><TrangThietKeQuyTrinh /></ChoTai> },
      { path: 'quan-tri/quy-trinh/:id/thanh-phan', element: <ChoTai><TrangThanhPhanHoSo /></ChoTai> },
      { path: 'quan-tri/quy-trinh/:id/lien-thong', element: <ChoTai><TrangLienThongBuoc /></ChoTai> },
      { path: 'quan-tri/tieu-chi', element: <ChoTai><TrangTieuChi /></ChoTai> },
      { path: 'quan-tri/tieu-chi/:id', element: <ChoTai><TrangCauHinhTieuChi /></ChoTai> },
      { path: 'quan-tri/nguoi-dung', element: <ChoTai><TrangNguoiDung /></ChoTai> },
      { path: 'quan-tri/don-vi', element: <ChoTai><TrangDonVi /></ChoTai> },
      { path: 'quan-tri/vai-tro', element: <ChoTai><TrangVaiTro /></ChoTai> },
      { path: 'quan-tri/gui-tin', element: <ChoTai><TrangCauHinhGuiTin /></ChoTai> },
      { path: 'quan-tri/cau-hinh/:nhom', element: <ChoTai><TrangCauHinhHeThong /></ChoTai> },
      { path: 'quan-tri/nhat-ky/:loai', element: <ChoTai><TrangNhatKy /></ChoTai> },

      { path: '*', element: <TrangKhongTonTai /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
]);

function TrangKhongTonTai() {
  return (
    <Result
      status="404"
      title="404"
      subTitle="Trang bạn tìm không tồn tại hoặc bạn không có quyền truy cập."
    />
  );
}

export { Outlet };
