using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ThucTheQuyetDinh = BlueIdea.Domain.DanhMuc.QuyetDinh;
using ThucTheQuyetDinhSangKien = BlueIdea.Domain.DanhMuc.QuyetDinhSangKien;

namespace BlueIdea.Application.BanHanh;

public sealed record HoSoDuDieuKienDto(
    Guid Id,
    string MaHoSo,
    string TenSangKien,
    string? TenTacGiaChinh,
    string? TenDonVi,
    string? TenLinhVuc,
    decimal? TongDiem,
    Guid? MucCongNhanId,
    string? TenMucCongNhan);

public sealed record QuyetDinhDto(
    Guid Id,
    string SoQuyetDinh,
    DateOnly NgayBanHanh,
    string Loai,
    string? TrichYeu,
    string? NguoiKy,
    string? ChucVuNguoiKy,
    Guid? DonViBanHanhId,
    string? TenDonViBanHanh,
    Guid? DotDeNghiId,
    string? TenDot,
    Guid? TepTinId,
    bool DaKySo,
    int SoSangKien,
    int SoDaCongBo);

public sealed record QuyetDinhChiTietDto(
    QuyetDinhDto ThongTin,
    IReadOnlyList<HoSoDuDieuKienDto> DanhSachSangKien);

public sealed record LuuQuyetDinhDto
{
    public string SoQuyetDinh { get; init; } = string.Empty;

    public DateOnly NgayBanHanh { get; init; }

    public string Loai { get; init; } = CapXetDuyet.CoSo;

    public string? TrichYeu { get; init; }

    public string? NguoiKy { get; init; }

    public string? ChucVuNguoiKy { get; init; }

    public Guid? DonViBanHanhId { get; init; }

    public Guid? DotDeNghiId { get; init; }

    /// <summary>Tep van ban quyet dinh da ban hanh — la doi tuong duoc ky so (chuc nang 49).</summary>
    public Guid? TepTinId { get; init; }

    public List<Guid> SangKienIds { get; init; } = new();
}

/// <summary>
/// Chuc nang 8, 31, 36, 32 - Ban hanh quyet dinh cong nhan sang kien va cong bo ket qua.
///
/// Rang buoc nghiep vu quan trong: mot sang kien chi duoc gan vao DUNG MOT quyet dinh cong nhan.
/// Cho phep gan trung se sinh ra hai so quyet dinh cho cung mot sang kien - sai ve mat hanh chinh.
/// </summary>
public sealed class DichVuQuyetDinh
{
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuThongBao _thongBao;
    private readonly IDichVuNhatKy _nhatKy;
    private readonly ILogger<DichVuQuyetDinh> _logger;

    public DichVuQuyetDinh(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, INguoiDungHienTai nguoiDung,
        IDongHoHeThong dongHo, IDichVuThongBao thongBao, IDichVuNhatKy nhatKy,
        ILogger<DichVuQuyetDinh> logger)
    {
        _db = db;
        _phanQuyen = phanQuyen;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
        _thongBao = thongBao;
        _nhatKy = nhatKy;
        _logger = logger;
    }

    /// <summary>Danh sach quyet dinh co loc va phan trang.</summary>
    public async Task<PagedResult<QuyetDinhDto>> DanhSachAsync(
        int trang, int soDong, string? tuKhoa, Guid? dotDeNghiId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhXem, ct: ct).ConfigureAwait(false);

        var truyVan = _db.QuyetDinh.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            // Quyet dinh khong co cot *_khong_dau nen so khop truc tiep, khong phan biet HOA/thuong.
            // So quyet dinh la chuoi ASCII nen tim theo so luon dung; trich yeu tim theo dung chinh ta.
            var hoa = tuKhoa.Trim().ToUpperInvariant();

            truyVan = truyVan.Where(x =>
                x.SoQuyetDinh.ToUpper().Contains(hoa)
                || (x.TrichYeu != null && x.TrichYeu.ToUpper().Contains(hoa)));
        }

        if (dotDeNghiId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DotDeNghiId == dotDeNghiId.Value);
        }

        var tongSo = await truyVan.CountAsync(ct).ConfigureAwait(false);

        var duLieu = await truyVan
            .OrderByDescending(x => x.NgayBanHanh).ThenByDescending(x => x.NgayTao)
            .Skip((trang - 1) * soDong)
            .Take(soDong)
            .Select(x => new
            {
                ThongTin = x,
                TenDonVi = _db.DonVi.Where(d => d.Id == x.DonViBanHanhId).Select(d => d.Ten).FirstOrDefault(),
                TenDot = _db.DotDeNghi.Where(d => d.Id == x.DotDeNghiId).Select(d => d.Ten).FirstOrDefault(),
                SoSangKien = _db.QuyetDinhSangKien.Count(q => q.QuyetDinhId == x.Id),
                SoDaCongBo = _db.QuyetDinhSangKien
                    .Count(q => q.QuyetDinhId == x.Id
                                && _db.SangKien.Any(h => h.Id == q.SangKienId && h.DaCongBoKetQua))
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ketQua = duLieu
            .Select(x => TaoDto(x.ThongTin, x.TenDonVi, x.TenDot, x.SoSangKien, x.SoDaCongBo))
            .ToList();

        return new PagedResult<QuyetDinhDto>(ketQua, tongSo, trang, soDong);
    }

    public async Task<QuyetDinhChiTietDto> ChiTietAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhXem, ct).ConfigureAwait(false);

        var quyetDinh = await _db.QuyetDinh.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("quyết định", id);

        var tenDonVi = await _db.DonVi.AsNoTracking()
            .Where(d => d.Id == quyetDinh.DonViBanHanhId).Select(d => d.Ten)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        var tenDot = await _db.DotDeNghi.AsNoTracking()
            .Where(d => d.Id == quyetDinh.DotDeNghiId).Select(d => d.Ten)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        var sangKienIds = await _db.QuyetDinhSangKien.AsNoTracking()
            .Where(x => x.QuyetDinhId == id)
            .Select(x => x.SangKienId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var danhSach = await LayThongTinHoSoAsync(sangKienIds, ct).ConfigureAwait(false);

        var soDaCongBo = await _db.SangKien.AsNoTracking()
            .CountAsync(h => sangKienIds.Contains(h.Id) && h.DaCongBoKetQua, ct)
            .ConfigureAwait(false);

        return new QuyetDinhChiTietDto(
            TaoDto(quyetDinh, tenDonVi, tenDot, sangKienIds.Count, soDaCongBo),
            danhSach);
    }

    /// <summary>
    /// Sang kien du dieu kien dua vao quyet dinh: da co ket qua DAT va CHUA nam trong quyet dinh nao.
    /// </summary>
    public async Task<IReadOnlyList<HoSoDuDieuKienDto>> HoSoDuDieuKienAsync(
        Guid? dotDeNghiId, Guid? quyetDinhDangSua, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhBanHanh, ct: ct).ConfigureAwait(false);

        var truyVan = _db.SangKien.AsNoTracking()
            .Where(x => x.KetQua == KetQuaXetDuyetGiaTri.Dat);

        if (dotDeNghiId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DotDeNghiId == dotDeNghiId.Value);
        }

        // Khi SUA mot quyet dinh, cac sang kien dang thuoc chinh no van phai hien ra de bo chon duoc.
        truyVan = truyVan.Where(x => !_db.QuyetDinhSangKien
            .Any(q => q.SangKienId == x.Id
                      && (quyetDinhDangSua == null || q.QuyetDinhId != quyetDinhDangSua.Value)));

        var ids = await truyVan
            .OrderByDescending(x => x.TongDiem)
            .Select(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return await LayThongTinHoSoAsync(ids, ct).ConfigureAwait(false);
    }

    public async Task<Guid> TaoAsync(LuuQuyetDinhDto dto, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhBanHanh, ct: ct).ConfigureAwait(false);

        await BatBuocSoQuyetDinhChuaDungAsync(dto.SoQuyetDinh, null, ct).ConfigureAwait(false);
        await BatBuocSangKienHopLeAsync(dto.SangKienIds, null, ct).ConfigureAwait(false);

        var quyetDinh = new ThucTheQuyetDinh
        {
            Id = Guid.NewGuid(),
            SoQuyetDinh = dto.SoQuyetDinh.Trim(),
            NgayBanHanh = dto.NgayBanHanh,
            Loai = dto.Loai,
            TrichYeu = dto.TrichYeu,
            NguoiKy = dto.NguoiKy,
            ChucVuNguoiKy = dto.ChucVuNguoiKy,
            DonViBanHanhId = dto.DonViBanHanhId,
            DotDeNghiId = dto.DotDeNghiId,
            TepTinId = dto.TepTinId
        };

        _db.QuyetDinh.Add(quyetDinh);

        await GanSangKienAsync(quyetDinh.Id, dto.NgayBanHanh, dto.SangKienIds, ct)
            .ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("BAN_HANH_QUYET_DINH", "QUYET_DINH", "QuyetDinh", quyetDinh.Id,
            $"Ban hành quyết định {quyetDinh.SoQuyetDinh} cho {dto.SangKienIds.Count} sáng kiến",
            duLieuSau: dto, ct: ct).ConfigureAwait(false);

        return quyetDinh.Id;
    }

    public async Task CapNhatAsync(Guid id, LuuQuyetDinhDto dto, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhBanHanh, ct).ConfigureAwait(false);

        var quyetDinh = await _db.QuyetDinh.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("quyết định", id);

        if (quyetDinh.DaKySo)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                "Quyết định đã ký số nên không được sửa. Hãy ban hành quyết định thay thế.");
        }

        await BatBuocSoQuyetDinhChuaDungAsync(dto.SoQuyetDinh, id, ct).ConfigureAwait(false);
        await BatBuocSangKienHopLeAsync(dto.SangKienIds, id, ct).ConfigureAwait(false);

        var truoc = new
        {
            quyetDinh.SoQuyetDinh, quyetDinh.NgayBanHanh, quyetDinh.TrichYeu, quyetDinh.NguoiKy
        };

        quyetDinh.SoQuyetDinh = dto.SoQuyetDinh.Trim();
        quyetDinh.NgayBanHanh = dto.NgayBanHanh;
        quyetDinh.Loai = dto.Loai;
        quyetDinh.TrichYeu = dto.TrichYeu;
        quyetDinh.NguoiKy = dto.NguoiKy;
        quyetDinh.ChucVuNguoiKy = dto.ChucVuNguoiKy;
        quyetDinh.DonViBanHanhId = dto.DonViBanHanhId;
        quyetDinh.DotDeNghiId = dto.DotDeNghiId;
        quyetDinh.TepTinId = dto.TepTinId;

        // Go het lien ket cu roi gan lai theo danh sach moi.
        var lienKetCu = await _db.QuyetDinhSangKien
            .Where(x => x.QuyetDinhId == id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var lienKet in lienKetCu)
        {
            lienKet.DaXoa = true;
        }

        var hoSoCu = lienKetCu.Select(x => x.SangKienId).ToList();
        await GoCongNhanAsync(hoSoCu, ct).ConfigureAwait(false);

        await GanSangKienAsync(id, dto.NgayBanHanh, dto.SangKienIds, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("SUA_QUYET_DINH", "QUYET_DINH", "QuyetDinh", id,
            $"Cập nhật quyết định {quyetDinh.SoQuyetDinh}",
            duLieuTruoc: truoc, duLieuSau: dto, ct: ct).ConfigureAwait(false);
    }

    public async Task XoaAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhBanHanh, ct).ConfigureAwait(false);

        var quyetDinh = await _db.QuyetDinh.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("quyết định", id);

        if (quyetDinh.DaKySo)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepXoa,
                "Quyết định đã ký số nên không được xoá.");
        }

        var sangKienIds = await _db.QuyetDinhSangKien.AsNoTracking()
            .Where(x => x.QuyetDinhId == id)
            .Select(x => x.SangKienId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var daCongBo = await _db.SangKien.AsNoTracking()
            .AnyAsync(h => sangKienIds.Contains(h.Id) && h.DaCongBoKetQua, ct)
            .ConfigureAwait(false);

        if (daCongBo)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepXoa,
                "Kết quả trong quyết định đã được công bố nên không được xoá.");
        }

        quyetDinh.DaXoa = true;

        var lienKet = await _db.QuyetDinhSangKien
            .Where(x => x.QuyetDinhId == id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var l in lienKet)
        {
            l.DaXoa = true;
        }

        await GoCongNhanAsync(sangKienIds, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("XOA_QUYET_DINH", "QUYET_DINH", "QuyetDinh", id,
            $"Xoá quyết định {quyetDinh.SoQuyetDinh}", ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Chuc nang 32 - Cong bo ket qua HANG LOAT cho toan bo sang kien trong mot quyet dinh:
    /// danh dau da cong bo, mo hien thi tren trang cong khai va thong bao cho tac gia.
    /// </summary>
    public async Task<int> CongBoAsync(Guid id, bool congKhai, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhBanHanh, ct).ConfigureAwait(false);

        var quyetDinh = await _db.QuyetDinh.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("quyết định", id);

        var sangKienIds = await _db.QuyetDinhSangKien.AsNoTracking()
            .Where(x => x.QuyetDinhId == id)
            .Select(x => x.SangKienId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (sangKienIds.Count == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Quyết định chưa gắn sáng kiến nào nên không có gì để công bố.");
        }

        var bayGio = _dongHo.BayGio;

        var ketQuaChuaCongBo = await _db.KetQuaXetDuyet
            .Where(k => sangKienIds.Contains(k.SangKienId) && !k.DaCongBo)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var k in ketQuaChuaCongBo)
        {
            k.DaCongBo = true;
            k.NgayCongBo = bayGio;
        }

        var hoSoCongBo = await _db.SangKien
            .Where(x => sangKienIds.Contains(x.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var soCapNhat = 0;

        foreach (var h in hoSoCongBo)
        {
            h.CongKhai = congKhai;

            if (!h.DaCongBoKetQua)
            {
                h.DaCongBoKetQua = true;
                h.NgayCongBoKetQua = bayGio;
                soCapNhat++;
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Thong bao cho tac gia cua tung ho so.
        var tacGia = await _db.SangKienTacGia.AsNoTracking()
            .Where(x => sangKienIds.Contains(x.SangKienId) && x.NguoiDungId != null)
            .Select(x => new { x.SangKienId, NguoiDungId = x.NguoiDungId!.Value })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var thongTinHoSo = await _db.SangKien.AsNoTracking()
            .Where(x => sangKienIds.Contains(x.Id))
            .Select(x => new { x.Id, x.MaHoSo, x.TenSangKien })
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);

        foreach (var nhom in tacGia.GroupBy(x => x.SangKienId))
        {
            if (!thongTinHoSo.TryGetValue(nhom.Key, out var hoSo))
            {
                continue;
            }

            await _thongBao.GuiTheoSuKienAsync(
                SuKienThongBao.CoKetQua,
                nhom.Select(x => x.NguoiDungId).Distinct(),
                new Dictionary<string, object?>
                {
                    ["sangKienId"] = nhom.Key,
                    ["maHoSo"] = hoSo.MaHoSo,
                    ["tenSangKien"] = hoSo.TenSangKien,
                    ["soQuyetDinh"] = quyetDinh.SoQuyetDinh,
                    ["ngayBanHanh"] = quyetDinh.NgayBanHanh.ToString("dd/MM/yyyy"),
                    ["duongDan"] = DuongDanGiaoDien.ChiTietHoSo(nhom.Key)
                },
                ct).ConfigureAwait(false);
        }

        await _nhatKy.GhiAsync("CONG_BO_KET_QUA", "QUYET_DINH", "QuyetDinh", id,
            $"Công bố kết quả quyết định {quyetDinh.SoQuyetDinh} cho {sangKienIds.Count} sáng kiến",
            ct: ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Đã công bố quyết định {SoQuyetDinh}: {SoCapNhat}/{TongSo} kết quả chuyển sang đã công bố.",
            quyetDinh.SoQuyetDinh, soCapNhat, sangKienIds.Count);

        return sangKienIds.Count;
    }

    /// <summary>Du lieu tho de tang API dung sinh PDF quyet dinh.</summary>
    public async Task<(ThucTheQuyetDinh QuyetDinh, string? TenDonVi, string? TieuDeVanBan,
        IReadOnlyList<HoSoDuDieuKienDto> DanhSach)> DuLieuXuatAsync(
        Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhXem, ct).ConfigureAwait(false);

        var quyetDinh = await _db.QuyetDinh.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("quyết định", id);

        var donVi = await _db.DonVi.AsNoTracking()
            .Where(d => d.Id == quyetDinh.DonViBanHanhId)
            .Select(d => new { d.Ten, d.TieuDeVanBan })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var ids = await _db.QuyetDinhSangKien.AsNoTracking()
            .Where(x => x.QuyetDinhId == id)
            .Select(x => x.SangKienId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var danhSach = await LayThongTinHoSoAsync(ids, ct).ConfigureAwait(false);

        return (quyetDinh, donVi?.Ten, donVi?.TieuDeVanBan, danhSach);
    }

    // -----------------------------------------------------------------------------------

    private static QuyetDinhDto TaoDto(
        ThucTheQuyetDinh x, string? tenDonVi, string? tenDot, int soSangKien, int soDaCongBo)
        => new(x.Id, x.SoQuyetDinh, x.NgayBanHanh, x.Loai, x.TrichYeu, x.NguoiKy, x.ChucVuNguoiKy,
            x.DonViBanHanhId, tenDonVi, x.DotDeNghiId, tenDot, x.TepTinId, x.DaKySo,
            soSangKien, soDaCongBo);

    /// <summary>Go danh dau cong nhan khoi ho so khi bi bo ra khoi quyet dinh.</summary>
    private async Task GoCongNhanAsync(List<Guid> sangKienIds, CancellationToken ct)
    {
        if (sangKienIds.Count == 0)
        {
            return;
        }

        var hoSo = await _db.SangKien
            .Where(x => sangKienIds.Contains(x.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var h in hoSo)
        {
            h.QuyetDinhId = null;
            h.NgayCongNhan = null;
        }
    }

    private async Task<IReadOnlyList<HoSoDuDieuKienDto>> LayThongTinHoSoAsync(
        List<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<HoSoDuDieuKienDto>();
        }

        return await _db.SangKien.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .OrderByDescending(x => x.TongDiem)
            .Select(x => new HoSoDuDieuKienDto(
                x.Id,
                x.MaHoSo,
                x.TenSangKien,
                _db.SangKienTacGia
                    .Where(t => t.SangKienId == x.Id && t.LaTacGiaChinh)
                    .Select(t => t.HoTen).FirstOrDefault(),
                _db.DonVi.Where(d => d.Id == x.DonViId).Select(d => d.Ten).FirstOrDefault(),
                _db.LinhVuc.Where(l => l.Id == x.LinhVucId).Select(l => l.Ten).FirstOrDefault(),
                x.TongDiem,
                x.MucCongNhanId,
                _db.MucCongNhan.Where(m => m.Id == x.MucCongNhanId).Select(m => m.Ten).FirstOrDefault()))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task BatBuocSoQuyetDinhChuaDungAsync(string soQuyetDinh, Guid? boQuaId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(soQuyetDinh))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe, "Chưa nhập số quyết định.");
        }

        var ma = soQuyetDinh.Trim();

        var trung = await _db.QuyetDinh.AsNoTracking()
            .AnyAsync(x => x.SoQuyetDinh == ma && (boQuaId == null || x.Id != boQuaId.Value), ct)
            .ConfigureAwait(false);

        if (trung)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Số quyết định '{ma}' đã tồn tại.");
        }
    }

    private async Task BatBuocSangKienHopLeAsync(List<Guid> ids, Guid? boQuaQuyetDinhId,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Phải chọn ít nhất một sáng kiến để đưa vào quyết định.");
        }

        var khongDat = await _db.SangKien.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.KetQua != KetQuaXetDuyetGiaTri.Dat)
            .Select(x => x.MaHoSo)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (khongDat.Count > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Các hồ sơ sau chưa có kết quả Đạt nên không được công nhận: {string.Join(", ", khongDat)}.");
        }

        var daThuocQuyetDinhKhac = await _db.QuyetDinhSangKien.AsNoTracking()
            .Where(x => ids.Contains(x.SangKienId)
                        && (boQuaQuyetDinhId == null || x.QuyetDinhId != boQuaQuyetDinhId.Value))
            .Join(_db.QuyetDinh.AsNoTracking(), x => x.QuyetDinhId, q => q.Id,
                (x, q) => new { x.SangKienId, q.SoQuyetDinh })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (daThuocQuyetDinhKhac.Count > 0)
        {
            var maHoSo = await _db.SangKien.AsNoTracking()
                .Where(x => daThuocQuyetDinhKhac.Select(d => d.SangKienId).Contains(x.Id))
                .Select(x => x.MaHoSo)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Các hồ sơ sau đã nằm trong quyết định khác: "
                + $"{string.Join(", ", maHoSo)} (quyết định {string.Join(", ",
                    daThuocQuyetDinhKhac.Select(d => d.SoQuyetDinh).Distinct())}).");
        }
    }

    /// <summary>
    /// Gan sang kien vao quyet dinh.
    ///
    /// <paramref name="ngayBanHanh"/> phai truyen vao chu KHONG duoc doc lai tu CSDL: khi tao moi,
    /// ban ghi quyet dinh moi chi nam trong change tracker chu chua duoc luu, doc lai se ra null
    /// va ho so mat ngay cong nhan.
    /// </summary>
    private async Task GanSangKienAsync(
        Guid quyetDinhId, DateOnly ngayBanHanh, List<Guid> ids, CancellationToken ct)
    {
        var mucCongNhan = await _db.SangKien.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.MucCongNhanId })
            .ToDictionaryAsync(x => x.Id, x => x.MucCongNhanId, ct)
            .ConfigureAwait(false);

        foreach (var sangKienId in ids.Distinct())
        {
            // Them truc tiep vao DbSet de EF luon sinh INSERT cho thuc the con moi.
            _db.QuyetDinhSangKien.Add(new ThucTheQuyetDinhSangKien
            {
                Id = Guid.NewGuid(),
                QuyetDinhId = quyetDinhId,
                SangKienId = sangKienId,
                MucCongNhanId = mucCongNhan.GetValueOrDefault(sangKienId)
            });
        }

        var hoSo = await _db.SangKien
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var h in hoSo)
        {
            h.QuyetDinhId = quyetDinhId;
            h.NgayCongNhan = ngayBanHanh;
        }
    }
}
