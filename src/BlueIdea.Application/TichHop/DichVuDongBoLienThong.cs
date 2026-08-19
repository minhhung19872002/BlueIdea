using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Application.TichHop;

/// <summary>Mot ban ghi sang kien duoc cong nhan, dinh dang de day sang he thong ngoai.</summary>
public sealed record BanGhiDongBo(
    string MaHoSo,
    string TenSangKien,
    string? TacGiaChinh,
    string? DonVi,
    string? LinhVuc,
    decimal? TongDiem,
    string? MucCongNhan,
    string? SoQuyetDinh,
    string? NgayCongNhan,
    int? Nam);

public sealed record KetQuaDongBo(
    Guid NhatKyId,
    string TenHeThong,
    int TongBanGhi,
    int ThanhCong,
    int ThatBai,
    string TrangThai,
    string? ThongBaoLoi);

/// <summary>Ket qua thu ket noi toi he thong ngoai.</summary>
public sealed record KetQuaThuKetNoi(
    bool ThanhCong, string TenHeThong, string? Endpoint, int SoMiliGiay, string? ThongBao);

public static class TrangThaiDongBo
{
    public const string DangChay = "DANG_CHAY";
    public const string ThanhCong = "THANH_CONG";
    public const string MotPhan = "MOT_PHAN";
    public const string ThatBai = "THAT_BAI";
}

/// <summary>
/// Hop dong goi API he thong ngoai. Tach ra de kiem thu duoc phan dieu phoi ma khong can
/// he thong that.
/// </summary>
public interface IBoGuiLienThong
{
    /// <summary>Tra ve (thanh cong, so ban ghi he thong ngoai xac nhan, thong bao).</summary>
    Task<(bool ThanhCong, int SoNhan, string PhanHoi)> GuiAsync(
        HeThongTichHop heThong, IReadOnlyList<BanGhiDongBo> duLieu, CancellationToken ct = default);
}

/// <summary>
/// Chuc nang 16, 41 - Day danh sach sang kien da cong nhan sang he thong ngoai
/// (Thi dua khen thuong, IOC cua thanh pho).
///
/// Chi day sang kien DA CONG BO ket qua: du lieu chua cong bo ma da xuat hien o he thong khac
/// la ro ri ket qua truoc thoi diem cong bo chinh thuc.
/// </summary>
public sealed class DichVuDongBoLienThong
{
    private readonly IAppDbContext _db;
    private readonly IBoGuiLienThong _boGui;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuNhatKy _nhatKy;
    private readonly ILogger<DichVuDongBoLienThong> _logger;

    public DichVuDongBoLienThong(
        IAppDbContext db, IBoGuiLienThong boGui, IDichVuPhanQuyen phanQuyen,
        IDongHoHeThong dongHo, IDichVuNhatKy nhatKy, ILogger<DichVuDongBoLienThong> logger)
    {
        _db = db;
        _boGui = boGui;
        _phanQuyen = phanQuyen;
        _dongHo = dongHo;
        _nhatKy = nhatKy;
        _logger = logger;
    }

    /// <summary>Xem trước dữ liệu sẽ đẩy đi, không gửi gì.</summary>
    public async Task<IReadOnlyList<BanGhiDongBo>> XemTruocAsync(
        Guid? dotDeNghiId, int? nam, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TichHopDongBo, ct: ct).ConfigureAwait(false);

        return await NapDuLieuAsync(dotDeNghiId, nam, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Dong bo MOT sang kien cu the sang he thong ngoai — chi goi tu workflow engine khi buoc co
    /// hanh dong DongBoLienThong. Khong kiem tra quyen — caller phai dam bao context da xac thuc.
    /// </summary>
    internal async Task<KetQuaDongBo> DongBoSangKienAsync(
        Guid heThongId, Guid sangKienId, CancellationToken ct = default)
    {
        var heThong = await _db.HeThongTichHop
            .FirstOrDefaultAsync(x => x.Id == heThongId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hệ thống tích hợp", heThongId);

        if (heThong.TrangThai != TrangThaiDanhMuc.HoatDong)
        {
            _logger.LogWarning(
                "Bỏ qua đồng bộ sáng kiến {SangKienId} — hệ thống '{TenHeThong}' ngừng hoạt động.",
                sangKienId, heThong.Ten);
            return new KetQuaDongBo(Guid.Empty, heThong.Ten, 0, 0, 0, TrangThaiDongBo.ThatBai,
                $"Hệ thống '{heThong.Ten}' đang ngừng hoạt động.");
        }

        if (string.IsNullOrWhiteSpace(heThong.EndpointBase))
        {
            _logger.LogWarning(
                "Bỏ qua đồng bộ sáng kiến {SangKienId} — hệ thống '{TenHeThong}' chưa cấu hình endpoint.",
                sangKienId, heThong.Ten);
            return new KetQuaDongBo(Guid.Empty, heThong.Ten, 0, 0, 0, TrangThaiDongBo.ThatBai,
                $"Hệ thống '{heThong.Ten}' chưa cấu hình endpoint.");
        }

        var duLieu = await NapDuLieuAsync(null, null, ct, sangKienId).ConfigureAwait(false);

        if (duLieu.Count == 0)
        {
            _logger.LogInformation(
                "Sáng kiến {SangKienId} chưa đủ điều kiện đồng bộ (chưa công bố kết quả).", sangKienId);
            return new KetQuaDongBo(Guid.Empty, heThong.Ten, 0, 0, 0, TrangThaiDongBo.ThanhCong,
                "Sáng kiến chưa đủ điều kiện đồng bộ (chưa công bố kết quả).");
        }

        var banGhi = new NhatKyDongBo
        {
            Id = Guid.NewGuid(),
            HeThongTichHopId = heThongId,
            Chieu = "GUI",
            LoaiDuLieu = "SANG_KIEN_DUOC_CONG_NHAN",
            TongBanGhi = duLieu.Count,
            TrangThaiDongBo = TrangThaiDongBo.DangChay,
            ThoiGianBatDau = _dongHo.BayGio
        };

        _db.NhatKyDongBo.Add(banGhi);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        try
        {
            var (thanhCong, soNhan, phanHoi) = await _boGui
                .GuiAsync(heThong, duLieu, ct)
                .ConfigureAwait(false);

            var trangThai = thanhCong
                ? soNhan >= duLieu.Count ? TrangThaiDongBo.ThanhCong : TrangThaiDongBo.MotPhan
                : TrangThaiDongBo.ThatBai;

            banGhi.PhanHoi = CatBot(phanHoi);

            return await KetThucAsync(banGhi, heThong, soNhan, duLieu.Count - soNhan, trangThai,
                thanhCong ? null : phanHoi, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đồng bộ sáng kiến {SangKienId} sang {TenHeThong} thất bại.",
                sangKienId, heThong.Ten);

            return await KetThucAsync(banGhi, heThong, 0, duLieu.Count, TrangThaiDongBo.ThatBai,
                ex.Message, ct).ConfigureAwait(false);
        }
    }

    public async Task<KetQuaDongBo> ChayAsync(
        Guid heThongId, Guid? dotDeNghiId, int? nam, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TichHopDongBo, ct)
            .ConfigureAwait(false);

        var heThong = await _db.HeThongTichHop
            .FirstOrDefaultAsync(x => x.Id == heThongId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hệ thống tích hợp", heThongId);

        if (heThong.TrangThai != TrangThaiDanhMuc.HoatDong)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                $"Hệ thống '{heThong.Ten}' đang ngừng hoạt động.");
        }

        if (string.IsNullOrWhiteSpace(heThong.EndpointBase))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Hệ thống '{heThong.Ten}' chưa cấu hình endpoint.");
        }

        var duLieu = await NapDuLieuAsync(dotDeNghiId, nam, ct).ConfigureAwait(false);

        // Ghi nhật ký TRƯỚC khi gọi: nếu tiến trình chết giữa chừng vẫn còn dấu vết là đã bắt đầu,
        // không để lần đồng bộ biến mất khỏi lịch sử.
        var banGhi = new NhatKyDongBo
        {
            Id = Guid.NewGuid(),
            HeThongTichHopId = heThongId,
            Chieu = "GUI",
            LoaiDuLieu = "SANG_KIEN_DUOC_CONG_NHAN",
            TongBanGhi = duLieu.Count,
            TrangThaiDongBo = TrangThaiDongBo.DangChay,
            ThoiGianBatDau = _dongHo.BayGio
        };

        _db.NhatKyDongBo.Add(banGhi);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (duLieu.Count == 0)
        {
            return await KetThucAsync(banGhi, heThong, 0, 0, TrangThaiDongBo.ThanhCong,
                "Không có bản ghi nào cần đồng bộ.", ct).ConfigureAwait(false);
        }

        try
        {
            var (thanhCong, soNhan, phanHoi) = await _boGui
                .GuiAsync(heThong, duLieu, ct)
                .ConfigureAwait(false);

            var trangThai = thanhCong
                ? soNhan >= duLieu.Count ? TrangThaiDongBo.ThanhCong : TrangThaiDongBo.MotPhan
                : TrangThaiDongBo.ThatBai;

            banGhi.PhanHoi = CatBot(phanHoi);

            return await KetThucAsync(banGhi, heThong, soNhan, duLieu.Count - soNhan, trangThai,
                thanhCong ? null : phanHoi, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đồng bộ sang {TenHeThong} thất bại.", heThong.Ten);

            return await KetThucAsync(banGhi, heThong, 0, duLieu.Count, TrangThaiDongBo.ThatBai,
                ex.Message, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Thu ket noi: goi he thong ngoai voi goi RONG.
    ///
    /// Khong gui du lieu nghiep vu vi day chi la phep thu cau hinh — gui du lieu that se tao ban
    /// ghi rac ben he thong nhan moi lan quan tri vien bam thu.
    /// </summary>
    public async Task<KetQuaThuKetNoi> ThuKetNoiAsync(Guid heThongId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TichHopCauHinh, ct)
            .ConfigureAwait(false);

        var heThong = await _db.HeThongTichHop.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == heThongId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hệ thống tích hợp", heThongId);

        if (string.IsNullOrWhiteSpace(heThong.EndpointBase))
        {
            return new KetQuaThuKetNoi(false, heThong.Ten, null, 0,
                "Chưa cấu hình endpoint cho hệ thống này.");
        }

        var batDau = _dongHo.BayGio;

        try
        {
            var (thanhCong, _, phanHoi) = await _boGui
                .GuiAsync(heThong, Array.Empty<BanGhiDongBo>(), ct)
                .ConfigureAwait(false);

            var soMs = (int)(_dongHo.BayGio - batDau).TotalMilliseconds;

            return new KetQuaThuKetNoi(thanhCong, heThong.Ten, heThong.EndpointBase, soMs,
                thanhCong ? "Kết nối thành công." : CatBot(phanHoi));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thử kết nối tới {TenHeThong} thất bại.", heThong.Ten);

            return new KetQuaThuKetNoi(false, heThong.Ten, heThong.EndpointBase,
                (int)(_dongHo.BayGio - batDau).TotalMilliseconds, ex.Message);
        }
    }

    /// <summary>
    /// Gui lai mot lan dong bo da that bai.
    ///
    /// Chay lai voi DUNG pham vi cua lan truoc (doc tu nhat ky) chu khong dong bo lai toan bo:
    /// nguoi dung bam "gui lai" mong lam lai dung viec do, khong phai day them du lieu khac.
    /// </summary>
    public async Task<KetQuaDongBo> GuiLaiAsync(Guid nhatKyId, CancellationToken ct = default)
    {
        var nhatKy = await _db.NhatKyDongBo.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == nhatKyId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("nhật ký đồng bộ", nhatKyId);

        if (nhatKy.TrangThaiDongBo == TrangThaiDongBo.ThanhCong)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Lần đồng bộ này đã thành công, không cần gửi lại.");
        }

        return await ChayAsync(nhatKy.HeThongTichHopId, null, null, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<NhatKyDongBo>> LichSuAsync(
        Guid? heThongId, int soDong = 50, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TichHopCauHinh, ct: ct).ConfigureAwait(false);

        var truyVan = _db.NhatKyDongBo.AsNoTracking();

        if (heThongId.HasValue)
        {
            truyVan = truyVan.Where(x => x.HeThongTichHopId == heThongId.Value);
        }

        return await truyVan
            .OrderByDescending(x => x.ThoiGianBatDau)
            .Take(Math.Clamp(soDong, 1, 200))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------------------

    private async Task<KetQuaDongBo> KetThucAsync(
        NhatKyDongBo banGhi, HeThongTichHop heThong, int thanhCong, int thatBai,
        string trangThai, string? loi, CancellationToken ct)
    {
        banGhi.ThanhCong = thanhCong;
        banGhi.ThatBai = thatBai;
        banGhi.TrangThaiDongBo = trangThai;
        banGhi.ThongBaoLoi = CatBot(loi);
        banGhi.ThoiGianKetThuc = _dongHo.BayGio;

        // Chỉ ghi mốc đồng bộ khi thực sự gửi được — nếu không, lần sau sẽ tưởng đã đồng bộ rồi.
        if (trangThai is TrangThaiDongBo.ThanhCong or TrangThaiDongBo.MotPhan)
        {
            heThong.LanDongBoCuoi = _dongHo.BayGio;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("DONG_BO_LIEN_THONG", "TICH_HOP", "HeThongTichHop", heThong.Id,
            $"Đồng bộ sang {heThong.Ten}: {thanhCong} thành công, {thatBai} thất bại",
            ketQua: trangThai == TrangThaiDongBo.ThatBai ? "THAT_BAI" : "THANH_CONG",
            ct: ct).ConfigureAwait(false);

        return new KetQuaDongBo(
            banGhi.Id, heThong.Ten, banGhi.TongBanGhi, thanhCong, thatBai, trangThai, banGhi.ThongBaoLoi);
    }

    private async Task<List<BanGhiDongBo>> NapDuLieuAsync(
        Guid? dotDeNghiId, int? nam, CancellationToken ct, Guid? sangKienId = null)
    {
        var truyVan = _db.SangKien.AsNoTracking()
            .Where(x => x.KetQua == KetQuaXetDuyetGiaTri.Dat && x.DaCongBoKetQua);

        if (sangKienId.HasValue)
        {
            truyVan = truyVan.Where(x => x.Id == sangKienId.Value);
        }

        if (dotDeNghiId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DotDeNghiId == dotDeNghiId.Value);
        }

        if (nam.HasValue)
        {
            truyVan = truyVan.Where(x => x.NgayCongNhan != null && x.NgayCongNhan.Value.Year == nam.Value);
        }

        return await truyVan
            .OrderBy(x => x.MaHoSo)
            .Select(x => new BanGhiDongBo(
                x.MaHoSo,
                x.TenSangKien,
                _db.SangKienTacGia
                    .Where(t => t.SangKienId == x.Id && t.LaTacGiaChinh)
                    .Select(t => t.HoTen).FirstOrDefault(),
                _db.DonVi.Where(d => d.Id == x.DonViId).Select(d => d.Ten).FirstOrDefault(),
                _db.LinhVuc.Where(l => l.Id == x.LinhVucId).Select(l => l.Ten).FirstOrDefault(),
                x.TongDiem,
                _db.MucCongNhan.Where(m => m.Id == x.MucCongNhanId).Select(m => m.Ten).FirstOrDefault(),
                _db.QuyetDinh.Where(q => q.Id == x.QuyetDinhId).Select(q => q.SoQuyetDinh).FirstOrDefault(),
                x.NgayCongNhan == null ? null : x.NgayCongNhan.Value.ToString("yyyy-MM-dd"),
                x.NgayCongNhan == null ? (int?)null : x.NgayCongNhan.Value.Year))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>Cột nhật ký có giới hạn độ dài; phản hồi lỗi của hệ thống ngoài có thể rất dài.</summary>
    private static string? CatBot(string? chuoi)
        => chuoi is null ? null : chuoi.Length <= 2000 ? chuoi : chuoi[..2000];
}
