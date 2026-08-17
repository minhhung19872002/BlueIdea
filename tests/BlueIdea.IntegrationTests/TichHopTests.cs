using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop cho chuc nang 16, 41 - lien thong sang he thong ngoai.
///
/// Dung mot may chu HTTP that chay cuc bo dong vai he thong Thi dua khen thuong / IOC:
/// kiem duoc CA than yeu cau gui di LAN cach doc phan hoi, ma khong phu thuoc he thong that.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class TichHopTests
{
    private readonly UngDungKiemThu _ungDung;

    public TichHopTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Xem_Truoc_Chi_Lay_Sang_Kien_Da_Cong_Bo()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var duLieu = await LayMangAsync(admin, "/api/v1/tich-hop/xem-truoc");

        // Du lieu mau co san sang kien da cong bo tu cac kiem thu truoc.
        foreach (var x in duLieu)
        {
            x.GetProperty("maHoSo").GetString().Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Dong_Bo_Thanh_Cong_Va_Ghi_Nhat_Ky()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        await using var heThongNgoai = MayChuNhanGia.Tao(HttpStatusCode.OK, """{"soNhan": 999}""");

        var heThongId = await TaoHeThongAsync(admin, heThongNgoai.DiaChi, "API_KEY", "khoa-bi-mat");

        var phanHoi = await admin.PostAsync($"/api/v1/tich-hop/he-thong/{heThongId}/dong-bo", null);
        phanHoi.EnsureSuccessStatusCode();

        var ketQua = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        ketQua.GetProperty("trangThai").GetString().Should().Be("THANH_CONG");

        // He thong ngoai phai nhan duoc than yeu cau dung dinh dang.
        heThongNgoai.SoLanNhan.Should().Be(1);

        using var than = JsonDocument.Parse(heThongNgoai.ThanCuoi!);
        than.RootElement.GetProperty("nguon").GetString().Should().Be("BLUEIDEA");
        than.RootElement.GetProperty("loaiDuLieu").GetString().Should().Be("SANG_KIEN_DUOC_CONG_NHAN");
        than.RootElement.GetProperty("duLieu").ValueKind.Should().Be(JsonValueKind.Array);

        // Khoa API phai nam o header Authorization, khong nam trong than.
        heThongNgoai.HeaderCuoi.Should().ContainKey("Authorization");
        heThongNgoai.ThanCuoi.Should().NotContain("khoa-bi-mat");

        // Nhat ky dong bo phai co ban ghi moi.
        var nhatKy = await LayMangAsync(
            admin, $"/api/v1/tich-hop/nhat-ky-dong-bo?heThongId={heThongId}");

        nhatKy.Should().NotBeEmpty();
        nhatKy[0].GetProperty("trangThaiDongBo").GetString().Should().Be("THANH_CONG");
        nhatKy[0].GetProperty("chieu").GetString().Should().Be("GUI");
    }

    [Fact]
    public async Task He_Thong_Ngoai_Loi_Thi_Ghi_That_Bai_Chu_Khong_Nem_Ngoai_Le()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        await using var heThongNgoai = MayChuNhanGia.Tao(
            HttpStatusCode.InternalServerError, "he thong dang bao tri");

        var heThongId = await TaoHeThongAsync(admin, heThongNgoai.DiaChi, "API_KEY", "khoa");

        var phanHoi = await admin.PostAsync($"/api/v1/tich-hop/he-thong/{heThongId}/dong-bo", null);

        // Loi cua he thong NGOAI khong duoc lam hong thao tac cua nguoi dung — phai bao cao lai,
        // khong phai nem 500.
        phanHoi.EnsureSuccessStatusCode();

        var ketQua = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        ketQua.GetProperty("trangThai").GetString().Should().Be("THAT_BAI");
        ketQua.GetProperty("thongBaoLoi").GetString().Should().Contain("500");
    }

    [Fact]
    public async Task Xac_Thuc_HMAC_Ky_Than_Va_Khong_Gui_Khoa_Qua_Mang()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        await using var heThongNgoai = MayChuNhanGia.Tao(HttpStatusCode.OK, "{}");

        const string biMat = "khoa-hmac-tuyet-doi-khong-duoc-lo";
        var heThongId = await TaoHeThongAsync(admin, heThongNgoai.DiaChi, "HMAC", biMat);

        var phanHoi = await admin.PostAsync($"/api/v1/tich-hop/he-thong/{heThongId}/dong-bo", null);
        phanHoi.EnsureSuccessStatusCode();

        heThongNgoai.HeaderCuoi.Should().ContainKey("X-Signature");
        heThongNgoai.HeaderCuoi["X-Signature"].Should().MatchRegex("^[0-9a-f]{64}$");

        // Điểm quan trọng nhất của HMAC: khoá bí mật KHÔNG đi qua mạng.
        heThongNgoai.ThanCuoi.Should().NotContain(biMat);
        string.Join(" ", heThongNgoai.HeaderCuoi.Values).Should().NotContain(biMat);
    }

    [Fact]
    public async Task Bi_Mat_Khong_Bao_Gio_Tra_Ve_Giao_Dien()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        const string biMat = "bi-mat-khong-duoc-tra-ve";
        await TaoHeThongAsync(admin, "https://khong-ton-tai.local/api", "API_KEY", biMat);

        var phanHoi = await admin.GetAsync("/api/v1/tich-hop/he-thong");
        phanHoi.EnsureSuccessStatusCode();

        var chuoi = await phanHoi.Content.ReadAsStringAsync();
        chuoi.Should().NotContain(biMat);

        var ds = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        ds.EnumerateArray().Should().Contain(x => x.GetProperty("daDatBiMat").GetBoolean());
    }

    [Fact]
    public async Task Endpoint_Khong_Phai_Http_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/tich-hop/he-thong", new
        {
            ma = $"KT_{Guid.NewGuid():N}"[..12],
            ten = "Hệ thống endpoint sai",
            endpointBase = "file:///etc/passwd",
            loaiXacThuc = "API_KEY",
            tanSuatDongBo = "THU_CONG",
            trangThai = 1
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("thongBao").GetString().Should().Contain("http");
    }

    [Fact]
    public async Task Tac_Gia_Khong_Duoc_Dong_Bo()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.GetAsync("/api/v1/tich-hop/he-thong");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------------------------

    private static async Task<string> TaoHeThongAsync(
        HttpClient client, string endpoint, string loaiXacThuc, string biMat)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/tich-hop/he-thong", new
        {
            ma = $"KT_{Guid.NewGuid():N}"[..12],
            ten = "Hệ thống kiểm thử",
            endpointBase = endpoint,
            loaiXacThuc,
            clientId = "blueidea",
            clientSecret = biMat,
            tanSuatDongBo = "THU_CONG",
            trangThai = 1
        });

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetString()!;
    }

    private static async Task<List<JsonElement>> LayMangAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();
    }

    /// <summary>Máy chủ HTTP thật chạy cục bộ, đóng vai hệ thống ngoài.</summary>
    private sealed class MayChuNhanGia : IAsyncDisposable
    {
        private readonly HttpListener _lang;
        private readonly Task _phucVu;

        private MayChuNhanGia(HttpListener lang, HttpStatusCode ma, string traLoi, string diaChi)
        {
            _lang = lang;
            DiaChi = diaChi;
            _phucVu = PhucVuAsync(ma, traLoi);
        }

        public string DiaChi { get; }

        public int SoLanNhan { get; private set; }

        public string? ThanCuoi { get; private set; }

        public Dictionary<string, string> HeaderCuoi { get; } = new(StringComparer.OrdinalIgnoreCase);

        public static MayChuNhanGia Tao(HttpStatusCode ma, string traLoi)
        {
            // Xin hệ điều hành cấp một cổng trống.
            var nghe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            nghe.Start();
            var cong = ((IPEndPoint)nghe.LocalEndpoint).Port;
            nghe.Stop();

            var lang = new HttpListener();
            lang.Prefixes.Add($"http://127.0.0.1:{cong}/");
            lang.Start();

            return new MayChuNhanGia(lang, ma, traLoi, $"http://127.0.0.1:{cong}/nhan");
        }

        private async Task PhucVuAsync(HttpStatusCode ma, string traLoi)
        {
            try
            {
                var nguCanh = await _lang.GetContextAsync();

                SoLanNhan++;

                foreach (var ten in nguCanh.Request.Headers.AllKeys)
                {
                    if (ten is not null)
                    {
                        HeaderCuoi[ten] = nguCanh.Request.Headers[ten] ?? string.Empty;
                    }
                }

                using (var doc = new StreamReader(nguCanh.Request.InputStream, Encoding.UTF8))
                {
                    ThanCuoi = await doc.ReadToEndAsync();
                }

                var byteTraLoi = Encoding.UTF8.GetBytes(traLoi);
                nguCanh.Response.StatusCode = (int)ma;
                nguCanh.Response.ContentType = "application/json";
                await nguCanh.Response.OutputStream.WriteAsync(byteTraLoi);
                nguCanh.Response.Close();
            }
            catch (HttpListenerException)
            {
                // Máy chủ bị đóng trước khi có yêu cầu — bình thường khi kiểm thử kết thúc sớm.
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Chờ xử lý xong yêu cầu rồi mới đóng, để assert đọc được thân yêu cầu.
            await Task.WhenAny(_phucVu, Task.Delay(TimeSpan.FromSeconds(10)));

            _lang.Stop();
            _lang.Close();
        }
    }
}
