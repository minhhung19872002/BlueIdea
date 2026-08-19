using BlueIdea.Application.Chung;
using BlueIdea.Application.HoiDong;
using BlueIdea.Application.TrungLap;
using BlueIdea.Domain.Ai;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.HoiDong;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.SangKien;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.CongViecNen;

/// <summary>
/// Chuc nang 26 - Trich xuat van ban cho tep vua tai len roi luu vao <c>noi_dung_trich_xuat</c>.
/// Nho vay kiem tra trung lap doc duoc ca noi dung file PDF/anh scan chu khong chi cac o nhap tay.
/// </summary>
public sealed class CongViecTrichXuatVanBan
{
    private readonly IAppDbContext _db;
    private readonly ILuuTruTep _luuTru;
    private readonly IDichVuOcr _ocr;
    private readonly IHangDoiCongViecNen _hangDoi;
    private readonly ILogger<CongViecTrichXuatVanBan> _logger;

    public CongViecTrichXuatVanBan(
        IAppDbContext db, ILuuTruTep luuTru, IDichVuOcr ocr, IHangDoiCongViecNen hangDoi,
        ILogger<CongViecTrichXuatVanBan> logger)
    {
        _db = db;
        _luuTru = luuTru;
        _ocr = ocr;
        _hangDoi = hangDoi;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 600 })]
    public async Task ChayAsync(Guid tepTinId, CancellationToken ct = default)
    {
        var tep = await _db.TepTin.FirstOrDefaultAsync(x => x.Id == tepTinId, ct)
            .ConfigureAwait(false);

        if (tep is null)
        {
            return;
        }

        if (!_ocr.HoTro(tep.PhanMoRong))
        {
            tep.TrangThaiOcr = TrangThaiOcrTep.KhongCan;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await DaySangKiemTraTrungLapAsync(tepTinId, ct).ConfigureAwait(false);
            return;
        }

        tep.TrangThaiOcr = TrangThaiOcrTep.DangXuLy;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        try
        {
            await using var luong = await _luuTru
                .TaiXuongAsync(tep.Bucket, tep.DuongDan, ct)
                .ConfigureAwait(false);

            var ketQua = await _ocr
                .TrichXuatAsync(luong, tep.TenGoc, tep.MimeType, ct)
                .ConfigureAwait(false);

            if (!ketQua.ThanhCong)
            {
                tep.TrangThaiOcr = TrangThaiOcrTep.Loi;
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                // Nem lai de Hangfire retry: dich vu OCR co the chi tam thoi khong san sang.
                throw new InvalidOperationException(
                    ketQua.ThongBaoLoi ?? "Trích xuất văn bản thất bại.");
            }

            tep.NoiDungTrichXuat = ketQua.VanBan;
            tep.TrangThaiOcr = TrangThaiOcrTep.HoanThanh;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Đã trích xuất {SoKyTu} ký tự từ {TenTep} bằng phương pháp {PhuongPhap}.",
                ketQua.VanBan.Length, tep.TenGoc, ketQua.PhuongPhap);

            await DaySangKiemTraTrungLapAsync(tepTinId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            tep.TrangThaiOcr = TrangThaiOcrTep.Loi;
            await _db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

            _logger.LogError(ex, "Lỗi trích xuất văn bản cho tệp {TepTinId}.", tepTinId);
            throw;
        }
    }

    /// <summary>
    /// Sau khi mot tep xong, day nhung ho so DA NOP dinh kem tep do sang kiem tra trung lap —
    /// nhung chi khi tep cuoi cung cua ho so da xu ly xong, de ket qua khong bo sot noi dung file.
    /// </summary>
    private async Task DaySangKiemTraTrungLapAsync(Guid tepTinId, CancellationToken ct)
    {
        var hoSoIds = await _db.SangKienTepDinhKem.AsNoTracking()
            .Where(x => x.TepTinId == tepTinId)
            .Select(x => x.SangKienId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var hoSoId in hoSoIds)
        {
            var daNop = await _db.SangKien.AsNoTracking()
                .AnyAsync(x => x.Id == hoSoId
                               && x.NgayNop != null
                               && x.TrangThaiKiemTraTrungLap == TrangThaiKiemTraTrungLap.ChuaKiemTra,
                    ct)
                .ConfigureAwait(false);

            if (!daNop)
            {
                continue;
            }

            var conCho = await _db.SangKienTepDinhKem.AsNoTracking()
                .Where(x => x.SangKienId == hoSoId)
                .Join(_db.TepTin.AsNoTracking(), x => x.TepTinId, t => t.Id, (_, t) => t.TrangThaiOcr)
                .AnyAsync(tt => tt == TrangThaiOcrTep.ChuaXuLy || tt == TrangThaiOcrTep.DangXuLy, ct)
                .ConfigureAwait(false);

            if (!conCho)
            {
                _hangDoi.XepLichKiemTraTrungLap(hoSoId);
            }
        }
    }
}

/// <summary>
/// Luoi an toan cho kiem tra trung lap.
///
/// Duong chinh la: nop ho so -> OCR xong -> tu day sang kiem tra. Nhung neu OCR that bai han,
/// dich vu AI chet dung luc nop, hoac ho so duoc nap bang duong khac, se con ho so mac ket o
/// trang thai CHUA_KIEM_TRA. Vong quet nay don not chung.
/// </summary>
public sealed class CongViecQuetTrungLapConThieu
{
    private const int SoHoSoMoiLuot = 20;

    private readonly IAppDbContext _db;
    private readonly IHangDoiCongViecNen _hangDoi;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly ILogger<CongViecQuetTrungLapConThieu> _logger;

    public CongViecQuetTrungLapConThieu(
        IAppDbContext db, IHangDoiCongViecNen hangDoi, IDichVuCauHinh cauHinh,
        ILogger<CongViecQuetTrungLapConThieu> logger)
    {
        _db = db;
        _hangDoi = hangDoi;
        _cauHinh = cauHinh;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ChayAsync(CancellationToken ct = default)
    {
        var batKiemTra = await _cauHinh
            .LayAsync(KhoaCauHinh.TuDongKiemTraTrungLap, true, ct)
            .ConfigureAwait(false);

        if (!batKiemTra)
        {
            return;
        }

        var conThieu = await _db.SangKien.AsNoTracking()
            .Where(x => x.NgayNop != null
                        && x.TrangThaiKiemTraTrungLap == TrangThaiKiemTraTrungLap.ChuaKiemTra)
            .OrderBy(x => x.NgayNop)
            .Take(SoHoSoMoiLuot)
            .Select(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var id in conThieu)
        {
            _hangDoi.XepLichKiemTraTrungLap(id);
        }

        if (conThieu.Count > 0)
        {
            _logger.LogInformation(
                "Đã xếp lịch kiểm tra trùng lặp bù cho {SoHoSo} hồ sơ.", conThieu.Count);
        }
    }
}

/// <summary>Chay kiem tra trung lap sau khi ho so duoc nop (chuc nang 26).</summary>
public sealed class CongViecKiemTraTrungLapNen
{
    private readonly DichVuKiemTraTrungLap _dichVu;
    private readonly ILogger<CongViecKiemTraTrungLapNen> _logger;

    public CongViecKiemTraTrungLapNen(
        DichVuKiemTraTrungLap dichVu, ILogger<CongViecKiemTraTrungLapNen> logger)
    {
        _dichVu = dichVu;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
    public async Task ChayAsync(Guid sangKienId, CancellationToken ct = default)
    {
        var ketQua = await _dichVu.ChayAsync(sangKienId, batBuocChayLai: false, ct)
            .ConfigureAwait(false);

        if (ketQua is not null)
        {
            _logger.LogInformation(
                "Kiểm tra trùng lặp hồ sơ {SangKienId}: tỷ lệ cao nhất {TyLe}%.",
                sangKienId, ketQua.TyLeCaoNhat);
        }
    }
}

/// <summary>
/// Nhac han xu ly va han cham diem.
///
/// Chay dinh ky, moi ho so chi nhac MOT lan trong <see cref="SoGioKhongNhacLai"/> gio - kiem tra
/// bang chinh bang thong bao nen khong can them cot trang thai rieng.
/// </summary>
public sealed class CongViecNhacHan
{
    /// <summary>Khoang lang giua hai lan nhac cho cung mot doi tuong.</summary>
    private const int SoGioKhongNhacLai = 20;

    private readonly IAppDbContext _db;
    private readonly IDichVuThongBao _thongBao;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly ILogger<CongViecNhacHan> _logger;

    public CongViecNhacHan(
        IAppDbContext db, IDichVuThongBao thongBao, IDongHoHeThong dongHo,
        IDichVuCauHinh cauHinh, ILogger<CongViecNhacHan> logger)
    {
        _db = db;
        _thongBao = thongBao;
        _dongHo = dongHo;
        _cauHinh = cauHinh;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ChayAsync(CancellationToken ct = default)
    {
        var soNgayTruoc = await _cauHinh
            .LayAsync(KhoaCauHinh.SoNgayNhacTruocHan, 1, ct)
            .ConfigureAwait(false);

        var bayGio = _dongHo.BayGio;
        var nguong = bayGio.AddDays(soNgayTruoc);
        var khongNhacTruoc = bayGio.AddHours(-SoGioKhongNhacLai);

        var soDaNhac = 0;

        // --- Han xu ly buoc quy trinh ---
        var buocSapHetHan = await _db.SangKienXuLy.AsNoTracking()
            .Where(x => x.ThoiGianXuLy == null
                        && x.NguoiXuLyId != null
                        && x.HanXuLy != null
                        && x.HanXuLy <= nguong)
            .Select(x => new
            {
                x.SangKienId,
                x.NguoiXuLyId,
                x.HanXuLy,
                x.TenBuocSnapshot
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var buoc in buocSapHetHan)
        {
            if (await DaNhacGanDayAsync(buoc.SangKienId, buoc.NguoiXuLyId!.Value, khongNhacTruoc, ct)
                    .ConfigureAwait(false))
            {
                continue;
            }

            var hoSo = await _db.SangKien.AsNoTracking()
                .Where(x => x.Id == buoc.SangKienId)
                .Select(x => new { x.MaHoSo, x.TenSangKien })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (hoSo is null)
            {
                continue;
            }

            var quaHan = buoc.HanXuLy < bayGio;

            await _thongBao.GuiTheoSuKienAsync(
                SuKienThongBao.SapHetHan,
                new[] { buoc.NguoiXuLyId!.Value },
                new Dictionary<string, object?>
                {
                    ["sangKienId"] = buoc.SangKienId,
                    ["maHoSo"] = hoSo.MaHoSo,
                    ["tenSangKien"] = hoSo.TenSangKien,
                    ["tenBuoc"] = buoc.TenBuocSnapshot,
                    ["hanXuLy"] = buoc.HanXuLy?.ToString("dd/MM/yyyy HH:mm"),
                    ["tinhTrang"] = quaHan ? "ĐÃ QUÁ HẠN" : "sắp đến hạn",
                    ["duongDan"] = DuongDanGiaoDien.ChiTietHoSo(buoc.SangKienId)
                },
                ct).ConfigureAwait(false);

            soDaNhac++;
        }

        // --- Han cham diem cua thanh vien hoi dong ---
        var phanCongSapHetHan = await _db.SangKienPhanCong.AsNoTracking()
            .Where(x => x.TrangThaiPhanCong != TrangThaiPhanCongCham.DaCham
                        && x.HanHoanThanh != null
                        && x.HanHoanThanh <= nguong)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var phanCong in phanCongSapHetHan)
        {
            var nguoiDungId = await _db.HoiDongThanhVien.AsNoTracking()
                .Where(x => x.Id == phanCong.ThanhVienId)
                .Select(x => x.NguoiDungId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            // Thanh vien hoi dong co the la nguoi ngoai he thong (chi luu ho ten) -> khong nhac duoc.
            if (nguoiDungId is null)
            {
                continue;
            }

            if (await DaNhacGanDayAsync(phanCong.SangKienId, nguoiDungId.Value, khongNhacTruoc, ct)
                    .ConfigureAwait(false))
            {
                continue;
            }

            var hoSo = await _db.SangKien.AsNoTracking()
                .Where(x => x.Id == phanCong.SangKienId)
                .Select(x => new { x.MaHoSo, x.TenSangKien })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (hoSo is null)
            {
                continue;
            }

            await _thongBao.GuiTheoSuKienAsync(
                SuKienThongBao.SapHetHan,
                new[] { nguoiDungId.Value },
                new Dictionary<string, object?>
                {
                    ["sangKienId"] = phanCong.SangKienId,
                    ["maHoSo"] = hoSo.MaHoSo,
                    ["tenSangKien"] = hoSo.TenSangKien,
                    ["tenBuoc"] = "Chấm điểm",
                    ["hanXuLy"] = phanCong.HanHoanThanh?.ToString("dd/MM/yyyy HH:mm"),
                    ["tinhTrang"] = phanCong.HanHoanThanh < bayGio ? "ĐÃ QUÁ HẠN" : "sắp đến hạn",
                    ["duongDan"] = DuongDanGiaoDien.ChamDiem(phanCong.SangKienId)
                },
                ct).ConfigureAwait(false);

            soDaNhac++;
        }

        // --- Danh dau qua han de giao dien hien badge canh bao ---
        var soQuaHan = await _db.SangKienXuLy
            .Where(x => x.ThoiGianXuLy == null && x.HanXuLy != null && x.HanXuLy < bayGio && !x.QuaHan)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.QuaHan, true), ct)
            .ConfigureAwait(false);

        var soPhanCongQuaHan = await _db.SangKienPhanCong
            .Where(x => x.TrangThaiPhanCong == TrangThaiPhanCongCham.ChuaCham
                        && x.HanHoanThanh != null && x.HanHoanThanh < bayGio)
            .ExecuteUpdateAsync(
                x => x.SetProperty(p => p.TrangThaiPhanCong, TrangThaiPhanCongCham.QuaHan), ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Nhắc hạn: đã gửi {SoDaNhac} thông báo, đánh dấu quá hạn {SoQuaHan} bước xử lý "
            + "và {SoPhanCongQuaHan} lượt chấm.",
            soDaNhac, soQuaHan, soPhanCongQuaHan);
    }

    private Task<bool> DaNhacGanDayAsync(
        Guid sangKienId, Guid nguoiNhanId, DateTimeOffset khongNhacTruoc, CancellationToken ct)
        => _db.ThongBao.AsNoTracking()
            .AnyAsync(x => x.NguoiNhanId == nguoiNhanId
                           && x.DoiTuongId == sangKienId
                           && x.LoaiSuKien == SuKienThongBao.SapHetHan
                           && x.ThoiGian >= khongNhacTruoc, ct);
}

/// <summary>
/// Chuc nang 3 - Tu dong dong dot de nghi khi qua han nop.
/// Chi ap dung cho dot bat co <c>tu_dong_khoa</c>; quan tri vien van dong tay duoc bat cu luc nao.
/// </summary>
public sealed class CongViecDongDotHetHan
{
    private readonly IAppDbContext _db;
    private readonly IDongHoHeThong _dongHo;
    private readonly ILogger<CongViecDongDotHetHan> _logger;

    public CongViecDongDotHetHan(
        IAppDbContext db, IDongHoHeThong dongHo, ILogger<CongViecDongDotHetHan> logger)
    {
        _db = db;
        _dongHo = dongHo;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ChayAsync(CancellationToken ct = default)
    {
        var bayGio = _dongHo.BayGio;

        var canDong = await _db.DotDeNghi
            .Where(x => x.TrangThaiDot == TrangThaiDot.DangMo
                        && x.TuDongKhoa
                        && x.HanNopHoSo != null
                        && x.HanNopHoSo < bayGio)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (canDong.Count == 0)
        {
            return;
        }

        foreach (var dot in canDong)
        {
            dot.TrangThaiDot = TrangThaiDot.DaDong;

            _logger.LogInformation(
                "Đã tự động đóng đợt '{TenDot}' vì quá hạn nộp {HanNop:dd/MM/yyyy HH:mm}.",
                dot.Ten, dot.HanNopHoSo);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Chuc nang 50 - Rut hang doi email/SMS va gui that.
///
/// Ban ghi that bai duoc tang <c>so_lan_thu</c> va thu lai o luot sau, toi da
/// <see cref="SoLanThuToiDa"/> lan. Rieng truong hop CHUA CAU HINH may chu gui tin thi giu nguyen
/// trang thai CHO_GUI - de khi quan tri vien cau hinh xong, hang doi tu chay tiep.
/// </summary>
public sealed class CongViecGuiHangDoi
{
    private const int SoLanThuToiDa = 5;
    private const int SoTinMoiLuot = 50;

    private readonly IAppDbContext _db;
    private readonly IDichVuGuiTin _guiTin;
    private readonly IDongHoHeThong _dongHo;
    private readonly ILogger<CongViecGuiHangDoi> _logger;

    public CongViecGuiHangDoi(
        IAppDbContext db, IDichVuGuiTin guiTin, IDongHoHeThong dongHo,
        ILogger<CongViecGuiHangDoi> logger)
    {
        _db = db;
        _guiTin = guiTin;
        _dongHo = dongHo;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ChayAsync(CancellationToken ct = default)
    {
        var danhSach = await _db.HangDoiGuiTin
            .Where(x => x.TrangThaiGui == TrangThaiGuiTin.ChoGui && x.SoLanThu < SoLanThuToiDa)
            .OrderBy(x => x.NgayTao)
            .Take(SoTinMoiLuot)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (danhSach.Count == 0)
        {
            return;
        }

        var soThanhCong = 0;
        var soLoi = 0;
        var chuaCauHinh = false;

        foreach (var tin in danhSach)
        {
            try
            {
                if (tin.Kenh == "SMS")
                {
                    await _guiTin.GuiSmsAsync(tin.NguoiNhan, tin.NoiDung, ct).ConfigureAwait(false);
                }
                else
                {
                    await _guiTin.GuiEmailAsync(tin.NguoiNhan, tin.TieuDe, tin.NoiDung, ct)
                        .ConfigureAwait(false);
                }

                tin.TrangThaiGui = TrangThaiGuiTin.DaGui;
                tin.ThoiGianGui = _dongHo.BayGio;
                tin.ThongBaoLoi = null;
                soThanhCong++;
            }
            catch (ChuaCauHinhGuiTinException ex)
            {
                // Giu nguyen CHO_GUI va KHONG tang so lan thu: day khong phai loi cua ban tin.
                tin.ThongBaoLoi = ex.Message;
                chuaCauHinh = true;
            }
            catch (Exception ex)
            {
                tin.SoLanThu++;
                tin.ThongBaoLoi = ex.Message;
                soLoi++;

                if (tin.SoLanThu >= SoLanThuToiDa)
                {
                    tin.TrangThaiGui = TrangThaiGuiTin.Loi;
                }

                _logger.LogWarning(ex,
                    "Gửi {Kenh} tới {NguoiNhan} thất bại (lần {SoLanThu}/{ToiDa}).",
                    tin.Kenh, tin.NguoiNhan, tin.SoLanThu, SoLanThuToiDa);
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (chuaCauHinh)
        {
            _logger.LogWarning(
                "Còn {SoTin} tin chờ gửi nhưng chưa cấu hình máy chủ email/SMS "
                + "(Quản trị → Cấu hình hệ thống).",
                danhSach.Count - soThanhCong - soLoi);
        }

        if (soThanhCong > 0 || soLoi > 0)
        {
            _logger.LogInformation(
                "Hàng đợi gửi tin: {SoThanhCong} thành công, {SoLoi} lỗi.", soThanhCong, soLoi);
        }
    }
}

/// <summary>
/// Chuc nang 33 tu dong — khi buoc quy trinh CHAM_DIEM bat dau, tu dong phan cong
/// thanh vien hoi dong cham ho so. Xung dot loi ich duoc loai tru (tac gia khong cham ho so minh).
/// Luy dang: goi nhieu lan cho cung (sangKien, buoc) khong tao ban ghi trung.
/// </summary>
public sealed class CongViecPhanCongCham
{
    private readonly IAppDbContext _db;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuThongBao _thongBao;
    private readonly ILogger<CongViecPhanCongCham> _logger;

    public CongViecPhanCongCham(
        IAppDbContext db, IDongHoHeThong dongHo, IDichVuThongBao thongBao,
        ILogger<CongViecPhanCongCham> logger)
    {
        _db = db;
        _dongHo = dongHo;
        _thongBao = thongBao;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
    public async Task ChayAsync(Guid sangKienId, Guid buocMoiId, CancellationToken ct = default)
    {
        var buoc = await _db.QuyTrinhBuoc.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == buocMoiId, ct)
            .ConfigureAwait(false);

        if (buoc?.HoiDongId is null)
        {
            _logger.LogWarning(
                "PHAN_CONG_CHAM bỏ qua: bước {BuocId} không có hội đồng gắn kết cho sáng kiến {SangKienId}.",
                buocMoiId, sangKienId);
            return;
        }

        var hoiDongId = buoc.HoiDongId.Value;

        var hoiDong = await _db.HoiDong.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == hoiDongId, ct)
            .ConfigureAwait(false);

        if (hoiDong is null)
        {
            _logger.LogWarning(
                "PHAN_CONG_CHAM bỏ qua: hội đồng {HoiDongId} không tồn tại cho sáng kiến {SangKienId}.",
                hoiDongId, sangKienId);
            return;
        }

        var thanhVien = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.HoiDongId == hoiDongId
                        && x.QuyenChamDiem
                        && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (thanhVien.Count == 0)
        {
            _logger.LogWarning(
                "PHAN_CONG_CHAM bỏ qua: hội đồng {TenHoiDong} không có thành viên nào có quyền chấm điểm.",
                hoiDong.Ten);
            return;
        }

        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is null)
        {
            return;
        }

        var tacGiaIds = hoSo.DanhSachTacGia
            .Where(t => t.NguoiDungId.HasValue)
            .Select(t => t.NguoiDungId!.Value)
            .ToHashSet();

        var duocPhep = thanhVien
            .Where(tv => tv.NguoiDungId is null || !tacGiaIds.Contains(tv.NguoiDungId.Value))
            .ToList();

        if (duocPhep.Count < thanhVien.Count)
        {
            _logger.LogInformation(
                "PHAN_CONG_CHAM: loại {SoBiLoai} thành viên do xung đột lợi ích cho sáng kiến {MaHoSo}.",
                thanhVien.Count - duocPhep.Count, hoSo.MaHoSo);
        }

        if (duocPhep.Count == 0)
        {
            _logger.LogWarning(
                "PHAN_CONG_CHAM bỏ qua: tất cả thành viên hội đồng đều bị xung đột lợi ích cho sáng kiến {MaHoSo}.",
                hoSo.MaHoSo);
            return;
        }

        var hanXuLy = await _db.SangKienXuLy.AsNoTracking()
            .Where(x => x.SangKienId == sangKienId && x.BuocId == buocMoiId && x.HanXuLy != null)
            .OrderByDescending(x => x.NgayTao)
            .Select(x => x.HanXuLy)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var daPhanCong = 0;

        foreach (var tv in duocPhep)
        {
            var daCo = await _db.SangKienPhanCong
                .AnyAsync(x => x.SangKienId == sangKienId && x.ThanhVienId == tv.Id, ct)
                .ConfigureAwait(false);

            if (daCo)
            {
                continue;
            }

            _db.SangKienPhanCong.Add(new SangKienPhanCong
            {
                SangKienId = sangKienId,
                HoiDongId = hoiDongId,
                ThanhVienId = tv.Id,
                NguoiPhanCongId = null,
                NgayPhanCong = _dongHo.BayGio,
                HanHoanThanh = hanXuLy,
                TrangThaiPhanCong = TrangThaiPhanCong.ChuaCham
            });

            daPhanCong++;
        }

        if (daPhanCong > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var nguoiNhan = duocPhep
            .Where(t => t.NguoiDungId.HasValue)
            .Select(t => t.NguoiDungId!.Value)
            .Distinct()
            .ToList();

        if (nguoiNhan.Count > 0 && daPhanCong > 0)
        {
            await _thongBao.GuiTheoSuKienAsync(
                SuKienThongBao.DuocPhanCongCham, nguoiNhan,
                new Dictionary<string, object?>
                {
                    ["soHoSo"] = 1,
                    ["tenHoiDong"] = hoiDong.Ten,
                    ["hanHoanThanh"] = hanXuLy,
                    ["duongDan"] = DuongDanGiaoDien.DanhSachViecDanhGia
                }, ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "PHAN_CONG_CHAM tự động: đã phân công {SoLuot} thành viên chấm sáng kiến {MaHoSo} "
            + "trong hội đồng {TenHoiDong}.",
            daPhanCong, hoSo.MaHoSo, hoiDong.Ten);
    }
}

/// <summary>
/// Hanh dong tu dong TAO_BIEN_BAN — khi buoc quy trinh (vd CHAM_DIEM, BO_PHIEU) hoan thanh,
/// tu dong lap bien ban phien hop hoi dong neu phien da ket thuc.
/// Luy dang: goi nhieu lan cho cung phien khong tao ban ghi trung.
/// </summary>
public sealed class CongViecTaoBienBan
{
    private readonly IAppDbContext _db;
    private readonly DichVuBienBanHop _dichVuBienBan;
    private readonly ILogger<CongViecTaoBienBan> _logger;

    public CongViecTaoBienBan(
        IAppDbContext db, DichVuBienBanHop dichVuBienBan,
        ILogger<CongViecTaoBienBan> logger)
    {
        _db = db;
        _dichVuBienBan = dichVuBienBan;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
    public async Task ChayAsync(Guid sangKienId, Guid buocTruocId, CancellationToken ct = default)
    {
        var buoc = await _db.QuyTrinhBuoc.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == buocTruocId, ct)
            .ConfigureAwait(false);

        if (buoc?.HoiDongId is null)
        {
            _logger.LogWarning(
                "TAO_BIEN_BAN bỏ qua: bước {BuocId} không có hội đồng gắn kết cho sáng kiến {SangKienId}.",
                buocTruocId, sangKienId);
            return;
        }

        var hoiDongId = buoc.HoiDongId.Value;

        var phienHopId = await _db.PhienHopHoSo.AsNoTracking()
            .Where(x => x.SangKienId == sangKienId
                        && x.PhienHop!.HoiDongId == hoiDongId
                        && x.PhienHop!.TrangThaiPhien == TrangThaiPhienHop.DaKetThuc)
            .OrderByDescending(x => x.PhienHop!.ThoiGianBatDau)
            .Select(x => (Guid?)x.PhienHopId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (phienHopId is null)
        {
            _logger.LogWarning(
                "TAO_BIEN_BAN bỏ qua: không tìm thấy phiên họp đã kết thúc "
                + "của hội đồng {HoiDongId} có sáng kiến {SangKienId} trong danh sách hồ sơ.",
                hoiDongId, sangKienId);
            return;
        }

        var bienBanId = await _dichVuBienBan.LapTuDongAsync(phienHopId.Value, ct)
            .ConfigureAwait(false);

        if (bienBanId is null)
        {
            _logger.LogInformation(
                "TAO_BIEN_BAN bỏ qua: phiên họp {PhienHopId} chưa kết thúc hoặc biên bản đã ký.",
                phienHopId.Value);
            return;
        }

        _logger.LogInformation(
            "TAO_BIEN_BAN tự động: đã lập biên bản {BienBanId} cho phiên họp {PhienHopId} "
            + "(sáng kiến {SangKienId} kích hoạt).",
            bienBanId.Value, phienHopId.Value, sangKienId);
    }
}
