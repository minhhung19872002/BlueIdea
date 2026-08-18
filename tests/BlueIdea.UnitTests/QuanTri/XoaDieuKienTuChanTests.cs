using BlueIdea.Infrastructure.Seed;

namespace BlueIdea.UnitTests.QuanTri;

/// <summary>
/// Kiem thu phep cat JSON tren snapshot quy trinh.
///
/// Quy trinh duoc dong bang vao tung ho so luc nop, nen sua bang quy trinh khong cham toi ho so
/// dang chay. De mo lai nhanh bi chan cho chung phai sua thang trong chuoi JSON — va cat JSON bang
/// tay la viec de sai ma hau qua nang: lam vo snapshot la ho so mat quy trinh, khong con di tiep
/// duoc buoc nao.
/// </summary>
public class XoaDieuKienTuChanTests
{
    [Fact]
    public void Bo_Dieu_Kien_Goc_Tren_Hanh_Dong_Nguoi_Dung()
    {
        const string truoc =
            """{"ma":"BO_SUNG_HO_SO","dieuKien":{"truong":"hanh_dong_nguoi_dung","toanTu":"=","giaTri":"BO_SUNG","laNhom":false},"hanhDong":["GUI_EMAIL"]}""";

        var sau = DuLieuMau.XoaDieuKienHanhDongNguoiDung(truoc);

        sau.Should().Be(
            """{"ma":"BO_SUNG_HO_SO","dieuKien":null,"hanhDong":["GUI_EMAIL"]}""");
    }

    [Fact]
    public void Bo_Duoc_Nhieu_Dieu_Kien_Trong_Cung_Mot_Snapshot()
    {
        const string truoc =
            """[{"dieuKien":{"truong":"hanh_dong_nguoi_dung","giaTri":"BO_SUNG"}},{"dieuKien":{"truong":"hanh_dong_nguoi_dung","giaTri":"TU_CHOI"}}]""";

        var sau = DuLieuMau.XoaDieuKienHanhDongNguoiDung(truoc);

        sau.Should().Be("""[{"dieuKien":null},{"dieuKien":null}]""");
    }

    /// <summary>
    /// Dieu kien tren bien khac phai giu nguyen tuyet doi — do la nhung nhanh do DU LIEU quyet dinh
    /// (tong diem, ty le trung lap) va chung dang hoat dong dung.
    /// </summary>
    [Fact]
    public void Giu_Nguyen_Dieu_Kien_Tren_Bien_Khac()
    {
        const string truoc =
            """{"dieuKien":{"truong":"tong_diem","toanTu":">=","giaTri":50,"laNhom":false}}""";

        DuLieuMau.XoaDieuKienHanhDongNguoiDung(truoc).Should().Be(truoc);
    }

    /// <summary>
    /// Phep so sanh nam BEN TRONG mot nhom AND/OR phai giu nguyen: do la bieu thuc quan tri vien tu
    /// khai cho quy trinh rieng, va thay no bang null se lam vo ca bieu thuc (mang chua null).
    /// </summary>
    [Fact]
    public void Giu_Nguyen_Khi_Nam_Trong_Nhom_And_Or()
    {
        const string truoc =
            """{"dieuKien":{"laNhom":true,"phep":"AND","cacDieuKien":[{"truong":"hanh_dong_nguoi_dung","giaTri":"KHAN"},{"truong":"tong_diem","toanTu":">=","giaTri":80}]}}""";

        DuLieuMau.XoaDieuKienHanhDongNguoiDung(truoc).Should().Be(truoc);
    }

    [Fact]
    public void Snapshot_Khong_Chua_Bien_Do_Thi_Khong_Doi_Gi()
    {
        const string truoc = """{"ma":"DAT","dieuKien":null,"hanhDong":[]}""";

        DuLieuMau.XoaDieuKienHanhDongNguoiDung(truoc).Should().Be(truoc);
    }

    /// <summary>
    /// Doi tuong dieu kien long nhau nhieu cap van phai cat dung dau ngoac dong tuong ung — dem sai
    /// mot dau ngoac la JSON con lai khong doc duoc.
    /// </summary>
    [Fact]
    public void Cat_Dung_Dau_Ngoac_Khi_Co_Doi_Tuong_Long_Nhau()
    {
        const string truoc =
            """{"dieuKien":{"truong":"hanh_dong_nguoi_dung","meta":{"a":{"b":1}},"giaTri":"BO_SUNG"},"sau":"con-lai"}""";

        DuLieuMau.XoaDieuKienHanhDongNguoiDung(truoc).Should()
            .Be("""{"dieuKien":null,"sau":"con-lai"}""");
    }
}
