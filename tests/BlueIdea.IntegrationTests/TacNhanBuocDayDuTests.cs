using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Bay loai tac nhan deu phai duoc doi xu day du (REQ-15).
///
/// CHUC_DANH_HOI_DONG va LANH_DAO_DON_VI_TAC_GIA truoc day khop quyen duoc — nguoi do bam xu ly
/// van qua — nhung KHONG duoc liet ke va KHONG duoc dem. Hau qua im lang: buoc chi giao cho
/// "Chu tich hoi dong" thi khong ai duoc bao la den luot minh, khong uy quyen cho ho duoc, va
/// quy tac TAT_CA dem nham thanh 1 nen chuyen buoc ngay sau nguoi dau tien.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class TacNhanBuocDayDuTests
{
    private readonly UngDungKiemThu _ungDung;

    public TacNhanBuocDayDuTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Tac_Nhan_Chuc_Danh_Hoi_Dong_Duoc_Liet_Ke()
    {
        var (dichVu, hoSoId, buoc, db) = await ChuanBiAsync();

        var hoiDong = await db.HoiDong.AsNoTracking().FirstAsync();

        var chuTich = await db.HoiDongThanhVien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.HoiDongId == hoiDong.Id
                                      && x.ChucDanh == "CHU_TICH"
                                      && x.NguoiDungId != null);

        chuTich.Should().NotBeNull("dữ liệu mẫu phải có chủ tịch hội đồng");

        buoc.HoiDongId = hoiDong.Id;
        buoc.TacNhan.Clear();
        buoc.TacNhan.Add(new Domain.QuyTrinh.QuyTrinhBuocTacNhan
        {
            Id = Guid.NewGuid(),
            BuocId = buoc.Id,
            LoaiTacNhan = Domain.Chung.LoaiTacNhan.ChucDanhHoiDong,
            ThamChieuMa = "CHU_TICH",
            QuyTacXuLy = Domain.Chung.QuyTacXuLy.MotNguoi
        });

        await LuuSnapshotAsync(db, hoSoId, buoc);

        var danhSach = await dichVu.LayTacNhanBuocHienTaiAsync(hoSoId, CancellationToken.None);

        danhSach.Should().Contain(x => x.Id == chuTich!.NguoiDungId!.Value,
            "chủ tịch hội đồng phải nằm trong danh sách tác nhân của bước");
    }

    [Fact]
    public async Task Tac_Nhan_Lanh_Dao_Don_Vi_Tac_Gia_Duoc_Liet_Ke()
    {
        var (dichVu, hoSoId, buoc, db) = await ChuanBiAsync();

        buoc.HoiDongId = null;
        buoc.TacNhan.Clear();
        buoc.TacNhan.Add(new Domain.QuyTrinh.QuyTrinhBuocTacNhan
        {
            Id = Guid.NewGuid(),
            BuocId = buoc.Id,
            LoaiTacNhan = Domain.Chung.LoaiTacNhan.LanhDaoDonViTacGia,
            QuyTacXuLy = Domain.Chung.QuyTacXuLy.MotNguoi
        });

        await LuuSnapshotAsync(db, hoSoId, buoc);

        var danhSach = await dichVu.LayTacNhanBuocHienTaiAsync(hoSoId, CancellationToken.None);

        // Du lieu mau: tai khoan "lanhdao" thuoc UBND_TP — don vi goc, phu moi ho so.
        var lanhDao = await db.NguoiDung.AsNoTracking()
            .FirstAsync(x => x.TenDangNhap == "lanhdao");

        danhSach.Should().Contain(x => x.Id == lanhDao.Id,
            "lãnh đạo đơn vị cấp trên của tác giả phải nằm trong danh sách tác nhân");
    }

    /// <summary>
    /// Quy tac TAT_CA voi tac nhan chuc danh hoi dong: so tac nhan du kien phai la SO NGUOI mang
    /// chuc danh do, khong phai 1.
    /// </summary>
    [Fact]
    public async Task Quy_Tac_Tat_Ca_Dem_Dung_So_Nguoi_Theo_Chuc_Danh()
    {
        var (dichVu, hoSoId, buoc, db) = await ChuanBiAsync();

        var hoiDong = await db.HoiDong.AsNoTracking().FirstAsync();

        var soUyVien = await db.HoiDongThanhVien.AsNoTracking()
            .CountAsync(x => x.HoiDongId == hoiDong.Id
                             && x.ChucDanh == "UY_VIEN"
                             && x.NguoiDungId != null
                             && x.TrangThai == 1);

        soUyVien.Should().BeGreaterThan(1, "dữ liệu mẫu phải có nhiều uỷ viên để phép đếm có nghĩa");

        buoc.HoiDongId = hoiDong.Id;
        buoc.TacNhan.Clear();
        buoc.TacNhan.Add(new Domain.QuyTrinh.QuyTrinhBuocTacNhan
        {
            Id = Guid.NewGuid(),
            BuocId = buoc.Id,
            LoaiTacNhan = Domain.Chung.LoaiTacNhan.ChucDanhHoiDong,
            ThamChieuMa = "UY_VIEN",
            QuyTacXuLy = Domain.Chung.QuyTacXuLy.TatCa
        });

        await LuuSnapshotAsync(db, hoSoId, buoc);

        var danhSach = await dichVu.LayTacNhanBuocHienTaiAsync(hoSoId, CancellationToken.None);

        danhSach.Count.Should().Be(soUyVien,
            "đếm nhầm thành 1 thì quy tắc TẤT_CẢ chuyển bước ngay sau người đầu tiên");
    }

    // ---------------------------------------------------------------------

    private async Task<(Application.XuLy.DichVuWorkflow DichVu, Guid HoSoId,
        Domain.QuyTrinh.QuyTrinhBuoc Buoc, Infrastructure.Persistence.AppDbContext Db)>
        ChuanBiAsync()
    {
        var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();
        var dichVu = pham.ServiceProvider.GetRequiredService<Application.XuLy.DichVuWorkflow>();

        var hoSo = await db.SangKien
            .FirstAsync(x => x.BuocHienTaiId != null && x.QuyTrinhSnapshot != null);

        var snapshot = pham.ServiceProvider
            .GetRequiredService<Workflow.IBoChuyenDoiSnapshotQuyTrinh>();

        var quyTrinh = snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot)!;
        var buoc = quyTrinh.DanhSachBuoc.First(b => b.Id == hoSo.BuocHienTaiId!.Value);

        return (dichVu, hoSo.Id, buoc, db);
    }

    /// <summary>
    /// Ghi lai snapshot sau khi sua tac nhan cua buoc.
    ///
    /// Ho so chay bang SNAPSHOT chu khong doc quy trinh hien hanh (ADR 0002), nen muon doi tac
    /// nhan cua ho so dang chay thi phai sua chinh snapshot cua no.
    /// </summary>
    private async Task LuuSnapshotAsync(
        Infrastructure.Persistence.AppDbContext db, Guid hoSoId, Domain.QuyTrinh.QuyTrinhBuoc buoc)
    {
        var hoSo = await db.SangKien.FirstAsync(x => x.Id == hoSoId);

        using var pham = _ungDung.Services.CreateScope();
        var snapshot = pham.ServiceProvider
            .GetRequiredService<Workflow.IBoChuyenDoiSnapshotQuyTrinh>();

        var quyTrinh = snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot)!;

        var buocTrongSnapshot = quyTrinh.DanhSachBuoc.First(b => b.Id == buoc.Id);
        buocTrongSnapshot.HoiDongId = buoc.HoiDongId;
        buocTrongSnapshot.TacNhan.Clear();

        foreach (var tn in buoc.TacNhan)
        {
            buocTrongSnapshot.TacNhan.Add(tn);
        }

        hoSo.QuyTrinhSnapshot = snapshot.TaoSnapshot(quyTrinh);
        await db.SaveChangesAsync();
    }
}
