using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BlueIdea.Application.Chung;
using BlueIdea.Application.XacThuc;
using BlueIdea.Domain.QuanTri;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BlueIdea.Infrastructure.DichVu;

/// <summary>Cau hinh JWT doc tu appsettings.</summary>
public sealed class TuyChonJwt
{
    public const string Muc = "Jwt";

    public string Issuer { get; set; } = "blueidea";

    public string Audience { get; set; } = "blueidea-web";

    /// <summary>Khoa ky HS256 - BAT BUOC dat qua bien moi truong o moi truong that.</summary>
    public string KhoaKy { get; set; } = string.Empty;

    public int SoPhutHetHanAccessToken { get; set; } = 15;

    public int SoNgayHetHanRefreshToken { get; set; } = 7;
}

/// <summary>Cau hinh ma hoa du lieu nhay cam.</summary>
public sealed class TuyChonMaHoa
{
    public const string Muc = "MaHoa";

    /// <summary>Khoa AES-256 dang base64 (32 byte). Bat buoc dat qua bien moi truong.</summary>
    public string KhoaBase64 { get; set; } = string.Empty;
}

/// <summary>
/// Bam mat khau bang Argon2id (yeu cau Muc 5 - chuc nang 21).
/// Tham so chon theo khuyen nghi OWASP: 4 lan lap, 64 MB bo nho, 4 luong.
/// </summary>
public sealed class DichVuMatKhauArgon2 : IDichVuMatKhau
{
    private const int SoLanLap = 4;
    private const int BoNhoKb = 65536;
    private const int SoLuong = 4;
    private const int DoDaiHash = 32;
    private const int DoDaiSalt = 16;

    private static readonly char[] KyTuDacBiet = "!@#$%^&*()-_=+[]{};:'\",.<>/?\\|`~".ToCharArray();

    public (string Hash, string Salt) BamMatKhau(string matKhau)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matKhau);

        var salt = RandomNumberGenerator.GetBytes(DoDaiSalt);
        var hash = Bam(matKhau, salt);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool KiemTra(string matKhau, string hash, string salt)
    {
        if (string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
        {
            return false;
        }

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashMongDoi = Convert.FromBase64String(hash);
            var hashThucTe = Bam(matKhau, saltBytes);

            // So sanh thoi gian hang so de chong timing attack.
            return CryptographicOperations.FixedTimeEquals(hashThucTe, hashMongDoi);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public IReadOnlyList<string> KiemTraChinhSach(string matKhau, ChinhSachMatKhau chinhSach)
    {
        var loi = new List<string>();

        if (string.IsNullOrWhiteSpace(matKhau))
        {
            loi.Add("Mật khẩu không được để trống.");
            return loi;
        }

        if (matKhau.Length < chinhSach.DoDaiToiThieu)
        {
            loi.Add($"Mật khẩu phải có ít nhất {chinhSach.DoDaiToiThieu} ký tự.");
        }

        if (chinhSach.YeuCauChuHoa && !matKhau.Any(char.IsUpper))
        {
            loi.Add("Mật khẩu phải có ít nhất một chữ hoa.");
        }

        if (chinhSach.YeuCauChuThuong && !matKhau.Any(char.IsLower))
        {
            loi.Add("Mật khẩu phải có ít nhất một chữ thường.");
        }

        if (chinhSach.YeuCauSo && !matKhau.Any(char.IsDigit))
        {
            loi.Add("Mật khẩu phải có ít nhất một chữ số.");
        }

        if (chinhSach.YeuCauKyTuDacBiet && !matKhau.Any(c => KyTuDacBiet.Contains(c)))
        {
            loi.Add("Mật khẩu phải có ít nhất một ký tự đặc biệt.");
        }

        return loi;
    }

    private static byte[] Bam(string matKhau, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(matKhau))
        {
            Salt = salt,
            DegreeOfParallelism = SoLuong,
            Iterations = SoLanLap,
            MemorySize = BoNhoKb
        };

        return argon2.GetBytes(DoDaiHash);
    }
}

/// <summary>Sinh JWT access token va refresh token.</summary>
public sealed class DichVuTokenJwt : IDichVuToken
{
    private readonly TuyChonJwt _tuyChon;

    public DichVuTokenJwt(IOptions<TuyChonJwt> tuyChon)
    {
        _tuyChon = tuyChon.Value;

        if (string.IsNullOrWhiteSpace(_tuyChon.KhoaKy) || _tuyChon.KhoaKy.Length < 32)
        {
            throw new InvalidOperationException(
                "Cấu hình Jwt:KhoaKy phải có ít nhất 32 ký tự. "
                + "Hãy đặt qua biến môi trường Jwt__KhoaKy ở môi trường triển khai.");
        }
    }

    public int SoGiayHetHanAccessToken => _tuyChon.SoPhutHetHanAccessToken * 60;

    public int SoNgayHetHanRefreshToken => _tuyChon.SoNgayHetHanRefreshToken;

    public string TaoAccessToken(
        NguoiDung nguoiDung, IReadOnlyCollection<string> vaiTro, IReadOnlyCollection<string> quyen)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, nguoiDung.Id.ToString()),
            new(ClaimTypes.NameIdentifier, nguoiDung.Id.ToString()),
            new(ClaimTypes.Name, nguoiDung.TenDangNhap),
            new(NguoiDungHienTai.ClaimHoTen, nguoiDung.HoTen),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (nguoiDung.DonViId.HasValue)
        {
            claims.Add(new Claim(NguoiDungHienTai.ClaimDonVi, nguoiDung.DonViId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(nguoiDung.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, nguoiDung.Email));
        }

        claims.AddRange(vaiTro.Select(v => new Claim(NguoiDungHienTai.ClaimVaiTro, v)));
        claims.AddRange(vaiTro.Select(v => new Claim(ClaimTypes.Role, v)));
        claims.AddRange(quyen.Select(q => new Claim(NguoiDungHienTai.ClaimQuyen, q)));

        var khoa = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tuyChon.KhoaKy));
        var thongTinKy = new SigningCredentials(khoa, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _tuyChon.Issuer,
            audience: _tuyChon.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_tuyChon.SoPhutHetHanAccessToken),
            signingCredentials: thongTinKy);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Token, string Hash) TaoRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (token, BamRefreshToken(token));
    }

    /// <summary>Refresh token luu duoi dang hash SHA-256, khong luu ban ro trong CSDL.</summary>
    public string BamRefreshToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

/// <summary>
/// Ma hoa du lieu nhay cam (so CCCD, secret tich hop) bang AES-256-GCM.
/// Dinh dang luu: base64(nonce | tag | ciphertext).
/// </summary>
public sealed class DichVuMaHoaAes : IDichVuMaHoa
{
    private const int DoDaiNonce = 12;
    private const int DoDaiTag = 16;

    private readonly byte[] _khoa;

    public DichVuMaHoaAes(IOptions<TuyChonMaHoa> tuyChon)
    {
        var base64 = tuyChon.Value.KhoaBase64;

        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new InvalidOperationException(
                "Cấu hình MaHoa:KhoaBase64 chưa được thiết lập. "
                + "Sinh khóa bằng: openssl rand -base64 32");
        }

        _khoa = Convert.FromBase64String(base64);

        if (_khoa.Length != 32)
        {
            throw new InvalidOperationException("MaHoa:KhoaBase64 phải là khóa AES-256 (32 byte).");
        }
    }

    public string MaHoa(string? banRo)
    {
        if (string.IsNullOrEmpty(banRo))
        {
            return string.Empty;
        }

        var duLieu = Encoding.UTF8.GetBytes(banRo);
        var nonce = RandomNumberGenerator.GetBytes(DoDaiNonce);
        var banMa = new byte[duLieu.Length];
        var tag = new byte[DoDaiTag];

        using var aes = new AesGcm(_khoa, DoDaiTag);
        aes.Encrypt(nonce, duLieu, banMa, tag);

        var ketQua = new byte[DoDaiNonce + DoDaiTag + banMa.Length];
        Buffer.BlockCopy(nonce, 0, ketQua, 0, DoDaiNonce);
        Buffer.BlockCopy(tag, 0, ketQua, DoDaiNonce, DoDaiTag);
        Buffer.BlockCopy(banMa, 0, ketQua, DoDaiNonce + DoDaiTag, banMa.Length);

        return Convert.ToBase64String(ketQua);
    }

    public string? GiaiMa(string? banMa)
    {
        if (string.IsNullOrEmpty(banMa))
        {
            return null;
        }

        try
        {
            var duLieu = Convert.FromBase64String(banMa);
            if (duLieu.Length < DoDaiNonce + DoDaiTag)
            {
                return null;
            }

            var nonce = duLieu[..DoDaiNonce];
            var tag = duLieu[DoDaiNonce..(DoDaiNonce + DoDaiTag)];
            var noiDung = duLieu[(DoDaiNonce + DoDaiTag)..];
            var banRo = new byte[noiDung.Length];

            using var aes = new AesGcm(_khoa, DoDaiTag);
            aes.Decrypt(nonce, noiDung, tag, banRo);

            return Encoding.UTF8.GetString(banRo);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return null;
        }
    }
}

/// <summary>Sinh ma ho so theo mau cau hinh, vi du "SK-{NAM}-{STT:0000}".</summary>
public sealed class SinhMaHoSo : ISinhMaHoSo
{
    private const string MauMacDinh = "SK-{NAM}-{STT:0000}";

    private readonly IAppDbContext _db;
    private readonly IDichVuCauHinh _cauHinh;

    public SinhMaHoSo(IAppDbContext db, IDichVuCauHinh cauHinh)
    {
        _db = db;
        _cauHinh = cauHinh;
    }

    public async Task<string> SinhAsync(int nam, CancellationToken ct = default)
    {
        var mau = await _cauHinh.LayAsync(Domain.Chung.KhoaCauHinh.MauMaHoSo, ct).ConfigureAwait(false);
        mau = string.IsNullOrWhiteSpace(mau) ? MauMacDinh : mau;

        var tienTo = mau.Replace("{NAM}", nam.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        var viTriStt = tienTo.IndexOf("{STT", StringComparison.Ordinal);
        var phanDau = viTriStt >= 0 ? tienTo[..viTriStt] : tienTo;

        // Dem so ho so cua nam de sinh so thu tu ke tiep (bao gom ca ban ghi da xoa mem).
        var soHienCo = await EntityFrameworkQueryableExtensions.CountAsync(_db.SangKien.IgnoreQueryFilters().Where(x => x.MaHoSo.StartsWith(phanDau)), ct)
            .ConfigureAwait(false);

        var soChuSo = LaySoChuSo(mau);

        for (var thu = 1; thu <= 1000; thu++)
        {
            var stt = (soHienCo + thu).ToString(new string('0', soChuSo), CultureInfo.InvariantCulture);
            var ma = ThayThamSo(mau, nam, stt);

            var daTonTai = await EntityFrameworkQueryableExtensions.AnyAsync(_db.SangKien.IgnoreQueryFilters().Where(x => x.MaHoSo == ma), ct)
                .ConfigureAwait(false);

            if (!daTonTai)
            {
                return ma;
            }
        }

        // Truong hop cuc hiem: rot ve ma ngau nhien de khong chan nguoi dung nop ho so.
        return ThayThamSo(mau, nam, Guid.NewGuid().ToString("N")[..8].ToUpperInvariant());
    }

    private static string ThayThamSo(string mau, int nam, string stt)
    {
        var ketQua = mau.Replace("{NAM}", nam.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        var viTri = ketQua.IndexOf("{STT", StringComparison.Ordinal);

        if (viTri < 0)
        {
            return ketQua + stt;
        }

        var ketThuc = ketQua.IndexOf('}', viTri);
        return ketThuc < 0
            ? ketQua + stt
            : ketQua[..viTri] + stt + ketQua[(ketThuc + 1)..];
    }

    private static int LaySoChuSo(string mau)
    {
        var viTri = mau.IndexOf("{STT:", StringComparison.Ordinal);
        if (viTri < 0)
        {
            return 4;
        }

        var ketThuc = mau.IndexOf('}', viTri);
        if (ketThuc < 0)
        {
            return 4;
        }

        var dinhDang = mau[(viTri + 5)..ketThuc];
        return dinhDang.Count(c => c == '0') is var so && so > 0 ? so : 4;
    }
}
