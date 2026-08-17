using BlueIdea.Domain.QuanTri;

namespace BlueIdea.Application.XacThuc;

/// <summary>Sinh va kiem tra JWT access token / refresh token.</summary>
public interface IDichVuToken
{
    /// <summary>Sinh access token JWT (mac dinh 15 phut).</summary>
    string TaoAccessToken(NguoiDung nguoiDung, IReadOnlyCollection<string> vaiTro,
        IReadOnlyCollection<string> quyen);

    /// <summary>Sinh refresh token ngau nhien va hash de luu DB.</summary>
    (string Token, string Hash) TaoRefreshToken();

    string BamRefreshToken(string token);

    int SoGiayHetHanAccessToken { get; }

    int SoNgayHetHanRefreshToken { get; }
}

/// <summary>Ket qua tra ve sau khi dang nhap thanh cong.</summary>
public sealed record KetQuaDangNhap(
    string AccessToken,
    string RefreshToken,
    int HetHanSau,
    ThongTinNguoiDungDto NguoiDung,
    bool BuocDoiMatKhau);

public sealed record ThongTinNguoiDungDto(
    Guid Id,
    string TenDangNhap,
    string HoTen,
    string? Email,
    string? ChucVu,
    Guid? DonViId,
    string? TenDonVi,
    IReadOnlyList<string> VaiTro,
    IReadOnlyList<string> Quyen,
    bool MfaEnabled);

/// <summary>Mot muc menu da loc theo quyen cua nguoi dung (chuc nang 48).</summary>
public sealed record MenuDto(
    Guid Id,
    string Ma,
    string Ten,
    string? Icon,
    string? DuongDan,
    int ThuTu,
    bool MoTabMoi,
    List<MenuDto> MenuCon);
