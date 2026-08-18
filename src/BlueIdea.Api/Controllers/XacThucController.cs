using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.XacThuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace BlueIdea.Api.Controllers;

/// <summary>Dữ liệu client gửi lên để đổi authorization code lấy token.</summary>
public sealed record DoiMaSsoDto(string Code, string CodeVerifier, string DuongDanTraVe, string State);

/// <summary>Yêu cầu gửi mã đặt lại mật khẩu — nhận tên đăng nhập hoặc email.</summary>
public sealed record YeuCauQuenMatKhauDto(string DinhDanh);

/// <summary>Đổi mã OTP lấy mật khẩu mới.</summary>
public sealed record DatLaiMatKhauDto(string TenDangNhap, string Ma, string MatKhauMoi);

/// <summary>Dữ liệu để dựng địa chỉ đăng xuất phía nhà cung cấp SSO.</summary>
public sealed record DiaChiDangXuatSsoDto(string? IdToken, string DuongDanTraVe);

/// <summary>Chức năng 21 — Đăng nhập, làm mới phiên, đổi mật khẩu, thông tin người dùng.</summary>
[ApiController]
[Route("api/v1/xac-thuc")]
[Produces("application/json")]
public sealed class XacThucController : ControllerBase
{
    private const string TienToSsoState = "SSO_STATE:";
    private static readonly TimeSpan ThoiGianSongState = TimeSpan.FromMinutes(5);

    private readonly IMediator _mediator;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IAppDbContext _db;
    private readonly DichVuDangNhapSso _sso;
    private readonly DichVuCaptcha _captcha;
    private readonly DichVuQuenMatKhau _quenMatKhau;
    private readonly IDistributedCache _cachePhanTan;
    private readonly IConfiguration _cauHinh;
    private readonly IDichVuCauHinh _dichVuCauHinh;

    public XacThucController(
        IMediator mediator,
        INguoiDungHienTai nguoiDung,
        IAppDbContext db,
        DichVuDangNhapSso sso,
        DichVuCaptcha captcha,
        DichVuQuenMatKhau quenMatKhau,
        IDistributedCache cachePhanTan,
        IConfiguration cauHinh,
        IDichVuCauHinh dichVuCauHinh)
    {
        _mediator = mediator;
        _nguoiDung = nguoiDung;
        _db = db;
        _sso = sso;
        _captcha = captcha;
        _quenMatKhau = quenMatKhau;
        _cachePhanTan = cachePhanTan;
        _cauHinh = cauHinh;
        _dichVuCauHinh = dichVuCauHinh;
    }

    /// <summary>Sinh ảnh CAPTCHA (SVG) cho trang đăng nhập.</summary>
    [HttpGet("captcha")]
    [AllowAnonymous]
    [EnableRateLimiting("DangNhap")]
    public async Task<IActionResult> TaoCaptchaAsync(CancellationToken ct)
        => Ok(PhanHoiApi<ThuThachCaptchaDto>.Ok(await _captcha.TaoAsync(ct)));

    /// <summary>
    /// Bước 1 quên mật khẩu — gửi mã OTP qua email.
    ///
    /// LUÔN trả về thành công, kể cả khi tài khoản không tồn tại: phản hồi khác nhau sẽ biến
    /// endpoint này thành công cụ dò danh sách tài khoản của cơ quan.
    /// </summary>
    [HttpPost("quen-mat-khau")]
    [AllowAnonymous]
    [EnableRateLimiting("DangNhap")]
    public async Task<IActionResult> QuenMatKhauAsync(
        [FromBody] YeuCauQuenMatKhauDto duLieu, CancellationToken ct)
    {
        await _quenMatKhau.YeuCauMaAsync(duLieu.DinhDanh, ct);

        return Ok(PhanHoiApi.Ok(
            "Nếu thông tin khớp với một tài khoản, mã đặt lại mật khẩu đã được gửi tới email đăng ký."));
    }

    /// <summary>Bước 2 quên mật khẩu — đổi mã OTP lấy mật khẩu mới.</summary>
    [HttpPost("dat-lai-mat-khau")]
    [AllowAnonymous]
    [EnableRateLimiting("DangNhap")]
    public async Task<IActionResult> DatLaiMatKhauAsync(
        [FromBody] DatLaiMatKhauDto duLieu, CancellationToken ct)
    {
        await _quenMatKhau.DatLaiAsync(duLieu.TenDangNhap, duLieu.Ma, duLieu.MatKhauMoi, ct);

        return Ok(PhanHoiApi.Ok("Đã đặt lại mật khẩu. Vui lòng đăng nhập lại."));
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
    // ------------------------------------------------------ Chức năng 21, 41 — SSO (OIDC)

    /// <summary>Hệ thống có bật đăng nhập một lần hay không — trang đăng nhập dùng để ẩn/hiện nút.</summary>
    [HttpGet("sso/trang-thai")]
    [AllowAnonymous]
    public IActionResult LayTrangThaiSso()
        => Ok(PhanHoiApi<object>.Ok(new { daCauHinh = _sso.DaCauHinh }));

    /// <summary>
    /// Bắt đầu luồng SSO: sinh <c>state</c> và cặp PKCE, gửi về cho client rồi chuyển hướng
    /// sang nhà cung cấp.
    ///
    /// <c>state</c> được lưu server-side (IDistributedCache — Redis khi triển khai nhiều bản,
    /// in-memory khi chạy đơn lẻ hoặc kiểm thử) với TTL 5 phút, kiểm tra lại khi đổi mã
    /// để chống CSRF.
    /// </summary>
    [HttpGet("sso/bat-dau")]
    [AllowAnonymous]
    [EnableRateLimiting("DangNhap")]
    public async Task<IActionResult> BatDauSsoAsync(
        [FromQuery] string duongDanTraVe, CancellationToken ct)
    {
        KiemTraDuongDanTraVe(duongDanTraVe);

        var state = TaoChuoiNgauNhien();
        var codeVerifier = TaoChuoiNgauNhien();
        var codeChallenge = TaoCodeChallenge(codeVerifier);

        await _cachePhanTan.SetAsync(
            TienToSsoState + state,
            [1],
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ThoiGianSongState
            },
            ct);

        var diaChi = _sso.TaoDiaChiDangNhap(state, codeChallenge, duongDanTraVe);

        return Ok(PhanHoiApi<object>.Ok(new { diaChi, state, codeVerifier }));
    }

    /// <summary>Đổi authorization code lấy token của hệ thống.</summary>
    [HttpPost("sso/doi-ma")]
    [AllowAnonymous]
    [EnableRateLimiting("DangNhap")]
    public async Task<IActionResult> DoiMaSsoAsync(
        [FromBody] DoiMaSsoDto duLieu, CancellationToken ct)
    {
        KiemTraDuongDanTraVe(duLieu.DuongDanTraVe);

        var khoaState = TienToSsoState + duLieu.State;
        var giaTri = await _cachePhanTan.GetAsync(khoaState, ct);
        if (giaTri is null)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Phiên SSO không hợp lệ hoặc đã hết hạn.");
        }

        await _cachePhanTan.RemoveAsync(khoaState, ct);

        var ketQua = await _sso.XuLyTraVeAsync(
            duLieu.Code, duLieu.CodeVerifier, duLieu.DuongDanTraVe, ct);

        return Ok(PhanHoiApi<KetQuaDangNhap>.Ok(ketQua, "Đăng nhập SSO thành công"));
    }

    /// <summary>
    /// Chan open redirect: chi chap nhan duong dan tra ve co scheme+host+port trung voi
    /// Cors:NguonChoPhep hoac Sso:AllowedRedirectHosts.
    /// </summary>
    private void KiemTraDuongDanTraVe(string? duongDanTraVe)
    {
        if (string.IsNullOrWhiteSpace(duongDanTraVe)
            || !Uri.TryCreate(duongDanTraVe, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Đường dẫn trả về không hợp lệ.");
        }

        var nguonChoPhep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var nguonCors = _cauHinh.GetSection("Cors:NguonChoPhep").Get<string[]>();
        if (nguonCors is not null)
        {
            foreach (var nguon in nguonCors)
            {
                if (Uri.TryCreate(nguon, UriKind.Absolute, out var u))
                    nguonChoPhep.Add(u.GetLeftPart(UriPartial.Authority));
            }
        }

        var ssoHosts = _cauHinh.GetSection("Sso:AllowedRedirectHosts").Get<string[]>();
        if (ssoHosts is not null)
        {
            foreach (var h in ssoHosts)
            {
                var trimmed = h.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (Uri.TryCreate(trimmed, UriKind.Absolute, out var u) && u.Scheme is "http" or "https")
                    nguonChoPhep.Add(u.GetLeftPart(UriPartial.Authority));
                else
                    nguonChoPhep.Add($"https://{trimmed}");
            }
        }

        var nguonYeuCau = uri.GetLeftPart(UriPartial.Authority);

        if (nguonChoPhep.Count == 0 || !nguonChoPhep.Contains(nguonYeuCau))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Đường dẫn trả về không thuộc miền được phép.");
        }
    }

    /// <summary>Chuỗi ngẫu nhiên mật mã dùng cho state và code_verifier (RFC 7636).</summary>
    private static string TaoChuoiNgauNhien()
        => Base64Url(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    private static string TaoCodeChallenge(string codeVerifier)
        => Base64Url(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.ASCII.GetBytes(codeVerifier)));

    /// <summary>Base64 URL-safe không đệm, đúng yêu cầu của PKCE.</summary>
    private static string Base64Url(byte[] duLieu)
        => Convert.ToBase64String(duLieu).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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

    /// <summary>
    /// Chức năng 41 — Single logout: địa chỉ để kết thúc luôn phiên bên nhà cung cấp SSO.
    ///
    /// Trả về <c>null</c> khi hệ thống không dùng SSO hoặc nhà cung cấp không công bố
    /// <c>end_session_endpoint</c>. Khi đó client chỉ đăng xuất cục bộ — vẫn đúng, chỉ là
    /// phiên bên nhà cung cấp còn sống.
    /// </summary>
    [HttpPost("sso/dia-chi-dang-xuat")]
    [Authorize]
    public async Task<IActionResult> LayDiaChiDangXuatSsoAsync(
        [FromBody] DiaChiDangXuatSsoDto duLieu, CancellationToken ct)
    {
        KiemTraDuongDanTraVe(duLieu.DuongDanTraVe);

        return Ok(PhanHoiApi<object>.Ok(new
        {
            diaChi = await _sso.TaoDiaChiDangXuatAsync(duLieu.IdToken, duLieu.DuongDanTraVe, ct)
        }));
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

        // Tai lai trang cung phai biet: nguoi dung F5 giua chung khong duoc thoat khoi rang buoc.
        var buocBatMfa = !nguoiDung.MfaEnabled
                         && _nguoiDung.VaiTro.Contains(MaVaiTro.QuanTriHeThong)
                         && await _dichVuCauHinh.LayAsync(KhoaCauHinh.MfaBatBuocQuanTri, false, ct);

        var dto = new ThongTinNguoiDungDto(
            nguoiDung.Id, nguoiDung.TenDangNhap, nguoiDung.HoTen, nguoiDung.Email,
            nguoiDung.ChucVu, nguoiDung.DonViId, tenDonVi,
            _nguoiDung.VaiTro.ToList(), _nguoiDung.Quyen.ToList(), nguoiDung.MfaEnabled,
            buocBatMfa);

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
