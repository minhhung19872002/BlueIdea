using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlueIdea.Application.XacThuc;
using BlueIdea.Infrastructure.XacThuc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueIdea.UnitTests.XacThuc;

/// <summary>
/// Kiem thu lop noi chuyen OIDC.
///
/// Dung mot nha cung cap OIDC gia (HttpListener that) de kiem duoc CA ba buoc: doc discovery
/// document, dung dia chi dang nhap dung chuan PKCE, va doi ma lay ho so nguoi dung.
/// </summary>
public sealed class BoXacThucOidcTests
{
    private const string DuongDanTraVe = "https://sangkien.local/dang-nhap/sso";

    [Fact]
    public void Chua_Cau_Hinh_Thi_Bao_Chua_Cau_Hinh()
    {
        var bo = new BoXacThucOidc(
            new HttpClient(),
            new ConfigurationBuilder().Build(),
            NullLogger<BoXacThucOidc>.Instance);

        bo.DaCauHinh.Should().BeFalse();
    }

    [Fact]
    public async Task Dia_Chi_Dang_Nhap_Dung_Chuan_Authorization_Code_Va_PKCE()
    {
        await using var gia = NhaCungCapGia.Tao();
        var bo = TaoBo(gia.Issuer);

        var diaChi = bo.TaoDiaChiDangNhap("state-abc", "challenge-xyz", DuongDanTraVe);

        var uri = new Uri(diaChi);
        var thamSo = System.Web.HttpUtility.ParseQueryString(uri.Query);

        uri.GetLeftPart(UriPartial.Path).Should().Be($"{gia.Issuer}/auth");

        thamSo["response_type"].Should().Be("code", "phải dùng Authorization Code, không dùng Implicit");
        thamSo["client_id"].Should().Be("blueidea");
        thamSo["redirect_uri"].Should().Be(DuongDanTraVe);
        thamSo["state"].Should().Be("state-abc");
        thamSo["code_challenge"].Should().Be("challenge-xyz");
        thamSo["code_challenge_method"].Should().Be("S256", "PKCE phải dùng S256, không dùng plain");
    }

    [Fact]
    public async Task Doi_Ma_Doc_Duoc_Ho_So_Nguoi_Dung()
    {
        await using var gia = NhaCungCapGia.Tao();
        var bo = TaoBo(gia.Issuer);

        var tt = await bo.DoiMaLayThongTinAsync("ma-uy-quyen", "verifier-abc", DuongDanTraVe);

        tt.Subject.Should().Be("sso-subject-001");
        tt.TenDangNhap.Should().Be("nguyen.van.a");
        tt.HoTen.Should().Be("Nguyễn Văn A");
        tt.Email.Should().Be("a.nguyen@tranbien.gov.vn");
        tt.MaDonVi.Should().Be("PHONG_GDDT");

        // code_verifier PHẢI được gửi lên: thiếu nó thì PKCE vô tác dụng.
        gia.ThanTokenCuoi.Should().Contain("code_verifier=verifier-abc");
        gia.ThanTokenCuoi.Should().Contain("grant_type=authorization_code");
    }

    [Fact]
    public async Task Nha_Cung_Cap_Tu_Choi_Thi_Nem_Loi_Ro_Rang()
    {
        await using var gia = NhaCungCapGia.Tao(loiToken: true);
        var bo = TaoBo(gia.Issuer);

        var hanhDong = async () =>
            await bo.DoiMaLayThongTinAsync("ma-sai", "verifier", DuongDanTraVe);

        await hanhDong.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*từ chối đổi mã*");
    }

    [Fact]
    public void PKCE_Challenge_Tinh_Dung_Cong_Thuc_RFC_7636()
    {
        // Giá trị mẫu trong RFC 7636 phụ lục B.
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        const string mongDoi = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        var bam = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Convert.ToBase64String(bam)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        challenge.Should().Be(mongDoi);
    }

    // ------------------------------------------------------------------------------------

    private static BoXacThucOidc TaoBo(string issuer)
    {
        var cauHinh = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sso:Issuer"] = issuer,
                ["Sso:ClientId"] = "blueidea",
                ["Sso:ClientSecret"] = "bi-mat",
                ["Sso:Claim:MaDonVi"] = "don_vi"
            })
            .Build();

        return new BoXacThucOidc(new HttpClient(), cauHinh, NullLogger<BoXacThucOidc>.Instance);
    }

    /// <summary>Nhà cung cấp OIDC giả: phục vụ discovery, token và userinfo.</summary>
    private sealed class NhaCungCapGia : IAsyncDisposable
    {
        private readonly HttpListener _lang;
        private readonly CancellationTokenSource _huy = new();
        private readonly Task _phucVu;
        private readonly bool _loiToken;

        private NhaCungCapGia(HttpListener lang, string issuer, bool loiToken)
        {
            _lang = lang;
            Issuer = issuer;
            _loiToken = loiToken;
            _phucVu = PhucVuAsync();
        }

        public string Issuer { get; }

        public string? ThanTokenCuoi { get; private set; }

        public static NhaCungCapGia Tao(bool loiToken = false)
        {
            var nghe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            nghe.Start();
            var cong = ((IPEndPoint)nghe.LocalEndpoint).Port;
            nghe.Stop();

            var lang = new HttpListener();
            lang.Prefixes.Add($"http://127.0.0.1:{cong}/");
            lang.Start();

            return new NhaCungCapGia(lang, $"http://127.0.0.1:{cong}", loiToken);
        }

        private async Task PhucVuAsync()
        {
            while (!_huy.IsCancellationRequested)
            {
                HttpListenerContext nguCanh;

                try
                {
                    nguCanh = await _lang.GetContextAsync();
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
                {
                    return;
                }

                var duongDan = nguCanh.Request.Url?.AbsolutePath ?? string.Empty;
                string traLoi;
                var ma = HttpStatusCode.OK;

                if (duongDan.EndsWith("/.well-known/openid-configuration", StringComparison.Ordinal))
                {
                    traLoi = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["issuer"] = Issuer,
                        ["authorization_endpoint"] = $"{Issuer}/auth",
                        ["token_endpoint"] = $"{Issuer}/token",
                        ["userinfo_endpoint"] = $"{Issuer}/userinfo"
                    });
                }
                else if (duongDan.EndsWith("/token", StringComparison.Ordinal))
                {
                    using (var doc = new StreamReader(nguCanh.Request.InputStream, Encoding.UTF8))
                    {
                        ThanTokenCuoi = await doc.ReadToEndAsync();
                    }

                    if (_loiToken)
                    {
                        ma = HttpStatusCode.BadRequest;
                        traLoi = """{"error":"invalid_grant"}""";
                    }
                    else
                    {
                        traLoi = """{"access_token":"token-gia","token_type":"Bearer","expires_in":300}""";
                    }
                }
                else if (duongDan.EndsWith("/userinfo", StringComparison.Ordinal))
                {
                    traLoi = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["sub"] = "sso-subject-001",
                        ["preferred_username"] = "nguyen.van.a",
                        ["name"] = "Nguyễn Văn A",
                        ["email"] = "a.nguyen@tranbien.gov.vn",
                        ["don_vi"] = "PHONG_GDDT"
                    });
                }
                else
                {
                    ma = HttpStatusCode.NotFound;
                    traLoi = "{}";
                }

                var byteTraLoi = Encoding.UTF8.GetBytes(traLoi);
                nguCanh.Response.StatusCode = (int)ma;
                nguCanh.Response.ContentType = "application/json";
                await nguCanh.Response.OutputStream.WriteAsync(byteTraLoi);
                nguCanh.Response.Close();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _huy.CancelAsync();
            _lang.Stop();
            _lang.Close();
            _huy.Dispose();
        }
    }
}
