using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// REQ-02 (Doi tuong), REQ-03 (Dot de nghi), REQ-04 (Loai tac gia), REQ-51 (Cau hinh sang kien):
/// Vong doi day du CRUD danh muc co ban, chuyen doi trang thai dot de nghi, va cau hinh he thong
/// lien quan den sang kien.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class DanhMucCoBanVaCauHinhTests
{
    private readonly UngDungKiemThu _ungDung;

    public DanhMucCoBanVaCauHinhTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ── REQ-02: Doi tuong ──────────────────────────────────────────────

    [Fact]
    public async Task Tao_Sua_Xoa_Doi_Tuong()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ma = $"DT_KT_{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var tao = await admin.PostAsJsonAsync("/api/v1/danh-muc/doi-tuong", new
        {
            ma,
            ten = "Đối tượng kiểm thử tự động",
            moTa = "Tạo bởi kiểm thử tích hợp",
            thuTu = 99,
            trangThai = 1
        });
        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        var chiTiet = await LayDuLieuAsync(admin, $"/api/v1/danh-muc/doi-tuong/{id}");
        chiTiet.GetProperty("ma").GetString().Should().Be(ma);
        chiTiet.GetProperty("ten").GetString().Should().Be("Đối tượng kiểm thử tự động");

        var sua = await admin.PutAsJsonAsync($"/api/v1/danh-muc/doi-tuong/{id}", new
        {
            ma,
            ten = "Đối tượng đã sửa",
            moTa = "Cập nhật lại",
            thuTu = 100,
            trangThai = 1
        });
        sua.EnsureSuccessStatusCode();

        var sauSua = await LayDuLieuAsync(admin, $"/api/v1/danh-muc/doi-tuong/{id}");
        sauSua.GetProperty("ten").GetString().Should().Be("Đối tượng đã sửa");

        (await admin.DeleteAsync($"/api/v1/danh-muc/doi-tuong/{id}")).EnsureSuccessStatusCode();

        var sauXoa = await admin.GetAsync($"/api/v1/danh-muc/doi-tuong/{id}");
        sauXoa.IsSuccessStatusCode.Should().BeFalse("đã xoá thì không đọc lại được");
    }

    [Fact]
    public async Task Trung_Ma_Doi_Tuong_Bi_Tu_Choi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ma = $"DT_DUP_{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var lanMot = await admin.PostAsJsonAsync("/api/v1/danh-muc/doi-tuong", new
        {
            ma,
            ten = "Đối tượng gốc",
            trangThai = 1
        });
        lanMot.EnsureSuccessStatusCode();
        var id = (await lanMot.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        try
        {
            var lanHai = await admin.PostAsJsonAsync("/api/v1/danh-muc/doi-tuong", new
            {
                ma,
                ten = "Đối tượng trùng mã",
                trangThai = 1
            });

            lanHai.StatusCode.Should().Be(HttpStatusCode.Conflict,
                "trùng mã phải bị từ chối 409");
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/danh-muc/doi-tuong/{id}");
        }
    }

    // ── REQ-04: Loai tac gia ───────────────────────────────────────────

    [Fact]
    public async Task Tao_Sua_Xoa_Loai_Tac_Gia_Voi_Nhieu_Tac_Gia()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ma = $"LTG_KT_{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var tao = await admin.PostAsJsonAsync("/api/v1/danh-muc/loai-tac-gia", new
        {
            ma,
            ten = "Nhóm tác giả kiểm thử",
            moTa = "Kiểm thử cho phép nhiều tác giả",
            thuTu = 99,
            trangThai = 1,
            choPhepNhieuTacGia = true,
            soTacGiaToiDa = 5
        });
        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        var chiTiet = await LayDuLieuAsync(admin, $"/api/v1/danh-muc/loai-tac-gia/{id}");
        chiTiet.GetProperty("ma").GetString().Should().Be(ma);
        chiTiet.GetProperty("choPhepNhieuTacGia").GetBoolean().Should().BeTrue();
        chiTiet.GetProperty("soTacGiaToiDa").GetInt32().Should().Be(5);

        var sua = await admin.PutAsJsonAsync($"/api/v1/danh-muc/loai-tac-gia/{id}", new
        {
            ma,
            ten = "Tác giả đơn kiểm thử",
            thuTu = 99,
            trangThai = 1,
            choPhepNhieuTacGia = false,
            soTacGiaToiDa = 1
        });
        sua.EnsureSuccessStatusCode();

        var sauSua = await LayDuLieuAsync(admin, $"/api/v1/danh-muc/loai-tac-gia/{id}");
        sauSua.GetProperty("choPhepNhieuTacGia").GetBoolean().Should().BeFalse();
        sauSua.GetProperty("soTacGiaToiDa").GetInt32().Should().Be(1);

        (await admin.DeleteAsync($"/api/v1/danh-muc/loai-tac-gia/{id}")).EnsureSuccessStatusCode();

        var sauXoa = await admin.GetAsync($"/api/v1/danh-muc/loai-tac-gia/{id}");
        sauXoa.IsSuccessStatusCode.Should().BeFalse("đã xoá thì không đọc lại được");
    }

    // ── REQ-03: Dot de nghi vong doi ───────────────────────────────────

    [Fact]
    public async Task Dot_De_Nghi_Vong_Doi_Mo_Dong_Khoa()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ma = $"DDN_KT_{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var quyTrinhId = await LayMotIdAsync(admin, "/api/v1/quy-trinh/chon");
        var boTieuChiId = await LayMotIdAsync(admin, "/api/v1/tieu-chi/chon");

        var tao = await admin.PostAsJsonAsync("/api/v1/danh-muc/dot-de-nghi", new
        {
            ma,
            ten = "Đợt kiểm thử vòng đời",
            nam = 2099,
            tuNgay = "2099-01-01",
            denNgay = "2099-12-31",
            hanNopHoSo = "2099-12-31T23:59:00+07:00",
            trangThai = 1,
            quyTrinhId,
            boTieuChiId
        });
        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        (await admin.PostAsync($"/api/v1/danh-muc/dot-de-nghi/{id}/mo-dot", null))
            .EnsureSuccessStatusCode();

        var dsDangMo = await LayDanhSachIdAsync(admin, "/api/v1/danh-muc/dot-de-nghi/dang-mo");
        dsDangMo.Should().Contain(id, "đợt vừa mở phải nằm trong danh sách đang mở");

        (await admin.PostAsync($"/api/v1/danh-muc/dot-de-nghi/{id}/dong-dot", null))
            .EnsureSuccessStatusCode();

        var dsSauDong = await LayDanhSachIdAsync(admin, "/api/v1/danh-muc/dot-de-nghi/dang-mo");
        dsSauDong.Should().NotContain(id, "đã đóng thì không còn trong danh sách đang mở");

        (await admin.PostAsync($"/api/v1/danh-muc/dot-de-nghi/{id}/khoa-dot", null))
            .EnsureSuccessStatusCode();

        await admin.DeleteAsync($"/api/v1/danh-muc/dot-de-nghi/{id}");
    }

    [Fact]
    public async Task Dot_De_Nghi_Sao_Chep_Tao_Ban_Moi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var dsCu = (await (await admin.GetAsync("/api/v1/danh-muc/dot-de-nghi/quan-ly?soDong=1"))
            .Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().First();
        var idGoc = dsCu.GetProperty("id").GetString()!;

        var maMoi = $"SC_KT_{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var saoChep = await admin.PostAsJsonAsync(
            $"/api/v1/danh-muc/dot-de-nghi/{idGoc}/sao-chep",
            new { ma = maMoi, ten = "Đợt sao chép kiểm thử", nam = 2098 });
        saoChep.EnsureSuccessStatusCode();

        var idMoi = (await saoChep.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        idMoi.Should().NotBe(idGoc, "bản sao phải có ID khác gốc");

        var chiTiet = await LayDuLieuAsync(admin, $"/api/v1/danh-muc/dot-de-nghi/{idMoi}");
        chiTiet.GetProperty("ma").GetString().Should().Be(maMoi);

        await admin.DeleteAsync($"/api/v1/danh-muc/dot-de-nghi/{idMoi}");
    }

    // ── REQ-51: Cau hinh thong tin sang kien ───────────────────────────

    [Fact]
    public async Task Cau_Hinh_Sang_Kien_Co_Day_Du_Khoa()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/he-thong/cau-hinh");
        phanHoi.EnsureSuccessStatusCode();

        var cacKhoa = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .Select(x => x.GetProperty("khoa").GetString()).ToList();

        var khoaCanCo = new[]
        {
            "MUC_CANH_BAO_TRUNG_LAP_VANG",
            "MUC_CANH_BAO_TRUNG_LAP_DO",
            "HE_SO_TU_VUNG",
            "HE_SO_NGU_NGHIA",
            "TU_DONG_KIEM_TRA_TRUNG_LAP",
            "MAU_MA_HO_SO",
            "DUNG_LUONG_TEP_TOI_DA_MB",
            "SO_TEP_TOI_DA"
        };

        foreach (var khoa in khoaCanCo)
            cacKhoa.Should().Contain(khoa,
                $"khoá {khoa} phải hiển thị trên giao diện quản trị");
    }

    [Fact]
    public async Task Sua_Nguong_Canh_Bao_Trung_Lap_Duoc_Luu_Lai()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var cu = await LayGiaTriAsync(admin, "MUC_CANH_BAO_TRUNG_LAP_DO");

        await DatGiaTriAsync(admin, "MUC_CANH_BAO_TRUNG_LAP_DO", "42");

        try
        {
            var sauSua = await LayGiaTriAsync(admin, "MUC_CANH_BAO_TRUNG_LAP_DO");
            sauSua.Should().Be("42", "giá trị ngưỡng vừa sửa phải được lưu lại");
        }
        finally
        {
            await DatGiaTriAsync(admin, "MUC_CANH_BAO_TRUNG_LAP_DO", cu ?? "40");
        }
    }

    // ── Authorization: Tac gia khong quan ly duoc danh muc ─────────────

    [Fact]
    public async Task Tac_Gia_Khong_Them_Duoc_Danh_Muc()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.PostAsJsonAsync("/api/v1/danh-muc/doi-tuong", new
        {
            ma = "KHONG_DUOC_THEM",
            ten = "Sẽ bị từ chối",
            trangThai = 1
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "tác giả không có quyền thêm danh mục");
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static async Task<JsonElement> LayDuLieuAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();
        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<List<string?>> LayDanhSachIdAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();
        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .Select(x => x.GetProperty("id").GetString()).ToList();
    }

    private static async Task<string> LayMotIdAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();
        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().First().GetProperty("id").GetString()!;
    }

    private static async Task<string?> LayGiaTriAsync(HttpClient client, string khoa)
    {
        var phanHoi = await client.GetAsync("/api/v1/he-thong/cau-hinh");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("khoa").GetString() == khoa)
            .TryGetProperty("giaTri", out var g) ? g.GetString() : null;
    }

    private static async Task DatGiaTriAsync(HttpClient client, string khoa, string giaTri)
    {
        (await client.PutAsJsonAsync("/api/v1/he-thong/cau-hinh",
            new[] { new { khoa, giaTri } })).EnsureSuccessStatusCode();
    }
}
