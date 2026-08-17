using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.TichHop;

/// <summary>Khoa API hien cho quan tri vien — KHONG bao gio kem khoa goc.</summary>
public sealed record KhoaApiNgoaiDto(
    Guid Id,
    string Ten,
    string TienTo,
    IReadOnlyList<string> DanhSachIp,
    bool DangHoatDong,
    DateTimeOffset? NgayHetHan,
    DateTimeOffset? LanGoiCuoi,
    long SoLanGoi,
    string? GhiChu);

/// <summary>Ket qua cap khoa moi — day la lan DUY NHAT khoa goc duoc hien.</summary>
public sealed record KhoaMoiDto(Guid Id, string Khoa);

/// <summary>Du lieu tao/sua khoa.</summary>
public sealed record LuuKhoaApiDto(
    string Ten, List<string>? DanhSachIp, DateTimeOffset? NgayHetHan, string? GhiChu);

/// <summary>
/// Chuc nang 41 — Quan ly khoa API cap cho he thong ngoai goi vao.
///
/// Khoa duoc luu duoi dang BAM, chi tra ve ban ro dung mot lan luc cap. Neu luu ban ro de "cho
/// xem lai", mot lan lo ban dump CSDL la lo toan bo quyen truy cap cua cac he thong doi tac.
/// </summary>
public sealed class DichVuKhoaApiNgoai
{
    private const string TienToChung = "bik_";
    private const int DoDaiTienTo = 12;

    private readonly IAppDbContext _db;
    private readonly IDongHoHeThong _dongHo;

    public DichVuKhoaApiNgoai(IAppDbContext db, IDongHoHeThong dongHo)
    {
        _db = db;
        _dongHo = dongHo;
    }

    public async Task<IReadOnlyList<KhoaApiNgoaiDto>> DanhSachAsync(CancellationToken ct = default)
        => await _db.KhoaApiNgoai.AsNoTracking()
            .OrderBy(x => x.Ten)
            .Select(x => new KhoaApiNgoaiDto(
                x.Id, x.Ten, x.TienTo, x.DanhSachIp, x.DangHoatDong,
                x.NgayHetHan, x.LanGoiCuoi, x.SoLanGoi, x.GhiChu))
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<KhoaMoiDto> CapAsync(LuuKhoaApiDto duLieu, CancellationToken ct = default)
    {
        KiemTraDanhSachIp(duLieu.DanhSachIp);

        // 32 byte ngau nhien ma Base64url — khong gian du rong de khong so do khoa.
        var phanBiMat = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var khoa = TienToChung + phanBiMat;

        var banGhi = new KhoaApiNgoai
        {
            Ten = duLieu.Ten.Trim(),
            TienTo = khoa[..DoDaiTienTo],
            KhoaBam = Bam(khoa),
            DanhSachIp = duLieu.DanhSachIp ?? new List<string>(),
            NgayHetHan = duLieu.NgayHetHan,
            GhiChu = duLieu.GhiChu
        };

        _db.KhoaApiNgoai.Add(banGhi);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new KhoaMoiDto(banGhi.Id, khoa);
    }

    public async Task CapNhatAsync(Guid id, LuuKhoaApiDto duLieu, CancellationToken ct = default)
    {
        KiemTraDanhSachIp(duLieu.DanhSachIp);

        var banGhi = await LayAsync(id, ct).ConfigureAwait(false);

        banGhi.Ten = duLieu.Ten.Trim();
        banGhi.DanhSachIp = duLieu.DanhSachIp ?? new List<string>();
        banGhi.NgayHetHan = duLieu.NgayHetHan;
        banGhi.GhiChu = duLieu.GhiChu;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DoiTrangThaiAsync(Guid id, bool bat, CancellationToken ct = default)
    {
        var banGhi = await LayAsync(id, ct).ConfigureAwait(false);

        banGhi.DangHoatDong = bat;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ThuHoiAsync(Guid id, CancellationToken ct = default)
    {
        var banGhi = await LayAsync(id, ct).ConfigureAwait(false);

        _db.KhoaApiNgoai.Remove(banGhi);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------- Dung o tang xac thuc

    /// <summary>
    /// Xac thuc mot lan goi vao. Tra ve ban ghi khoa neu hop le, <c>null</c> neu khong.
    ///
    /// Kiem tra theo thu tu: tim theo tien to → so bam → con hieu luc → IP nam trong danh sach.
    /// </summary>
    public async Task<KhoaApiNgoai?> XacThucAsync(
        string? khoa, string? diaChiIp, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(khoa) || khoa.Length <= DoDaiTienTo)
        {
            return null;
        }

        var tienTo = khoa[..DoDaiTienTo];

        var ungVien = await _db.KhoaApiNgoai
            .Where(x => x.TienTo == tienTo)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var bam = Bam(khoa);

        var banGhi = ungVien.FirstOrDefault(x =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(x.KhoaBam), Encoding.UTF8.GetBytes(bam)));

        if (banGhi is null || !banGhi.ConHieuLuc(_dongHo.BayGio))
        {
            return null;
        }

        if (!IpDuocPhep(banGhi.DanhSachIp, diaChiIp))
        {
            return null;
        }

        banGhi.LanGoiCuoi = _dongHo.BayGio;
        banGhi.SoLanGoi++;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return banGhi;
    }

    /// <summary>Danh sach rong = khong chan theo IP. Ho tro ca dia chi don va dai CIDR.</summary>
    public static bool IpDuocPhep(IReadOnlyCollection<string> danhSach, string? diaChiIp)
    {
        if (danhSach.Count == 0)
        {
            return true;
        }

        if (!IPAddress.TryParse(diaChiIp, out var ip))
        {
            return false;
        }

        // IPv4-mapped IPv6 (::ffff:10.0.0.1): khong go bo thi mot may chu lang nghe kieu kep
        // se khong bao gio khop voi dai IPv4 ma quan tri vien khai bao.
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        foreach (var muc in danhSach)
        {
            var sach = muc.Trim();

            if (sach.Contains('/', StringComparison.Ordinal))
            {
                if (TrongDaiCidr(ip, sach))
                {
                    return true;
                }
            }
            else if (IPAddress.TryParse(sach, out var don) && don.Equals(ip))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TrongDaiCidr(IPAddress ip, string cidr)
    {
        var phan = cidr.Split('/', 2);

        if (!IPAddress.TryParse(phan[0], out var goc)
            || !int.TryParse(phan[1], out var soBit))
        {
            return false;
        }

        if (goc.AddressFamily != ip.AddressFamily)
        {
            return false;
        }

        var duGoc = goc.GetAddressBytes();
        var duIp = ip.GetAddressBytes();

        var toiDa = duGoc.Length * 8;

        if (soBit < 0 || soBit > toiDa)
        {
            return false;
        }

        var soByteDay = soBit / 8;
        var soBitLe = soBit % 8;

        for (var i = 0; i < soByteDay; i++)
        {
            if (duGoc[i] != duIp[i])
            {
                return false;
            }
        }

        if (soBitLe == 0)
        {
            return true;
        }

        var matNa = (byte)(0xFF << (8 - soBitLe));

        return (duGoc[soByteDay] & matNa) == (duIp[soByteDay] & matNa);
    }

    // ------------------------------------------------------------------------------

    private async Task<KhoaApiNgoai> LayAsync(Guid id, CancellationToken ct)
        => await _db.KhoaApiNgoai.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
           ?? throw new NghiepVuException(MaLoiHeThong.KhongTimThay, "Không tìm thấy khoá API.");

    private static void KiemTraDanhSachIp(List<string>? danhSach)
    {
        foreach (var muc in danhSach ?? new List<string>())
        {
            var sach = muc.Trim();

            var hopLe = sach.Contains('/', StringComparison.Ordinal)
                ? HopLeCidr(sach)
                : IPAddress.TryParse(sach, out _);

            if (!hopLe)
            {
                // Bat o day chu khong de den luc goi that: mot dong sai chinh ta trong danh
                // sach se lam he thong doi tac bi tu choi ma khong ai biet vi sao.
                throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                    $"'{muc}' không phải địa chỉ IP hoặc dải CIDR hợp lệ.");
            }
        }
    }

    private static bool HopLeCidr(string cidr)
    {
        var phan = cidr.Split('/', 2);

        if (!IPAddress.TryParse(phan[0], out var goc) || !int.TryParse(phan[1], out var soBit))
        {
            return false;
        }

        var toiDa = goc.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;

        return soBit >= 0 && soBit <= toiDa;
    }

    private static string Bam(string khoa)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(khoa)));
}
