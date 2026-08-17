using BlueIdea.Application.Chung;
using BlueIdea.Application.XacThuc;
using BlueIdea.Infrastructure.DichVu;
using BlueIdea.Infrastructure.Persistence;
using BlueIdea.Infrastructure.Seed;
using BlueIdea.Workflow.ThoiHan;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.Configure<TuyChonJwt>(cauHinh.GetSection(TuyChonJwt.Muc));
        services.Configure<TuyChonMaHoa>(cauHinh.GetSection(TuyChonMaHoa.Muc));
        services.Configure<TuyChonLuuTru>(cauHinh.GetSection(TuyChonLuuTru.Muc));

        services.AddSingleton<IDongHoHeThong, DongHoHeThong>();
        services.AddSingleton<IDichVuMatKhau, DichVuMatKhauArgon2>();
        services.AddSingleton<IDichVuToken, DichVuTokenJwt>();
        services.AddSingleton<IDichVuMaHoa, DichVuMaHoaAes>();
        services.AddSingleton<ILuuTruTep, LuuTruTepCucBo>();
        services.AddSingleton<INguonNgayNghiLe, NguonNgayNghiLeTuCsdl>();

        services.AddScoped<INguoiDungHienTai, NguoiDungHienTai>();
        services.AddScoped<IDichVuPhanQuyen, DichVuPhanQuyen>();
        services.AddScoped<IDichVuCauHinh, DichVuCauHinh>();
        services.AddScoped<IDichVuNhatKy, DichVuNhatKy>();
        services.AddScoped<IDichVuThongBao, DichVuThongBao>();
        services.AddScoped<ISinhMaHoSo, SinhMaHoSo>();
        services.AddScoped<DuLieuMau>();

        return services;
    }
}
