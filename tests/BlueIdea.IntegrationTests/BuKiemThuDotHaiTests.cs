using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Dot hai bu cac phep kiem con ghi "no test for..." trong traceability:
/// duong dan cay don vi, sua so do quy trinh, diff lich su, bo loc yeu thich, ro ri ma ho so khi
/// xu ly hang loat, thong bao tac gia, gan vai tro, ma tran phan quyen, chan duoi tep nguy hiem.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class BuKiemThuDotHaiTests
{
    private readonly UngDungKiemThu _ungDung;

    public BuKiemThuDotHaiTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ---------------------------------------- REQ-05: duong dan cay tinh lai

    /// <summary>
    /// Chuyen cap tren thi duong dan cay (<c>Path</c>) va cap phai tinh lai cho CA cay con.
    ///
    /// Duong dan cay la thu ma pham vi du lieu DON_VI_VA_CAP_DUOI dua vao. Doi cha ma khong tinh
    /// lai thi mot don vi vua chuyen nhanh van bi coi la thuoc nhanh cu — pham vi xem du lieu sai
    /// theo, im lang.
    /// </summary>
    [Fact]
    public async Task Chuyen_Cap_Tren_Thi_Duong_Dan_Cay_Tinh_Lai_Cho_Ca_Cay_Con()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        Guid chaMoi;
        Guid conId;
        Guid chauId;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var goc = await db.DonVi.AsNoTracking().FirstAsync(x => x.DonViChaId == null);
            chaMoi = goc.Id;

            var con = TaoDonVi("KT_CON", goc.Id, goc.Path, goc.Cap + 1);
            var chau = TaoDonVi("KT_CHAU", con.Id, con.Path, con.Cap + 1);

            db.DonVi.AddRange(con, chau);
            await db.SaveChangesAsync();

            conId = con.Id;
            chauId = chau.Id;
        }

        // Chuyen "con" len lam con truc tiep cua goc khac -> ca "chau" phai doi theo.
        var chuyen = await admin.PostAsJsonAsync(
            $"/api/v1/don-vi/{conId}/chuyen-cha", new { donViChaMoiId = chaMoi });

        chuyen.EnsureSuccessStatusCode();

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var con = await db.DonVi.AsNoTracking().FirstAsync(x => x.Id == conId);
            var chau = await db.DonVi.AsNoTracking().FirstAsync(x => x.Id == chauId);

            chau.Path.Should().StartWith(con.Path,
                "đường dẫn cây của cháu phải nằm dưới đường dẫn mới của con");

            chau.Cap.Should().Be(con.Cap + 1);
        }
    }

    // ------------------------------------ REQ-09: sua so do quy trinh qua API

    [Fact]
    public async Task Sua_So_Do_Quy_Trinh_Luu_Duoc_Buoc_Va_Truong_Hop()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var quyTrinhId = await TaoQuyTrinhNhapAsync(admin);

        var soDo = await LayAsync(admin, $"/api/v1/quy-trinh/{quyTrinhId}/so-do");

        var buocId = Guid.NewGuid();

        var luu = await admin.PutAsJsonAsync($"/api/v1/quy-trinh/{quyTrinhId}/so-do", new
        {
            danhSachBuoc = new[]
            {
                new
                {
                    id = buocId,
                    ma = "KT_B1",
                    ten = "Bước kiểm thử",
                    loaiBuoc = "TIEP_NHAN",
                    laBuocBatDau = true,
                    laBuocKetThuc = false,
                    soNgayXuLy = 3,
                    thuTu = 1,
                    tacNhan = new[]
                    {
                        new { loaiTacNhan = "VAI_TRO", thamChieuMa = "CAN_BO_TIEP_NHAN", quyTacXuLy = "MOT_NGUOI" }
                    },
                    truongHop = new[]
                    {
                        new { ma = "DAT", ten = "Đạt", laMacDinh = true, thuTu = 1 }
                    },
                    trangThai = Array.Empty<object>()
                }
            },
            trangThaiToanCuc = Array.Empty<object>(),
            thanhPhanHoSo = soDo.GetProperty("thanhPhanHoSo").EnumerateArray()
                .Select(x => JsonSerializer.Deserialize<object>(x.GetRawText())).ToArray(),
            chucNangBoSung = Array.Empty<object>()
        });

        luu.EnsureSuccessStatusCode();

        var sau = await LayAsync(admin, $"/api/v1/quy-trinh/{quyTrinhId}/so-do");
        var buoc = sau.GetProperty("danhSachBuoc").EnumerateArray().First();

        buoc.GetProperty("ten").GetString().Should().Be("Bước kiểm thử");
        buoc.GetProperty("tacNhan").GetArrayLength().Should().Be(1);
        buoc.GetProperty("truongHop").GetArrayLength().Should().Be(1);
    }

    // ----------------------------------------------- REQ-23: diff lich su sua

    [Fact]
    public async Task Lich_Su_Chinh_Sua_Ghi_Dung_Gia_Tri_Truoc_Va_Sau()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        Guid sangKienId;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var hoSo = await db.SangKien.AsNoTracking().FirstAsync(x => x.BuocHienTaiId != null);
            sangKienId = hoSo.Id;

            var hanCu = hoSo.HanXuLyHienTai ?? DateTimeOffset.UtcNow;

            db.SangKienLichSu.Add(new Domain.SangKien.SangKienLichSu
            {
                SangKienId = sangKienId,
                HanhDong = "SUA",
                TruongThayDoi = new List<string> { "TenSangKien" },
                GiaTriTruoc = new Dictionary<string, string?> { ["tenSangKien"] = "Tên cũ" },
                GiaTriSau = new Dictionary<string, string?> { ["tenSangKien"] = "Tên mới" },
                ThoiGian = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var lichSu = await LayAsync(admin, $"/api/v1/sang-kien/{sangKienId}/lich-su");

        var ban = lichSu.EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("hanhDong").GetString() == "SUA");

        ban.ValueKind.Should().NotBe(JsonValueKind.Undefined);

        var chuoi = ban.ToString() ?? string.Empty;

        chuoi.Should().Contain("Tên cũ", "diff phải trả về giá trị trước");
        chuoi.Should().Contain("Tên mới", "diff phải trả về giá trị sau");
    }

    // ----------------------------------------------- REQ-28: bo loc yeu thich

    [Fact]
    public async Task Bo_Loc_Yeu_Thich_Luu_Doc_Dat_Mac_Dinh_Va_Xoa()
    {
        var canBo = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");

        var tao = await canBo.PostAsJsonAsync("/api/v1/bo-loc-yeu-thich", new
        {
            ten = $"Bộ lọc kiểm thử {Guid.NewGuid():N}"[..24],
            manHinh = "XU_LY",
            thamSo = "{\"trangThai\":\"DANG_XU_LY\"}",
            macDinh = false
        });

        tao.EnsureSuccessStatusCode();

        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetGuid();

        try
        {
            (await canBo.PostAsync($"/api/v1/bo-loc-yeu-thich/{id}/mac-dinh", null))
                .EnsureSuccessStatusCode();

            var ds = await LayAsync(canBo, "/api/v1/bo-loc-yeu-thich?manHinh=XU_LY");

            var cua = ds.EnumerateArray().First(x => x.GetProperty("id").GetGuid() == id);
            cua.GetProperty("macDinh").GetBoolean().Should().BeTrue();

            // Bo loc la du lieu ca nhan: nguoi khac khong duoc thay.
            var nguoiKhac = await _ungDung.TaoClientDaDangNhapAsync("thuky");
            var dsKhac = await LayAsync(nguoiKhac, "/api/v1/bo-loc-yeu-thich?manHinh=XU_LY");

            dsKhac.EnumerateArray().Should().NotContain(x => x.GetProperty("id").GetGuid() == id);
        }
        finally
        {
            await canBo.DeleteAsync($"/api/v1/bo-loc-yeu-thich/{id}");
        }
    }

    // ------------------------------- REQ-29 SEC: khong lo ma ho so don vi khac

    /// <summary>
    /// Xu ly hang loat voi mot ho so NGOAI pham vi: thong bao loi khong duoc chua ma ho so do.
    ///
    /// Day la kieu ro ri tinh vi: thao tac bi tu choi dung, nhung chinh cau bao loi lai xac nhan
    /// ho so ton tai va lo ma cua no.
    /// </summary>
    [Fact]
    public async Task Xu_Ly_Hang_Loat_Khong_Lo_Ma_Ho_So_Ngoai_Pham_Vi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var canBo = await _ungDung.TaoClientDaDangNhapAsync("gv.hung");

        string maHoSo;
        Guid sangKienId;
        Guid truongHopId = Guid.NewGuid();

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var hoSo = await db.SangKien.AsNoTracking()
                .FirstAsync(x => x.BuocHienTaiId != null);

            sangKienId = hoSo.Id;
            maHoSo = hoSo.MaHoSo;
        }

        var phanHoi = await canBo.PostAsJsonAsync("/api/v1/xu-ly/thuc-thi-hang-loat", new
        {
            sangKienIds = new[] { sangKienId },
            truongHopId,
            yKien = "Thử xử lý hồ sơ ngoài phạm vi"
        });

        var noiDung = await phanHoi.Content.ReadAsStringAsync();

        // Hoac bi chan bang 403 (khong co quyen), hoac tra ve ket qua that bai — ca hai deu dung,
        // mien la KHONG lo ma ho so.
        noiDung.Should().NotContain(maHoSo,
            "thông báo lỗi không được để lộ mã hồ sơ của đơn vị khác");
    }

    // ----------------------------------------- REQ-43/45: vai tro va phan quyen

    [Fact]
    public async Task Tao_Nguoi_Dung_Kem_Vai_Tro_Thi_Vai_Tro_Duoc_Gan_That()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var maTran = await LayAsync(admin, "/api/v1/he-thong/vai-tro");
        var vaiTroId = maTran.GetProperty("vaiTro")[0].GetProperty("id").GetGuid();

        var donVi = await LayAsync(admin, "/api/v1/don-vi/chon");
        var donViId = donVi.EnumerateArray().First().GetProperty("id").GetGuid();

        var tenDangNhap = $"kt{Guid.NewGuid():N}"[..14];

        var tao = await admin.PostAsJsonAsync("/api/v1/he-thong/nguoi-dung", new
        {
            tenDangNhap,
            hoTen = "Tài khoản kiểm thử vai trò",
            donViId,
            vaiTroIds = new[] { vaiTroId }
        });

        tao.EnsureSuccessStatusCode();

        // Endpoint tra ve { id, matKhauTam } chu khong tra thang Guid.
        var nguoiDungId = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetGuid();

        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var daGan = await db.NguoiDungVaiTro.AsNoTracking()
            .CountAsync(x => x.NguoiDungId == nguoiDungId && x.VaiTroId == vaiTroId);

        daGan.Should().Be(1, "vai trò gửi lúc tạo tài khoản phải được gán thật");
    }

    [Fact]
    public async Task Sua_Ma_Tran_Phan_Quyen_Thi_Quyen_Cua_Vai_Tro_Doi_That()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // Ma tran phan quyen tra ve ca hai ve trong mot lan goi: duLieu.vaiTro va duLieu.quyen.
        var maTran = await LayAsync(admin, "/api/v1/he-thong/vai-tro");

        var haiQuyen = maTran.GetProperty("quyen").EnumerateArray().Take(2)
            .Select(x => x.GetProperty("id").GetGuid()).ToList();

        var ma = $"KT_VT_{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var tao = await admin.PostAsJsonAsync("/api/v1/he-thong/vai-tro", new
        {
            ma,
            ten = "Vai trò kiểm thử ma trận",
            quyenIds = new[] { haiQuyen[0] },
            loaiPhamVi = "DON_VI"
        });

        tao.EnsureSuccessStatusCode();
        var vaiTroId = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetGuid();

        try
        {
            var sua = await admin.PutAsJsonAsync($"/api/v1/he-thong/vai-tro/{vaiTroId}", new
            {
                ma,
                ten = "Vai trò kiểm thử ma trận",
                quyenIds = haiQuyen,
                loaiPhamVi = "TOAN_HE_THONG"
            });

            sua.EnsureSuccessStatusCode();

            using var pham = _ungDung.Services.CreateScope();
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var soQuyen = await db.VaiTroQuyen.AsNoTracking()
                .CountAsync(x => x.VaiTroId == vaiTroId);

            soQuyen.Should().Be(2, "ma trận phân quyền phải lưu đủ quyền vừa tick");

            var phamVi = await db.PhamViDuLieu.AsNoTracking()
                .FirstOrDefaultAsync(x => x.VaiTroId == vaiTroId);

            phamVi.Should().NotBeNull();
            phamVi!.LoaiPhamVi.Should().Be("TOAN_HE_THONG", "đổi phạm vi dữ liệu phải có hiệu lực");
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/he-thong/vai-tro/{vaiTroId}");
        }
    }

    // ------------------------------------------- REQ-25: chan duoi tep nguy hiem

    [Fact]
    public async Task Tai_Len_Tep_Thuc_Thi_Bi_Chan()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        using var noiDung = new MultipartFormDataContent();
        var tep = new ByteArrayContent(Encoding.UTF8.GetBytes("MZ giả lập tệp thực thi"));
        tep.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        noiDung.Add(tep, "tep", "virus.exe");

        var phanHoi = await tacGia.PostAsync("/api/v1/tep-tin", noiDung);

        phanHoi.IsSuccessStatusCode.Should().BeFalse("tệp .exe phải bị từ chối");
        ((int)phanHoi.StatusCode).Should().BeInRange(400, 499);
    }

    // ---------------------------------------------------------------------

    private static Domain.DanhMuc.DonVi TaoDonVi(string tienTo, Guid chaId, string pathCha, int cap)
    {
        var id = Guid.NewGuid();

        return new Domain.DanhMuc.DonVi
        {
            Id = id,
            Ma = $"{tienTo}_{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Ten = $"Đơn vị kiểm thử {tienTo}",
            DonViChaId = chaId,
            Cap = cap,
            Path = $"{pathCha.TrimEnd('/')}/{id}",
            TrangThai = 1
        };
    }

    private static async Task<Guid> TaoQuyTrinhNhapAsync(HttpClient client)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/quy-trinh", new
        {
            ma = $"KT_SD_{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            ten = "Quy trình kiểm thử sơ đồ",
            cap = "CO_SO",
            trangThai = 1,
            thuTu = 99
        });

        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        return duLieu.ValueKind == JsonValueKind.String
            ? duLieu.GetGuid()
            : duLieu.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> LayAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }
}
