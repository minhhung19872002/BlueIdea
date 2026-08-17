using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.QuanTri;

/// <summary>
/// Nhat ky he thong (audit log). Nguyen tac 4 cua dac ta: moi thao tac thay doi du lieu
/// deu phai ghi ai / khi nao / tu IP nao / gia tri truoc - sau.
/// Bang nay partition theo thang.
/// </summary>
public class NhatKyHeThong : ThucThe
{
    public Guid? NguoiDungId { get; set; }

    public string? TenDangNhap { get; set; }

    /// <summary>TAO | SUA | XOA | DANG_NHAP | XUAT | PHE_DUYET ...</summary>
    public string HanhDong { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string? DoiTuong { get; set; }

    public Guid? DoiTuongId { get; set; }

    public string? MoTa { get; set; }

    public string? DuLieuTruoc { get; set; }

    public string? DuLieuSau { get; set; }

    public string? DiaChiIp { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>THANH_CONG | THAT_BAI</summary>
    public string KetQua { get; set; } = "THANH_CONG";

    public DateTimeOffset ThoiGian { get; set; } = DateTimeOffset.UtcNow;
}

public static class HanhDongAudit
{
    public const string Tao = "TAO";
    public const string Sua = "SUA";
    public const string Xoa = "XOA";
    public const string Xem = "XEM";
    public const string Xuat = "XUAT";
    public const string Nhap = "NHAP";
    public const string DangNhap = "DANG_NHAP";
    public const string DangXuat = "DANG_XUAT";
    public const string PheDuyet = "PHE_DUYET";
    public const string TuChoi = "TU_CHOI";
    public const string KySo = "KY_SO";
    public const string DoiMatKhau = "DOI_MAT_KHAU";
}

public class NhatKyLoi : ThucThe
{
    /// <summary>CANH_BAO | LOI | NGHIEM_TRONG</summary>
    public string MucDo { get; set; } = "LOI";

    public string Nguon { get; set; } = string.Empty;

    public string ThongBao { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    public Dictionary<string, object>? DuLieuNguCanh { get; set; }

    public Guid? NguoiDungId { get; set; }

    public string? DiaChiIp { get; set; }

    public DateTimeOffset ThoiGian { get; set; } = DateTimeOffset.UtcNow;

    public bool DaXuLy { get; set; }
}

public class NhatKyDangNhap : ThucThe
{
    public string TenDangNhap { get; set; } = string.Empty;

    public Guid? NguoiDungId { get; set; }

    public bool ThanhCong { get; set; }

    public string? LyDoThatBai { get; set; }

    public string? DiaChiIp { get; set; }

    public string? UserAgent { get; set; }

    public string? ThietBi { get; set; }

    public DateTimeOffset ThoiGian { get; set; } = DateTimeOffset.UtcNow;
}
