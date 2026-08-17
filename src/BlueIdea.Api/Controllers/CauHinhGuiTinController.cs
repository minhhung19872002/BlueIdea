using BlueIdea.Api.Chung;
using BlueIdea.Application.QuanTri;
using BlueIdea.Domain.Chung;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>Chức năng 50 — Cấu hình máy chủ email và nhà cung cấp SMS.</summary>
[ApiController]
[Route("api/v1/cau-hinh-gui-tin")]
[Authorize]
[Produces("application/json")]
public sealed class CauHinhGuiTinController : ControllerBase
{
    private readonly DichVuCauHinhGuiTin _dichVu;

    public CauHinhGuiTinController(DichVuCauHinhGuiTin dichVu) => _dichVu = dichVu;

    /// <summary>
    /// Danh sách cấu hình. Mật khẩu và API key <b>không bao giờ</b> được trả về —
    /// chỉ có cờ báo đã đặt hay chưa.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = MaQuyen.CauHinhXem)]
    public async Task<IActionResult> LayDanhSachAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<CauHinhGuiTinDto>>.Ok(await _dichVu.DanhSachAsync(ct)));

    /// <summary>Thống kê hàng đợi gửi tin — cho biết cấu hình có thực sự hoạt động không.</summary>
    [HttpGet("thong-ke-hang-doi")]
    [Authorize(Policy = MaQuyen.CauHinhXem)]
    public async Task<IActionResult> LayThongKeHangDoiAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyDictionary<string, int>>.Ok(await _dichVu.ThongKeHangDoiAsync(ct)));

    [HttpPost]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> ThemAsync(
        [FromBody] LuuCauHinhGuiTinDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<Guid>.Ok(await _dichVu.ThemAsync(duLieu, ct), "Đã thêm cấu hình"));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuCauHinhGuiTinDto duLieu, CancellationToken ct)
    {
        await _dichVu.CapNhatAsync(id, duLieu, ct);
        return Ok(PhanHoiApi.Ok("Đã cập nhật cấu hình"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xoá cấu hình"));
    }

    /// <summary>Gửi thử ngay bằng cấu hình đang lưu để kiểm chứng trước khi đưa vào vận hành.</summary>
    [HttpPost("{id:guid}/gui-thu")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> GuiThuAsync(
        Guid id, [FromQuery] string nguoiNhan, CancellationToken ct)
        => Ok(PhanHoiApi.Ok(await _dichVu.GuiThuAsync(id, nguoiNhan, ct)));
}
