using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.XacThuc;
using BlueIdea.Domain.Chung;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

/// <summary>Chức năng 21 — Đăng nhập, làm mới phiên, đổi mật khẩu, thông tin người dùng.</summary>
[ApiController]
[Route("api/v1/xac-thuc")]
[Produces("application/json")]
public sealed class XacThucController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IAppDbContext _db;

    public XacThucController(IMediator mediator, INguoiDungHienTai nguoiDung, IAppDbContext db)
    {
        _mediator = mediator;
        _nguoiDung = nguoiDung;
        _db = db;
    }

    /// <summary>Đăng nhập bằng tài khoản nội bộ. Giới hạn 5 lần/phút/IP.</summary>
    [HttpPost("dang-nhap")]
    [AllowAnonymous]
    [EnableRateLimiting("DangNhap")]
    [ProducesResponseType(typeof(PhanHoiApi<KetQuaDangNhap>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PhanHoiApi), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DangNhapAsync(
        [FromBody] DangNhapCommand yeuCau, CancellationToken ct)
    {
        var ketQua = await _mediator.Send(yeuCau, ct);
        return Ok(PhanHoiApi<KetQuaDangNhap>.Ok(ketQua, "Đăng nhập thành công"));
    }

    /// <summary>Làm mới access token bằng refresh token (token cũ bị thu hồi).</summary>
    [HttpPost("lam-moi-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PhanHoiApi<KetQuaDangNhap>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LamMoiTokenAsync(
        [FromBody] LamMoiTokenCommand yeuCau, CancellationToken ct)
    {
        var ketQua = await _mediator.Send(yeuCau, ct);
        return Ok(PhanHoiApi<KetQuaDangNhap>.Ok(ketQua));
    }

    /// <summary>Đăng xuất và thu hồi refresh token.</summary>
    [HttpPost("dang-xuat")]
    [Authorize]
    [ProducesResponseType(typeof(PhanHoiApi), StatusCodes.Status200OK)]
    public async Task<IActionResult> DangXuatAsync(
        [FromBody] DangXuatCommand yeuCau, CancellationToken ct)
    {
        await _mediator.Send(yeuCau, ct);
        return Ok(PhanHoiApi.Ok("Đã đăng xuất"));
    }

    /// <summary>Đổi mật khẩu (áp dụng chính sách mật khẩu cấu hình được).</summary>
    [HttpPost("doi-mat-khau")]
    [Authorize]
    [ProducesResponseType(typeof(PhanHoiApi), StatusCodes.Status200OK)]
    public async Task<IActionResult> DoiMatKhauAsync(
        [FromBody] DoiMatKhauCommand yeuCau, CancellationToken ct)
    {
        await _mediator.Send(yeuCau, ct);
        return Ok(PhanHoiApi.Ok("Đổi mật khẩu thành công. Vui lòng đăng nhập lại."));
    }

    /// <summary>Thông tin người dùng đang đăng nhập (dùng khi tải lại trang).</summary>
    [HttpGet("toi")]
    [Authorize]
    [ProducesResponseType(typeof(PhanHoiApi<ThongTinNguoiDungDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayThongTinAsync(CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
        {
            return Unauthorized(PhanHoiApi.Loi("CHUA_XAC_THUC", "Chưa đăng nhập."));
        }

        var nguoiDung = await _db.NguoiDung.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == _nguoiDung.Id.Value, ct);

        if (nguoiDung is null)
        {
            return Unauthorized(PhanHoiApi.Loi("CHUA_XAC_THUC", "Tài khoản không còn tồn tại."));
        }

        var tenDonVi = nguoiDung.DonViId.HasValue
            ? await _db.DonVi.AsNoTracking()
                .Where(x => x.Id == nguoiDung.DonViId.Value)
                .Select(x => x.Ten)
                .FirstOrDefaultAsync(ct)
            : null;

        var dto = new ThongTinNguoiDungDto(
            nguoiDung.Id, nguoiDung.TenDangNhap, nguoiDung.HoTen, nguoiDung.Email,
            nguoiDung.ChucVu, nguoiDung.DonViId, tenDonVi,
            _nguoiDung.VaiTro.ToList(), _nguoiDung.Quyen.ToList(), nguoiDung.MfaEnabled);

        return Ok(PhanHoiApi<ThongTinNguoiDungDto>.Ok(dto));
    }

    /// <summary>Chức năng 48 — Menu đã lọc theo quyền của người dùng hiện tại.</summary>
    [HttpGet("menu")]
    [Authorize]
    [ProducesResponseType(typeof(PhanHoiApi<IReadOnlyList<MenuDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayMenuAsync(
        [FromQuery] string loai = "WEB", CancellationToken ct = default)
    {
        var tatCa = await _db.CauHinhMenu.AsNoTracking()
            .Where(x => x.Loai == loai && x.HienThi)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(ct);

        var quyen = _nguoiDung.Quyen;
        var laQuanTri = _nguoiDung.VaiTro.Contains(MaVaiTro.QuanTriHeThong);

        bool DuocXem(Domain.QuanTri.CauHinhMenu m)
            => laQuanTri || string.IsNullOrEmpty(m.QuyenMa) || quyen.Contains(m.QuyenMa);

        var choPhep = tatCa.Where(DuocXem).ToList();

        MenuDto ChuyenDoi(Domain.QuanTri.CauHinhMenu m) => new(
            m.Id, m.Ma, m.Ten, m.Icon, m.DuongDan, m.ThuTu, m.MoTabMoi,
            choPhep.Where(c => c.MenuChaId == m.Id).OrderBy(c => c.ThuTu).Select(ChuyenDoi).ToList());

        // Menu cha khong co duong dan va khong con menu con hop le thi an di.
        var ketQua = choPhep
            .Where(m => m.MenuChaId is null)
            .Select(ChuyenDoi)
            .Where(m => !string.IsNullOrEmpty(m.DuongDan) || m.MenuCon.Count > 0)
            .ToList();

        return Ok(PhanHoiApi<IReadOnlyList<MenuDto>>.Ok(ketQua));
    }
}
