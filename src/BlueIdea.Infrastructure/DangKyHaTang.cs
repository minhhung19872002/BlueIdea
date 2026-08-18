using BlueIdea.Application.Chung;
using BlueIdea.Application.XacThuc;
using BlueIdea.Infrastructure.BaoMat;
using BlueIdea.Infrastructure.CongViecNen;
using BlueIdea.Infrastructure.DichVu;
using BlueIdea.Infrastructure.TichHop;
using BlueIdea.Infrastructure.XacThuc;
using BlueIdea.Infrastructure.KySo;
using BlueIdea.Application.KySo;
using BlueIdea.Application.TichHop;
using Hangfire;
using Hangfire.PostgreSql;
using BlueIdea.Infrastructure.Persistence;
using BlueIdea.Infrastructure.Seed;
using BlueIdea.Workflow.ThoiHan;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace BlueIdea.Infrastructure;

/// <summary>Dang ky toan bo dich vu ha tang (CSDL, bao mat, luu tru, thong bao).</summary>
public static class DangKyHaTang
{
    public static IServiceCollection ThemTangHaTang(
        this IServiceCollection services, IConfiguration cauHinh)
    {
        var chuoiKetNoi = cauHinh.GetConnectionString("Postgres")
                          ?? throw new InvalidOperationException(
                              "Thiếu chuỗi kết nối 'ConnectionStrings:Postgres'.");

        services.AddDbContext<AppDbContext>((sp, tuyChon) =>
        {
            tuyChon.UseNpgsql(chuoiKetNoi, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<BoChanAudit>());

            if (cauHinh.GetValue<bool>("EfCore:GhiLogChiTiet"))
            {
                tuyChon.EnableSensitiveDataLogging().EnableDetailedErrors();
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<BoChanAudit>();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        var redisCs = cauHinh.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisCs))
        {
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = redisCs;
                o.InstanceName = "blueidea:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.Configure<TuyChonJwt>(cauHinh.GetSection(TuyChonJwt.Muc));
        services.Configure<TuyChonMaHoa>(cauHinh.GetSection(TuyChonMaHoa.Muc));
        services.Configure<TuyChonLuuTru>(cauHinh.GetSection(TuyChonLuuTru.Muc));

        services.AddSingleton<IDongHoHeThong, DongHoHeThong>();
        services.AddSingleton<IDichVuMatKhau, DichVuMatKhauArgon2>();
        services.AddSingleton<IDichVuToken, DichVuTokenJwt>();
        services.AddSingleton<IDichVuMaHoa, DichVuMaHoaAes>();
        // Kho luu tru tep chon theo cau hinh. Mac dinh la dia cuc bo — may chu san xuat hien
        // tai khong chay MinIO (xem ghi chu dau deploy/docker-compose.prod.yml).
        if (string.Equals(cauHinh[$"{TuyChonLuuTru.Muc}:Loai"], "MINIO",
                StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ILuuTruTep, LuuTruTepMinio>();
        }
        else
        {
            services.AddSingleton<ILuuTruTep, LuuTruTepCucBo>();
        }

        services.AddSingleton<INguonNgayNghiLe, NguonNgayNghiLeTuCsdl>();

        services.AddScoped<INguoiDungHienTai, NguoiDungHienTai>();
        services.AddScoped<IDichVuPhanQuyen, DichVuPhanQuyen>();
        services.AddScoped<IDichVuCauHinh, DichVuCauHinh>();
        services.AddScoped<IDichVuNhatKy, DichVuNhatKy>();
        services.AddScoped<IDichVuThongBao, DichVuThongBao>();
        services.AddScoped<ISinhMaHoSo, SinhMaHoSo>();
        services.AddScoped<DuLieuMau>();

        ThemCongViecNen(services, cauHinh);
        ThemQuetVirus(services, cauHinh);

        // Lien thong: timeout ngan hon mac dinh vi he thong ngoai treo khong duoc keo dai
        // thao tac dong bo cua nguoi dung.
        services.AddHttpClient("lien-thong", http => http.Timeout = TimeSpan.FromSeconds(60))
            .AddPolicyHandler((sp, _) => ChinhSachChiuLoi(sp, "lien-thong"));
        services.AddScoped<IBoAnhXaLienThong, AnhXaThiDuaKhenThuong>();
        services.AddScoped<IBoAnhXaLienThong, AnhXaIoc>();
        services.AddScoped<IBoAnhXaLienThong, AnhXaLienThongChung>();
        services.AddScoped<IBoGuiLienThong, BoGuiLienThongHttp>();
        services.AddScoped<IBoKySo, BoKySoPkcs7>();

        // SSO: doc discovery document cua nha cung cap nen can HttpClient rieng.
        services.AddHttpClient<IBoXacThucOidc, BoXacThucOidc>(
                http => http.Timeout = TimeSpan.FromSeconds(30))
            .AddPolicyHandler((sp, _) => ChinhSachChiuLoi(sp, "sso"));

        return services;
    }

    /// <summary>
    /// Ngat mach cho moi loi goi ra HE THONG NGOAI (Muc 7 dac ta - Chiu loi).
    ///
    /// 5 lan hong lien tiep thi NGUNG goi trong 30 giay. Khong co lop nay thi mot he thong ngoai
    /// treo se lam moi yeu cau cua nguoi dung phai cho het timeout, cac luong nen tich lai, va
    /// mot dich vu ngoai keo sap ca he thong.
    ///
    /// CO Y KHONG THU LAI. Day du lieu sang he thong lien thong va gui SMS deu khong idempotent:
    /// may chu ngoai co the da nhan va xu ly xong roi moi hong luc tra loi, thu lai luc do la gui
    /// trung - mot ho so vao he thong Thi dua khen thuong hai lan, hoac mot nguoi dan nhan hai tin
    /// nhan. Rieng OCR co Hangfire tu xep lich lai o cap cong viec nen, khong can them mot lop nua.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> ChinhSachChiuLoi(
        IServiceProvider sp, string ten)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ChiuLoi." + ten);

        var ngatMach = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (_, thoiGian) => logger.LogWarning(
                    "Ngat mach '{Ten}' trong {Giay}s: he thong ngoai hong lien tiep.",
                    ten, thoiGian.TotalSeconds),
                onReset: () => logger.LogInformation("Dong mach '{Ten}': he thong ngoai da hoi.", ten));

        return ngatMach;
    }

    /// <summary>
    /// Dang ky bo quet ma doc cho tep tai len (chuc nang 25).
    ///
    /// Tat bang <c>QuetVirus:Bat=false</c> khi chay cuc bo / kiem thu tu dong de khong phai
    /// dung them container ClamAV. Khi tat, he thong bao ro la CHUA QUET chu khong bao "sach".
    /// </summary>
    private static void ThemQuetVirus(IServiceCollection services, IConfiguration cauHinh)
    {
        if (cauHinh.GetValue("QuetVirus:Bat", false))
        {
            services.AddSingleton<IDichVuQuetVirus, DichVuQuetVirusClamAv>();
        }
        else
        {
            services.AddSingleton<IDichVuQuetVirus, DichVuQuetVirusTat>();
        }
    }

    /// <summary>
    /// Dang ky cong viec nen (Hangfire) va cac dich vu phu tro.
    ///
    /// Co the TAT hoan toan bang <c>CongViecNen:BatHangfire=false</c> - khi do he thong van chay
    /// day du nghiep vu chinh, chi khong co job dinh ky. Integration test dung che do nay de
    /// khong phai dung them ha tang.
    /// </summary>
    private static void ThemCongViecNen(IServiceCollection services, IConfiguration cauHinh)
    {
        var diaChiOcr = cauHinh["CongViecNen:DiaChiDichVuOcr"] ?? "http://ai-service:8000";

        services.AddHttpClient<IDichVuOcr, DichVuOcrNoiBo>(http =>
        {
            http.BaseAddress = new Uri(diaChiOcr);

            // OCR mot tep PDF scan nhieu trang co the mat vai phut - timeout mac dinh 100s la qua ngan.
            http.Timeout = TimeSpan.FromMinutes(5);
        })
            // Dich vu AI hong thi ho so van nop duoc (Muc 7 - graceful degradation); ngat mach o
            // day de moi tep tai len khong phai cho het 5 phut moi biet dieu do.
            .AddPolicyHandler((sp, _) => ChinhSachChiuLoi(sp, "ocr"));

        services.AddHttpClient("sms", http => http.Timeout = TimeSpan.FromSeconds(30))
            .AddPolicyHandler((sp, _) => ChinhSachChiuLoi(sp, "sms"));

        services.AddScoped<IDichVuGuiTin, DichVuGuiTin>();
        services.AddScoped<CongViecTrichXuatVanBan>();
        services.AddScoped<CongViecKiemTraTrungLapNen>();
        services.AddScoped<CongViecQuetTrungLapConThieu>();
        services.AddScoped<CongViecNhacHan>();
        services.AddScoped<CongViecDongDotHetHan>();
        services.AddScoped<CongViecGuiHangDoi>();

        if (!cauHinh.GetValue("CongViecNen:BatHangfire", true))
        {
            services.AddSingleton<IHangDoiCongViecNen, HangDoiCongViecNenKhongHoatDong>();
            return;
        }

        var chuoiKetNoi = cauHinh.GetConnectionString("Postgres")!;

        services.AddHangfire(hf => hf
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(tuyChon => tuyChon.UseNpgsqlConnection(chuoiKetNoi)));

        services.AddHangfireServer(tuyChon =>
        {
            tuyChon.WorkerCount = cauHinh.GetValue("CongViecNen:SoWorker", 4);
            tuyChon.Queues = new[] { "default" };
        });

        services.AddScoped<IHangDoiCongViecNen, HangDoiCongViecNenHangfire>();
    }
}
