using System.Text;
using BlueIdea.Reporting;

namespace BlueIdea.UnitTests.Reporting;

/// <summary>
/// Kiem thu ban PDF cua bao cao trung lap (chuc nang 26).
///
/// Bao cao nay di kem ho so hoi dong nen phai sinh duoc ca khi KHONG co ho so trung nao — luc do
/// no chinh la bang chung "da kiem tra va khong phat hien", thu ma tac gia can khi bi nghi ngo.
/// </summary>
public sealed class BoXuatBaoCaoTrungLapPdfTests
{
    public BoXuatBaoCaoTrungLapPdfTests() => BoXuatPdf.CauHinhGiayPhep();

    [Fact]
    public void Sinh_Duoc_Pdf_Khi_Co_Ho_So_Trung()
    {
        var tep = BoXuatBaoCaoTrungLapPdf.Xuat("UBND THÀNH PHỐ", "HỘI ĐỒNG SÁNG KIẾN", TaoBaoCao());

        tep.Length.Should().BeGreaterThan(1000);
        Encoding.ASCII.GetString(tep, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void Sinh_Duoc_Pdf_Khi_Khong_Co_Ho_So_Trung()
    {
        var baoCao = TaoBaoCao() with
        {
            TyLeCaoNhat = 0m,
            MucCanhBao = "AN_TOAN",
            ChiTiet = Array.Empty<DongDoiChieuTrungLapPdf>()
        };

        var tep = BoXuatBaoCaoTrungLapPdf.Xuat("UBND", "HỘI ĐỒNG", baoCao);

        tep.Length.Should().BeGreaterThan(500);
        Encoding.ASCII.GetString(tep, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void Doan_Trung_Rat_Dai_Khong_Lam_Hong_Ban_Xuat()
    {
        var baoCao = TaoBaoCao() with
        {
            ChiTiet = new[]
            {
                new DongDoiChieuTrungLapPdf(
                    "SK-2025-0002", "Hồ sơ đối chiếu", "Phòng Kế hoạch",
                    91m, 88m, 93m, 12,
                    Enumerable.Range(0, 12)
                        .Select(i => new CapDoanTrungPdf(
                            new string('a', 5000), new string('b', 5000), 90m - i))
                        .ToList())
            }
        };

        var tep = BoXuatBaoCaoTrungLapPdf.Xuat("UBND", "HỘI ĐỒNG", baoCao);

        Encoding.ASCII.GetString(tep, 0, 4).Should().Be("%PDF");
    }

    private static BaoCaoTrungLapPdf TaoBaoCao() => new(
        MaHoSo: "SK-2026-0001",
        TenSangKien: "Số hoá quy trình tiếp nhận hồ sơ",
        TenTacGiaChinh: "Nguyễn Văn A",
        TenDonVi: "Phòng Kế hoạch",
        NgayChay: DateTimeOffset.Now,
        PhienBanThuatToan: "1.0",
        TenMoHinhNhung: "BoNhungBamTuVung",
        TongSoDoiChieu: 40,
        TyLeCaoNhat: 62.5m,
        MucCanhBao: "NGHIEM_TRONG",
        DaXemXet: true,
        YKienHoiDong: "Hội đồng xác định là trích dẫn hợp lệ.",
        ChiTiet: new[]
        {
            new DongDoiChieuTrungLapPdf(
                "SK-2025-0002", "Ứng dụng phần mềm quản lý hồ sơ", "Phòng Tổ chức",
                62.5m, 55m, 68m, 3,
                new[]
                {
                    new CapDoanTrungPdf("đoạn văn nguồn", "đoạn văn đối chiếu", 71m)
                })
        });
}
