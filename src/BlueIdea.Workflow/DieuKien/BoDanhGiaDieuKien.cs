using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Shared.TiengViet;

namespace BlueIdea.Workflow.DieuKien;

/// <summary>
/// Bo danh gia dieu kien chuyen tiep (rule evaluator) tu viet - KHONG dung eval dong.
/// Ho tro toan tu = != &gt; &gt;= &lt; &lt;= IN CONTAINS BETWEEN va phep AND/OR/NOT long nhau.
/// </summary>
public interface IBoDanhGiaDieuKien
{
    /// <summary>Danh gia bieu thuc. Bieu thuc null/rong duoc coi la LUON DUNG.</summary>
    bool DanhGia(BieuThucDieuKien? bieuThuc, NguCanhDieuKien nguCanh);

    /// <summary>Danh gia kem giai thich - dung cho man hinh mo phong quy trinh va log.</summary>
    KetQuaDanhGiaDieuKien DanhGiaChiTiet(BieuThucDieuKien? bieuThuc, NguCanhDieuKien nguCanh);

    /// <summary>Kiem tra cu phap bieu thuc, tra ve danh sach loi (rong = hop le).</summary>
    IReadOnlyList<string> KiemTraCuPhap(BieuThucDieuKien? bieuThuc);
}

public sealed record KetQuaDanhGiaDieuKien(bool Khop, string GiaiThich);

/// <inheritdoc />
public sealed class BoDanhGiaDieuKien : IBoDanhGiaDieuKien
{
    /// <summary>Gioi han do sau de tranh bieu thuc long nhau vo han (du lieu loi/co y pha hoai).</summary>
    private const int DoSauToiDa = 20;

    public static class Phep
    {
        public const string And = "AND";
        public const string Or = "OR";
        public const string Not = "NOT";
    }

    public static class ToanTu
    {
        public const string Bang = "=";
        public const string KhacBang = "!=";
        public const string LonHon = ">";
        public const string LonHonBang = ">=";
        public const string NhoHon = "<";
        public const string NhoHonBang = "<=";
        public const string Trong = "IN";
        public const string Chua = "CONTAINS";
        public const string Giua = "BETWEEN";

        public static readonly IReadOnlyList<string> TatCa = new[]
        {
            Bang, KhacBang, LonHon, LonHonBang, NhoHon, NhoHonBang, Trong, Chua, Giua
        };
    }

    public bool DanhGia(BieuThucDieuKien? bieuThuc, NguCanhDieuKien nguCanh)
        => DanhGiaChiTiet(bieuThuc, nguCanh).Khop;

    public KetQuaDanhGiaDieuKien DanhGiaChiTiet(BieuThucDieuKien? bieuThuc, NguCanhDieuKien nguCanh)
    {
        ArgumentNullException.ThrowIfNull(nguCanh);

        if (bieuThuc is null)
        {
            return new KetQuaDanhGiaDieuKien(true, "Không có điều kiện — luôn thỏa mãn");
        }

        try
        {
            var khop = DanhGiaNut(bieuThuc, nguCanh, 0, out var giaiThich);
            return new KetQuaDanhGiaDieuKien(khop, giaiThich);
        }
        catch (DieuKienKhongHopLeException ex)
        {
            return new KetQuaDanhGiaDieuKien(false, $"Điều kiện không hợp lệ: {ex.Message}");
        }
    }

    public IReadOnlyList<string> KiemTraCuPhap(BieuThucDieuKien? bieuThuc)
    {
        var loi = new List<string>();
        if (bieuThuc is not null)
        {
            KiemTraNut(bieuThuc, 0, loi);
        }

        return loi;
    }

    // ----------------------------------------------------------------------------------
    // Danh gia
    // ----------------------------------------------------------------------------------

    private bool DanhGiaNut(BieuThucDieuKien nut, NguCanhDieuKien nguCanh, int doSau, out string giaiThich)
    {
        if (doSau > DoSauToiDa)
        {
            throw new DieuKienKhongHopLeException($"Biểu thức lồng quá {DoSauToiDa} cấp");
        }

        if (nut.LaNhom)
        {
            return DanhGiaNhom(nut, nguCanh, doSau, out giaiThich);
        }

        return DanhGiaSoSanh(nut, nguCanh, out giaiThich);
    }

    private bool DanhGiaNhom(BieuThucDieuKien nut, NguCanhDieuKien nguCanh, int doSau, out string giaiThich)
    {
        var phep = (nut.Phep ?? string.Empty).Trim().ToUpperInvariant();
        var con = nut.CacDieuKien ?? new List<BieuThucDieuKien>();

        if (con.Count == 0)
        {
            // Nhom rong: AND rong = true, OR rong = false (theo logic tap hop chuan).
            giaiThich = $"{phep}(rỗng)";
            return phep == Phep.And;
        }

        switch (phep)
        {
            case Phep.And:
            {
                var moTa = new List<string>();
                var ketQua = true;
                foreach (var c in con)
                {
                    // Van danh gia het de giai thich day du (bo qua toi uu short-circuit).
                    var khop = DanhGiaNut(c, nguCanh, doSau + 1, out var mt);
                    moTa.Add(mt);
                    ketQua = ketQua && khop;
                }

                giaiThich = $"({string.Join(" VÀ ", moTa)}) → {DienGiai(ketQua)}";
                return ketQua;
            }

            case Phep.Or:
            {
                var moTa = new List<string>();
                var ketQua = false;
                foreach (var c in con)
                {
                    var khop = DanhGiaNut(c, nguCanh, doSau + 1, out var mt);
                    moTa.Add(mt);
                    ketQua = ketQua || khop;
                }

                giaiThich = $"({string.Join(" HOẶC ", moTa)}) → {DienGiai(ketQua)}";
                return ketQua;
            }

            case Phep.Not:
            {
                if (con.Count != 1)
                {
                    throw new DieuKienKhongHopLeException("Phép NOT phải có đúng 1 điều kiện con");
                }

                var khop = DanhGiaNut(con[0], nguCanh, doSau + 1, out var mt);
                giaiThich = $"KHÔNG({mt}) → {DienGiai(!khop)}";
                return !khop;
            }

            default:
                throw new DieuKienKhongHopLeException($"Phép logic không hỗ trợ: '{nut.Phep}'");
        }
    }

    private static bool DanhGiaSoSanh(BieuThucDieuKien nut, NguCanhDieuKien nguCanh, out string giaiThich)
    {
        if (string.IsNullOrWhiteSpace(nut.Truong))
        {
            throw new DieuKienKhongHopLeException("Thiếu tên trường trong điều kiện");
        }

        var toanTu = (nut.ToanTu ?? "=").Trim().ToUpperInvariant();
        var giaTriTrai = nguCanh.Lay(nut.Truong);
        var giaTriPhai = nut.GiaTri;

        var khop = SoSanh(giaTriTrai, toanTu, giaTriPhai);
        giaiThich = $"{nut.Truong}[{HienThi(giaTriTrai)}] {toanTu} {HienThi(giaTriPhai)} → {DienGiai(khop)}";
        return khop;
    }

    private static bool SoSanh(object? trai, string toanTu, object? phai)
    {
        switch (toanTu)
        {
            case ToanTu.Bang:
                return BangNhau(trai, phai);

            case ToanTu.KhacBang:
                return !BangNhau(trai, phai);

            case ToanTu.LonHon:
            case ToanTu.LonHonBang:
            case ToanTu.NhoHon:
            case ToanTu.NhoHonBang:
                return SoSanhThuTu(trai, toanTu, phai);

            case ToanTu.Trong:
                return TrongDanhSach(trai, phai);

            case ToanTu.Chua:
                return Chua(trai, phai);

            case ToanTu.Giua:
                return Giua(trai, phai);

            default:
                throw new DieuKienKhongHopLeException($"Toán tử không hỗ trợ: '{toanTu}'");
        }
    }

    private static bool BangNhau(object? trai, object? phai)
    {
        var a = ChuyenDoiGiaTri.BocJson(trai);
        var b = ChuyenDoiGiaTri.BocJson(phai);

        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        // Uu tien so sanh so de 80 == 80.0 == "80".
        if (ChuyenDoiGiaTri.ThuLaySo(a, out var soA) && ChuyenDoiGiaTri.ThuLaySo(b, out var soB))
        {
            return soA == soB;
        }

        if (ChuyenDoiGiaTri.ThuLayBool(a, out var boolA) && ChuyenDoiGiaTri.ThuLayBool(b, out var boolB))
        {
            return boolA == boolB;
        }

        if (a is DateTimeOffset or DateTime or DateOnly || b is DateTimeOffset or DateTime or DateOnly)
        {
            if (ChuyenDoiGiaTri.ThuLayThoiGian(a, out var tgA) && ChuyenDoiGiaTri.ThuLayThoiGian(b, out var tgB))
            {
                return tgA == tgB;
            }
        }

        var chuoiA = ChuyenDoiGiaTri.LayChuoi(a);
        var chuoiB = ChuyenDoiGiaTri.LayChuoi(b);
        return string.Equals(chuoiA, chuoiB, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SoSanhThuTu(object? trai, string toanTu, object? phai)
    {
        int thuTu;

        if (ChuyenDoiGiaTri.ThuLaySo(trai, out var soA) && ChuyenDoiGiaTri.ThuLaySo(phai, out var soB))
        {
            thuTu = soA.CompareTo(soB);
        }
        else if (ChuyenDoiGiaTri.ThuLayThoiGian(trai, out var tgA) && ChuyenDoiGiaTri.ThuLayThoiGian(phai, out var tgB))
        {
            thuTu = tgA.CompareTo(tgB);
        }
        else
        {
            var chuoiA = ChuyenDoiGiaTri.LayChuoi(trai);
            var chuoiB = ChuyenDoiGiaTri.LayChuoi(phai);

            // Khong so sanh thu tu duoc khi mot ben null.
            if (chuoiA is null || chuoiB is null)
            {
                return false;
            }

            thuTu = string.Compare(chuoiA, chuoiB, StringComparison.OrdinalIgnoreCase);
        }

        return toanTu switch
        {
            ToanTu.LonHon => thuTu > 0,
            ToanTu.LonHonBang => thuTu >= 0,
            ToanTu.NhoHon => thuTu < 0,
            ToanTu.NhoHonBang => thuTu <= 0,
            _ => false
        };
    }

    private static bool TrongDanhSach(object? trai, object? phai)
    {
        var danhSach = ChuyenDoiGiaTri.ThuLayDanhSach(phai);
        if (danhSach is null)
        {
            // Cho phep viet gia tri don: IN "DAT" tuong duong = "DAT".
            return BangNhau(trai, phai);
        }

        return danhSach.Any(phanTu => BangNhau(trai, phanTu));
    }

    private static bool Chua(object? trai, object? phai)
    {
        // Truong hop 1: ve trai la danh sach -> kiem tra chua phan tu.
        var danhSachTrai = ChuyenDoiGiaTri.ThuLayDanhSach(trai);
        if (danhSachTrai is not null)
        {
            return danhSachTrai.Any(phanTu => BangNhau(phanTu, phai));
        }

        // Truong hop 2: ve trai la van ban -> kiem tra chua chuoi con, khong phan biet hoa thuong va dau.
        var chuoiTrai = ChuyenDoiGiaTri.LayChuoi(trai);
        var chuoiPhai = ChuyenDoiGiaTri.LayChuoi(phai);

        if (chuoiTrai is null || chuoiPhai is null)
        {
            return false;
        }

        return VanBanTiengViet.TaoKhongDau(chuoiTrai)
            .Contains(VanBanTiengViet.TaoKhongDau(chuoiPhai), StringComparison.Ordinal);
    }

    private static bool Giua(object? trai, object? phai)
    {
        var danhSach = ChuyenDoiGiaTri.ThuLayDanhSach(phai);
        if (danhSach is null || danhSach.Count != 2)
        {
            throw new DieuKienKhongHopLeException("Toán tử BETWEEN cần giá trị là mảng 2 phần tử [cận dưới, cận trên]");
        }

        return SoSanhThuTu(trai, ToanTu.LonHonBang, danhSach[0])
               && SoSanhThuTu(trai, ToanTu.NhoHonBang, danhSach[1]);
    }

    // ----------------------------------------------------------------------------------
    // Kiem tra cu phap
    // ----------------------------------------------------------------------------------

    private static void KiemTraNut(BieuThucDieuKien nut, int doSau, List<string> loi)
    {
        if (doSau > DoSauToiDa)
        {
            loi.Add($"Biểu thức lồng quá {DoSauToiDa} cấp");
            return;
        }

        if (nut.LaNhom)
        {
            var phep = (nut.Phep ?? string.Empty).Trim().ToUpperInvariant();
            if (phep is not (Phep.And or Phep.Or or Phep.Not))
            {
                loi.Add($"Phép logic không hợp lệ: '{nut.Phep}'");
            }

            var con = nut.CacDieuKien ?? new List<BieuThucDieuKien>();
            if (phep == Phep.Not && con.Count != 1)
            {
                loi.Add("Phép NOT phải có đúng 1 điều kiện con");
            }

            foreach (var c in con)
            {
                KiemTraNut(c, doSau + 1, loi);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(nut.Truong))
        {
            loi.Add("Điều kiện thiếu tên trường");
        }

        var toanTu = (nut.ToanTu ?? string.Empty).Trim().ToUpperInvariant();
        if (!ToanTu.TatCa.Contains(toanTu))
        {
            loi.Add($"Toán tử không hợp lệ: '{nut.ToanTu}' (hợp lệ: {string.Join(", ", ToanTu.TatCa)})");
        }

        if (toanTu == ToanTu.Giua)
        {
            var ds = ChuyenDoiGiaTri.ThuLayDanhSach(nut.GiaTri);
            if (ds is null || ds.Count != 2)
            {
                loi.Add($"Toán tử BETWEEN tại trường '{nut.Truong}' cần mảng 2 phần tử");
            }
        }
    }

    private static string HienThi(object? giaTri)
    {
        var boc = ChuyenDoiGiaTri.BocJson(giaTri);
        if (boc is null)
        {
            return "(trống)";
        }

        var danhSach = ChuyenDoiGiaTri.ThuLayDanhSach(boc);
        return danhSach is not null
            ? "[" + string.Join(", ", danhSach.Select(p => ChuyenDoiGiaTri.LayChuoi(p) ?? "(trống)")) + "]"
            : ChuyenDoiGiaTri.LayChuoi(boc) ?? "(trống)";
    }

    private static string DienGiai(bool khop) => khop ? "ĐÚNG" : "SAI";
}

/// <summary>Loi cu phap / ngu nghia cua bieu thuc dieu kien.</summary>
public sealed class DieuKienKhongHopLeException : Exception
{
    public DieuKienKhongHopLeException(string message) : base(message)
    {
    }
}
