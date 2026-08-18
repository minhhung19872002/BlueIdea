using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using FluentAssertions;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem tra bao ve IDOR (Insecure Direct Object Reference) cho:
/// - REQ-25: tep tin (tai len, tai xuong, xem truoc, xoa)
/// - REQ-23: chi tiet ho so, tien do, lich su, trung lap
///
/// Moi kiem tra dung HAI nguoi dung thuoc HAI nhanh to chuc KHAC NHAU
/// de xac nhan pham vi du lieu duoc thuc thi o muc truy van.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class IdorBaoVeTests
{
    private readonly UngDungKiemThu _ungDung;

    public IdorBaoVeTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ── REQ-23: Chi tiet / Tien do / Lich su / Trung lap ────────────────

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Xem_Duoc_Chi_Tiet_Ho_So()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await orgB.GetAsync($"/api/v1/sang-kien/{hoSoId}");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc xem chi tiet ho so cua don vi khac");
    }

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Xem_Duoc_Tien_Do()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await orgB.GetAsync($"/api/v1/sang-kien/{hoSoId}/tien-do");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc xem tien do xu ly");
    }

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Xem_Duoc_Lich_Su()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await orgB.GetAsync($"/api/v1/sang-kien/{hoSoId}/lich-su");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc xem lich su chinh sua");
    }

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Xem_Duoc_Trung_Lap()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await orgB.GetAsync($"/api/v1/sang-kien/{hoSoId}/trung-lap");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc xem ket qua trung lap");
    }

    [Fact]
    public async Task Tac_Gia_Van_Xem_Duoc_Ho_So_Cua_Minh()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();

        var phanHoi = await orgA.GetAsync($"/api/v1/sang-kien/{hoSoId}");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK,
            "tac gia phai xem duoc ho so cua minh");
    }

    [Fact]
    public async Task Tac_Gia_Van_Xem_Duoc_Tien_Do_Cua_Minh()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();

        var phanHoi = await orgA.GetAsync($"/api/v1/sang-kien/{hoSoId}/tien-do");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK,
            "tac gia phai xem duoc tien do ho so cua minh");
    }

    [Fact]
    public async Task Tac_Gia_Van_Xem_Duoc_Lich_Su_Cua_Minh()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();

        var phanHoi = await orgA.GetAsync($"/api/v1/sang-kien/{hoSoId}/lich-su");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK,
            "tac gia phai xem duoc lich su ho so cua minh");
    }

    // ── REQ-25: Tep tin — Tai len / Tai xuong / Xem truoc / Xoa ────────

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Tai_Len_Duoc_Tep_Cho_Ho_So_Don_Vi_Khac()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await TaiLenTepAsync(orgB, hoSoId);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc tai tep len ho so cua don vi khac");
    }

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Tai_Xuong_Duoc_Tep_Cua_Don_Vi_Khac()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var tepTinId = await TaiLenVaLayIdTepAsync(orgA, hoSoId);
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await orgB.GetAsync($"/api/v1/tep-tin/{tepTinId}/tai-ve");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc tai xuong tep cua don vi khac");
    }

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Xem_Truoc_Duoc_Tep_Cua_Don_Vi_Khac()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var tepTinId = await TaiLenVaLayIdTepAsync(orgA, hoSoId);
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await orgB.GetAsync($"/api/v1/tep-tin/{tepTinId}/xem-truoc");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc xem truoc tep cua don vi khac");
    }

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Lay_Duoc_Lien_Ket_Tai_Xuong_Cua_Don_Vi_Khac()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var tepTinId = await TaiLenVaLayIdTepAsync(orgA, hoSoId);
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await orgB.GetAsync($"/api/v1/tep-tin/{tepTinId}/lien-ket-tai-xuong");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc lay lien ket tai xuong");
    }

    [Fact]
    public async Task Nguoi_Don_Vi_Khac_Khong_Xoa_Duoc_Tep_Cua_Don_Vi_Khac()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var dinhKemId = await TaiLenVaLayIdDinhKemAsync(orgA, hoSoId);
        var orgB = await _ungDung.TaoClientDaDangNhapAsync("bs.tuan");

        var phanHoi = await orgB.DeleteAsync($"/api/v1/tep-tin/dinh-kem/{dinhKemId}");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "nguoi dung don vi khac khong duoc xoa tep dinh kem cua don vi khac");
    }

    [Fact]
    public async Task Tac_Gia_Tai_Len_Va_Tai_Xuong_Duoc_Tep_Cua_Minh()
    {
        var (orgA, hoSoId) = await TaoHoSoCuaOrgAAsync();
        var tepTinId = await TaiLenVaLayIdTepAsync(orgA, hoSoId);

        var phanHoi = await orgA.GetAsync($"/api/v1/tep-tin/{tepTinId}/tai-ve");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK,
            "tac gia phai tai xuong duoc tep cua minh");
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<(HttpClient client, string hoSoId)> TaoHoSoCuaOrgAAsync()
    {
        var orgA = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var dot = (await LayDuLieuAsync(orgA, "/api/v1/danh-muc/dot-de-nghi/dang-mo"))[0];
        var linhVuc = (await LayDuLieuAsync(orgA, "/api/v1/danh-muc/linh-vuc/chon"))[0];
        var loaiTacGia = (await LayDuLieuAsync(orgA, "/api/v1/danh-muc/loai-tac-gia/chon"))[0];

        var phanHoi = await orgA.PostAsJsonAsync("/api/v1/sang-kien", new
        {
            tenSangKien = $"IDOR test {Guid.NewGuid():N}",
            dotDeNghiId = dot.GetProperty("id").GetString()!,
            linhVucId = linhVuc.GetProperty("id").GetString()!,
            loaiTacGiaId = loaiTacGia.GetProperty("id").GetString()!,
            moTaGiaiPhap = string.Concat(Enumerable.Repeat(
                "Mô tả chi tiết giải pháp kiểm thử IDOR cho hệ thống sáng kiến. ", 8)),
            tinhTrangTruocKhiApDung = string.Concat(Enumerable.Repeat(
                "Trước khi áp dụng phải thao tác thủ công rất mất thời gian. ", 4)),
            noiDungGiaiPhap = string.Concat(Enumerable.Repeat(
                "Nội dung chi tiết của giải pháp bảo mật được trình bày theo từng bước. ", 10)),
            tinhMoi = string.Concat(Enumerable.Repeat(
                "Tính mới của giải pháp so với cách làm cũ tại đơn vị. ", 4)),
            khaNangApDung = string.Concat(Enumerable.Repeat(
                "Khả năng áp dụng rộng rãi cho các đơn vị tương tự. ", 4)),
            danhSachTacGia = new[]
            {
                new { hoTen = "Nguyễn Thị Lan", tyLeDongGop = 100, laTacGiaChinh = true }
            }
        });

        phanHoi.EnsureSuccessStatusCode();
        var hoSoId = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetString()!;

        return (orgA, hoSoId);
    }

    private static async Task<HttpResponseMessage> TaiLenTepAsync(
        HttpClient client, string hoSoId)
    {
        var noiDungPdf = "%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF"u8
            .ToArray();

        using var form = new MultipartFormDataContent();
        var tep = new ByteArrayContent(noiDungPdf);
        tep.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        form.Add(tep, "tep", "idor-test.pdf");
        form.Add(new StringContent(hoSoId), "sangKienId");
        form.Add(new StringContent("MINH_CHUNG"), "thanhPhanHoSoMa");

        return await client.PostAsync("/api/v1/tep-tin/tai-len", form);
    }

    private static async Task<string> TaiLenVaLayIdTepAsync(
        HttpClient client, string hoSoId)
    {
        var phanHoi = await TaiLenTepAsync(client, hoSoId);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;
    }

    private static async Task<string> TaiLenVaLayIdDinhKemAsync(
        HttpClient client, string hoSoId)
    {
        var phanHoi = await TaiLenTepAsync(client, hoSoId);
        phanHoi.EnsureSuccessStatusCode();

        var chiTiet = await LayMotDuLieuAsync(client, $"/api/v1/sang-kien/{hoSoId}");
        var tepDinhKem = chiTiet.GetProperty("tepDinhKem").EnumerateArray().ToList();

        return tepDinhKem.Last().GetProperty("id").GetString()!;
    }

    private static async Task<List<JsonElement>> LayDuLieuAsync(
        HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu").EnumerateArray().ToList();
    }

    private static async Task<JsonElement> LayMotDuLieuAsync(
        HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu");
    }
}
