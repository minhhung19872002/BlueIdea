import {
  capNhatDuLieu,
  capNhatMotPhan,
  guiDuLieu,
  layDuLieu,
  layPhanTrang,
  xoaDuLieu,
  type PhanHoiPhanTrang,
} from './client';

// --- Kiểu dùng chung -------------------------------------------------------

export interface DanhMucDto {
  id: string;
  ma: string;
  ten: string;
  moTa?: string | null;
  thuTu: number;
  trangThai: number;
  ngayTao: string;
}

export interface NutCay {
  id: string;
  ma: string;
  ten: string;
  chaId?: string | null;
  trangThai: number;
  con: NutCay[];
}

export interface ThamSoLoc {
  trang?: number;
  soDong?: number;
  sapXep?: string;
  huong?: 'asc' | 'desc';
  tuKhoa?: string;
  trangThai?: number;
}

// --- Danh mục --------------------------------------------------------------

/** Tạo bộ hàm CRUD cho một danh mục — dùng chung cho 8 danh mục (chức năng 1–8). */
export function taoApiDanhMuc<TChiTiet = DanhMucDto, TLuu = Record<string, unknown>>(goc: string) {
  return {
    danhSach: (thamSo?: ThamSoLoc) => layPhanTrang<DanhMucDto>(goc, thamSo),
    chon: () => layDuLieu<DanhMucDto[]>(`${goc}/chon`),
    theoId: (id: string) => layDuLieu<TChiTiet>(`${goc}/${id}`),
    them: (duLieu: TLuu) => guiDuLieu<TChiTiet>(goc, duLieu),
    sua: (id: string, duLieu: TLuu) => capNhatDuLieu<TChiTiet>(`${goc}/${id}`, duLieu),
    xoa: (id: string) => xoaDuLieu(`${goc}/${id}`),
  };
}

export const apiLinhVuc = {
  ...taoApiDanhMuc('/api/v1/danh-muc/linh-vuc'),
  cay: () => layDuLieu<NutCay[]>('/api/v1/danh-muc/linh-vuc/cay'),
};

export const apiDoiTuong = taoApiDanhMuc('/api/v1/danh-muc/doi-tuong');
export const apiLoaiTacGia = taoApiDanhMuc('/api/v1/danh-muc/loai-tac-gia');
export const apiDotDeNghi = {
  ...taoApiDanhMuc('/api/v1/danh-muc/dot-de-nghi'),
  dangMo: () => layDuLieu<DanhMucDto[]>('/api/v1/danh-muc/dot-de-nghi/dang-mo'),
  moDot: (id: string) => guiDuLieu(`/api/v1/danh-muc/dot-de-nghi/${id}/mo-dot`),
  dongDot: (id: string) => guiDuLieu(`/api/v1/danh-muc/dot-de-nghi/${id}/dong-dot`),
  khoaDot: (id: string) => guiDuLieu(`/api/v1/danh-muc/dot-de-nghi/${id}/khoa-dot`),
};

export const apiDonVi = {
  ...taoApiDanhMuc('/api/v1/don-vi'),
  cay: () => layDuLieu<NutCay[]>('/api/v1/don-vi/cay'),
};

// --- Sáng kiến -------------------------------------------------------------

export interface SangKienTomTat {
  id: string;
  maHoSo: string;
  tenSangKien: string;
  tenLinhVuc?: string | null;
  tenDonVi?: string | null;
  tenDot?: string | null;
  trangThaiTong: string;
  tenBuocHienTai?: string | null;
  tenTrangThaiHienTai?: string | null;
  tongDiem?: number | null;
  tyLeTrungLap?: number | null;
  ketQua?: string | null;
  ngayNop?: string | null;
  hanXuLyHienTai?: string | null;
  quaHan: boolean;
  tacGiaChinh: string;
  phienBan: number;
}

export interface TacGia {
  id?: string;
  nguoiDungId?: string | null;
  hoTen: string;
  ngaySinh?: string | null;
  gioiTinh?: string | null;
  soCccd?: string | null;
  chucVu?: string | null;
  donViCongTac?: string | null;
  trinhDoChuyenMon?: string | null;
  email?: string | null;
  dienThoai?: string | null;
  tyLeDongGop: number;
  laTacGiaChinh: boolean;
  thuTu?: number;
}

export interface ThanhPhanHoSo {
  ma: string;
  ten: string;
  batBuoc: boolean;
  loaiDuLieu: string;
  soKyTuToiThieu: number;
  soKyTuToiDa: number;
  soLuongToiDa: number;
  dungLuongToiDaMb: number;
  dinhDangChoPhep: string[];
  moTaHuongDan?: string | null;
  trangThai: 'DU' | 'THIEU' | 'CHUA_DAT' | 'KHONG_BAT_BUOC';
  canhBao?: string | null;
}

export interface TepDinhKem {
  id: string;
  tepTinId: string;
  tenGoc: string;
  kichThuoc: number;
  mimeType?: string | null;
  thanhPhanHoSoMa: string;
  moTa?: string | null;
  ngayTaiLen: string;
}

export interface SangKienChiTiet {
  id: string;
  maHoSo: string;
  tenSangKien: string;
  dotDeNghiId: string;
  tenDot?: string | null;
  linhVucId: string;
  tenLinhVuc?: string | null;
  doiTuongId?: string | null;
  tenDoiTuong?: string | null;
  loaiTacGiaId?: string | null;
  donViId?: string | null;
  tenDonVi?: string | null;
  trangThaiTong: string;
  buocHienTaiId?: string | null;
  tenBuocHienTai?: string | null;
  tenTrangThaiHienTai?: string | null;
  mauTrangThai?: string | null;
  moTaGiaiPhap?: string | null;
  tinhTrangTruocKhiApDung?: string | null;
  noiDungGiaiPhap?: string | null;
  tinhMoi?: string | null;
  khaNangApDung?: string | null;
  phamViApDung?: string | null;
  hieuQuaKinhTe?: string | null;
  giaTriLamLoiUocTinh?: number | null;
  hieuQuaXaHoi?: string | null;
  thoiGianApDungTu?: string | null;
  thoiGianApDungDen?: string | null;
  noiDungDong: Record<string, string>;
  tyLeTrungLap?: number | null;
  trangThaiKiemTraTrungLap: string;
  tongDiem?: number | null;
  diemTrungBinh?: number | null;
  ketQua?: string | null;
  tenMucCongNhan?: string | null;
  ngayCongNhan?: string | null;
  ngayNop?: string | null;
  hanXuLyHienTai?: string | null;
  dangKhoa: boolean;
  lyDoKhoa?: string | null;
  congKhai: boolean;
  phienBan: number;
  choPhepSua: boolean;
  choPhepRut: boolean;
  danhSachTacGia: TacGia[];
  tepDinhKem: TepDinhKem[];
  thanhPhanHoSo: ThanhPhanHoSo[];
}

export interface HanhDongKhaDung {
  truongHopId: string;
  ma: string;
  ten: string;
  buocId: string;
  tenBuoc: string;
  mauNut: string;
  batBuocNhapYKien: boolean;
  batBuocDinhKem: boolean;
  tepBatBuoc: string[];
  biChan: boolean;
  lyDoChan?: string | null;
  buocTiepTheoId?: string | null;
  tenBuocTiepTheo?: string | null;
  hanhDongTuDong: string[];
}

export interface MocTienDo {
  id: string;
  buocId: string;
  tenBuoc: string;
  tenTrangThai?: string | null;
  tenTruongHop?: string | null;
  nguoiXuLy?: string | null;
  yKien?: string | null;
  thoiGianNhan: string;
  hanXuLy?: string | null;
  thoiGianXuLy?: string | null;
  soNgayXuLy?: number | null;
  quaHan: boolean;
  tepDinhKem: TepDinhKem[];
}

export interface NoiDungHoSo {
  tenSangKien: string;
  dotDeNghiId: string;
  linhVucId: string;
  doiTuongId?: string | null;
  loaiTacGiaId?: string | null;
  donViId?: string | null;
  moTaGiaiPhap?: string | null;
  tinhTrangTruocKhiApDung?: string | null;
  noiDungGiaiPhap?: string | null;
  tinhMoi?: string | null;
  khaNangApDung?: string | null;
  phamViApDung?: string | null;
  hieuQuaKinhTe?: string | null;
  giaTriLamLoiUocTinh?: number | null;
  hieuQuaXaHoi?: string | null;
  thoiGianApDungTu?: string | null;
  thoiGianApDungDen?: string | null;
  noiDungDong?: Record<string, string>;
  danhSachTacGia: TacGia[];
}

export const apiSangKien = {
  danhSach: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<SangKienTomTat>('/api/v1/sang-kien', thamSo),
  cuaToi: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<SangKienTomTat>('/api/v1/sang-kien/cua-toi', thamSo),
  chiTiet: (id: string) => layDuLieu<SangKienChiTiet>(`/api/v1/sang-kien/${id}`),
  tienDo: (id: string) => layDuLieu<MocTienDo[]>(`/api/v1/sang-kien/${id}/tien-do`),
  lichSu: (id: string) => layDuLieu<LichSuChinhSua[]>(`/api/v1/sang-kien/${id}/lich-su`),
  hanhDong: (id: string) => layDuLieu<HanhDongKhaDung[]>(`/api/v1/sang-kien/${id}/hanh-dong`),
  trungLap: (id: string) => layDuLieu<KetQuaTrungLap | null>(`/api/v1/sang-kien/${id}/trung-lap`),
  chayLaiTrungLap: (id: string) =>
    guiDuLieu<KetQuaTrungLap>(`/api/v1/sang-kien/${id}/trung-lap/chay-lai`),
  tao: (duLieu: NoiDungHoSo) => guiDuLieu<string>('/api/v1/sang-kien', duLieu),
  capNhat: (id: string, duLieu: NoiDungHoSo, phienBan?: number) =>
    capNhatDuLieu(`/api/v1/sang-kien/${id}${phienBan ? `?phienBan=${phienBan}` : ''}`, duLieu),
  nop: (id: string) =>
    guiDuLieu<{ id: string; maHoSo: string; trangThaiTong: string; tenBuocHienTai?: string }>(
      `/api/v1/sang-kien/${id}/nop`,
    ),
  rut: (id: string, lyDo: string) => guiDuLieu(`/api/v1/sang-kien/${id}/rut`, { lyDo }),
};

export interface LichSuChinhSua {
  id: string;
  hanhDong: string;
  truongThayDoi: string[];
  giaTriTruoc?: Record<string, string | null> | null;
  giaTriSau?: Record<string, string | null> | null;
  thoiGian: string;
  ghiChu?: string | null;
  diaChiIp?: string | null;
}

export interface CapDoanTrung {
  doanNguon: string;
  doanDich: string;
  tyLe: number;
  viTriBatDau: number;
  viTriKetThuc: number;
}

export interface ChiTietTrungLap {
  sangKienDoiChieuId: string;
  tyLeTuongDong: number;
  tyLeTuVung: number;
  tyLeNguNghia: number;
  soDoanTrung: number;
  cacDoanTrung: CapDoanTrung[];
}

export interface KetQuaTrungLap {
  id: string;
  sangKienId: string;
  ngayChay: string;
  tongSoDoiChieu: number;
  tyLeCaoNhat: number;
  mucCanhBao: 'AN_TOAN' | 'CANH_BAO' | 'NGHIEM_TRONG';
  trangThaiChay: string;
  thoiGianXuLyMs: number;
  daXemXet: boolean;
  yKienHoiDong?: string | null;
  chiTiet: ChiTietTrungLap[];
}

// --- Xử lý -----------------------------------------------------------------

export const apiXuLy = {
  thucThi: (duLieu: {
    sangKienId: string;
    truongHopId: string;
    yKien?: string;
    tepDinhKemIds?: string[];
    phienBanHoSo?: number;
  }) =>
    guiDuLieu<{ thongBao: string; tenBuocMoi?: string; choThemTacNhan: boolean }>(
      '/api/v1/xu-ly/thuc-thi',
      duLieu,
      { headers: { 'Idempotency-Key': crypto.randomUUID() } },
    ),
  thucThiHangLoat: (duLieu: { sangKienIds: string[]; truongHopId: string; yKien?: string }) =>
    guiDuLieu<{ tongSo: number; thanhCong: number; thatBai: number; chiTietLoi: string[] }>(
      '/api/v1/xu-ly/thuc-thi-hang-loat',
      duLieu,
    ),
  thuHoi: (sangKienId: string, lyDo: string) =>
    guiDuLieu('/api/v1/xu-ly/thu-hoi', { sangKienId, lyDo }),
};

// --- Đánh giá --------------------------------------------------------------

export interface TieuChi {
  id: string;
  ma: string;
  ten: string;
  moTa?: string | null;
  diemToiDa: number;
  diemToiThieu: number;
  trongSo: number;
  kieuNhap: string;
  buocNhay: number;
  batBuocNhanXet: boolean;
  huongDanCham?: string | null;
  thuTu: number;
  danhSachMucDiem: { id: string; ten: string; diem: number; moTa?: string | null }[];
}

export interface NhomTieuChi {
  id: string;
  ma: string;
  ten: string;
  moTa?: string | null;
  trongSo: number;
  diemToiDa: number;
  thuTu: number;
  danhSachTieuChi: TieuChi[];
}

export interface BoTieuChi {
  id: string;
  ma: string;
  ten: string;
  thangDiemToiDa: number;
  diemDatToiThieu: number;
  cachTinh: string;
  lamTron: number;
  danhSachNhom: NhomTieuChi[];
}

export interface PhieuDanhGia {
  id: string;
  sangKienId: string;
  maHoSo: string;
  tenSangKien: string;
  hoiDongId: string;
  boTieuChiId: string;
  trangThaiPhieu: string;
  tongDiem: number;
  nhanXetChung?: string | null;
  uuDiem?: string | null;
  hanChe?: string | null;
  ketLuan?: string | null;
  soPhieu?: string | null;
  choPhepSua: boolean;
  chiTiet: { tieuChiId: string; diem: number; mucDiemId?: string | null; nhanXet?: string | null }[];
  boTieuChi?: BoTieuChi | null;
}

export interface PhanCongCham {
  id: string;
  sangKienId: string;
  maHoSo: string;
  tenSangKien: string;
  tenLinhVuc?: string | null;
  hoiDongId: string;
  tenHoiDong: string;
  trangThaiPhanCong: string;
  ngayPhanCong: string;
  hanHoanThanh?: string | null;
  quaHan: boolean;
  tongDiemDaCham?: number | null;
  phieuDanhGiaId?: string | null;
}

export const apiDanhGia = {
  viecCuaToi: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<PhanCongCham>('/api/v1/danh-gia/viec-cua-toi', thamSo),
  layPhieu: (sangKienId: string, hoiDongId: string) =>
    layDuLieu<PhieuDanhGia>(
      `/api/v1/danh-gia/phieu?sangKienId=${sangKienId}&hoiDongId=${hoiDongId}`,
    ),
  luuNhap: (duLieu: unknown) => guiDuLieu<PhieuDanhGia>('/api/v1/danh-gia/phieu/luu-nhap', duLieu),
  gui: (duLieu: unknown) => guiDuLieu<PhieuDanhGia>('/api/v1/danh-gia/phieu/gui', duLieu),
  phanCong: (duLieu: unknown) => guiDuLieu('/api/v1/danh-gia/phan-cong', duLieu),
  tongHop: (sangKienId: string, hoiDongId: string) =>
    guiDuLieu(`/api/v1/danh-gia/tong-hop?sangKienId=${sangKienId}&hoiDongId=${hoiDongId}`),
  maTranDiem: (hoiDongId: string) =>
    layDuLieu<unknown[]>(`/api/v1/danh-gia/ma-tran-diem?hoiDongId=${hoiDongId}`),
};

// --- Báo cáo ---------------------------------------------------------------

export interface ThongKeTongQuan {
  tongHoSo: number;
  hoSoDangXuLy: number;
  hoSoQuaHan: number;
  hoSoDat: number;
  hoSoKhongDat: number;
  hoSoChoTiepNhan: number;
  tyLeDat: number;
  soCanhBaoTrungLapCao: number;
  theoTrangThai: { ten: string; soLuong: number }[];
  theoLinhVuc: { ten: string; soLuong: number }[];
  topDonVi: { ten: string; soLuong: number; giaTriPhu?: number | null }[];
  xuHuongTheoNam: { ten: string; soLuong: number; giaTriPhu?: number | null }[];
}

export interface DongBaoCaoSangKien {
  maHoSo: string;
  tenSangKien: string;
  tacGia: string;
  tenDonVi?: string | null;
  tenLinhVuc?: string | null;
  tenDot?: string | null;
  tongDiem?: number | null;
  tenMucCongNhan?: string | null;
  ketQua?: string | null;
  lyDo?: string | null;
  ngayCongNhan?: string | null;
  soQuyetDinh?: string | null;
}

export interface DongBaoCaoDonVi {
  maDonVi: string;
  tenDonVi: string;
  tongSo: number;
  soDat: number;
  soKhongDat: number;
  soDangXuLy: number;
  tyLeDat: number;
}

export const apiBaoCao = {
  tongQuan: (thamSo?: Record<string, unknown>) =>
    layDuLieu<ThongKeTongQuan>('/api/v1/bao-cao/tong-quan', { params: thamSo }),
  sangKienDat: (thamSo?: Record<string, unknown>) =>
    layDuLieu<DongBaoCaoSangKien[]>('/api/v1/bao-cao/sang-kien-dat', { params: thamSo }),
  sangKienChuaDat: (thamSo?: Record<string, unknown>) =>
    layDuLieu<DongBaoCaoSangKien[]>('/api/v1/bao-cao/sang-kien-chua-dat', { params: thamSo }),
  theoDonVi: (thamSo?: Record<string, unknown>) =>
    layDuLieu<DongBaoCaoDonVi[]>('/api/v1/bao-cao/theo-don-vi', { params: thamSo }),
};

// --- Quy trình / tiêu chí / hội đồng ---------------------------------------

export const apiQuyTrinh = {
  ...taoApiDanhMuc('/api/v1/quy-trinh'),
  soDo: (id: string) => layDuLieu<SoDoQuyTrinh>(`/api/v1/quy-trinh/${id}/so-do`),
  luuSoDo: (id: string, soDo: SoDoQuyTrinh) =>
    capNhatDuLieu(`/api/v1/quy-trinh/${id}/so-do`, soDo),
  kiemTra: (id: string) => guiDuLieu<KetQuaKiemTraQuyTrinh>(`/api/v1/quy-trinh/${id}/kiem-tra`),
  kichHoat: (id: string) => guiDuLieu<KetQuaKiemTraQuyTrinh>(`/api/v1/quy-trinh/${id}/kich-hoat`),
  ngungApDung: (id: string) => guiDuLieu(`/api/v1/quy-trinh/${id}/ngung-ap-dung`),
  phienBanMoi: (id: string) => guiDuLieu<string>(`/api/v1/quy-trinh/${id}/phien-ban-moi`),
  saoChep: (id: string, ma: string, ten: string) =>
    guiDuLieu<string>(`/api/v1/quy-trinh/${id}/sao-chep`, { ma, ten }),
};

export interface PhatHien {
  ma: string;
  thongBao: string;
  buocId?: string | null;
  tenBuoc?: string | null;
}

export interface KetQuaKiemTraQuyTrinh {
  hopLe: boolean;
  danhSachLoi: PhatHien[];
  danhSachCanhBao: PhatHien[];
}

export interface BuocQuyTrinh {
  id: string;
  ma: string;
  ten: string;
  thuTu: number;
  loaiBuoc: string;
  soNgayXuLy: number;
  tinhTheoNgayLamViec: boolean;
  batBuocDinhKem: boolean;
  danhSachTepBatBuoc: string[];
  batBuocNhapYKien: boolean;
  choPhepUyQuyen: boolean;
  choPhepThuHoi: boolean;
  laBuocBatDau: boolean;
  laBuocKetThuc: boolean;
  canhBaoTruocHanGio: number;
  moTaHuongDan?: string | null;
  hoiDongId?: string | null;
  boTieuChiId?: string | null;
  tacNhan: {
    id: string;
    loaiTacNhan: string;
    thamChieuId?: string | null;
    thamChieuMa?: string | null;
    quyTacXuLy: string;
    tyLeDongThuan?: number | null;
    thuTu: number;
  }[];
  truongHop: {
    id: string;
    ma: string;
    ten: string;
    buocTiepTheoId?: string | null;
    trangThaiGanId?: string | null;
    dieuKien?: unknown;
    hanhDong: string[];
    mauNut?: string | null;
    thuTu: number;
    laMacDinh: boolean;
  }[];
  trangThai: {
    id: string;
    ma: string;
    ten: string;
    mauSac?: string | null;
    laTrangThaiKetThuc: boolean;
    hienThiChoTacGia: boolean;
    thuTu: number;
  }[];
}

export interface SoDoQuyTrinh {
  danhSachBuoc: BuocQuyTrinh[];
  trangThaiToanCuc: {
    id: string;
    ma: string;
    ten: string;
    mauSac?: string | null;
    laTrangThaiKetThuc: boolean;
    hienThiChoTacGia: boolean;
    thuTu: number;
  }[];
  thanhPhanHoSo: {
    id: string;
    ma: string;
    ten: string;
    batBuoc: boolean;
    loaiDuLieu: string;
    dinhDangChoPhep: string[];
    dungLuongToiDaMb: number;
    soLuongToiDa: number;
    soKyTuToiThieu: number;
    soKyTuToiDa: number;
    dungDeKiemTraTrungLap: boolean;
    thuTu: number;
    moTaHuongDan?: string | null;
  }[];
  chucNangBoSung: {
    id: string;
    buocId?: string | null;
    maChucNang: string;
    batBuoc: boolean;
  }[];
  soDoLayout?: Record<string, unknown> | null;
}

export const apiTieuChi = {
  ...taoApiDanhMuc('/api/v1/tieu-chi'),
  chiTiet: (id: string) => layDuLieu<BoTieuChi>(`/api/v1/tieu-chi/${id}`),
  kiemTra: (id: string) => guiDuLieu<string[]>(`/api/v1/tieu-chi/${id}/kiem-tra`),
  luuCay: (id: string, nhom: unknown[]) => capNhatDuLieu(`/api/v1/tieu-chi/${id}/cay`, nhom),
  luuMucCongNhan: (id: string, danhSach: unknown[]) =>
    capNhatDuLieu(`/api/v1/tieu-chi/${id}/muc-cong-nhan`, danhSach),
};

export const apiHoiDong = {
  ...taoApiDanhMuc('/api/v1/hoi-dong'),
  luuThanhVien: (id: string, danhSach: unknown[]) =>
    capNhatDuLieu(`/api/v1/hoi-dong/${id}/thanh-vien`, danhSach),
  taoPhienHop: (duLieu: unknown) => guiDuLieu('/api/v1/hoi-dong/phien-hop', duLieu),
  boPhieu: (duLieu: unknown) => guiDuLieu('/api/v1/hoi-dong/phien-hop/bo-phieu', duLieu),
};

// --- Hệ thống --------------------------------------------------------------

export interface CauHinhMuc {
  id: string;
  nhom: string;
  khoa: string;
  giaTri?: string | null;
  kieuDuLieu: string;
  tenHienThi: string;
  moTa?: string | null;
  choPhepSua: boolean;
}

export interface ThongTinNguoiDung {
  id: string;
  tenDangNhap: string;
  hoTen: string;
  email?: string | null;
  dienThoai?: string | null;
  chucVu?: string | null;
  donViId?: string | null;
  tenDonVi?: string | null;
  ngaySinh?: string | null;
  gioiTinh?: string | null;
  trangThaiTaiKhoan: string;
  buocDoiMatKhau: boolean;
  lanDangNhapCuoi?: string | null;
  vaiTroIds: string[];
  tenVaiTro: string[];
}

export interface LuuNguoiDung {
  tenDangNhap: string;
  hoTen: string;
  email?: string | null;
  dienThoai?: string | null;
  chucVu?: string | null;
  donViId?: string | null;
  soCccd?: string | null;
  ngaySinh?: string | null;
  gioiTinh?: string | null;
  trangThaiTaiKhoan: string;
  vaiTroIds: string[];
}

export interface LuuVaiTro {
  ma: string;
  ten: string;
  moTa?: string | null;
  thuTu: number;
  trangThai: number;
  quyenIds: string[];
  loaiPhamVi: string;
  donViIds: string[];
}

export const apiHeThong = {
  cauHinh: (nhom?: string) =>
    layDuLieu<CauHinhMuc[]>('/api/v1/he-thong/cau-hinh', { params: { nhom } }),
  luuCauHinh: (danhSach: { khoa: string; giaTri?: string | null }[]) =>
    capNhatDuLieu('/api/v1/he-thong/cau-hinh', danhSach),
  vaiTro: () => layDuLieu<unknown>('/api/v1/he-thong/vai-tro'),
  nguoiDung: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<Record<string, unknown>>('/api/v1/he-thong/nguoi-dung', thamSo),
  nhatKyHeThong: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<Record<string, unknown>>('/api/v1/he-thong/nhat-ky/he-thong', thamSo),
  nhatKyDangNhap: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<Record<string, unknown>>('/api/v1/he-thong/nhat-ky/dang-nhap', thamSo),

  // Chức năng 43 — quản lý người dùng
  chiTietNguoiDung: (id: string) =>
    layDuLieu<ThongTinNguoiDung>(`/api/v1/he-thong/nguoi-dung/${id}`),
  themNguoiDung: (duLieu: LuuNguoiDung) =>
    guiDuLieu<{ id: string; matKhauTam: string }>('/api/v1/he-thong/nguoi-dung', duLieu),
  suaNguoiDung: (id: string, duLieu: LuuNguoiDung) =>
    capNhatDuLieu(`/api/v1/he-thong/nguoi-dung/${id}`, duLieu),
  doiTrangThaiNguoiDung: (id: string, trangThai: string) =>
    capNhatMotPhan(
      `/api/v1/he-thong/nguoi-dung/${id}/trang-thai?trangThai=${encodeURIComponent(trangThai)}`,
    ),
  datLaiMatKhau: (id: string) =>
    guiDuLieu<{ matKhauTam: string }>(`/api/v1/he-thong/nguoi-dung/${id}/dat-lai-mat-khau`),

  // Chức năng 45 — ma trận phân quyền
  themVaiTro: (duLieu: LuuVaiTro) => guiDuLieu<string>('/api/v1/he-thong/vai-tro', duLieu),
  suaVaiTro: (id: string, duLieu: LuuVaiTro) =>
    capNhatDuLieu(`/api/v1/he-thong/vai-tro/${id}`, duLieu),
  xoaVaiTro: (id: string) => xoaDuLieu(`/api/v1/he-thong/vai-tro/${id}`),
};

// --- Quyết định công nhận (chức năng 8, 31, 32, 36) -------------------------

export interface HoSoDuDieuKien {
  id: string;
  maHoSo: string;
  tenSangKien: string;
  tenTacGiaChinh?: string | null;
  tenDonVi?: string | null;
  tenLinhVuc?: string | null;
  tongDiem?: number | null;
  mucCongNhanId?: string | null;
  tenMucCongNhan?: string | null;
}

export interface QuyetDinh {
  id: string;
  soQuyetDinh: string;
  ngayBanHanh: string;
  loai: string;
  trichYeu?: string | null;
  nguoiKy?: string | null;
  chucVuNguoiKy?: string | null;
  donViBanHanhId?: string | null;
  tenDonViBanHanh?: string | null;
  dotDeNghiId?: string | null;
  tenDot?: string | null;
  daKySo: boolean;
  soSangKien: number;
  soDaCongBo: number;
}

export interface LuuQuyetDinh {
  soQuyetDinh: string;
  ngayBanHanh: string;
  loai: string;
  trichYeu?: string | null;
  nguoiKy?: string | null;
  chucVuNguoiKy?: string | null;
  donViBanHanhId?: string | null;
  dotDeNghiId?: string | null;
  sangKienIds: string[];
}

export const apiQuyetDinh = {
  danhSach: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<QuyetDinh>('/api/v1/quyet-dinh', thamSo),
  chiTiet: (id: string) =>
    layDuLieu<{ thongTin: QuyetDinh; danhSachSangKien: HoSoDuDieuKien[] }>(
      `/api/v1/quyet-dinh/${id}`,
    ),
  hoSoDuDieuKien: (dotDeNghiId?: string, quyetDinhDangSua?: string) =>
    layDuLieu<HoSoDuDieuKien[]>('/api/v1/quyet-dinh/ho-so-du-dieu-kien', {
      params: { dotDeNghiId, quyetDinhDangSua },
    }),
  banHanh: (duLieu: LuuQuyetDinh) => guiDuLieu<string>('/api/v1/quyet-dinh', duLieu),
  sua: (id: string, duLieu: LuuQuyetDinh) => capNhatDuLieu(`/api/v1/quyet-dinh/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/quyet-dinh/${id}`),
  congBo: (id: string, congKhai: boolean) =>
    guiDuLieu(`/api/v1/quyet-dinh/${id}/cong-bo?congKhai=${congKhai}`),
  duongDanPdf: (id: string) => `/api/v1/quyet-dinh/${id}/xuat-pdf`,
};

export type { PhanHoiPhanTrang };
