using System.Globalization;

namespace BlueIdea.Workflow.DieuKien;

/// <summary>
/// Ngu canh danh gia dieu kien chuyen tiep: tap bien lay tu ho so va ket qua xu ly.
/// Ten bien dung snake_case giong dac ta (tong_diem, ty_le_trung_lap, linh_vuc_id...).
/// </summary>
public sealed class NguCanhDieuKien
{
    private readonly Dictionary<string, object?> _bien = new(StringComparer.OrdinalIgnoreCase);

    public NguCanhDieuKien()
    {
    }

    public NguCanhDieuKien(IDictionary<string, object?> bien)
    {
        foreach (var cap in bien)
        {
            _bien[cap.Key] = cap.Value;
        }
    }

    public IReadOnlyDictionary<string, object?> Bien => _bien;

    public NguCanhDieuKien Dat(string ten, object? giaTri)
    {
        _bien[ten] = giaTri;
        return this;
    }

    public bool CoBien(string ten) => _bien.ContainsKey(ten);

    public object? Lay(string ten) => _bien.TryGetValue(ten, out var giaTri) ? giaTri : null;

    public static NguCanhDieuKien Tu(params (string Ten, object? GiaTri)[] cacBien)
    {
        var nguCanh = new NguCanhDieuKien();
        foreach (var (ten, giaTri) in cacBien)
        {
            nguCanh.Dat(ten, giaTri);
        }

        return nguCanh;
    }

    public override string ToString()
        => string.Join(", ", _bien.Select(c =>
            $"{c.Key}={Convert.ToString(c.Value, CultureInfo.InvariantCulture)}"));
}

/// <summary>Ten bien chuan dung trong dieu kien chuyen tiep.</summary>
public static class BienNguCanh
{
    public const string TongDiem = "tong_diem";
    public const string DiemTrungBinh = "diem_trung_binh";
    public const string TyLeTrungLap = "ty_le_trung_lap";
    public const string LinhVucId = "linh_vuc_id";
    public const string DonViId = "don_vi_id";
    public const string DotDeNghiId = "dot_de_nghi_id";
    public const string SoPhieuDongY = "so_phieu_dong_y";
    public const string SoPhieuKhongDongY = "so_phieu_khong_dong_y";
    public const string SoPhieuCham = "so_phieu_cham";
    public const string TyLeDongThuan = "ty_le_dong_thuan";
    public const string CapXetDuyet = "cap_xet_duyet";
    public const string KetQua = "ket_qua";
    public const string MucCongNhanId = "muc_cong_nhan_id";
    public const string TrangThaiTong = "trang_thai_tong";
    public const string HanhDongNguoiDung = "hanh_dong_nguoi_dung";
    public const string SoTacGia = "so_tac_gia";
    public const string GiaTriLamLoi = "gia_tri_lam_loi";
}
