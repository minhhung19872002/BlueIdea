import { create } from 'zustand';
import { persist } from 'zustand/middleware';

import { boNhoToken, guiDuLieu, layDuLieu } from '@/api/client';
import { dungRealtime } from '@/api/realtime';

export interface NguoiDung {
  id: string;
  tenDangNhap: string;
  hoTen: string;
  email?: string | null;
  chucVu?: string | null;
  donViId?: string | null;
  tenDonVi?: string | null;
  vaiTro: string[];
  quyen: string[];
  mfaEnabled: boolean;
  /** Đang bị buộc bật xác thực hai lớp trước khi dùng các chức năng khác. */
  buocBatMfa?: boolean;
}

interface KetQuaDangNhap {
  accessToken: string;
  refreshToken: string;
  hetHanSau: number;
  nguoiDung: NguoiDung;
  buocDoiMatKhau: boolean;
}

interface TrangThaiAuth {
  nguoiDung: NguoiDung | null;
  buocDoiMatKhau: boolean;
  dangTai: boolean;
  daKhoiTao: boolean;

  dangNhap: (
    tenDangNhap: string,
    matKhau: string,
    them?: { maMfa?: string; captchaId?: string; captchaLoiGiai?: string },
  ) => Promise<void>;
  dangXuat: () => Promise<void>;
  napLaiThongTin: () => Promise<void>;
  coQuyen: (maQuyen: string | string[]) => boolean;
  coVaiTro: (maVaiTro: string | string[]) => boolean;
}

const VAI_TRO_QUAN_TRI = 'QUAN_TRI_HE_THONG';

export const useAuthStore = create<TrangThaiAuth>()(
  persist(
    (set, get) => ({
      nguoiDung: null,
      buocDoiMatKhau: false,
      dangTai: false,
      daKhoiTao: false,

      async dangNhap(tenDangNhap, matKhau, them) {
        set({ dangTai: true });
        try {
          const ketQua = await guiDuLieu<KetQuaDangNhap>('/api/v1/xac-thuc/dang-nhap', {
            tenDangNhap,
            matKhau,
            ...them,
          });

          boNhoToken.luu(ketQua.accessToken, ketQua.refreshToken);
          set({
            nguoiDung: ketQua.nguoiDung,
            buocDoiMatKhau: ketQua.buocDoiMatKhau,
            daKhoiTao: true,
          });
        } finally {
          set({ dangTai: false });
        }
      },

      async dangXuat() {
        try {
          await guiDuLieu('/api/v1/xac-thuc/dang-xuat', {
            refreshToken: boNhoToken.layRefreshToken(),
          });
        } catch {
          // Kể cả khi gọi API thất bại vẫn phải xoá phiên phía client.
        } finally {
          // Ngắt realtime trước khi xoá token: kết nối cũ mang token đã thu hồi.
          await dungRealtime();
          boNhoToken.xoa();
          set({ nguoiDung: null, buocDoiMatKhau: false });
        }
      },

      async napLaiThongTin() {
        if (!boNhoToken.layAccessToken()) {
          set({ nguoiDung: null, daKhoiTao: true });
          return;
        }

        try {
          const nguoiDung = await layDuLieu<NguoiDung>('/api/v1/xac-thuc/toi');
          set({ nguoiDung, daKhoiTao: true });
        } catch {
          boNhoToken.xoa();
          set({ nguoiDung: null, daKhoiTao: true });
        }
      },

      coQuyen(maQuyen) {
        const nguoiDung = get().nguoiDung;
        if (!nguoiDung) return false;
        if (nguoiDung.vaiTro.includes(VAI_TRO_QUAN_TRI)) return true;

        const danhSach = Array.isArray(maQuyen) ? maQuyen : [maQuyen];
        return danhSach.some((q) => nguoiDung.quyen.includes(q));
      },

      coVaiTro(maVaiTro) {
        const nguoiDung = get().nguoiDung;
        if (!nguoiDung) return false;

        const danhSach = Array.isArray(maVaiTro) ? maVaiTro : [maVaiTro];
        return danhSach.some((v) => nguoiDung.vaiTro.includes(v));
      },
    }),
    {
      name: 'blueidea.auth',
      partialize: (trangThai) => ({
        nguoiDung: trangThai.nguoiDung,
        buocDoiMatKhau: trangThai.buocDoiMatKhau,
      }),
    },
  ),
);
