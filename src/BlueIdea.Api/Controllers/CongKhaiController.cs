using BlueIdea.Api.Chung;
using BlueIdea.Application.CongKhai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>
/// Chức năng 37 — Cổng tra cứu công khai cho người dân, KHÔNG cần đăng nhập.
///
/// Toàn bộ controller để <c>[AllowAnonymous]</c> ở mức lớp thay vì mở từng hành động: đặt ở
/// lớp thì một hành động thêm sau này không thể vô tình thừa kế chính sách xác thực mặc định
/// rồi trả 401 cho người dân — lỗi kiểu đó không lộ ra khi thử bằng trình duyệt đang đăng nhập.
///
/// Dữ liệu trả về do <see cref="DichVuTraCuuCongKhai"/> giới hạn sẵn ở 5 trường của bản thiết kế.
/// </summary>
[ApiController]
[Route("api/v1/cong-khai")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class CongKhaiController : ControllerBase
{
    private readonly DichVuTraCuuCongKhai _dichVu;

    public CongKhaiController(DichVuTraCuuCongKhai dichVu) => _dichVu = dichVu;

    /// <summary>Danh sách sáng kiến đã được công nhận và công bố công khai.</summary>
    [HttpGet("sang-kien")]
    public async Task<IActionResult> TimAsync(
        [FromQuery] string? tuKhoa,
        [FromQuery] Guid? linhVucId,
        [FromQuery] int trang = 1,
        [FromQuery] int soDong = 20,
        CancellationToken ct = default)
        => Ok(PhanHoiPhanTrang<SangKienCongKhaiDto>.Tu(
            await _dichVu.TimAsync(tuKhoa, linhVucId, trang, soDong, ct)));

    /// <summary>Ba số liệu trên dải thống kê đầu trang.</summary>
    [HttpGet("thong-ke")]
    public async Task<IActionResult> ThongKeAsync(CancellationToken ct)
        => Ok(PhanHoiApi<ThongKeCongKhaiDto>.Ok(await _dichVu.ThongKeAsync(ct)));

    /// <summary>Danh sách lĩnh vực dùng cho chip lọc.</summary>
    [HttpGet("linh-vuc")]
    public async Task<IActionResult> LinhVucAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<LinhVucCongKhaiDto>>.Ok(await _dichVu.LinhVucAsync(ct)));
}
