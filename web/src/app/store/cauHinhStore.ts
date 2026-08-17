import { create } from 'zustand';

import { layDuLieu } from '@/api/client';

export interface MucMenu {
  id: string;
  ma: string;
  ten: string;
  icon?: string | null;
  duongDan?: string | null;
  thuTu: number;
  moTabMoi: boolean;
  menuCon: MucMenu[];
}

interface TrangThaiCauHinh {
  tenHeThong: string;
  tenDonVi: string;
  mauChuDao: string;
  emailHoTro: string;
  dienThoaiHoTro: string;
  menu: MucMenu[];
  dangTaiMenu: boolean;

  napCauHinhCongKhai: () => Promise<void>;
  napMenu: () => Promise<void>;
}

/**
 * Cấu hình giao diện đọc từ API `cau_hinh_he_thong` — không hardcode màu/tên
 * (yêu cầu Mục 9 đặc tả).
 */
export const useCauHinhStore = create<TrangThaiCauHinh>((set) => ({
  tenHeThong: 'Nền tảng Sáng kiến',
  tenDonVi: '',
  mauChuDao: '#1677ff',
  emailHoTro: '',
  dienThoaiHoTro: '',
  menu: [],
  dangTaiMenu: false,

  async napCauHinhCongKhai() {
    try {
      const cauHinh = await layDuLieu<Record<string, string>>('/api/v1/he-thong/cau-hinh-cong-khai');

      set({
        tenHeThong: cauHinh.TEN_HE_THONG ?? 'Nền tảng Sáng kiến',
        tenDonVi: cauHinh.TEN_DON_VI ?? '',
        mauChuDao: cauHinh.MAU_CHU_DAO ?? '#1677ff',
        emailHoTro: cauHinh.EMAIL_HO_TRO ?? '',
        dienThoaiHoTro: cauHinh.DIEN_THOAI_HO_TRO ?? '',
      });
    } catch {
      // Không chặn ứng dụng nếu chưa lấy được cấu hình — dùng giá trị mặc định.
    }
  },

  async napMenu() {
    set({ dangTaiMenu: true });
    try {
      const menu = await layDuLieu<MucMenu[]>('/api/v1/xac-thuc/menu?loai=WEB');
      set({ menu });
    } catch {
      set({ menu: [] });
    } finally {
      set({ dangTaiMenu: false });
    }
  },
}));
