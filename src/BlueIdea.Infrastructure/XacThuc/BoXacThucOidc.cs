using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.Application.XacThuc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.XacThuc;

/// <summary>
/// Xac thuc OpenID Connect theo luong Authorization Code + PKCE (chuc nang 21, 41).
///
/// Chon Authorization Code + PKCE chu KHONG dung Implicit: access token khong bao gio di qua
/// thanh dia chi trinh duyet, va PKCE chan duoc tan cong doi ma ke ca khi authorization code
/// bi chan lai tren duong ve.
///
/// Endpoint doc tu discovery document cua nha cung cap (/.well-known/openid-configuration) nen
/// khong co dia chi cua nha cung cap cu the nao nam trong ma nguon.
/// </summary>
public sealed class BoXacThucOidc : IBoXacThucOidc
{
    private readonly HttpClient _http;
    private readonly ILogger<BoXacThucOidc> _logger;

    private readonly string? _issuer;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly string _scope;

    /// <summary>Ten claim doc tung truong — khac nhau giua cac nha cung cap nen phai cau hinh.</summary>
    private readonly string _claimTenDangNhap;
    private readonly string _claimHoTen;
    private readonly string _claimEmail;
    private readonly string _claimDienThoai;
    private readonly string _claimMaDonVi;

    private CauHinhOidc? _cauHinhNhaCungCap;

    public BoXacThucOidc(HttpClient http, IConfiguration cauHinh, ILogger<BoXacThucOidc> logger)
    {
        _http = http;
        _logger = logger;

        _issuer = cauHinh["Sso:Issuer"];
        _clientId = cauHinh["Sso:ClientId"];
        _clientSecret = cauHinh["Sso:ClientSecret"];
        _scope = cauHinh["Sso:Scope"] ?? "openid profile email";

        _claimTenDangNhap = cauHinh["Sso:Claim:TenDangNhap"] ?? "preferred_username";
        _claimHoTen = cauHinh["Sso:Claim:HoTen"] ?? "name";
        _claimEmail = cauHinh["Sso:Claim:Email"] ?? "email";
        _claimDienThoai = cauHinh["Sso:Claim:DienThoai"] ?? "phone_number";
        _claimMaDonVi = cauHinh["Sso:Claim:MaDonVi"] ?? "don_vi";
    }

    public bool DaCauHinh =>
        !string.IsNullOrWhiteSpace(_issuer) && !string.IsNullOrWhiteSpace(_clientId);

    public string TaoDiaChiDangNhap(string state, string codeChallenge, string duongDanTraVe)
    {
        var cauHinh = LayCauHinhNhaCungCapAsync(CancellationToken.None)
            .GetAwaiter().GetResult();

        var thamSo = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = _clientId,
            ["redirect_uri"] = duongDanTraVe,
            ["scope"] = _scope,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        var chuoi = string.Join('&', thamSo
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

        return $"{cauHinh.AuthorizationEndpoint}?{chuoi}";
    }

    /// <summary>Chuc nang 41 — Single logout: ket thuc ca phien ben nha cung cap SSO.</summary>
    public async Task<string?> TaoDiaChiDangXuatAsync(
        string? idToken, string duongDanTraVe, CancellationToken ct = default)
    {
        if (!DaCauHinh)
        {
            return null;
        }

        var cauHinh = await LayCauHinhNhaCungCapAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(cauHinh.EndSessionEndpoint))
        {
            return null;
        }

        var thamSo = new Dictionary<string, string?>
        {
            // id_token_hint cho nha cung cap biet ket thuc phien NAO. Thieu no thi phan lon
            // nha cung cap se hien mot trang hoi "ban co chac muon dang xuat?" thay vi dang
            // xuat thang, va mot so tu choi ca tham so post_logout_redirect_uri.
            ["id_token_hint"] = idToken,
            ["post_logout_redirect_uri"] = duongDanTraVe,
            ["client_id"] = _clientId
        };

        var chuoi = string.Join('&', thamSo
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

        return $"{cauHinh.EndSessionEndpoint}?{chuoi}";
    }

    public async Task<ThongTinNguoiDungSso> DoiMaLayThongTinAsync(
        string code, string codeVerifier, string duongDanTraVe, CancellationToken ct = default)
    {
        var cauHinh = await LayCauHinhNhaCungCapAsync(ct).ConfigureAwait(false);

        var truong = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = duongDanTraVe,
            ["client_id"] = _clientId!,
            ["code_verifier"] = codeVerifier
        };

        // Client bí mật (confidential client) mới gửi secret; client công khai thì chỉ dựa vào PKCE.
        if (!string.IsNullOrWhiteSpace(_clientSecret))
        {
            truong["client_secret"] = _clientSecret;
        }

        using var phanHoiToken = await _http
            .PostAsync(cauHinh.TokenEndpoint, new FormUrlEncodedContent(truong), ct)
            .ConfigureAwait(false);

        var noiDungToken = await phanHoiToken.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!phanHoiToken.IsSuccessStatusCode)
        {
            _logger.LogWarning("Đổi mã SSO thất bại: {NoiDung}", noiDungToken);

            throw new InvalidOperationException(
                $"Nhà cung cấp SSO từ chối đổi mã (HTTP {(int)phanHoiToken.StatusCode}).");
        }

        using var tai = JsonDocument.Parse(noiDungToken);

        if (!tai.RootElement.TryGetProperty("access_token", out var accessToken))
        {
            throw new InvalidOperationException("Nhà cung cấp SSO không trả về access_token.");
        }

        // Đọc hồ sơ người dùng từ userinfo endpoint thay vì tự giải mã id_token: userinfo luôn
        // là dữ liệu mới nhất, và tránh phải tự kiểm chữ ký JWT của bên thứ ba.
        using var yeuCauHoSo = new HttpRequestMessage(HttpMethod.Get, cauHinh.UserInfoEndpoint);
        yeuCauHoSo.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.GetString());

        using var phanHoiHoSo = await _http.SendAsync(yeuCauHoSo, ct).ConfigureAwait(false);
        phanHoiHoSo.EnsureSuccessStatusCode();

        var hoSo = await phanHoiHoSo.Content
            .ReadFromJsonAsync<Dictionary<string, JsonElement>>(cancellationToken: ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Không đọc được hồ sơ người dùng từ SSO.");

        return new ThongTinNguoiDungSso(
            Subject: DocChuoi(hoSo, "sub") ?? string.Empty,
            TenDangNhap: DocChuoi(hoSo, _claimTenDangNhap),
            HoTen: DocChuoi(hoSo, _claimHoTen),
            Email: DocChuoi(hoSo, _claimEmail),
            DienThoai: DocChuoi(hoSo, _claimDienThoai),
            MaDonVi: DocChuoi(hoSo, _claimMaDonVi));
    }

    /// <summary>
    /// Doc discovery document cua nha cung cap, cache lai vi no gan nhu khong doi.
    /// </summary>
    private async Task<CauHinhOidc> LayCauHinhNhaCungCapAsync(CancellationToken ct)
    {
        if (_cauHinhNhaCungCap is not null)
        {
            return _cauHinhNhaCungCap;
        }

        if (!DaCauHinh)
        {
            throw new InvalidOperationException("Chưa cấu hình Sso:Issuer và Sso:ClientId.");
        }

        var diaChi = $"{_issuer!.TrimEnd('/')}/.well-known/openid-configuration";

        var tai = await _http
            .GetFromJsonAsync<Dictionary<string, JsonElement>>(diaChi, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Không đọc được cấu hình OIDC của nhà cung cấp.");

        _cauHinhNhaCungCap = new CauHinhOidc(
            DocChuoi(tai, "authorization_endpoint")
                ?? throw new InvalidOperationException("Thiếu authorization_endpoint."),
            DocChuoi(tai, "token_endpoint")
                ?? throw new InvalidOperationException("Thiếu token_endpoint."),
            DocChuoi(tai, "userinfo_endpoint")
                ?? throw new InvalidOperationException("Thiếu userinfo_endpoint."),
            // Khong bat buoc: dac ta OIDC RP-Initiated Logout la mot ban rieng, nhieu nha
            // cung cap khong trien khai.
            DocChuoi(tai, "end_session_endpoint"));

        _logger.LogInformation("Đã nạp cấu hình OIDC từ {Issuer}.", _issuer);

        return _cauHinhNhaCungCap;
    }

    private static string? DocChuoi(IReadOnlyDictionary<string, JsonElement> tai, string ten)
        => tai.TryGetValue(ten, out var giaTri) && giaTri.ValueKind == JsonValueKind.String
            ? giaTri.GetString()
            : null;

    private sealed record CauHinhOidc(
        string AuthorizationEndpoint,
        string TokenEndpoint,
        string UserInfoEndpoint,
        string? EndSessionEndpoint);
}
