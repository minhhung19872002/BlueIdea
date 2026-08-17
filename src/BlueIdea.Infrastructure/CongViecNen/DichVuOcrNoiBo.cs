using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BlueIdea.Application.Chung;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.CongViecNen;

/// <summary>
/// Trich xuat van ban bang dich vu OCR NOI BO (Tesseract 5, traineddata vie + eng).
///
/// Dich vu nay chay trong mang rieng cua docker-compose va KHONG duoc expose ra ngoai.
/// Tuyet doi khong goi API AI ben thu ba - xem docs/ADR/0001-ai-noi-bo.md.
/// </summary>
public sealed class DichVuOcrNoiBo : IDichVuOcr
{
    /// <summary>Chi nhung dinh dang thuc su rut duoc van ban moi goi OCR.</summary>
    private static readonly HashSet<string> DinhDangHoTro = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg"
    };

    private readonly HttpClient _http;
    private readonly ILogger<DichVuOcrNoiBo> _logger;

    public DichVuOcrNoiBo(HttpClient http, ILogger<DichVuOcrNoiBo> logger)
    {
        _http = http;
        _logger = logger;
    }

    public bool HoTro(string? phanMoRong)
        => !string.IsNullOrWhiteSpace(phanMoRong) && DinhDangHoTro.Contains(phanMoRong);

    public async Task<KetQuaTrichXuatVanBan> TrichXuatAsync(
        Stream noiDung, string tenTep, string? mimeType, CancellationToken ct = default)
    {
        try
        {
            using var noiDungForm = new MultipartFormDataContent();
            using var noiDungTep = new StreamContent(noiDung);

            noiDungTep.Headers.ContentType =
                new MediaTypeHeaderValue(mimeType ?? "application/octet-stream");

            noiDungForm.Add(noiDungTep, "tep", tenTep);

            // Endpoint /ocr da tu uu tien text-layer va chi OCR nhung trang khong co van ban,
            // nen goi thang mot lan la du - khong can tu phan nhanh o phia .NET.
            using var phanHoi = await _http.PostAsync("/ocr", noiDungForm, ct).ConfigureAwait(false);

            if (!phanHoi.IsSuccessStatusCode)
            {
                var chiTiet = await phanHoi.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                _logger.LogWarning(
                    "Dịch vụ OCR trả về {MaTrangThai} cho tệp {TenTep}: {ChiTiet}",
                    (int)phanHoi.StatusCode, tenTep, chiTiet);

                return new KetQuaTrichXuatVanBan
                {
                    ThanhCong = false,
                    ThongBaoLoi = $"OCR trả về HTTP {(int)phanHoi.StatusCode}."
                };
            }

            var ketQua = await phanHoi.Content
                .ReadFromJsonAsync<PhanHoiOcr>(cancellationToken: ct)
                .ConfigureAwait(false);

            if (ketQua is null)
            {
                return new KetQuaTrichXuatVanBan
                {
                    ThanhCong = false,
                    ThongBaoLoi = "Dịch vụ OCR trả về nội dung rỗng."
                };
            }

            return new KetQuaTrichXuatVanBan
            {
                ThanhCong = ketQua.ThanhCong,
                VanBan = ketQua.VanBan ?? string.Empty,
                SoTrang = ketQua.SoTrang,
                PhuongPhap = ketQua.PhuongPhap ?? "TEXT_LAYER"
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Khong bat duoc dich vu OCR thi ho so van phai nop duoc: day la buoc lam giau
            // du lieu, khong phai dieu kien nghiep vu (graceful degradation - Muc 7 dac ta).
            _logger.LogWarning(ex, "Không gọi được dịch vụ OCR cho tệp {TenTep}.", tenTep);

            return new KetQuaTrichXuatVanBan
            {
                ThanhCong = false,
                ThongBaoLoi = "Không kết nối được dịch vụ OCR nội bộ."
            };
        }
    }

    /// <summary>Anh xa dung snake_case ma FastAPI tra ve.</summary>
    private sealed record PhanHoiOcr
    {
        [JsonPropertyName("thanh_cong")]
        public bool ThanhCong { get; init; }

        [JsonPropertyName("van_ban")]
        public string? VanBan { get; init; }

        [JsonPropertyName("so_trang")]
        public int SoTrang { get; init; }

        [JsonPropertyName("phuong_phap")]
        public string? PhuongPhap { get; init; }
    }
}
