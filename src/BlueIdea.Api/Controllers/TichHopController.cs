using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.TichHop;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

/// <summary>Hệ thống tích hợp trả về giao diện — không bao giờ kèm secret đã giải mã.</summary>
public sealed record HeThongTichHopDto(
    Guid Id,
    string Ma,
    string Ten,
    string? EndpointBase,
    string LoaiXacThuc,
    string? ClientId,
    bool DaDatBiMat,
    string? Scope,
    string TanSuatDongBo,
    DateTimeOffset? LanDongBoCuoi,
    short TrangThai,
    Dictionary<string, string>? CauHinhMapping);

public sealed record LuuHeThongTichHopDto
{
    public string Ma { get; init; } = string.Empty;

    public string Ten { get; init; } = string.Empty;

    public string? EndpointBase { get; init; }

    /// <summary>API_KEY | HMAC | OAUTH2</summary>
    public string LoaiXacThuc { get; init; } = "API_KEY";

    public string? ClientId { get; init; }

    /// <summary>Bỏ trống khi sửa = giữ nguyên bí mật đang có.</summary>
    public string? ClientSecret { get; init; }

    public string? Scope { get; init; }

    public string TanSuatDongBo { get; init; } = "THU_CONG";

    public short TrangThai { get; init; } = TrangThaiDanhMuc.HoatDong;

    public Dictionary<string, string>? CauHinhMapping { get; init; }
}

/// <summary>Chức năng 16, 41 — Cấu hình liên thông và đồng bộ dữ liệu sang hệ thống ngoài.</summary>
[ApiController]
[Route("api/v1/tich-hop")]
[Authorize]
[Produces("application/json")]
public sealed class TichHopController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDichVuMaHoa _maHoa;
    private readonly IDichVuNhatKy _nhatKy;
    private readonly DichVuDongBoLienThong _dongBo;

    public TichHopController(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDichVuMaHoa maHoa,
        IDichVuNhatKy nhatKy, DichVuDongBoLienThong dongBo)
    {
        _db = db;
        _phanQuyen = phanQuyen;
        _maHoa = maHoa;
        _nhatKy = nhatKy;
        _dongBo = dongBo;
    }

    /// <summary>Danh sách hệ thống liên thông. Bí mật chỉ báo "đã đặt", không trả giá trị.</summary>
    [HttpGet("he-thong")]
    [Authorize(Policy = MaQuyen.TichHopCauHinh)]
    public async Task<IActionResult> LayDanhSachAsync(CancellationToken ct)
    {
        var ds = await _db.HeThongTichHop.AsNoTracking()
            .OrderBy(x => x.ThuTu).ThenBy(x => x.Ten)
            .Select(x => new HeThongTichHopDto(
                x.Id, x.Ma, x.Ten, x.EndpointBase, x.LoaiXacThuc, x.ClientId,
                x.ClientSecretMaHoa != null && x.ClientSecretMaHoa != "",
                x.Scope, x.TanSuatDongBo, x.LanDongBoCuoi, x.TrangThai, x.CauHinhMapping))
            .ToListAsync(ct);

        return Ok(PhanHoiApi<List<HeThongTichHopDto>>.Ok(ds));
    }

    [HttpPost("he-thong")]
    [Authorize(Policy = MaQuyen.TichHopCauHinh)]
    public async Task<IActionResult> ThemAsync(
        [FromBody] LuuHeThongTichHopDto duLieu, CancellationToken ct)
    {
        KiemTraHopLe(duLieu);

        var ma = duLieu.Ma.Trim().ToUpperInvariant();

        if (await _db.HeThongTichHop.AsNoTracking().AnyAsync(x => x.Ma == ma, ct))
        {
            throw new NghiepVuException(MaLoiHeThong.TrungMa, $"Mã hệ thống '{ma}' đã tồn tại.");
        }

        var heThong = new HeThongTichHop { Id = Guid.NewGuid(), Ma = ma };
        ApDung(heThong, duLieu);

        _db.HeThongTichHop.Add(heThong);
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("THEM_HE_THONG_TICH_HOP", "TICH_HOP", "HeThongTichHop", heThong.Id,
            $"Thêm hệ thống liên thông {ma}", duLieuSau: AnBiMat(duLieu), ct: ct);

        return Ok(PhanHoiApi<Guid>.Ok(heThong.Id, "Đã thêm hệ thống liên thông"));
    }

    [HttpPut("he-thong/{id:guid}")]
    [Authorize(Policy = MaQuyen.TichHopCauHinh)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuHeThongTichHopDto duLieu, CancellationToken ct)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TichHopCauHinh, id, ct);
        KiemTraHopLe(duLieu);

        var heThong = await _db.HeThongTichHop.FirstOrDefaultAsync(x => x.Id == id, ct)
                      ?? throw new KhongTimThayException("hệ thống tích hợp", id);

        ApDung(heThong, duLieu);
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("SUA_HE_THONG_TICH_HOP", "TICH_HOP", "HeThongTichHop", id,
            $"Cập nhật hệ thống liên thông {heThong.Ma}", duLieuSau: AnBiMat(duLieu), ct: ct);

        return Ok(PhanHoiApi.Ok("Đã cập nhật"));
    }

    [HttpDelete("he-thong/{id:guid}")]
    [Authorize(Policy = MaQuyen.TichHopCauHinh)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TichHopCauHinh, id, ct);

        var heThong = await _db.HeThongTichHop.FirstOrDefaultAsync(x => x.Id == id, ct)
                      ?? throw new KhongTimThayException("hệ thống tích hợp", id);

        heThong.DaXoa = true;
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("XOA_HE_THONG_TICH_HOP", "TICH_HOP", "HeThongTichHop", id,
            $"Xoá hệ thống liên thông {heThong.Ma}", ct: ct);

        return Ok(PhanHoiApi.Ok("Đã xoá"));
    }

    /// <summary>Xem trước dữ liệu sẽ đẩy đi — không gửi gì cả.</summary>
    [HttpGet("xem-truoc")]
    [Authorize(Policy = MaQuyen.TichHopDongBo)]
    public async Task<IActionResult> XemTruocAsync(
        [FromQuery] Guid? dotDeNghiId, [FromQuery] int? nam, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<BanGhiDongBo>>.Ok(
            await _dongBo.XemTruocAsync(dotDeNghiId, nam, ct)));

    /// <summary>Đẩy danh sách sáng kiến đã công bố sang hệ thống ngoài.</summary>
    [HttpPost("he-thong/{id:guid}/dong-bo")]
    [Authorize(Policy = MaQuyen.TichHopDongBo)]
    public async Task<IActionResult> DongBoAsync(
        Guid id, [FromQuery] Guid? dotDeNghiId, [FromQuery] int? nam, CancellationToken ct)
    {
        var ketQua = await _dongBo.ChayAsync(id, dotDeNghiId, nam, ct);

        return Ok(PhanHoiApi<KetQuaDongBo>.Ok(
            ketQua,
            $"Đồng bộ sang {ketQua.TenHeThong}: {ketQua.ThanhCong}/{ketQua.TongBanGhi} bản ghi"));
    }

    [HttpGet("nhat-ky-dong-bo")]
    [Authorize(Policy = MaQuyen.TichHopCauHinh)]
    public async Task<IActionResult> LayNhatKyAsync(
        [FromQuery] Guid? heThongId, [FromQuery] int soDong = 50, CancellationToken ct = default)
        => Ok(PhanHoiApi<IReadOnlyList<NhatKyDongBo>>.Ok(
            await _dongBo.LichSuAsync(heThongId, soDong, ct)));

    // -----------------------------------------------------------------------------------

    private void ApDung(HeThongTichHop x, LuuHeThongTichHopDto d)
    {
        x.Ten = d.Ten.Trim();
        x.TenKhongDau = VanBanTiengViet.TaoKhongDau(d.Ten);
        x.EndpointBase = d.EndpointBase?.Trim();
        x.LoaiXacThuc = d.LoaiXacThuc;
        x.ClientId = d.ClientId;
        x.Scope = d.Scope;
        x.TanSuatDongBo = d.TanSuatDongBo;
        x.TrangThai = d.TrangThai;
        x.CauHinhMapping = d.CauHinhMapping;

        // Bỏ trống = giữ nguyên bí mật đang có, để sửa cấu hình không phải nhập lại khoá.
        if (!string.IsNullOrWhiteSpace(d.ClientSecret))
        {
            x.ClientSecretMaHoa = _maHoa.MaHoa(d.ClientSecret);
        }
    }

    private static void KiemTraHopLe(LuuHeThongTichHopDto d)
    {
        if (string.IsNullOrWhiteSpace(d.Ma) || string.IsNullOrWhiteSpace(d.Ten))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe, "Mã và tên là bắt buộc.");
        }

        if (d.LoaiXacThuc is not ("API_KEY" or "HMAC" or "OAUTH2"))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Loại xác thực '{d.LoaiXacThuc}' không hợp lệ (chỉ API_KEY, HMAC, OAUTH2).");
        }

        if (!string.IsNullOrWhiteSpace(d.EndpointBase)
            && !Uri.TryCreate(d.EndpointBase, UriKind.Absolute, out var uri))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Endpoint phải là URL tuyệt đối.");
        }
        else if (!string.IsNullOrWhiteSpace(d.EndpointBase)
                 && Uri.TryCreate(d.EndpointBase, UriKind.Absolute, out uri)
                 && uri.Scheme is not ("http" or "https"))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Endpoint chỉ chấp nhận giao thức http hoặc https.");
        }
    }

    private static object AnBiMat(LuuHeThongTichHopDto d) => new
    {
        d.Ma,
        d.Ten,
        d.EndpointBase,
        d.LoaiXacThuc,
        d.ClientId,
        d.Scope,
        d.TanSuatDongBo,
        d.TrangThai,
        ClientSecret = string.IsNullOrWhiteSpace(d.ClientSecret) ? "(giữ nguyên)" : "(đã đổi)"
    };
}
