using BlueIdea.Domain.DanhMuc;

namespace BlueIdea.Workflow.ThoiHan;

/// <summary>
/// Tinh han xu ly cua mot buoc. Ho tro hai che do:
/// - Theo ngay lich (cong thang so ngay).
/// - Theo ngay lam viec: loai tru Thu 7, Chu nhat va ngay nghi le (bang <c>ngay_nghi_le</c>).
/// Moi phep tinh thuc hien tren mui gio Asia/Ho_Chi_Minh roi tra ve UTC.
/// </summary>
public interface ITinhHanXuLy
{
    DateTimeOffset TinhHan(DateTimeOffset batDau, int soNgay, bool theoNgayLamViec);

    bool LaNgayLamViec(DateOnly ngay);

    /// <summary>Dem so ngay lam viec trong khoang (tu, den] - dung cho bao cao thoi gian xu ly.</summary>
    int DemNgayLamViec(DateOnly tu, DateOnly den);

    /// <summary>So ngay (thap phan) da xu ly, tinh tu luc nhan den luc hoan thanh.</summary>
    decimal TinhSoNgayXuLy(DateTimeOffset thoiGianNhan, DateTimeOffset thoiGianXuLy, bool theoNgayLamViec);
}

/// <summary>Nguon cung cap danh sach ngay nghi le (co the doc tu DB hoac cache).</summary>
public interface INguonNgayNghiLe
{
    IReadOnlyCollection<NgayNghiLe> LayDanhSach();
}

/// <summary>Nguon ngay nghi le tinh (dung cho test va cau hinh mac dinh).</summary>
public sealed class NguonNgayNghiLeCoDinh : INguonNgayNghiLe
{
    private readonly IReadOnlyCollection<NgayNghiLe> _danhSach;

    public NguonNgayNghiLeCoDinh(IEnumerable<NgayNghiLe>? danhSach = null)
        => _danhSach = danhSach?.ToList() ?? new List<NgayNghiLe>();

    public IReadOnlyCollection<NgayNghiLe> LayDanhSach() => _danhSach;

    /// <summary>Ngay nghi le co dinh theo Bo luat Lao dong Viet Nam (lap lai hang nam).</summary>
    public static NguonNgayNghiLeCoDinh MacDinhVietNam() => new(new[]
    {
        Le(1, 1, "Tết Dương lịch"),
        Le(4, 30, "Ngày Giải phóng miền Nam"),
        Le(5, 1, "Ngày Quốc tế Lao động"),
        Le(9, 2, "Quốc khánh"),
        Le(9, 1, "Quốc khánh (ngày liền kề)")
    });

    private static NgayNghiLe Le(int thang, int ngay, string ten) => new()
    {
        Ngay = new DateOnly(2000, thang, ngay),
        Ten = ten,
        LapLaiHangNam = true
    };
}

/// <inheritdoc />
public sealed class TinhHanXuLy : ITinhHanXuLy
{
    /// <summary>Mui gio nghiep vu cua he thong (dac ta Muc 4: hien thi theo Asia/Ho_Chi_Minh).</summary>
    public static readonly TimeSpan LechGioVietNam = TimeSpan.FromHours(7);

    /// <summary>Chan vong lap khi du lieu cau hinh sai (vi du toan bo nam la ngay nghi).</summary>
    private const int SoNgayQuetToiDa = 3650;

    private readonly INguonNgayNghiLe _nguonNgayNghi;

    public TinhHanXuLy(INguonNgayNghiLe? nguonNgayNghi = null)
        => _nguonNgayNghi = nguonNgayNghi ?? new NguonNgayNghiLeCoDinh();

    public DateTimeOffset TinhHan(DateTimeOffset batDau, int soNgay, bool theoNgayLamViec)
    {
        if (soNgay <= 0)
        {
            return batDau;
        }

        var gioVn = ChuyenSangGioVietNam(batDau);

        if (!theoNgayLamViec)
        {
            return gioVn.AddDays(soNgay);
        }

        var ngay = DateOnly.FromDateTime(gioVn.DateTime);
        var conLai = soNgay;
        var soVongQuet = 0;

        // Dem tu NGAY KE TIEP: "han xu ly 3 ngay lam viec" = ngay lam viec thu 3 sau ngay nhan.
        while (conLai > 0)
        {
            if (++soVongQuet > SoNgayQuetToiDa)
            {
                throw new InvalidOperationException(
                    "Không xác định được hạn xử lý: cấu hình ngày nghỉ lễ khiến không còn ngày làm việc nào.");
            }

            ngay = ngay.AddDays(1);
            if (LaNgayLamViec(ngay))
            {
                conLai--;
            }
        }

        return new DateTimeOffset(ngay.ToDateTime(TimeOnly.FromDateTime(gioVn.DateTime)), LechGioVietNam);
    }

    public bool LaNgayLamViec(DateOnly ngay)
    {
        if (ngay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        foreach (var le in _nguonNgayNghi.LayDanhSach())
        {
            if (le.TrangThai != Domain.Chung.TrangThaiDanhMuc.HoatDong)
            {
                continue;
            }

            var trung = le.LapLaiHangNam
                ? le.Ngay.Month == ngay.Month && le.Ngay.Day == ngay.Day
                : le.Ngay == ngay;

            if (trung)
            {
                return false;
            }
        }

        return true;
    }

    public int DemNgayLamViec(DateOnly tu, DateOnly den)
    {
        if (den <= tu)
        {
            return 0;
        }

        var dem = 0;
        var ngay = tu;
        while (ngay < den)
        {
            ngay = ngay.AddDays(1);
            if (LaNgayLamViec(ngay))
            {
                dem++;
            }
        }

        return dem;
    }

    public decimal TinhSoNgayXuLy(DateTimeOffset thoiGianNhan, DateTimeOffset thoiGianXuLy, bool theoNgayLamViec)
    {
        if (thoiGianXuLy <= thoiGianNhan)
        {
            return 0m;
        }

        if (!theoNgayLamViec)
        {
            return Math.Round((decimal)(thoiGianXuLy - thoiGianNhan).TotalDays, 2, MidpointRounding.AwayFromZero);
        }

        var nhanVn = ChuyenSangGioVietNam(thoiGianNhan);
        var xuLyVn = ChuyenSangGioVietNam(thoiGianXuLy);

        var ngayNhan = DateOnly.FromDateTime(nhanVn.DateTime);
        var ngayXuLy = DateOnly.FromDateTime(xuLyVn.DateTime);

        var soNgayTron = DemNgayLamViec(ngayNhan, ngayXuLy);

        // Phan le trong ngay hoan thanh (tinh theo chenh lech gio trong ngay).
        var phanLe = (decimal)(xuLyVn.TimeOfDay - nhanVn.TimeOfDay).TotalDays;
        var ketQua = soNgayTron + phanLe;

        return Math.Round(Math.Max(ketQua, 0m), 2, MidpointRounding.AwayFromZero);
    }

    public static DateTimeOffset ChuyenSangGioVietNam(DateTimeOffset thoiDiem)
        => thoiDiem.ToOffset(LechGioVietNam);
}
