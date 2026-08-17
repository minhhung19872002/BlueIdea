using System.Security.Claims;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Workflow.ThoiHan;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.Infrastructure.DichVu;

/// <summary>Nguon thoi gian he thong (UTC). Tach ra de test tat dinh.</summary>
public sealed class DongHoHeThong : IDongHoHeThong
{
    public DateTimeOffset BayGio => DateTimeOffset.UtcNow;

    /// <summary>Ngay hien tai theo mui gio Viet Nam (Asia/Ho_Chi_Minh).</summary>
    public DateOnly HomNay => DateOnly.FromDateTime(
        DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
}

/// <summary>Doc thong tin nguoi dung tu JWT claims cua request hien tai.</summary>
public sealed class NguoiDungHienTai : INguoiDungHienTai
{
    public const string ClaimVaiTro = "vai_tro";
    public const string ClaimQuyen = "quyen";
    public const string ClaimDonVi = "don_vi_id";
    public const string ClaimHoTen = "ho_ten";

    private readonly IHttpContextAccessor _accessor;

    public NguoiDungHienTai(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? NguoiDung => _accessor.HttpContext?.User;

    public Guid? Id => Guid.TryParse(
        NguoiDung?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? TenDangNhap => NguoiDung?.FindFirstValue(ClaimTypes.Name);

    public string? HoTen => NguoiDung?.FindFirstValue(ClaimHoTen);

    public Guid? DonViId => Guid.TryParse(NguoiDung?.FindFirstValue(ClaimDonVi), out var id) ? id : null;

    public IReadOnlySet<string> VaiTro => NguoiDung?.FindAll(ClaimVaiTro)
        .Select(c => c.Value).ToHashSet() ?? new HashSet<string>();

    public IReadOnlySet<string> Quyen => NguoiDung?.FindAll(ClaimQuyen)
        .Select(c => c.Value).ToHashSet() ?? new HashSet<string>();

    public string? DiaChiIp => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public bool DaXacThuc => NguoiDung?.Identity?.IsAuthenticated == true;
}

/// <summary>
/// Nguon ngay nghi le doc tu bang <c>ngay_nghi_le</c>, co cache 30 phut
/// (du lieu it thay doi nhung duoc doc rat nhieu khi tinh han xu ly).
/// </summary>
public sealed class NguonNgayNghiLeTuCsdl : INguonNgayNghiLe
{
    private const string KhoaCache = "ngay-nghi-le";

    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;

    public NguonNgayNghiLeTuCsdl(IServiceProvider serviceProvider, IMemoryCache cache)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
    }

    public IReadOnlyCollection<NgayNghiLe> LayDanhSach()
        => _cache.GetOrCreate(KhoaCache, muc =>
        {
            muc.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetService<IAppDbContext>();

            if (db is null)
            {
                return (IReadOnlyCollection<NgayNghiLe>)Array.Empty<NgayNghiLe>();
            }

            try
            {
                return db.NgayNghiLe.AsNoTracking().ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // CSDL chua san sang (vi du luc chay migration) -> khong chan luong nghiep vu.
                return Array.Empty<NgayNghiLe>();
            }
        }) ?? Array.Empty<NgayNghiLe>();
}
