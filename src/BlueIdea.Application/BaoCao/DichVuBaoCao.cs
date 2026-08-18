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

/// <summary>Mot dong thong ke theo tac gia.</summary>
public sealed record DongBaoCaoTacGia(
    string HoTen,
    string? DonViCongTac,
    string? ChucVu,
    int TongSo,
    int SoDat,
    int SoLaTacGiaChinh,
    decimal? DiemTrungBinh,
    decimal TyLeDat);

/// <summary>Thoi gian xu ly trung binh cua mot buoc trong quy trinh.</summary>
public sealed record DongThoiGianXuLy(
    string TenBuoc,
    int SoLuot,
    decimal SoNgayTrungBinh,
    decimal SoNgayLauNhat,
    int SoLuotQuaHan);

/// <summary>Bao cao tong hop mot nam — dung lam so lieu bao cao cuoi nam.</summary>
public sealed record BaoCaoTongHopNam(
    int Nam,
    int TongHoSo,
    int SoDat,
    int SoKhongDat,
    int SoDangXuLy,
    decimal TyLeDat,
    decimal? TongGiaTriLamLoi,
    int SoTacGia,
    int SoDonViThamGia,
    IReadOnlyList<MucThongKe> TheoLinhVuc,
    IReadOnlyList<MucThongKe> TheoDot,
    IReadOnlyList<MucThongKe> TheoMucCongNhan);

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

        var truyVan = ApDungLocDay(_db.SangKien.AsNoTracking(), thamSo)
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

        var truyVan = ApDungLocDay(_db.SangKien.AsNoTracking(), thamSo)
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
        var truyVan = ApDungLocDay(_db.SangKien.AsNoTracking(), thamSo);

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

    /// <summary>
    /// Thong ke theo TAC GIA. Gom theo ho ten + don vi cong tac chu khong theo tai khoan: nhieu
    /// tac gia khong co tai khoan trong he thong (nguoi ngoai don vi dong tac gia), gom theo
    /// nguoi dung se lam mat han nhom nay khoi bao cao.
    /// </summary>
    public async Task<IReadOnlyList<DongBaoCaoTacGia>> TheoTacGiaAsync(
        ThamSoBaoCao thamSo, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.BaoCaoXem, ct: ct).ConfigureAwait(false);

        var hoSo = ApDungLocDay(_db.SangKien.AsNoTracking(), thamSo);

        var duLieu = await _db.SangKienTacGia.AsNoTracking()
            .Where(t => hoSo.Any(h => h.Id == t.SangKienId))
            .Select(t => new
            {
                t.HoTen,
                t.DonViCongTac,
                t.ChucVu,
                t.LaTacGiaChinh,
                KetQua = _db.SangKien.Where(h => h.Id == t.SangKienId)
                    .Select(h => h.KetQua).FirstOrDefault(),
                Diem = _db.SangKien.Where(h => h.Id == t.SangKienId)
                    .Select(h => h.TongDiem).FirstOrDefault(),
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return duLieu
            .GroupBy(x => new { x.HoTen, x.DonViCongTac })
            .Select(g =>
            {
                var tong = g.Count();
                var dat = g.Count(x => x.KetQua == KetQuaXetDuyetGiaTri.Dat);
                var diem = g.Where(x => x.Diem.HasValue).Select(x => x.Diem!.Value).ToList();

                return new DongBaoCaoTacGia(
                    g.Key.HoTen,
                    g.Key.DonViCongTac,
                    g.Select(x => x.ChucVu).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                    tong,
                    dat,
                    g.Count(x => x.LaTacGiaChinh),
                    diem.Count == 0 ? null : Math.Round(diem.Average(), 2),
                    tong == 0 ? 0 : Math.Round(dat * 100m / tong, 2));
            })
            .OrderByDescending(x => x.SoDat)
            .ThenByDescending(x => x.TongSo)
            .ThenBy(x => x.HoTen, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>
    /// Thoi gian xu ly trung binh theo tung buoc quy trinh, tinh tu nhat ky xu ly.
    ///
    /// Chi tinh cac luot DA HOAN THANH: luot dang mo chua biet ket thuc luc nao, gop vao se keo
    /// trung binh xuong va che mat cac buoc that su cham.
    /// </summary>
    public async Task<IReadOnlyList<DongThoiGianXuLy>> ThoiGianXuLyAsync(
        ThamSoBaoCao thamSo, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.BaoCaoXem, ct: ct).ConfigureAwait(false);

        var hoSo = ApDungLocDay(_db.SangKien.AsNoTracking(), thamSo);

        var luot = await _db.SangKienXuLy.AsNoTracking()
            .Where(x => hoSo.Any(h => h.Id == x.SangKienId))
            .Where(x => x.ThoiGianXuLy != null)
            .Select(x => new
            {
                x.TenBuocSnapshot,
                x.ThoiGianNhan,
                ThoiGianXuLy = x.ThoiGianXuLy!.Value,
                x.SoNgayXuLy,
                x.QuaHan,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return luot
            .GroupBy(x => string.IsNullOrWhiteSpace(x.TenBuocSnapshot)
                ? "(không rõ bước)"
                : x.TenBuocSnapshot)
            .Select(g =>
            {
                // Uu tien SoNgayXuLy da chot luc xu ly; thieu thi tinh lai tu moc thoi gian.
                var ngay = g
                    .Select(x => x.SoNgayXuLy
                        ?? (decimal)(x.ThoiGianXuLy - x.ThoiGianNhan).TotalDays)
                    .ToList();

                return new DongThoiGianXuLy(
                    g.Key,
                    g.Count(),
                    Math.Round(ngay.Average(), 2),
                    Math.Round(ngay.Max(), 2),
                    g.Count(x => x.QuaHan));
            })
            .OrderByDescending(x => x.SoNgayTrungBinh)
            .ToList();
    }

    /// <summary>Bao cao tong hop mot nam — so lieu dua vao bao cao cuoi nam cua don vi.</summary>
    public async Task<BaoCaoTongHopNam> TongHopNamAsync(
        int nam, ThamSoBaoCao thamSo, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.BaoCaoXem, ct: ct).ConfigureAwait(false);

        thamSo.Nam = nam;
        var truyVan = ApDungLocDay(_db.SangKien.AsNoTracking(), thamSo);

        var duLieu = await truyVan
            .Select(x => new
            {
                x.Id,
                x.LinhVucId,
                x.DotDeNghiId,
                x.DonViId,
                x.MucCongNhanId,
                x.KetQua,
                x.TrangThaiTong,
                x.GiaTriLamLoiUocTinh,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ids = duLieu.Select(x => x.Id).ToList();

        var soTacGia = await _db.SangKienTacGia.AsNoTracking()
            .Where(t => ids.Contains(t.SangKienId))
            .Select(t => new { t.HoTen, t.DonViCongTac })
            .Distinct()
            .CountAsync(ct)
            .ConfigureAwait(false);

        var tenLinhVuc = await _db.LinhVuc.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);
        var tenDot = await _db.DotDeNghi.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);
        var tenMuc = await _db.MucCongNhan.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var tong = duLieu.Count;
        var dat = duLieu.Count(x => x.KetQua == KetQuaXetDuyetGiaTri.Dat);

        return new BaoCaoTongHopNam(
            nam,
            tong,
            dat,
            duLieu.Count(x => x.KetQua == KetQuaXetDuyetGiaTri.KhongDat),
            duLieu.Count(x => x.TrangThaiTong == TrangThaiTongHoSo.DangXuLy),
            tong == 0 ? 0 : Math.Round(dat * 100m / tong, 2),
            duLieu.Sum(x => x.GiaTriLamLoiUocTinh),
            soTacGia,
            duLieu.Where(x => x.DonViId.HasValue).Select(x => x.DonViId!.Value).Distinct().Count(),
            duLieu.GroupBy(x => x.LinhVucId)
                .Select(g => new MucThongKe(
                    tenLinhVuc.GetValueOrDefault(g.Key, "(không rõ)"), g.Count()))
                .OrderByDescending(x => x.SoLuong).ToList(),
            duLieu.GroupBy(x => x.DotDeNghiId)
                .Select(g => new MucThongKe(
                    tenDot.GetValueOrDefault(g.Key, "(không rõ)"), g.Count()))
                .OrderByDescending(x => x.SoLuong).ToList(),
            duLieu.Where(x => x.MucCongNhanId.HasValue)
                .GroupBy(x => x.MucCongNhanId!.Value)
                .Select(g => new MucThongKe(
                    tenMuc.GetValueOrDefault(g.Key, "(không rõ)"), g.Count()))
                .OrderByDescending(x => x.SoLuong).ToList());
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

    /// <summary>
    /// Loc theo nam va cap xet duyet — hai truong nay nam tren DOT DE NGHI chu khong tren ho so,
    /// nen phai loc qua bang dot. Truoc day hai tham so nay bi bo qua im lang: nguoi dung chon
    /// nam tren dashboard ma so lieu khong doi.
    /// </summary>
    private IQueryable<HoSoSangKien> ApDungLocDay(
        IQueryable<HoSoSangKien> truyVan, ThamSoBaoCao thamSo)
    {
        truyVan = ApDungLoc(truyVan, thamSo);

        if (thamSo.Nam.HasValue)
        {
            truyVan = truyVan.Where(x =>
                _db.DotDeNghi.Any(d => d.Id == x.DotDeNghiId && d.Nam == thamSo.Nam!.Value));
        }

        if (!string.IsNullOrWhiteSpace(thamSo.CapXetDuyet))
        {
            truyVan = truyVan.Where(x =>
                _db.DotDeNghi.Any(d => d.Id == x.DotDeNghiId
                    && d.CapXetDuyet == thamSo.CapXetDuyet));
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
