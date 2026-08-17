using BlueIdea.Domain.DanhMuc;
using BlueIdea.Workflow.ThoiHan;

namespace BlueIdea.UnitTests.Workflow;

/// <summary>Kiem thu tinh han xu ly theo ngay lam viec + ngay le (Muc 11 dac ta).</summary>
public class TinhHanXuLyTests
{
    private static readonly TimeSpan GioVn = TimeSpan.FromHours(7);

    private static DateTimeOffset NgayVn(int nam, int thang, int ngay, int gio = 9, int phut = 0)
        => new(nam, thang, ngay, gio, phut, 0, GioVn);

    [Fact]
    public void Khong_Theo_Ngay_Lam_Viec_Thi_Cong_Thang_So_Ngay()
    {
        var tinhHan = new TinhHanXuLy();

        // Thu Sau 2026-08-14 + 3 ngay lich = Thu Hai 2026-08-17.
        var han = tinhHan.TinhHan(NgayVn(2026, 8, 14), 3, theoNgayLamViec: false);

        han.Should().Be(NgayVn(2026, 8, 17));
    }

    [Fact]
    public void Theo_Ngay_Lam_Viec_Bo_Qua_Thu_Bay_Chu_Nhat()
    {
        var tinhHan = new TinhHanXuLy();

        // Thu Nam 2026-08-13 + 3 ngay lam viec = Thu Ba 2026-08-18
        // (14 Sáu, [15-16 cuối tuần], 17 Hai, 18 Ba).
        var han = tinhHan.TinhHan(NgayVn(2026, 8, 13), 3, theoNgayLamViec: true);

        han.Should().Be(NgayVn(2026, 8, 18));
        han.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
    }

    [Fact]
    public void Theo_Ngay_Lam_Viec_Bo_Qua_Ngay_Le_Lap_Lai_Hang_Nam()
    {
        var tinhHan = new TinhHanXuLy(NguonNgayNghiLeCoDinh.MacDinhVietNam());

        // Thu Ba 2026-04-28 + 2 ngay lam viec:
        // 29/4 (Tư) là ngày làm việc thứ 1; 30/4 nghỉ lễ; 1/5 nghỉ lễ;
        // 2-3/5 cuối tuần; 4/5 (Hai) là ngày làm việc thứ 2.
        var han = tinhHan.TinhHan(NgayVn(2026, 4, 28), 2, theoNgayLamViec: true);

        han.Date.Should().Be(new DateTime(2026, 5, 4));
    }

    [Fact]
    public void Ngay_Le_Khong_Lap_Lai_Chi_Ap_Dung_Dung_Nam_Do()
    {
        var leRieng = new NguonNgayNghiLeCoDinh(new[]
        {
            new NgayNghiLe { Ngay = new DateOnly(2026, 8, 18), Ten = "Nghỉ bù đơn vị", LapLaiHangNam = false }
        });
        var tinhHan = new TinhHanXuLy(leRieng);

        tinhHan.LaNgayLamViec(new DateOnly(2026, 8, 18)).Should().BeFalse();
        tinhHan.LaNgayLamViec(new DateOnly(2027, 8, 18)).Should().BeTrue("ngày nghỉ này không lặp lại hằng năm");
    }

    [Fact]
    public void Giu_Nguyen_Gio_Trong_Ngay_Khi_Tinh_Han()
    {
        var tinhHan = new TinhHanXuLy();

        var han = tinhHan.TinhHan(NgayVn(2026, 8, 17, 14, 30), 1, theoNgayLamViec: true);

        han.Hour.Should().Be(14);
        han.Minute.Should().Be(30);
    }

    [Fact]
    public void So_Ngay_Bang_Khong_Tra_Ve_Chinh_Thoi_Diem_Bat_Dau()
    {
        var tinhHan = new TinhHanXuLy();
        var batDau = NgayVn(2026, 8, 17);

        tinhHan.TinhHan(batDau, 0, theoNgayLamViec: true).Should().Be(batDau);
    }

    [Fact]
    public void Dem_Ngay_Lam_Viec_Trong_Mot_Tuan()
    {
        var tinhHan = new TinhHanXuLy();

        // Từ Thứ Hai 17/8 đến Thứ Hai 24/8 (khoảng mở đầu, đóng cuối) = 5 ngày làm việc.
        var so = tinhHan.DemNgayLamViec(new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 24));

        so.Should().Be(5);
    }

    [Fact]
    public void Tinh_So_Ngay_Xu_Ly_Theo_Ngay_Lich()
    {
        var tinhHan = new TinhHanXuLy();

        var so = tinhHan.TinhSoNgayXuLy(NgayVn(2026, 8, 17, 8), NgayVn(2026, 8, 19, 20), theoNgayLamViec: false);

        so.Should().Be(2.5m);
    }

    [Fact]
    public void Tinh_So_Ngay_Xu_Ly_Tra_Ve_Khong_Khi_Xu_Ly_Truoc_Khi_Nhan()
    {
        var tinhHan = new TinhHanXuLy();

        tinhHan.TinhSoNgayXuLy(NgayVn(2026, 8, 19), NgayVn(2026, 8, 17), theoNgayLamViec: true)
            .Should().Be(0m);
    }

    [Fact]
    public void Cau_Hinh_Sai_Khien_Khong_Con_Ngay_Lam_Viec_Thi_Nem_Loi_Ro_Rang()
    {
        // Toan bo 7 ngay trong tuan deu la ngay nghi -> khong the tinh han.
        var moiNgayDeuNghi = Enumerable.Range(1, 12)
            .SelectMany(thang => Enumerable.Range(1, DateTime.DaysInMonth(2000, thang))
                .Select(ngay => new NgayNghiLe
                {
                    Ngay = new DateOnly(2000, thang, ngay),
                    Ten = "Nghỉ",
                    LapLaiHangNam = true
                }))
            .ToList();

        var tinhHan = new TinhHanXuLy(new NguonNgayNghiLeCoDinh(moiNgayDeuNghi));

        var hanhDong = () => tinhHan.TinhHan(NgayVn(2026, 8, 17), 1, theoNgayLamViec: true);

        hanhDong.Should().Throw<InvalidOperationException>()
            .WithMessage("*ngày nghỉ lễ*");
    }

    [Fact]
    public void Quy_Doi_Ve_Gio_Viet_Nam_Truoc_Khi_Tinh()
    {
        var tinhHan = new TinhHanXuLy();

        // 2026-08-16 20:00 UTC = 2026-08-17 03:00 giờ VN (Thứ Hai) -> +1 ngày làm việc = Thứ Ba 18/8.
        var utc = new DateTimeOffset(2026, 8, 16, 20, 0, 0, TimeSpan.Zero);
        var han = tinhHan.TinhHan(utc, 1, theoNgayLamViec: true);

        han.ToOffset(GioVn).Date.Should().Be(new DateTime(2026, 8, 18));
    }
}
