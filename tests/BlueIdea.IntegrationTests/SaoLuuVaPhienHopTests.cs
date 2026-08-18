using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu dot 7: theo doi tinh trang sao luu va phan quyen tren API sao luu.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class SaoLuuVaPhienHopTests
{
    private readonly UngDungKiemThu _ungDung;

    public SaoLuuVaPhienHopTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Chua_Gan_Thu_Muc_Sao_Luu_Thi_Bao_Ro_Thay_Vi_Bao_Loi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/sao-luu");
        phanHoi.EnsureSuccessStatusCode();

        var kq = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        // Moi truong kiem thu khong gan thu muc sao luu. Truong hop nay phai tra ve trang thai
        // "chua cau hinh" kem huong dan, khong duoc nem 500 — quan tri vien mo man hinh ra ma
        // thay trang loi do thi tuong he thong hong.
        kq.GetProperty("daCauHinh").GetBoolean().Should().BeFalse();
        kq.GetProperty("mucCanhBao").GetString().Should().Be("CHUA_CAU_HINH");
        kq.GetProperty("thongDiep").GetString().Should().Contain("sao-luu-blueidea.sh");
        kq.GetProperty("danhSach").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Tai_Khoan_Khong_Co_Quyen_Cau_Hinh_Khong_Xem_Duoc_Tinh_Trang_Sao_Luu()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.GetAsync("/api/v1/sao-luu");

        // Duong dan thu muc va lich sao luu la thong tin ha tang — khong de lo cho moi tai khoan.
        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Khong_Co_Api_Khoi_Phuc_Tren_Web()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // Khoi phuc ghi de toan bo CSDL dang chay nen co y khong mo qua web. Kiem thu nay chot
        // lai quyet dinh do de lan sau khong ai vo tinh them vao.
        var khoiPhuc = await admin.PostAsync("/api/v1/sao-luu/khoi-phuc", null);
        var tao = await admin.PostAsync("/api/v1/sao-luu", null);

        // 404 hoac 405 deu dat yeu cau: dieu can chot la KHONG co endpoint nao thuc thi duoc.
        khoiPhuc.StatusCode.Should()
            .BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        tao.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }
}
