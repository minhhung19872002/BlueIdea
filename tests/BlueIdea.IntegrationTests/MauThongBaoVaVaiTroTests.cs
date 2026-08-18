using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop dot bo sung: mau thong bao (chuc nang 50), ngay nghi le (chuc nang 46),
/// sao chep vai tro (chuc nang 45), thu ket noi lien thong va y kien rieng tung ho so.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class MauThongBaoVaVaiTroTests
{
    private readonly UngDungKiemThu _ungDung;

    public MauThongBaoVaVaiTroTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ---------------------------------------------------------- Mau thong bao

    [Fact]
    public async Task Xem_Truoc_Mau_Thay_Bien_Chua_Co_Du_Lieu_Bang_Placeholder()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tao = await admin.PostAsJsonAsync("/api/v1/mau-thong-bao", new
        {
            ma = $"KT_XEM_TRUOC_{Guid.NewGuid():N}"[..24],
            ten = "Mẫu kiểm thử xem trước",
            kenh = "EMAIL",
            suKien = "HO_SO_TIEP_NHAN",
            tieuDe = "Hồ sơ {{ ma_ho_so }} đã tiếp nhận",
            noiDung = "Kính gửi {{ ho_ten }}, hồ sơ {{ ma_ho_so }} đã được tiếp nhận.",
            danhSachBien = new[] { "ho_ten", "ma_ho_so" },
            trangThai = 1
        });

        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetString()!;

        var xemTruoc = await admin.PostAsJsonAsync(
            $"/api/v1/mau-thong-bao/{id}/xem-truoc",
            new Dictionary<string, string> { ["ho_ten"] = "Nguyễn Văn A" });

        xemTruoc.EnsureSuccessStatusCode();
        var kq = (await xemTruoc.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        // Bien co du lieu thi thay that; bien khong truyen thi hien [ten_bien] de nguoi soan
        // nhin ra ngay minh con thieu gi, thay vi de lai chuoi {{ }} trong mail gui di.
        kq.GetProperty("noiDung").GetString().Should().Contain("Nguyễn Văn A");
        kq.GetProperty("noiDung").GetString().Should().Contain("[ma_ho_so]");
        kq.GetProperty("noiDung").GetString().Should().NotContain("{{");
    }

    // ------------------------------------------------------------ Ngay nghi le

    [Fact]
    public async Task Khong_Khai_Bao_Trung_Mot_Ngay_Nghi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ngay = new DateOnly(2031, 5, 17).ToString("yyyy-MM-dd");

        var lan1 = await admin.PostAsJsonAsync("/api/v1/ngay-nghi-le", new
        {
            ngay,
            ten = "Ngày kiểm thử",
            lapLaiHangNam = false,
            trangThai = 1
        });

        lan1.EnsureSuccessStatusCode();

        var lan2 = await admin.PostAsJsonAsync("/api/v1/ngay-nghi-le", new
        {
            ngay,
            ten = "Ngày kiểm thử trùng",
            lapLaiHangNam = false,
            trangThai = 1
        });

        // Trung khoa => 409 Conflict (dung ngu nghia HTTP cho xung dot du lieu ton tai san).
        lan2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var trongNam = await admin.GetAsync("/api/v1/ngay-nghi-le?nam=2031");
        trongNam.EnsureSuccessStatusCode();

        (await trongNam.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .Count(x => x.GetProperty("ngay").GetString() == ngay)
            .Should().Be(1);
    }

    // --------------------------------------------------------- Sao chep vai tro

    [Fact]
    public async Task Sao_Chep_Vai_Tro_Giu_Nguyen_Quyen_Va_Khong_Phai_Vai_Tro_He_Thong()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var dsBanDau = await LayVaiTroAsync(admin);
        var nguon = dsBanDau.First(x => x.GetProperty("quyenIds").GetArrayLength() > 0);
        var soQuyenGoc = nguon.GetProperty("quyenIds").GetArrayLength();
        var maBanSao = $"KT_SAO_{Guid.NewGuid():N}"[..20].ToUpperInvariant();

        var saoChep = await admin.PostAsJsonAsync(
            $"/api/v1/he-thong/vai-tro/{nguon.GetProperty("id").GetString()}/sao-chep",
            new { ma = maBanSao, ten = "Vai trò bản sao kiểm thử" });

        saoChep.EnsureSuccessStatusCode();

        var banSao = (await LayVaiTroAsync(admin))
            .Single(x => x.GetProperty("ma").GetString() == maBanSao);

        banSao.GetProperty("quyenIds").GetArrayLength().Should().Be(soQuyenGoc);

        // Ban sao khong bao gio la vai tro he thong: vai tro he thong bi chan xoa, sao ra ma giu
        // co do thi sinh ra vai tro rac vinh vien khong go duoc.
        banSao.GetProperty("laHeThong").GetBoolean().Should().BeFalse();

        var trung = await admin.PostAsJsonAsync(
            $"/api/v1/he-thong/vai-tro/{nguon.GetProperty("id").GetString()}/sao-chep",
            new { ma = maBanSao, ten = "Trùng mã" });

        trung.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ------------------------------------------------- Y kien rieng tung ho so

    [Fact]
    public async Task Ghi_Y_Kien_Rieng_Tung_Ho_So_Trong_Phien_Hop()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var hoiDong = await LayDuLieuAsync(admin, "/api/v1/hoi-dong/chon");
        var dsHoSo = await admin.GetAsync("/api/v1/sang-kien?soDong=1");
        dsHoSo.EnsureSuccessStatusCode();

        var sangKienId = (await dsHoSo.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu")[0].GetProperty("id").GetString()!;

        var tao = await admin.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop", new
        {
            hoiDongId = hoiDong[0].GetProperty("id").GetString(),
            tenPhien = "Phiên kiểm thử ý kiến riêng",
            thoiGianBatDau = DateTimeOffset.UtcNow,
            hinhThuc = "TRUC_TIEP",
            sangKienIds = new[] { sangKienId }
        });

        tao.EnsureSuccessStatusCode();
        var phienId = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        var ghi = await admin.PostAsJsonAsync($"/api/v1/hoi-dong/phien-hop/{phienId}/y-kien-ho-so",
            new { sangKienId, ketLuanRieng = "Hồ sơ đủ điều kiện công nhận.", ketQua = "DAT" });

        ghi.EnsureSuccessStatusCode();

        var phien = await LayDuLieuAsync(admin, $"/api/v1/hoi-dong/phien-hop/{phienId}");
        var dong = phien.GetProperty("danhSachHoSo").EnumerateArray()
            .Single(x => x.GetProperty("sangKienId").GetString() == sangKienId);

        dong.GetProperty("ketLuanRieng").GetString().Should().Contain("đủ điều kiện");
        dong.GetProperty("ketQua").GetString().Should().Be("DAT");

        var saiKetQua = await admin.PostAsJsonAsync(
            $"/api/v1/hoi-dong/phien-hop/{phienId}/y-kien-ho-so",
            new { sangKienId, ketLuanRieng = "x", ketQua = "KHONG_TON_TAI" });

        saiKetQua.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // -------------------------------------------------------- Thu ket noi

    [Fact]
    public async Task Thu_Ket_Noi_He_Thong_Ngoai_Bao_Ro_Ket_Qua()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tao = await admin.PostAsJsonAsync("/api/v1/tich-hop/he-thong", new
        {
            ma = $"KT{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            ten = "Hệ thống kiểm thử kết nối",
            endpointBase = "http://khong-ton-tai.blueidea.local/api",
            loaiXacThuc = "API_KEY",
            tanSuatDongBo = "THU_CONG",
            trangThai = 1
        });

        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetString()!;

        var thu = await admin.PostAsync($"/api/v1/tich-hop/he-thong/{id}/thu-ket-noi", null);
        thu.EnsureSuccessStatusCode();

        var kq = (await thu.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        // Endpoint khong ton tai => bao that bai kem ly do, KHONG nem loi 500: day la ket qua
        // chan doan binh thuong, quan tri vien can doc duoc thong bao chu khong phai trang loi.
        kq.GetProperty("thanhCong").GetBoolean().Should().BeFalse();
        kq.GetProperty("thongBao").GetString().Should().NotBeNullOrWhiteSpace();
        kq.GetProperty("tenHeThong").GetString().Should().Be("Hệ thống kiểm thử kết nối");
    }

    private static async Task<List<JsonElement>> LayVaiTroAsync(HttpClient client)
        => (await LayDuLieuAsync(client, "/api/v1/he-thong/vai-tro"))
            .GetProperty("vaiTro").EnumerateArray().ToList();

    private static async Task<JsonElement> LayDuLieuAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }
}
