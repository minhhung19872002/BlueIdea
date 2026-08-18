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
  /** Đã cấu hình logo hay chưa — chưa thì giao diện dùng chữ viết tắt như trước. */
  coLogo: boolean;
  menu: MucMenu[];
  dangTaiMenu: boolean;

  napCauHinhCongKhai: () => Promise<void>;
  napMenu: () => Promise<void>;
}

/**
 * Cấu hình giao diện đọc từ API `cau_hinh_he_thong` — không hardcode màu/tên
 * (yêu cầu Mục 9 đặc tả).
 */
/** Địa chỉ ảnh thương hiệu — máy chủ trả 204 khi chưa cấu hình nên ảnh sẽ không hiện. */
export const DUONG_DAN_LOGO = `${import.meta.env.VITE_API_URL ?? ''}/api/v1/he-thong/anh-thuong-hieu/logo`;

const DUONG_DAN_FAVICON = `${import.meta.env.VITE_API_URL ?? ''}/api/v1/he-thong/anh-thuong-hieu/favicon`;

/**
 * Đổi biểu tượng trên thẻ trình duyệt.
 *
 * Phải làm bằng tay vì thẻ &lt;link rel="icon"&gt; nằm trong index.html tĩnh, React không đụng tới.
 * Chưa cấu hình thì giữ nguyên biểu tượng mặc định đi kèm bản dựng.
 */
function apDungFavicon(coFavicon: boolean) {
  if (!coFavicon || typeof document === 'undefined') return;

  const the =
    document.querySelector<HTMLLinkElement>("link[rel~='icon']") ??
    document.head.appendChild(Object.assign(document.createElement('link'), { rel: 'icon' }));

  the.href = DUONG_DAN_FAVICON;
}

export const useCauHinhStore = create<TrangThaiCauHinh>((set) => ({
  tenHeThong: 'Nền tảng Sáng kiến',
  tenDonVi: '',
  mauChuDao: '#1677ff',
  emailHoTro: '',
  dienThoaiHoTro: '',
  coLogo: false,
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
        coLogo: !!cauHinh.LOGO_ID,
      });

      apDungFavicon(!!cauHinh.FAVICON_ID);
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
