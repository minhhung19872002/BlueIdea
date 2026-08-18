using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.KySo;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

/// <summary>Cấu hình chữ ký số trả về giao diện — không bao giờ kèm bí mật đã giải mã.</summary>
public sealed record CauHinhChuKySoDto(
    Guid Id,
    string NhaCungCap,
    string LoaiKy,
    string? Endpoint,
    string? ClientId,
    bool DaDatBiMat,
    string? ChungThuSo,
    string ThuatToan,
    short TrangThai,
    bool LaMacDinh);

public sealed record LuuCauHinhChuKySoDto
{
    /// <summary>BAN_CO_YEU_CHINH_PHU | VNPT_CA | VIETTEL_CA | FPT_CA | MISA | KHAC</summary>
    public string NhaCungCap { get; init; } = "KHAC";

    /// <summary>USB_TOKEN | HSM | REMOTE_SIGNING | SMART_CA</summary>
    public string LoaiKy { get; init; } = "REMOTE_SIGNING";

    public string? Endpoint { get; init; }

    public string? ClientId { get; init; }

    /// <summary>Bỏ trống khi sửa = giữ nguyên bí mật đang lưu.</summary>
    public string? ClientSecret { get; init; }

    /// <summary>Serial chứng thư trong kho chứng thư của máy chủ (USB token / HSM).</summary>
    public string? ChungThuSo { get; init; }

    public string ThuatToan { get; init; } = "SHA256withRSA";

    public short TrangThai { get; init; } = TrangThaiDanhMuc.HoatDong;

    public bool LaMacDinh { get; init; }
}

/// <summary>
/// Chức năng 49 — Cấu hình chữ ký số.
///
/// Khoá bí mật của chứng thư (tệp PFX) <b>không</b> nằm ở đây: đường dẫn PFX và mật khẩu đọc từ
/// cấu hình máy chủ (<c>KySo:DuongDanPfx</c>, <c>KySo:MatKhauPfx</c>), vì lưu khoá ký trong cơ sở
/// dữ liệu thì một lần lộ bản dump là lộ luôn quyền ký văn bản. Bản ghi ở đây chỉ mô tả dùng
/// chứng thư nào và ký bằng cách nào.
/// </summary>
[ApiController]
[Route("api/v1/cau-hinh-chu-ky-so")]
[Authorize]
[Produces("application/json")]
public sealed class CauHinhChuKySoController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IDichVuMaHoa _maHoa;
    private readonly IDichVuNhatKy _nhatKy;
    private readonly IBoKySo _boKySo;

    public CauHinhChuKySoController(
        IAppDbContext db, IDichVuMaHoa maHoa, IDichVuNhatKy nhatKy, IBoKySo boKySo)
    {
        _db = db;
        _maHoa = maHoa;
        _nhatKy = nhatKy;
        _boKySo = boKySo;
    }

    [HttpGet]
    [Authorize(Policy = MaQuyen.CauHinhXem)]
    public async Task<IActionResult> LayDanhSachAsync(CancellationToken ct)
    {
        var ds = await _db.CauHinhChuKySo.AsNoTracking()
            .OrderByDescending(x => x.LaMacDinh)
            .Select(x => new CauHinhChuKySoDto(
                x.Id, x.NhaCungCap, x.LoaiKy, x.Endpoint, x.ClientId,
                x.ClientSecretMaHoa != null && x.ClientSecretMaHoa != "",
                x.ChungThuSo, x.ThuatToan, x.TrangThai, x.LaMacDinh))
            .ToListAsync(ct);

        return Ok(PhanHoiApi<List<CauHinhChuKySoDto>>.Ok(ds));
    }

    /// <summary>
    /// Máy chủ đã sẵn sàng ký hay chưa — giao diện dùng để cảnh báo trước khi người dùng bấm
    /// Ký số rồi mới nhận lỗi.
    /// </summary>
    [HttpGet("trang-thai")]
    [Authorize(Policy = MaQuyen.CauHinhXem)]
    public async Task<IActionResult> LayTrangThaiAsync(CancellationToken ct)
    {
        var coCauHinh = await _db.CauHinhChuKySo.AsNoTracking()
            .AnyAsync(x => x.TrangThai == TrangThaiDanhMuc.HoatDong, ct);

        return Ok(PhanHoiApi<object>.Ok(new
        {
            coCauHinhHoatDong = coCauHinh,
            mayChuSanSangKy = _boKySo.DaCauHinh,
            sanSang = coCauHinh && _boKySo.DaCauHinh
        }));
    }

    [HttpPost]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> ThemAsync(
        [FromBody] LuuCauHinhChuKySoDto duLieu, CancellationToken ct)
    {
        KiemTraHopLe(duLieu);

        var banGhi = new CauHinhChuKySo { Id = Guid.NewGuid() };
        ApDung(banGhi, duLieu);

        _db.CauHinhChuKySo.Add(banGhi);

        if (duLieu.LaMacDinh)
        {
            await BoMacDinhKhacAsync(banGhi.Id, ct);
        }

        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("THEM_CAU_HINH_KY_SO", "KY_SO", "CauHinhChuKySo", banGhi.Id,
            $"Thêm cấu hình ký số {duLieu.NhaCungCap}", duLieuSau: AnBiMat(duLieu), ct: ct);

        return Ok(PhanHoiApi<Guid>.Ok(banGhi.Id, "Đã thêm cấu hình chữ ký số"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuCauHinhChuKySoDto duLieu, CancellationToken ct)
    {
        KiemTraHopLe(duLieu);

        var banGhi = await _db.CauHinhChuKySo.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("cấu hình chữ ký số", id);

        ApDung(banGhi, duLieu);

        if (duLieu.LaMacDinh)
        {
            await BoMacDinhKhacAsync(id, ct);
        }

        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("SUA_CAU_HINH_KY_SO", "KY_SO", "CauHinhChuKySo", id,
            $"Cập nhật cấu hình ký số {duLieu.NhaCungCap}", duLieuSau: AnBiMat(duLieu), ct: ct);

        return Ok(PhanHoiApi.Ok("Đã cập nhật cấu hình"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        var banGhi = await _db.CauHinhChuKySo.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("cấu hình chữ ký số", id);

        banGhi.DaXoa = true;
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("XOA_CAU_HINH_KY_SO", "KY_SO", "CauHinhChuKySo", id,
            "Xoá cấu hình chữ ký số", ct: ct);

        return Ok(PhanHoiApi.Ok("Đã xoá cấu hình"));
    }

    // -----------------------------------------------------------------------------------

    private void ApDung(CauHinhChuKySo x, LuuCauHinhChuKySoDto d)
    {
        x.NhaCungCap = d.NhaCungCap;
        x.LoaiKy = d.LoaiKy;
        x.Endpoint = d.Endpoint?.Trim();
        x.ClientId = d.ClientId?.Trim();
        x.ChungThuSo = d.ChungThuSo?.Trim();
        x.ThuatToan = d.ThuatToan;
        x.TrangThai = d.TrangThai;
        x.LaMacDinh = d.LaMacDinh;

        // Bỏ trống = giữ nguyên bí mật đang có, để sửa cấu hình không phải nhập lại.
        if (!string.IsNullOrWhiteSpace(d.ClientSecret))
        {
            x.ClientSecretMaHoa = _maHoa.MaHoa(d.ClientSecret);
        }
    }

    private async Task BoMacDinhKhacAsync(Guid ngoaiTru, CancellationToken ct)
    {
        var khac = await _db.CauHinhChuKySo
            .Where(x => x.LaMacDinh && x.Id != ngoaiTru)
            .ToListAsync(ct);

        foreach (var x in khac)
        {
            x.LaMacDinh = false;
        }
    }

    private static void KiemTraHopLe(LuuCauHinhChuKySoDto d)
    {
        if (d.LoaiKy is not ("USB_TOKEN" or "HSM" or "REMOTE_SIGNING" or "SMART_CA"))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Loại ký '{d.LoaiKy}' không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(d.Endpoint)
            && (!Uri.TryCreate(d.Endpoint, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https")))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Endpoint phải là URL http hoặc https tuyệt đối.");
        }
    }

    private static object AnBiMat(LuuCauHinhChuKySoDto d) => new
    {
        d.NhaCungCap,
        d.LoaiKy,
        d.Endpoint,
        d.ClientId,
        d.ChungThuSo,
        d.ThuatToan,
        d.TrangThai,
        d.LaMacDinh,
        ClientSecret = string.IsNullOrWhiteSpace(d.ClientSecret) ? "(giữ nguyên)" : "(đã đổi)"
    };
}
