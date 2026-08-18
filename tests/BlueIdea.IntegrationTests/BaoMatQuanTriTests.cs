using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.Domain.Chung;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Hai bien phap an toan thong tin cap do 2 danh rieng cho tai khoan quan tri (Muc 6 dac ta):
/// buoc bat xac thuc hai lop, va gioi han dai dia chi IP duoc dung tai khoan quan tri.
///
/// Ca hai mac dinh TAT — bat san khi nang cap se khoa luon nguoi dang van hanh he thong — nen
/// kiem thu phai tu bat len roi tra lai nguyen trang.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class BaoMatQuanTriTests
{
    private readonly UngDungKiemThu _ungDung;

    public BaoMatQuanTriTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Bat_Yeu_Cau_Mfa_Thi_Quan_Tri_Bi_Danh_Dau_Phai_Bat()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        (await LayToiAsync(admin)).GetProperty("buocBatMfa").GetBoolean().Should()
            .BeFalse("mặc định tắt nên không ai bị ràng buộc");

        await DatCauHinhAsync(admin, KhoaCauHinh.MfaBatBuocQuanTri, "true");

        try
        {
            var sauKhiBat = await LayToiAsync(admin);

            sauKhiBat.GetProperty("buocBatMfa").GetBoolean().Should()
                .BeTrue("quản trị viên chưa bật MFA thì phải bị đưa sang màn hình cài đặt");

            // Van dang nhap duoc: chan dang nhap thi ho khong con duong nao vao de bat MFA.
            var lai = await _ungDung.TaoClientDaDangNhapAsync("admin");
            (await LayToiAsync(lai)).GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        }
        finally
        {
            await DatCauHinhAsync(admin, KhoaCauHinh.MfaBatBuocQuanTri, "false");
        }
    }

    [Fact]
    public async Task Nguoi_Dung_Thuong_Khong_Bi_Rang_Buoc_Mfa()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        await DatCauHinhAsync(admin, KhoaCauHinh.MfaBatBuocQuanTri, "true");

        try
        {
            var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

            (await LayToiAsync(tacGia)).GetProperty("buocBatMfa").GetBoolean().Should()
                .BeFalse("yêu cầu này chỉ áp cho vai trò quản trị hệ thống");
        }
        finally
        {
            await DatCauHinhAsync(admin, KhoaCauHinh.MfaBatBuocQuanTri, "false");
        }
    }

    [Fact]
    public async Task Ip_Ngoai_Danh_Sach_Bi_Chan_Voi_Tai_Khoan_Quan_Tri()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // Dai khong the chua may chay kiem thu.
        await DatCauHinhAsync(admin, KhoaCauHinh.IpChoPhepQuanTri, "203.0.113.0/24");

        try
        {
            var bijChan = await admin.GetAsync("/api/v1/he-thong/cau-hinh");

            bijChan.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "tài khoản quản trị gọi từ ngoài dải đã đăng ký phải bị chặn");

            var loi = await bijChan.Content.ReadFromJsonAsync<JsonElement>();
            loi.GetProperty("thongBao").GetString().Should().Contain("dải địa chỉ");
        }
        finally
        {
            // Go bang duong khac vi tai khoan quan tri dang bi chan chinh minh.
            await GoGioiHanIpAsync();
        }
    }

    [Fact]
    public async Task De_Trong_Danh_Sach_Ip_Thi_Khong_Gioi_Han()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        (await admin.GetAsync("/api/v1/he-thong/cau-hinh")).EnsureSuccessStatusCode();
    }

    // ---------------------------------------------------------------------------------

    private static async Task<JsonElement> LayToiAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/xac-thuc/toi");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task DatCauHinhAsync(HttpClient client, string khoa, string giaTri)
    {
        var phanHoi = await client.PutAsJsonAsync("/api/v1/he-thong/cau-hinh",
            new[] { new { khoa, giaTri } });

        phanHoi.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Ghi thang xuong CSDL roi xoa cache cau hinh: tai khoan quan tri luc nay dang bi chinh
    /// gioi han vua dat chan lai nen khong goi API de go duoc.
    /// </summary>
    private async Task GoGioiHanIpAsync()
    {
        using var pham = _ungDung.Services.CreateScope();

        var db = pham.ServiceProvider
            .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var muc = await db.CauHinhHeThong
            .FirstAsync(x => x.Khoa == KhoaCauHinh.IpChoPhepQuanTri);

        muc.GiaTri = string.Empty;
        await db.SaveChangesAsync();

        await pham.ServiceProvider
            .GetRequiredService<Application.Chung.IDichVuCauHinh>()
            .XoaCacheAsync();
    }
}
