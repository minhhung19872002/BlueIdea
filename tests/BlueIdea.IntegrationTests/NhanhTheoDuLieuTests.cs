using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Ba nhanh quy trinh mo theo DU LIEU, khong theo lua chon cua nguoi xu ly.
///
/// Dieu quan trong nhat o day khong phai "nhanh co mo duoc khong" ma la "nhanh co bi CHAN dung khi
/// khong duoc phep khong". Chan sai chieu la loi nang nhat cua ca he thong: hoi dong cho 30 diem ma
/// he thong van mo nhanh "de nghi cong nhan" thi mot sang kien khong dat duoc cong nhan, va khong
/// co gi tren man hinh bao rang co chuyen bat thuong.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class NhanhTheoDuLieuTests
{
    private readonly UngDungKiemThu _ungDung;

    public NhanhTheoDuLieuTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    /// <summary>
    /// Ho so trung lap qua nguong thi buoc tham dinh phai mo nhanh loai ho so.
    /// Dieu kien trong quy trinh mau: <c>ty_le_trung_lap &gt; 40</c>.
    /// </summary>
    [Fact]
    public async Task Trung_Lap_Qua_Nguong_Thi_Mo_Nhanh_Loai_O_Tham_Dinh()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var tiepNhan = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");
        var thuKy = await _ungDung.TaoClientDaDangNhapAsync("thuky");

        // Hai ho so cung mot noi dung — bo phan tich phai nhan ra.
        var vanBan = Lap(
            "Giải pháp ứng dụng công nghệ thông tin để rút ngắn thời gian xử lý hồ sơ hành chính "
            + "tại bộ phận một cửa, thay thế hoàn toàn việc ghi sổ tay bằng phần mềm tập trung. ", 6);

        await NopHoSoAsync(tacGia, vanBan);
        var saoChep = await NopHoSoAsync(tacGia, vanBan);

        (await thuKy.PostAsync($"/api/v1/sang-kien/{saoChep}/trung-lap/chay-lai", null))
            .EnsureSuccessStatusCode();

        var tyLe = (await LayHoSoAsync(thuKy, saoChep)).GetProperty("tyLeTrungLap").GetDecimal();

        tyLe.Should().BeGreaterThan(40m,
            "hai hồ sơ có nội dung y nguyên nhau thì bộ phân tích phải nhận ra");

        await ThucThiAsync(tiepNhan, saoChep, "DAT", "Tiếp nhận để thẩm định.");

        var khongDat = await LayNhanhAsync(thuKy, saoChep, "KHONG_DAT");

        khongDat.GetProperty("biChan").GetBoolean().Should().BeFalse(
            $"tỷ lệ trùng lặp {tyLe}% vượt ngưỡng 40% nên thẩm định phải loại được hồ sơ");

        var kq = await ThucThiAsync(thuKy, saoChep, "KHONG_DAT", "Trùng lặp vượt ngưỡng.");

        kq.GetProperty("daKetThucQuyTrinh").GetBoolean().Should().BeTrue();

        (await LayHoSoAsync(thuKy, saoChep)).GetProperty("trangThaiTong").GetString()
            .Should().Be("KHONG_DAT");
    }

    /// <summary>
    /// Diem thap thi nhanh "de nghi cong nhan" PHAI bi chan.
    ///
    /// Day la phep kiem quan trong nhat cua ca bo: no chan viec mot sang kien khong dat duoc cong
    /// nhan. Neu dieu kien <c>tong_diem &gt;= 50</c> hong, khong co gi khac trong he thong bat lai.
    /// </summary>
    [Fact]
    public async Task Diem_Thap_Thi_Chan_Nhanh_Cong_Nhan_Va_Mo_Nhanh_Khong_Dat()
    {
        var (hoSoId, chuTich, tongDiem) = await ChamDenBuocHopAsync(tyLeDiem: 0.30m);

        tongDiem.Should().BeLessThan(50m, "chấm 30% điểm tối đa thì tổng phải dưới ngưỡng đạt");

        var nhanhDat = await LayNhanhAsync(chuTich, hoSoId, "DAT");
        nhanhDat.GetProperty("biChan").GetBoolean().Should().BeTrue(
            $"tổng điểm {tongDiem} dưới 50 nên KHÔNG được mở nhánh đề nghị công nhận");

        var nhanhKhongDat = await LayNhanhAsync(chuTich, hoSoId, "KHONG_DAT");
        nhanhKhongDat.GetProperty("biChan").GetBoolean().Should().BeFalse(
            "hội đồng phải kết luận được không đạt");

        await ThucThiAsync(chuTich, hoSoId, "KHONG_DAT", "Hội đồng kết luận không đạt.");

        var hoSo = await LayHoSoAsync(chuTich, hoSoId);
        hoSo.GetProperty("trangThaiTong").GetString().Should().Be("KHONG_DAT");
        hoSo.GetProperty("ketQua").GetString().Should().Be("KHONG_DAT");
    }

    /// <summary>
    /// Diem cao mo nhanh de nghi xet cap cao hon, va dong nhanh khong dat.
    /// Dieu kien trong quy trinh mau: <c>tong_diem &gt;= 80</c>.
    /// </summary>
    [Fact]
    public async Task Diem_Cao_Thi_Mo_Nhanh_Chuyen_Cap_Va_Chan_Nhanh_Khong_Dat()
    {
        var (hoSoId, chuTich, tongDiem) = await ChamDenBuocHopAsync(tyLeDiem: 0.95m);

        tongDiem.Should().BeGreaterThanOrEqualTo(80m);

        (await LayNhanhAsync(chuTich, hoSoId, "CHUYEN_CAP_CAO_HON"))
            .GetProperty("biChan").GetBoolean().Should().BeFalse(
                $"tổng điểm {tongDiem} đạt ngưỡng 80 nên phải đề nghị xét cấp cao hơn được");

        (await LayNhanhAsync(chuTich, hoSoId, "KHONG_DAT"))
            .GetProperty("biChan").GetBoolean().Should().BeTrue(
                "điểm cao thì không được mở nhánh không đạt");

        var kq = await ThucThiAsync(chuTich, hoSoId, "CHUYEN_CAP_CAO_HON", "Đề nghị xét cấp thành phố.");

        kq.GetProperty("tenBuocMoi").GetString().Should().NotBeNullOrWhiteSpace();
    }

    // ---------------------------------------------------------------------------------

    /// <summary>Nop ho so, dua den buoc hop hoi dong, cham diem theo ty le chi dinh.</summary>
    private async Task<(string HoSoId, HttpClient ChuTich, decimal TongDiem)> ChamDenBuocHopAsync(
        decimal tyLeDiem)
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var tiepNhan = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");
        var thuKy = await _ungDung.TaoClientDaDangNhapAsync("thuky");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");

        var tenCham = new[] { "hoidong01", "hoidong02", "hoidong03" };
        var thanhVien = new List<HttpClient>();
        var idNguoiCham = new List<string>();

        foreach (var ten in tenCham)
        {
            var client = await _ungDung.TaoClientDaDangNhapAsync(ten);
            thanhVien.Add(client);

            var toi = await client.GetAsync("/api/v1/xac-thuc/toi");
            toi.EnsureSuccessStatusCode();

            idNguoiCham.Add((await toi.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("duLieu").GetProperty("id").GetString()!);
        }

        var hoSoId = await NopHoSoAsync(tacGia);

        await ThucThiAsync(tiepNhan, hoSoId, "DAT", "Tiếp nhận.");
        await ThucThiAsync(thuKy, hoSoId, "DAT", "Đạt thẩm định.");

        var hoiDong = await LayHoiDongAsync(thuKy);
        var hoiDongId = hoiDong.GetProperty("id").GetString()!;

        var thanhVienIds = hoiDong.GetProperty("thanhVien").EnumerateArray()
            .Where(x => x.TryGetProperty("nguoiDungId", out var nd)
                        && nd.ValueKind == JsonValueKind.String
                        && idNguoiCham.Contains(nd.GetString()!))
            .Select(x => x.GetProperty("id").GetString())
            .ToList();

        thanhVienIds.Should().HaveCount(3, "phải xác định đúng ba thành viên sẽ chấm");

        (await thuKy.PostAsJsonAsync("/api/v1/danh-gia/phan-cong", new
        {
            hoiDongId,
            sangKienIds = new[] { hoSoId },
            thanhVienIds,
            hanHoanThanh = DateTimeOffset.UtcNow.AddDays(7),
            tuDongChiaDeu = false
        })).EnsureSuccessStatusCode();

        await ThucThiAsync(thuKy, hoSoId, "DAT", "Đã phân công.");

        foreach (var tv in thanhVien)
        {
            var phieu = await tv.GetAsync(
                $"/api/v1/danh-gia/phieu?sangKienId={hoSoId}&hoiDongId={hoiDongId}");
            phieu.EnsureSuccessStatusCode();

            var boTieuChi = (await phieu.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("duLieu").GetProperty("boTieuChi");

            var chiTiet = boTieuChi.GetProperty("danhSachNhom").EnumerateArray()
                .SelectMany(n => n.GetProperty("danhSachTieuChi").EnumerateArray())
                .Select(tc => new
                {
                    tieuChiId = tc.GetProperty("id").GetString(),
                    diem = Math.Round(tc.GetProperty("diemToiDa").GetDecimal() * tyLeDiem, 1)
                })
                .ToList();

            (await tv.PostAsJsonAsync("/api/v1/danh-gia/phieu/gui", new
            {
                sangKienId = hoSoId,
                hoiDongId,
                chiTiet,
                nhanXetChung = "Kiểm thử tự động."
            })).EnsureSuccessStatusCode();
        }

        var tongHop = await thuKy.PostAsync(
            $"/api/v1/danh-gia/tong-hop?sangKienId={hoSoId}&hoiDongId={hoiDongId}", null);
        tongHop.EnsureSuccessStatusCode();

        var tongDiem = (await tongHop.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("diemCuoiCung").GetDecimal();

        // Quy tac TAT_CA: du ba thanh vien xac nhan thi moi chuyen sang buoc hop.
        foreach (var tv in thanhVien)
        {
            await ThucThiAsync(tv, hoSoId, "DAT", "Đã chấm xong.");
        }

        return (hoSoId, chuTich, tongDiem);
    }

    private async Task<string> NopHoSoAsync(HttpClient tacGia, string? noiDungRieng = null)
    {
        var dot = await LayMotIdAsync(tacGia, "/api/v1/danh-muc/dot-de-nghi/dang-mo");
        var linhVuc = await LayMotIdAsync(tacGia, "/api/v1/danh-muc/linh-vuc/chon");

        var noiDung = noiDungRieng ?? Lap("Nội dung chi tiết của giải pháp theo từng bước. ", 10);

        var tao = await tacGia.PostAsJsonAsync("/api/v1/sang-kien", new
        {
            tenSangKien = $"Kiểm thử nhánh dữ liệu {Guid.NewGuid():N}",
            dotDeNghiId = dot,
            linhVucId = linhVuc,
            moTaGiaiPhap = noiDung,
            tinhTrangTruocKhiApDung = Lap("Trước khi áp dụng phải làm thủ công. ", 4),
            noiDungGiaiPhap = noiDung,
            tinhMoi = Lap("Tính mới so với cách làm cũ tại đơn vị. ", 4),
            khaNangApDung = Lap("Khả năng áp dụng rộng cho đơn vị tương tự. ", 4),
            danhSachTacGia = new[] { new { hoTen = "Nguyễn Thị Lan", tyLeDongGop = 100, laTacGiaChinh = true } }
        });

        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu").GetString()!;

        await TaiMinhChungAsync(tacGia, id);
        (await tacGia.PostAsync($"/api/v1/sang-kien/{id}/nop", null)).EnsureSuccessStatusCode();

        return id;
    }

    private static string Lap(string doan, int lan) => string.Concat(Enumerable.Repeat(doan, lan));

    private static async Task TaiMinhChungAsync(HttpClient client, string hoSoId)
    {
        var pdf = "%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF"u8.ToArray();

        using var form = new MultipartFormDataContent();
        var tep = new ByteArrayContent(pdf);
        tep.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        form.Add(tep, "tep", "minh-chung.pdf");
        form.Add(new StringContent(hoSoId), "sangKienId");
        form.Add(new StringContent("MINH_CHUNG"), "thanhPhanHoSoMa");

        (await client.PostAsync("/api/v1/tep-tin/tai-len", form)).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> LayNhanhAsync(HttpClient client, string hoSoId, string ma)
    {
        var phanHoi = await client.GetAsync($"/api/v1/sang-kien/{hoSoId}/hanh-dong");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .First(x => x.GetProperty("ma").GetString() == ma);
    }

    private static async Task<JsonElement> ThucThiAsync(
        HttpClient client, string hoSoId, string ma, string yKien)
    {
        var nhanh = await LayNhanhAsync(client, hoSoId, ma);

        var thucThi = await client.PostAsJsonAsync("/api/v1/xu-ly/thuc-thi", new
        {
            sangKienId = hoSoId,
            truongHopId = nhanh.GetProperty("truongHopId").GetString(),
            yKien
        });

        thucThi.EnsureSuccessStatusCode();

        return (await thucThi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<JsonElement> LayHoSoAsync(HttpClient client, string hoSoId)
    {
        var phanHoi = await client.GetAsync($"/api/v1/sang-kien/{hoSoId}");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<JsonElement> LayHoiDongAsync(HttpClient client)
    {
        var ds = await client.GetAsync("/api/v1/hoi-dong?trang=1&soDong=1");
        ds.EnsureSuccessStatusCode();

        var id = (await ds.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().First().GetProperty("id").GetString()!;

        var chiTiet = await client.GetAsync($"/api/v1/hoi-dong/{id}");
        chiTiet.EnsureSuccessStatusCode();

        return (await chiTiet.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<string> LayMotIdAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().First().GetProperty("id").GetString()!;
    }
}
