using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace BlueIdea.IntegrationTests.HaTang;

/// <summary>
/// Khoi tao mot PostgreSQL that bang Testcontainers va chay toan bo ung dung tren do
/// (Muc 11 dac ta). Migration va du lieu mau duoc nap nhu moi truong thuc te.
/// </summary>
public sealed class UngDungKiemThu : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Anh Docker co san pgvector + cac extension can thiet.</summary>
    private const string AnhPostgres = "pgvector/pgvector:pg16";

    /// <summary>
    /// Dùng thẳng tài khoản mặc định của ảnh PostgreSQL. Việc đổi tên tài khoản không mang lại
    /// giá trị bảo mật nào cho container tạm chỉ sống trong một lần chạy kiểm thử, mà lại
    /// dễ gặp lỗi xác thực do initdb chạy hai giai đoạn.
    /// </summary>
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage(AnhPostgres)
        .WithDatabase("blueidea_test")
        .Build();

    /// <summary>Cấu hình áp cho ứng dụng khi chạy kiểm thử.</summary>
    private Dictionary<string, string?> CauHinhKiemThu => new()
    {
        ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
        ["Jwt:KhoaKy"] = "khoa-ky-danh-rieng-cho-kiem-thu-tich-hop-32-ky-tu",
        ["MaHoa:KhoaBase64"] = "a2llbS10aHUtMzItYnl0ZS1rZXktY2hvLXRpY2hob3A=",
        ["KhoiTao:TuDongMigrate"] = "true",
        ["KhoiTao:NapDuLieuMau"] = "true",
        ["LuuTru:ThuMucGoc"] = Path.Combine(Path.GetTempPath(), "blueidea-test-files"),
        ["Serilog:MinimumLevel:Default"] = "Warning",

        // Tat bo dieu phoi Hangfire: kiem thu goi THANG vao lop cong viec de ket qua tat dinh,
        // thay vi cho scheduler chay nen roi phai poll.
        ["CongViecNen:BatHangfire"] = "false",

        // Toan bo kiem thu di chung mot IP nen se cham tran gioi han 100 req/phut cua moi truong
        // that. Nang nguong rieng cho kiem thu de khong bi 429 gia.
        ["GioiHanTruyCap:SoRequestMoiPhut"] = "100000",
        ["GioiHanTruyCap:SoLanDangNhapMoiPhut"] = "100000"
    };

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await ChoCsdlSanSangAsync();

        // Với minimal hosting, các nguồn cấu hình khai báo trong Program.cs (appsettings.json)
        // được nạp SAU nguồn do WebApplicationFactory thêm vào, nên in-memory không thắng được.
        // Biến môi trường luôn được AddEnvironmentVariables() nạp sau cùng nên chắc chắn thắng.
        foreach (var (khoa, giaTri) in CauHinhKiemThu)
        {
            Environment.SetEnvironmentVariable(khoa.Replace(":", "__"), giaTri);
        }

        // Cac extension phai co truoc khi migration chay.
        var ketQua = await _postgres.ExecScriptAsync(
            """
            CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
            CREATE EXTENSION IF NOT EXISTS pgcrypto;
            CREATE EXTENSION IF NOT EXISTS pg_trgm;
            CREATE EXTENSION IF NOT EXISTS unaccent;
            CREATE EXTENSION IF NOT EXISTS vector;
            """);

        if (ketQua.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Không tạo được extension PostgreSQL: {ketQua.Stderr}");
        }
    }

    /// <summary>
    /// Ảnh PostgreSQL khởi tạo qua hai giai đoạn: một máy chủ tạm để chạy initdb, rồi mới
    /// khởi động máy chủ thật với tài khoản đã cấu hình. Health check mặc định có thể báo sẵn
    /// sàng ngay ở giai đoạn tạm, khiến kết nối đầu tiên gặp lỗi xác thực.
    /// Vì vậy phải chờ đến khi mở được kết nối thật bằng đúng tài khoản ứng dụng.
    /// </summary>
    private async Task ChoCsdlSanSangAsync()
    {
        const int soLanThuToiDa = 30;

        for (var lan = 1; lan <= soLanThuToiDa; lan++)
        {
            try
            {
                await using var ketNoi = new Npgsql.NpgsqlConnection(_postgres.GetConnectionString());
                await ketNoi.OpenAsync();
                return;
            }
            catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException)
            {
                if (lan == soLanThuToiDa)
                {
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, cauHinh) => cauHinh.AddInMemoryCollection(CauHinhKiemThu));

        builder.ConfigureLogging(log => log.SetMinimumLevel(LogLevel.Warning));

        /*
         * TestServer chay trong bo nho nen khong co dia chi IP cua nguoi goi, trong khi mot yeu
         * cau that luon co. Gan san dia chi loopback de cac phep kiem theo IP (chan /metrics tu
         * ngoai, gioi han IP cho tai khoan quan tri) chay dung nhu tren may that.
         *
         * Test nao can gia lap dia chi khac thi gui header X-Dia-Chi-Gia-Lap — chi ban do kiem
         * thu doc header nay.
         */
        builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter, GanDiaChiGoi>());
    }

    private sealed class GanDiaChiGoi : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> tiep) => app =>
        {
            app.Use(async (ngCanh, sau) =>
            {
                var giaLap = ngCanh.Request.Headers["X-Dia-Chi-Gia-Lap"].ToString();

                ngCanh.Connection.RemoteIpAddress = System.Net.IPAddress.TryParse(giaLap, out var ip)
                    ? ip
                    : System.Net.IPAddress.Loopback;

                await sau();
            });

            tiep(app);
        };
    }

    /// <summary>Tao HttpClient da dang nhap bang tai khoan chi dinh.</summary>
    public async Task<HttpClient> TaoClientDaDangNhapAsync(string tenDangNhap)
    {
        var client = CreateClient();

        var phanHoi = await client.PostAsJsonAsync("/api/v1/xac-thuc/dang-nhap", new
        {
            tenDangNhap,
            matKhau = "Sk@2026"
        });

        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        var token = noiDung.GetProperty("duLieu").GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

/// <summary>Chia se mot container PostgreSQL cho toan bo lop kiem thu tich hop.</summary>
[CollectionDefinition(Ten)]
public sealed class BoKiemThuTichHop : ICollectionFixture<UngDungKiemThu>
{
    public const string Ten = "tich-hop";
}
