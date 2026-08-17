using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop cho chuc nang 37 — cong tra cuu cong khai.
///
/// Diem chinh cua ca lop nay: MOI yeu cau deu goi bang <c>CreateClient()</c> tran, KHONG gan
/// header Authorization. Loi truoc day khong lo ra vi khi thu bang trinh duyet thi token van
/// con trong localStorage tu phien dang nhap truoc, nen trang chay binh thuong voi nguoi phat
/// trien nhung tra 401 cho nguoi dan.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class CongKhaiTests
{
    private readonly UngDungKiemThu _ungDung;

    public CongKhaiTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Theory]
    [InlineData("/api/v1/cong-khai/sang-kien")]
    [InlineData("/api/v1/cong-khai/thong-ke")]
    [InlineData("/api/v1/cong-khai/linh-vuc")]
    public async Task Nguoi_Chua_Dang_Nhap_Truy_Cap_Duoc(string duongDan)
    {
        var khach = _ungDung.CreateClient();

        var phanHoi = await khach.GetAsync(duongDan);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK,
            "cong tra cuu danh cho nguoi dan nen khong duoc doi dang nhap");
    }

    [Fact]
    public async Task Chi_Tra_Ve_Sang_Kien_Cong_Khai_Va_Dat()
    {
        var khach = _ungDung.CreateClient();
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var congKhai = await LayMangAsync(khach, "/api/v1/cong-khai/sang-kien?soDong=100");
        var maCongKhai = congKhai.Select(x => x.GetProperty("maHoSo").GetString()).ToHashSet();

        // Doi chieu voi toan bo ho so ma quan tri vien nhin thay: moi ma xuat hien o cong cong
        // khai deu phai la ho so co co cong khai va ket qua Dat.
        var tatCa = await LayMangAsync(admin, "/api/v1/sang-kien?soDong=200");

        foreach (var hoSo in tatCa)
        {
            var ma = hoSo.GetProperty("maHoSo").GetString()!;

            if (!maCongKhai.Contains(ma))
            {
                continue;
            }

            hoSo.GetProperty("ketQua").GetString().Should().Be("DAT",
                $"ho so {ma} hien tren cong cong khai thi phai la sang kien da duoc cong nhan");
        }
    }

    [Fact]
    public async Task Khong_Lo_Truong_Noi_Bo()
    {
        var khach = _ungDung.CreateClient();

        var duLieu = await LayMangAsync(khach, "/api/v1/cong-khai/sang-kien?soDong=100");

        // Diem so, ty le trung lap va trang thai xu ly la thong tin noi bo. Neu ai do noi rong
        // DTO cong khai bang cach tai su dung DTO noi bo, kiem thu nay phai do ngay.
        string[] truongCam = ["tongDiem", "tyLeTrungLap", "trangThaiTong", "trangThaiHienTai", "hanXuLyHienTai"];

        foreach (var dong in duLieu)
        {
            foreach (var truong in truongCam)
            {
                dong.TryGetProperty(truong, out _).Should().BeFalse(
                    $"truong noi bo '{truong}' khong duoc lot ra cong cong khai");
            }
        }
    }

    [Fact]
    public async Task So_Tren_Dai_Thong_Ke_Khop_Voi_So_Dong_Liet_Ke_Duoc()
    {
        var khach = _ungDung.CreateClient();

        var thongKe = await LayDuLieuAsync(khach, "/api/v1/cong-khai/thong-ke");
        var soSangKien = thongKe.GetProperty("soSangKien").GetInt32();

        var phanHoi = await khach.GetAsync("/api/v1/cong-khai/sang-kien?soDong=100");
        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();

        // Neu hai truy van dung dieu kien loc khac nhau thi nguoi dan se thay "N sang kien
        // cong nhan" o dai thong ke nhung dem duoc mot so khac ben duoi.
        noiDung.GetProperty("tongSo").GetInt32().Should().Be(soSangKien);
    }

    [Fact]
    public async Task Chip_Linh_Vuc_Cong_Lai_Bang_Tong_So()
    {
        var khach = _ungDung.CreateClient();

        var linhVuc = await LayMangAsync(khach, "/api/v1/cong-khai/linh-vuc");
        var tongTheoLinhVuc = linhVuc.Sum(x => x.GetProperty("soLuong").GetInt32());

        var thongKe = await LayDuLieuAsync(khach, "/api/v1/cong-khai/thong-ke");

        tongTheoLinhVuc.Should().Be(thongKe.GetProperty("soSangKien").GetInt32());
    }

    [Fact]
    public async Task Loc_Theo_Linh_Vuc_Tra_Dung_So_Luong_Ghi_Tren_Chip()
    {
        var khach = _ungDung.CreateClient();

        var linhVuc = await LayMangAsync(khach, "/api/v1/cong-khai/linh-vuc");

        if (linhVuc.Count == 0)
        {
            return;
        }

        var dau = linhVuc[0];
        var id = dau.GetProperty("id").GetString();
        var soLuong = dau.GetProperty("soLuong").GetInt32();

        var phanHoi = await khach.GetAsync($"/api/v1/cong-khai/sang-kien?linhVucId={id}&soDong=100");
        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();

        noiDung.GetProperty("tongSo").GetInt32().Should().Be(soLuong);
    }

    [Fact]
    public async Task Tim_Khong_Dau_Van_Ra_Ket_Qua_Co_Dau()
    {
        var khach = _ungDung.CreateClient();

        var tatCa = await LayMangAsync(khach, "/api/v1/cong-khai/sang-kien?soDong=100");

        if (tatCa.Count == 0)
        {
            return;
        }

        // Lay mot tu that su co dau trong du lieu mau roi go lai khong dau.
        var ten = tatCa[0].GetProperty("tenSangKien").GetString()!;
        var ma = tatCa[0].GetProperty("maHoSo").GetString()!;

        var khongDau = BlueIdea.Shared.TiengViet.VanBanTiengViet.TaoKhongDau(ten);

        var phanHoi = await khach.GetAsync(
            $"/api/v1/cong-khai/sang-kien?tuKhoa={Uri.EscapeDataString(khongDau)}&soDong=100");

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();

        noiDung.GetProperty("duLieu").EnumerateArray()
            .Select(x => x.GetProperty("maHoSo").GetString())
            .Should().Contain(ma, "go khong dau la cach nguoi dan thuong nhap nhat");
    }

    [Fact]
    public async Task Chan_So_Dong_Qua_Lon()
    {
        var khach = _ungDung.CreateClient();

        var phanHoi = await khach.GetAsync("/api/v1/cong-khai/sang-kien?soDong=100000");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();

        // Endpoint mo cho ca Internet: khong chan thi mot yeu cau du de keo sap CSDL.
        noiDung.GetProperty("soDong").GetInt32().Should().BeLessThanOrEqualTo(100);
    }

    // ------------------------------------------------------------------------------

    private static async Task<JsonElement> LayDuLieuAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu").Clone();
    }

    private static async Task<List<JsonElement>> LayMangAsync(HttpClient client, string duongDan)
    {
        var duLieu = await LayDuLieuAsync(client, duongDan);
        return duLieu.EnumerateArray().Select(x => x.Clone()).ToList();
    }
}
