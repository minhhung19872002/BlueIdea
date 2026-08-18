using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using BlueIdea.Application.Chung;
using BlueIdea.Application.TichHop;
using BlueIdea.Domain.QuanTri;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.TichHop;

/// <summary>
/// Day du lieu sang he thong ngoai (Thi dua khen thuong, IOC) bang REST.
///
/// Ho tro ba kieu xac thuc thuong gap o cac he thong dung chung cua tinh/thanh:
///   API_KEY  - dat khoa vao header Authorization
///   HMAC     - ky than yeu cau bang SHA-256, dat chu ky vao header (khong gui khoa qua mang)
///   OAUTH2   - lay access token theo luong client_credentials roi dat vao Authorization
///
/// Endpoint, khoa va anh xa truong deu doc tu bang <c>he_thong_tich_hop</c>, khong co gia tri
/// nao cua he thong cu the nam trong ma nguon.
/// </summary>
public sealed class BoGuiLienThongHttp : IBoGuiLienThong
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IDichVuMaHoa _maHoa;
    private readonly IReadOnlyList<IBoAnhXaLienThong> _boAnhXa;
    private readonly ILogger<BoGuiLienThongHttp> _logger;

    public BoGuiLienThongHttp(
        IHttpClientFactory httpFactory, IDichVuMaHoa maHoa,
        IEnumerable<IBoAnhXaLienThong> boAnhXa, ILogger<BoGuiLienThongHttp> logger)
    {
        _httpFactory = httpFactory;
        _maHoa = maHoa;
        _boAnhXa = boAnhXa?.ToList() ?? new List<IBoAnhXaLienThong>();
        _logger = logger;
    }

    public async Task<(bool ThanhCong, int SoNhan, string PhanHoi)> GuiAsync(
        HeThongTichHop heThong, IReadOnlyList<BanGhiDongBo> duLieu, CancellationToken ct = default)
    {
        var http = _httpFactory.CreateClient("lien-thong");

        // Moi he thong dich co hop dong du lieu rieng; khong co bo anh xa rieng thi dung bo
        // chung — them mot he thong moi chi can khai bao trong bang, khong phai sua ma nguon.
        var boAnhXa = _boAnhXa.FirstOrDefault(x =>
                          string.Equals(x.MaHeThong, heThong.Ma, StringComparison.OrdinalIgnoreCase))
                      ?? _boAnhXa.FirstOrDefault(x => x.MaHeThong == "*")
                      ?? new AnhXaLienThongChung();

        var than = boAnhXa.TaoThan(heThong, duLieu);

        var json = System.Text.Json.JsonSerializer.Serialize(than);

        using var yeuCau = new HttpRequestMessage(HttpMethod.Post, heThong.EndpointBase)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        await GanXacThucAsync(yeuCau, json, heThong, http, ct).ConfigureAwait(false);

        using var phanHoi = await http.SendAsync(yeuCau, ct).ConfigureAwait(false);
        var noiDung = await phanHoi.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!phanHoi.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Hệ thống {Ten} trả về {MaTrangThai}: {NoiDung}",
                heThong.Ten, (int)phanHoi.StatusCode, noiDung);

            return (false, 0, $"HTTP {(int)phanHoi.StatusCode}: {noiDung}");
        }

        // He thong ngoai co the tra ve so ban ghi da nhan. Khong co thi coi nhu nhan het.
        var soNhan = DocSoNhan(noiDung) ?? duLieu.Count;

        return (true, soNhan, noiDung);
    }

    private async Task GanXacThucAsync(
        HttpRequestMessage yeuCau, string than, HeThongTichHop heThong, HttpClient http,
        CancellationToken ct)
    {
        var biMat = _maHoa.GiaiMa(heThong.ClientSecretMaHoa);

        switch (heThong.LoaiXacThuc)
        {
            case "HMAC":
                if (!string.IsNullOrWhiteSpace(biMat))
                {
                    // Ky than yeu cau: khoa bi mat KHONG bao gio di qua mang.
                    using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(biMat)))
                    {
                        var chuKy = Convert.ToHexString(
                            hmac.ComputeHash(Encoding.UTF8.GetBytes(than))).ToLowerInvariant();

                        yeuCau.Headers.TryAddWithoutValidation("X-Client-Id", heThong.ClientId ?? string.Empty);
                        yeuCau.Headers.TryAddWithoutValidation("X-Signature", chuKy);
                    }
                }

                break;

            case "OAUTH2":
                var token = await LayTokenAsync(heThong, biMat, http, ct).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(token))
                {
                    yeuCau.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                break;

            default:
                if (!string.IsNullOrWhiteSpace(biMat))
                {
                    yeuCau.Headers.Authorization = new AuthenticationHeaderValue("Bearer", biMat);
                }

                break;
        }
    }

    /// <summary>Lay access token theo luong client_credentials.</summary>
    private async Task<string?> LayTokenAsync(
        HeThongTichHop heThong, string? biMat, HttpClient http, CancellationToken ct)
    {
        var diaChiToken = heThong.CauHinhMapping?.GetValueOrDefault("__tokenUrl");

        if (string.IsNullOrWhiteSpace(diaChiToken))
        {
            _logger.LogWarning(
                "Hệ thống {Ten} dùng OAUTH2 nhưng chưa cấu hình __tokenUrl trong ánh xạ.", heThong.Ten);

            return null;
        }

        using var yeuCau = new HttpRequestMessage(HttpMethod.Post, diaChiToken)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = heThong.ClientId ?? string.Empty,
                ["client_secret"] = biMat ?? string.Empty,
                ["scope"] = heThong.Scope ?? string.Empty
            })
        };

        using var phanHoi = await http.SendAsync(yeuCau, ct).ConfigureAwait(false);

        if (!phanHoi.IsSuccessStatusCode)
        {
            _logger.LogWarning("Không lấy được token OAuth2 cho {Ten}.", heThong.Ten);
            return null;
        }

        var noiDung = await phanHoi.Content
            .ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>(cancellationToken: ct)
            .ConfigureAwait(false);

        return noiDung is not null && noiDung.TryGetValue("access_token", out var t)
            ? t.GetString()
            : null;
    }

    /// <summary>Doc so ban ghi he thong ngoai xac nhan da nhan, neu ho co tra ve.</summary>
    private static int? DocSoNhan(string noiDung)
    {
        if (string.IsNullOrWhiteSpace(noiDung))
        {
            return null;
        }

        try
        {
            using var tai = System.Text.Json.JsonDocument.Parse(noiDung);

            foreach (var ten in new[] { "soNhan", "soBanGhi", "received", "count" })
            {
                if (tai.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                    && tai.RootElement.TryGetProperty(ten, out var giaTri)
                    && giaTri.TryGetInt32(out var so))
                {
                    return so;
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // He thong ngoai tra ve van ban thuan - khong phai loi, chi la khong doc duoc so.
        }

        return null;
    }
}
