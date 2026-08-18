using System.Net;
using System.Net.Sockets;

namespace BlueIdea.Api.Chung;

/// <summary>
/// Chỉ cho phép đọc <c>/metrics</c> từ máy chủ nội bộ.
///
/// Số đếm mang theo đường dẫn, mã trạng thái và nhịp sử dụng của hệ thống — đủ để người ngoài
/// đoán ra có bao nhiêu hồ sơ và giờ nào ít người trực.
///
/// Vì sao chặn ở đây chứ không chỉ ở Nginx: <c>deploy/nginx/blueidea.conf</c> là cấu hình của
/// máy chủ, phải cài lại bằng tay sau mỗi lần sửa. Một endpoint mới đi theo ảnh Docker sẽ lên
/// trước khi ai kịp cài lại Nginx — tức là có một khoảng thời gian nó phơi ra Internet. Chặn tại
/// ứng dụng thì endpoint an toàn ngay từ lần triển khai đầu tiên, và vẫn an toàn nếu sau này có
/// người dựng lại máy chủ mà quên khối <c>location /metrics</c>.
///
/// Trả 404 chứ không phải 403: báo "cấm" cho một địa chỉ đoán mò cũng là xác nhận nó có tồn tại.
/// </summary>
public sealed class ChanMetricsTuNgoai
{
    private readonly RequestDelegate _tiep;

    public ChanMetricsTuNgoai(RequestDelegate tiep) => _tiep = tiep;

    public async Task InvokeAsync(HttpContext ngCanh)
    {
        if (!ngCanh.Request.Path.StartsWithSegments("/metrics")
            || LaMangNoiBo(ngCanh.Connection.RemoteIpAddress))
        {
            await _tiep(ngCanh).ConfigureAwait(false);
            return;
        }

        ngCanh.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    /// <summary>
    /// Loopback hoac dai IP rieng (RFC 1918 / RFC 4193) — noi Prometheus va cac container cung
    /// mang Docker goi toi.
    /// </summary>
    private static bool LaMangNoiBo(IPAddress? ip)
    {
        if (ip is null) return false;

        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();

            return b[0] switch
            {
                10 => true,
                127 => true,
                172 => b[1] >= 16 && b[1] <= 31,
                192 => b[1] == 168,
                _ => false,
            };
        }

        // IPv6: unique local address (fc00::/7).
        return ip.AddressFamily == AddressFamily.InterNetworkV6
               && (ip.GetAddressBytes()[0] & 0xFE) == 0xFC;
    }
}
