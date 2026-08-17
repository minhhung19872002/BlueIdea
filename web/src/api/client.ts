import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';

/** Cấu trúc phản hồi chuẩn của toàn hệ thống (Mục 8 đặc tả). */
export interface PhanHoiApi<T> {
  thanhCong: boolean;
  duLieu: T;
  thongBao: string;
  maLoi: string | null;
  chiTietLoi: ChiTietLoi[];
}

export interface ChiTietLoi {
  truong: string;
  thongBao: string;
}

/** Phản hồi phân trang chuẩn. */
export interface PhanHoiPhanTrang<T> {
  duLieu: T[];
  tongSo: number;
  trang: number;
  soDong: number;
  tongTrang: number;
}

/** Lỗi nghiệp vụ đã chuẩn hoá — UI chỉ cần đọc `maLoi` và `thongBao`. */
export class LoiApi extends Error {
  constructor(
    public readonly maLoi: string,
    thongBao: string,
    public readonly chiTietLoi: ChiTietLoi[] = [],
    public readonly maHttp?: number,
  ) {
    super(thongBao);
    this.name = 'LoiApi';
  }
}

const KHOA_ACCESS_TOKEN = 'blueidea.accessToken';
const KHOA_REFRESH_TOKEN = 'blueidea.refreshToken';

export const boNhoToken = {
  layAccessToken: () => localStorage.getItem(KHOA_ACCESS_TOKEN),
  layRefreshToken: () => localStorage.getItem(KHOA_REFRESH_TOKEN),
  luu(accessToken: string, refreshToken: string) {
    localStorage.setItem(KHOA_ACCESS_TOKEN, accessToken);
    localStorage.setItem(KHOA_REFRESH_TOKEN, refreshToken);
  },
  xoa() {
    localStorage.removeItem(KHOA_ACCESS_TOKEN);
    localStorage.removeItem(KHOA_REFRESH_TOKEN);
  },
};

export const http: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  timeout: 60_000,
  headers: { 'Content-Type': 'application/json' },
});

http.interceptors.request.use((cauHinh: InternalAxiosRequestConfig) => {
  const token = boNhoToken.layAccessToken();
  if (token) {
    cauHinh.headers.Authorization = `Bearer ${token}`;
  }
  return cauHinh;
});

// --- Tự động làm mới token khi gặp 401 -------------------------------------
let dangLamMoi = false;
let hangDoiCho: Array<(token: string | null) => void> = [];

function giaiPhongHangDoi(token: string | null) {
  hangDoiCho.forEach((tiepTuc) => tiepTuc(token));
  hangDoiCho = [];
}

http.interceptors.response.use(
  (phanHoi) => phanHoi,
  async (loi: AxiosError<PhanHoiApi<unknown>>) => {
    const yeuCauGoc = loi.config as InternalAxiosRequestConfig & { _daThuLai?: boolean };
    const maHttp = loi.response?.status;

    const laYeuCauXacThuc = yeuCauGoc?.url?.includes('/xac-thuc/');

    if (maHttp === 401 && yeuCauGoc && !yeuCauGoc._daThuLai && !laYeuCauXacThuc) {
      const refreshToken = boNhoToken.layRefreshToken();

      if (!refreshToken) {
        boNhoToken.xoa();
        chuyenVeDangNhap();
        return Promise.reject(taoLoiApi(loi));
      }

      if (dangLamMoi) {
        // Chờ phiên làm mới đang chạy thay vì gọi trùng nhiều lần.
        return new Promise((giaiQuyet, tuChoi) => {
          hangDoiCho.push((token) => {
            if (!token) {
              tuChoi(taoLoiApi(loi));
              return;
            }
            yeuCauGoc.headers.Authorization = `Bearer ${token}`;
            giaiQuyet(http(yeuCauGoc));
          });
        });
      }

      yeuCauGoc._daThuLai = true;
      dangLamMoi = true;

      try {
        const { data } = await axios.post<
          PhanHoiApi<{ accessToken: string; refreshToken: string }>
        >(`${http.defaults.baseURL ?? ''}/api/v1/xac-thuc/lam-moi-token`, { refreshToken });

        boNhoToken.luu(data.duLieu.accessToken, data.duLieu.refreshToken);
        giaiPhongHangDoi(data.duLieu.accessToken);

        yeuCauGoc.headers.Authorization = `Bearer ${data.duLieu.accessToken}`;
        return http(yeuCauGoc);
      } catch {
        giaiPhongHangDoi(null);
        boNhoToken.xoa();
        chuyenVeDangNhap();
        return Promise.reject(taoLoiApi(loi));
      } finally {
        dangLamMoi = false;
      }
    }

    return Promise.reject(taoLoiApi(loi));
  },
);

function chuyenVeDangNhap() {
  if (!window.location.pathname.startsWith('/dang-nhap')) {
    window.location.href = `/dang-nhap?tiepTuc=${encodeURIComponent(window.location.pathname)}`;
  }
}

function taoLoiApi(loi: AxiosError<PhanHoiApi<unknown>>): LoiApi {
  const duLieu = loi.response?.data;

  if (duLieu?.maLoi) {
    return new LoiApi(duLieu.maLoi, duLieu.thongBao, duLieu.chiTietLoi ?? [], loi.response?.status);
  }

  if (loi.code === 'ECONNABORTED') {
    return new LoiApi('HET_THOI_GIAN', 'Yêu cầu quá thời gian chờ. Vui lòng thử lại.');
  }

  if (!loi.response) {
    return new LoiApi('MAT_KET_NOI', 'Không kết nối được tới máy chủ. Vui lòng kiểm tra mạng.');
  }

  return new LoiApi('LOI_HE_THONG', loi.message, [], loi.response.status);
}

// --- Hàm tiện ích ----------------------------------------------------------

export async function layDuLieu<T>(duongDan: string, cauHinh?: AxiosRequestConfig): Promise<T> {
  const { data } = await http.get<PhanHoiApi<T>>(duongDan, cauHinh);
  return data.duLieu;
}

export async function layPhanTrang<T>(
  duongDan: string,
  thamSo?: object,
): Promise<PhanHoiPhanTrang<T>> {
  const { data } = await http.get<PhanHoiPhanTrang<T>>(duongDan, { params: locThamSo(thamSo) });
  return data;
}

export async function guiDuLieu<T>(
  duongDan: string,
  than?: unknown,
  cauHinh?: AxiosRequestConfig,
): Promise<T> {
  const { data } = await http.post<PhanHoiApi<T>>(duongDan, than, cauHinh);
  return data.duLieu;
}

export async function capNhatDuLieu<T>(duongDan: string, than?: unknown): Promise<T> {
  const { data } = await http.put<PhanHoiApi<T>>(duongDan, than);
  return data.duLieu;
}

export async function xoaDuLieu(duongDan: string): Promise<void> {
  await http.delete(duongDan);
}

/** Bỏ các tham số rỗng để URL gọn và cache của TanStack Query ổn định. */
export function locThamSo(thamSo?: object): Record<string, unknown> {
  if (!thamSo) return {};

  return Object.fromEntries(
    Object.entries(thamSo).filter(
      ([, giaTri]) => giaTri !== undefined && giaTri !== null && giaTri !== '',
    ),
  );
}

/** Tải tệp nhị phân (xuất Excel/PDF) và kích hoạt lưu về máy. */
export async function taiTep(duongDan: string, tenTep: string, thamSo?: object) {
  const phanHoi = await http.get(duongDan, {
    params: locThamSo(thamSo),
    responseType: 'blob',
  });

  const url = window.URL.createObjectURL(new Blob([phanHoi.data]));
  const the = document.createElement('a');
  the.href = url;
  the.download = tenTep;
  document.body.appendChild(the);
  the.click();
  the.remove();
  window.URL.revokeObjectURL(url);
}
