using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu dot 2: sinh van ban tu bieu mau xuat (chuc nang 6), phieu tiep nhan (chuc nang 22),
/// xuat phieu cham dang ZIP va ky so phieu danh gia (chuc nang 35).
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class BieuMauVaPhieuTests
{
    private readonly UngDungKiemThu _ungDung;

    public BieuMauVaPhieuTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Xem_Truoc_Bieu_Mau_Dung_Du_Lieu_Mau_Khi_Chua_Chon_Ho_So()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tao = await admin.PostAsJsonAsync("/api/v1/danh-muc/bieu-mau-xuat", new
        {
            ma = $"KT_BM_{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            ten = "Biểu mẫu kiểm thử xem trước",
            loai = "PHIEU_TIEP_NHAN",
            dinhDang = "PDF",
            trangThai = 1,
            thuTu = 90,
            cauHinhTruong = new[]
            {
                new { placeholder = "Mã hồ sơ", nguon = "maHoSo", kieu = "text" },
                new { placeholder = "Ngày nộp", nguon = "ngayNop", kieu = "date" },
                new { placeholder = "Trường sai", nguon = "khong_ton_tai", kieu = "text" },
            }
        });

        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        var xem = await admin.GetAsync($"/api/v1/danh-muc/bieu-mau-xuat/{id}/xem-truoc");
        xem.EnsureSuccessStatusCode();

        var kq = (await xem.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        kq.GetProperty("dongDuLieu").GetArrayLength().Should().Be(3);

        // Anh xa sai KHONG bi bo im lang — bao lai de nguoi cau hinh sua duoc.
        kq.GetProperty("truongThieu").EnumerateArray()
            .Select(x => x.GetString()).Should().Contain("khong_ton_tai");

        var pdf = await admin.GetAsync($"/api/v1/danh-muc/bieu-mau-xuat/{id}/xem-truoc.pdf");
        pdf.EnsureSuccessStatusCode();
        pdf.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await pdf.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task Truong_Kha_Dung_Dung_Chung_Ma_Nguon_Voi_Bao_Cao_Tuy_Bien()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var truong = await admin.GetAsync(
            "/api/v1/danh-muc/bieu-mau-xuat/truong-kha-dung?loai=PHIEU_TIEP_NHAN");

        truong.EnsureSuccessStatusCode();

        var ma = (await truong.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .Select(x => x.GetProperty("nguon").GetString()).ToList();

        // Man hinh cau hinh bieu mau dung dropdown nguon cua bao cao tuy bien; hai ben lech thu
        // ngu thi quan tri vien chon xong van in ra o trong.
        var nguonBaoCao = await admin.GetAsync("/api/v1/nhap-xuat/bao-cao-tuy-bien/nguon-du-lieu");
        nguonBaoCao.EnsureSuccessStatusCode();

        var maBaoCao = (await nguonBaoCao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .Select(x => x.GetProperty("ma").GetString()).ToList();

        ma.Should().Contain("maHoSo");
        ma.Should().Contain(maBaoCao.Where(x => x is "maHoSo" or "tenSangKien" or "tenDonVi")!);
    }

    [Fact]
    public async Task Ho_So_Chua_Nop_Thi_Khong_Co_Phieu_Tiep_Nhan()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var nhap = await tacGia.GetAsync("/api/v1/sang-kien/cua-toi?trangThaiTong=NHAP&soDong=1");
        nhap.EnsureSuccessStatusCode();

        var duLieu = (await nhap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        if (duLieu.GetArrayLength() == 0)
        {
            return; // Du lieu mau khong con ho so nhap nao — khong co gi de kiem tra.
        }

        var id = duLieu[0].GetProperty("id").GetString()!;
        var phanHoi = await tacGia.GetAsync($"/api/v1/sang-kien/{id}/phieu-tiep-nhan");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Xuat_Phieu_Tiep_Nhan_Cua_Ho_So_Da_Nop_Ra_Pdf()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var ds = await admin.GetAsync("/api/v1/sang-kien?trangThaiTong=DA_NOP&soDong=1");
        ds.EnsureSuccessStatusCode();

        var duLieu = (await ds.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        if (duLieu.GetArrayLength() == 0)
        {
            ds = await admin.GetAsync("/api/v1/sang-kien?trangThaiTong=DANG_XU_LY&soDong=1");
            ds.EnsureSuccessStatusCode();
            duLieu = (await ds.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        }

        duLieu.GetArrayLength().Should().BeGreaterThan(0, "dữ liệu mẫu phải có hồ sơ đã nộp");

        var id = duLieu[0].GetProperty("id").GetString()!;
        var phanHoi = await admin.GetAsync($"/api/v1/sang-kien/{id}/phieu-tiep-nhan");

        phanHoi.EnsureSuccessStatusCode();
        phanHoi.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var tep = await phanHoi.Content.ReadAsByteArrayAsync();
        System.Text.Encoding.ASCII.GetString(tep, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task Xuat_Phieu_Cham_Dang_Zip_Tach_Moi_Phieu_Mot_Tep()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var hoiDong = await admin.GetAsync("/api/v1/hoi-dong/chon");
        hoiDong.EnsureSuccessStatusCode();

        var hoiDongId = (await hoiDong.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu")[0].GetProperty("id").GetString()!;

        var zip = await admin.GetAsync(
            $"/api/v1/nhap-xuat/phieu-cham/hoi-dong/{hoiDongId}?dinhDang=ZIP");

        if (zip.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            // Hoi dong chua co phieu nao da gui — bao loi ro rang la dung hanh vi.
            var loi = await zip.Content.ReadFromJsonAsync<JsonElement>();
            loi.GetProperty("thongBao").GetString().Should().Contain("phiếu chấm");
            return;
        }

        zip.EnsureSuccessStatusCode();
        zip.Content.Headers.ContentType!.MediaType.Should().Be("application/zip");

        using var luong = await zip.Content.ReadAsStreamAsync();
        using var nen = new System.IO.Compression.ZipArchive(
            luong, System.IO.Compression.ZipArchiveMode.Read);

        nen.Entries.Should().NotBeEmpty();
        nen.Entries.Should().OnlyContain(x => x.FullName.EndsWith(".pdf", StringComparison.Ordinal));

        // Ten tep trong ZIP phai duy nhat, neu khong giai nen ra se de len nhau.
        nen.Entries.Select(x => x.FullName).Should().OnlyHaveUniqueItems();
    }
}
