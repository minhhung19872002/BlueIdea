using System.Net;
using System.Text;
using BlueIdea.Infrastructure.CongViecNen;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueIdea.UnitTests.CongViecNen;

/// <summary>
/// Kiem thu lop goi dich vu OCR NOI BO.
///
/// Trong tam: (1) anh xa dung JSON snake_case cua FastAPI, (2) khi dich vu loi hoac khong ket noi
/// duoc thi phai suy giam mem chu KHONG duoc nem ra ngoai lam hong luong nop ho so.
/// </summary>
public sealed class DichVuOcrNoiBoTests
{
    [Theory]
    [InlineData(".pdf", true)]
    [InlineData(".PDF", true)]
    [InlineData(".png", true)]
    [InlineData(".jpg", true)]
    [InlineData(".jpeg", true)]
    [InlineData(".docx", false)]
    [InlineData(".xlsx", false)]
    [InlineData(".zip", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Chi_Ho_Tro_Dinh_Dang_Rut_Duoc_Van_Ban(string? phanMoRong, bool mongDoi)
    {
        var dichVu = TaoDichVu(_ => TaoPhanHoi(HttpStatusCode.OK, "{}"));

        dichVu.HoTro(phanMoRong).Should().Be(mongDoi);
    }

    [Fact]
    public async Task Doc_Dung_Phan_Hoi_Snake_Case_Cua_Dich_Vu_Python()
    {
        const string json = """
            {
              "thanh_cong": true,
              "van_ban": "Sáng kiến cải tiến quy trình tiếp nhận hồ sơ",
              "so_trang": 3,
              "so_ky_tu": 46,
              "phuong_phap": "OCR",
              "thoi_gian_ms": 1234
            }
            """;

        var dichVu = TaoDichVu(_ => TaoPhanHoi(HttpStatusCode.OK, json));

        var ketQua = await dichVu.TrichXuatAsync(
            new MemoryStream("%PDF-1.4"u8.ToArray()), "ho-so.pdf", "application/pdf");

        ketQua.ThanhCong.Should().BeTrue();
        ketQua.VanBan.Should().Be("Sáng kiến cải tiến quy trình tiếp nhận hồ sơ");
        ketQua.SoTrang.Should().Be(3);
        ketQua.PhuongPhap.Should().Be("OCR");
    }

    [Fact]
    public async Task Goi_Dung_Endpoint_Ocr_Voi_Multipart()
    {
        HttpRequestMessage? daGui = null;

        var dichVu = TaoDichVu(yeuCau =>
        {
            daGui = yeuCau;
            return TaoPhanHoi(HttpStatusCode.OK, """{"thanh_cong":true,"van_ban":"x","so_trang":1}""");
        });

        await dichVu.TrichXuatAsync(new MemoryStream([1, 2, 3]), "anh.png", "image/png");

        daGui.Should().NotBeNull();
        daGui!.Method.Should().Be(HttpMethod.Post);
        daGui.RequestUri!.AbsolutePath.Should().Be("/ocr");
        daGui.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public async Task Dich_Vu_Tra_Loi_Thi_Bao_That_Bai_Chu_Khong_Nem_Ngoai_Le()
    {
        var dichVu = TaoDichVu(_ =>
            TaoPhanHoi(HttpStatusCode.InternalServerError, """{"detail":"Tesseract chết"}"""));

        var ketQua = await dichVu.TrichXuatAsync(new MemoryStream([1]), "a.pdf", "application/pdf");

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.VanBan.Should().BeEmpty();
        ketQua.ThongBaoLoi.Should().Contain("500");
    }

    [Fact]
    public async Task Khong_Ket_Noi_Duoc_Thi_Suy_Giam_Mem()
    {
        // Day la rang buoc quan trong: ho so van phai nop duoc khi dich vu AI chet.
        var dichVu = TaoDichVu(_ => throw new HttpRequestException("Không phân giải được tên miền"));

        var ketQua = await dichVu.TrichXuatAsync(new MemoryStream([1]), "a.pdf", "application/pdf");

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.ThongBaoLoi.Should().Contain("Không kết nối được");
    }

    [Fact]
    public async Task Het_Thoi_Gian_Cho_Cung_Suy_Giam_Mem()
    {
        var dichVu = TaoDichVu(_ => throw new TaskCanceledException("Hết thời gian chờ"));

        var ketQua = await dichVu.TrichXuatAsync(new MemoryStream([1]), "a.pdf", "application/pdf");

        ketQua.ThanhCong.Should().BeFalse();
    }

    // ------------------------------------------------------------------------------------

    private static DichVuOcrNoiBo TaoDichVu(Func<HttpRequestMessage, HttpResponseMessage> xuLy)
    {
        var http = new HttpClient(new BoXuLyGia(xuLy))
        {
            BaseAddress = new Uri("http://ai-service:8000")
        };

        return new DichVuOcrNoiBo(http, NullLogger<DichVuOcrNoiBo>.Instance);
    }

    private static HttpResponseMessage TaoPhanHoi(HttpStatusCode ma, string noiDung)
        => new(ma)
        {
            Content = new StringContent(noiDung, Encoding.UTF8, "application/json")
        };

    /// <summary>Bo xu ly HTTP gia - thay the mang de kiem thu tat dinh, khong can dich vu that.</summary>
    private sealed class BoXuLyGia : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _xuLy;

        public BoXuLyGia(Func<HttpRequestMessage, HttpResponseMessage> xuLy) => _xuLy = xuLy;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_xuLy(request));
    }
}
