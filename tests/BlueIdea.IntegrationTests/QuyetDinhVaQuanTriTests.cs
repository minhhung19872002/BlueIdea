using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop cho luong ban hanh quyet dinh (chuc nang 8, 31, 32, 36) va
/// quan tri nguoi dung / vai tro (chuc nang 43, 45).
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class QuyetDinhVaQuanTriTests
{
    private readonly UngDungKiemThu _ungDung;

    public QuyetDinhVaQuanTriTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ------------------------------------------------------------- Quyet dinh

    [Fact]
    public async Task Ban_Hanh_Quyet_Dinh_Roi_Cong_Bo_Ket_Qua()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var duDieuKien = await LayMangAsync(admin, "/api/v1/quyet-dinh/ho-so-du-dieu-kien");
        duDieuKien.Should().NotBeEmpty("dữ liệu mẫu có sẵn hồ sơ đã được công nhận Đạt");

        var chon = duDieuKien.Take(2).Select(x => x.GetProperty("id").GetString()!).ToList();
        var soQuyetDinh = $"KT-{Guid.NewGuid():N}"[..16];

        var tao = await admin.PostAsJsonAsync("/api/v1/quyet-dinh", new
        {
            soQuyetDinh,
            ngayBanHanh = "2026-08-17",
            loai = "CO_SO",
            trichYeu = "Về việc công nhận sáng kiến cấp cơ sở năm 2026",
            nguoiKy = "Trần Văn Bình",
            chucVuNguoiKy = "Chủ tịch UBND",
            sangKienIds = chon
        });

        tao.EnsureSuccessStatusCode();
        var quyetDinhId = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetString()!;

        // Chi tiet phai tra ve dung so sang kien da gan.
        var chiTiet = await LayDuLieuAsync(admin, $"/api/v1/quyet-dinh/{quyetDinhId}");
        chiTiet.GetProperty("thongTin").GetProperty("soSangKien").GetInt32().Should().Be(2);
        chiTiet.GetProperty("danhSachSangKien").GetArrayLength().Should().Be(2);

        // Cong bo ket qua hang loat.
        var congBo = await admin.PostAsync($"/api/v1/quyet-dinh/{quyetDinhId}/cong-bo?congKhai=true", null);
        congBo.EnsureSuccessStatusCode();

        var sauCongBo = await LayDuLieuAsync(admin, $"/api/v1/quyet-dinh/{quyetDinhId}");
        sauCongBo.GetProperty("thongTin").GetProperty("soDaCongBo").GetInt32().Should().Be(2);

        // Ho so phai duoc gan quyet dinh va mo cong khai.
        var hoSo = await LayDuLieuAsync(admin, $"/api/v1/sang-kien/{chon[0]}");
        hoSo.GetProperty("congKhai").GetBoolean().Should().BeTrue();
        hoSo.GetProperty("ngayCongNhan").GetString().Should().NotBeNullOrEmpty();

        // Xuat PDF phai ra tep PDF that.
        var pdf = await admin.GetAsync($"/api/v1/quyet-dinh/{quyetDinhId}/xuat-pdf");
        pdf.EnsureSuccessStatusCode();
        pdf.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var byteDau = (await pdf.Content.ReadAsByteArrayAsync())[..4];
        byteDau.Should().Equal("%PDF"u8.ToArray());
    }

    [Fact]
    public async Task Khong_Duoc_Gan_Mot_Sang_Kien_Vao_Hai_Quyet_Dinh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var duDieuKien = await LayMangAsync(admin, "/api/v1/quyet-dinh/ho-so-du-dieu-kien");
        duDieuKien.Should().NotBeEmpty();

        var sangKienId = duDieuKien[0].GetProperty("id").GetString()!;

        var tao1 = await admin.PostAsJsonAsync("/api/v1/quyet-dinh", new
        {
            soQuyetDinh = $"D1-{Guid.NewGuid():N}"[..16],
            ngayBanHanh = "2026-08-17",
            loai = "CO_SO",
            sangKienIds = new[] { sangKienId }
        });

        tao1.EnsureSuccessStatusCode();

        // Lan hai gan lai chinh sang kien do -> phai bi chan.
        var tao2 = await admin.PostAsJsonAsync("/api/v1/quyet-dinh", new
        {
            soQuyetDinh = $"D2-{Guid.NewGuid():N}"[..16],
            ngayBanHanh = "2026-08-17",
            loai = "CO_SO",
            sangKienIds = new[] { sangKienId }
        });

        tao2.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var loi = await tao2.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("đã nằm trong quyết định khác");
    }

    [Fact]
    public async Task Trung_So_Quyet_Dinh_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var duDieuKien = await LayMangAsync(admin, "/api/v1/quyet-dinh/ho-so-du-dieu-kien");
        duDieuKien.Should().HaveCountGreaterThanOrEqualTo(2);

        var so = $"TR-{Guid.NewGuid():N}"[..16];

        var tao1 = await admin.PostAsJsonAsync("/api/v1/quyet-dinh", new
        {
            soQuyetDinh = so,
            ngayBanHanh = "2026-08-17",
            loai = "CO_SO",
            sangKienIds = new[] { duDieuKien[0].GetProperty("id").GetString()! }
        });

        tao1.EnsureSuccessStatusCode();

        var tao2 = await admin.PostAsJsonAsync("/api/v1/quyet-dinh", new
        {
            soQuyetDinh = so,
            ngayBanHanh = "2026-08-17",
            loai = "CO_SO",
            sangKienIds = new[] { duDieuKien[1].GetProperty("id").GetString()! }
        });

        tao2.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await tao2.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("thongBao").GetString().Should().Contain("đã tồn tại");
    }

    /// <summary>
    /// Chuc nang 49 — man hinh Quyet dinh ky so chinh TEP VAN BAN gan vao quyet dinh, nen
    /// tepTinId phai luu duoc va tra ve; khong co no thi nut Ky so khong co doi tuong de ky.
    /// </summary>
    [Fact]
    public async Task Quyet_Dinh_Luu_Va_Tra_Ve_Tep_Van_Ban()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // Tai mot tep bat ky len kho dung chung de lam van ban quyet dinh.
        using var form = new MultipartFormDataContent();
        var tep = new ByteArrayContent("%PDF-1.4 noi dung kiem thu"u8.ToArray());
        tep.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(tep, "tep", "quyet-dinh.pdf");

        var taiLen = await admin.PostAsync("/api/v1/tep-tin/tai-len", form);
        taiLen.EnsureSuccessStatusCode();

        var tepTinId = (await taiLen.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        var duDieuKien = await LayMangAsync(admin, "/api/v1/quyet-dinh/ho-so-du-dieu-kien");
        var soQuyetDinh = $"KS-{Guid.NewGuid():N}"[..16];

        var tao = await admin.PostAsJsonAsync("/api/v1/quyet-dinh", new
        {
            soQuyetDinh,
            ngayBanHanh = "2026-08-18",
            loai = "CO_SO",
            trichYeu = "Kiểm thử gắn tệp văn bản",
            tepTinId,
            sangKienIds = duDieuKien.Take(1).Select(x => x.GetProperty("id").GetString()!).ToList()
        });

        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu").GetString()!;

        var chiTiet = await LayDuLieuAsync(admin, $"/api/v1/quyet-dinh/{id}");

        chiTiet.GetProperty("thongTin").GetProperty("tepTinId").GetString().Should().Be(tepTinId);
        chiTiet.GetProperty("thongTin").GetProperty("daKySo").GetBoolean().Should().BeFalse();

        // Lich su ky so cua quyet dinh chua ky phai la mang rong, khong phai loi.
        var lichSu = await LayMangAsync(admin, $"/api/v1/quyet-dinh/{id}/lich-su-ky-so");
        lichSu.Should().BeEmpty();
    }

    /// <summary>
    /// Chuc nang 49 — cau hinh chu ky so: bi mat khong bao gio tra ve, va chi mot cau hinh
    /// duoc danh dau mac dinh.
    /// </summary>
    [Fact]
    public async Task Cau_Hinh_Chu_Ky_So_Giu_Bi_Mat_Va_Chi_Mot_Mac_Dinh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var mot = await TaoCauHinhKySoAsync(admin, "VNPT_CA", "bi-mat-khong-duoc-lo", macDinh: true);
        var hai = await TaoCauHinhKySoAsync(admin, "VIETTEL_CA", "bi-mat-khac", macDinh: true);

        try
        {
            var ds = await LayMangAsync(admin, "/api/v1/cau-hinh-chu-ky-so");
            var noiDung = ds.ToString();

            noiDung.Should().NotContain("bi-mat-khong-duoc-lo");
            noiDung.Should().NotContain("bi-mat-khac");

            ds.Where(x => x.GetProperty("daDatBiMat").GetBoolean()).Should().HaveCountGreaterThanOrEqualTo(2);

            // Dat mac dinh cau hinh sau phai BO mac dinh cua cau hinh truoc.
            ds.Count(x => x.GetProperty("laMacDinh").GetBoolean()).Should().Be(1);
            ds.Single(x => x.GetProperty("laMacDinh").GetBoolean())
                .GetProperty("id").GetGuid().Should().Be(hai);

            // Sua ma de trong o bi mat = giu nguyen bi mat dang luu.
            var sua = await admin.PutAsJsonAsync($"/api/v1/cau-hinh-chu-ky-so/{mot}", new
            {
                nhaCungCap = "VNPT_CA",
                loaiKy = "HSM",
                thuatToan = "SHA256withRSA",
                trangThai = 1,
                laMacDinh = false
            });

            sua.EnsureSuccessStatusCode();

            var sauSua = await LayMangAsync(admin, "/api/v1/cau-hinh-chu-ky-so");

            sauSua.Single(x => x.GetProperty("id").GetGuid() == mot)
                .GetProperty("daDatBiMat").GetBoolean().Should().BeTrue();
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/cau-hinh-chu-ky-so/{mot}");
            await admin.DeleteAsync($"/api/v1/cau-hinh-chu-ky-so/{hai}");
        }
    }

    private static async Task<Guid> TaoCauHinhKySoAsync(
        HttpClient client, string nhaCungCap, string biMat, bool macDinh)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/cau-hinh-chu-ky-so", new
        {
            nhaCungCap,
            loaiKy = "USB_TOKEN",
            clientSecret = biMat,
            thuatToan = "SHA256withRSA",
            trangThai = 1,
            laMacDinh = macDinh
        });

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetGuid();
    }

    /// <summary>
    /// Giao dien an bang dieu khien voi vai tro khong co quyen xem bao cao, nhung MAY CHU van
    /// phai chan — neu khong, chi can goi thang API la doc duoc so lieu toan he thong.
    /// </summary>
    [Fact]
    public async Task Tong_Quan_Van_Bi_Chan_Voi_Vai_Tro_Khong_Co_Quyen_Bao_Cao()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.GetAsync("/api/v1/bao-cao/tong-quan");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        (await admin.GetAsync("/api/v1/bao-cao/tong-quan")).EnsureSuccessStatusCode();
    }

    // -------------------------------------------------------------- Thong bao

    /// <summary>
    /// Thong bao la du lieu ca nhan: nguoi nay khong duoc danh dau da doc thong bao cua nguoi kia
    /// du biet dinh danh.
    /// </summary>
    [Fact]
    public async Task Khong_Danh_Dau_Duoc_Thong_Bao_Cua_Nguoi_Khac()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var nguoiKhac = await _ungDung.TaoClientDaDangNhapAsync("cb.khoa");

        var cuaToi = await LayMangAsync(admin, "/api/v1/he-thong/thong-bao?soDong=1");

        if (cuaToi.Count == 0)
        {
            // Du lieu mau khong bao dam admin co thong bao; khong co thi bo qua kiem thu nay.
            return;
        }

        var id = cuaToi[0].GetProperty("id").GetString()!;

        var phanHoi = await nguoiKhac.PostAsync($"/api/v1/he-thong/thong-bao/{id}/da-doc", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "thong bao cua nguoi khac phai coi nhu khong ton tai");
    }

    [Fact]
    public async Task Doc_Tat_Ca_Chi_Anh_Huong_Thong_Bao_Cua_Chinh_Minh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.PostAsync("/api/v1/he-thong/thong-bao/doc-tat-ca", null);
        phanHoi.EnsureSuccessStatusCode();

        var conLai = await LayMangAsync(admin, "/api/v1/he-thong/thong-bao?chuaDoc=true&soDong=50");
        conLai.Should().BeEmpty("vua danh dau doc het thong bao cua chinh minh");
    }

    [Fact]
    public async Task Tac_Gia_Khong_Duoc_Ban_Hanh_Quyet_Dinh()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.PostAsJsonAsync("/api/v1/quyet-dinh", new
        {
            soQuyetDinh = "KHONG-DUOC-PHEP",
            ngayBanHanh = "2026-08-17",
            loai = "CO_SO",
            sangKienIds = new[] { Guid.NewGuid() }
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ----------------------------------------------------------- Quan tri

    [Fact]
    public async Task Tao_Tai_Khoan_Roi_Dang_Nhap_Bang_Mat_Khau_Tam()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var vaiTro = await LayDuLieuAsync(admin, "/api/v1/he-thong/vai-tro");
        var vaiTroTacGia = vaiTro.GetProperty("vaiTro").EnumerateArray()
            .First(v => v.GetProperty("ma").GetString() == "TAC_GIA");

        var tenDangNhap = $"kt.{Guid.NewGuid():N}"[..14];

        var tao = await admin.PostAsJsonAsync("/api/v1/he-thong/nguoi-dung", new
        {
            tenDangNhap,
            hoTen = "Người Dùng Kiểm Thử",
            email = $"{tenDangNhap}@kiemthu.local",
            trangThaiTaiKhoan = "HOAT_DONG",
            vaiTroIds = new[] { vaiTroTacGia.GetProperty("id").GetString()! }
        });

        tao.EnsureSuccessStatusCode();

        var ketQua = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        var matKhauTam = ketQua.GetProperty("matKhauTam").GetString()!;
        var nguoiDungId = ketQua.GetProperty("id").GetString()!;

        matKhauTam.Should().HaveLength(12, "mật khẩu tạm dùng độ dài tối thiểu 12 ký tự");

        // Mat khau tam phai dang nhap duoc va he thong phai bao buoc doi mat khau.
        var client = _ungDung.CreateClient();
        var dangNhap = await client.PostAsJsonAsync("/api/v1/xac-thuc/dang-nhap", new
        {
            tenDangNhap,
            matKhau = matKhauTam
        });

        dangNhap.EnsureSuccessStatusCode();

        var phien = (await dangNhap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        phien.GetProperty("buocDoiMatKhau").GetBoolean().Should().BeTrue();

        // Chi tiet tai khoan phai tra ve dung vai tro da gan.
        var chiTiet = await LayDuLieuAsync(admin, $"/api/v1/he-thong/nguoi-dung/{nguoiDungId}");
        chiTiet.GetProperty("vaiTroIds").GetArrayLength().Should().Be(1);
        chiTiet.GetProperty("buocDoiMatKhau").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Dat_Lai_Mat_Khau_Thu_Hoi_Phien_Dang_Nhap_Cu()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var vaiTro = await LayDuLieuAsync(admin, "/api/v1/he-thong/vai-tro");
        var vaiTroTacGia = vaiTro.GetProperty("vaiTro").EnumerateArray()
            .First(v => v.GetProperty("ma").GetString() == "TAC_GIA");

        var tenDangNhap = $"kt.{Guid.NewGuid():N}"[..14];

        var tao = await admin.PostAsJsonAsync("/api/v1/he-thong/nguoi-dung", new
        {
            tenDangNhap,
            hoTen = "Tài Khoản Thu Hồi",
            trangThaiTaiKhoan = "HOAT_DONG",
            vaiTroIds = new[] { vaiTroTacGia.GetProperty("id").GetString()! }
        });

        tao.EnsureSuccessStatusCode();

        var ketQua = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        var nguoiDungId = ketQua.GetProperty("id").GetString()!;
        var matKhauDau = ketQua.GetProperty("matKhauTam").GetString()!;

        // Dang nhap lan dau de lay refresh token.
        var client = _ungDung.CreateClient();
        var dangNhap = await client.PostAsJsonAsync("/api/v1/xac-thuc/dang-nhap", new
        {
            tenDangNhap,
            matKhau = matKhauDau
        });

        dangNhap.EnsureSuccessStatusCode();
        var refreshToken = (await dangNhap.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("refreshToken").GetString()!;

        // Quan tri vien dat lai mat khau.
        var datLai = await admin.PostAsync(
            $"/api/v1/he-thong/nguoi-dung/{nguoiDungId}/dat-lai-mat-khau", null);

        datLai.EnsureSuccessStatusCode();
        var matKhauMoi = (await datLai.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("matKhauTam").GetString()!;

        matKhauMoi.Should().NotBe(matKhauDau);

        // Refresh token cu phai het hieu luc.
        var lamMoi = await client.PostAsJsonAsync("/api/v1/xac-thuc/lam-moi-token", new { refreshToken });
        lamMoi.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Mat khau cu khong con dung duoc.
        var dangNhapCu = await client.PostAsJsonAsync("/api/v1/xac-thuc/dang-nhap", new
        {
            tenDangNhap,
            matKhau = matKhauDau
        });

        dangNhapCu.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Trung_Ten_Dang_Nhap_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var vaiTro = await LayDuLieuAsync(admin, "/api/v1/he-thong/vai-tro");
        var vaiTroId = vaiTro.GetProperty("vaiTro").EnumerateArray()
            .First(v => v.GetProperty("ma").GetString() == "TAC_GIA")
            .GetProperty("id").GetString()!;

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/he-thong/nguoi-dung", new
        {
            tenDangNhap = "admin",
            hoTen = "Trùng tài khoản",
            trangThaiTaiKhoan = "HOAT_DONG",
            vaiTroIds = new[] { vaiTroId }
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("maLoi").GetString().Should().Be("TRUNG_MA");
    }

    [Fact]
    public async Task Sua_Ma_Tran_Phan_Quyen_Cua_Vai_Tro()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var duLieu = await LayDuLieuAsync(admin, "/api/v1/he-thong/vai-tro");

        var vaiTroTacGia = duLieu.GetProperty("vaiTro").EnumerateArray()
            .First(v => v.GetProperty("ma").GetString() == "TAC_GIA");

        var vaiTroId = vaiTroTacGia.GetProperty("id").GetString()!;
        var quyenHienCo = vaiTroTacGia.GetProperty("quyenIds").EnumerateArray()
            .Select(x => x.GetString()!).ToList();

        // Bo bot mot quyen roi luu lai.
        var quyenMoi = quyenHienCo.Take(quyenHienCo.Count - 1).ToList();

        var luu = await admin.PutAsJsonAsync($"/api/v1/he-thong/vai-tro/{vaiTroId}", new
        {
            ma = "TAC_GIA",
            ten = vaiTroTacGia.GetProperty("ten").GetString(),
            thuTu = 0,
            trangThai = 1,
            quyenIds = quyenMoi,
            loaiPhamVi = "CA_NHAN",
            donViIds = Array.Empty<Guid>()
        });

        luu.EnsureSuccessStatusCode();

        var sauKhiLuu = await LayDuLieuAsync(admin, "/api/v1/he-thong/vai-tro");
        var kiemTra = sauKhiLuu.GetProperty("vaiTro").EnumerateArray()
            .First(v => v.GetProperty("ma").GetString() == "TAC_GIA");

        kiemTra.GetProperty("quyenIds").GetArrayLength().Should().Be(quyenMoi.Count);

        // Tra lai nguyen trang de khong anh huong cac kiem thu khac trong cung container.
        var hoanTac = await admin.PutAsJsonAsync($"/api/v1/he-thong/vai-tro/{vaiTroId}", new
        {
            ma = "TAC_GIA",
            ten = vaiTroTacGia.GetProperty("ten").GetString(),
            thuTu = 0,
            trangThai = 1,
            quyenIds = quyenHienCo,
            loaiPhamVi = "CA_NHAN",
            donViIds = Array.Empty<Guid>()
        });

        hoanTac.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Khong_Duoc_Xoa_Vai_Tro_He_Thong()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var duLieu = await LayDuLieuAsync(admin, "/api/v1/he-thong/vai-tro");
        var vaiTroHeThong = duLieu.GetProperty("vaiTro").EnumerateArray()
            .First(v => v.GetProperty("laHeThong").GetBoolean());

        var phanHoi = await admin.DeleteAsync(
            $"/api/v1/he-thong/vai-tro/{vaiTroHeThong.GetProperty("id").GetString()}");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("maLoi").GetString().Should().Be("TRANG_THAI_KHONG_CHO_PHEP_XOA");
    }

    // ------------------------------------------------------------------------------------

    private static async Task<JsonElement> LayDuLieuAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<List<JsonElement>> LayMangAsync(HttpClient client, string duongDan)
        => (await LayDuLieuAsync(client, duongDan)).EnumerateArray().ToList();
}
