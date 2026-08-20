using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu ba luong nghiep vu bo sung sau ra soat 20/08/2026:
/// gia han xu ly, huy ho so, va hai loai buoc CONG_BO / BO_PHIEU von khai duoc tren trinh thiet
/// ke nhung may chay quy trinh khong lam gi khac biet.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class LuongDieuPhoiHoSoTests
{
    private readonly UngDungKiemThu _ungDung;

    public LuongDieuPhoiHoSoTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ---------------------------------------------------------------- Gia han

    [Fact]
    public async Task Gia_Han_Doi_Duoc_Han_Cua_Buoc_Dang_Mo()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var (sangKienId, hanCu) = await LayHoSoDangOBuocAsync();

        // Du lieu mau co ho so han da nam trong qua khu — han moi phai sau ca bay gio lan han cu.
        var moc = hanCu is { } h && h > DateTimeOffset.UtcNow ? h : DateTimeOffset.UtcNow;
        var hanMoi = moc.AddDays(5);

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/xu-ly/gia-han", new
        {
            sangKienId,
            hanMoi,
            lyDo = "Cán bộ xử lý nghỉ phép, gia hạn 5 ngày."
        });

        phanHoi.EnsureSuccessStatusCode();

        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var hoSo = await db.SangKien.AsNoTracking().FirstAsync(x => x.Id == sangKienId);

        hoSo.HanXuLyHienTai.Should().NotBeNull();
        hoSo.HanXuLyHienTai!.Value.Should().BeCloseTo(hanMoi, TimeSpan.FromSeconds(2));

        // Luot xu ly dang mo phai doi theo, neu khong thi timeline va co qua han van theo moc cu.
        var luot = await db.SangKienXuLy.AsNoTracking()
            .Where(x => x.SangKienId == sangKienId && x.ThoiGianXuLy == null)
            .OrderByDescending(x => x.ThuTu)
            .FirstOrDefaultAsync();

        if (luot is not null)
        {
            luot.HanXuLy.Should().NotBeNull();
            luot.HanXuLy!.Value.Should().BeCloseTo(hanMoi, TimeSpan.FromSeconds(2));
            luot.QuaHan.Should().BeFalse();
        }

        // Lich su ho so phai ghi lai viec gia han kem ly do.
        var lichSu = await db.SangKienLichSu.AsNoTracking()
            .Where(x => x.SangKienId == sangKienId && x.HanhDong == "GIA_HAN")
            .ToListAsync();

        lichSu.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Gia_Han_Som_Hon_Han_Hien_Tai_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var (sangKienId, hanCu) = await LayHoSoDangOBuocAsync();

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/xu-ly/gia-han", new
        {
            sangKienId,
            hanMoi = (hanCu ?? DateTimeOffset.UtcNow).AddDays(-1),
            lyDo = "Ép tiến độ"
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Gia_Han_Thieu_Ly_Do_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var (sangKienId, _) = await LayHoSoDangOBuocAsync();

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/xu-ly/gia-han", new
        {
            sangKienId,
            hanMoi = DateTimeOffset.UtcNow.AddDays(30),
            lyDo = ""
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Tac_Gia_Khong_Gia_Han_Duoc()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var (sangKienId, _) = await LayHoSoDangOBuocAsync();

        var phanHoi = await tacGia.PostAsJsonAsync("/api/v1/xu-ly/gia-han", new
        {
            sangKienId,
            hanMoi = DateTimeOffset.UtcNow.AddDays(30),
            lyDo = "Xin thêm thời gian"
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------- Huy

    [Fact]
    public async Task Huy_Ho_So_Dat_Trang_Thai_Da_Huy_Va_Dong_Luot_Dang_Mo()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await TaoHoSoDaNopAsync();

        var phanHoi = await admin.PostAsJsonAsync($"/api/v1/sang-kien/{sangKienId}/huy", new
        {
            lyDo = "Tác giả nộp nhầm đợt, đã nộp lại ở đợt đúng."
        });

        phanHoi.EnsureSuccessStatusCode();

        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var hoSo = await db.SangKien.AsNoTracking().FirstAsync(x => x.Id == sangKienId);

        hoSo.TrangThaiTong.Should().Be("DA_HUY");
        hoSo.BuocHienTaiId.Should().BeNull("hồ sơ đã huỷ không còn nằm chờ ở bước nào");
        hoSo.HanXuLyHienTai.Should().BeNull();

        var conMo = await db.SangKienXuLy.AsNoTracking()
            .CountAsync(x => x.SangKienId == sangKienId && x.ThoiGianXuLy == null);

        conMo.Should().Be(0, "không được để hồ sơ đã huỷ nằm trong việc cần xử lý của ai");

        var lichSu = await db.SangKienLichSu.AsNoTracking()
            .CountAsync(x => x.SangKienId == sangKienId && x.HanhDong == "HUY");

        lichSu.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Huy_Lan_Hai_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await TaoHoSoDaNopAsync();

        (await admin.PostAsJsonAsync($"/api/v1/sang-kien/{sangKienId}/huy",
            new { lyDo = "Huỷ lần đầu" })).EnsureSuccessStatusCode();

        var lanHai = await admin.PostAsJsonAsync($"/api/v1/sang-kien/{sangKienId}/huy",
            new { lyDo = "Huỷ lần hai" });

        // TRANG_THAI_KHONG_CHO_PHEP_SUA anh xa sang 409 — xung dot trang thai, khong phai
        // du lieu gui len sai.
        lanHai.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Tac_Gia_Khong_Huy_Duoc_Ho_So()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var sangKienId = await TaoHoSoDaNopAsync();

        var phanHoi = await tacGia.PostAsJsonAsync($"/api/v1/sang-kien/{sangKienId}/huy",
            new { lyDo = "Tôi muốn huỷ" });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------- Loai buoc BO_PHIEU

    [Fact]
    public async Task Buoc_Bo_Phieu_Chua_Kiem_Phieu_Thi_Khong_Ket_Luan_Dat_Duoc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await TaoHoSoDaNopAsync();

        Guid truongHopId;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var hoSo = await db.SangKien.FirstAsync(x => x.Id == sangKienId);
            var quyTrinhId = hoSo.QuyTrinhId!.Value;

            var buoc = new Domain.QuyTrinh.QuyTrinhBuoc
            {
                Id = Guid.NewGuid(),
                QuyTrinhId = quyTrinhId,
                Ma = $"KT_BP_{Guid.NewGuid():N}"[..12],
                Ten = "Bỏ phiếu kiểm thử",
                LoaiBuoc = Domain.Chung.LoaiBuoc.BoPhieu,
                ThuTu = 90
            };

            var truongHop = new Domain.QuyTrinh.QuyTrinhTruongHop
            {
                Id = Guid.NewGuid(),
                BuocId = buoc.Id,
                Ma = Domain.Chung.MaTruongHop.Dat,
                Ten = "Thông qua",
                LaMacDinh = true,
                ThuTu = 1
            };

            db.QuyTrinhBuoc.Add(buoc);
            db.QuyTrinhTruongHop.Add(truongHop);

            hoSo.BuocHienTaiId = buoc.Id;
            await db.SaveChangesAsync();

            truongHopId = truongHop.Id;
        }

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/xu-ly/thuc-thi", new
        {
            sangKienId,
            truongHopId,
            yKien = "Thông qua khi chưa ai bỏ phiếu"
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var loi = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("chưa bỏ phiếu");
    }

    // -------------------------------------------------------- Loai buoc CONG_BO

    [Fact]
    public async Task Qua_Buoc_Cong_Bo_Thi_Ket_Qua_Duoc_Cong_Bo()
    {
        var sangKienId = await TaoHoSoDaNopAsync();

        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var hoSo = await db.SangKien.FirstAsync(x => x.Id == sangKienId);

        var buocCongBo = new Domain.QuyTrinh.QuyTrinhBuoc
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = hoSo.QuyTrinhId!.Value,
            Ma = $"KT_CB_{Guid.NewGuid():N}"[..12],
            Ten = "Công bố kiểm thử",
            LoaiBuoc = Domain.Chung.LoaiBuoc.CongBo,
            ThuTu = 91
        };

        db.QuyTrinhBuoc.Add(buocCongBo);

        hoSo.KetQua = Domain.SangKien.KetQuaXetDuyetGiaTri.Dat;
        hoSo.DaCongBoKetQua = false;
        hoSo.CongKhai = false;

        await db.SaveChangesAsync();

        var dieuPhai = pham.ServiceProvider
            .GetRequiredService<Application.XuLy.DichVuDieuPhaiHanhDong>();

        await dieuPhai.DieuPhaiAsync(sangKienId, new Workflow.MoHinh.KetQuaXuLy
        {
            ThanhCong = true,
            BuocTruocId = buocCongBo.Id,
            TrangThaiTongMoi = "DA_PHE_DUYET"
        }, CancellationToken.None);

        var sau = await db.SangKien.AsNoTracking().FirstAsync(x => x.Id == sangKienId);

        sau.DaCongBoKetQua.Should().BeTrue("đi qua bước loại CONG_BO là kết quả được công bố");
        sau.NgayCongBoKetQua.Should().NotBeNull();
        sau.CongKhai.Should().BeTrue();
    }

    // ---------------------------------------------------------------------

    /// <summary>Mot ho so dang nam o mot buoc xu ly, kem han hien tai (co the null).</summary>
    private async Task<(Guid SangKienId, DateTimeOffset? HanCu)> LayHoSoDangOBuocAsync()
    {
        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var hoSo = await db.SangKien.AsNoTracking()
            .Where(x => x.BuocHienTaiId != null && x.TrangThaiTong != "DA_HUY")
            .OrderBy(x => x.NgayTao)
            .FirstOrDefaultAsync();

        hoSo.Should().NotBeNull("dữ liệu mẫu phải có hồ sơ đang nằm ở một bước xử lý");

        return (hoSo!.Id, hoSo.HanXuLyHienTai);
    }

    /// <summary>
    /// Nhan ban mot ho so da nop de moi kiem thu co ho so rieng — huy la thao tac mot chieu, dung
    /// chung ho so voi kiem thu khac se lam nhung kiem thu do do dinh nhau.
    /// </summary>
    private async Task<Guid> TaoHoSoDaNopAsync()
    {
        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var goc = await db.SangKien.AsNoTracking()
            .Where(x => x.BuocHienTaiId != null && x.TrangThaiTong == "DANG_XU_LY")
            .OrderBy(x => x.NgayTao)
            .FirstOrDefaultAsync()
            ?? await db.SangKien.AsNoTracking()
                .Where(x => x.BuocHienTaiId != null)
                .OrderBy(x => x.NgayTao)
                .FirstAsync();

        var moi = new Domain.SangKien.HoSoSangKien
        {
            Id = Guid.NewGuid(),
            MaHoSo = $"KT-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            TenSangKien = "Hồ sơ kiểm thử điều phối",
            DotDeNghiId = goc.DotDeNghiId,
            LinhVucId = goc.LinhVucId,
            DonViId = goc.DonViId,
            QuyTrinhId = goc.QuyTrinhId,
            QuyTrinhSnapshot = goc.QuyTrinhSnapshot,
            BuocHienTaiId = goc.BuocHienTaiId,
            HanXuLyHienTai = DateTimeOffset.UtcNow.AddDays(3),
            TrangThaiTong = "DANG_XU_LY",
            NgayNop = DateTimeOffset.UtcNow.AddDays(-1),
            NguoiTaoId = goc.NguoiTaoId
        };

        db.SangKien.Add(moi);

        db.SangKienXuLy.Add(new Domain.SangKien.SangKienXuLy
        {
            Id = Guid.NewGuid(),
            SangKienId = moi.Id,
            BuocId = goc.BuocHienTaiId!.Value,
            TenBuocSnapshot = "Bước kiểm thử",
            ThoiGianNhan = DateTimeOffset.UtcNow.AddDays(-1),
            HanXuLy = DateTimeOffset.UtcNow.AddDays(3),
            ThuTu = 1
        });

        await db.SaveChangesAsync();

        return moi.Id;
    }
}
