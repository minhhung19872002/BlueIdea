using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Chuc nang 12 — chin o tick "chuc nang bo sung" tren buoc quy trinh phai co tac dung that.
///
/// Ba trong so do truoc day khong dong lenh nao doc toi: BO_PHIEU_KIN, CHAM_DIEM_DOC_LAP,
/// XUAT_BIEU_MAU. Quan tri vien tick tren man hinh thiet ke quy trinh, luu xuong CSDL, va khong co
/// gi xay ra — ho tuong minh dang siet mot thu ma thuc te khong siet gi ca.
///
/// (Phan BO_PHIEU_KIN nam trong <c>HoiDongTests</c> vi no di lien mach voi luong phien hop.)
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class ChucNangBoSungCoHieuLucTests
{
    private readonly UngDungKiemThu _ungDung;

    public ChucNangBoSungCoHieuLucTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ============================================ CHAM_DIEM_DOC_LAP

    /// <summary>
    /// Dac ta Muc 5: "thanh vien khong thay diem cua nguoi khac cho den khi Thu ky bam Tong hop
    /// hoac du 100% phieu".
    ///
    /// Truoc day ma tran chi kiem "phieu da gui", nen diem lo ra ngay khi tung nguoi bam gui. Nguoi
    /// chua cham van mo duoc ma tran va nhin diem nhung nguoi da cham — dung dieu ma cham doc lap
    /// sinh ra de ngan. Chu tich hoi dong vua co quyen tong hop vua tu cham diem, nen day khong
    /// phai lo hong ly thuyet.
    /// </summary>
    [Fact]
    public async Task Cham_Diem_Doc_Lap_Giau_Diem_Khi_Chua_Du_Phieu_Va_Chua_Tong_Hop()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var (hoiDongId, sangKienId) = await ChuanBiPhanCongNhieuNguoiAsync();

        // Mot nguoi da gui phieu, nhung chua du 100% va thu ky chua bam Tong hop.
        await GuiMotPhieuAsync(hoiDongId, sangKienId, daGui: true);

        var maTran = await LayMaTranAsync(admin, hoiDongId);
        var dong = TimDong(maTran, sangKienId);

        var soODaCham = dong.GetProperty("diemThanhVien").EnumerateArray()
            .Count(x => x.GetProperty("trangThai").GetString() == "DA_GUI");

        soODaCham.Should().BeGreaterThan(0, "phải có phiếu đã gửi thì phép kiểm mới có nghĩa");

        var soOLoDiem = dong.GetProperty("diemThanhVien").EnumerateArray()
            .Count(x => x.TryGetProperty("diem", out var d) && d.ValueKind != JsonValueKind.Null);

        soOLoDiem.Should().Be(0,
            "chấm điểm độc lập: chưa đủ 100% phiếu và chưa tổng hợp thì không được lộ điểm");

        // Trang thai van phai hien, khong thi thu ky khong biet con ai de nhac.
        dong.GetProperty("diemThanhVien").EnumerateArray()
            .Should().Contain(x => x.GetProperty("trangThai").GetString() == "DA_GUI");
    }

    /// <summary>Tong hop xong thi diem lo ra binh thuong — giau la giau tam, khong phai giau han.</summary>
    [Fact]
    public async Task Tong_Hop_Xong_Thi_Diem_Lo_Ra()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var (hoiDongId, sangKienId) = await ChuanBiPhanCongNhieuNguoiAsync();
        await GuiMotPhieuAsync(hoiDongId, sangKienId, daGui: true);

        var tongHop = await admin.PostAsync(
            $"/api/v1/danh-gia/tong-hop?sangKienId={sangKienId}&hoiDongId={hoiDongId}", null);

        tongHop.EnsureSuccessStatusCode();

        var maTran = await LayMaTranAsync(admin, hoiDongId);
        var dong = TimDong(maTran, sangKienId);

        var soOLoDiem = dong.GetProperty("diemThanhVien").EnumerateArray()
            .Count(x => x.TryGetProperty("diem", out var d) && d.ValueKind != JsonValueKind.Null);

        soOLoDiem.Should().BeGreaterThan(0, "tổng hợp xong thì điểm phải xem lại được");
    }

    /// <summary>
    /// Ho so khong bat CHAM_DIEM_DOC_LAP thi khong bi giau gi — o tick la mot lua chon, khong phai
    /// mot luat cung ap cho moi don vi.
    /// </summary>
    [Fact]
    public async Task Khong_Bat_Cham_Diem_Doc_Lap_Thi_Diem_Hien_Nhu_Cu()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var (hoiDongId, sangKienId) = await ChuanBiPhanCongNhieuNguoiAsync();
        await GuiMotPhieuAsync(hoiDongId, sangKienId, daGui: true);

        await GoChucNangKhoiSnapshotAsync(sangKienId, MaChucNangBoSung.ChamDiemDocLap);

        var maTran = await LayMaTranAsync(admin, hoiDongId);
        var dong = TimDong(maTran, sangKienId);

        var soOLoDiem = dong.GetProperty("diemThanhVien").EnumerateArray()
            .Count(x => x.TryGetProperty("diem", out var d) && d.ValueKind != JsonValueKind.Null);

        soOLoDiem.Should().BeGreaterThan(0,
            "không bật chấm điểm độc lập thì điểm đã gửi hiện như trước");
    }

    // ============================================ XUAT_BIEU_MAU

    /// <summary>
    /// Quy trinh khong khai bao XUAT_BIEU_MAU dong nao thi xuat binh thuong.
    ///
    /// Day la nua quan trong cua phep kiem: neu mac dinh la cam thi ban moi len se khoa cung chuc
    /// nang xuat tren moi he thong dang chay, vi chua quy trinh nao khai o tick nay.
    /// </summary>
    [Fact]
    public async Task Khong_Cau_Hinh_Xuat_Bieu_Mau_Thi_Khong_Gioi_Han()
    {
        var thuKy = await _ungDung.TaoClientDaDangNhapAsync("thuky");

        var sangKienId = await LaySangKienCoPhieuDaGuiAsync();

        await GoChucNangKhoiSnapshotAsync(sangKienId, MaChucNangBoSung.XuatBieuMau);

        var xuat = await thuKy.GetAsync($"/api/v1/nhap-xuat/phieu-cham/ho-so/{sangKienId}");

        xuat.StatusCode.Should().Be(HttpStatusCode.OK,
            "quy trình không khai báo gì thì xuất như từ trước tới nay");
    }

    /// <summary>
    /// Da khai bao XUAT_BIEU_MAU cho MOT buoc khac thi buoc hien tai bi chan.
    ///
    /// Chi khi quan tri vien chu dong khai bao thi cau hinh moi co hieu luc — va khi ay no phai
    /// thuc su chan, khong thi o tick lai tro ve dung tinh trang cu: khai bao duoc ma khong lam gi.
    /// </summary>
    [Fact]
    public async Task Khai_Bao_Xuat_Bieu_Mau_Cho_Buoc_Khac_Thi_Buoc_Hien_Tai_Bi_Chan()
    {
        var thuKy = await _ungDung.TaoClientDaDangNhapAsync("thuky");

        var sangKienId = await LaySangKienCoPhieuDaGuiAsync();

        await DatChucNangChoBuocKhacAsync(sangKienId, MaChucNangBoSung.XuatBieuMau);

        var xuat = await thuKy.GetAsync($"/api/v1/nhap-xuat/phieu-cham/ho-so/{sangKienId}");

        xuat.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "bước hiện tại không được cấu hình cho phép xuất biểu mẫu");
    }

    // ---------------------------------------------------------------------

    private static JsonElement TimDong(JsonElement maTran, Guid sangKienId)
        => maTran.EnumerateArray()
            .Single(x => x.GetProperty("sangKienId").GetString() == sangKienId.ToString());

    private static async Task<JsonElement> LayMaTranAsync(HttpClient client, Guid hoiDongId)
    {
        var phanHoi = await client.GetAsync($"/api/v1/danh-gia/ma-tran-diem?hoiDongId={hoiDongId}");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu");
    }

    /// <summary>
    /// Ho so co it nhat HAI nguoi duoc phan cong cham, de dieu kien "du 100% phieu" khong tu dung
    /// ngay khi mot nguoi gui.
    /// </summary>
    private async Task<(Guid HoiDongId, Guid SangKienId)> ChuanBiPhanCongNhieuNguoiAsync()
    {
        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        // Du lieu mau khong gieo san phan cong (viec do do thu ky lam tren giao dien), nen phep kiem
        // tu dung lay bo do cua minh thay vi phu thuoc vao du lieu ngau nhien con sot lai tu phep
        // kiem khac chay truoc.
        var hoiDong = await db.HoiDong.AsNoTracking().FirstAsync();

        var thanhVienIds = await db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.HoiDongId == hoiDong.Id && x.QuyenChamDiem && x.TrangThai == 1)
            .OrderBy(x => x.ThuTu)
            .Select(x => x.Id)
            .Take(2)
            .ToListAsync();

        thanhVienIds.Count.Should().Be(2,
            "hội đồng mẫu phải có từ 2 thành viên chấm điểm để điều kiện đủ 100% có nghĩa");

        var sangKienId = await db.SangKien.AsNoTracking()
            .Where(x => x.QuyTrinhSnapshot != null)
            .Select(x => x.Id)
            .FirstAsync();

        var daCo = await db.SangKienPhanCong
            .Where(x => x.HoiDongId == hoiDong.Id && x.SangKienId == sangKienId)
            .ToListAsync();

        foreach (var thanhVienId in thanhVienIds.Where(
                     id => daCo.All(pc => pc.ThanhVienId != id)))
        {
            db.SangKienPhanCong.Add(new Domain.SangKien.SangKienPhanCong
            {
                SangKienId = sangKienId,
                HoiDongId = hoiDong.Id,
                ThanhVienId = thanhVienId
            });
        }

        await db.SaveChangesAsync();

        return (hoiDong.Id, sangKienId);
    }

    /// <summary>Ghi thang mot phieu danh gia vao CSDL de khong phu thuoc luong cham diem qua API.</summary>
    private async Task GuiMotPhieuAsync(Guid hoiDongId, Guid sangKienId, bool daGui)
    {
        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        var thanhVienId = await db.SangKienPhanCong.AsNoTracking()
            .Where(x => x.HoiDongId == hoiDongId && x.SangKienId == sangKienId)
            .Select(x => x.ThanhVienId)
            .FirstAsync();

        var phieu = await db.PhieuDanhGia
            .FirstOrDefaultAsync(x => x.HoiDongId == hoiDongId
                                      && x.SangKienId == sangKienId
                                      && x.ThanhVienId == thanhVienId);

        if (phieu is null)
        {
            phieu = new Domain.SangKien.PhieuDanhGia
            {
                HoiDongId = hoiDongId,
                SangKienId = sangKienId,
                ThanhVienId = thanhVienId
            };

            db.PhieuDanhGia.Add(phieu);
        }

        phieu.TongDiem = 85m;
        phieu.TrangThaiPhieu = daGui ? "DA_GUI" : "NHAP";

        // Ho so nay phai chua tong hop, khong thi diem lo ra vi ly do khac.
        var ketQua = await db.KetQuaXetDuyet
            .FirstOrDefaultAsync(x => x.HoiDongId == hoiDongId && x.SangKienId == sangKienId);

        if (ketQua is not null)
        {
            ketQua.NgayKetLuan = null;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Ho so co it nhat mot phieu da gui — khong thi endpoint xuat khong co gi de xuat.</summary>
    private async Task<Guid> LaySangKienCoPhieuDaGuiAsync()
    {
        var (hoiDongId, sangKienId) = await ChuanBiPhanCongNhieuNguoiAsync();
        await GuiMotPhieuAsync(hoiDongId, sangKienId, daGui: true);

        return sangKienId;
    }

    /// <summary>
    /// Go mot chuc nang bo sung khoi SNAPSHOT cua ho so.
    ///
    /// Sua snapshot chu khong sua bang quy trinh: ho so chay bang ban quy trinh chup luc nop
    /// (ADR 0002), nen doi dinh nghia hien hanh khong anh huong gi toi no.
    /// </summary>
    private async Task GoChucNangKhoiSnapshotAsync(Guid sangKienId, string maChucNang)
        => await SuaSnapshotAsync(sangKienId, (quyTrinh, _) =>
        {
            foreach (var cn in quyTrinh.ChucNangBoSung.Where(c => c.MaChucNang == maChucNang))
            {
                cn.DaXoa = true;
            }
        });

    /// <summary>Gan chuc nang cho mot buoc KHAC buoc hien tai cua ho so.</summary>
    private async Task DatChucNangChoBuocKhacAsync(Guid sangKienId, string maChucNang)
        => await SuaSnapshotAsync(sangKienId, (quyTrinh, buocHienTaiId) =>
        {
            foreach (var cn in quyTrinh.ChucNangBoSung.Where(c => c.MaChucNang == maChucNang))
            {
                cn.DaXoa = true;
            }

            var buocKhac = quyTrinh.DanhSachBuoc.FirstOrDefault(b => b.Id != buocHienTaiId)
                           ?? quyTrinh.DanhSachBuoc.First();

            quyTrinh.ChucNangBoSung.Add(new QuyTrinhChucNangBoSung
            {
                Id = Guid.NewGuid(),
                QuyTrinhId = quyTrinh.Id,
                BuocId = buocKhac.Id,
                MaChucNang = maChucNang,
                BatBuoc = false
            });
        });

    private async Task SuaSnapshotAsync(Guid sangKienId, Action<QuyTrinh, Guid?> sua)
    {
        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();
        var boSnapshot = pham.ServiceProvider
            .GetRequiredService<Workflow.IBoChuyenDoiSnapshotQuyTrinh>();

        var hoSo = await db.SangKien.FirstAsync(x => x.Id == sangKienId);

        var quyTrinh = boSnapshot.DocSnapshot(hoSo.QuyTrinhSnapshot);

        quyTrinh.Should().NotBeNull("hồ sơ phải có snapshot quy trình để phép kiểm có nghĩa");

        // Ho so chua vao buoc nao thi phep kiem "buoc khac" khong con nghia — gan tam buoc dau.
        hoSo.BuocHienTaiId ??= quyTrinh!.DanhSachBuoc.First().Id;

        sua(quyTrinh!, hoSo.BuocHienTaiId);

        hoSo.QuyTrinhSnapshot = boSnapshot.TaoSnapshot(quyTrinh!);
        await db.SaveChangesAsync();
    }
}
