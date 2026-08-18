using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.KySo;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

public sealed record ChuanBiKyUsbDto(Guid TepTinId, string DoiTuong, Guid DoiTuongId);

public sealed record HoanTatKyUsbDto(string ChuKyBase64, string ChungThuBase64);

/// <summary>
/// Chức năng 49 — Ký số bằng USB token (chứng thư cá nhân cắm trên máy người ký).
///
/// Khoá bí mật trong USB token không rời khỏi thiết bị, nên máy chủ không ký thay được. Luồng
/// gồm ba nhịp: máy chủ phát giá trị băm → công cụ ký ở máy trạm ký giá trị băm bằng token →
/// máy chủ xác minh chữ ký rồi mới ghi nhận.
///
/// Khác với ký bằng chứng thư của tổ chức (đặt trên máy chủ, xem <c>/quyet-dinh/{id}/ky-so</c>):
/// ở đây chữ ký gắn với CÁ NHÂN người ký, dùng khi văn bản đòi chữ ký của đúng người có thẩm quyền.
/// </summary>
[ApiController]
[Route("api/v1/ky-so-usb")]
[Authorize(Policy = MaQuyen.QuyetDinhKySo)]
[Produces("application/json")]
public sealed class KySoUsbController : ControllerBase
{
    private readonly DichVuKySoUsbToken _dichVu;

    public KySoUsbController(DichVuKySoUsbToken dichVu) => _dichVu = dichVu;

    /// <summary>Nhịp 1 — lấy giá trị băm của nội dung cần ký.</summary>
    [HttpPost("chuan-bi")]
    public async Task<IActionResult> ChuanBiAsync(
        [FromBody] ChuanBiKyUsbDto duLieu, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(duLieu);

        var ketQua = await _dichVu
            .ChuanBiAsync(duLieu.TepTinId, duLieu.DoiTuong, duLieu.DoiTuongId, ct);

        return Ok(PhanHoiApi<YeuCauKyUsbDto>.Ok(ketQua,
            "Hãy cắm USB token và xác nhận trên công cụ ký"));
    }

    /// <summary>Nhịp 3 — gửi chữ ký và chứng thư từ công cụ ký ở máy trạm về để xác minh.</summary>
    [HttpPost("{phienId:guid}/hoan-tat")]
    public async Task<IActionResult> HoanTatAsync(
        Guid phienId, [FromBody] HoanTatKyUsbDto duLieu, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(duLieu);

        if (string.IsNullOrWhiteSpace(duLieu.ChuKyBase64)
            || string.IsNullOrWhiteSpace(duLieu.ChungThuBase64))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Thiếu chữ ký hoặc chứng thư.");
        }

        var ketQua = await _dichVu
            .HoanTatAsync(phienId, duLieu.ChuKyBase64, duLieu.ChungThuBase64, ct);

        return Ok(PhanHoiApi<KetQuaKyUsbDto>.Ok(ketQua, "Đã ký số bằng USB token"));
    }

    /// <summary>Huỷ phiên ký (người dùng đóng hộp thoại giữa chừng).</summary>
    [HttpDelete("{phienId:guid}")]
    public IActionResult Huy(Guid phienId)
    {
        _dichVu.Huy(phienId);
        return Ok(PhanHoiApi.Ok("Đã huỷ phiên ký"));
    }
}
