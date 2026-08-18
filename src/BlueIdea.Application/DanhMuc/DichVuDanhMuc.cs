using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.DanhMuc;

/// <summary>Chuc nang 1 - Danh muc linh vuc (co phan cap cha/con).</summary>
public sealed class DichVuLinhVuc : DichVuDanhMucCoSo<LinhVuc>
{
    public DichVuLinhVuc(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
        : base(db, phanQuyen, dongHo)
    {
    }

    protected override DbSet<LinhVuc> BangDuLieu => Db.LinhVuc;

    protected override string TenDanhMuc => "Lĩnh vực";

    /// <summary>Tra ve cay linh vuc (UI hien thi dang Tree).</summary>
    public async Task<IReadOnlyList<NutCay>> LayCayAsync(CancellationToken ct = default)
    {
        var tatCa = await Db.LinhVuc.AsNoTracking()
            .OrderBy(x => x.ThuTu).ThenBy(x => x.Ten)
            .Select(x => new { x.Id, x.Ma, x.Ten, x.LinhVucChaId, x.TrangThai })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nut = tatCa.ToDictionary(
            x => x.Id,
            x => new NutCay(x.Id, x.Ma, x.Ten, x.LinhVucChaId, x.TrangThai, new List<NutCay>()));

        var goc = new List<NutCay>();
        foreach (var x in tatCa)
        {
            if (x.LinhVucChaId.HasValue && nut.TryGetValue(x.LinhVucChaId.Value, out var cha))
            {
                cha.Con.Add(nut[x.Id]);
            }
            else
            {
                goc.Add(nut[x.Id]);
            }
        }

        return goc;
    }

    protected override async Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(
        Guid id, CancellationToken ct)
    {
        var ketQua = new List<NoiThamChieu>();

        var soHoSo = await Db.SangKien.CountAsync(x => x.LinhVucId == id, ct).ConfigureAwait(false);
        if (soHoSo > 0)
        {
            ketQua.Add(new NoiThamChieu("sang_kien", "Hồ sơ sáng kiến", soHoSo));
        }

        var soCon = await Db.LinhVuc.CountAsync(x => x.LinhVucChaId == id, ct).ConfigureAwait(false);
        if (soCon > 0)
        {
            ketQua.Add(new NoiThamChieu("linh_vuc", "Lĩnh vực con", soCon));
        }

        return ketQua;
    }
}

/// <summary>Mot nut trong cay danh muc (linh vuc / don vi).</summary>
public sealed record NutCay(Guid Id, string Ma, string Ten, Guid? ChaId, short TrangThai, List<NutCay> Con);

/// <summary>Chuc nang 2 - Danh muc doi tuong ap dung.</summary>
public sealed class DichVuDoiTuong : DichVuDanhMucCoSo<DoiTuong>
{
    public DichVuDoiTuong(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
        : base(db, phanQuyen, dongHo)
    {
    }

    protected override DbSet<DoiTuong> BangDuLieu => Db.DoiTuong;

    protected override string TenDanhMuc => "Đối tượng";

    protected override async Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(
        Guid id, CancellationToken ct)
    {
        var so = await Db.SangKien.CountAsync(x => x.DoiTuongId == id, ct).ConfigureAwait(false);
        return so > 0
            ? new[] { new NoiThamChieu("sang_kien", "Hồ sơ sáng kiến", so) }
            : Array.Empty<NoiThamChieu>();
    }
}

/// <summary>Chuc nang 4 - Danh muc loai tac gia.</summary>
public sealed class DichVuLoaiTacGia : DichVuDanhMucCoSo<LoaiTacGia>
{
    public DichVuLoaiTacGia(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
        : base(db, phanQuyen, dongHo)
    {
    }

    protected override DbSet<LoaiTacGia> BangDuLieu => Db.LoaiTacGia;

    protected override string TenDanhMuc => "Loại tác giả";

    protected override async Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(
        Guid id, CancellationToken ct)
    {
        var so = await Db.SangKien.CountAsync(x => x.LoaiTacGiaId == id, ct).ConfigureAwait(false);
        return so > 0
            ? new[] { new NoiThamChieu("sang_kien", "Hồ sơ sáng kiến", so) }
            : Array.Empty<NoiThamChieu>();
    }
}

/// <summary>Chuc nang 5, 44, 47 - Co cau to chuc dang cay.</summary>
public sealed class DichVuDonVi : DichVuDanhMucCoSo<DonVi>
{
    public DichVuDonVi(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
        : base(db, phanQuyen, dongHo)
    {
    }

    protected override DbSet<DonVi> BangDuLieu => Db.DonVi;

    protected override string TenDanhMuc => "Đơn vị";

    public async Task<IReadOnlyList<NutCay>> LayCayAsync(CancellationToken ct = default)
    {
        var tatCa = await Db.DonVi.AsNoTracking()
            .OrderBy(x => x.Cap).ThenBy(x => x.ThuTu).ThenBy(x => x.Ten)
            .Select(x => new { x.Id, x.Ma, x.Ten, x.DonViChaId, x.TrangThai })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nut = tatCa.ToDictionary(
            x => x.Id, x => new NutCay(x.Id, x.Ma, x.Ten, x.DonViChaId, x.TrangThai, new List<NutCay>()));

        var goc = new List<NutCay>();
        foreach (var x in tatCa)
        {
            if (x.DonViChaId.HasValue && nut.TryGetValue(x.DonViChaId.Value, out var cha))
            {
                cha.Con.Add(nut[x.Id]);
            }
            else
            {
                goc.Add(nut[x.Id]);
            }
        }

        return goc;
    }

    /// <summary>Lay id don vi va toan bo cap duoi - dung cho pham vi du lieu DON_VI_VA_CAP_DUOI.</summary>
    public async Task<IReadOnlyList<Guid>> LayIdCapDuoiAsync(Guid donViId, CancellationToken ct = default)
    {
        var donVi = await Db.DonVi.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == donViId, ct)
            .ConfigureAwait(false);

        if (donVi is null)
        {
            return Array.Empty<Guid>();
        }

        var tienTo = donVi.Path;
        return await Db.DonVi.AsNoTracking()
            .Where(x => x.Id == donViId || x.Path.StartsWith(tienTo))
            .Select(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>Cap nhat lai <c>Path</c> va <c>Cap</c> cho don vi va toan bo nhanh con.</summary>
    public async Task CapNhatDuongDanCayAsync(Guid donViId, CancellationToken ct = default)
    {
        var tatCa = await Db.DonVi.ToListAsync(ct).ConfigureAwait(false);
        var theoCha = tatCa.ToLookup(x => x.DonViChaId);

        void Duyet(DonVi nut, string pathCha, int cap)
        {
            nut.Path = $"{pathCha}{VanBanTiengViet.TaoSlug(nut.Ma)}/";
            nut.Cap = cap;

            foreach (var con in theoCha[nut.Id])
            {
                Duyet(con, nut.Path, cap + 1);
            }
        }

        var batDau = tatCa.FirstOrDefault(x => x.Id == donViId);
        if (batDau is null)
        {
            return;
        }

        var cha = batDau.DonViChaId.HasValue
            ? tatCa.FirstOrDefault(x => x.Id == batDau.DonViChaId.Value)
            : null;

        Duyet(batDau, cha?.Path ?? "/", (cha?.Cap ?? 0) + 1);
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    protected override async Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(
        Guid id, CancellationToken ct)
    {
        var ketQua = new List<NoiThamChieu>();

        var soNguoiDung = await Db.NguoiDung.CountAsync(x => x.DonViId == id, ct).ConfigureAwait(false);
        if (soNguoiDung > 0)
        {
            ketQua.Add(new NoiThamChieu("nguoi_dung", "Người dùng", soNguoiDung));
        }

        var soHoSo = await Db.SangKien.CountAsync(x => x.DonViId == id, ct).ConfigureAwait(false);
        if (soHoSo > 0)
        {
            ketQua.Add(new NoiThamChieu("sang_kien", "Hồ sơ sáng kiến", soHoSo));
        }

        var soCon = await Db.DonVi.CountAsync(x => x.DonViChaId == id, ct).ConfigureAwait(false);
        if (soCon > 0)
        {
            ketQua.Add(new NoiThamChieu("don_vi", "Đơn vị trực thuộc", soCon));
        }

        return ketQua;
    }
}

/// <summary>Chuc nang 3 - Dot de nghi (co vong doi Mo / Dong / Khoa).</summary>
/// <summary>Mot dot de nghi kem trang thai vong doi, phuc vu man hinh quan tri dot.</summary>
public sealed record DotDeNghiQuanLyDto(
    Guid Id,
    string Ma,
    string Ten,
    int Nam,
    string CapXetDuyet,
    string TrangThaiDot,
    bool TuDongKhoa,
    DateTimeOffset? HanNopHoSo,
    DateTimeOffset? HanChamDiem,
    Guid? QuyTrinhId,
    Guid? BoTieuChiId,
    short TrangThai);

public sealed class DichVuDotDeNghi : DichVuDanhMucCoSo<DotDeNghi>
{
    public DichVuDotDeNghi(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
        : base(db, phanQuyen, dongHo)
    {
    }

    protected override DbSet<DotDeNghi> BangDuLieu => Db.DotDeNghi;

    protected override string TenDanhMuc => "Đợt đề nghị";

    /// <summary>
    /// Danh sach dot kem trang thai vong doi - man hinh quan tri dung de bat/tat nut
    /// Mo / Dong / Khoa dot. Danh sach danh muc chung khong co truong nay.
    /// </summary>
    public async Task<PagedResult<DotDeNghiQuanLyDto>> LayDanhSachQuanLyAsync(
        ThamSoLocDanhMuc thamSo, CancellationToken ct = default)
    {
        var truyVan = Db.DotDeNghi.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(thamSo.TuKhoa))
        {
            var khongDau = VanBanTiengViet.TaoKhongDau(thamSo.TuKhoa);
            truyVan = truyVan.Where(x => x.TenKhongDau.Contains(khongDau)
                                         || x.Ma.ToUpper().Contains(thamSo.TuKhoa.ToUpper()));
        }

        var tongSo = await truyVan.CountAsync(ct).ConfigureAwait(false);

        var duLieu = await truyVan
            .OrderByDescending(x => x.Nam).ThenBy(x => x.ThuTu)
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .Select(x => new DotDeNghiQuanLyDto(
                x.Id, x.Ma, x.Ten, x.Nam, x.CapXetDuyet, x.TrangThaiDot, x.TuDongKhoa,
                x.HanNopHoSo, x.HanChamDiem, x.QuyTrinhId, x.BoTieuChiId, x.TrangThai))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<DotDeNghiQuanLyDto>(duLieu, tongSo, thamSo.Trang, thamSo.SoDong);
    }

    /// <summary>Cac dot dang mo va con han nop - dung cho wizard nop ho so.</summary>
    public async Task<IReadOnlyList<DanhMucDto>> LayDotDangMoAsync(CancellationToken ct = default)
    {
        var bayGio = DongHo.BayGio;

        return await Db.DotDeNghi.AsNoTracking()
            .Where(x => x.TrangThaiDot == TrangThaiDot.DangMo
                        && (x.HanNopHoSo == null || x.HanNopHoSo >= bayGio))
            .OrderByDescending(x => x.Nam).ThenBy(x => x.ThuTu)
            .Select(x => new DanhMucDto(x.Id, x.Ma, x.Ten, x.MoTa, x.ThuTu, x.TrangThai, x.NgayTao))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task DoiTrangThaiDotAsync(Guid id, string trangThaiMoi, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucSua, id, ct).ConfigureAwait(false);

        var dot = await Db.DotDeNghi.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                  ?? throw new KhongTimThayException(TenDanhMuc, id);

        if (dot.TrangThaiDot == TrangThaiDot.DaKhoa)
        {
            throw new NghiepVuException(MaLoiHeThong.DotDeNghiDaKhoa,
                "Đợt đã khóa, không thể thay đổi trạng thái.");
        }

        var hopLe = trangThaiMoi is TrangThaiDot.Nhap or TrangThaiDot.DangMo
            or TrangThaiDot.DaDong or TrangThaiDot.DaKhoa;

        if (!hopLe)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Trạng thái đợt '{trangThaiMoi}' không hợp lệ.");
        }

        if (trangThaiMoi == TrangThaiDot.DangMo)
        {
            if (dot.QuyTrinhId is null || dot.BoTieuChiId is null)
            {
                throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                    "Phải gán quy trình và bộ tiêu chí trước khi mở đợt.");
            }
        }

        dot.TrangThaiDot = trangThaiMoi;
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Sao chep cau hinh dot tu nam truoc (chuc nang 3 - nut "Sao chép đợt").</summary>
    public async Task<DotDeNghi> SaoChepAsync(
        Guid dotNguonId, string maMoi, string tenMoi, int namMoi, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucThem, ct: ct).ConfigureAwait(false);

        var nguon = await Db.DotDeNghi.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dotNguonId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, dotNguonId);

        await BatBuocMaChuaTonTaiAsync(maMoi, null, ct).ConfigureAwait(false);

        var moi = new DotDeNghi
        {
            Id = Guid.NewGuid(),
            Ma = maMoi,
            Ten = tenMoi,
            TenKhongDau = VanBanTiengViet.TaoKhongDau(tenMoi),
            MoTa = nguon.MoTa,
            Nam = namMoi,
            Ky = nguon.Ky,
            CapXetDuyet = nguon.CapXetDuyet,
            QuyTrinhId = nguon.QuyTrinhId,
            BoTieuChiId = nguon.BoTieuChiId,
            DonViApDungIds = nguon.DonViApDungIds.ToList(),
            TuDongKhoa = nguon.TuDongKhoa,
            TrangThaiDot = TrangThaiDot.Nhap,
            ThuTu = nguon.ThuTu,
            TrangThai = TrangThaiDanhMuc.HoatDong
        };

        Db.DotDeNghi.Add(moi);
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
        return moi;
    }

    /// <summary>
    /// Kiem tra dot co cho phep nop/sua ho so tai thoi diem hien tai khong.
    /// Nem loi nghiep vu voi ma loi ro rang de frontend hien thi dung thong bao.
    /// </summary>
    public async Task BatBuocDotChoPhepNopAsync(Guid dotId, CancellationToken ct = default)
    {
        var dot = await Db.DotDeNghi.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dotId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, dotId);

        if (dot.TrangThaiDot == TrangThaiDot.DaKhoa)
        {
            throw new NghiepVuException(MaLoiHeThong.DotDeNghiDaKhoa, $"Đợt '{dot.Ten}' đã khóa.");
        }

        if (dot.TrangThaiDot != TrangThaiDot.DangMo)
        {
            throw new NghiepVuException(MaLoiHeThong.DotDeNghiDaDong,
                $"Đợt '{dot.Ten}' hiện không nhận hồ sơ.");
        }

        if (dot.HanNopHoSo.HasValue && DongHo.BayGio > dot.HanNopHoSo.Value)
        {
            throw new NghiepVuException(MaLoiHeThong.QuaHanNopHoSo,
                $"Đã quá hạn nộp hồ sơ của đợt '{dot.Ten}'.");
        }
    }

    protected override async Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(
        Guid id, CancellationToken ct)
    {
        var so = await Db.SangKien.CountAsync(x => x.DotDeNghiId == id, ct).ConfigureAwait(false);
        return so > 0
            ? new[] { new NoiThamChieu("sang_kien", "Hồ sơ sáng kiến", so) }
            : Array.Empty<NoiThamChieu>();
    }
}

/// <summary>Chuc nang 6 - Bieu mau xuat du lieu.</summary>
public sealed class DichVuBieuMauXuat : DichVuDanhMucCoSo<BieuMauXuat>
{
    public DichVuBieuMauXuat(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
        : base(db, phanQuyen, dongHo)
    {
    }

    protected override DbSet<BieuMauXuat> BangDuLieu => Db.BieuMauXuat;

    protected override string TenDanhMuc => "Biểu mẫu xuất";

    public async Task<IReadOnlyList<BieuMauXuat>> LayTheoLoaiAsync(string loai, CancellationToken ct = default)
        => await Db.BieuMauXuat.AsNoTracking()
            .Where(x => x.Loai == loai && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

/// <summary>Chuc nang 7 - Bieu mau bao cao thong ke.</summary>
public sealed class DichVuBieuMauThongKe : DichVuDanhMucCoSo<BieuMauThongKe>
{
    public DichVuBieuMauThongKe(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
        : base(db, phanQuyen, dongHo)
    {
    }

    protected override DbSet<BieuMauThongKe> BangDuLieu => Db.BieuMauThongKe;

    protected override string TenDanhMuc => "Biểu mẫu thống kê";
}
