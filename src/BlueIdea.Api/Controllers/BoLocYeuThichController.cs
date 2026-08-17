using BlueIdea.Api.Chung;
using BlueIdea.Application.QuanTri;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>
/// Chức năng 28 — Bộ lọc yêu thích trên các màn hình danh sách.
///
/// Không có chính sách quyền riêng: bộ lọc là dữ liệu cá nhân của chính người đăng nhập,
/// ai đăng nhập được thì quản lý bộ lọc của mình. Phạm vi được chặn ở tầng dịch vụ theo
/// định danh phiên, không nhận từ client.
/// </summary>
[ApiController]
[Route("api/v1/bo-loc-yeu-thich")]
[Authorize]
[Produces("application/json")]
public sealed class BoLocYeuThichController : ControllerBase
{
    private readonly DichVuBoLocYeuThich _dichVu;

    public BoLocYeuThichController(DichVuBoLocYeuThich dichVu) => _dichVu = dichVu;

    /// <summary>Bộ lọc đã lưu của tôi trên một màn hình.</summary>
    [HttpGet]
    public async Task<IActionResult> DanhSachAsync(
        [FromQuery] string manHinh, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<BoLocYeuThichDto>>.Ok(
            await _dichVu.DanhSachAsync(manHinh, ct)));

    /// <summary>Lưu bộ lọc mới, hoặc ghi đè bộ lọc cùng tên trên cùng màn hình.</summary>
    [HttpPost]
    public async Task<IActionResult> LuuAsync([FromBody] LuuBoLocDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<Guid>.Ok(await _dichVu.LuuAsync(duLieu, ct), "Đã lưu bộ lọc"));

    /// <summary>Đặt một bộ lọc làm mặc định khi mở màn hình.</summary>
    [HttpPost("{id:guid}/mac-dinh")]
    public async Task<IActionResult> DatMacDinhAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.DatMacDinhAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã đặt bộ lọc mặc định"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xoá bộ lọc"));
    }
}
