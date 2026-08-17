import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom';
import { Result, Spin } from 'antd';

import { BoCucChinh } from '@/app/layout/BoCucChinh';
import { CanDangNhap } from '@/app/layout/CanDangNhap';

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
const TrangCongKhai = lazy(() => import('@/features/cong-khai/TrangCongKhai'));

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
    children: [
      { index: true, element: <ChoTai><TrangDashboard /></ChoTai> },
      { path: 'doi-mat-khau', element: <ChoTai><TrangDoiMatKhau /></ChoTai> },

      { path: 'sang-kien/cua-toi', element: <ChoTai><TrangHoSoCuaToi /></ChoTai> },
      { path: 'sang-kien/nop-moi', element: <ChoTai><TrangNopHoSo /></ChoTai> },
      { path: 'sang-kien/:id/sua', element: <ChoTai><TrangNopHoSo /></ChoTai> },
      { path: 'sang-kien/:id', element: <ChoTai><TrangChiTietHoSo /></ChoTai> },

      { path: 'tiep-nhan', element: <ChoTai><TrangDanhSachXuLy /></ChoTai> },
      { path: 'xu-ly', element: <ChoTai><TrangDanhSachXuLy /></ChoTai> },

      { path: 'danh-gia', element: <ChoTai><TrangViecDanhGia /></ChoTai> },
      { path: 'danh-gia/:id/cham-diem', element: <ChoTai><TrangChamDiem /></ChoTai> },

      { path: 'tra-cuu', element: <ChoTai><TrangTraCuu /></ChoTai> },
      { path: 'bao-cao/:loai', element: <ChoTai><TrangBaoCao /></ChoTai> },

      { path: 'quan-tri/danh-muc/:ma', element: <ChoTai><TrangDanhMuc /></ChoTai> },
      { path: 'quan-tri/quy-trinh', element: <ChoTai><TrangQuyTrinh /></ChoTai> },
      { path: 'quan-tri/quy-trinh/:id/thiet-ke', element: <ChoTai><TrangThietKeQuyTrinh /></ChoTai> },
      { path: 'quan-tri/tieu-chi', element: <ChoTai><TrangTieuChi /></ChoTai> },
      { path: 'quan-tri/tieu-chi/:id', element: <ChoTai><TrangCauHinhTieuChi /></ChoTai> },
      { path: 'quan-tri/nguoi-dung', element: <ChoTai><TrangNguoiDung /></ChoTai> },
      { path: 'quan-tri/don-vi', element: <ChoTai><TrangDonVi /></ChoTai> },
      { path: 'quan-tri/vai-tro', element: <ChoTai><TrangVaiTro /></ChoTai> },
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
