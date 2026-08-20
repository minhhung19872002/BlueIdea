using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Pham vi don vi khi quan tri tai khoan nguoi khac (REQ-21, REQ-43).
///
/// Hai luong nay tung duoc sua loi IDOR nhung ghi chu truy vet noi "moi truong khong co Docker
/// nen chua kiem thu duoc" — ly do do khong con dung. Day la phan chan IDOR: quyen
/// NGUOI_DUNG.DAT_LAI_MAT_KHAU cho phep dat lai mat khau, nhung KHONG cho phep dat lai mat khau
/// cua nguoi ngoai pham vi don vi cua minh. Thieu phep kiem nay thi mot quan tri don vi doi duoc
/// mat khau cua bat ky ai trong he thong, chi can biet id.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class PhamViQuanTriNguoiDungTests
{
    private readonly UngDungKiemThu _ungDung;

    public PhamViQuanTriNguoiDungTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Quan_Tri_Don_Vi_Khong_Dat_Lai_Mat_Khau_Cho_Nguoi_Don_Vi_Khac()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var qtDonVi = await _ungDung.TaoClientDaDangNhapAsync("qtdonvi");

        // gv.lan thuoc TH_LE_LOI (duoi PHONG_GDDT), ngoai pham vi cua qtdonvi (VAN_PHONG).
        var nguoiNgoaiPhamVi = await LayIdNguoiDungAsync(admin, "gv.lan");

        var phanHoi = await qtDonVi.PostAsync(
            $"/api/v1/he-thong/nguoi-dung/{nguoiNgoaiPhamVi}/dat-lai-mat-khau", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "ngoài phạm vi thì phải trả 404 chứ không phải 403 — 403 xác nhận id đó có thật");
    }

    [Fact]
    public async Task Quan_Tri_Don_Vi_Dat_Lai_Duoc_Mat_Khau_Trong_Don_Vi_Minh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var qtDonVi = await _ungDung.TaoClientDaDangNhapAsync("qtdonvi");

        // hoidong05 thuoc VAN_PHONG, cung don vi voi qtdonvi.
        var nguoiTrongPhamVi = await LayIdNguoiDungAsync(admin, "hoidong05");

        var phanHoi = await qtDonVi.PostAsync(
            $"/api/v1/he-thong/nguoi-dung/{nguoiTrongPhamVi}/dat-lai-mat-khau", null);

        // Phep doi chung: neu ca truong hop nay cung hong thi phep kiem tren khong chung minh
        // duoc gi — co the chi la thieu quyen chu khong phai chan dung pham vi.
        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu");

        duLieu.GetProperty("matKhauTam").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Quan_Tri_Don_Vi_Khong_Go_Mfa_Cho_Nguoi_Don_Vi_Khac()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var qtDonVi = await _ungDung.TaoClientDaDangNhapAsync("qtdonvi");

        var nguoiNgoaiPhamVi = await LayIdNguoiDungAsync(admin, "gv.lan");

        var phanHoi = await qtDonVi.PostAsync(
            $"/api/v1/xac-thuc/mfa/go/{nguoiNgoaiPhamVi}", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Khong_Go_Mfa_Cho_Chinh_Minh_Bang_Duong_Quan_Tri()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chinhMinh = await LayIdNguoiDungAsync(admin, "admin");

        var phanHoi = await admin.PostAsync($"/api/v1/xac-thuc/mfa/go/{chinhMinh}", null);

        // Go MFA cua chinh minh phai di qua luong thong thuong (co nhap mat khau), khong duoc
        // muon duong quan tri de tu thao MFA cua minh ma khong xac thuc lai.
        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Tac_Gia_Khong_Dat_Lai_Mat_Khau_Cho_Ai()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.hung");

        var mucTieu = await LayIdNguoiDungAsync(admin, "gv.lan");

        var phanHoi = await tacGia.PostAsync(
            $"/api/v1/he-thong/nguoi-dung/{mucTieu}/dat-lai-mat-khau", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------------

    private static async Task<Guid> LayIdNguoiDungAsync(HttpClient admin, string tenDangNhap)
    {
        var phanHoi = await admin.GetAsync(
            $"/api/v1/he-thong/nguoi-dung?tuKhoa={tenDangNhap}&trang=1&soDong=20");

        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu");

        foreach (var x in duLieu.EnumerateArray())
        {
            if (x.GetProperty("tenDangNhap").GetString() == tenDangNhap)
            {
                return x.GetProperty("id").GetGuid();
            }
        }

        throw new InvalidOperationException($"Không tìm thấy tài khoản '{tenDangNhap}'.");
    }
}
