using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Bay quyen truoc day co ten trong ma tran phan quyen nhung khong endpoint nao doi den, nen bat
/// hay tat chung deu khong thay doi gi — quan tri vien tuong minh dang siet mot thu ma thuc te
/// khong siet gi ca. Bo kiem nay chot lai: hai chuc nang con thieu da co that, va nam quyen con lai
/// gio thuc su chan duong.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class QuyenTachRiengTests
{
    private const string MatKhauTam = "Kt@Xoa2026";

    private readonly UngDungKiemThu _ungDung;

    public QuyenTachRiengTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ================================================= Chuc nang 43 — xoa tai khoan

    [Fact]
    public async Task Xoa_Tai_Khoan_Thi_Khong_Dang_Nhap_Duoc_Nua()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var (id, matKhau) = await TaoTaiKhoanTamAsync(admin, "kt.xoa.01");

        // Tai khoan vua tao dang dang nhap duoc.
        var truoc = await _ungDung.CreateClient().PostAsJsonAsync("/api/v1/xac-thuc/dang-nhap",
            new { tenDangNhap = "kt.xoa.01", matKhau });

        truoc.StatusCode.Should().Be(HttpStatusCode.OK, "tài khoản mới phải dùng được trước đã");

        var xoa = await admin.DeleteAsync($"/api/v1/he-thong/nguoi-dung/{id}");
        xoa.StatusCode.Should().Be(HttpStatusCode.OK);

        var sau = await _ungDung.CreateClient().PostAsJsonAsync("/api/v1/xac-thuc/dang-nhap",
            new { tenDangNhap = "kt.xoa.01", matKhau });

        sau.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "xoá mềm mà vẫn đăng nhập được thì xoá chẳng có tác dụng gì");

        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        // IgnoreQueryFilters: xoa MEM nen dong du lieu phai con, de nhat ky xu ly con truy nguoc duoc.
        var conBanGhi = await db.NguoiDung.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(x => x.Id == id && x.DaXoa);

        conBanGhi.Should().BeTrue(
            "phải là xoá mềm — xoá hẳn sẽ làm nhật ký trỏ tới người không còn tồn tại");
    }

    [Fact]
    public async Task Khong_Tu_Xoa_Tai_Khoan_Cua_Chinh_Minh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        Guid adminId;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            adminId = await db.NguoiDung.AsNoTracking()
                .Where(x => x.TenDangNhap == "admin")
                .Select(x => x.Id)
                .FirstAsync();
        }

        var phanHoi = await admin.DeleteAsync($"/api/v1/he-thong/nguoi-dung/{adminId}");

        phanHoi.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "quản trị viên tự xoá mình có thể khoá cứng cả hệ thống");
    }

    /// <summary>
    /// Tai khoan dang giu mot buoc xu ly chua xong thi khong xoa duoc.
    ///
    /// Xoa di thi buoc do khong con ai la tac nhan hop le: ho so ket cung giua chung, khong ai bam
    /// tiep duoc va cung khong co canh bao nao bao rang no dang mac ket.
    /// </summary>
    [Fact]
    public async Task Khong_Xoa_Tai_Khoan_Dang_Giu_Buoc_Xu_Ly()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var (id, _) = await TaoTaiKhoanTamAsync(admin, "kt.xoa.02");

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var hoSo = await db.SangKien.AsNoTracking()
                .FirstAsync(x => x.BuocHienTaiId != null);

            db.SangKienXuLy.Add(new Domain.SangKien.SangKienXuLy
            {
                SangKienId = hoSo.Id,
                BuocId = hoSo.BuocHienTaiId!.Value,
                NguoiXuLyId = id,
                ThoiGianNhan = DateTimeOffset.UtcNow,
                ThoiGianXuLy = null
            });

            await db.SaveChangesAsync();
        }

        var phanHoi = await admin.DeleteAsync($"/api/v1/he-thong/nguoi-dung/{id}");

        phanHoi.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "xoá người đang giữ bước sẽ để hồ sơ kẹt lại không ai xử lý tiếp được");
    }

    [Fact]
    public async Task Khong_Co_Quyen_Nguoi_Dung_Xoa_Thi_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var (id, _) = await TaoTaiKhoanTamAsync(admin, "kt.xoa.03");

        // "qtdonvi" quan ly duoc nguoi dung (them/sua/dat lai mat khau) nhung khong co NGUOI_DUNG.XOA.
        var quanTriDonVi = await _ungDung.TaoClientDaDangNhapAsync("qtdonvi");

        var phanHoi = await quanTriDonVi.DeleteAsync($"/api/v1/he-thong/nguoi-dung/{id}");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "quyền xoá tài khoản tách riêng khỏi quyền quản lý tài khoản");
    }

    // ================================================= Chuc nang 23 — xoa ho so nhap

    [Fact]
    public async Task Tac_Gia_Xoa_Duoc_Ho_So_Nhap_Cua_Minh()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var id = await TaoHoSoNhapAsync(tacGia);

        var xoa = await tacGia.DeleteAsync($"/api/v1/sang-kien/{id}");
        xoa.StatusCode.Should().Be(HttpStatusCode.OK);

        var docLai = await tacGia.GetAsync($"/api/v1/sang-kien/{id}");
        docLai.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "hồ sơ đã xoá không được đọc lại qua API");
    }

    /// <summary>
    /// Ho so DA NOP thi khong xoa duoc — duong ra la rut hoac huy.
    ///
    /// Day la ranh gioi quan trong: van ban da vao he thong xet duyet phai de lai dau vet co ly do
    /// va co nguoi chiu trach nhiem, khong duoc bien mat lang le khoi moi bao cao.
    /// </summary>
    [Fact]
    public async Task Khong_Xoa_Duoc_Ho_So_Da_Nop()
    {
        // Dung admin: pham vi TOAN_HE_THONG va co SANG_KIEN.XOA, nen neu bi tu choi thi la do
        // TRANG THAI ho so chu khong phai do thieu quyen hay ngoai pham vi.
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        Guid daNopId;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            daNopId = await db.SangKien.AsNoTracking()
                .Where(x => x.NgayNop != null)
                .Select(x => x.Id)
                .FirstAsync();
        }

        var xoa = await admin.DeleteAsync($"/api/v1/sang-kien/{daNopId}");
        xoa.StatusCode.Should().NotBe(HttpStatusCode.OK);

        // Doc maLoi chu khong doc thongBao: JSON escape ky tu co dau nen so khop chuoi tieng Viet
        // trong than phan hoi la kiem thu don vao chinh cach ma hoa, khong phai vao hanh vi.
        var noiDung = await xoa.Content.ReadFromJsonAsync<JsonElement>();
        noiDung.GetProperty("maLoi").GetString().Should().Be("TRANG_THAI_KHONG_CHO_PHEP_SUA",
            "phải từ chối vì trạng thái hồ sơ, không phải vì thiếu quyền");

        var docLai = await admin.GetAsync($"/api/v1/sang-kien/{daNopId}");
        docLai.StatusCode.Should().Be(HttpStatusCode.OK, "hồ sơ đã nộp phải còn nguyên");
    }

    /// <summary>Ho so nhap cua NGUOI KHAC thi khong dung den duoc, du cung mang quyen SANG_KIEN.XOA.</summary>
    [Fact]
    public async Task Khong_Xoa_Duoc_Ho_So_Nhap_Cua_Tac_Gia_Khac()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var nguoiKhac = await _ungDung.TaoClientDaDangNhapAsync("gv.hung");

        var id = await TaoHoSoNhapAsync(tacGia);

        var xoa = await nguoiKhac.DeleteAsync($"/api/v1/sang-kien/{id}");
        xoa.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);

        var docLai = await tacGia.GetAsync($"/api/v1/sang-kien/{id}");
        docLai.StatusCode.Should().Be(HttpStatusCode.OK, "hồ sơ của tác giả phải còn nguyên");
    }

    // ================================================= Chuc nang 27 — hang cho tiep nhan

    /// <summary>
    /// Endpoint tiep nhan ep trang thai DA_NOP tai may chu.
    ///
    /// Neu no nhan <c>trangThaiTong</c> tu may khach thi <c>TIEP_NHAN.XEM</c> tro thanh duong vong
    /// lay tron danh sach ho so — nguoi chi duoc phep thay hang cho lai xem duoc ca ho so da phe
    /// duyet, da rut, dang cham diem.
    /// </summary>
    [Fact]
    public async Task Hang_Cho_Tiep_Nhan_Ep_Trang_Thai_Tai_May_Chu()
    {
        var tiepNhan = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");

        var phanHoi = await tiepNhan.GetAsync(
            "/api/v1/sang-kien/cho-tiep-nhan?soDong=100&trangThaiTong=DA_PHE_DUYET");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK);

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        var danhSach = noiDung.GetProperty("duLieu").EnumerateArray().ToList();

        danhSach.Should().OnlyContain(
            x => x.GetProperty("trangThaiTong").GetString() == "DA_NOP",
            "tham số trạng thái do máy khách gửi lên phải bị bỏ qua");
    }

    [Fact]
    public async Task Khong_Co_Quyen_Tiep_Nhan_Xem_Thi_Khong_Vao_Duoc_Hang_Cho()
    {
        // "gv.lan" la tac gia: xem duoc ho so cua minh, nhung khong phai can bo tiep nhan.
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.GetAsync("/api/v1/sang-kien/cho-tiep-nhan");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "tiếp nhận là chức năng riêng nên có quyền riêng, không dùng chung SANG_KIEN.XEM");
    }

    // ================================================= Bu quyen cho CSDL cai san

    /// <summary>
    /// Vai tro dang co "quyen anh em" phai duoc cap quyen vua tach ra.
    ///
    /// Khong lam buoc nay thi ngay khi ban moi len, mot chuc nang dang chay tot bong tra 403 cho
    /// tat ca moi nguoi tru quan tri he thong — va khong ai biet vi sao, vi chang ai doi gi ca.
    /// </summary>
    [Fact]
    public async Task Vai_Tro_Co_Quyen_Anh_Em_Duoc_Bu_Quyen_Vua_Tach()
    {
        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var capKiem = new (string QuyenMoi, string QuyenAnhEm)[]
        {
            (Domain.Chung.MaQuyen.DanhMucNhap, Domain.Chung.MaQuyen.DanhMucSua),
            (Domain.Chung.MaQuyen.XuLyUyQuyen, Domain.Chung.MaQuyen.XuLyThucThi),
            (Domain.Chung.MaQuyen.TiepNhanXuLy, Domain.Chung.MaQuyen.TiepNhanXem)
        };

        foreach (var (maMoi, maAnhEm) in capKiem)
        {
            var vaiTroCoAnhEm = await LayVaiTroCoQuyenAsync(db, maAnhEm);
            var vaiTroCoQuyenMoi = await LayVaiTroCoQuyenAsync(db, maMoi);

            vaiTroCoAnhEm.Should().NotBeEmpty($"dữ liệu mẫu phải có vai trò mang {maAnhEm}");

            vaiTroCoQuyenMoi.Should().Contain(vaiTroCoAnhEm,
                $"vai trò đang có {maAnhEm} phải được cấp {maMoi} — "
                + "nếu không thì siết quyền làm hỏng chức năng đang chạy");
        }

        // Danh dau da chay phai duoc ghi lai, khong thi lan khoi dong sau se dap len quyet dinh
        // cua quan tri vien khi ho co y go quyen do ra khoi mot vai tro.
        foreach (var (maMoi, _) in capKiem)
        {
            var daDanhDau = await db.CauHinhHeThong.AsNoTracking()
                .AnyAsync(x => x.Khoa == $"he_thong.da_bu_quyen.{maMoi}");

            daDanhDau.Should().BeTrue(
                $"phải đánh dấu đã bù {maMoi} để không cấp lại mỗi lần khởi động");
        }
    }

    // ---------------------------------------------------------------------

    private static async Task<List<Guid>> LayVaiTroCoQuyenAsync(
        Infrastructure.Persistence.AppDbContext db, string maQuyen)
    {
        var quyenId = await db.Quyen.AsNoTracking()
            .Where(x => x.Ma == maQuyen)
            .Select(x => x.Id)
            .FirstAsync();

        return await db.VaiTroQuyen.AsNoTracking()
            .Where(x => x.QuyenId == quyenId)
            .Select(x => x.VaiTroId)
            .ToListAsync();
    }

    private async Task<(Guid Id, string MatKhauTam)> TaoTaiKhoanTamAsync(
        HttpClient admin, string tenDangNhap)
    {
        Guid donViId;
        Guid vaiTroId;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            donViId = await db.DonVi.AsNoTracking()
                .Where(x => x.DonViChaId == null)
                .Select(x => x.Id)
                .FirstAsync();

            // May chu doi tai khoan phai co it nhat mot vai tro.
            vaiTroId = await db.VaiTro.AsNoTracking()
                .Where(x => x.Ma == Domain.Chung.MaVaiTro.TacGia)
                .Select(x => x.Id)
                .FirstAsync();
        }

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/he-thong/nguoi-dung", new
        {
            tenDangNhap,
            hoTen = "Tài khoản kiểm thử",
            email = $"{tenDangNhap}@kiemthu.local",
            donViId,
            matKhau = MatKhauTam,
            buocDoiMatKhau = false,
            vaiTroIds = new[] { vaiTroId }
        });

        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        var duLieu = noiDung.GetProperty("duLieu");

        // May chu tu sinh mat khau tam va tra ve — khong dung lai mat khau gui len.
        return (duLieu.GetProperty("id").GetGuid(), duLieu.GetProperty("matKhauTam").GetString()!);
    }

    private async Task<Guid> TaoHoSoNhapAsync(HttpClient tacGia)
    {
        var dot = await LayMotIdAsync(tacGia, "/api/v1/danh-muc/dot-de-nghi/dang-mo");
        var linhVuc = await LayMotIdAsync(tacGia, "/api/v1/danh-muc/linh-vuc/chon");

        var phanHoi = await tacGia.PostAsJsonAsync("/api/v1/sang-kien", new
        {
            tenSangKien = $"Hồ sơ kiểm thử xoá nháp {Guid.NewGuid():N}",
            dotDeNghiId = dot,
            linhVucId = linhVuc,
            moTaGiaiPhap = string.Concat(Enumerable.Repeat(
                "Mô tả chi tiết giải pháp phục vụ kiểm thử xoá hồ sơ nháp. ", 8)),
            tinhTrangTruocKhiApDung = string.Concat(Enumerable.Repeat(
                "Trước khi áp dụng phải thao tác thủ công rất mất thời gian. ", 4)),
            noiDungGiaiPhap = string.Concat(Enumerable.Repeat(
                "Nội dung chi tiết của giải pháp được trình bày đầy đủ theo từng bước. ", 10)),
            tinhMoi = string.Concat(Enumerable.Repeat(
                "Tính mới của giải pháp so với cách làm cũ tại đơn vị. ", 4)),
            khaNangApDung = string.Concat(Enumerable.Repeat(
                "Khả năng áp dụng rộng rãi cho các đơn vị tương tự. ", 4)),
            danhSachTacGia = new[]
            {
                new { hoTen = "Nguyễn Văn A", tyLeDongGop = 100, laTacGiaChinh = true }
            }
        });

        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(noiDung.GetProperty("duLieu").GetString()!);
    }

    private static async Task<string> LayMotIdAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu").EnumerateArray().First()
            .GetProperty("id").GetString()!;
    }
}
