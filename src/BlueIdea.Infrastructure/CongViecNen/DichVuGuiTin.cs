using System.Net.Http.Json;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BlueIdea.Infrastructure.CongViecNen;

/// <summary>
/// Bao hieu chua co cau hinh may chu gui tin. Job bat rieng ngoai le nay de GIU nguyen ban ghi
/// trong hang doi thay vi danh dau loi - khi quan tri vien cau hinh xong thi hang doi tu chay tiep.
/// </summary>
public sealed class ChuaCauHinhGuiTinException : Exception
{
    public ChuaCauHinhGuiTinException(string thongBao) : base(thongBao)
    {
    }
}

/// <summary>
/// Chuc nang 50 - Gui email qua SMTP (MailKit) va SMS qua API nha cung cap.
/// Cau hinh doc tu bang <c>cau_hinh_email_sms</c>, mat khau/API key giai ma AES-256-GCM.
/// </summary>
public sealed class DichVuGuiTin : IDichVuGuiTin
{
    private readonly IAppDbContext _db;
    private readonly IDichVuMaHoa _maHoa;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<DichVuGuiTin> _logger;

    public DichVuGuiTin(
        IAppDbContext db, IDichVuMaHoa maHoa, IHttpClientFactory httpFactory,
        ILogger<DichVuGuiTin> logger)
    {
        _db = db;
        _maHoa = maHoa;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task GuiEmailAsync(
        string nguoiNhan, string? tieuDe, string noiDung, CancellationToken ct = default)
    {
        var cauHinh = await LayCauHinhAsync("EMAIL", ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(cauHinh.Host) || string.IsNullOrWhiteSpace(cauHinh.EmailGuiDi))
        {
            throw new ChuaCauHinhGuiTinException(
                "Chưa cấu hình máy chủ SMTP (thiếu host hoặc địa chỉ gửi đi).");
        }

        var thu = new MimeMessage();
        thu.From.Add(new MailboxAddress(cauHinh.TenHienThi ?? cauHinh.EmailGuiDi, cauHinh.EmailGuiDi));
        thu.To.Add(MailboxAddress.Parse(nguoiNhan));
        thu.Subject = tieuDe ?? "(Không có tiêu đề)";

        // Noi dung mau thong bao la van ban thuan do quan tri vien nhap. Gui dang TextBody
        // de trinh duyet mail khong dien giai the HTML -> loai bo hoan toan nguy co HTML injection.
        thu.Body = new TextPart("plain") { Text = noiDung };

        using var smtp = new SmtpClient();

        var baoMat = cauHinh.SuDungSsl
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;

        await smtp.ConnectAsync(cauHinh.Host, cauHinh.Port ?? 587, baoMat, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(cauHinh.TenDangNhap))
        {
            var matKhau = _maHoa.GiaiMa(cauHinh.MatKhauMaHoa) ?? string.Empty;
            await smtp.AuthenticateAsync(cauHinh.TenDangNhap, matKhau, ct).ConfigureAwait(false);
        }

        await smtp.SendAsync(thu, ct).ConfigureAwait(false);
        await smtp.DisconnectAsync(true, ct).ConfigureAwait(false);

        _logger.LogInformation("Đã gửi email tới {NguoiNhan}.", nguoiNhan);
    }

    public async Task GuiSmsAsync(string soDienThoai, string noiDung, CancellationToken ct = default)
    {
        var cauHinh = await LayCauHinhAsync("SMS", ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(cauHinh.ApiEndpoint))
        {
            throw new ChuaCauHinhGuiTinException("Chưa cấu hình API nhà cung cấp SMS.");
        }

        var http = _httpFactory.CreateClient("sms");
        var apiKey = _maHoa.GiaiMa(cauHinh.ApiKeyMaHoa);

        using var yeuCau = new HttpRequestMessage(HttpMethod.Post, cauHinh.ApiEndpoint)
        {
            Content = JsonContent.Create(new
            {
                brandname = cauHinh.Brandname,
                so_dien_thoai = soDienThoai,
                noi_dung = noiDung
            })
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            yeuCau.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        }

        using var phanHoi = await http.SendAsync(yeuCau, ct).ConfigureAwait(false);
        phanHoi.EnsureSuccessStatusCode();

        _logger.LogInformation("Đã gửi SMS tới {SoDienThoai}.", soDienThoai);
    }

    private async Task<CauHinhEmailSms> LayCauHinhAsync(string loai, CancellationToken ct)
    {
        var cauHinh = await _db.CauHinhEmailSms.AsNoTracking()
            .Where(x => x.Loai == loai && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .OrderByDescending(x => x.LaMacDinh)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return cauHinh
               ?? throw new ChuaCauHinhGuiTinException(
                   $"Chưa có cấu hình '{loai}' nào đang hoạt động.");
    }
}
