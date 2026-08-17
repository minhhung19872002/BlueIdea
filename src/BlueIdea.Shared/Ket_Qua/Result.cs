namespace BlueIdea.Shared.KetQua;

/// <summary>
/// Chi tiet mot loi nghiep vu / loi validation gan voi mot truong du lieu.
/// Tuong ung phan tu trong "chiTietLoi" cua response chuan (Muc 8 dac ta).
/// </summary>
public sealed record ChiTietLoi(string Truong, string ThongBao);

/// <summary>
/// Ket qua tra ve cua mot thao tac nghiep vu (khong mang du lieu).
/// Dung xuyen suot Application layer thay cho viec nem exception cho luong nghiep vu binh thuong.
/// </summary>
public class Result
{
    protected Result(bool thanhCong, string? thongBao, string? maLoi, IReadOnlyList<ChiTietLoi>? chiTietLoi)
    {
        ThanhCong = thanhCong;
        ThongBao = thongBao ?? string.Empty;
        MaLoi = maLoi;
        ChiTietLoi = chiTietLoi ?? Array.Empty<ChiTietLoi>();
    }

    public bool ThanhCong { get; }

    public bool ThatBai => !ThanhCong;

    public string ThongBao { get; }

    /// <summary>Ma loi nghiep vu dang chuoi, vi du: DOT_DE_NGHI_DA_DONG.</summary>
    public string? MaLoi { get; }

    public IReadOnlyList<ChiTietLoi> ChiTietLoi { get; }

    public static Result Ok(string? thongBao = null) => new(true, thongBao, null, null);

    public static Result Loi(string maLoi, string? thongBao = null, IReadOnlyList<ChiTietLoi>? chiTiet = null)
        => new(false, thongBao ?? maLoi, maLoi, chiTiet);

    public static Result LoiValidation(IReadOnlyList<ChiTietLoi> chiTiet, string? thongBao = null)
        => new(false, thongBao ?? "Dữ liệu không hợp lệ", MaLoiHeThong.DuLieuKhongHopLe, chiTiet);
}

/// <summary>
/// Ket qua tra ve co kem du lieu.
/// </summary>
public sealed class Result<T> : Result
{
    private Result(bool thanhCong, T? duLieu, string? thongBao, string? maLoi, IReadOnlyList<ChiTietLoi>? chiTietLoi)
        : base(thanhCong, thongBao, maLoi, chiTietLoi)
    {
        DuLieu = duLieu;
    }

    public T? DuLieu { get; }

    public static Result<T> Ok(T duLieu, string? thongBao = null) => new(true, duLieu, thongBao, null, null);

    public static new Result<T> Loi(string maLoi, string? thongBao = null, IReadOnlyList<ChiTietLoi>? chiTiet = null)
        => new(false, default, thongBao ?? maLoi, maLoi, chiTiet);

    public static new Result<T> LoiValidation(IReadOnlyList<ChiTietLoi> chiTiet, string? thongBao = null)
        => new(false, default, thongBao ?? "Dữ liệu không hợp lệ", MaLoiHeThong.DuLieuKhongHopLe, chiTiet);

    public static implicit operator Result<T>(T duLieu) => Ok(duLieu);
}
