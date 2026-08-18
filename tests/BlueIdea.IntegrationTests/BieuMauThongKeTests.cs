using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu chuc nang 7 — Bieu mau thong ke: vong doi day du them/sua/xoa qua dung cac endpoint
/// ma man hinh quan tri goi, va bieu mau vua tao phai chay duoc o bao cao tuy bien.
///
/// Truoc day chi co API, khong co man hinh nao tao hay sua bieu mau, nen phan nay chua he duoc
/// kiem thu qua duong nguoi dung that su di.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class BieuMauThongKeTests
{
    private const string Goc = "/api/v1/danh-muc/bieu-mau-thong-ke";

    private readonly UngDungKiemThu _ungDung;

    public BieuMauThongKeTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Tao_Sua_Xoa_Bieu_Mau_Thong_Ke()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ma = $"BMTK_KT_{Guid.NewGuid():N}"[..24].ToUpperInvariant();

        // --- Them ----------------------------------------------------------------
        var tao = await admin.PostAsJsonAsync(Goc, new
        {
            ma,
            ten = "Biểu mẫu kiểm thử tự động",
            moTa = "Tạo bởi kiểm thử tích hợp",
            thuTu = 0,
            trangThai = 1,
            cauHinhCot = new[]
            {
                new { ma = "maHoSo", tieuDe = "Mã hồ sơ", nguon = "maHoSo", hamTongHop = (string?)null, thuTu = 1 },
                new { ma = "tenDonVi", tieuDe = "Đơn vị", nguon = "tenDonVi", hamTongHop = (string?)null, thuTu = 2 }
            },
            dinhDangXuat = new[] { "XLSX" }
        });

        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        // --- Doc lai -------------------------------------------------------------
        var chiTiet = await LayDuLieuAsync(admin, $"{Goc}/{id}");
        chiTiet.GetProperty("cauHinhCot").GetArrayLength().Should().Be(2);

        // --- Chay bao cao tu bieu mau vua tao -------------------------------------
        var chay = await admin.GetAsync($"/api/v1/nhap-xuat/bao-cao-tuy-bien/{id}");
        chay.EnsureSuccessStatusCode();

        var ketQua = (await chay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        // Bao cao phai chay dung bo cot vua khai, dung thu tu.
        ketQua.GetProperty("tieuDeCot").EnumerateArray()
            .Select(x => x.GetString())
            .Should().Equal(new[] { "Mã hồ sơ", "Đơn vị" });

        // --- Sua -----------------------------------------------------------------
        var sua = await admin.PutAsJsonAsync($"{Goc}/{id}", new
        {
            ma,
            ten = "Biểu mẫu kiểm thử đã sửa",
            moTa = (string?)null,
            thuTu = 5,
            trangThai = 1,
            cauHinhCot = new[]
            {
                new { ma = "maHoSo", tieuDe = "Mã hồ sơ", nguon = "maHoSo", hamTongHop = (string?)null, thuTu = 1 }
            },
            dinhDangXuat = new[] { "XLSX", "PDF" }
        });

        sua.EnsureSuccessStatusCode();
        (await LayDuLieuAsync(admin, $"{Goc}/{id}"))
            .GetProperty("cauHinhCot").GetArrayLength().Should().Be(1);

        // --- Xoa -----------------------------------------------------------------
        (await admin.DeleteAsync($"{Goc}/{id}")).EnsureSuccessStatusCode();

        var sauXoa = await admin.GetAsync($"{Goc}/{id}");
        sauXoa.IsSuccessStatusCode.Should().BeFalse("biểu mẫu đã xoá thì không đọc lại được");
    }

    /// <summary>
    /// O "Dinh dang cho phep xuat" phai that su quyet dinh duoc gi.
    ///
    /// Truoc day gia tri nay duoc luu nhung khong nhanh code nao doc: bo chon XLSX xong van xuat
    /// Excel binh thuong, va chon PDF thi khong co gi xay ra vi khong he co duong xuat PDF.
    /// </summary>
    [Fact]
    public async Task Chi_Xuat_Duoc_Dinh_Dang_Bieu_Mau_Cho_Phep()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var chiPdf = await TaoBieuMauAsync(admin, new[] { "PDF" });
        var chiExcel = await TaoBieuMauAsync(admin, new[] { "XLSX" });

        // Bieu mau chi cho PDF: xuat PDF duoc, xuat Excel bi tu choi.
        (await admin.GetAsync($"/api/v1/nhap-xuat/bao-cao-tuy-bien/{chiPdf}/xuat-pdf"))
            .EnsureSuccessStatusCode();

        var excelBiChan = await admin.GetAsync(
            $"/api/v1/nhap-xuat/bao-cao-tuy-bien/{chiPdf}/xuat-excel");

        excelBiChan.IsSuccessStatusCode.Should().BeFalse();
        (await excelBiChan.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("thongBao").GetString().Should().Contain("không cho phép xuất XLSX");

        // Va nguoc lai.
        (await admin.GetAsync($"/api/v1/nhap-xuat/bao-cao-tuy-bien/{chiExcel}/xuat-excel"))
            .EnsureSuccessStatusCode();

        (await admin.GetAsync($"/api/v1/nhap-xuat/bao-cao-tuy-bien/{chiExcel}/xuat-pdf"))
            .IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Xuat_Pdf_Tra_Ve_Dung_Tep_Pdf()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var id = await TaoBieuMauAsync(admin, new[] { "XLSX", "PDF" });

        var phanHoi = await admin.GetAsync($"/api/v1/nhap-xuat/bao-cao-tuy-bien/{id}/xuat-pdf");
        phanHoi.EnsureSuccessStatusCode();

        phanHoi.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var byteDau = await phanHoi.Content.ReadAsByteArrayAsync();
        System.Text.Encoding.ASCII.GetString(byteDau, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Bieu_Mau_Khong_Co_Cot_Bi_Chan_Ngay_Khi_Luu()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tao = await admin.PostAsJsonAsync(Goc, new
        {
            ma = $"BMTK_RONG_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            ten = "Biểu mẫu không có cột",
            thuTu = 0,
            trangThai = 1,
            cauHinhCot = Array.Empty<object>(),
            dinhDangXuat = new[] { "XLSX" }
        });

        tao.IsSuccessStatusCode.Should().BeFalse();

        var loi = await tao.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("ít nhất một cột");
    }

    [Fact]
    public async Task Cot_Tro_Toi_Nguon_La_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tao = await admin.PostAsJsonAsync(Goc, new
        {
            ma = $"BMTK_SAI_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            ten = "Biểu mẫu có nguồn sai",
            thuTu = 0,
            trangThai = 1,
            cauHinhCot = new[]
            {
                new { ma = "linhTinh", tieuDe = "Linh tinh", nguon = "khong_ton_tai", hamTongHop = (string?)null, thuTu = 1 }
            },
            dinhDangXuat = new[] { "XLSX" }
        });

        tao.IsSuccessStatusCode.Should().BeFalse(
            "chặn ngay khi lưu thì quản trị viên thấy lỗi ở màn hình vừa nhập, "
            + "thay vì báo cáo vỡ lúc chạy vài ngày sau");

        var loi = await tao.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("không nằm trong danh sách");
    }

    [Fact]
    public async Task Danh_Sach_Nguon_Du_Lieu_Du_De_Man_Hinh_Do_Vao_O_Chon()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var ds = await LayDuLieuAsync(admin, "/api/v1/nhap-xuat/bao-cao-tuy-bien/nguon-du-lieu");

        var ma = ds.EnumerateArray().Select(x => x.GetProperty("ma").GetString()).ToList();

        ma.Should().Contain("maHoSo");
        ma.Should().Contain("tenDonVi");
        ma.Should().Contain("tongDiem");
        ds.EnumerateArray().Should().OnlyContain(
            x => !string.IsNullOrWhiteSpace(x.GetProperty("tieuDeGoiY").GetString()),
            "màn hình hiển thị tiêu đề gợi ý trong ô chọn, thiếu là ô chọn trống trơn");
    }

    private static async Task<string> TaoBieuMauAsync(HttpClient client, string[] dinhDangXuat)
    {
        var tao = await client.PostAsJsonAsync(Goc, new
        {
            ma = $"BMTK_DD_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            ten = $"Biểu mẫu {string.Join("-", dinhDangXuat)}",
            thuTu = 0,
            trangThai = 1,
            cauHinhCot = new[]
            {
                new { ma = "maHoSo", tieuDe = "Mã hồ sơ", nguon = "maHoSo", hamTongHop = (string?)null, thuTu = 1 }
            },
            dinhDangXuat
        });

        tao.EnsureSuccessStatusCode();

        return (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> LayDuLieuAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }
}
