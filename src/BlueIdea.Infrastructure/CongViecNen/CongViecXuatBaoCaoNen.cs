using BlueIdea.Application.BaoCao;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Reporting;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.CongViecNen;

/// <summary>
/// Nhom X dac ta — Xuat bao cao lon CHAY NEN roi gui liên ket tai ve qua thong bao.
///
/// Bao cao toan he thong nhieu nam co the mat hang chuc giay: giu nguoi dung cho tren mot request
/// HTTP vua de time-out o reverse proxy, vua chiem mot luong xu ly cua may chu suot thoi gian do.
/// Chay nen thi nguoi dung bam xong lam viec khac, xong nhan thong bao kem lien ket tai ve.
///
/// Quyen: cong viec nay kiem lai quyen cua CHINH NGUOI YEU CAU truoc khi chay. Khong co ngu canh
/// HTTP nen khong the dua vao [Authorize] — bo qua buoc nay la mo duong xuat du lieu cho tai khoan
/// da bi thu hoi quyen giua chung.
/// </summary>
public sealed class CongViecXuatBaoCaoNen
{
    private readonly DichVuBaoCao _baoCao;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly ILuuTruTep _luuTru;
    private readonly IDichVuThongBao _thongBao;
    private readonly IAppDbContext _db;
    private readonly IDongHoHeThong _dongHo;
    private readonly ILogger<CongViecXuatBaoCaoNen> _logger;

    public CongViecXuatBaoCaoNen(
        DichVuBaoCao baoCao, IDichVuPhanQuyen phanQuyen, ILuuTruTep luuTru,
        IDichVuThongBao thongBao, IAppDbContext db, IDongHoHeThong dongHo,
        ILogger<CongViecXuatBaoCaoNen> logger)
    {
        _baoCao = baoCao;
        _phanQuyen = phanQuyen;
        _luuTru = luuTru;
        _thongBao = thongBao;
        _db = db;
        _dongHo = dongHo;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ChayAsync(
        string loaiBaoCao, ThamSoBaoCao thamSo, Guid nguoiYeuCauId, CancellationToken ct = default)
    {
        var duQuyen = await _phanQuyen
            .KiemTraQuyenAsync(nguoiYeuCauId, MaQuyen.BaoCaoXuat, ct)
            .ConfigureAwait(false);

        if (!duQuyen)
        {
            _logger.LogWarning(
                "Bỏ qua xuất báo cáo nền '{Loai}': người dùng {NguoiDungId} không còn quyền xuất.",
                loaiBaoCao, nguoiYeuCauId);
            return;
        }

        var (tenTep, noiDung) = await SinhTepAsync(loaiBaoCao, thamSo, ct).ConfigureAwait(false);

        var tenLuuTru = $"{Guid.NewGuid():N}.xlsx";

        var duongDan = await _luuTru
            .TaiLenAsync(
                new MemoryStream(noiDung), tenLuuTru,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "blueidea", ct)
            .ConfigureAwait(false);

        var tepTin = new TepTin
        {
            Id = Guid.NewGuid(),
            TenGoc = tenTep,
            TenLuuTru = tenLuuTru,
            DuongDan = duongDan,
            Bucket = "blueidea",
            KichThuoc = noiDung.Length,
            MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            PhanMoRong = ".xlsx",
            HashSha256 = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(noiDung)).ToLowerInvariant(),

            // Gan nguoi tai len = nguoi yeu cau: quy tac truy cap tep (chuc nang 25) cho phep
            // chinh nguoi tai len mo tep khong gan vao ho so nao — dung nguoi khac la ho khong
            // tai duoc chinh bao cao minh dat.
            NguoiTaiLenId = nguoiYeuCauId,
            NgayTaiLen = _dongHo.BayGio,
            TrangThaiOcr = TrangThaiOcrTep.KhongCan
        };

        _db.TepTin.Add(tepTin);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _thongBao.GuiTrongUngDungAsync(
            nguoiYeuCauId,
            "Báo cáo đã sẵn sàng tải về",
            $"Báo cáo '{tenTep}' đã xuất xong. Bấm để tải về.",
            $"/api/v1/tep-tin/{tepTin.Id}/tai-ve",
            ct: ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Đã xuất báo cáo nền '{Loai}' ({KichThuoc} byte) cho người dùng {NguoiDungId}.",
            loaiBaoCao, noiDung.Length, nguoiYeuCauId);
    }

    private async Task<(string TenTep, byte[] NoiDung)> SinhTepAsync(
        string loaiBaoCao, ThamSoBaoCao thamSo, CancellationToken ct)
    {
        switch (loaiBaoCao)
        {
            case "sang-kien-dat":
            {
                var duLieu = await _baoCao.SangKienDatAsync(thamSo, ct).ConfigureAwait(false);
                return ("sang-kien-dat.xlsx",
                    BoXuatExcel.Xuat("Sang kien dat", "DANH SÁCH SÁNG KIẾN ĐƯỢC CÔNG NHẬN",
                        duLieu, CotSangKien()));
            }

            case "sang-kien-chua-dat":
            {
                var duLieu = await _baoCao.SangKienChuaDatAsync(thamSo, ct).ConfigureAwait(false);
                return ("sang-kien-chua-dat.xlsx",
                    BoXuatExcel.Xuat("Sang kien chua dat", "DANH SÁCH SÁNG KIẾN CHƯA ĐẠT",
                        duLieu, CotSangKien()));
            }

            case "theo-don-vi":
            {
                var duLieu = await _baoCao.TheoDonViAsync(thamSo, ct).ConfigureAwait(false);
                return ("thong-ke-theo-don-vi.xlsx",
                    BoXuatExcel.Xuat("Theo don vi", "THỐNG KÊ SÁNG KIẾN THEO ĐƠN VỊ", duLieu,
                        new List<CotXuat<DongBaoCaoDonVi>>
                        {
                            new("Mã đơn vị", x => x.MaDonVi, 18),
                            new("Tên đơn vị", x => x.TenDonVi, 40),
                            new("Tổng hồ sơ", x => x.TongSo, 14),
                            new("Đạt", x => x.SoDat, 10),
                            new("Không đạt", x => x.SoKhongDat, 14),
                            new("Đang xử lý", x => x.SoDangXuLy, 14),
                            new("Tỷ lệ đạt (%)", x => x.TyLeDat, 16)
                        }));
            }

            case "theo-tac-gia":
            {
                var duLieu = await _baoCao.TheoTacGiaAsync(thamSo, ct).ConfigureAwait(false);
                return ("thong-ke-theo-tac-gia.xlsx",
                    BoXuatExcel.Xuat("Theo tac gia", "THỐNG KÊ SÁNG KIẾN THEO TÁC GIẢ", duLieu,
                        new List<CotXuat<DongBaoCaoTacGia>>
                        {
                            new("Họ và tên", x => x.HoTen, 30),
                            new("Đơn vị công tác", x => x.DonViCongTac, 35),
                            new("Chức vụ", x => x.ChucVu, 25),
                            new("Tổng số", x => x.TongSo, 12),
                            new("Là tác giả chính", x => x.SoLaTacGiaChinh, 18),
                            new("Đạt", x => x.SoDat, 10),
                            new("Điểm trung bình", x => x.DiemTrungBinh, 18),
                            new("Tỷ lệ đạt (%)", x => x.TyLeDat, 16)
                        }));
            }

            case "thoi-gian-xu-ly":
            {
                var duLieu = await _baoCao.ThoiGianXuLyAsync(thamSo, ct).ConfigureAwait(false);
                return ("thoi-gian-xu-ly.xlsx",
                    BoXuatExcel.Xuat("Thoi gian xu ly", "THỜI GIAN XỬ LÝ TRUNG BÌNH THEO BƯỚC",
                        duLieu,
                        new List<CotXuat<DongThoiGianXuLy>>
                        {
                            new("Bước xử lý", x => x.TenBuoc, 40),
                            new("Số lượt", x => x.SoLuot, 12),
                            new("Số ngày trung bình", x => x.SoNgayTrungBinh, 20),
                            new("Lâu nhất (ngày)", x => x.SoNgayLauNhat, 18),
                            new("Số lượt quá hạn", x => x.SoLuotQuaHan, 18)
                        }));
            }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(loaiBaoCao), loaiBaoCao, "Loại báo cáo không hỗ trợ xuất nền.");
        }
    }

    /// <summary>Cot xuat cho hai bao cao danh sach — giu giong ban xuat truc tiep tren man hinh.</summary>
    private static List<CotXuat<DongBaoCaoSangKien>> CotSangKien() => new()
    {
        new("Mã hồ sơ", x => x.MaHoSo, 18),
        new("Tên sáng kiến", x => x.TenSangKien, 50),
        new("Tác giả", x => x.TacGia, 30),
        new("Đơn vị", x => x.TenDonVi, 30),
        new("Lĩnh vực", x => x.TenLinhVuc, 25),
        new("Đợt đề nghị", x => x.TenDot, 25),
        new("Tổng điểm", x => x.TongDiem, 14),
        new("Mức công nhận", x => x.TenMucCongNhan, 22),
        new("Kết quả", x => x.KetQua, 14),
        new("Lý do", x => x.LyDo, 40),
        new("Ngày công nhận", x => x.NgayCongNhan, 18),
        new("Số quyết định", x => x.SoQuyetDinh, 20)
    };
}
