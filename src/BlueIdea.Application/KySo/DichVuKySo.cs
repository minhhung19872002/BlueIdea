using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Application.KySo;

/// <summary>Ket qua ky mot tep.</summary>
public sealed record KetQuaKySo(
    bool ThanhCong,
    byte[]? TepDaKy,
    string? SerialChungThu,
    string? NguoiCapChungThu,
    DateTimeOffset? HieuLucTu,
    DateTimeOffset? HieuLucDen,
    string? ThongBaoLoi,
    /// <summary>PADES (chữ ký nằm trong PDF) | CMS_DETACHED (tệp .p7s riêng).</summary>
    string ChuanKy = ChuanChuKySo.CmsDetached);

/// <summary>Chuẩn chữ ký áp dụng cho một lần ký.</summary>
public static class ChuanChuKySo
{
    /// <summary>Chữ ký nhúng trong PDF — trình đọc PDF thông thường kiểm tra được.</summary>
    public const string Pades = "PADES";

    /// <summary>Chữ ký tách rời, sinh ra tệp .p7s đi kèm bản gốc.</summary>
    public const string CmsDetached = "CMS_DETACHED";
}

/// <summary>Ket qua xac minh chu ky tren mot tep.</summary>
public sealed record KetQuaXacMinhChuKy(
    bool CoChuKy,
    bool HopLe,
    string? SerialChungThu,
    string? NguoiKy,
    DateTimeOffset? ThoiGianKy,
    string? ThongBaoLoi);

public static class TrangThaiKySo
{
    public const string ThanhCong = "THANH_CONG";
    public const string ThatBai = "THAT_BAI";
}

/// <summary>
/// Hop dong ky so. Tach ra de doi nha cung cap CA ma khong sua nghiep vu, va de kiem thu
/// duoc toan bo luong bang chung thu tu ky.
/// </summary>
public interface IBoKySo
{
    bool DaCauHinh { get; }

    Task<KetQuaKySo> KyAsync(byte[] noiDung, CauHinhChuKySo cauHinh, CancellationToken ct = default);

    /// <summary>
    /// Xac minh chu ky. Chu ky la DETACHED nen bat buoc phai co CA ban goc lan tep chu ky —
    /// chi mot minh tep chu ky thi khong the doi chieu duoc noi dung.
    /// </summary>
    Task<KetQuaXacMinhChuKy> XacMinhAsync(
        byte[] noiDungGoc, byte[] chuKy, CancellationToken ct = default);
}

/// <summary>
/// Chuc nang 49 - Ky so van ban (quyet dinh, bien ban) va xac minh chu ky.
///
/// Ky so KHONG duoc ghi de tep goc: luu tep da ky thanh ban ghi rieng va giu nguyen ban goc,
/// vi tranh chap ve sau can doi chieu duoc ca hai.
/// </summary>
public sealed class DichVuKySo
{
    private readonly IAppDbContext _db;
    private readonly IBoKySo _boKy;
    private readonly ILuuTruTep _luuTru;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuNhatKy _nhatKy;
    private readonly ILogger<DichVuKySo> _logger;

    public DichVuKySo(
        IAppDbContext db, IBoKySo boKy, ILuuTruTep luuTru, IDichVuPhanQuyen phanQuyen,
        INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo, IDichVuNhatKy nhatKy,
        ILogger<DichVuKySo> logger)
    {
        _db = db;
        _boKy = boKy;
        _luuTru = luuTru;
        _phanQuyen = phanQuyen;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
        _nhatKy = nhatKy;
        _logger = logger;
    }

    public bool DaCauHinh => _boKy.DaCauHinh;

    /// <summary>
    /// Ky so mot tep dinh kem va gan ket qua vao doi tuong nghiep vu (quyet dinh, bien ban).
    /// </summary>
    public async Task<Guid> KyTepAsync(
        Guid tepTinId, string doiTuong, Guid doiTuongId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhKySo, ct)
            .ConfigureAwait(false);

        var cauHinh = await _db.CauHinhChuKySo.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .OrderByDescending(x => x.LaMacDinh)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Chưa cấu hình chữ ký số. Vào Quản trị → Cấu hình → Chữ ký số để thiết lập.");

        var tepGoc = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tepTinId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp tin", tepTinId);

        byte[] noiDung;

        await using (var luong = await _luuTru
                         .TaiXuongAsync(tepGoc.Bucket, tepGoc.DuongDan, ct)
                         .ConfigureAwait(false))
        {
            using var bo = new MemoryStream();
            await luong.CopyToAsync(bo, ct).ConfigureAwait(false);
            noiDung = bo.ToArray();
        }

        var ketQua = await _boKy.KyAsync(noiDung, cauHinh, ct).ConfigureAwait(false);

        var banGhi = new NhatKyKySo
        {
            Id = Guid.NewGuid(),
            DoiTuong = doiTuong,
            DoiTuongId = doiTuongId,
            NguoiKyId = _nguoiDung.Id,
            ThoiGianKy = _dongHo.BayGio,
            TepGocId = tepTinId,
            SerialChungThu = ketQua.SerialChungThu,
            NguoiCapChungThu = ketQua.NguoiCapChungThu,
            HieuLucTu = ketQua.HieuLucTu,
            HieuLucDen = ketQua.HieuLucDen,
            TrangThaiKy = ketQua.ThanhCong ? TrangThaiKySo.ThanhCong : TrangThaiKySo.ThatBai
        };

        if (!ketQua.ThanhCong || ketQua.TepDaKy is null)
        {
            banGhi.ThongTinXacThuc = new Dictionary<string, object>
            {
                ["loi"] = ketQua.ThongBaoLoi ?? "Không rõ nguyên nhân"
            };

            _db.NhatKyKySo.Add(banGhi);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            throw new NghiepVuException(MaLoiHeThong.LoiHeThong,
                $"Ký số thất bại: {ketQua.ThongBaoLoi}");
        }

        // PAdES tra ve MOT TEP PDF da nhung chu ky; CMS detached tra ve tep .p7s di kem ban goc.
        // Luu dung phan mo rong va MIME cua tung loai, neu khong trinh duyet tai ve mot tep PDF
        // mang duoi .p7s va nguoi dung khong mo duoc.
        var laPades = ketQua.ChuanKy == ChuanChuKySo.Pades;
        var phanMoRong = laPades ? ".pdf" : ".p7s";
        var mime = laPades ? "application/pdf" : "application/pkcs7-signature";

        // Lưu tệp đã ký thành bản ghi RIÊNG, giữ nguyên bản gốc.
        var tenLuuTru = $"{Guid.NewGuid():N}{phanMoRong}";

        var duongDan = await _luuTru
            .TaiLenAsync(new MemoryStream(ketQua.TepDaKy), tenLuuTru, mime, tepGoc.Bucket, ct)
            .ConfigureAwait(false);

        var tepDaKy = new Domain.SangKien.TepTin
        {
            Id = Guid.NewGuid(),
            TenGoc = $"{Path.GetFileNameWithoutExtension(tepGoc.TenGoc)}-da-ky{phanMoRong}",
            TenLuuTru = tenLuuTru,
            DuongDan = duongDan,
            Bucket = tepGoc.Bucket,
            KichThuoc = ketQua.TepDaKy.Length,
            MimeType = mime,
            PhanMoRong = phanMoRong,
            HashSha256 = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(ketQua.TepDaKy)).ToLowerInvariant(),
            NguoiTaiLenId = _nguoiDung.Id,
            NgayTaiLen = _dongHo.BayGio,
            TrangThaiOcr = TrangThaiOcrTep.KhongCan
        };

        _db.TepTin.Add(tepDaKy);
        banGhi.TepDaKyId = tepDaKy.Id;

        banGhi.ThongTinXacThuc = new Dictionary<string, object>
        {
            ["thuatToan"] = cauHinh.ThuatToan,
            ["nhaCungCap"] = cauHinh.NhaCungCap,
            ["loaiKy"] = cauHinh.LoaiKy,
            ["chuanKy"] = ketQua.ChuanKy
        };

        _db.NhatKyKySo.Add(banGhi);

        // Đánh dấu quyết định đã ký số để chặn sửa/xoá về sau.
        if (doiTuong == "QUYET_DINH")
        {
            var quyetDinh = await _db.QuyetDinh
                .FirstOrDefaultAsync(x => x.Id == doiTuongId, ct)
                .ConfigureAwait(false);

            if (quyetDinh is not null)
            {
                quyetDinh.DaKySo = true;
                quyetDinh.TepTinId ??= tepDaKy.Id;
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("KY_SO", "KY_SO", doiTuong, doiTuongId,
            $"Ký số tệp {tepGoc.TenGoc} (chứng thư {ketQua.SerialChungThu})", ct: ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Đã ký số {DoiTuong} {DoiTuongId} bằng chứng thư {Serial}.",
            doiTuong, doiTuongId, ketQua.SerialChungThu);

        return tepDaKy.Id;
    }

    /// <summary>
    /// Xác minh chữ ký của một lần ký.
    ///
    /// Nhận Id của bản ghi nhật ký ký số chứ không phải Id tệp: chữ ký detached cần CẢ bản gốc
    /// lẫn tệp chữ ký, và bản ghi nhật ký là nơi duy nhất ghép được đúng cặp đó.
    /// </summary>
    public async Task<KetQuaXacMinhChuKy> XacMinhAsync(
        Guid nhatKyKySoId, CancellationToken ct = default)
    {
        var banGhi = await _db.NhatKyKySo.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == nhatKyKySoId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("lần ký số", nhatKyKySoId);

        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhXem, ct)
            .ConfigureAwait(false);

        if (banGhi.TepGocId is null || banGhi.TepDaKyId is null)
        {
            return new KetQuaXacMinhChuKy(
                CoChuKy: false, HopLe: false, null, null, null,
                "Lần ký này không có đủ tệp gốc và tệp chữ ký để đối chiếu.");
        }

        var noiDungGoc = await DocTepAsync(banGhi.TepGocId.Value, ct).ConfigureAwait(false);
        var chuKy = await DocTepAsync(banGhi.TepDaKyId.Value, ct).ConfigureAwait(false);

        return await _boKy.XacMinhAsync(noiDungGoc, chuKy, ct).ConfigureAwait(false);
    }

    private async Task<byte[]> DocTepAsync(Guid tepTinId, CancellationToken ct)
    {
        var tep = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tepTinId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp tin", tepTinId);

        await using var luong = await _luuTru
            .TaiXuongAsync(tep.Bucket, tep.DuongDan, ct)
            .ConfigureAwait(false);

        using var bo = new MemoryStream();
        await luong.CopyToAsync(bo, ct).ConfigureAwait(false);

        return bo.ToArray();
    }

    public async Task<IReadOnlyList<NhatKyKySo>> LichSuAsync(
        string doiTuong, Guid doiTuongId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhXem, ct).ConfigureAwait(false);

        return await _db.NhatKyKySo.AsNoTracking()
            .Where(x => x.DoiTuong == doiTuong && x.DoiTuongId == doiTuongId)
            .OrderByDescending(x => x.ThoiGianKy)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
