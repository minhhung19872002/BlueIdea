using BlueIdea.IntegrationTests.HaTang;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Hai yeu cau phi chuc nang o Muc 7 dac ta: giam sat bang metrics Prometheus, va chiu loi bang
/// ngat mach cho moi loi goi ra he thong ngoai.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class GiamSatVaChiuLoiTests
{
    private readonly UngDungKiemThu _ungDung;

    public GiamSatVaChiuLoiTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Endpoint_Metrics_Tra_Ve_Dinh_Dang_Prometheus()
    {
        var khach = _ungDung.CreateClient();

        // Sinh mot chut luu luong de co so lieu.
        await khach.GetAsync("/api/v1/he-thong/cau-hinh-cong-khai");

        var phanHoi = await khach.GetAsync("/metrics");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadAsStringAsync();

        noiDung.Should().Contain("# HELP", "định dạng phơi bày của Prometheus bắt đầu bằng dòng HELP");
        noiDung.Should().Contain("http_request",
            "phải có bộ đếm HTTP, nếu không thì endpoint chỉ phơi ra số liệu của .NET runtime");
    }

    /// <summary>
    /// /metrics khong duoc doc tu ngoai mang noi bo.
    ///
    /// Chan ngay trong ung dung chu khong chi o Nginx: cau hinh Nginx cua may chu phai cai lai
    /// bang tay, con endpoint moi thi len theo anh Docker — tuc la co mot khoang thoi gian no
    /// phoi ra Internet neu chi trong cho Nginx.
    /// </summary>
    [Fact]
    public async Task Metrics_Tu_Dia_Chi_Ngoai_Bi_Tu_Choi()
    {
        var khach = _ungDung.CreateClient();
        khach.DefaultRequestHeaders.Add("X-Dia-Chi-Gia-Lap", "203.0.113.10");

        var phanHoi = await khach.GetAsync("/metrics");

        phanHoi.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound,
            "trả 404 chứ không phải 403: báo cấm cho một địa chỉ đoán mò cũng là xác nhận nó có thật");
    }

    /// <summary>
    /// Moi HttpClient goi ra ngoai deu phai co chinh sach chiu loi.
    ///
    /// Khong co ngat mach thi mot he thong ngoai treo se lam moi yeu cau cua nguoi dung phai cho
    /// het timeout, cac luong nen tich lai, va mot dich vu ngoai keo sap ca he thong.
    /// </summary>
    [Theory]
    [InlineData("lien-thong")]
    [InlineData("sms")]
    public void HttpClient_Goi_Ra_Ngoai_Deu_Co_Chinh_Sach_Chiu_Loi(string ten)
    {
        var factory = _ungDung.Services.GetRequiredService<IHttpMessageHandlerFactory>();

        var handler = factory.CreateHandler(ten);

        // Polly gan vao chuoi duoi ten PolicyHttpMessageHandler.
        TenCacHandler(handler).Should().Contain(
            x => x.Contains("PolicyHttpMessageHandler", StringComparison.Ordinal),
            $"HttpClient '{ten}' gọi ra hệ thống ngoài nên phải có ngắt mạch");
    }

    private static IEnumerable<string> TenCacHandler(HttpMessageHandler? handler)
    {
        while (handler is not null)
        {
            yield return handler.GetType().Name;

            handler = handler is DelegatingHandler uyQuyen ? uyQuyen.InnerHandler : null;
        }
    }
}
