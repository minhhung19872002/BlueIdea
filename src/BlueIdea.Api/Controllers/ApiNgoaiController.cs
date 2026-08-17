using BlueIdea.Api.Chung;
using BlueIdea.Application.CongKhai;
using BlueIdea.Application.TichHop;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;

namespace BlueIdea.Api.Controllers;

/// <summary>
/// Bộ lọc xác thực khoá API cho hệ thống ngoài gọi vào.
///
/// Đặt thành filter riêng thay vì dùng scheme xác thực: hệ thống ngoài không có tài khoản
/// người dùng, không có vai trò, và không được đi qua bất kỳ đường phân quyền nội bộ nào.
/// Dựng một <c>ClaimsPrincipal</c> giả cho chúng sẽ tạo ra một danh tính lửng lơ mà các
/// tầng khác có thể vô tình tin tưởng.
/// </summary>
public sealed class XacThucKhoaApiAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string TenHeader = "X-Api-Key";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var dichVu = context.HttpContext.RequestServices.GetRequiredService<DichVuKhoaApiNgoai>();

        var khoa = context.HttpContext.Request.Headers[TenHeader].FirstOrDefault();
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();

        var banGhi = await dichVu.XacThucAsync(khoa, ip, context.HttpContext.RequestAborted);

        if (banGhi is null)
        {
            // Một thông báo duy nhất cho mọi lý do (sai khoá / hết hạn / sai IP): phân biệt
            // ra thì bên gọi dò được khoá nào có thật và dải IP nào đang mở.
            context.Result = new ObjectResult(
                PhanHoiApi.Loi(MaLoiHeThong.ChuaXacThuc, "Khoá API không hợp lệ."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }
    }
}

/// <summary>
/// Chức năng 41 — API cho hệ thống ngoài (Thi đua khen thưởng, IOC) gọi vào.
///
/// Xác thực bằng khoá API ở header <c>X-Api-Key</c> kèm danh sách IP cho phép, có giới hạn
/// tần suất riêng. Chỉ trả dữ liệu sáng kiến ĐÃ CÔNG NHẬN VÀ CÔNG KHAI — dùng chung dịch vụ
/// với cổng tra cứu công khai để không tồn tại hai định nghĩa "công khai" khác nhau.
/// </summary>
[ApiController]
[Route("api/public/v1")]
[AllowAnonymous]
[XacThucKhoaApi]
[EnableRateLimiting("ApiNgoai")]
[Produces("application/json")]
public sealed class ApiNgoaiController : ControllerBase
{
    private readonly DichVuTraCuuCongKhai _traCuu;

    public ApiNgoaiController(DichVuTraCuuCongKhai traCuu) => _traCuu = traCuu;

    /// <summary>Danh sách sáng kiến đã được công nhận và công bố công khai.</summary>
    [HttpGet("sang-kien")]
    public async Task<IActionResult> SangKienAsync(
        [FromQuery] string? tuKhoa,
        [FromQuery] Guid? linhVucId,
        [FromQuery] int trang = 1,
        [FromQuery] int soDong = 50,
        CancellationToken ct = default)
        => Ok(PhanHoiPhanTrang<SangKienCongKhaiDto>.Tu(
            await _traCuu.TimAsync(tuKhoa, linhVucId, trang, soDong, ct)));

    /// <summary>Chỉ số tổng hợp phục vụ hệ thống IOC.</summary>
    [HttpGet("thong-ke")]
    public async Task<IActionResult> ThongKeAsync(CancellationToken ct)
        => Ok(PhanHoiApi<ThongKeCongKhaiDto>.Ok(await _traCuu.ThongKeAsync(ct)));

    /// <summary>Danh mục lĩnh vực kèm số lượng sáng kiến công khai.</summary>
    [HttpGet("linh-vuc")]
    public async Task<IActionResult> LinhVucAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<LinhVucCongKhaiDto>>.Ok(await _traCuu.LinhVucAsync(ct)));
}

/// <summary>Chức năng 41 — Màn hình quản trị khoá API cấp cho hệ thống ngoài.</summary>
[ApiController]
[Route("api/v1/khoa-api-ngoai")]
[Authorize(Policy = MaQuyen.TichHopCauHinh)]
[Produces("application/json")]
public sealed class KhoaApiNgoaiController : ControllerBase
{
    private readonly DichVuKhoaApiNgoai _dichVu;

    public KhoaApiNgoaiController(DichVuKhoaApiNgoai dichVu) => _dichVu = dichVu;

    [HttpGet]
    public async Task<IActionResult> DanhSachAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<KhoaApiNgoaiDto>>.Ok(await _dichVu.DanhSachAsync(ct)));

    /// <summary>Cấp khoá mới. Khoá gốc chỉ hiển thị đúng một lần trong phản hồi này.</summary>
    [HttpPost]
    public async Task<IActionResult> CapAsync([FromBody] LuuKhoaApiDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<KhoaMoiDto>.Ok(
            await _dichVu.CapAsync(duLieu, ct),
            "Đã cấp khoá API. Hãy sao chép ngay — khoá sẽ không hiển thị lại."));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> CapNhatAsync(
        Guid id, [FromBody] LuuKhoaApiDto duLieu, CancellationToken ct)
    {
        await _dichVu.CapNhatAsync(id, duLieu, ct);
        return Ok(PhanHoiApi.Ok("Đã cập nhật khoá API"));
    }

    [HttpPost("{id:guid}/trang-thai")]
    public async Task<IActionResult> DoiTrangThaiAsync(
        Guid id, [FromQuery] bool bat, CancellationToken ct)
    {
        await _dichVu.DoiTrangThaiAsync(id, bat, ct);
        return Ok(PhanHoiApi.Ok(bat ? "Đã bật khoá API" : "Đã tạm dừng khoá API"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> ThuHoiAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.ThuHoiAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã thu hồi khoá API"));
    }
}
