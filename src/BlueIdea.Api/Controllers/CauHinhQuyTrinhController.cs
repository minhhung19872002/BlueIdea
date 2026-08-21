using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

/// <summary>Một cấu hình liên thông gắn vào bước của quy trình.</summary>
public sealed record LienThongBuocDto(
    Guid Id,
    Guid QuyTrinhId,
    Guid? BuocId,
    string? TenBuoc,
    Guid HeThongTichHopId,
    string TenHeThong,
    string SuKien,
    string? LoaiDuLieu,
    short TrangThai);

public sealed record LuuLienThongBuocDto
{
    public Guid? BuocId { get; init; }

    public Guid HeThongTichHopId { get; init; }

    /// <summary>KHI_VAO_BUOC | KHI_HOAN_THANH | KHI_PHE_DUYET</summary>
    public string SuKien { get; init; } = "KHI_HOAN_THANH";

    public string? LoaiDuLieu { get; init; }

    public Dictionary<string, string>? CauHinhMapping { get; init; }

    public short TrangThai { get; init; } = TrangThaiDanhMuc.HoatDong;
}

/// <summary>Một cấp phê duyệt áp cho đợt / lĩnh vực.</summary>
public sealed record CapPheDuyetDto(
    Guid Id,
    Guid? DotDeNghiId,
    string? TenDot,
    Guid? LinhVucId,
    string? TenLinhVuc,
    Guid DonViPheDuyetId,
    string TenDonViPheDuyet,
    int ThuTuCap,
    string? GhiChu);

public sealed record LuuCapPheDuyetDto
{
    public Guid? DotDeNghiId { get; init; }

    public Guid? LinhVucId { get; init; }

    public Guid DonViPheDuyetId { get; init; }

    public int ThuTuCap { get; init; } = 1;

    public string? GhiChu { get; init; }
}

/// <summary>
/// Chức năng 5 và 16 — hai loại cấu hình gắn với quy trình xét duyệt:
/// cấp phê duyệt theo đợt/lĩnh vực, và liên thông sang hệ thống ngoài tại từng bước.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[Produces("application/json")]
public sealed class CauHinhQuyTrinhController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IDichVuNhatKy _nhatKy;

    public CauHinhQuyTrinhController(IAppDbContext db, IDichVuNhatKy nhatKy)
    {
        _db = db;
        _nhatKy = nhatKy;
    }

    // ------------------------------------------- Chức năng 16: liên thông theo bước

    /// <summary>Danh sách cấu hình liên thông của một quy trình.</summary>
    [HttpGet("quy-trinh/{quyTrinhId:guid}/lien-thong")]
    [Authorize(Policy = MaQuyen.QuyTrinhXem)]
    public async Task<IActionResult> LayLienThongAsync(Guid quyTrinhId, CancellationToken ct)
    {
        var ds = await _db.QuyTrinhLienThong.AsNoTracking()
            .Where(x => x.QuyTrinhId == quyTrinhId)
            .ToListAsync(ct);

        var buocIds = ds.Where(x => x.BuocId.HasValue).Select(x => x.BuocId!.Value).ToList();
        var heThongIds = ds.Select(x => x.HeThongTichHopId).ToList();

        var tenBuoc = await _db.QuyTrinhBuoc.AsNoTracking()
            .Where(x => buocIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct);

        var tenHeThong = await _db.HeThongTichHop.AsNoTracking()
            .Where(x => heThongIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct);

        var ketQua = ds
            .OrderBy(x => x.SuKien)
            .Select(x => new LienThongBuocDto(
                x.Id, x.QuyTrinhId, x.BuocId,
                x.BuocId.HasValue ? tenBuoc.GetValueOrDefault(x.BuocId.Value) : null,
                x.HeThongTichHopId,
                tenHeThong.GetValueOrDefault(x.HeThongTichHopId) ?? "(hệ thống đã xoá)",
                x.SuKien, x.LoaiDuLieu, x.TrangThai))
            .ToList();

        return Ok(PhanHoiApi<List<LienThongBuocDto>>.Ok(ketQua));
    }

    [HttpPost("quy-trinh/{quyTrinhId:guid}/lien-thong")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> ThemLienThongAsync(
        Guid quyTrinhId, [FromBody] LuuLienThongBuocDto duLieu, CancellationToken ct)
    {
        await KiemTraLienThongAsync(quyTrinhId, duLieu, ct);

        var banGhi = new QuyTrinhLienThong
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinhId,
            BuocId = duLieu.BuocId,
            HeThongTichHopId = duLieu.HeThongTichHopId,
            SuKien = duLieu.SuKien,
            LoaiDuLieu = duLieu.LoaiDuLieu,
            CauHinhMapping = duLieu.CauHinhMapping,
            TrangThai = duLieu.TrangThai
        };

        _db.QuyTrinhLienThong.Add(banGhi);
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("THEM_LIEN_THONG_BUOC", "QUY_TRINH", "QuyTrinhLienThong",
            banGhi.Id, "Thêm cấu hình liên thông cho bước quy trình", duLieuSau: duLieu, ct: ct);

        return Ok(PhanHoiApi<Guid>.Ok(banGhi.Id, "Đã thêm cấu hình liên thông"));
    }

    [HttpPut("quy-trinh/lien-thong/{id:guid}")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> SuaLienThongAsync(
        Guid id, [FromBody] LuuLienThongBuocDto duLieu, CancellationToken ct)
    {
        var banGhi = await _db.QuyTrinhLienThong.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("cấu hình liên thông", id);

        await KiemTraLienThongAsync(banGhi.QuyTrinhId, duLieu, ct);

        banGhi.BuocId = duLieu.BuocId;
        banGhi.HeThongTichHopId = duLieu.HeThongTichHopId;
        banGhi.SuKien = duLieu.SuKien;
        banGhi.LoaiDuLieu = duLieu.LoaiDuLieu;
        banGhi.CauHinhMapping = duLieu.CauHinhMapping;
        banGhi.TrangThai = duLieu.TrangThai;

        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("SUA_LIEN_THONG_BUOC", "QUY_TRINH", "QuyTrinhLienThong",
            id, "Cập nhật cấu hình liên thông của bước", duLieuSau: duLieu, ct: ct);

        return Ok(PhanHoiApi.Ok("Đã cập nhật"));
    }

    [HttpDelete("quy-trinh/lien-thong/{id:guid}")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> XoaLienThongAsync(Guid id, CancellationToken ct)
    {
        var banGhi = await _db.QuyTrinhLienThong.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("cấu hình liên thông", id);

        banGhi.DaXoa = true;
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("XOA_LIEN_THONG_BUOC", "QUY_TRINH", "QuyTrinhLienThong", id,
            "Xoá cấu hình liên thông của bước", ct: ct);

        return Ok(PhanHoiApi.Ok("Đã xoá"));
    }

    // ------------------------------------------- Chức năng 5: cấp phê duyệt

    /// <summary>Danh sách cấp phê duyệt, lọc được theo đợt và lĩnh vực.</summary>
    [HttpGet("cap-phe-duyet")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayCapPheDuyetAsync(
        [FromQuery] Guid? dotDeNghiId, [FromQuery] Guid? linhVucId, CancellationToken ct)
    {
        var truyVan = _db.CauHinhCapPheDuyet.AsNoTracking();

        if (dotDeNghiId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DotDeNghiId == dotDeNghiId || x.DotDeNghiId == null);
        }

        if (linhVucId.HasValue)
        {
            truyVan = truyVan.Where(x => x.LinhVucId == linhVucId || x.LinhVucId == null);
        }

        var ds = await truyVan.OrderBy(x => x.ThuTuCap).ToListAsync(ct);

        var tenDot = await _db.DotDeNghi.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct);

        var tenLinhVuc = await _db.LinhVuc.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct);

        var tenDonVi = await _db.DonVi.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct);

        var ketQua = ds.Select(x => new CapPheDuyetDto(
            x.Id, x.DotDeNghiId,
            x.DotDeNghiId.HasValue ? tenDot.GetValueOrDefault(x.DotDeNghiId.Value) : null,
            x.LinhVucId,
            x.LinhVucId.HasValue ? tenLinhVuc.GetValueOrDefault(x.LinhVucId.Value) : null,
            x.DonViPheDuyetId,
            tenDonVi.GetValueOrDefault(x.DonViPheDuyetId) ?? "(đơn vị đã xoá)",
            x.ThuTuCap, x.GhiChu)).ToList();

        return Ok(PhanHoiApi<List<CapPheDuyetDto>>.Ok(ketQua));
    }

    [HttpPost("cap-phe-duyet")]
    [Authorize(Policy = MaQuyen.DanhMucThem)]
    public async Task<IActionResult> ThemCapPheDuyetAsync(
        [FromBody] LuuCapPheDuyetDto duLieu, CancellationToken ct)
    {
        await KiemTraCapPheDuyetAsync(duLieu, null, ct);

        var banGhi = new CauHinhCapPheDuyet
        {
            Id = Guid.NewGuid(),
            DotDeNghiId = duLieu.DotDeNghiId,
            LinhVucId = duLieu.LinhVucId,
            DonViPheDuyetId = duLieu.DonViPheDuyetId,
            ThuTuCap = duLieu.ThuTuCap,
            GhiChu = duLieu.GhiChu
        };

        _db.CauHinhCapPheDuyet.Add(banGhi);
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("THEM_CAP_PHE_DUYET", "DANH_MUC", "CauHinhCapPheDuyet",
            banGhi.Id, "Thêm cấp phê duyệt", duLieuSau: duLieu, ct: ct);

        return Ok(PhanHoiApi<Guid>.Ok(banGhi.Id, "Đã thêm cấp phê duyệt"));
    }

    [HttpPut("cap-phe-duyet/{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    public async Task<IActionResult> SuaCapPheDuyetAsync(
        Guid id, [FromBody] LuuCapPheDuyetDto duLieu, CancellationToken ct)
    {
        var banGhi = await _db.CauHinhCapPheDuyet.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("cấp phê duyệt", id);

        await KiemTraCapPheDuyetAsync(duLieu, id, ct);

        banGhi.DotDeNghiId = duLieu.DotDeNghiId;
        banGhi.LinhVucId = duLieu.LinhVucId;
        banGhi.DonViPheDuyetId = duLieu.DonViPheDuyetId;
        banGhi.ThuTuCap = duLieu.ThuTuCap;
        banGhi.GhiChu = duLieu.GhiChu;

        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("SUA_CAP_PHE_DUYET", "DANH_MUC", "CauHinhCapPheDuyet", id,
            "Cập nhật cấp phê duyệt", duLieuSau: duLieu, ct: ct);

        return Ok(PhanHoiApi.Ok("Đã cập nhật"));
    }

    [HttpDelete("cap-phe-duyet/{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucXoa)]
    public async Task<IActionResult> XoaCapPheDuyetAsync(Guid id, CancellationToken ct)
    {
        var banGhi = await _db.CauHinhCapPheDuyet.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("cấp phê duyệt", id);

        banGhi.DaXoa = true;
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("XOA_CAP_PHE_DUYET", "DANH_MUC", "CauHinhCapPheDuyet", id,
            "Xoá cấp phê duyệt", ct: ct);

        return Ok(PhanHoiApi.Ok("Đã xoá"));
    }

    // -----------------------------------------------------------------------------------

    private async Task KiemTraLienThongAsync(
        Guid quyTrinhId, LuuLienThongBuocDto duLieu, CancellationToken ct)
    {
        if (duLieu.SuKien is not ("KHI_VAO_BUOC" or "KHI_HOAN_THANH" or "KHI_PHE_DUYET"))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Sự kiện '{duLieu.SuKien}' không hợp lệ.");
        }

        if (!await _db.HeThongTichHop.AsNoTracking()
                .AnyAsync(x => x.Id == duLieu.HeThongTichHopId, ct))
        {
            throw new KhongTimThayException("hệ thống liên thông", duLieu.HeThongTichHopId);
        }

        // Buoc phai thuoc dung quy trinh dang cau hinh — gan nham buoc cua quy trinh khac se
        // khien luong dong bo khong bao gio chay ma khong ai hieu tai sao.
        if (duLieu.BuocId.HasValue
            && !await _db.QuyTrinhBuoc.AsNoTracking()
                .AnyAsync(x => x.Id == duLieu.BuocId && x.QuyTrinhId == quyTrinhId, ct))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Bước được chọn không thuộc quy trình này.");
        }
    }

    private async Task KiemTraCapPheDuyetAsync(
        LuuCapPheDuyetDto duLieu, Guid? ngoaiTru, CancellationToken ct)
    {
        var donVi = await _db.DonVi.AsNoTracking()
            .Where(x => x.Id == duLieu.DonViPheDuyetId)
            .Select(x => new { x.Ten, x.LaDonViPheDuyet })
            .FirstOrDefaultAsync(ct)
            ?? throw new KhongTimThayException("đơn vị phê duyệt", duLieu.DonViPheDuyetId);

        // O tick "La don vi phe duyet" tren ho so don vi phai co hieu luc that: khong danh dau ma
        // van gan duoc lam cap phe duyet thi o tick do chi la trang tri, va danh sach cap phe
        // duyet co the tro vao mot don vi khong co tham quyen ky.
        if (!donVi.LaDonViPheDuyet)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Đơn vị \"{donVi.Ten}\" chưa được đánh dấu là đơn vị phê duyệt — "
                + "mở hồ sơ đơn vị và bật ô \"Là đơn vị phê duyệt\" trước khi khai cấp xét.");
        }

        if (duLieu.ThuTuCap < 1)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Thứ tự cấp phải từ 1 trở lên.");
        }

        // Trung (dot, linh vuc, thu tu cap) = hai don vi cung mot cap cho cung pham vi, khong
        // biet ho so phai qua don vi nao truoc.
        var trung = await _db.CauHinhCapPheDuyet.AsNoTracking()
            .AnyAsync(x => x.DotDeNghiId == duLieu.DotDeNghiId
                           && x.LinhVucId == duLieu.LinhVucId
                           && x.ThuTuCap == duLieu.ThuTuCap
                           && (ngoaiTru == null || x.Id != ngoaiTru), ct);

        if (trung)
        {
            throw new NghiepVuException(MaLoiHeThong.TrungMa,
                $"Đã có cấp phê duyệt thứ {duLieu.ThuTuCap} cho phạm vi đợt/lĩnh vực này.");
        }
    }
}
