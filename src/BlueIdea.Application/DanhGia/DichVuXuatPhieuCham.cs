using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.DanhGia;

/// <summary>Du lieu tho cua mot phieu cham, de tang API dung sinh PDF.</summary>
public sealed record DuLieuPhieuCham(
    string SoPhieu,
    string MaHoSo,
    string TenSangKien,
    string? TenTacGiaChinh,
    string? TenDonVi,
    string TenThanhVien,
    string? ChucDanh,
    string TenHoiDong,
    decimal TongDiem,
    string? KetLuan,
    string? TenMucCongNhanDeXuat,
    string? NhanXetChung,
    string? UuDiem,
    string? HanChe,
    DateTimeOffset? NgayGui,
    IReadOnlyList<DongChiTietPhieu> ChiTiet);

public sealed record DongChiTietPhieu(
    string TenTieuChi, decimal DiemToiDa, decimal Diem, string? NhanXet);

/// <summary>
/// Chuc nang 35 - Lay du lieu phieu cham de xuat PDF (mot phieu hoac hang loat theo ho so/hoi dong).
///
/// Chi lay phieu DA GUI: phieu con o trang thai nhap la ban nhap ca nhan cua thanh vien, xuat ra
/// van ban chinh thuc se lam sai lech ho so nghiem thu.
/// </summary>
public sealed class DichVuXuatPhieuCham
{
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;

    public DichVuXuatPhieuCham(IAppDbContext db, IDichVuPhanQuyen phanQuyen)
    {
        _db = db;
        _phanQuyen = phanQuyen;
    }

    /// <summary>Toan bo phieu da gui cua mot ho so.</summary>
    public Task<IReadOnlyList<DuLieuPhieuCham>> TheoHoSoAsync(
        Guid sangKienId, CancellationToken ct = default)
        => LayAsync(x => x.SangKienId == sangKienId, sangKienId, ct);

    /// <summary>Toan bo phieu da gui cua mot hoi dong - dung khi in ho so phien hop.</summary>
    public Task<IReadOnlyList<DuLieuPhieuCham>> TheoHoiDongAsync(
        Guid hoiDongId, Guid? dotDeNghiId, CancellationToken ct = default)
        => LayAsync(
            x => x.HoiDongId == hoiDongId
                 && (dotDeNghiId == null
                     || _db.SangKien.Any(s => s.Id == x.SangKienId && s.DotDeNghiId == dotDeNghiId)),
            hoiDongId,
            ct);

    /// <summary>Mot phieu cu the — dung khi ky so tung phieu.</summary>
    public Task<IReadOnlyList<DuLieuPhieuCham>> TheoPhieuAsync(
        Guid phieuId, CancellationToken ct = default)
        => LayAsync(x => x.Id == phieuId, phieuId, ct);

    private async Task<IReadOnlyList<DuLieuPhieuCham>> LayAsync(
        System.Linq.Expressions.Expression<Func<Domain.SangKien.PhieuDanhGia, bool>> dieuKien,
        Guid doiTuongId,
        CancellationToken ct)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhGiaXem, ct).ConfigureAwait(false);

        var phieu = await _db.PhieuDanhGia.AsNoTracking()
            .Where(dieuKien)
            .Where(x => x.TrangThaiPhieu == "DA_GUI" || x.TrangThaiPhieu == "DA_KY")
            .Include(x => x.ChiTiet)
            .OrderBy(x => x.SangKienId).ThenBy(x => x.NgayGui)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (phieu.Count == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Không có phiếu chấm nào đã gửi để xuất.");
        }

        var sangKienIds = phieu.Select(x => x.SangKienId).Distinct().ToList();
        var thanhVienIds = phieu.Select(x => x.ThanhVienId).Distinct().ToList();
        var hoiDongIds = phieu.Select(x => x.HoiDongId).Distinct().ToList();
        var mucIds = phieu.Where(x => x.DeXuatMucCongNhanId.HasValue)
            .Select(x => x.DeXuatMucCongNhanId!.Value).Distinct().ToList();

        var hoSo = await _db.SangKien.AsNoTracking()
            .Where(x => sangKienIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.MaHoSo,
                x.TenSangKien,
                TenTacGia = _db.SangKienTacGia
                    .Where(t => t.SangKienId == x.Id && t.LaTacGiaChinh)
                    .Select(t => t.HoTen).FirstOrDefault(),
                TenDonVi = _db.DonVi.Where(d => d.Id == x.DonViId).Select(d => d.Ten).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);

        var thanhVien = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => thanhVienIds.Contains(x.Id))
            .Select(x => new { x.Id, x.HoTenHienThi, x.ChucDanh })
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);

        var hoiDong = await _db.HoiDong.AsNoTracking()
            .Where(x => hoiDongIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct)
            .ConfigureAwait(false);

        var mucCongNhan = await _db.MucCongNhan.AsNoTracking()
            .Where(x => mucIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct)
            .ConfigureAwait(false);

        return phieu.Select(p =>
        {
            var h = hoSo.GetValueOrDefault(p.SangKienId);
            var tv = thanhVien.GetValueOrDefault(p.ThanhVienId);

            return new DuLieuPhieuCham(
                SoPhieu: p.SoPhieu ?? $"PC-{p.Id.ToString()[..8].ToUpperInvariant()}",
                MaHoSo: h?.MaHoSo ?? string.Empty,
                TenSangKien: h?.TenSangKien ?? string.Empty,
                TenTacGiaChinh: h?.TenTacGia,
                TenDonVi: h?.TenDonVi,
                TenThanhVien: tv?.HoTenHienThi ?? string.Empty,
                ChucDanh: tv?.ChucDanh,
                TenHoiDong: hoiDong.GetValueOrDefault(p.HoiDongId) ?? string.Empty,
                TongDiem: p.TongDiem,
                KetLuan: p.KetLuan,
                TenMucCongNhanDeXuat: p.DeXuatMucCongNhanId.HasValue
                    ? mucCongNhan.GetValueOrDefault(p.DeXuatMucCongNhanId.Value)
                    : null,
                NhanXetChung: p.NhanXetChung,
                UuDiem: p.UuDiem,
                HanChe: p.HanChe,
                NgayGui: p.NgayGui,
                ChiTiet: p.ChiTiet
                    .Select(c => new DongChiTietPhieu(
                        c.TenTieuChiSnapshot, c.DiemToiDaSnapshot, c.Diem, c.NhanXet))
                    .ToList());
        }).ToList();
    }
}
