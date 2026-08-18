using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.HoiDong;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.HoiDong;

/// <summary>Mot dong ket qua bo phieu cua ho so trong bien ban.</summary>
public sealed record DongBienBanHoSo(
    Guid SangKienId,
    string MaHoSo,
    string TenSangKien,
    string? TacGiaChinh,
    decimal? TongDiem,
    int SoPhieuDongY,
    int SoPhieuKhongDongY,
    int SoPhieuYKienKhac,
    decimal TyLeDongY,
    bool DatNguong,
    string? KetLuanRieng);

/// <summary>Mot chu ky tren bien ban.</summary>
public sealed record ChuKyBienBanDto(
    Guid ThanhVienId,
    string HoTen,
    string ChucDanh,
    bool DaKy,
    DateTimeOffset? ThoiGianKy);

/// <summary>Bien ban hop day du de hien thi va xuat PDF.</summary>
public sealed record BienBanHopDto(
    Guid Id,
    Guid PhienHopId,
    string SoBienBan,
    string TrangThaiBienBan,
    DateOnly? NgayLap,
    string TenHoiDong,
    string TenPhien,
    DateTimeOffset ThoiGianBatDau,
    string? DiaDiem,
    string HinhThuc,
    string? KetLuanChung,
    int SoThanhVien,
    int SoCoMat,
    IReadOnlyList<string> ThanhPhanDuHop,
    IReadOnlyList<string> ThanhVienVang,
    IReadOnlyList<DongBienBanHoSo> DanhSachHoSo,
    IReadOnlyList<ChuKyBienBanDto> ChuKy,
    Guid? TepTinId);

public static class TrangThaiBienBan
{
    public const string Nhap = "NHAP";
    public const string DaLap = "DA_LAP";
    public const string DaKy = "DA_KY";
    public const string DaBanHanh = "DA_BAN_HANH";
}

/// <summary>
/// Nhom IV — Bien ban phien hop hoi dong.
///
/// Bien ban duoc SINH TU DU LIEU CO THAT cua phien hop (diem danh, phieu bo phieu, ket luan)
/// chu khong phai mot form nhap tay: bien ban la can cu hanh chinh, go tay lai so lieu la mo
/// duong cho sai lech giua bien ban va du lieu he thong.
/// </summary>
public sealed class DichVuBienBanHop
{
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDung;

    public DichVuBienBanHop(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo,
        INguoiDungHienTai nguoiDung)
    {
        _db = db;
        _phanQuyen = phanQuyen;
        _dongHo = dongHo;
        _nguoiDung = nguoiDung;
    }

    /// <summary>
    /// Lap bien ban cho mot phien hop. Goi lai lan hai tren cung phien = LAM MOI so lieu cua
    /// bien ban dang co, khong tao ban thu hai — mot phien hop chi co mot bien ban.
    /// </summary>
    public async Task<BienBanHopDto> LapAsync(Guid phienHopId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongHopPhien, phienHopId, ct)
            .ConfigureAwait(false);

        var phien = await LayPhienAsync(phienHopId, ct).ConfigureAwait(false);

        if (phien.TrangThaiPhien != TrangThaiPhienHop.DaKetThuc)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Chỉ lập biên bản sau khi phiên họp đã kết thúc và có kết luận.");
        }

        var bienBan = await _db.BienBanHop
            .Include(x => x.ChuKy)
            .FirstOrDefaultAsync(x => x.PhienHopId == phienHopId, ct)
            .ConfigureAwait(false);

        if (bienBan is null)
        {
            bienBan = new BienBanHop
            {
                Id = Guid.NewGuid(),
                PhienHopId = phienHopId,
                SoBienBan = $"BB-{phien.MaPhien}",
                NgayLap = DateOnly.FromDateTime(_dongHo.BayGio.DateTime),
                TrangThaiBienBan = TrangThaiBienBan.DaLap
            };

            _db.BienBanHop.Add(bienBan);
        }
        else if (bienBan.TrangThaiBienBan is TrangThaiBienBan.DaKy or TrangThaiBienBan.DaBanHanh)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Biên bản đã có chữ ký nên không lập lại được. Muốn sửa phải huỷ chữ ký trước.");
        }

        // Dong chu ky: moi thanh vien co quyen ky bien ban duoc mot dong.
        var nguoiKy = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.HoiDongId == phien.HoiDongId
                        && x.QuyenKyBienBan
                        && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .Select(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var thanhVienId in nguoiKy.Where(id => bienBan.ChuKy.All(k => k.ThanhVienId != id)))
        {
            bienBan.ChuKy.Add(new BienBanChuKy
            {
                Id = Guid.NewGuid(),
                BienBanId = bienBan.Id,
                ThanhVienId = thanhVienId
            });
        }

        bienBan.NoiDungJson = await TaoNoiDungAsync(phien, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return await LayAsync(bienBan.Id, ct).ConfigureAwait(false);
    }

    public async Task<BienBanHopDto?> LayTheoPhienAsync(
        Guid phienHopId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongXem, phienHopId, ct)
            .ConfigureAwait(false);

        var id = await _db.BienBanHop.AsNoTracking()
            .Where(x => x.PhienHopId == phienHopId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return id is null ? null : await LayAsync(id.Value, ct).ConfigureAwait(false);
    }

    /// <summary>Thanh vien ky nhan bien ban (ky "xac nhan noi dung", khac voi ky so bang CA).</summary>
    public async Task KyAsync(Guid bienBanId, CancellationToken ct = default)
    {
        var bienBan = await _db.BienBanHop
            .Include(x => x.ChuKy)
            .FirstOrDefaultAsync(x => x.Id == bienBanId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("biên bản", bienBanId);

        var phien = await LayPhienAsync(bienBan.PhienHopId, ct).ConfigureAwait(false);

        // Chi thanh vien CUA CHINH hoi dong do va co quyen ky bien ban moi ky duoc.
        var thanhVien = await _db.HoiDongThanhVien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.HoiDongId == phien.HoiDongId
                                      && x.NguoiDungId == _nguoiDung.Id, ct)
            .ConfigureAwait(false)
            ?? throw new NghiepVuException(MaLoiHeThong.KhongCoQuyen,
                "Bạn không phải thành viên của hội đồng này.");

        if (!thanhVien.QuyenKyBienBan)
        {
            throw new NghiepVuException(MaLoiHeThong.KhongCoQuyen,
                "Bạn không có quyền ký biên bản của hội đồng này.");
        }

        var dong = bienBan.ChuKy.FirstOrDefault(x => x.ThanhVienId == thanhVien.Id);

        if (dong is null)
        {
            dong = new BienBanChuKy
            {
                Id = Guid.NewGuid(),
                BienBanId = bienBan.Id,
                ThanhVienId = thanhVien.Id
            };

            bienBan.ChuKy.Add(dong);
        }

        dong.DaKy = true;
        dong.ThoiGianKy = _dongHo.BayGio;

        // Du chu ky = bien ban chuyen trang thai da ky.
        if (bienBan.ChuKy.All(x => x.DaKy))
        {
            bienBan.TrangThaiBienBan = TrangThaiBienBan.DaKy;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Gan tep PDF da sinh (hoac da ky so) vao bien ban.</summary>
    public async Task GanTepAsync(Guid bienBanId, Guid tepTinId, CancellationToken ct = default)
    {
        var bienBan = await _db.BienBanHop.FirstOrDefaultAsync(x => x.Id == bienBanId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("biên bản", bienBanId);

        bienBan.TepTinId = tepTinId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<BienBanHopDto> LayAsync(Guid bienBanId, CancellationToken ct = default)
    {
        var bienBan = await _db.BienBanHop.AsNoTracking()
            .Include(x => x.ChuKy)
            .FirstOrDefaultAsync(x => x.Id == bienBanId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("biên bản", bienBanId);

        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongXem, bienBan.PhienHopId, ct)
            .ConfigureAwait(false);

        var phien = await LayPhienAsync(bienBan.PhienHopId, ct).ConfigureAwait(false);
        var noiDung = bienBan.NoiDungJson ?? await TaoNoiDungAsync(phien, ct).ConfigureAwait(false);

        var thanhVien = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.HoiDongId == phien.HoiDongId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var tenHoiDong = await _db.HoiDong.AsNoTracking()
            .Where(x => x.Id == phien.HoiDongId)
            .Select(x => x.Ten)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? string.Empty;

        var chuKy = bienBan.ChuKy
            .Select(k =>
            {
                var tv = thanhVien.FirstOrDefault(x => x.Id == k.ThanhVienId);
                return new ChuKyBienBanDto(
                    k.ThanhVienId,
                    tv?.HoTenHienThi ?? "(không rõ)",
                    tv?.ChucDanh ?? ChucDanhHoiDong.UyVien,
                    k.DaKy,
                    k.ThoiGianKy);
            })
            .OrderBy(x => x.ChucDanh == ChucDanhHoiDong.ChuTich ? 0 : 1)
            .ThenBy(x => x.HoTen)
            .ToList();

        return new BienBanHopDto(
            bienBan.Id,
            bienBan.PhienHopId,
            bienBan.SoBienBan,
            bienBan.TrangThaiBienBan,
            bienBan.NgayLap,
            tenHoiDong,
            phien.TenPhien,
            phien.ThoiGianBatDau,
            phien.DiaDiem,
            phien.HinhThuc,
            phien.KetLuan,
            DocSo(noiDung, "soThanhVien"),
            DocSo(noiDung, "soCoMat"),
            DocDanhSachChuoi(noiDung, "thanhPhanDuHop"),
            DocDanhSachChuoi(noiDung, "thanhVienVang"),
            DocDanhSachHoSo(noiDung),
            chuKy,
            bienBan.TepTinId);
    }

    // ------------------------------------------------------------------------------------

    private async Task<PhienHopHoiDong> LayPhienAsync(Guid phienHopId, CancellationToken ct)
        => await _db.PhienHop.AsNoTracking()
               .Include(x => x.DanhSachHoSo)
               .Include(x => x.DiemDanh)
               .Include(x => x.PhieuBoPhieu)
               .FirstOrDefaultAsync(x => x.Id == phienHopId, ct)
               .ConfigureAwait(false)
           ?? throw new KhongTimThayException("phiên họp", phienHopId);

    /// <summary>Chup lai so lieu phien hop thanh JSON de bien ban khong doi khi du lieu sau nay doi.</summary>
    private async Task<Dictionary<string, object>> TaoNoiDungAsync(
        PhienHopHoiDong phien, CancellationToken ct)
    {
        var thanhVien = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.HoiDongId == phien.HoiDongId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var tyLeThongQua = await _db.HoiDong.AsNoTracking()
            .Where(x => x.Id == phien.HoiDongId)
            .Select(x => x.TyLeThongQua)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var sangKienIds = phien.DanhSachHoSo.Select(x => x.SangKienId).ToList();

        var hoSo = await _db.SangKien.AsNoTracking()
            .Where(x => sangKienIds.Contains(x.Id))
            .Select(x => new { x.Id, x.MaHoSo, x.TenSangKien, x.TongDiem })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var tacGia = await _db.SangKienTacGia.AsNoTracking()
            .Where(x => sangKienIds.Contains(x.SangKienId) && x.LaTacGiaChinh)
            .Select(x => new { x.SangKienId, x.HoTen })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var coMat = phien.DiemDanh.Where(x => x.CoMat).Select(x => x.ThanhVienId).ToHashSet();

        var dongHoSo = phien.DanhSachHoSo
            .OrderBy(x => x.ThuTu)
            .Select(hs =>
            {
                var phieu = phien.PhieuBoPhieu.Where(p => p.SangKienId == hs.SangKienId).ToList();
                var dongY = phieu.Count(p => p.YKien == YKienBoPhieu.DongY);
                var tong = phieu.Count;
                var tyLe = tong == 0 ? 0m : Math.Round(dongY * 100m / tong, 2);
                var thongTin = hoSo.FirstOrDefault(x => x.Id == hs.SangKienId);

                return new Dictionary<string, object?>
                {
                    ["sangKienId"] = hs.SangKienId,
                    ["duongDan"] = DuongDanGiaoDien.ChiTietHoSo(hs.SangKienId),
                    ["maHoSo"] = thongTin?.MaHoSo ?? string.Empty,
                    ["tenSangKien"] = thongTin?.TenSangKien ?? string.Empty,
                    ["tacGiaChinh"] = tacGia.FirstOrDefault(x => x.SangKienId == hs.SangKienId)?.HoTen,
                    ["tongDiem"] = thongTin?.TongDiem,
                    ["soPhieuDongY"] = dongY,
                    ["soPhieuKhongDongY"] = phieu.Count(p => p.YKien == YKienBoPhieu.KhongDongY),
                    ["soPhieuYKienKhac"] = phieu.Count(p => p.YKien == YKienBoPhieu.YKienKhac),
                    ["tyLeDongY"] = tyLe,
                    ["datNguong"] = tyLe >= tyLeThongQua,
                    ["ketLuanRieng"] = hs.KetLuanRieng
                };
            })
            .ToList();

        return new Dictionary<string, object>
        {
            ["soThanhVien"] = thanhVien.Count,
            ["soCoMat"] = coMat.Count,
            ["tyLeThongQua"] = tyLeThongQua,
            ["thanhPhanDuHop"] = thanhVien
                .Where(x => coMat.Contains(x.Id))
                .OrderBy(x => x.ThuTu)
                .Select(x => $"{x.HoTenHienThi} — {TenChucDanh(x.ChucDanh)}")
                .ToList(),
            ["thanhVienVang"] = thanhVien
                .Where(x => !coMat.Contains(x.Id))
                .OrderBy(x => x.ThuTu)
                .Select(x => $"{x.HoTenHienThi} — {TenChucDanh(x.ChucDanh)}")
                .ToList(),
            ["danhSachHoSo"] = dongHoSo
        };
    }

    public static string TenChucDanh(string ma) => ma switch
    {
        ChucDanhHoiDong.ChuTich => "Chủ tịch hội đồng",
        ChucDanhHoiDong.PhoChuTich => "Phó chủ tịch hội đồng",
        ChucDanhHoiDong.UyVienThuKy => "Uỷ viên thư ký",
        ChucDanhHoiDong.ThuKy => "Thư ký",
        _ => "Uỷ viên"
    };

    private static int DocSo(IReadOnlyDictionary<string, object> noiDung, string khoa)
        => noiDung.TryGetValue(khoa, out var v) && v is not null
           && int.TryParse(DocChuoiTho(v), out var so)
            ? so
            : 0;

    private static IReadOnlyList<string> DocDanhSachChuoi(
        IReadOnlyDictionary<string, object> noiDung, string khoa)
    {
        if (!noiDung.TryGetValue(khoa, out var v) || v is null) return Array.Empty<string>();

        if (v is IEnumerable<string> chuoi) return chuoi.ToList();

        if (v is System.Text.Json.JsonElement je
            && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return je.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<DongBienBanHoSo> DocDanhSachHoSo(
        IReadOnlyDictionary<string, object> noiDung)
    {
        if (!noiDung.TryGetValue("danhSachHoSo", out var v) || v is null)
        {
            return Array.Empty<DongBienBanHoSo>();
        }

        // Vua lap xong thi con la doi tuong .NET; doc lai tu CSDL thi la JsonElement.
        if (v is IEnumerable<Dictionary<string, object?>> banDo)
        {
            return banDo.Select(TuBanDo).ToList();
        }

        if (v is System.Text.Json.JsonElement je
            && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return je.EnumerateArray().Select(TuJson).ToList();
        }

        return Array.Empty<DongBienBanHoSo>();
    }

    private static DongBienBanHoSo TuBanDo(Dictionary<string, object?> x) => new(
        x.TryGetValue("sangKienId", out var id) && id is Guid g ? g : Guid.Empty,
        DocChuoiTho(x.GetValueOrDefault("maHoSo")),
        DocChuoiTho(x.GetValueOrDefault("tenSangKien")),
        x.GetValueOrDefault("tacGiaChinh") as string,
        x.GetValueOrDefault("tongDiem") as decimal?,
        Convert.ToInt32(x.GetValueOrDefault("soPhieuDongY") ?? 0),
        Convert.ToInt32(x.GetValueOrDefault("soPhieuKhongDongY") ?? 0),
        Convert.ToInt32(x.GetValueOrDefault("soPhieuYKienKhac") ?? 0),
        Convert.ToDecimal(x.GetValueOrDefault("tyLeDongY") ?? 0m),
        x.GetValueOrDefault("datNguong") is true,
        x.GetValueOrDefault("ketLuanRieng") as string);

    private static DongBienBanHoSo TuJson(System.Text.Json.JsonElement x) => new(
        x.TryGetProperty("sangKienId", out var id) && Guid.TryParse(id.GetString(), out var g)
            ? g
            : Guid.Empty,
        LayChuoi(x, "maHoSo") ?? string.Empty,
        LayChuoi(x, "tenSangKien") ?? string.Empty,
        LayChuoi(x, "tacGiaChinh"),
        x.TryGetProperty("tongDiem", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.Number
            ? d.GetDecimal()
            : null,
        LaySo(x, "soPhieuDongY"),
        LaySo(x, "soPhieuKhongDongY"),
        LaySo(x, "soPhieuYKienKhac"),
        x.TryGetProperty("tyLeDongY", out var t)
        && t.ValueKind == System.Text.Json.JsonValueKind.Number
            ? t.GetDecimal()
            : 0m,
        x.TryGetProperty("datNguong", out var dn)
        && dn.ValueKind == System.Text.Json.JsonValueKind.True,
        LayChuoi(x, "ketLuanRieng"));

    private static string? LayChuoi(System.Text.Json.JsonElement x, string ten)
        => x.TryGetProperty(ten, out var v)
           && v.ValueKind == System.Text.Json.JsonValueKind.String
            ? v.GetString()
            : null;

    private static int LaySo(System.Text.Json.JsonElement x, string ten)
        => x.TryGetProperty(ten, out var v)
           && v.ValueKind == System.Text.Json.JsonValueKind.Number
            ? v.GetInt32()
            : 0;

    private static string DocChuoiTho(object? v)
        => v is System.Text.Json.JsonElement je
            ? je.ValueKind == System.Text.Json.JsonValueKind.String
                ? je.GetString() ?? string.Empty
                : je.ToString()
            : v?.ToString() ?? string.Empty;
}
