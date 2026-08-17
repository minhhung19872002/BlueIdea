using BlueIdea.Api.Chung;
using BlueIdea.Application.XacThuc;
using BlueIdea.Domain.Chung;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>Mã người dùng nhập từ ứng dụng xác thực (hoặc một mã khôi phục).</summary>
public sealed record MaMfaDto(string Ma);

/// <summary>Tắt MFA / sinh lại mã khôi phục — bắt buộc nhập lại mật khẩu.</summary>
public sealed record XacNhanBangMatKhauDto(string MatKhau, string? Ma);

/// <summary>
/// Chức năng 21 — Xác thực hai lớp (TOTP) cho tài khoản.
///
/// Toàn bộ endpoint ở đây thao tác trên CHÍNH tài khoản đang đăng nhập, không nhận
/// <c>nguoiDungId</c> từ client — trừ endpoint gỡ MFA dành cho quản trị viên. Nếu nhận id từ
/// client thì bất kỳ ai đăng nhập được cũng tắt được MFA của người khác.
/// </summary>
[ApiController]
[Route("api/v1/xac-thuc/mfa")]
[Authorize]
[Produces("application/json")]
public sealed class MfaController : ControllerBase
{
    private readonly DichVuMfa _mfa;

    public MfaController(DichVuMfa mfa) => _mfa = mfa;

    /// <summary>Trạng thái MFA của tài khoản đang đăng nhập.</summary>
    [HttpGet("trang-thai")]
    public async Task<IActionResult> TrangThaiAsync(CancellationToken ct)
        => Ok(PhanHoiApi<TrangThaiMfaDto>.Ok(await _mfa.TrangThaiAsync(ct)));

    /// <summary>Bước 1 — sinh bí mật và chuỗi otpauth để quét mã QR.</summary>
    [HttpPost("bat-dau-ghi-danh")]
    public async Task<IActionResult> BatDauGhiDanhAsync(CancellationToken ct)
        => Ok(PhanHoiApi<BatDauGhiDanhMfaDto>.Ok(await _mfa.BatDauGhiDanhAsync(ct)));

    /// <summary>
    /// Bước 2 — nhập mã từ ứng dụng xác thực để bật MFA. Trả về bộ mã khôi phục,
    /// và đây là lần duy nhất chúng được hiển thị.
    /// </summary>
    [HttpPost("xac-nhan-ghi-danh")]
    public async Task<IActionResult> XacNhanGhiDanhAsync(
        [FromBody] MaMfaDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<KetQuaBatMfaDto>.Ok(
            await _mfa.XacNhanGhiDanhAsync(duLieu.Ma, ct),
            "Đã bật xác thực hai lớp. Hãy lưu bộ mã khôi phục ở nơi an toàn."));

    /// <summary>Tắt xác thực hai lớp.</summary>
    [HttpPost("tat")]
    public async Task<IActionResult> TatAsync(
        [FromBody] XacNhanBangMatKhauDto duLieu, CancellationToken ct)
    {
        await _mfa.TatAsync(duLieu.MatKhau, duLieu.Ma, ct);
        return Ok(PhanHoiApi.Ok("Đã tắt xác thực hai lớp"));
    }

    /// <summary>Sinh lại bộ mã khôi phục, huỷ toàn bộ bộ cũ.</summary>
    [HttpPost("tao-lai-ma-khoi-phuc")]
    public async Task<IActionResult> TaoLaiMaKhoiPhucAsync(
        [FromBody] XacNhanBangMatKhauDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<KetQuaBatMfaDto>.Ok(
            await _mfa.TaoLaiMaKhoiPhucAsync(duLieu.MatKhau, ct),
            "Đã sinh bộ mã khôi phục mới. Bộ mã cũ không còn dùng được."));

    /// <summary>
    /// Quản trị viên gỡ MFA cho tài khoản khác — dùng khi người dùng mất thiết bị và hết mã
    /// khôi phục. Ghi nhật ký như mọi thao tác đổi dữ liệu.
    /// </summary>
    [HttpPost("go/{nguoiDungId:guid}")]
    [Authorize(Policy = MaQuyen.NguoiDungDatLaiMatKhau)]
    public async Task<IActionResult> GoChoNguoiKhacAsync(Guid nguoiDungId, CancellationToken ct)
    {
        await _mfa.GoMfaChoNguoiKhacAsync(nguoiDungId, ct);
        return Ok(PhanHoiApi.Ok("Đã gỡ xác thực hai lớp cho tài khoản"));
    }
}
