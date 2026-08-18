using System.Linq.Expressions;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.DanhMuc;

/// <summary>Tham so truy van danh sach danh muc (dung chung cho 8 danh muc - chuc nang 1..8).</summary>
public class ThamSoLocDanhMuc : ThamSoPhanTrang
{
    /// <summary>Tu khoa tim kiem - ho tro go khong dau ra ket qua co dau.</summary>
    public string? TuKhoa { get; set; }

    /// <summary>1 hoat dong / 0 ngung. Null = tat ca.</summary>
    public short? TrangThai { get; set; }
}

/// <summary>Dong du lieu rut gon dung cho bang danh sach danh muc.</summary>
public sealed record DanhMucDto(
    Guid Id,
    string Ma,
    string Ten,
    string? MoTa,
    int ThuTu,
    short TrangThai,
    DateTimeOffset NgayTao,
    /// <summary>
    /// Danh muc cap tren, chi co y nghia voi danh muc phan cap (hien tai la linh vuc).
    ///
    /// De o DTO dung chung thay vi tao rieng mot DTO cho linh vuc: cac danh muc khac tra null,
    /// con man hinh danh muc dung chung mot bang nen khong phai re nhanh kieu du lieu.
    /// </summary>
    Guid? DanhMucChaId = null);

/// <summary>Mot vi tri dang tham chieu toi ban ghi - tra ve kem HTTP 409 khi chan xoa.</summary>
public sealed record NoiThamChieu(string Bang, string MoTa, int SoLuong);

/// <summary>
/// Nghiep vu dung chung cho moi bang danh muc: phan trang, tim kiem khong dau,
/// kiem tra trung ma, chan xoa khi dang duoc tham chieu, doi thu tu.
/// </summary>
public abstract class DichVuDanhMucCoSo<T> where T : ThucTheDanhMuc
{
    protected DichVuDanhMucCoSo(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
    {
        Db = db;
        PhanQuyen = phanQuyen;
        DongHo = dongHo;
    }

    protected IAppDbContext Db { get; }

    protected IDichVuPhanQuyen PhanQuyen { get; }

    protected IDongHoHeThong DongHo { get; }

    protected abstract DbSet<T> BangDuLieu { get; }

    /// <summary>Ten hien thi cua danh muc - dung trong thong bao loi.</summary>
    protected abstract string TenDanhMuc { get; }

    /// <summary>Liet ke cac noi dang tham chieu toi ban ghi (de chan xoa).</summary>
    protected virtual Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(Guid id, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<NoiThamChieu>>(Array.Empty<NoiThamChieu>());

    // ------------------------------------------------------------------------------------

    public virtual async Task<PagedResult<DanhMucDto>> LayDanhSachAsync(
        ThamSoLocDanhMuc thamSo, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucXem, ct: ct).ConfigureAwait(false);

        var truyVan = TaoTruyVanCoSo();

        if (thamSo.TrangThai.HasValue)
        {
            truyVan = truyVan.Where(x => x.TrangThai == thamSo.TrangThai.Value);
        }

        truyVan = ApDungTimKiem(truyVan, thamSo.TuKhoa);
        truyVan = ApDungSapXep(truyVan, thamSo);

        var tongSo = await truyVan.CountAsync(ct).ConfigureAwait(false);
        var duLieu = await truyVan
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .Select(x => new DanhMucDto(x.Id, x.Ma, x.Ten, x.MoTa, x.ThuTu, x.TrangThai, x.NgayTao, null))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<DanhMucDto>(duLieu, tongSo, thamSo.Trang, thamSo.SoDong);
    }

    /// <summary>Lay toan bo ban ghi dang hoat dong - dung cho dropdown tren UI.</summary>
    public async Task<IReadOnlyList<DanhMucDto>> LayDanhSachChonAsync(CancellationToken ct = default)
        => await TaoTruyVanCoSo()
            .Where(x => x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .OrderBy(x => x.ThuTu).ThenBy(x => x.Ten)
            .Select(x => new DanhMucDto(x.Id, x.Ma, x.Ten, x.MoTa, x.ThuTu, x.TrangThai, x.NgayTao, null))
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<T> LayTheoIdAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucXem, id, ct).ConfigureAwait(false);

        var banGhi = await TaoTruyVanChiTiet()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);

        return banGhi ?? throw new KhongTimThayException(TenDanhMuc, id);
    }

    public async Task<T> ThemAsync(T banGhi, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucThem, ct: ct).ConfigureAwait(false);
        await BatBuocMaChuaTonTaiAsync(banGhi.Ma, null, ct).ConfigureAwait(false);

        banGhi.Id = banGhi.Id == Guid.Empty ? Guid.NewGuid() : banGhi.Id;
        banGhi.TenKhongDau = VanBanTiengViet.TaoKhongDau(banGhi.Ten);

        if (banGhi.ThuTu == 0)
        {
            banGhi.ThuTu = await LayThuTuKeTiepAsync(ct).ConfigureAwait(false);
        }

        BangDuLieu.Add(banGhi);
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
        return banGhi;
    }

    public async Task<T> CapNhatAsync(Guid id, Action<T> apDungThayDoi, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucSua, id, ct).ConfigureAwait(false);

        var banGhi = await BangDuLieu.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                     ?? throw new KhongTimThayException(TenDanhMuc, id);

        apDungThayDoi(banGhi);

        await BatBuocMaChuaTonTaiAsync(banGhi.Ma, id, ct).ConfigureAwait(false);
        banGhi.TenKhongDau = VanBanTiengViet.TaoKhongDau(banGhi.Ten);

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
        return banGhi;
    }

    /// <summary>Xoa mem. Chan xoa khi ban ghi dang duoc tham chieu (tra ve HTTP 409).</summary>
    public async Task XoaAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucXoa, id, ct).ConfigureAwait(false);

        var banGhi = await BangDuLieu.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                     ?? throw new KhongTimThayException(TenDanhMuc, id);

        var thamChieu = await LayNoiThamChieuAsync(id, ct).ConfigureAwait(false);
        if (thamChieu.Count > 0)
        {
            var moTa = string.Join("; ", thamChieu.Select(t => $"{t.MoTa} ({t.SoLuong})"));
            throw new NghiepVuException(MaLoiHeThong.DangDuocThamChieu,
                $"Không thể xóa vì bản ghi đang được sử dụng tại: {moTa}.");
        }

        banGhi.DaXoa = true;
        banGhi.NgayXoa = DongHo.BayGio;
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DoiTrangThaiAsync(Guid id, short trangThai, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucSua, id, ct).ConfigureAwait(false);

        var banGhi = await BangDuLieu.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                     ?? throw new KhongTimThayException(TenDanhMuc, id);

        banGhi.TrangThai = trangThai;
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Doi thu tu hien thi hang loat (keo tha tren UI).</summary>
    public async Task SapXepAsync(IReadOnlyList<Guid> thuTuMoi, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucSua, ct: ct).ConfigureAwait(false);

        var banGhis = await BangDuLieu
            .Where(x => thuTuMoi.Contains(x.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        for (var i = 0; i < thuTuMoi.Count; i++)
        {
            var banGhi = banGhis.FirstOrDefault(x => x.Id == thuTuMoi[i]);
            if (banGhi is not null)
            {
                banGhi.ThuTu = i + 1;
            }
        }

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------

    protected virtual IQueryable<T> TaoTruyVanCoSo() => BangDuLieu.AsNoTracking();

    protected virtual IQueryable<T> TaoTruyVanChiTiet() => BangDuLieu.AsNoTracking();

    /// <summary>
    /// Tim kiem tren cot <c>ten_khong_dau</c> - nho vay go "sang kien" van ra "sáng kiến".
    /// </summary>
    protected virtual IQueryable<T> ApDungTimKiem(IQueryable<T> truyVan, string? tuKhoa)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa))
        {
            return truyVan;
        }

        var khongDau = VanBanTiengViet.TaoKhongDau(tuKhoa);
        var hoa = tuKhoa.Trim().ToUpperInvariant();

        return truyVan.Where(x =>
            x.TenKhongDau.Contains(khongDau)
            || x.Ma.ToUpper().Contains(hoa));
    }

    protected virtual IQueryable<T> ApDungSapXep(IQueryable<T> truyVan, ThamSoPhanTrang thamSo)
    {
        Expression<Func<T, object>> khoa = (thamSo.SapXep ?? string.Empty).ToLowerInvariant() switch
        {
            "ma" => x => x.Ma,
            "ten" => x => x.Ten,
            "trangthai" => x => x.TrangThai,
            "ngaytao" => x => x.NgayTao,
            _ => x => x.ThuTu
        };

        return thamSo.GiamDan ? truyVan.OrderByDescending(khoa) : truyVan.OrderBy(khoa);
    }

    protected async Task BatBuocMaChuaTonTaiAsync(string ma, Guid? boQuaId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ma))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe, "Mã không được để trống.");
        }

        var daTonTai = await BangDuLieu
            .AnyAsync(x => x.Ma == ma && (boQuaId == null || x.Id != boQuaId), ct)
            .ConfigureAwait(false);

        if (daTonTai)
        {
            throw new NghiepVuException(MaLoiHeThong.TrungMa,
                $"Mã '{ma}' đã tồn tại trong danh mục {TenDanhMuc}.");
        }
    }

    private async Task<int> LayThuTuKeTiepAsync(CancellationToken ct)
    {
        var lonNhat = await BangDuLieu
            .Select(x => (int?)x.ThuTu)
            .MaxAsync(ct)
            .ConfigureAwait(false);

        return (lonNhat ?? 0) + 1;
    }
}
