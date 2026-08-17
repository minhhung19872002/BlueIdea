using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueIdea.Api.Chung;
using BlueIdea.Api.Hubs;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Infrastructure;
using BlueIdea.Infrastructure.CongViecNen;
using BlueIdea.Infrastructure.DichVu;
using Hangfire;
using Hangfire.Dashboard;
using BlueIdea.Infrastructure.Persistence;
using BlueIdea.Infrastructure.Seed;
using BlueIdea.Reporting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------
// Ghi log (Serilog): Console + tep xoay vong hang ngay.
// ---------------------------------------------------------------------------------------
builder.Host.UseSerilog((nguCanh, cauHinh) => cauHinh
    .ReadFrom.Configuration(nguCanh.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine("logs", "blueidea-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true));

BoXuatPdf.CauHinhGiayPhep();

// ---------------------------------------------------------------------------------------
// Dich vu
// ---------------------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Nền tảng số dùng chung phục vụ hoạt động sáng kiến",
        Version = "v1",
        Description = "Toàn bộ nghiệp vụ quản lý sáng kiến: đăng ký, tiếp nhận, thẩm định, "
                      + "hội đồng chấm điểm, công nhận, thống kê báo cáo."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập access token nhận được từ POST /api/v1/xac-thuc/dang-nhap"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var duongDanXml = Path.Combine(AppContext.BaseDirectory, "BlueIdea.Api.xml");
    if (File.Exists(duongDanXml))
    {
        c.IncludeXmlComments(duongDanXml);
    }
});

builder.Services.ThemTangUngDung();
builder.Services.ThemTangHaTang(builder.Configuration);

builder.Services.AddSignalR();

// Xac thuc JWT.
var khoaKy = builder.Configuration["Jwt:KhoaKy"] ?? string.Empty;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(khoaKy.Length >= 32 ? khoaKy : new string('k', 32))),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // SignalR truyen token qua query string.
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ngCanh =>
            {
                var token = ngCanh.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token)
                    && ngCanh.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ngCanh.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(o =>
{
    // Chinh sach theo tung ma quyen - dung trong [Authorize(Policy = MaQuyen.***)].
    foreach (var maQuyen in LayTatCaMaQuyen())
    {
        o.AddPolicy(maQuyen, chinhSach => chinhSach.RequireAssertion(ngCanh =>
            ngCanh.User.HasClaim(NguoiDungHienTai.ClaimQuyen, maQuyen)
            || ngCanh.User.HasClaim(NguoiDungHienTai.ClaimVaiTro, MaVaiTro.QuanTriHeThong)));
    }
});

builder.Services.AddCors(o => o.AddPolicy("MacDinh", chinhSach =>
{
    var nguonChoPhep = builder.Configuration
        .GetSection("Cors:NguonChoPhep").Get<string[]>() ?? new[] { "http://localhost:5173" };

    chinhSach.WithOrigins(nguonChoPhep)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("csdl", tags: new[] { "ready" });

// Nguong doc tu cau hinh de van hanh dieu chinh duoc theo tai thuc te, va de kiem thu tu dong
// (hang tram request tu cung mot IP) khong bi chan. Mac dinh dung dung Muc 6 dac ta.
var gioiHanChung = builder.Configuration.GetValue("GioiHanTruyCap:SoRequestMoiPhut", 100);
var gioiHanDangNhap = builder.Configuration.GetValue("GioiHanTruyCap:SoLanDangNhapMoiPhut", 5);

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    o.AddPolicy("MacDinh", ngCanh =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            ngCanh.Connection.RemoteIpAddress?.ToString() ?? "khong-xac-dinh",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = gioiHanChung,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    o.AddPolicy("DangNhap", ngCanh =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            ngCanh.Connection.RemoteIpAddress?.ToString() ?? "khong-xac-dinh",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = gioiHanDangNhap,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

// ---------------------------------------------------------------------------------------
// Chay sau proxy nguoc (Nginx cua may chu dam nhan TLS).
//
// Khong co buoc nay thi Connection.RemoteIpAddress luon la IP cua proxy, keo theo hai hau qua:
//   - Rate limiter phan vung theo IP se gop TAT CA nguoi dung vao chung mot xo. Ca he thong
//     dung o 100 request/phut, va gioi han 5 lan dang nhap/phut tro thanh gioi han toan cuc:
//     mot nguoi go sai mat khau 5 lan la nhung nguoi con lai het dang nhap duoc.
//   - Log kiem toan ghi IP cua proxy nen mat dau vet nguoi dung that.
//
// Chi bat khi ForwardedHeaders:BatTin = true, tuc chi o moi truong that su co proxy dat truoc.
// Bat vo dieu kien la mot lo hong: khi API mo truc tiep ra Internet, bat ky ai cung co the
// tu dat X-Forwarded-For de mao danh IP khac va vuot rate limit.
// ---------------------------------------------------------------------------------------
var tinProxy = builder.Configuration.GetValue("ForwardedHeaders:BatTin", false);
if (tinProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // Mac dinh middleware chi tin loopback, nen trong Docker (proxy den tu gateway
        // 172.x.x.x cua mang bridge) header se bi bo qua ma khong bao loi gi.
        // Dia chi gateway doi moi lan tao lai mang nen khong the ghi cung; thay vao do
        // xoa danh sach tin cay va gioi han so tang proxy dung 1 - chinh la Nginx.
        o.KnownNetworks.Clear();
        o.KnownProxies.Clear();
        o.ForwardLimit = 1;
    });
}

var app = builder.Build();

// ---------------------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------------------
// Phai dung dau tien: moi middleware phia sau (rate limiter, log, HSTS, sinh URL tuyet doi)
// deu doc IP va scheme, nen neu dat sau chung thi cac gia tri do da bi chot sai.
if (tinProxy)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<MiddlewareXuLyLoi>();
app.UseMiddleware<MiddlewareHeaderBaoMat>();
app.UseResponseCompression();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:BatTrenProduction"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlueIdea API v1");
        c.DocumentTitle = "Tài liệu API - Phần mềm Sáng kiến";
        c.DisplayRequestDuration();
    });
}
else
{
    app.UseHsts();
}

app.UseCors("MacDinh");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("MacDinh");
app.MapHub<ThongBaoHub>("/hubs/thong-bao");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

// ---------------------------------------------------------------------------------------
// Cong viec nen (Hangfire)
// ---------------------------------------------------------------------------------------
if (app.Configuration.GetValue("CongViecNen:BatHangfire", true))
{
    // Dashboard chi danh cho quan tri vien - filter tu viet doc quyen tu JWT.
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new ChiQuanTriVaoHangfire() },
        DashboardTitle = "BlueIdea — Công việc nền",
        IsReadOnlyFunc = _ => false
    });

    DangKyCongViecDinhKy(app);
}

// ---------------------------------------------------------------------------------------
// Migration + seed du lieu mau khi khoi dong
// ---------------------------------------------------------------------------------------
if (app.Configuration.GetValue("KhoiTao:TuDongMigrate", true))
{
    await KhoiTaoCoSoDuLieuAsync(app);
}

await app.RunAsync();

/// <summary>
/// Chạy migration và nạp dữ liệu mẫu, có thử lại với thời gian chờ tăng dần.
///
/// Khi chạy bằng docker-compose, cơ sở dữ liệu có thể đã "healthy" nhưng DNS nội bộ của
/// container hoặc mạng overlay chưa sẵn sàng, khiến lần kết nối đầu tiên thất bại tạm thời.
/// Nếu thoát ngay, container rơi vào vòng lặp khởi động lại. Vì vậy phải thử lại vài lần
/// trước khi kết luận là lỗi thật.
/// </summary>
static async Task KhoiTaoCoSoDuLieuAsync(WebApplication app)
{
    const int soLanThuToiDa = 10;

    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    for (var lan = 1; lan <= soLanThuToiDa; lan++)
    {
        using var pham = app.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Đã áp dụng migration cơ sở dữ liệu.");

            if (app.Configuration.GetValue("KhoiTao:NapDuLieuMau", true))
            {
                var seed = pham.ServiceProvider.GetRequiredService<DuLieuMau>();
                await seed.ChayAsync();
            }

            return;
        }
        catch (Exception ex) when (LaLoiKetNoiTamThoi(ex) && lan < soLanThuToiDa)
        {
            var choGiay = Math.Min(lan * 2, 15);

            logger.LogWarning(
                "Chưa kết nối được cơ sở dữ liệu (lần {Lan}/{Tong}): {ThongBao}. Thử lại sau {Giay}s.",
                lan, soLanThuToiDa, ex.Message, choGiay);

            await Task.Delay(TimeSpan.FromSeconds(choGiay));
        }
        catch (Exception ex)
        {
            // Lỗi không phải do kết nối (ví dụ migration sai) thì phải dừng hẳn,
            // tuyệt đối không để ứng dụng chạy trên schema không đúng.
            logger.LogError(ex, "Không thể khởi tạo cơ sở dữ liệu.");
            throw;
        }
    }
}

/// <summary>Nhận diện lỗi mạng/kết nối tạm thời để quyết định có thử lại hay không.</summary>
static bool LaLoiKetNoiTamThoi(Exception ex)
{
    for (var hienTai = ex; hienTai is not null; hienTai = hienTai.InnerException!)
    {
        if (hienTai is System.Net.Sockets.SocketException
            or TimeoutException
            or Npgsql.NpgsqlException { IsTransient: true })
        {
            return true;
        }

        if (hienTai is Npgsql.PostgresException pg)
        {
            // 57P03 the_database_system_is_starting_up
            return pg.SqlState == "57P03";
        }
    }

    return false;
}

/// <summary>
/// Dang ky cac cong viec chay dinh ky. Bieu thuc cron doc tu cau hinh de van hanh doi duoc
/// tan suat ma khong phai build lai (Muc 13 dac ta).
/// </summary>
static void DangKyCongViecDinhKy(WebApplication app)
{
    var muiGio = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
    var quanLy = app.Services.GetRequiredService<IRecurringJobManager>();

    string Lich(string khoa, string macDinh)
        => app.Configuration[$"CongViecNen:Lich:{khoa}"] ?? macDinh;

    // Nhac han xu ly / han cham diem - 7h sang moi ngay.
    quanLy.AddOrUpdate<CongViecNhacHan>(
        "nhac-han-xu-ly",
        x => x.ChayAsync(CancellationToken.None),
        Lich("NhacHan", "0 7 * * *"),
        new RecurringJobOptions { TimeZone = muiGio });

    // Dong dot de nghi qua han - moi gio.
    quanLy.AddOrUpdate<CongViecDongDotHetHan>(
        "dong-dot-het-han",
        x => x.ChayAsync(CancellationToken.None),
        Lich("DongDot", "0 * * * *"),
        new RecurringJobOptions { TimeZone = muiGio });

    // Rut hang doi email/SMS - moi 5 phut.
    quanLy.AddOrUpdate<CongViecGuiHangDoi>(
        "gui-hang-doi",
        x => x.ChayAsync(CancellationToken.None),
        Lich("GuiHangDoi", "*/5 * * * *"),
        new RecurringJobOptions { TimeZone = muiGio });

    // Quet bu kiem tra trung lap - moi 15 phut.
    quanLy.AddOrUpdate<CongViecQuetTrungLapConThieu>(
        "quet-trung-lap-con-thieu",
        x => x.ChayAsync(CancellationToken.None),
        Lich("QuetTrungLap", "*/15 * * * *"),
        new RecurringJobOptions { TimeZone = muiGio });
}

static IEnumerable<string> LayTatCaMaQuyen()
    => typeof(MaQuyen)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .Distinct();

/// <summary>
/// Chi cho phep vai tro quan tri he thong mo dashboard Hangfire.
///
/// Dashboard hien noi dung tham so cua job (co the chua Id ho so, dia chi email) nen phai
/// dong quyen chat, khong duoc de mo nhu trang tinh.
/// </summary>
public sealed class ChiQuanTriVaoHangfire : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var nguoiDung = context.GetHttpContext().User;

        return nguoiDung.Identity?.IsAuthenticated == true
               && nguoiDung.HasClaim(NguoiDungHienTai.ClaimVaiTro, MaVaiTro.QuanTriHeThong);
    }
}

/// <summary>Lop moc de WebApplicationFactory trong integration test tham chieu duoc.</summary>
public partial class Program
{
}
