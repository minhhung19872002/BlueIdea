namespace BlueIdea.Domain.Chung;

/// <summary>
/// Lop co so cho moi thuc the trong he thong.
/// Ap dung quy uoc chung Muc 4 dac ta: khoa chinh uuid + audit + soft delete.
/// </summary>
public abstract class ThucThe
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? NguoiTaoId { get; set; }

    public DateTimeOffset NgayTao { get; set; }

    public Guid? NguoiSuaId { get; set; }

    public DateTimeOffset? NgaySua { get; set; }

    /// <summary>Soft delete toan he thong - EF Core ap dung global query filter tren truong nay.</summary>
    public bool DaXoa { get; set; }

    public Guid? NguoiXoaId { get; set; }

    public DateTimeOffset? NgayXoa { get; set; }
}

/// <summary>
/// Lop co so cho bang danh muc: co ma duy nhat, ten, mo ta, thu tu, trang thai.
/// </summary>
public abstract class ThucTheDanhMuc : ThucThe
{
    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    /// <summary>Cot suy dien phuc vu tim kiem khong dau (unaccent + lower).</summary>
    public string TenKhongDau { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public int ThuTu { get; set; }

    /// <summary>1 = hoat dong, 0 = ngung hoat dong.</summary>
    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;
}

public static class TrangThaiDanhMuc
{
    public const short NgungHoatDong = 0;
    public const short HoatDong = 1;
}

/// <summary>
/// Danh dau thuc the co the phat sinh su kien mien (domain event).
/// </summary>
public interface ICoSuKienMien
{
    IReadOnlyCollection<SuKienMien> CacSuKien { get; }

    void ThemSuKien(SuKienMien suKien);

    void XoaCacSuKien();
}

/// <summary>Su kien mien - duoc publish sau khi luu thanh cong (MediatR notification).</summary>
public abstract record SuKienMien
{
    public DateTimeOffset ThoiDiem { get; init; } = DateTimeOffset.UtcNow;
}
