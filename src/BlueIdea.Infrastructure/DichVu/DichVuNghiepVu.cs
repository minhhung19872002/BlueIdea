using System.Globalization;
using System.Text.Json;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.DichVu;

/// <summary>Doc/ghi cau hinh he thong co cache trong bo nho (5 phut).</summary>
public sealed class DichVuCauHinh : IDichVuCauHinh
{
    private const string TienToCache = "cau-hinh:";
    private static readonly TimeSpan ThoiGianCache = TimeSpan.FromMinutes(5);

    private readonly IAppDbContext _db;
    private readonly IMemoryCache _cache;

    public DichVuCauHinh(IAppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<string?> LayAsync(string khoa, CancellationToken ct = default)
    {
        if (_cache.TryGetValue<string?>(TienToCache + khoa, out var giaTriCache))
        {
            return giaTriCache;
        }

        var giaTri = await _db.CauHinhHeThong.AsNoTracking()
            .Where(x => x.Khoa == khoa)
            .Select(x => x.GiaTri)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        _cache.Set(TienToCache + khoa, giaTri, ThoiGianCache);
        return giaTri;
    }

    public async Task<T> LayAsync<T>(string khoa, T macDinh, CancellationToken ct = default)
    {
        var chuoi = await LayAsync(khoa, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(chuoi))
        {
            return macDinh;
        }

        try
        {
            var kieu = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (kieu == typeof(bool))
            {
                return (T)(object)(chuoi.Equals("true", StringComparison.OrdinalIgnoreCase) || chuoi == "1");
            }

            if (kieu.IsEnum)
            {
                return (T)Enum.Parse(kieu, chuoi, ignoreCase: true);
            }

            return (T)Convert.ChangeType(chuoi, kieu, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException
                                       or ArgumentException)
        {
            return macDinh;
        }
    }

    public async Task DatAsync(string khoa, string? giaTri, CancellationToken ct = default)
    {
        var banGhi = await _db.CauHinhHeThong
            .FirstOrDefaultAsync(x => x.Khoa == khoa, ct)
            .ConfigureAwait(false);

        if (banGhi is null)
        {
            _db.CauHinhHeThong.Add(new CauHinhHeThong
            {
                Khoa = khoa,
                GiaTri = giaTri,
                TenHienThi = khoa
            });
        }
        else
        {
            if (!banGhi.ChoPhepSua)
            {
                throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.KhongCoQuyen,
                    $"Cấu hình '{khoa}' không cho phép sửa.");
            }

            banGhi.GiaTri = giaTri;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _cache.Remove(TienToCache + khoa);
    }

    public Task XoaCacheAsync(CancellationToken ct = default)
    {
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0);
        }

        _ = ct;
        return Task.CompletedTask;
    }
}

/// <summary>Ghi nhat ky he thong (audit log) - nguyen tac 4 dac ta.</summary>
public sealed class DichVuNhatKy : IDichVuNhatKy
{
    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>Cac truong nhay cam khong duoc ghi vao log (Muc 6 dac ta).</summary>
    private static readonly string[] TruongNhayCam =
    {
        "matKhau", "matKhauCu", "matKhauMoi", "matKhauHash", "matKhauSalt",
        "token", "accessToken", "refreshToken", "clientSecret", "apiKey", "soCccd", "mfaSecret"
    };

    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;
    private readonly ILogger<DichVuNhatKy> _logger;

    public DichVuNhatKy(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo, ILogger<DichVuNhatKy> logger)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
        _logger = logger;
    }

    public async Task GhiAsync(
        string hanhDong, string module, string? doiTuong = null, Guid? doiTuongId = null,
        string? moTa = null, object? duLieuTruoc = null, object? duLieuSau = null,
        string ketQua = "THANH_CONG", CancellationToken ct = default)
    {
        try
        {
            _db.NhatKyHeThong.Add(new NhatKyHeThong
            {
                NguoiDungId = _nguoiDung.Id,
                TenDangNhap = _nguoiDung.TenDangNhap,
                HanhDong = hanhDong,
                Module = module,
                DoiTuong = doiTuong,
                DoiTuongId = doiTuongId,
                MoTa = moTa,
                DuLieuTruoc = LamSach(duLieuTruoc),
                DuLieuSau = LamSach(duLieuSau),
                DiaChiIp = _nguoiDung.DiaChiIp,
                UserAgent = _nguoiDung.UserAgent,
                KetQua = ketQua,
                ThoiGian = _dongHo.BayGio
            });

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Ghi audit that bai khong duoc lam hong nghiep vu chinh.
            _logger.LogError(ex, "Không ghi được nhật ký hệ thống cho hành động {HanhDong}", hanhDong);
        }
    }

    /// <summary>Serialize doi tuong va loai bo cac truong nhay cam.</summary>
    private static string? LamSach(object? duLieu)
    {
        if (duLieu is null)
        {
            return null;
        }

        try
        {
            var json = JsonSerializer.Serialize(duLieu, TuyChonJson);
            var node = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (node is null)
            {
                return json;
            }

            foreach (var khoa in node.Keys.ToList())
            {
                if (TruongNhayCam.Any(t => khoa.Contains(t, StringComparison.OrdinalIgnoreCase)))
                {
                    node[khoa] = JsonSerializer.SerializeToElement("***");
                }
            }

            return JsonSerializer.Serialize(node, TuyChonJson);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}

/// <summary>
/// Gui thong bao trong ung dung + day vao hang doi email/SMS theo mau cau hinh (Scriban).
/// </summary>
public sealed class DichVuThongBao : IDichVuThongBao
{
    private readonly IAppDbContext _db;
    private readonly IDongHoHeThong _dongHo;
    private readonly ILogger<DichVuThongBao> _logger;
    private readonly IBoDayRealtime? _realtime;

    public DichVuThongBao(
        IAppDbContext db, IDongHoHeThong dongHo, ILogger<DichVuThongBao> logger,
        IBoDayRealtime? realtime = null)
    {
        _db = db;
        _dongHo = dongHo;
        _logger = logger;

        // Cho phep null de kiem thu va cac tien trinh nen (khong co SignalR) van chay duoc:
        // thong bao van luu vao CSDL, chi khong day realtime.
        _realtime = realtime;
    }

    public Task GuiTheoSuKienAsync(
        string maSuKien, IEnumerable<Guid> nguoiNhanIds,
        IDictionary<string, object?> bien, CancellationToken ct = default)
        => GuiTheoSuKienCoreAsync(maSuKien, nguoiNhanIds, bien, null, ct);

    public Task GuiTheoSuKienAsync(
        string maSuKien, IEnumerable<Guid> nguoiNhanIds,
        IDictionary<string, object?> bien, IReadOnlyCollection<string> kenhChoPhep,
        CancellationToken ct = default)
        => GuiTheoSuKienCoreAsync(maSuKien, nguoiNhanIds, bien, kenhChoPhep, ct);

    private async Task GuiTheoSuKienCoreAsync(
        string maSuKien, IEnumerable<Guid> nguoiNhanIds,
        IDictionary<string, object?> bien, IReadOnlyCollection<string>? kenhChoPhep,
        CancellationToken ct)
    {
        var danhSachNhan = nguoiNhanIds.Distinct().ToList();
        if (danhSachNhan.Count == 0)
        {
            return;
        }

        var mau = await _db.MauThongBao.AsNoTracking()
            .Where(x => x.SuKien == maSuKien && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (mau.Count == 0)
        {
            if (kenhChoPhep is null || kenhChoPhep.Contains("APP"))
            {
                foreach (var id in danhSachNhan)
                {
                    await GuiTrongUngDungAsync(id, maSuKien.Replace('_', ' '),
                        string.Join(", ", bien.Select(b => $"{b.Key}: {b.Value}")), ct: ct)
                        .ConfigureAwait(false);
                }
            }

            return;
        }

        var nguoiNhan = await _db.NguoiDung.AsNoTracking()
            .Where(x => danhSachNhan.Contains(x.Id))
            .Select(x => new { x.Id, x.HoTen, x.Email, x.DienThoai })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var m in mau)
        {
            foreach (var nd in nguoiNhan)
            {
                var duLieu = new Dictionary<string, object?>(bien)
                {
                    ["hoTen"] = nd.HoTen,
                    ["email"] = nd.Email
                };

                var tieuDe = KetXuat(m.TieuDe, duLieu);
                var noiDung = KetXuat(m.NoiDung, duLieu);

                if (m.Kenh is "APP" or "TAT_CA"
                    && (kenhChoPhep is null || kenhChoPhep.Contains("APP")))
                {
                    _db.ThongBao.Add(new ThongBao
                    {
                        NguoiNhanId = nd.Id,
                        TieuDe = tieuDe,
                        NoiDung = noiDung,
                        LoaiSuKien = maSuKien,
                        DoiTuongId = bien.TryGetValue("sangKienId", out var skId) && skId is Guid g ? g : null,
                        DuongDan = bien.TryGetValue("duongDan", out var dd) ? dd?.ToString() : null,
                        ThoiGian = _dongHo.BayGio
                    });
                }

                if (m.Kenh is "EMAIL" or "TAT_CA" && !string.IsNullOrWhiteSpace(nd.Email)
                    && (kenhChoPhep is null || kenhChoPhep.Contains("EMAIL")))
                {
                    _db.HangDoiGuiTin.Add(new HangDoiGuiTin
                    {
                        Kenh = "EMAIL",
                        NguoiNhan = nd.Email,
                        TieuDe = tieuDe,
                        NoiDung = noiDung
                    });
                }

                if (m.Kenh is "SMS" or "TAT_CA" && !string.IsNullOrWhiteSpace(nd.DienThoai)
                    && (kenhChoPhep is null || kenhChoPhep.Contains("SMS")))
                {
                    _db.HangDoiGuiTin.Add(new HangDoiGuiTin
                    {
                        Kenh = "SMS",
                        NguoiNhan = nd.DienThoai,
                        NoiDung = noiDung
                    });
                }
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task GuiTrongUngDungAsync(
        Guid nguoiNhanId, string tieuDe, string noiDung,
        string? duongDan = null, string? mucDo = null, CancellationToken ct = default)
    {
        _db.ThongBao.Add(new ThongBao
        {
            NguoiNhanId = nguoiNhanId,
            TieuDe = tieuDe,
            NoiDung = noiDung,
            DuongDan = duongDan,
            MucDo = mucDo ?? "BINH_THUONG",
            ThoiGian = _dongHo.BayGio
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Day realtime SAU khi luu thanh cong: day truoc ma luu that bai thi chuong bao co
        // thong bao moi trong khi khong co gi trong danh sach.
        if (_realtime is not null)
        {
            try
            {
                await _realtime.ThongBaoMoiAsync(nguoiNhanId, tieuDe, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Mat ket noi realtime khong duoc lam hong nghiep vu — thong bao da nam trong
                // CSDL, chuong se thay o lan hoi tiep theo.
                _logger.LogWarning(ex, "Không đẩy được thông báo realtime tới {NguoiNhan}.",
                    nguoiNhanId);
            }
        }
    }

    /// <summary>
    /// Ket xuat mau bang bo thay the placeholder an toan (khong thuc thi ma trong mau).
    /// Neu co loi thi tra ve nguyen van de khong chan nghiep vu chinh.
    /// </summary>
    private string KetXuat(string mau, IDictionary<string, object?> bien)
    {
        try
        {
            return BoKetXuatMau.KetXuat(mau, bien);
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Mẫu thông báo quá phức tạp, dùng nguyên văn.");
            return mau;
        }
    }
}
