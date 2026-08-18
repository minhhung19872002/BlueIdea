import {
  capNhatDuLieu,
  capNhatMotPhan,
  guiDuLieu,
  http,
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
  /** Danh mục cấp trên — chỉ có giá trị với danh mục phân cấp (hiện tại là lĩnh vực). */
  danhMucChaId?: string | null;
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
  /** Lưu thứ tự sau khi kéo–thả: gửi mảng id theo đúng thứ tự mong muốn. */
  sapXep: (idTheoThuTu: string[]) =>
    capNhatDuLieu('/api/v1/danh-muc/linh-vuc/sap-xep', idTheoThuTu),
};

export const apiDoiTuong = taoApiDanhMuc('/api/v1/danh-muc/doi-tuong');
export const apiLoaiTacGia = taoApiDanhMuc('/api/v1/danh-muc/loai-tac-gia');
export interface DotDeNghiQuanLy {
  id: string;
  ma: string;
  ten: string;
  nam: number;
  capXetDuyet: string;
  /** NHAP | DANG_MO | DA_DONG | DA_KHOA */
  trangThaiDot: string;
  tuDongKhoa: boolean;
  hanNopHoSo?: string | null;
  hanChamDiem?: string | null;
  quyTrinhId?: string | null;
  boTieuChiId?: string | null;
  trangThai: number;
}

export const apiDotDeNghi = {
  ...taoApiDanhMuc('/api/v1/danh-muc/dot-de-nghi'),
  dangMo: () => layDuLieu<DanhMucDto[]>('/api/v1/danh-muc/dot-de-nghi/dang-mo'),
  /** Danh sách kèm trạng thái vòng đời — dùng cho màn hình quản trị đợt. */
  danhSachQuanLy: (thamSo?: ThamSoLoc) =>
    layPhanTrang<DotDeNghiQuanLy>('/api/v1/danh-muc/dot-de-nghi/quan-ly', thamSo),
  moDot: (id: string) => guiDuLieu(`/api/v1/danh-muc/dot-de-nghi/${id}/mo-dot`),
  dongDot: (id: string) => guiDuLieu(`/api/v1/danh-muc/dot-de-nghi/${id}/dong-dot`),
  khoaDot: (id: string) => guiDuLieu(`/api/v1/danh-muc/dot-de-nghi/${id}/khoa-dot`),
  saoChep: (id: string, duLieu: { ma: string; ten: string; nam: number }) =>
    guiDuLieu(`/api/v1/danh-muc/dot-de-nghi/${id}/sao-chep`, duLieu),
  /** Số liệu tổng quan của một đợt — dùng cho màn hình chi tiết đợt. */
  tongQuan: (id: string) =>
    layDuLieu<TongQuanDot>(`/api/v1/danh-muc/dot-de-nghi/${id}/tong-quan`),
};

export interface TongQuanDot {
  id: string;
  ma: string;
  ten: string;
  nam: number;
  capXetDuyet: string;
  trangThaiDot: string;
  hanNopHoSo?: string | null;
  hanChamDiem?: string | null;
  tenQuyTrinh?: string | null;
  tenBoTieuChi?: string | null;
  donViApDung: DanhMucDto[];
  tongHoSo: number;
  soNhap: number;
  soDangXuLy: number;
  soDat: number;
  soKhongDat: number;
  soHoiDong: number;
  soQuyetDinh: number;
  soPhieuDaGui: number;
  soPhieuCanCham: number;
}

/** Chức năng 47 — cấu hình riêng của đơn vị (tiêu đề văn bản, người ký mặc định). */
export interface DonViChiTiet extends DanhMucDto {
  tenVietTat?: string | null;
  donViChaId?: string | null;
  cap: number;
  loai: string;
  laDonViPheDuyet: boolean;
  capPheDuyet?: string | null;
  tieuDeVanBan?: string | null;
  nguoiKyMacDinh?: string | null;
  chucVuNguoiKyMacDinh?: string | null;
}

export const apiDonVi = {
  /** Chức năng 44 — kéo thả đổi cấp trên trên cây tổ chức. */
  chuyenCha: (id: string, donViChaMoiId: string | null) =>
    guiDuLieu(`/api/v1/don-vi/${id}/chuyen-cha`, { donViChaMoiId }),
  /** Chức năng 44 — gộp đơn vị khi sáp nhập. */
  gop: (nguonId: string, dichId: string) =>
    guiDuLieu<number>(`/api/v1/don-vi/${nguonId}/gop-vao/${dichId}`),
  ...taoApiDanhMuc<DonViChiTiet>('/api/v1/don-vi'),
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

export interface GoiYTimKiem {
  giaTri: string;
  /** MA_HO_SO | SANG_KIEN | TAC_GIA */
  loai: string;
  moTa?: string | null;
}

export const apiSangKien = {
  danhSach: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<SangKienTomTat>('/api/v1/sang-kien', thamSo),
  cuaToi: (thamSo?: Record<string, unknown>) =>
    layPhanTrang<SangKienTomTat>('/api/v1/sang-kien/cua-toi', thamSo),
  chiTiet: (id: string) => layDuLieu<SangKienChiTiet>(`/api/v1/sang-kien/${id}`),
  tienDo: (id: string) => layDuLieu<MocTienDo[]>(`/api/v1/sang-kien/${id}/tien-do`),
  duongDanPhieuTiepNhan: (id: string) => `/api/v1/sang-kien/${id}/phieu-tiep-nhan`,
  /** Chức năng 37 — gợi ý từ khoá khi gõ ở ô tìm kiếm. */
  goiY: (tuKhoa: string, soLuong = 8) =>
    layDuLieu<GoiYTimKiem[]>('/api/v1/sang-kien/goi-y', { params: { tuKhoa, soLuong } }),
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
  /** Chức năng 37 — tìm theo ý nghĩa câu hỏi, không cần trùng từ khoá. */
  timNguNghia: (thamSo: {
    cauHoi: string;
    soKetQua?: number;
    linhVucId?: string;
    nam?: number;
  }) => layDuLieu<KetQuaTimNguNghia[]>('/api/v1/sang-kien/tim-ngu-nghia', { params: thamSo }),
};

export interface KetQuaTimNguNghia {
  sangKienId: string;
  maHoSo: string;
  tenSangKien: string;
  tenTacGiaChinh?: string | null;
  tenDonVi?: string | null;
  tenLinhVuc?: string | null;
  namCongNhan?: number | null;
  ketQua?: string | null;
  /** 0–100, càng cao càng sát ý câu hỏi. */
  doTuongDong: number;
  doanKhopNhat: string;
}

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

export interface MucCongNhan {
  id: string;
  ma: string;
  ten: string;
  diemTu: number;
  diemDen: number;
  laDat: boolean;
  mauSac?: string | null;
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
  danhSachMucCongNhan?: MucCongNhan[];
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
  phanCong: (duLieu: {
    hoiDongId?: string;
    sangKienIds: string[];
    thanhVienIds?: string[] | null;
    hanHoanThanh?: string | null;
    tuDongChiaDeu: boolean;
  }) => guiDuLieu('/api/v1/danh-gia/phan-cong', duLieu),
  tongHop: (sangKienId: string, hoiDongId: string) =>
    guiDuLieu<KetQuaTongHop>(
      `/api/v1/danh-gia/tong-hop?sangKienId=${sangKienId}&hoiDongId=${hoiDongId}`,
    ),
  maTranDiem: (hoiDongId: string, dotDeNghiId?: string) =>
    layDuLieu<DongMaTranDiem[]>('/api/v1/danh-gia/ma-tran-diem', {
      params: { hoiDongId, dotDeNghiId },
    }),
  /** Chức năng 35 — thư ký mở lại phiếu đã gửi để thành viên sửa. */
  moLaiPhieu: (phieuId: string) => guiDuLieu(`/api/v1/danh-gia/phieu/${phieuId}/mo-lai`),
  kySoPhieu: (phieuId: string) => guiDuLieu<string>(`/api/v1/danh-gia/phieu/${phieuId}/ky-so`),
  lichSuKySoPhieu: (phieuId: string) =>
    layDuLieu<NhatKyKySo[]>(`/api/v1/danh-gia/phieu/${phieuId}/lich-su-ky-so`),
};

export interface KetQuaTongHop {
  sangKienId: string;
  soPhieu: number;
  soPhieuSuDung: number;
  diemCaoNhat: number;
  diemThapNhat: number;
  diemTrungBinh: number;
  diemCuoiCung: number;
  dat: boolean;
  mucCongNhanId?: string | null;
  tenMucCongNhan?: string | null;
  canhBao: string[];
}

export interface ODiemMaTran {
  thanhVienId: string;
  tenThanhVien: string;
  diem?: number | null;
  /** CHUA_CHAM | NHAP | DA_GUI | DA_KY | "-" khi chưa phân công */
  trangThai: string;
  /** Có giá trị khi thành viên đã có phiếu — dùng cho thao tác mở lại phiếu. */
  phieuId?: string | null;
}

export interface DongMaTranDiem {
  sangKienId: string;
  maHoSo: string;
  tenSangKien: string;
  diemThanhVien: ODiemMaTran[];
  diemTrungBinh?: number | null;
  diemCaoNhat?: number | null;
  diemThapNhat?: number | null;
  soPhieuDaCham: number;
  soPhieuPhanCong: number;
}

// --- Báo cáo ---------------------------------------------------------------

export interface MucThongKe {
  ten: string;
  soLuong: number;
  giaTriPhu?: number | null;
}

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
  theoTacGia: (thamSo?: Record<string, unknown>) =>
    layDuLieu<DongBaoCaoTacGia[]>('/api/v1/bao-cao/theo-tac-gia', { params: thamSo }),
  thoiGianXuLy: (thamSo?: Record<string, unknown>) =>
    layDuLieu<DongThoiGianXuLy[]>('/api/v1/bao-cao/thoi-gian-xu-ly', { params: thamSo }),
  tongHopNam: (nam: number, thamSo?: Record<string, unknown>) =>
    layDuLieu<BaoCaoTongHopNam>(`/api/v1/bao-cao/tong-hop-nam/${nam}`, { params: thamSo }),
  duongDanTongHopNamPdf: (nam: number) => `/api/v1/bao-cao/tong-hop-nam/${nam}/xuat-pdf`,
};

export interface DongBaoCaoTacGia {
  hoTen: string;
  donViCongTac?: string | null;
  chucVu?: string | null;
  tongSo: number;
  soDat: number;
  soLaTacGiaChinh: number;
  diemTrungBinh?: number | null;
  tyLeDat: number;
}

export interface DongThoiGianXuLy {
  tenBuoc: string;
  soLuot: number;
  soNgayTrungBinh: number;
  soNgayLauNhat: number;
  soLuotQuaHan: number;
}

export interface BaoCaoTongHopNam {
  nam: number;
  tongHoSo: number;
  soDat: number;
  soKhongDat: number;
  soDangXuLy: number;
  tyLeDat: number;
  tongGiaTriLamLoi?: number | null;
  soTacGia: number;
  soDonViThamGia: number;
  theoLinhVuc: MucThongKe[];
  theoDot: MucThongKe[];
  theoMucCongNhan: MucThongKe[];
}

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

/** Một dòng cấu hình thành phần hồ sơ của quy trình (chức năng 13). */
export interface ThanhPhanHoSoCauHinh {
  id: string;
  ma: string;
  ten: string;
  batBuoc: boolean;
  /** CA_HAI | VAN_BAN | TEP */
  loaiDuLieu: string;
  dinhDangChoPhep: string[];
  dungLuongToiDaMb: number;
  soLuongToiDa: number;
  soKyTuToiThieu: number;
  soKyTuToiDa: number;
  dungDeKiemTraTrungLap: boolean;
  thuTu: number;
  moTaHuongDan?: string | null;
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
  thanhPhanHoSo: ThanhPhanHoSoCauHinh[];
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
  saoChep: (id: string, duLieu: { ma: string; ten: string; nam: number }) =>
    guiDuLieu<string>(`/api/v1/tieu-chi/${id}/sao-chep`, duLieu),
};

// --- Hội đồng (chức năng 19–20) --------------------------------------------

export interface ThanhVienHoiDong {
  id: string;
  nguoiDungId?: string | null;
  hoTenHienThi: string;
  chucVuCongTac?: string | null;
  donViCongTac?: string | null;
  /** CHU_TICH | PHO_CHU_TICH | UY_VIEN_THU_KY | UY_VIEN | THU_KY */
  chucDanh: string;
  quyenChamDiem: boolean;
  quyenNhanXet: boolean;
  quyenBoPhieu: boolean;
  quyenKyBienBan: boolean;
  quyenKetLuan: boolean;
  thuTu: number;
  trangThai: number;
}

export interface PhienHopHoSoDto {
  id: string;
  sangKienId: string;
  thuTu: number;
  ketQua?: string | null;
  ketLuanRieng?: string | null;
}

export interface PhienHopDiemDanhDto {
  id: string;
  thanhVienId: string;
  coMat: boolean;
  lyDoVang?: string | null;
  thoiGianDiemDanh?: string | null;
}

export interface PhieuBoPhieuDto {
  id: string;
  sangKienId: string;
  thanhVienId: string;
  yKien: string;
  ghiChu?: string | null;
  laPhieuKin: boolean;
  thoiGian: string;
}

export interface PhienHop {
  id: string;
  hoiDongId: string;
  maPhien: string;
  tenPhien: string;
  thoiGianBatDau: string;
  thoiGianKetThuc?: string | null;
  diaDiem?: string | null;
  /** TRUC_TIEP | TRUC_TUYEN | KET_HOP */
  hinhThuc: string;
  chuTriId?: string | null;
  thuKyId?: string | null;
  noiDung?: string | null;
  ketLuan?: string | null;
  /** DU_KIEN | DANG_DIEN_RA | DA_KET_THUC | DA_HUY */
  trangThaiPhien: string;
  danhSachHoSo: PhienHopHoSoDto[];
  diemDanh: PhienHopDiemDanhDto[];
  phieuBoPhieu: PhieuBoPhieuDto[];
}

export interface HoiDongChiTiet extends DanhMucDto {
  cap: string;
  dotDeNghiId?: string | null;
  donViId?: string | null;
  soQuyetDinhThanhLap?: string | null;
  ngayQuyetDinh?: string | null;
  tepQuyetDinhId?: string | null;
  thoiGianHoatDongTu?: string | null;
  thoiGianHoatDongDen?: string | null;
  linhVucPhuTrach: string[];
  soThanhVienToiThieu: number;
  tyLeThongQua: number;
  /** DANG_HOAT_DONG | DA_KET_THUC */
  trangThaiHoatDong: string;
  thanhVien: ThanhVienHoiDong[];
  phienHop: PhienHop[];
}

export interface KetQuaBoPhieu {
  tongPhieu: number;
  dongY: number;
  khongDongY: number;
  yKienKhac: number;
  tyLeDongY: number;
}

export const apiHoiDong = {
  ...taoApiDanhMuc<HoiDongChiTiet>('/api/v1/hoi-dong'),
  luuThanhVien: (id: string, danhSach: unknown[]) =>
    capNhatDuLieu(`/api/v1/hoi-dong/${id}/thanh-vien`, danhSach),
  taoPhienHop: (duLieu: unknown) => guiDuLieu<PhienHop>('/api/v1/hoi-dong/phien-hop', duLieu),
  phienHop: (id: string) => layDuLieu<PhienHop>(`/api/v1/hoi-dong/phien-hop/${id}`),
  diemDanh: (phienHopId: string, duLieu: { thanhVienId: string; coMat: boolean; lyDoVang?: string }) =>
    guiDuLieu(`/api/v1/hoi-dong/phien-hop/${phienHopId}/diem-danh`, duLieu),
  boPhieu: (duLieu: unknown) => guiDuLieu('/api/v1/hoi-dong/phien-hop/bo-phieu', duLieu),
  ketQuaBoPhieu: (phienHopId: string, sangKienId: string) =>
    layDuLieu<KetQuaBoPhieu>(`/api/v1/hoi-dong/phien-hop/${phienHopId}/ket-qua-bo-phieu`, {
      params: { sangKienId },
    }),
  ghiYKienHoSo: (
    phienHopId: string,
    duLieu: { sangKienId: string; ketLuanRieng?: string | null; ketQua?: string | null },
  ) => guiDuLieu(`/api/v1/hoi-dong/phien-hop/${phienHopId}/y-kien-ho-so`, duLieu),
  ketThucPhienHop: (phienHopId: string, ketLuan?: string) =>
    guiDuLieu(`/api/v1/hoi-dong/phien-hop/${phienHopId}/ket-thuc`, { ketLuan }),
};

// --- Hệ thống --------------------------------------------------------------

export interface ThongBaoTrongUngDung {
  id: string;
  tieuDe: string;
  noiDung: string;
  loaiSuKien?: string | null;
  doiTuongLienQuan?: string | null;
  doiTuongId?: string | null;
  /** Đường dẫn trong ứng dụng để mở thẳng đối tượng liên quan. */
  duongDan?: string | null;
  /** THAP | BINH_THUONG | CAO | KHAN */
  mucDo: string;
  daDoc: boolean;
  ngayDoc?: string | null;
  thoiGian: string;
}

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

  // Thông báo trong ứng dụng
  thongBao: (thamSo?: { trang?: number; soDong?: number; chuaDoc?: boolean }) =>
    layPhanTrang<ThongBaoTrongUngDung>('/api/v1/he-thong/thong-bao', thamSo),
  danhDauDaDoc: (id: string) => guiDuLieu(`/api/v1/he-thong/thong-bao/${id}/da-doc`),
  danhDauDocTatCa: () => guiDuLieu('/api/v1/he-thong/thong-bao/doc-tat-ca'),

  saoChepVaiTro: (id: string, duLieu: { ma: string; ten: string }) =>
    guiDuLieu<string>(`/api/v1/he-thong/vai-tro/${id}/sao-chep`, duLieu),

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
  /** Tệp văn bản quyết định đã ban hành — đối tượng được ký số (chức năng 49). */
  tepTinId?: string | null;
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
  tepTinId?: string | null;
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

  // Chức năng 49 — ký số văn bản quyết định
  kySo: (id: string, tepTinId: string) =>
    guiDuLieu<string>(`/api/v1/quyet-dinh/${id}/ky-so?tepTinId=${tepTinId}`),
  lichSuKySo: (id: string) =>
    layDuLieu<NhatKyKySo[]>(`/api/v1/quyet-dinh/${id}/lich-su-ky-so`),
  xacMinhChuKy: (nhatKyKySoId: string) =>
    layDuLieu<KetQuaXacMinhChuKy>(`/api/v1/quyet-dinh/xac-minh-chu-ky/${nhatKyKySoId}`),
};

export interface NhatKyKySo {
  id: string;
  doiTuong: string;
  doiTuongId: string;
  nguoiKyId?: string | null;
  thoiGianKy: string;
  serialChungThu?: string | null;
  nguoiCapChungThu?: string | null;
  hieuLucTu?: string | null;
  hieuLucDen?: string | null;
  tepGocId?: string | null;
  tepDaKyId?: string | null;
  /** THANH_CONG | THAT_BAI */
  trangThaiKy: string;
  thongTinXacThuc?: Record<string, unknown> | null;
}

export interface KetQuaXacMinhChuKy {
  coChuKy: boolean;
  hopLe: boolean;
  serialChungThu?: string | null;
  nguoiKy?: string | null;
  thoiGianKy?: string | null;
  thongBaoLoi?: string | null;
}

// --- Cấu hình gửi tin (chức năng 50) ---------------------------------------

export interface CauHinhGuiTin {
  id: string;
  loai: string;
  nhaCungCap?: string | null;
  host?: string | null;
  port?: number | null;
  tenDangNhap?: string | null;
  daDatMatKhau: boolean;
  suDungSsl: boolean;
  emailGuiDi?: string | null;
  tenHienThi?: string | null;
  apiEndpoint?: string | null;
  daDatApiKey: boolean;
  brandname?: string | null;
  trangThai: number;
  laMacDinh: boolean;
}

export interface LuuCauHinhGuiTin {
  loai: string;
  nhaCungCap?: string | null;
  host?: string | null;
  port?: number | null;
  tenDangNhap?: string | null;
  matKhau?: string | null;
  suDungSsl: boolean;
  emailGuiDi?: string | null;
  tenHienThi?: string | null;
  apiEndpoint?: string | null;
  apiKey?: string | null;
  brandname?: string | null;
  trangThai: number;
  laMacDinh: boolean;
}

export const apiCauHinhGuiTin = {
  danhSach: () => layDuLieu<CauHinhGuiTin[]>('/api/v1/cau-hinh-gui-tin'),
  thongKeHangDoi: () =>
    layDuLieu<Record<string, number>>('/api/v1/cau-hinh-gui-tin/thong-ke-hang-doi'),
  them: (duLieu: LuuCauHinhGuiTin) => guiDuLieu<string>('/api/v1/cau-hinh-gui-tin', duLieu),
  sua: (id: string, duLieu: LuuCauHinhGuiTin) =>
    capNhatDuLieu(`/api/v1/cau-hinh-gui-tin/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/cau-hinh-gui-tin/${id}`),
  guiThu: (id: string, nguoiNhan: string) =>
    guiDuLieu(`/api/v1/cau-hinh-gui-tin/${id}/gui-thu?nguoiNhan=${encodeURIComponent(nguoiNhan)}`),
};

// --- Nhập / xuất (chức năng 6, 7, 35, 43) -----------------------------------

export interface KetQuaDongNhap {
  soDong: number;
  tenDangNhap?: string | null;
  hoTen?: string | null;
  hopLe: boolean;
  loi?: string | null;
  matKhauTam?: string | null;
}

export interface KetQuaNhapNguoiDung {
  chayThu: boolean;
  tongDong: number;
  soHopLe: number;
  soLoi: number;
  chiTiet: KetQuaDongNhap[];
}

export interface KetQuaBaoCaoTuyBien {
  tenBaoCao: string;
  tieuDeCot: string[];
  cacDong: string[][];
  boLocDaApDung: Record<string, string>;
  tongSo: number;
}

export interface NguonDuLieuBaoCao {
  ma: string;
  tieuDeGoiY: string;
}

export interface KetQuaQuetPlaceholder {
  placeholder: string[];
  soDoanVan: number;
  soBang: number;
  canhBao?: string | null;
  nguonGoiY: NguonDuLieuBaoCao[];
}

export const apiNhapXuat = {
  duongDanMauNhapNguoiDung: () => '/api/v1/nhap-xuat/nguoi-dung/mau-nhap',

  nhapNguoiDung: async (tep: File, chayThu: boolean) => {
    const form = new FormData();
    form.append('tep', tep);

    const { data } = await http.post<{ duLieu: KetQuaNhapNguoiDung; thongBao: string }>(
      `/api/v1/nhap-xuat/nguoi-dung?chayThu=${chayThu}`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );

    return data;
  },

  quetPlaceholder: async (tep: File) => {
    const form = new FormData();
    form.append('tep', tep);

    const { data } = await http.post<{ duLieu: KetQuaQuetPlaceholder }>(
      '/api/v1/nhap-xuat/bieu-mau/quet-placeholder',
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );

    return data.duLieu;
  },

  duongDanMauNhapDanhMuc: (loai: string) =>
    `/api/v1/nhap-xuat/danh-muc/mau?loai=${encodeURIComponent(loai)}`,

  /** Nhập danh mục từ Excel. chayThu = true chỉ kiểm tra, không ghi. */
  nhapDanhMuc: async (loai: string, tep: File, chayThu: boolean) => {
    const form = new FormData();
    form.append('tep', tep);

    const { data } = await http.post<{ duLieu: KetQuaNhapDanhMuc }>(
      `/api/v1/nhap-xuat/danh-muc?loai=${encodeURIComponent(loai)}&chayThu=${chayThu}`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );

    return data.duLieu;
  },

  duongDanPhieuChamHoSo: (sangKienId: string, dinhDang?: 'PDF' | 'ZIP') =>
    `/api/v1/nhap-xuat/phieu-cham/ho-so/${sangKienId}` +
    (dinhDang === 'ZIP' ? '?dinhDang=ZIP' : ''),
  duongDanPhieuChamHoiDong: (hoiDongId: string, dinhDang?: 'PDF' | 'ZIP') =>
    `/api/v1/nhap-xuat/phieu-cham/hoi-dong/${hoiDongId}` +
    (dinhDang === 'ZIP' ? '?dinhDang=ZIP' : ''),

  nguonDuLieuBaoCao: () =>
    layDuLieu<NguonDuLieuBaoCao[]>('/api/v1/nhap-xuat/bao-cao-tuy-bien/nguon-du-lieu'),
  chayBaoCaoTuyBien: (bieuMauId: string, thamSo?: Record<string, unknown>) =>
    layDuLieu<KetQuaBaoCaoTuyBien>(`/api/v1/nhap-xuat/bao-cao-tuy-bien/${bieuMauId}`, {
      params: thamSo,
    }),
  duongDanXuatBaoCaoTuyBien: (bieuMauId: string) =>
    `/api/v1/nhap-xuat/bao-cao-tuy-bien/${bieuMauId}/xuat-excel`,
};

export type { PhanHoiPhanTrang };

// --- Cấu hình chữ ký số (chức năng 49) -------------------------------------

export interface CauHinhChuKySo {
  id: string;
  /** BAN_CO_YEU_CHINH_PHU | VNPT_CA | VIETTEL_CA | FPT_CA | MISA | KHAC */
  nhaCungCap: string;
  /** USB_TOKEN | HSM | REMOTE_SIGNING | SMART_CA */
  loaiKy: string;
  endpoint?: string | null;
  clientId?: string | null;
  daDatBiMat: boolean;
  chungThuSo?: string | null;
  thuatToan: string;
  trangThai: number;
  laMacDinh: boolean;
}

export interface LuuCauHinhChuKySo {
  nhaCungCap: string;
  loaiKy: string;
  endpoint?: string | null;
  clientId?: string | null;
  clientSecret?: string | null;
  chungThuSo?: string | null;
  thuatToan: string;
  trangThai: number;
  laMacDinh: boolean;
}

export const apiChuKySo = {
  danhSach: () => layDuLieu<CauHinhChuKySo[]>('/api/v1/cau-hinh-chu-ky-so'),
  trangThai: () =>
    layDuLieu<{ coCauHinhHoatDong: boolean; mayChuSanSangKy: boolean; sanSang: boolean }>(
      '/api/v1/cau-hinh-chu-ky-so/trang-thai',
    ),
  them: (duLieu: LuuCauHinhChuKySo) => guiDuLieu<string>('/api/v1/cau-hinh-chu-ky-so', duLieu),
  sua: (id: string, duLieu: LuuCauHinhChuKySo) =>
    capNhatDuLieu(`/api/v1/cau-hinh-chu-ky-so/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/cau-hinh-chu-ky-so/${id}`),
};

// --- Liên thông hệ thống ngoài (chức năng 16, 41) --------------------------

export interface HeThongTichHop {
  id: string;
  ma: string;
  ten: string;
  endpointBase?: string | null;
  /** API_KEY | HMAC | OAUTH2 */
  loaiXacThuc: string;
  clientId?: string | null;
  daDatBiMat: boolean;
  scope?: string | null;
  /** THU_CONG | HANG_NGAY | HANG_TUAN */
  tanSuatDongBo: string;
  lanDongBoCuoi?: string | null;
  trangThai: number;
  cauHinhMapping?: Record<string, string> | null;
}

export interface LuuHeThongTichHop {
  ma: string;
  ten: string;
  endpointBase?: string | null;
  loaiXacThuc: string;
  clientId?: string | null;
  /** Bỏ trống khi sửa = giữ nguyên bí mật đang lưu. */
  clientSecret?: string | null;
  scope?: string | null;
  tanSuatDongBo: string;
  trangThai: number;
  cauHinhMapping?: Record<string, string> | null;
}

export interface BanGhiDongBo {
  maHoSo: string;
  tenSangKien: string;
  tacGiaChinh?: string | null;
  donVi?: string | null;
  linhVuc?: string | null;
  tongDiem?: number | null;
  mucCongNhan?: string | null;
  soQuyetDinh?: string | null;
  ngayCongNhan?: string | null;
  nam?: number | null;
}

export interface KetQuaDongBo {
  nhatKyId: string;
  tenHeThong: string;
  tongBanGhi: number;
  thanhCong: number;
  thatBai: number;
  trangThai: string;
  thongBaoLoi?: string | null;
}

export interface NhatKyDongBo {
  id: string;
  heThongTichHopId: string;
  chieu: string;
  loaiDuLieu?: string | null;
  tongBanGhi: number;
  thanhCong: number;
  thatBai: number;
  trangThaiDongBo: string;
  thongBaoLoi?: string | null;
  thoiGianBatDau: string;
  thoiGianKetThuc?: string | null;
}

export const apiTichHop = {
  danhSach: () => layDuLieu<HeThongTichHop[]>('/api/v1/tich-hop/he-thong'),
  them: (duLieu: LuuHeThongTichHop) => guiDuLieu<string>('/api/v1/tich-hop/he-thong', duLieu),
  sua: (id: string, duLieu: LuuHeThongTichHop) =>
    capNhatDuLieu(`/api/v1/tich-hop/he-thong/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/tich-hop/he-thong/${id}`),
  xemTruoc: (thamSo?: { dotDeNghiId?: string; nam?: number }) =>
    layDuLieu<BanGhiDongBo[]>('/api/v1/tich-hop/xem-truoc', { params: thamSo }),
  dongBo: (id: string, thamSo?: { dotDeNghiId?: string; nam?: number }) =>
    guiDuLieu<KetQuaDongBo>(
      `/api/v1/tich-hop/he-thong/${id}/dong-bo`,
      undefined,
      { params: thamSo },
    ),
  thuKetNoi: (id: string) =>
    guiDuLieu<KetQuaThuKetNoi>(`/api/v1/tich-hop/he-thong/${id}/thu-ket-noi`),
  guiLai: (nhatKyId: string) =>
    guiDuLieu<KetQuaDongBo>(`/api/v1/tich-hop/nhat-ky-dong-bo/${nhatKyId}/gui-lai`),
  nhatKyDongBo: (heThongId?: string, soDong = 50) =>
    layDuLieu<NhatKyDongBo[]>('/api/v1/tich-hop/nhat-ky-dong-bo', {
      params: { heThongId, soDong },
    }),
};

// --- Biểu mẫu xuất (chức năng 6) -------------------------------------------

export interface TruongBieuMau {
  placeholder: string;
  nguon: string;
  /** text | number | date | table */
  kieu: string;
  cot?: string[] | null;
  dinhDangHienThi?: string | null;
}

export interface BieuMauXuat extends DanhMucDto {
  /** PHIEU_TIEP_NHAN | PHIEU_DANH_GIA | BIEN_BAN_HOP | QUYET_DINH | TONG_HOP | KHAC */
  loai: string;
  /** DOCX | XLSX | PDF */
  dinhDang: string;
  fileTemplateId?: string | null;
  cauHinhTruong: TruongBieuMau[];
}

export interface LuuBieuMauXuat {
  ma: string;
  ten: string;
  moTa?: string | null;
  thuTu: number;
  trangThai: number;
  loai: string;
  dinhDang: string;
  fileTemplateId?: string | null;
  cauHinhTruong: TruongBieuMau[];
}

export interface TruongBieuMauKhaDung {
  nguon: string;
  nhan: string;
  kieu: string;
  viDu: string;
}

export interface BieuMauDaPhanGiai {
  tieuDe: string;
  phuDe?: string | null;
  dongDuLieu: { nhan: string; giaTri?: string | null }[];
  /** Placeholder ánh xạ sang nguồn không tồn tại — cần sửa lại cấu hình. */
  truongThieu: string[];
}

export interface CotBaoCao {
  ma: string;
  tieuDe: string;
  nguon: string;
  hamTongHop?: string | null;
  thuTu: number;
}

export interface BieuMauThongKeChiTiet extends DanhMucDto {
  loaiBaoCao?: string | null;
  cauHinhCot: CotBaoCao[];
  dinhDangXuat: string[];
}

export interface LuuBieuMauThongKe {
  ma: string;
  ten: string;
  moTa?: string | null;
  loaiBaoCao?: string | null;
  thuTu: number;
  trangThai: number;
  cauHinhCot: CotBaoCao[];
  dinhDangXuat: string[];
}

/** Chức năng 7 — Biểu mẫu thống kê dùng cho báo cáo tuỳ biến. */
export const apiBieuMauThongKe = taoApiDanhMuc<BieuMauThongKeChiTiet, LuuBieuMauThongKe>(
  '/api/v1/danh-muc/bieu-mau-thong-ke',
);

export const apiBieuMauXuat = {
  ...taoApiDanhMuc<BieuMauXuat, LuuBieuMauXuat>('/api/v1/danh-muc/bieu-mau-xuat'),
  truongKhaDung: (loai: string) =>
    layDuLieu<TruongBieuMauKhaDung[]>('/api/v1/danh-muc/bieu-mau-xuat/truong-kha-dung', {
      params: { loai },
    }),
  xemTruoc: (id: string, thamSo?: { hoSoId?: string; phienHopId?: string }) =>
    layDuLieu<BieuMauDaPhanGiai>(`/api/v1/danh-muc/bieu-mau-xuat/${id}/xem-truoc`, {
      params: thamSo,
    }),
  duongDanXemTruocPdf: (id: string, thamSo?: { hoSoId?: string; phienHopId?: string }) => {
    const q = new URLSearchParams();
    if (thamSo?.hoSoId) q.set('hoSoId', thamSo.hoSoId);
    if (thamSo?.phienHopId) q.set('phienHopId', thamSo.phienHopId);
    const chuoi = q.toString();

    return `/api/v1/danh-muc/bieu-mau-xuat/${id}/xem-truoc.pdf${chuoi ? `?${chuoi}` : ''}`;
  },
};

export interface MucMenuQuanTri {
  id: string;
  ma: string;
  ten: string;
  icon?: string | null;
  duongDan?: string | null;
  menuChaId?: string | null;
  thuTu: number;
  quyenMa?: string | null;
  /** WEB | MOBILE */
  loai: string;
  hienThi: boolean;
  moTabMoi: boolean;
}

export interface LuuMucMenu {
  ma: string;
  ten: string;
  icon?: string | null;
  duongDan?: string | null;
  menuChaId?: string | null;
  thuTu: number;
  quyenMa?: string | null;
  loai: string;
  hienThi: boolean;
  moTabMoi: boolean;
}

/** Chức năng 48 — quản trị cây menu điều hướng. */
export const apiCauHinhMenu = {
  danhSach: (loai: 'WEB' | 'MOBILE') =>
    layDuLieu<MucMenuQuanTri[]>('/api/v1/cau-hinh-menu', { params: { loai } }),
  them: (duLieu: LuuMucMenu) => guiDuLieu<string>('/api/v1/cau-hinh-menu', duLieu),
  sua: (id: string, duLieu: LuuMucMenu) => capNhatDuLieu(`/api/v1/cau-hinh-menu/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/cau-hinh-menu/${id}`),
  sapXep: (loai: 'WEB' | 'MOBILE', cay: { id: string; con?: unknown }[]) =>
    capNhatDuLieu(`/api/v1/cau-hinh-menu/sap-xep?loai=${loai}`, cay),
};

export interface YeuCauKyUsb {
  phienId: string;
  hashBase64: string;
  thuatToanBam: string;
  tenTep: string;
  hetHan: string;
}

export interface KetQuaKyUsb {
  nhatKyKySoId: string;
  tepChuKyId: string;
  chuTheChungThu: string;
  serialChungThu?: string | null;
  thoiGianKy: string;
}

/** Chức năng 49 — ký số bằng USB token (khoá bí mật không rời khỏi thiết bị người ký). */
export const apiKySoUsb = {
  chuanBi: (duLieu: { tepTinId: string; doiTuong: string; doiTuongId: string }) =>
    guiDuLieu<YeuCauKyUsb>('/api/v1/ky-so-usb/chuan-bi', duLieu),
  hoanTat: (phienId: string, duLieu: { chuKyBase64: string; chungThuBase64: string }) =>
    guiDuLieu<KetQuaKyUsb>(`/api/v1/ky-so-usb/${phienId}/hoan-tat`, duLieu),
  huy: (phienId: string) => xoaDuLieu(`/api/v1/ky-so-usb/${phienId}`),
};

export interface BanSaoLuu {
  ten: string;
  thoiGian: string;
  kichThuocByte: number;
  coCsdl: boolean;
  coTepDinhKem: boolean;
  dayDu: boolean;
}

export interface TinhTrangSaoLuu {
  daCauHinh: boolean;
  thuMuc: string;
  soBan: number;
  tongKichThuocByte: number;
  lanGanNhat?: string | null;
  soGioTuLanGanNhat?: number | null;
  /** BINH_THUONG | CANH_BAO | NGUY_HIEM | CHUA_CAU_HINH */
  mucCanhBao: string;
  thongDiep: string;
  danhSach: BanSaoLuu[];
}

/** Chỉ đọc: tạo bản sao lưu và khôi phục chạy bằng script trên máy chủ. */
export const apiSaoLuu = {
  tinhTrang: () => layDuLieu<TinhTrangSaoLuu>('/api/v1/sao-luu'),
};

export interface DongKetQuaNhapDanhMuc {
  soDong: number;
  ma: string;
  ten: string;
  hopLe: boolean;
  loi?: string | null;
}

export interface KetQuaNhapDanhMuc {
  chayThu: boolean;
  loai: string;
  tongDong: number;
  soHopLe: number;
  soLoi: number;
  soThemMoi: number;
  soCapNhat: number;
  chiTiet: DongKetQuaNhapDanhMuc[];
}

export interface TepTinDaTaiLen {
  id: string;
  tenGoc: string;
  kichThuoc: number;
  mimeType?: string | null;
  phanMoRong: string;
  hashSha256: string;
  ngayTaiLen: string;
}

/** Kích thước mỗi mảnh khi tải tệp lớn — phải khớp TepTinController.KichThuocManhToiDa. */
export const KICH_THUOC_MANH = 5 * 1024 * 1024;

/** Ngưỡng chuyển sang tải theo mảnh. Dưới ngưỡng thì gửi một lần cho nhanh. */
export const NGUONG_TAI_THEO_MANH = 8 * 1024 * 1024;

export interface PhienTaiManh {
  phienId: string;
  soManh: number;
  kichThuocManhToiDa: number;
  manhDaNhan: number[];
}

/**
 * Tải tệp lớn theo từng mảnh, có báo tiến độ.
 *
 * Mạng ở nhiều đơn vị chập chờn: một tệp 80MB gửi trong một yêu cầu mà rớt giữa chừng là mất
 * trắng. Chia mảnh thì chỉ phải gửi lại đúng mảnh hỏng.
 */
export async function taiTepLenTheoManh(
  tep: File,
  gan?: { sangKienId?: string; thanhPhanHoSoMa?: string; moTa?: string },
  onTienDo?: (phanTram: number) => void,
): Promise<TepTinDaTaiLen> {
  const soManh = Math.max(1, Math.ceil(tep.size / KICH_THUOC_MANH));

  const { data: batDau } = await http.post<{ duLieu: PhienTaiManh }>('/api/v1/tep-tin/manh/bat-dau', {
    tenTep: tep.name,
    tongKichThuoc: tep.size,
    soManh,
  });

  const phienId = batDau.duLieu.phienId;

  try {
    for (let i = 0; i < soManh; i += 1) {
      const form = new FormData();
      form.append('manh', tep.slice(i * KICH_THUOC_MANH, (i + 1) * KICH_THUOC_MANH));

      await http.post(`/api/v1/tep-tin/manh/${phienId}/${i}`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });

      onTienDo?.(Math.round(((i + 1) / soManh) * 100));
    }

    const { data } = await http.post<{ duLieu: TepTinDaTaiLen }>(
      `/api/v1/tep-tin/manh/${phienId}/hoan-tat`,
      undefined,
      { params: gan },
    );

    return data.duLieu;
  } catch (loi) {
    // Huỷ phiên để mảnh dở không nằm lại chiếm đĩa máy chủ cho tới khi hết hạn 6 giờ.
    await http.delete(`/api/v1/tep-tin/manh/${phienId}`).catch(() => undefined);
    throw loi;
  }
}

/** Đường dẫn xem trước tệp ngay trong trình duyệt (chỉ PDF và ảnh). */
export const duongDanXemTruocTep = (id: string) => `/api/v1/tep-tin/${id}/xem-truoc`;

export const apiTepTin = {
  lienKetTaiXuong: (id: string, soPhut = 10) =>
    layDuLieu<{ url: string; tenGoc: string; hetHanSauPhut: number }>(
      `/api/v1/tep-tin/${id}/lien-ket-tai-xuong`,
      { params: { soPhut } },
    ),
};

/** Tải một tệp lên kho dùng chung (không gắn vào hồ sơ nào). */
export async function taiTepLen(tep: File): Promise<TepTinDaTaiLen> {
  const form = new FormData();
  form.append('tep', tep);

  const { data } = await http.post<{ duLieu: TepTinDaTaiLen }>('/api/v1/tep-tin/tai-len', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  return data.duLieu;
}

// --- Bộ lọc yêu thích (chức năng 28) ---------------------------------------

export interface BoLocYeuThich {
  id: string;
  manHinh: string;
  ten: string;
  /** Chuỗi query đã lưu, ví dụ "trangThaiTong=DA_NOP&linhVucId=..." */
  thamSo: string;
  macDinh: boolean;
  ngayTao: string;
}

export const apiBoLoc = {
  danhSach: (manHinh: string) =>
    layDuLieu<BoLocYeuThich[]>('/api/v1/bo-loc-yeu-thich', { params: { manHinh } }),
  luu: (duLieu: { manHinh: string; ten: string; thamSo: string; macDinh: boolean }) =>
    guiDuLieu<string>('/api/v1/bo-loc-yeu-thich', duLieu),
  datMacDinh: (id: string) => guiDuLieu(`/api/v1/bo-loc-yeu-thich/${id}/mac-dinh`),
  xoa: (id: string) => xoaDuLieu(`/api/v1/bo-loc-yeu-thich/${id}`),
};

// --- Đăng nhập một lần SSO (chức năng 21, 41) ------------------------------

export const apiSso = {
  trangThai: () => layDuLieu<{ daCauHinh: boolean }>('/api/v1/xac-thuc/sso/trang-thai'),
  batDau: (duongDanTraVe: string) =>
    layDuLieu<{ diaChi: string; state: string; codeVerifier: string }>(
      '/api/v1/xac-thuc/sso/bat-dau',
      { params: { duongDanTraVe } },
    ),
  doiMa: (duLieu: { code: string; codeVerifier: string; duongDanTraVe: string }) =>
    guiDuLieu<KetQuaDangNhapSso>('/api/v1/xac-thuc/sso/doi-ma', duLieu),
  diaChiDangXuat: (duLieu: { idToken?: string | null; duongDanTraVe: string }) =>
    guiDuLieu<{ diaChi: string | null }>('/api/v1/xac-thuc/sso/dia-chi-dang-xuat', duLieu),
};

export interface KetQuaDangNhapSso {
  accessToken: string;
  refreshToken: string;
  hetHanSau: number;
  nguoiDung: {
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
  };
  buocDoiMatKhau: boolean;
}

// --- Biên bản họp hội đồng (nhóm IV) ---------------------------------------

export interface DongBienBanHoSo {
  sangKienId: string;
  maHoSo: string;
  tenSangKien: string;
  tacGiaChinh?: string | null;
  tongDiem?: number | null;
  soPhieuDongY: number;
  soPhieuKhongDongY: number;
  soPhieuYKienKhac: number;
  tyLeDongY: number;
  datNguong: boolean;
  ketLuanRieng?: string | null;
}

export interface ChuKyBienBan {
  thanhVienId: string;
  hoTen: string;
  chucDanh: string;
  daKy: boolean;
  thoiGianKy?: string | null;
}

export interface BienBanHop {
  id: string;
  phienHopId: string;
  soBienBan: string;
  /** NHAP | DA_LAP | DA_KY | DA_BAN_HANH */
  trangThaiBienBan: string;
  ngayLap?: string | null;
  tenHoiDong: string;
  tenPhien: string;
  thoiGianBatDau: string;
  diaDiem?: string | null;
  hinhThuc: string;
  ketLuanChung?: string | null;
  soThanhVien: number;
  soCoMat: number;
  thanhPhanDuHop: string[];
  thanhVienVang: string[];
  danhSachHoSo: DongBienBanHoSo[];
  chuKy: ChuKyBienBan[];
  tepTinId?: string | null;
}

export const apiBienBan = {
  lap: (phienHopId: string) =>
    guiDuLieu<BienBanHop>(`/api/v1/bien-ban-hop/phien-hop/${phienHopId}`),
  theoPhien: (phienHopId: string) =>
    layDuLieu<BienBanHop | null>(`/api/v1/bien-ban-hop/phien-hop/${phienHopId}`),
  ky: (id: string) => guiDuLieu(`/api/v1/bien-ban-hop/${id}/ky`),
  kySo: (id: string) => guiDuLieu<string>(`/api/v1/bien-ban-hop/${id}/ky-so`),
  lichSuKySo: (id: string) => layDuLieu<NhatKyKySo[]>(`/api/v1/bien-ban-hop/${id}/lich-su-ky-so`),
  duongDanPdf: (id: string) => `/api/v1/bien-ban-hop/${id}/xuat-pdf`,
};

// --- Liên thông theo bước quy trình (chức năng 16) --------------------------

export interface LienThongBuoc {
  id: string;
  quyTrinhId: string;
  buocId?: string | null;
  tenBuoc?: string | null;
  heThongTichHopId: string;
  tenHeThong: string;
  /** KHI_VAO_BUOC | KHI_HOAN_THANH | KHI_PHE_DUYET */
  suKien: string;
  loaiDuLieu?: string | null;
  dongBoHaiChieu: boolean;
  trangThai: number;
}

export interface LuuLienThongBuoc {
  buocId?: string | null;
  heThongTichHopId: string;
  suKien: string;
  loaiDuLieu?: string | null;
  cauHinhMapping?: Record<string, string> | null;
  dongBoHaiChieu: boolean;
  trangThai: number;
}

export const apiLienThongBuoc = {
  danhSach: (quyTrinhId: string) =>
    layDuLieu<LienThongBuoc[]>(`/api/v1/quy-trinh/${quyTrinhId}/lien-thong`),
  them: (quyTrinhId: string, duLieu: LuuLienThongBuoc) =>
    guiDuLieu<string>(`/api/v1/quy-trinh/${quyTrinhId}/lien-thong`, duLieu),
  sua: (id: string, duLieu: LuuLienThongBuoc) =>
    capNhatDuLieu(`/api/v1/quy-trinh/lien-thong/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/quy-trinh/lien-thong/${id}`),
};

// --- Cấp phê duyệt theo đợt / lĩnh vực (chức năng 5) ------------------------

export interface CapPheDuyet {
  id: string;
  dotDeNghiId?: string | null;
  tenDot?: string | null;
  linhVucId?: string | null;
  tenLinhVuc?: string | null;
  donViPheDuyetId: string;
  tenDonViPheDuyet: string;
  thuTuCap: number;
  ghiChu?: string | null;
}

export interface LuuCapPheDuyet {
  dotDeNghiId?: string | null;
  linhVucId?: string | null;
  donViPheDuyetId: string;
  thuTuCap: number;
  ghiChu?: string | null;
}

export const apiCapPheDuyet = {
  danhSach: (thamSo?: { dotDeNghiId?: string; linhVucId?: string }) =>
    layDuLieu<CapPheDuyet[]>('/api/v1/cap-phe-duyet', { params: thamSo }),
  them: (duLieu: LuuCapPheDuyet) => guiDuLieu<string>('/api/v1/cap-phe-duyet', duLieu),
  sua: (id: string, duLieu: LuuCapPheDuyet) => capNhatDuLieu(`/api/v1/cap-phe-duyet/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/cap-phe-duyet/${id}`),
};

// --- Nhật ký lỗi hệ thống ---------------------------------------------------

export interface NhatKyLoi {
  id: string;
  /** CANH_BAO | LOI | NGHIEM_TRONG */
  mucDo: string;
  nguon: string;
  thongBao: string;
  stackTrace?: string | null;
  duLieuNguCanh?: Record<string, unknown> | null;
  nguoiDungId?: string | null;
  diaChiIp?: string | null;
  thoiGian: string;
  daXuLy: boolean;
}

export const apiNhatKyLoi = {
  danhSach: (thamSo?: { trang?: number; soDong?: number; mucDo?: string; daXuLy?: boolean }) =>
    layPhanTrang<NhatKyLoi>('/api/v1/he-thong/nhat-ky/loi', thamSo),
  danhDauDaXuLy: (id: string) => guiDuLieu(`/api/v1/he-thong/nhat-ky/loi/${id}/da-xu-ly`),
};

// --- Mẫu thông báo và ngày nghỉ lễ (chức năng 50, 46) ----------------------

export interface MauThongBao {
  id: string;
  ma: string;
  ten: string;
  /** EMAIL | SMS | APP | TAT_CA */
  kenh: string;
  suKien: string;
  tieuDe: string;
  noiDung: string;
  danhSachBien: string[];
  trangThai: number;
}

export interface LuuMauThongBao {
  ma: string;
  ten: string;
  kenh: string;
  suKien: string;
  tieuDe: string;
  noiDung: string;
  danhSachBien: string[];
  trangThai: number;
}

export const apiMauThongBao = {
  danhSach: (thamSo?: { suKien?: string; kenh?: string }) =>
    layDuLieu<MauThongBao[]>('/api/v1/mau-thong-bao', { params: thamSo }),
  suKien: () => layDuLieu<{ ma: string; ten: string }[]>('/api/v1/mau-thong-bao/su-kien'),
  them: (duLieu: LuuMauThongBao) => guiDuLieu<string>('/api/v1/mau-thong-bao', duLieu),
  sua: (id: string, duLieu: LuuMauThongBao) =>
    capNhatDuLieu(`/api/v1/mau-thong-bao/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/mau-thong-bao/${id}`),
  xemTruoc: (id: string, bien?: Record<string, string>) =>
    guiDuLieu<{ tieuDe: string; noiDung: string }>(
      `/api/v1/mau-thong-bao/${id}/xem-truoc`,
      bien ?? {},
    ),
};

export interface NgayNghiLe {
  id: string;
  ngay: string;
  ten: string;
  lapLaiHangNam: boolean;
  trangThai: number;
}

export const apiNgayNghiLe = {
  danhSach: (nam?: number) =>
    layDuLieu<NgayNghiLe[]>('/api/v1/ngay-nghi-le', { params: { nam } }),
  them: (duLieu: { ngay: string; ten: string; lapLaiHangNam: boolean; trangThai: number }) =>
    guiDuLieu<string>('/api/v1/ngay-nghi-le', duLieu),
  sua: (
    id: string,
    duLieu: { ngay: string; ten: string; lapLaiHangNam: boolean; trangThai: number },
  ) => capNhatDuLieu(`/api/v1/ngay-nghi-le/${id}`, duLieu),
  xoa: (id: string) => xoaDuLieu(`/api/v1/ngay-nghi-le/${id}`),
};

export interface KetQuaThuKetNoi {
  thanhCong: boolean;
  tenHeThong: string;
  endpoint?: string | null;
  soMiliGiay: number;
  thongBao?: string | null;
}
