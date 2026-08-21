using BlueIdea.Application.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XuLy;

/// <summary>
/// Chuc nang 12 — tra loi mot cau hoi duy nhat: ho so nay co dang bat chuc nang bo sung X khong.
///
/// Truoc day chi <c>BoMayQuyTrinh</c> doc bang chuc nang bo sung, va chi de LOC HANH DONG TU DONG
/// (gui email, ky so, tao bien ban...). Nhung ba chuc nang trong danh sach khong phai hanh dong tu
/// dong ma la CHINH SACH — bo phieu kin, cham diem doc lap, xuat bieu mau — nen khong co gi doc toi
/// chung. Quan tri vien tick tren man hinh thiet ke quy trinh, luu xuong CSDL, va khong co gi xay ra.
///
/// Doc tu SNAPSHOT cua ho so chu khong tu dinh nghia quy trinh hien hanh (ADR 0002): ho so chay
/// bang ban quy trinh chup luc nop. Sua cau hinh giua chung khong duoc doi luat choi cua mot phien
/// hop dang dien ra.
/// </summary>
public sealed class DichVuChucNangBuoc
{
    private readonly IAppDbContext _db;
    private readonly IBoChuyenDoiSnapshotQuyTrinh _snapshot;

    public DichVuChucNangBuoc(IAppDbContext db, IBoChuyenDoiSnapshotQuyTrinh snapshot)
    {
        _db = db;
        _snapshot = snapshot;
    }

    /// <summary>
    /// Ho so co bat chuc nang <paramref name="maChucNang"/> khong.
    ///
    /// Xet ca cau hinh gan cho BUOC HIEN TAI lan cau hinh gan cho TOAN QUY TRINH
    /// (<c>BuocId == null</c>) — cung quy uoc ma <c>BoMayQuyTrinh.LayChucNangBuoc</c> dung, hai noi
    /// lech nhau la cung mot o tick lai co hai nghia khac nhau tuy ai doc.
    /// </summary>
    public async Task<bool> CoBatAsync(Guid sangKienId, string maChucNang, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .Where(x => x.Id == sangKienId)
            .Select(x => new { x.BuocHienTaiId, x.QuyTrinhSnapshot })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (hoSo?.QuyTrinhSnapshot is null)
        {
            return false;
        }

        var quyTrinh = _snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot);

        if (quyTrinh is null)
        {
            return false;
        }

        return CoBat(quyTrinh, hoSo.BuocHienTaiId, maChucNang);
    }

    /// <summary>
    /// Bat cho BAT KY buoc nao cua quy trinh khong.
    ///
    /// Dung khi thao tac khong gan voi buoc hien tai cua ho so. Vi du diem cham: ho so co the da roi
    /// buoc CHAM_DIEM sang buoc hop hoi dong, nhung luat "khong thay diem nguoi khac" phai giu
    /// nguyen — neu khong thi chi can chuyen buoc la lo het diem da cham.
    /// </summary>
    public async Task<bool> CoBatOBatKyBuocNaoAsync(
        Guid sangKienId, string maChucNang, CancellationToken ct)
    {
        var snapshotJson = await _db.SangKien.AsNoTracking()
            .Where(x => x.Id == sangKienId)
            .Select(x => x.QuyTrinhSnapshot)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (snapshotJson is null)
        {
            return false;
        }

        var quyTrinh = _snapshot.DocSnapshot(snapshotJson);

        return quyTrinh?.ChucNangBoSung
            .Any(c => !c.DaXoa && c.MaChucNang == maChucNang) == true;
    }

    /// <summary>
    /// Ho so co duoc phep xuat bieu mau o buoc hien tai khong.
    ///
    /// Quy uoc "khong cau hinh gi thi khong gioi han": quy trinh KHONG co dong XUAT_BIEU_MAU nao
    /// thi xuat binh thuong nhu tu truoc toi nay. Chi khi quan tri vien da chu dong khai bao it
    /// nhat mot dong thi cau hinh do moi co hieu luc, va khi ay chi nhung buoc duoc khai moi xuat
    /// duoc.
    ///
    /// Lam nguoc lai — mac dinh cam, phai bat moi cho — se khoa cung chuc nang xuat tren MOI he
    /// thong dang chay ngay khi ban moi len, vi chua quy trinh nao khai o tick nay. Do la cai gia
    /// khong ai dong y tra cho viec bat mot o tick von chua tung co tac dung.
    /// </summary>
    public async Task<bool> DuocXuatBieuMauAsync(Guid sangKienId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .Where(x => x.Id == sangKienId)
            .Select(x => new { x.BuocHienTaiId, x.QuyTrinhSnapshot })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (hoSo?.QuyTrinhSnapshot is null)
        {
            return true;
        }

        var quyTrinh = _snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot);

        if (quyTrinh is null)
        {
            return true;
        }

        var coKhaiBao = quyTrinh.ChucNangBoSung
            .Any(c => !c.DaXoa && c.MaChucNang == MaChucNangBoSung.XuatBieuMau);

        return !coKhaiBao || CoBat(quyTrinh, hoSo.BuocHienTaiId, MaChucNangBoSung.XuatBieuMau);
    }

    /// <summary>Phien ban dong bo, dung khi da co san doi tuong quy trinh trong tay.</summary>
    public static bool CoBat(QuyTrinh quyTrinh, Guid? buocId, string maChucNang)
        => quyTrinh.ChucNangBoSung
            .Any(c => !c.DaXoa
                      && c.MaChucNang == maChucNang
                      && (c.BuocId == null || c.BuocId == buocId));
}
