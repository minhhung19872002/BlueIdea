using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
            ghiChu = "Đồng ý công nhận"
        });

        boPhieu.EnsureSuccessStatusCode();

        var ketQua = await LayMotAsync(
            chuTich,
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-qua-bo-phieu?sangKienId={sangKienId}");

        /*
         * Quy trinh mau bat "Bo phieu kin" o buoc hop hoi dong, nen trong luc phien CON MO chi thay
         * so nguoi da bo, khong thay ai bo gi. Thu ky van biet con ai chua bo de nhac, nhung khong
         * ai nhin duoc bang diem roi bo theo.
         */
        ketQua.GetProperty("boPhieuKin").GetBoolean().Should().BeTrue();
        ketQua.GetProperty("daChotPhien").GetBoolean().Should().BeFalse();
        ketQua.GetProperty("tongPhieu").GetInt32().Should().Be(1, "vẫn đếm được số người đã bỏ");
        ketQua.GetProperty("dongY").GetInt32().Should().Be(0, "số liệu kiểm phiếu chưa được lộ");
        ketQua.GetProperty("tyLeDongY").GetDecimal().Should().Be(0m);

        // --- 4. Ket thuc phien ---------------------------------------------------
        var ketThuc = await chuTich.PostAsJsonAsync(
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-thuc",
            new { ketLuan = "Thông qua hồ sơ đưa ra xét." });

        ketThuc.EnsureSuccessStatusCode();

        var sauKetThuc = await LayMotAsync(chuTich, $"/api/v1/hoi-dong/phien-hop/{phienId}");

        sauKetThuc.GetProperty("trangThaiPhien").GetString().Should().Be("DA_KET_THUC");
        sauKetThuc.GetProperty("ketLuan").GetString().Should().Contain("Thông qua");

        // Chot phien roi thi so lieu kiem phieu moi lo ra.
        var ketQuaSauChot = await LayMotAsync(
            chuTich,
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-qua-bo-phieu?sangKienId={sangKienId}");

        ketQuaSauChot.GetProperty("daChotPhien").GetBoolean().Should().BeTrue();
        ketQuaSauChot.GetProperty("dongY").GetInt32().Should().Be(1);
        ketQuaSauChot.GetProperty("tyLeDongY").GetDecimal().Should().Be(100m);

        // --- 5. Phien da ket thuc thi khoa bo phieu ------------------------------
        var boPhieuLai = await chuTich.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop/bo-phieu", new
        {
            phienHopId = phienId,
            sangKienId,
            yKien = "KHONG_DONG_Y"
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
            yKien = "DONG_Y"
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

    /// <summary>
    /// Phieu kin phai kin ca o API: nguoi khac chi thay co mot la phieu, khong thay ai bo va
    /// khong doc duoc ghi chu kem phieu. Chinh chu van thay lai la phieu cua minh.
    /// </summary>
    [Fact]
    public async Task Phieu_Kin_Khong_Lo_Danh_Tinh_Nguoi_Bo_Phieu()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");
        var thanhVienKhac = await _ungDung.TaoClientDaDangNhapAsync("hoidong01");

        var hoiDongId = await LayHoiDongMauAsync(admin);
        var sangKienId = await LaySangKienBatKyAsync(admin);
        var phienId = await TaoPhienHopAsync(admin, hoiDongId, sangKienId, "Phiên kiểm thử phiếu kín");

        const string ghiChuRieng = "Ghi chú riêng của người bỏ phiếu kín";

        var boPhieu = await chuTich.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop/bo-phieu", new
        {
            phienHopId = phienId,
            sangKienId,
            yKien = "DONG_Y",
            ghiChu = ghiChuRieng
        });

        boPhieu.EnsureSuccessStatusCode();

        // --- Chinh chu doc lai: van thay day du ---------------------------------
        var theoChuTich = await LayMotAsync(chuTich, $"/api/v1/hoi-dong/phien-hop/{phienId}");
        var phieuCuaToi = theoChuTich.GetProperty("phieuBoPhieu").EnumerateArray()
            .Single(x => x.GetProperty("sangKienId").GetString() == sangKienId);

        phieuCuaToi.GetProperty("thanhVienId").GetString().Should().NotBe(Guid.Empty.ToString());
        phieuCuaToi.GetProperty("ghiChu").GetString().Should().Be(ghiChuRieng);

        // --- Thanh vien khac doc: chi thay la phieu, khong thay danh tinh -------
        var theoNguoiKhac = await LayMotAsync(thanhVienKhac, $"/api/v1/hoi-dong/phien-hop/{phienId}");
        var phieuAn = theoNguoiKhac.GetProperty("phieuBoPhieu").EnumerateArray()
            .Single(x => x.GetProperty("sangKienId").GetString() == sangKienId);

        phieuAn.GetProperty("thanhVienId").GetString().Should().Be(Guid.Empty.ToString());
        // Truong null bi bo qua khi tuan tu hoa, nen "khong co truong" cung la khong lo ghi chu.
        var loGhiChu = phieuAn.TryGetProperty("ghiChu", out var gc)
                       && gc.ValueKind != JsonValueKind.Null;

        loGhiChu.Should().BeFalse("ghi chú kèm phiếu kín không được trả cho người khác");
        phieuAn.GetProperty("laPhieuKin").GetBoolean().Should().BeTrue();

        // --- Phien con mo: so lieu kiem phieu chua lo -------------------------
        var ketQua = await LayMotAsync(
            thanhVienKhac,
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-qua-bo-phieu?sangKienId={sangKienId}");

        ketQua.GetProperty("tongPhieu").GetInt32().Should().Be(1, "vẫn đếm được số người đã bỏ");
        ketQua.GetProperty("dongY").GetInt32().Should().Be(0);

        // --- Chot phien: an danh tinh khong duoc lam sai so lieu kiem phieu -----
        var ketThuc = await chuTich.PostAsJsonAsync(
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-thuc",
            new { ketLuan = "Kết thúc phiên kiểm thử phiếu kín." });

        ketThuc.EnsureSuccessStatusCode();

        var ketQuaSauChot = await LayMotAsync(
            thanhVienKhac,
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-qua-bo-phieu?sangKienId={sangKienId}");

        ketQuaSauChot.GetProperty("tongPhieu").GetInt32().Should().Be(1);
        ketQuaSauChot.GetProperty("dongY").GetInt32().Should().Be(1);

        // Danh tinh thi KHONG lo lai sau khi chot: kin la kin vinh vien, chi so lieu moi mo ra.
        var sauChot = await LayMotAsync(thanhVienKhac, $"/api/v1/hoi-dong/phien-hop/{phienId}");
        var phieuVanAn = sauChot.GetProperty("phieuBoPhieu").EnumerateArray()
            .Single(x => x.GetProperty("sangKienId").GetString() == sangKienId);

        phieuVanAn.GetProperty("thanhVienId").GetString().Should().Be(Guid.Empty.ToString());
    }

    /// <summary>
    /// Nguoi bo phieu KHONG tu quyet dinh duoc phieu cua minh kin hay ho.
    ///
    /// Truoc day <c>laPhieuKin</c> la mot truong cua than yeu cau, nen o tick "Bo phieu kin" ma
    /// quan tri vien dat tren buoc quy trinh khong ep duoc ai — moi thanh vien mot kieu trong cung
    /// mot phien hop. Nay may chu suy ra tu cau hinh buoc va bo qua moi thu may khach gui len.
    /// </summary>
    [Fact]
    public async Task May_Khach_Khong_Tu_Dat_Duoc_Phieu_Kin_Hay_Ho()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");

        var hoiDongId = await LayHoiDongMauAsync(admin);
        var sangKienId = await LaySangKienBatKyAsync(admin);
        var phienId = await TaoPhienHopAsync(
            admin, hoiDongId, sangKienId, "Phiên kiểm thử máy khách ép phiếu hở");

        // Co tinh gui laPhieuKin = false de doi phieu cong khai.
        var boPhieu = await chuTich.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop/bo-phieu", new
        {
            phienHopId = phienId,
            sangKienId,
            yKien = "DONG_Y",
            ghiChu = "Cố tình xin phiếu hở",
            laPhieuKin = false
        });

        boPhieu.EnsureSuccessStatusCode();

        var ketQua = await LayMotAsync(
            chuTich,
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-qua-bo-phieu?sangKienId={sangKienId}");

        ketQua.GetProperty("boPhieuKin").GetBoolean().Should().BeTrue(
            "cấu hình bước quyết định, không phải trường máy khách gửi lên");

        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var laPhieuKin = await db.PhieuBoPhieu.AsNoTracking()
            .Where(x => x.PhienHopId == Guid.Parse(phienId)
                        && x.SangKienId == Guid.Parse(sangKienId))
            .Select(x => x.LaPhieuKin)
            .FirstAsync();

        laPhieuKin.Should().BeTrue("lá phiếu phải được ghi là kín bất kể máy khách gửi gì");
    }

    /// <summary>
    /// Bo tick "Ket luan" cua mot thanh vien thi chinh thanh vien do khong ket thuc duoc phien,
    /// du vai tro cua ho van co quyen HOI_DONG.KET_LUAN.
    /// </summary>
    [Fact]
    public async Task Thanh_Vien_Bi_Tat_Quyen_Ket_Luan_Khong_Ket_Thuc_Duoc_Phien()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");

        var hoiDongId = await LayHoiDongMauAsync(admin);
        var sangKienId = await LaySangKienBatKyAsync(admin);
        var phienId = await TaoPhienHopAsync(
            admin, hoiDongId, sangKienId, "Phiên kiểm thử quyền kết luận");

        await DoiQuyenThanhVienAsync(admin, hoiDongId, "CHU_TICH", "quyenKetLuan", false);

        try
        {
            var biChan = await chuTich.PostAsJsonAsync(
                $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-thuc",
                new { ketLuan = "Kết luận khi đã bị tắt quyền" });

            biChan.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var loi = await biChan.Content.ReadFromJsonAsync<JsonElement>();
            loi.GetProperty("thongBao").GetString().Should().Contain("không có quyền kết luận");
        }
        finally
        {
            await DoiQuyenThanhVienAsync(admin, hoiDongId, "CHU_TICH", "quyenKetLuan", true);
        }

        // Bat lai quyen thi chinh nguoi do ket thuc duoc — chung minh chan la do o tick,
        // khong phai do mot rang buoc nao khac cua phien.
        var ketThuc = await chuTich.PostAsJsonAsync(
            $"/api/v1/hoi-dong/phien-hop/{phienId}/ket-thuc",
            new { ketLuan = "Thông qua." });

        ketThuc.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Bo tick "Nhan xet" thi thanh vien khong ghi duoc y kien cho ho so; va thanh vien khong co
    /// tick "Ket luan" thi khong chot duoc ket qua xet cua ho so.
    /// </summary>
    [Fact]
    public async Task Thanh_Vien_Bi_Tat_Quyen_Nhan_Xet_Khong_Ghi_Y_Kien_Duoc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var thuKy = await _ungDung.TaoClientDaDangNhapAsync("thuky");

        var hoiDongId = await LayHoiDongMauAsync(admin);
        var sangKienId = await LaySangKienBatKyAsync(admin);
        var phienId = await TaoPhienHopAsync(
            admin, hoiDongId, sangKienId, "Phiên kiểm thử quyền nhận xét");

        await DoiQuyenThanhVienAsync(admin, hoiDongId, "UY_VIEN_THU_KY", "quyenNhanXet", false);

        try
        {
            var biChan = await thuKy.PostAsJsonAsync(
                $"/api/v1/hoi-dong/phien-hop/{phienId}/y-kien-ho-so",
                new
                {
                    sangKienId,
                    ketLuanRieng = "Nhận xét khi đã bị tắt quyền",
                    ketQua = (string?)null
                });

            biChan.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var loi = await biChan.Content.ReadFromJsonAsync<JsonElement>();
            loi.GetProperty("thongBao").GetString().Should().Contain("không có quyền nhận xét");
        }
        finally
        {
            await DoiQuyenThanhVienAsync(admin, hoiDongId, "UY_VIEN_THU_KY", "quyenNhanXet", true);
        }

        const string yKien = "Hồ sơ trình bày rõ ràng.";

        var ghiDuoc = await thuKy.PostAsJsonAsync(
            $"/api/v1/hoi-dong/phien-hop/{phienId}/y-kien-ho-so",
            new { sangKienId, ketLuanRieng = yKien, ketQua = (string?)null });

        ghiDuoc.EnsureSuccessStatusCode();

        // Du lieu mau: thu ky khong co tick "Ket luan" nen khong chot duoc ket qua xet.
        var chotKetQua = await thuKy.PostAsJsonAsync(
            $"/api/v1/hoi-dong/phien-hop/{phienId}/y-kien-ho-so",
            new { sangKienId, ketLuanRieng = yKien, ketQua = "DAT" });

        chotKetQua.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var loiKetLuan = await chotKetQua.Content.ReadFromJsonAsync<JsonElement>();
        loiKetLuan.GetProperty("thongBao").GetString()
            .Should().Contain("không có quyền kết luận");
    }

    // ---------------------------------------------------------------------------------

    /// <summary>Tao mot phien hop kem dung mot ho so de kiem thu, tra ve id phien.</summary>
    private static async Task<string> TaoPhienHopAsync(
        HttpClient admin, string hoiDongId, string sangKienId, string tenPhien)
    {
        var taoPhien = await admin.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop", new
        {
            hoiDongId,
            tenPhien,
            thoiGianBatDau = DateTimeOffset.UtcNow,
            hinhThuc = "TRUC_TIEP",
            sangKienIds = new[] { sangKienId }
        });

        taoPhien.EnsureSuccessStatusCode();

        return (await taoPhien.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;
    }

    /// <summary>
    /// Bat / tat mot o tick quyen cua thanh vien theo chuc danh, gui lai nguyen danh sach de
    /// khong pha rang buoc "dung 1 chu tich" va "du so thanh vien toi thieu".
    /// </summary>
    private static async Task DoiQuyenThanhVienAsync(
        HttpClient admin, string hoiDongId, string chucDanh, string tenQuyen, bool giaTri)
    {
        var hoiDong = await LayMotAsync(admin, $"/api/v1/hoi-dong/{hoiDongId}");

        var danhSach = hoiDong.GetProperty("thanhVien").EnumerateArray()
            .Select(tv => tv.GetProperty("chucDanh").GetString() == chucDanh
                ? TaoDtoThanhVien(tv, doiQuyen: (tenQuyen, giaTri))
                : TaoDtoThanhVien(tv))
            .ToList();

        var phanHoi = await admin.PutAsJsonAsync(
            $"/api/v1/hoi-dong/{hoiDongId}/thanh-vien", danhSach);

        phanHoi.EnsureSuccessStatusCode();
    }

    // ---------------------------------------------------------------------------------

    /// <summary>DTO thanh vien dung cho endpoint luu danh sach (giu nguyen quyen han cu).</summary>
    private static object TaoDtoThanhVien(
        JsonElement tv, string? chucDanh = null, (string Ten, bool GiaTri)? doiQuyen = null)
    {
        bool Quyen(string ten)
            => doiQuyen is { } doi && doi.Ten == ten
                ? doi.GiaTri
                : tv.GetProperty(ten).GetBoolean();

        return new
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
            quyenChamDiem = Quyen("quyenChamDiem"),
            quyenNhanXet = Quyen("quyenNhanXet"),
            quyenBoPhieu = Quyen("quyenBoPhieu"),
            quyenKyBienBan = Quyen("quyenKyBienBan"),
            quyenKetLuan = Quyen("quyenKetLuan")
        };
    }

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

    /// <summary>
    /// Mot ho so DA NOP — tuc la co snapshot quy trinh.
    ///
    /// Truoc day ham nay lay ho so dau tien bat ky, va co luc trung phai mot ban NHAP do phep kiem
    /// khac de lai. Ho so nhap chua co snapshot quy trinh, nen moi luat lay tu cau hinh buoc
    /// (bo phieu kin, cham diem doc lap...) deu tra ve "khong bat" — phep kiem hong theo thu tu
    /// chay chu khong theo hanh vi cua he thong.
    /// </summary>
    private async Task<string> LaySangKienBatKyAsync(HttpClient client)
    {
        _ = client;

        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider
            .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var id = await db.SangKien.AsNoTracking()
            .Where(x => x.QuyTrinhSnapshot != null && x.NgayNop != null)
            .OrderBy(x => x.NgayTao)
            .Select(x => x.Id)
            .FirstAsync();

        return id.ToString();
    }

    private static async Task<JsonElement> LayMotAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu");
    }
}
