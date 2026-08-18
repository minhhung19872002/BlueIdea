using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop chuc nang 19-20: hoi dong, thanh vien, phien hop, diem danh, bo phieu,
/// ket luan — dung cac endpoint ma man hinh Hoi dong sang kien goi.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class HoiDongTests
{
    private readonly UngDungKiemThu _ungDung;

    public HoiDongTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Luu_Thanh_Vien_Bat_Buoc_Dung_Mot_Chu_Tich()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var hoiDongId = await LayHoiDongMauAsync(admin);
        var hoiDong = await LayMotAsync(admin, $"/api/v1/hoi-dong/{hoiDongId}");

        // Ep TAT CA thanh vien thanh chu tich -> vi pham rang buoc dung mot chu tich.
        var danhSach = hoiDong.GetProperty("thanhVien").EnumerateArray()
            .Select(tv => TaoDtoThanhVien(tv, chucDanh: "CHU_TICH"))
            .ToList();

        var phanHoi = await admin.PutAsJsonAsync(
            $"/api/v1/hoi-dong/{hoiDongId}/thanh-vien", danhSach);

        phanHoi.IsSuccessStatusCode.Should().BeFalse();

        var loi = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("đúng 1 Chủ tịch");
    }

    [Fact]
    public async Task Luu_Thanh_Vien_It_Hon_Toi_Thieu_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var hoiDongId = await LayHoiDongMauAsync(admin);
        var hoiDong = await LayMotAsync(admin, $"/api/v1/hoi-dong/{hoiDongId}");

        var soToiThieu = hoiDong.GetProperty("soThanhVienToiThieu").GetInt32();

        // Chi giu lai dung mot chu tich: hop le ve chuc danh nhung thieu thanh vien.
        var chuTich = hoiDong.GetProperty("thanhVien").EnumerateArray()
            .First(tv => tv.GetProperty("chucDanh").GetString() == "CHU_TICH");

        var phanHoi = await admin.PutAsJsonAsync(
            $"/api/v1/hoi-dong/{hoiDongId}/thanh-vien",
            new[] { TaoDtoThanhVien(chuTich) });

        phanHoi.IsSuccessStatusCode.Should().BeFalse();

        var loi = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain($"tối thiểu {soToiThieu}");
    }

    [Fact]
    public async Task Luong_Phien_Hop_Diem_Danh_Bo_Phieu_Ket_Luan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");

        var hoiDongId = await LayHoiDongMauAsync(admin);
        var hoiDong = await LayMotAsync(admin, $"/api/v1/hoi-dong/{hoiDongId}");
        var sangKienId = await LaySangKienBatKyAsync(admin);

        // --- 1. Tao phien hop ----------------------------------------------------
        var taoPhien = await admin.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop", new
        {
            hoiDongId,
            tenPhien = "Phiên họp kiểm thử tự động",
            thoiGianBatDau = DateTimeOffset.UtcNow,
            hinhThuc = "TRUC_TIEP",
            diaDiem = "Phòng họp kiểm thử",
            sangKienIds = new[] { sangKienId }
        });

        taoPhien.EnsureSuccessStatusCode();

        var phien = (await taoPhien.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        var phienId = phien.GetProperty("id").GetString()!;

        phien.GetProperty("trangThaiPhien").GetString().Should().Be("DU_KIEN");
        phien.GetProperty("maPhien").GetString().Should().NotBeNullOrWhiteSpace();

        // Tao phien phai sinh san dong diem danh cho MOI thanh vien dang hoat dong.
        var soThanhVien = hoiDong.GetProperty("thanhVien").GetArrayLength();
        phien.GetProperty("diemDanh").GetArrayLength().Should().Be(soThanhVien);
        phien.GetProperty("danhSachHoSo").GetArrayLength().Should().Be(1);

        // --- 2. Diem danh --------------------------------------------------------
        var thanhVienChuTich = hoiDong.GetProperty("thanhVien").EnumerateArray()
            .First(tv => tv.GetProperty("chucDanh").GetString() == "CHU_TICH");

        var thanhVienId = thanhVienChuTich.GetProperty("id").GetString()!;

        var diemDanh = await admin.PostAsJsonAsync(
            $"/api/v1/hoi-dong/phien-hop/{phienId}/diem-danh",
            new { thanhVienId, coMat = true, lyDoVang = (string?)null });

        diemDanh.EnsureSuccessStatusCode();

        var sauDiemDanh = await LayMotAsync(chuTich, $"/api/v1/hoi-dong/phien-hop/{phienId}");

        sauDiemDanh.GetProperty("diemDanh").EnumerateArray()
            .Count(x => x.GetProperty("coMat").GetBoolean())
            .Should().Be(1);

        // --- 3. Bo phieu ---------------------------------------------------------
        var boPhieu = await chuTich.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop/bo-phieu", new
        {
            phienHopId = phienId,
            sangKienId,
            yKien = "DONG_Y",
            ghiChu = "Đồng ý công nhận",
            laPhieuKin = false
        });

        boPhieu.EnsureSuccessStatusCode();

        var ketQua = await LayMotAsync(
            chuTich,
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-qua-bo-phieu?sangKienId={sangKienId}");

        ketQua.GetProperty("tongPhieu").GetInt32().Should().Be(1);
        ketQua.GetProperty("dongY").GetInt32().Should().Be(1);
        ketQua.GetProperty("tyLeDongY").GetDecimal().Should().Be(100m);

        // --- 4. Ket thuc phien ---------------------------------------------------
        var ketThuc = await chuTich.PostAsJsonAsync(
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-thuc",
            new { ketLuan = "Thông qua hồ sơ đưa ra xét." });

        ketThuc.EnsureSuccessStatusCode();

        var sauKetThuc = await LayMotAsync(chuTich, $"/api/v1/hoi-dong/phien-hop/{phienId}");

        sauKetThuc.GetProperty("trangThaiPhien").GetString().Should().Be("DA_KET_THUC");
        sauKetThuc.GetProperty("ketLuan").GetString().Should().Contain("Thông qua");

        // --- 5. Phien da ket thuc thi khoa bo phieu ------------------------------
        var boPhieuLai = await chuTich.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop/bo-phieu", new
        {
            phienHopId = phienId,
            sangKienId,
            yKien = "KHONG_DONG_Y",
            laPhieuKin = false
        });

        boPhieuLai.IsSuccessStatusCode.Should().BeFalse();

        var loi = await boPhieuLai.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("đã kết thúc");
    }

    [Fact]
    public async Task Nguoi_Ngoai_Hoi_Dong_Khong_Bo_Phieu_Duoc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var hoiDongId = await LayHoiDongMauAsync(admin);
        var sangKienId = await LaySangKienBatKyAsync(admin);

        var taoPhien = await admin.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop", new
        {
            hoiDongId,
            tenPhien = "Phiên họp kiểm thử quyền bỏ phiếu",
            thoiGianBatDau = DateTimeOffset.UtcNow,
            hinhThuc = "TRUC_TIEP",
            sangKienIds = new[] { sangKienId }
        });

        taoPhien.EnsureSuccessStatusCode();

        var phienId = (await taoPhien.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        // admin co toan quyen he thong nhung KHONG phai thanh vien hoi dong nay.
        var boPhieu = await admin.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop/bo-phieu", new
        {
            phienHopId = phienId,
            sangKienId,
            yKien = "DONG_Y",
            laPhieuKin = false
        });

        boPhieu.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var loi = await boPhieu.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("không phải thành viên");
    }

    /// <summary>
    /// Nut "Xuat phieu cham" tren trang hoi dong: co phieu da gui thi tra PDF, chua co phieu
    /// nao thi bao loi nghiep vu ro rang chu khong tra ve mot tep PDF rong.
    /// </summary>
    [Fact]
    public async Task Xuat_Phieu_Cham_Theo_Hoi_Dong_Tra_Ve_Pdf_Hoac_Bao_Chua_Co_Phieu()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var hoiDongId = await LayHoiDongMauAsync(admin);

        var phanHoi = await admin.GetAsync($"/api/v1/nhap-xuat/phieu-cham/hoi-dong/{hoiDongId}");

        if (phanHoi.IsSuccessStatusCode)
        {
            phanHoi.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

            var noiDung = await phanHoi.Content.ReadAsByteArrayAsync();
            System.Text.Encoding.ASCII.GetString(noiDung, 0, 4).Should().Be("%PDF");
            return;
        }

        // Du lieu mau khong bao dam co san phieu da gui cua hoi dong nay (phu thuoc thu tu
        // chay cua cac lop kiem thu khac), nen truong hop chua co phieu cung phai dung.
        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var loi = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("Không có phiếu chấm nào");
    }

    // ---------------------------------------------------------------------------------

    /// <summary>DTO thanh vien dung cho endpoint luu danh sach (giu nguyen quyen han cu).</summary>
    private static object TaoDtoThanhVien(JsonElement tv, string? chucDanh = null) => new
    {
        id = tv.GetProperty("id").GetString(),
        nguoiDungId = tv.TryGetProperty("nguoiDungId", out var nd)
                      && nd.ValueKind == JsonValueKind.String
            ? nd.GetString()
            : null,
        hoTenHienThi = tv.GetProperty("hoTenHienThi").GetString(),
        chucVuCongTac = DocChuoi(tv, "chucVuCongTac"),
        donViCongTac = DocChuoi(tv, "donViCongTac"),
        chucDanh = chucDanh ?? tv.GetProperty("chucDanh").GetString(),
        quyenChamDiem = tv.GetProperty("quyenChamDiem").GetBoolean(),
        quyenNhanXet = tv.GetProperty("quyenNhanXet").GetBoolean(),
        quyenBoPhieu = tv.GetProperty("quyenBoPhieu").GetBoolean(),
        quyenKyBienBan = tv.GetProperty("quyenKyBienBan").GetBoolean(),
        quyenKetLuan = tv.GetProperty("quyenKetLuan").GetBoolean()
    };

    private static string? DocChuoi(JsonElement muc, string ten)
        => muc.TryGetProperty(ten, out var giaTri) && giaTri.ValueKind == JsonValueKind.String
            ? giaTri.GetString()
            : null;

    private static async Task<string> LayHoiDongMauAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/hoi-dong?soDong=1");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu")[0].GetProperty("id").GetString()!;
    }

    private static async Task<string> LaySangKienBatKyAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/sang-kien?soDong=1");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu")[0].GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> LayMotAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu");
    }
}
