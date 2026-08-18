using BlueIdea.Application.Chung;
using BlueIdea.Application.TichHop;
using BlueIdea.Domain.Chung;
using BlueIdea.Infrastructure.DichVu;
using BlueIdea.Shared.KetQua;

namespace BlueIdea.Api.Chung;

/// <summary>
/// Giới hạn địa chỉ IP được dùng tài khoản quản trị (Mục 6 đặc tả — an toàn thông tin cấp độ 2).
///
/// Chặn theo **vai trò** chứ không theo đường dẫn: quản trị viên đụng tới rất nhiều endpoint dùng
/// chung với người dùng thường, nên lọc theo tiền tố đường dẫn sẽ vừa sót vừa khó bảo trì. Cách
/// này nói đúng điều đơn vị muốn — "tài khoản quản trị chỉ dùng được từ trong mạng cơ quan".
///
/// Để trống danh sách = không giới hạn, và đó là mặc định: bật sẵn khi nâng cấp sẽ khoá luôn
/// người đang vận hành hệ thống.
///
/// Đặt SAU UseAuthentication vì cần biết người gọi là ai; và trước UseAuthorization để yêu cầu
/// bị chặn không kịp chạm vào nghiệp vụ.
/// </summary>
public sealed class ChanIpQuanTri
{
    private readonly RequestDelegate _tiep;
    private readonly ILogger<ChanIpQuanTri> _logger;

    public ChanIpQuanTri(RequestDelegate tiep, ILogger<ChanIpQuanTri> logger)
    {
        _tiep = tiep;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ngCanh, IDichVuCauHinh cauHinh)
    {
        if (!LaTaiKhoanQuanTri(ngCanh))
        {
            await _tiep(ngCanh).ConfigureAwait(false);
            return;
        }

        var khaiBao = await cauHinh
            .LayAsync(KhoaCauHinh.IpChoPhepQuanTri, ngCanh.RequestAborted)
            .ConfigureAwait(false);

        var danhSach = TachDanhSach(khaiBao);

        if (danhSach.Count == 0)
        {
            await _tiep(ngCanh).ConfigureAwait(false);
            return;
        }

        // Sau UseForwardedHeaders thi day da la IP that cua nguoi dung, khong con la IP cua Nginx.
        var ip = ngCanh.Connection.RemoteIpAddress?.ToString();

        if (DichVuKhoaApiNgoai.IpDuocPhep(danhSach, ip))
        {
            await _tiep(ngCanh).ConfigureAwait(false);
            return;
        }

        _logger.LogWarning(
            "Chan tai khoan quan tri {TenDangNhap} tu IP {Ip} — khong nam trong danh sach cho phep.",
            ngCanh.User.Identity?.Name, ip);

        ngCanh.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ngCanh.Response.WriteAsJsonAsync(
            PhanHoiApi.Loi(MaLoiHeThong.KhongCoQuyen,
                "Tài khoản quản trị chỉ được dùng từ dải địa chỉ đã đăng ký."),
            ngCanh.RequestAborted).ConfigureAwait(false);
    }

    private static bool LaTaiKhoanQuanTri(HttpContext ngCanh)
        => ngCanh.User.Identity?.IsAuthenticated == true
           && ngCanh.User.HasClaim(NguoiDungHienTai.ClaimVaiTro, MaVaiTro.QuanTriHeThong);

    private static List<string> TachDanhSach(string? khaiBao)
        => string.IsNullOrWhiteSpace(khaiBao)
            ? new List<string>()
            : khaiBao.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
}
