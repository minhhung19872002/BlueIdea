using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.BaoCao;

/// <summary>Tham so loc chung cho cac bao cao.</summary>
public sealed class ThamSoBaoCao
{
    public Guid? DotDeNghiId { get; set; }

    public int? Nam { get; set; }

    public Guid? DonViId { get; set; }

    public Guid? LinhVucId { get; set; }

    public string? CapXetDuyet { get; set; }
}

public sealed record DongBaoCaoSangKien(
    string MaHoSo,
    string TenSangKien,
    string TacGia,
    string? TenDonVi,
    string? TenLinhVuc,
    string? TenDot,
    decimal? TongDiem,
    string? TenMucCongNhan,
    string? KetQua,
    string? LyDo,
    DateOnly? NgayCongNhan,
    string? SoQuyetDinh);

public sealed record DongBaoCaoDonVi(
    string MaDonVi,
    string TenDonVi,
    int TongSo,
    int SoDat,
    int SoKhongDat,
    int SoDangXuLy,
    decimal TyLeDat);

/// <summary>So lieu tong quan cho dashboard.</summary>
public sealed class ThongKeTongQuan
{
    public int TongHoSo { get; init; }

    public int HoSoDangXuLy { get; init; }

    public int HoSoQuaHan { get; init; }

    public int HoSoDat { get; init; }

    public int HoSoKhongDat { get; init; }

    public int HoSoChoTiepNhan { get; init; }

    public decimal TyLeDat { get; init; }

    public int SoCanhBaoTrungLapCao { get; init; }

    public IReadOnlyList<MucThongKe> TheoTrangThai { get; init; } = Array.Empty<MucThongKe>();

    public IReadOnlyList<MucThongKe> TheoLinhVuc { get; init; } = Array.Empty<MucThongKe>();

    public IReadOnlyList<MucThongKe> TopDonVi { get; init; } = Array.Empty<MucThongKe>();

    public IReadOnlyList<MucThongKe> XuHuongTheoNam { get; init; } = Array.Empty<MucThongKe>();
}

public sealed record MucThongKe(string Ten, int SoLuong, decimal? GiaTriPhu = null);

/// <summary>Chuc nang 38-40 + dashboard: bao cao thong ke sang kien.</summary>
public sealed class DichVuBaoCao
{
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDongHoHeThong _dongHo;

    public DichVuBaoCao(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
    {
        _db = db;
        _phanQuyen = phanQuyen;
        _dongHo = dongHo;
    }

    /// <summary>Chuc nang 38 — Danh sach sang kien DAT.</summary>
    public Task<IReadOnlyList<DongBaoCaoSangKien>> SangKienDatAsync(
        ThamSoBaoCao thamSo, CancellationToken ct = default)
        => LayDanhSachAsync(thamSo, KetQuaXetDuyetGiaTri.Dat, ct);

    /// <summary>Chuc nang 39 — Danh sach sang kien CHUA DAT (kem ly do va diem).</summary>
    public Task<IReadOnlyList<DongBaoCaoSangKien>> SangKienChuaDatAsync(
        ThamSoBaoCao thamSo, CancellationToken ct = default)
        => LayDanhSachAsync(thamSo, KetQuaXetDuyetGiaTri.KhongDat, ct);

    private async Task<IReadOnlyList<DongBaoCaoSangKien>> LayDanhSachAsync(
        ThamSoBaoCao thamSo, string ketQua, CancellationToken ct)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.BaoCaoXem, ct: ct).ConfigureAwait(false);

        var truyVan = ApDungLoc(_db.SangKien.AsNoTracking(), thamSo)
            .Where(x => x.KetQua == ketQua);

        var duLieu = await truyVan
            .OrderBy(x => x.MaHoSo)
            .Select(x => new
            {
                x.Id,
                x.MaHoSo,
                x.TenSangKien,
                x.LinhVucId,
                x.DonViId,
                x.DotDeNghiId,
                x.TongDiem,
                x.MucCongNhanId,
                x.KetQua,
                x.NgayCongNhan,
                x.QuyetDinhId,
                TacGia = x.DanhSachTacGia
                    .OrderByDescending(t => t.LaTacGiaChinh)
                    .Select(t => t.HoTen)
                    .ToList()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var tenLinhVuc = await _db.LinhVuc.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);
        var tenDonVi = await _db.DonVi.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);
        var tenDot = await _db.DotDeNghi.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);
        var tenMuc = await _db.MucCongNhan.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);
        var soQuyetDinh = await _db.QuyetDinh.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.SoQuyetDinh, ct).ConfigureAwait(false);

        // Ly do khong dat lay tu ket qua xet duyet cua hoi dong.
        var sangKienIds = duLieu.Select(x => x.Id).ToList();
        var lyDo = await _db.KetQuaXetDuyet.AsNoTracking()
            .Where(x => sangKienIds.Contains(x.SangKienId))
            .GroupBy(x => x.SangKienId)
            .Select(g => new { SangKienId = g.Key, LyDo = g.OrderByDescending(x => x.NgayKetLuan).First().LyDo })
            .ToDictionaryAsync(x => x.SangKienId, x => x.LyDo, ct)
            .ConfigureAwait(false);

        return duLieu.Select(x => new DongBaoCaoSangKien(
            x.MaHoSo,
            x.TenSangKien,
            string.Join(", ", x.TacGia),
            x.DonViId.HasValue ? tenDonVi.GetValueOrDefault(x.DonViId.Value) : null,
            tenLinhVuc.GetValueOrDefault(x.LinhVucId),
            tenDot.GetValueOrDefault(x.DotDeNghiId),
            x.TongDiem,
            x.MucCongNhanId.HasValue ? tenMuc.GetValueOrDefault(x.MucCongNhanId.Value) : null,
            x.KetQua,
            lyDo.GetValueOrDefault(x.Id),
            x.NgayCongNhan,
            x.QuyetDinhId.HasValue ? soQuyetDinh.GetValueOrDefault(x.QuyetDinhId.Value) : null))
            .ToList();
    }

    /// <summary>Chuc nang 40 — Thong ke sang kien theo don vi (phuc vu danh gia thi dua).</summary>
    public async Task<IReadOnlyList<DongBaoCaoDonVi>> TheoDonViAsync(
        ThamSoBaoCao thamSo, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.BaoCaoXem, ct: ct).ConfigureAwait(false);

        var truyVan = ApDungLoc(_db.SangKien.AsNoTracking(), thamSo)
            .Where(x => x.TrangThaiTong != TrangThaiTongHoSo.Nhap);

        var nhom = await truyVan
            .GroupBy(x => x.DonViId)
            .Select(g => new
            {
                DonViId = g.Key,
                TongSo = g.Count(),
                SoDat = g.Count(x => x.KetQua == KetQuaXetDuyetGiaTri.Dat),
                SoKhongDat = g.Count(x => x.KetQua == KetQuaXetDuyetGiaTri.KhongDat),
                SoDangXuLy = g.Count(x => x.KetQua == null
                                          && x.TrangThaiTong != TrangThaiTongHoSo.DaRut)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var donVi = await _db.DonVi.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => new { x.Ma, x.Ten }, ct)
            .ConfigureAwait(false);

        return nhom
            .Select(g =>
            {
                var thongTin = g.DonViId.HasValue ? donVi.GetValueOrDefault(g.DonViId.Value) : null;
                var tyLe = g.TongSo == 0 ? 0m : Math.Round(g.SoDat * 100m / g.TongSo, 2);

                return new DongBaoCaoDonVi(
                    thongTin?.Ma ?? "(chưa xác định)",
                    thongTin?.Ten ?? "(chưa xác định)",
                    g.TongSo, g.SoDat, g.SoKhongDat, g.SoDangXuLy, tyLe);
            })
            .OrderByDescending(x => x.SoDat)
            .ThenByDescending(x => x.TongSo)
            .ToList();
    }

    /// <summary>Dashboard tong quan theo vai tro.</summary>
    public async Task<ThongKeTongQuan> TongQuanAsync(
        ThamSoBaoCao thamSo, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.BaoCaoXem, ct: ct).ConfigureAwait(false);

        var bayGio = _dongHo.BayGio;
        var truyVan = ApDungLoc(_db.SangKien.AsNoTracking(), thamSo);

        var tong = await truyVan.CountAsync(ct).ConfigureAwait(false);
        var dat = await truyVan.CountAsync(x => x.KetQua == KetQuaXetDuyetGiaTri.Dat, ct)
            .ConfigureAwait(false);
        var khongDat = await truyVan.CountAsync(x => x.KetQua == KetQuaXetDuyetGiaTri.KhongDat, ct)
            .ConfigureAwait(false);
        var dangXuLy = await truyVan.CountAsync(x => x.TrangThaiTong == TrangThaiTongHoSo.DangXuLy, ct)
            .ConfigureAwait(false);
        var choTiepNhan = await truyVan.CountAsync(x => x.TrangThaiTong == TrangThaiTongHoSo.DaNop, ct)
            .ConfigureAwait(false);
        var quaHan = await truyVan
            .CountAsync(x => x.HanXuLyHienTai != null && x.HanXuLyHienTai < bayGio, ct)
            .ConfigureAwait(false);
        var trungLapCao = await truyVan.CountAsync(x => x.TyLeTrungLap != null && x.TyLeTrungLap > 40m, ct)
            .ConfigureAwait(false);

        var theoTrangThai = await truyVan
            .GroupBy(x => x.TrangThaiTong)
            .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var theoLinhVucRaw = await truyVan
            .GroupBy(x => x.LinhVucId)
            .Select(g => new { LinhVucId = g.Key, SoLuong = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var tenLinhVuc = await _db.LinhVuc.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var theoDonViRaw = await truyVan
            .Where(x => x.DonViId != null)
            .GroupBy(x => x.DonViId!.Value)
            .Select(g => new
            {
                DonViId = g.Key,
                SoLuong = g.Count(),
                SoDat = g.Count(x => x.KetQua == KetQuaXetDuyetGiaTri.Dat)
            })
            .OrderByDescending(g => g.SoDat)
            .Take(10)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var tenDonVi = await _db.DonVi.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var theoNamRaw = await _db.SangKien.AsNoTracking()
            .Where(x => x.NgayNop != null)
            .Join(_db.DotDeNghi.AsNoTracking(), sk => sk.DotDeNghiId, d => d.Id,
                (sk, d) => new { d.Nam, sk.KetQua })
            .GroupBy(x => x.Nam)
            .Select(g => new
            {
                Nam = g.Key,
                SoLuong = g.Count(),
                SoDat = g.Count(x => x.KetQua == KetQuaXetDuyetGiaTri.Dat)
            })
            .OrderBy(g => g.Nam)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new ThongKeTongQuan
        {
            TongHoSo = tong,
            HoSoDangXuLy = dangXuLy,
            HoSoQuaHan = quaHan,
            HoSoDat = dat,
            HoSoKhongDat = khongDat,
            HoSoChoTiepNhan = choTiepNhan,
            TyLeDat = dat + khongDat == 0 ? 0m : Math.Round(dat * 100m / (dat + khongDat), 2),
            SoCanhBaoTrungLapCao = trungLapCao,
            TheoTrangThai = theoTrangThai
                .Select(x => new MucThongKe(TenTrangThai(x.TrangThai), x.SoLuong))
                .ToList(),
            TheoLinhVuc = theoLinhVucRaw
                .Select(x => new MucThongKe(
                    tenLinhVuc.GetValueOrDefault(x.LinhVucId) ?? "(khác)", x.SoLuong))
                .OrderByDescending(x => x.SoLuong)
                .ToList(),
            TopDonVi = theoDonViRaw
                .Select(x => new MucThongKe(
                    tenDonVi.GetValueOrDefault(x.DonViId) ?? "(khác)", x.SoLuong, x.SoDat))
                .ToList(),
            XuHuongTheoNam = theoNamRaw
                .Select(x => new MucThongKe(x.Nam.ToString(), x.SoLuong, x.SoDat))
                .ToList()
        };
    }

    private static IQueryable<HoSoSangKien> ApDungLoc(
        IQueryable<HoSoSangKien> truyVan, ThamSoBaoCao thamSo)
    {
        if (thamSo.DotDeNghiId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DotDeNghiId == thamSo.DotDeNghiId.Value);
        }

        if (thamSo.LinhVucId.HasValue)
        {
            truyVan = truyVan.Where(x => x.LinhVucId == thamSo.LinhVucId.Value);
        }

        if (thamSo.DonViId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DonViId == thamSo.DonViId.Value);
        }

        return truyVan;
    }

    private static string TenTrangThai(string ma) => ma switch
    {
        TrangThaiTongHoSo.Nhap => "Nháp",
        TrangThaiTongHoSo.DaNop => "Đã nộp",
        TrangThaiTongHoSo.DangXuLy => "Đang xử lý",
        TrangThaiTongHoSo.YeuCauBoSung => "Yêu cầu bổ sung",
        TrangThaiTongHoSo.DaPheDuyet => "Đã phê duyệt",
        TrangThaiTongHoSo.KhongDat => "Không đạt",
        TrangThaiTongHoSo.DaRut => "Đã rút",
        TrangThaiTongHoSo.DaHuy => "Đã hủy",
        _ => ma
    };
}
