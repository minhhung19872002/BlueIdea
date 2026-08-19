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

    /// <summary>
    /// Danh sach linh vuc kem Id linh vuc cap tren.
    ///
    /// Bo sung sau khi goi lop co so thay vi viet lai truy van: giu nguyen mot cho duy nhat xu ly
    /// loc / tim kiem / sap xep / phan trang, chi tra them mot truong ma lop co so khong biet.
    /// </summary>
    public override async Task<PagedResult<DanhMucDto>> LayDanhSachAsync(
        ThamSoLocDanhMuc thamSo, CancellationToken ct = default)
    {
        var ketQua = await base.LayDanhSachAsync(thamSo, ct).ConfigureAwait(false);

        var ids = ketQua.DuLieu.Select(x => x.Id).ToList();

        var cha = await Db.LinhVuc.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.LinhVucChaId != null)
            .ToDictionaryAsync(x => x.Id, x => x.LinhVucChaId, ct)
            .ConfigureAwait(false);

        if (cha.Count == 0) return ketQua;

        var duLieu = ketQua.DuLieu
            .Select(x => cha.TryGetValue(x.Id, out var chaId)
                ? x with { DanhMucChaId = chaId }
                : x)
            .ToList();

        return new PagedResult<DanhMucDto>(duLieu, ketQua.TongSo, ketQua.Trang, ketQua.SoDong);
    }

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
    private readonly INguoiDungHienTai _nguoiDung;

    public DichVuDonVi(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo,
        INguoiDungHienTai nguoiDung)
        : base(db, phanQuyen, dongHo)
    {
        _nguoiDung = nguoiDung;
    }

    protected override DbSet<DonVi> BangDuLieu => Db.DonVi;

    protected override string TenDanhMuc => "Đơn vị";

    protected override string QuyenXem => MaQuyen.DonViXem;
    protected override string QuyenThem => MaQuyen.DonViCauHinh;
    protected override string QuyenSua => MaQuyen.DonViCauHinh;
    protected override string QuyenXoa => MaQuyen.DonViCauHinh;

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
    /// <summary>
    /// Chuyen mot don vi sang don vi cha khac (keo tha tren cay to chuc).
    ///
    /// Chan chuyen vao chinh no hoac vao mot don vi cap duoi cua no: lam vay se cat ca nhanh do
    /// khoi cay va sinh vong lap cha-con, sau do moi truy van "don vi va cap duoi" deu treo.
    /// </summary>
    public async Task ChuyenChaAsync(Guid id, Guid? donViChaMoiId, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DonViCauHinh, id, ct).ConfigureAwait(false);

        await BatBuocDonViTrongPhamViAsync(id, ct).ConfigureAwait(false);
        if (donViChaMoiId.HasValue)
        {
            await BatBuocDonViTrongPhamViAsync(donViChaMoiId.Value, ct).ConfigureAwait(false);
        }

        var donVi = await Db.DonVi.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
            ?? throw new KhongTimThayException("đơn vị", id);

        if (donViChaMoiId == id)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Không đặt một đơn vị làm cấp trên của chính nó.");
        }

        if (donViChaMoiId.HasValue)
        {
            await BatBuocKhongPhaiCapDuoiAsync(id, donViChaMoiId.Value, donVi.Ten, ct)
                .ConfigureAwait(false);
        }

        donVi.DonViChaId = donViChaMoiId;
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);

        // CapNhatDuongDanCayAsync tu duyet xuong toan bo nhanh con nen chi can goi mot lan.
        await CapNhatDuongDanCayAsync(id, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Chan dat <paramref name="chaMoiId"/> lam cap tren cua <paramref name="id"/> khi cha moi
    /// nam trong chinh nhanh cua no.
    ///
    /// Di NGUOC theo DonViChaId chu khong doi chieu <c>Path</c>: path duoc dung bang slug cua ma
    /// don vi, khong chua Id, nen so khop Id vao path se luon truot va vong lap lot qua — luc do
    /// moi lan dung lai cay se de quy vo han.
    /// </summary>
    private async Task BatBuocKhongPhaiCapDuoiAsync(
        Guid id, Guid chaMoiId, string tenDonVi, CancellationToken ct)
    {
        var quanHe = await Db.DonVi.AsNoTracking()
            .Select(x => new { x.Id, x.DonViChaId })
            .ToDictionaryAsync(x => x.Id, x => x.DonViChaId, ct)
            .ConfigureAwait(false);

        if (!quanHe.ContainsKey(chaMoiId))
        {
            throw new KhongTimThayException("đơn vị cấp trên", chaMoiId);
        }

        var daQua = new HashSet<Guid>();
        Guid? hienTai = chaMoiId;

        // daQua cung la chot chan: neu du lieu san co da co vong lap thi thoat thay vi treo.
        while (hienTai.HasValue && daQua.Add(hienTai.Value))
        {
            if (hienTai.Value == id)
            {
                throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                    $"Không chuyển \"{tenDonVi}\" vào đơn vị cấp dưới của chính nó.");
            }

            hienTai = quanHe.GetValueOrDefault(hienTai.Value);
        }
    }

    private async Task BatBuocDonViTrongPhamViAsync(Guid donViId, CancellationToken ct)
    {
        var nguoiGoiId = _nguoiDung.Id
                         ?? throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");

        var phamVi = await PhanQuyen.LayPhamViTruyCapAsync(nguoiGoiId, ct).ConfigureAwait(false);

        if (phamVi.ToanHeThong) return;

        if (phamVi.ChiCaNhan || !phamVi.DonViIds.Contains(donViId))
        {
            throw new KhongTimThayException("đơn vị", donViId);
        }
    }

    /// <summary>
    /// Gop don vi <paramref name="nguonId"/> vao <paramref name="dichId"/> — dung khi sap nhap
    /// don vi hanh chinh.
    ///
    /// Ho so, tai khoan va don vi con duoc CHUYEN sang don vi dich roi moi xoa mem don vi nguon.
    /// Xoa truoc rồi chuyen se de lai ho so tro toi mot don vi khong con ton tai.
    /// </summary>
    public async Task<int> GopAsync(Guid nguonId, Guid dichId, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DonViCauHinh, ct: ct).ConfigureAwait(false);

        await BatBuocDonViTrongPhamViAsync(nguonId, ct).ConfigureAwait(false);
        await BatBuocDonViTrongPhamViAsync(dichId, ct).ConfigureAwait(false);

        if (nguonId == dichId)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Đơn vị nguồn và đơn vị đích phải khác nhau.");
        }

        var nguon = await Db.DonVi.FirstOrDefaultAsync(x => x.Id == nguonId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("đơn vị nguồn", nguonId);

        _ = await Db.DonVi.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dichId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("đơn vị đích", dichId);

        await BatBuocKhongPhaiCapDuoiAsync(nguonId, dichId, nguon.Ten, ct).ConfigureAwait(false);

        var hoSo = await Db.SangKien.Where(x => x.DonViId == nguonId).ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var x in hoSo) x.DonViId = dichId;

        var nguoiDung = await Db.NguoiDung.Where(x => x.DonViId == nguonId).ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var x in nguoiDung) x.DonViId = dichId;

        var con = await Db.DonVi.Where(x => x.DonViChaId == nguonId).ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var x in con) x.DonViChaId = dichId;

        nguon.DaXoa = true;
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var x in con)
        {
            await CapNhatDuongDanCayAsync(x.Id, ct).ConfigureAwait(false);
        }

        return hoSo.Count + nguoiDung.Count + con.Count;
    }

    public async Task CapNhatDuongDanCayAsync(Guid donViId, CancellationToken ct = default)
    {
        var tatCa = await Db.DonVi.ToListAsync(ct).ConfigureAwait(false);
        var theoCha = tatCa.ToLookup(x => x.DonViChaId);

        // Chot chan: du lieu loi co the tao vong cha-con, khong co set nay thi de quy vo han
        // va tien trinh chet vi tran ngan xep thay vi bao loi.
        var daQua = new HashSet<Guid>();

        void Duyet(DonVi nut, string pathCha, int cap)
        {
            if (!daQua.Add(nut.Id)) return;

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

/// <summary>So lieu tong quan cua mot dot — phuc vu man hinh chi tiet dot.</summary>
public sealed record TongQuanDotDto(
    Guid Id,
    string Ma,
    string Ten,
    int Nam,
    string CapXetDuyet,
    string TrangThaiDot,
    DateTimeOffset? HanNopHoSo,
    DateTimeOffset? HanChamDiem,
    string? TenQuyTrinh,
    string? TenBoTieuChi,
    IReadOnlyList<DanhMucDto> DonViApDung,
    int TongHoSo,
    int SoNhap,
    int SoDangXuLy,
    int SoDat,
    int SoKhongDat,
    int SoHoiDong,
    int SoQuyetDinh,
    int SoPhieuDaGui,
    int SoPhieuCanCham);

public sealed class DichVuDotDeNghi : DichVuDanhMucCoSo<DotDeNghi>
{
    public DichVuDotDeNghi(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
        : base(db, phanQuyen, dongHo)
    {
    }

    protected override DbSet<DotDeNghi> BangDuLieu => Db.DotDeNghi;

    protected override string TenDanhMuc => "Đợt đề nghị";

    /// <summary>
    /// So lieu tong quan cua mot dot cho man hinh chi tiet.
    ///
    /// Gom tat ca trong MOT lan goi thay vi de man hinh goi sau bay API roi tu cong: mo mot dot
    /// dang chay se ban ra hang chuc truy van, va cac con so lay o thoi diem khac nhau se lech.
    /// </summary>
    public async Task<TongQuanDotDto> LayTongQuanAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucXem, id, ct).ConfigureAwait(false);

        var dot = await Db.DotDeNghi.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("đợt đề nghị", id);

        var hoSo = Db.SangKien.AsNoTracking().Where(x => x.DotDeNghiId == id);

        var theoTrangThai = await hoSo
            .GroupBy(x => x.TrangThaiTong)
            .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var theoKetQua = await hoSo
            .Where(x => x.KetQua != null)
            .GroupBy(x => x.KetQua!)
            .Select(g => new { KetQua = g.Key, SoLuong = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var donViApDung = dot.DonViApDungIds.Count == 0
            ? new List<DanhMucDto>()
            : await Db.DonVi.AsNoTracking()
                .Where(x => dot.DonViApDungIds.Contains(x.Id))
                .OrderBy(x => x.ThuTu)
                .Select(x => new DanhMucDto(x.Id, x.Ma, x.Ten, x.MoTa, x.ThuTu, x.TrangThai, x.NgayTao, null))
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var soPhieuDaGui = await Db.PhieuDanhGia.AsNoTracking()
            .CountAsync(p => hoSo.Any(h => h.Id == p.SangKienId)
                && (p.TrangThaiPhieu == "DA_GUI" || p.TrangThaiPhieu == "DA_KY"), ct)
            .ConfigureAwait(false);

        var soPhanCong = await Db.SangKienPhanCong.AsNoTracking()
            .CountAsync(p => hoSo.Any(h => h.Id == p.SangKienId), ct)
            .ConfigureAwait(false);

        return new TongQuanDotDto(
            dot.Id, dot.Ma, dot.Ten, dot.Nam, dot.CapXetDuyet, dot.TrangThaiDot,
            dot.HanNopHoSo, dot.HanChamDiem,
            dot.QuyTrinhId is null
                ? null
                : await Db.QuyTrinh.AsNoTracking().Where(x => x.Id == dot.QuyTrinhId)
                    .Select(x => x.Ten).FirstOrDefaultAsync(ct).ConfigureAwait(false),
            dot.BoTieuChiId is null
                ? null
                : await Db.BoTieuChi.AsNoTracking().Where(x => x.Id == dot.BoTieuChiId)
                    .Select(x => x.Ten).FirstOrDefaultAsync(ct).ConfigureAwait(false),
            donViApDung,
            theoTrangThai.Sum(x => x.SoLuong),
            theoTrangThai.FirstOrDefault(x => x.TrangThai == TrangThaiTongHoSo.Nhap)?.SoLuong ?? 0,
            theoTrangThai.FirstOrDefault(x => x.TrangThai == TrangThaiTongHoSo.DangXuLy)?.SoLuong ?? 0,
            theoKetQua.FirstOrDefault(x => x.KetQua == Domain.SangKien.KetQuaXetDuyetGiaTri.Dat)?.SoLuong ?? 0,
            theoKetQua.FirstOrDefault(x => x.KetQua == Domain.SangKien.KetQuaXetDuyetGiaTri.KhongDat)?.SoLuong ?? 0,
            await Db.HoiDong.AsNoTracking().CountAsync(x => x.DotDeNghiId == id, ct)
                .ConfigureAwait(false),
            await Db.QuyetDinh.AsNoTracking().CountAsync(x => x.DotDeNghiId == id, ct)
                .ConfigureAwait(false),
            soPhieuDaGui,
            Math.Max(0, soPhanCong - soPhieuDaGui));
    }

    /// <summary>
    /// Danh sach dot kem trang thai vong doi - man hinh quan tri dung de bat/tat nut
    /// Mo / Dong / Khoa dot. Danh sach danh muc chung khong co truong nay.
    /// </summary>
    public async Task<PagedResult<DotDeNghiQuanLyDto>> LayDanhSachQuanLyAsync(
        ThamSoLocDanhMuc thamSo, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucXem, ct: ct).ConfigureAwait(false);

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
            .Select(x => new DanhMucDto(x.Id, x.Ma, x.Ten, x.MoTa, x.ThuTu, x.TrangThai, x.NgayTao, null))
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
