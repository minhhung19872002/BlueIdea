using BlueIdea.Application.Chung;
using Microsoft.Extensions.Options;

namespace BlueIdea.Infrastructure.DichVu;

public sealed class TuyChonLuuTru
{
    public const string Muc = "LuuTru";

    /// <summary>DIA_CUC_BO | MINIO</summary>
    public string Loai { get; set; } = "DIA_CUC_BO";

    public string ThuMucGoc { get; set; } = "du-lieu/tep-tin";

    public string BucketMacDinh { get; set; } = "blueidea";

    public string? MinioEndpoint { get; set; }

    public string? MinioAccessKey { get; set; }

    public string? MinioSecretKey { get; set; }

    public bool MinioSuDungSsl { get; set; }
}

/// <summary>
/// Luu tru tep tren dia cuc bo. Dung cho moi truong phat trien va cac trien khai
/// khong dung MinIO. Interface <see cref="ILuuTruTep"/> cho phep doi sang MinIO
/// ma khong sua nghiep vu (yeu cau Muc 2 dac ta).
/// </summary>
public sealed class LuuTruTepCucBo : ILuuTruTep
{
    private readonly TuyChonLuuTru _tuyChon;

    public LuuTruTepCucBo(IOptions<TuyChonLuuTru> tuyChon)
    {
        _tuyChon = tuyChon.Value;
        Directory.CreateDirectory(_tuyChon.ThuMucGoc);
    }

    public async Task<string> TaiLenAsync(
        Stream noiDung, string tenLuuTru, string? mimeType, string bucket, CancellationToken ct = default)
    {
        var thuMuc = LayThuMucBucket(bucket);
        Directory.CreateDirectory(thuMuc);

        var duongDanTuongDoi = Path.Combine(
            DateTime.UtcNow.ToString("yyyy/MM", System.Globalization.CultureInfo.InvariantCulture),
            tenLuuTru);

        var duongDanDayDu = Path.Combine(thuMuc, duongDanTuongDoi);
        Directory.CreateDirectory(Path.GetDirectoryName(duongDanDayDu)!);

        await using var tep = File.Create(duongDanDayDu);
        await noiDung.CopyToAsync(tep, ct).ConfigureAwait(false);

        _ = mimeType;
        return duongDanTuongDoi.Replace('\\', '/');
    }

    public Task<Stream> TaiXuongAsync(string bucket, string duongDan, CancellationToken ct = default)
    {
        var duongDanDayDu = LayDuongDanDayDu(bucket, duongDan);

        if (!File.Exists(duongDanDayDu))
        {
            throw new FileNotFoundException($"Không tìm thấy tệp '{duongDan}'.", duongDanDayDu);
        }

        _ = ct;
        Stream stream = File.OpenRead(duongDanDayDu);
        return Task.FromResult(stream);
    }

    public Task XoaAsync(string bucket, string duongDan, CancellationToken ct = default)
    {
        var duongDanDayDu = LayDuongDanDayDu(bucket, duongDan);
        if (File.Exists(duongDanDayDu))
        {
            File.Delete(duongDanDayDu);
        }

        _ = ct;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Voi luu tru cuc bo, URL co thoi han duoc phuc vu qua endpoint API co kiem tra quyen
    /// (khong expose duong dan he thong tep ra ngoai).
    /// </summary>
    public Task<string> TaoUrlCoThoiHanAsync(
        string bucket, string duongDan, TimeSpan thoiHan, CancellationToken ct = default)
    {
        var hetHan = DateTimeOffset.UtcNow.Add(thoiHan).ToUnixTimeSeconds();
        var url = $"/api/v1/tep-tin/tai-xuong?bucket={Uri.EscapeDataString(bucket)}"
                  + $"&duongDan={Uri.EscapeDataString(duongDan)}&hetHan={hetHan}";

        _ = ct;
        return Task.FromResult(url);
    }

    public Task<bool> TonTaiAsync(string bucket, string duongDan, CancellationToken ct = default)
    {
        _ = ct;
        return Task.FromResult(File.Exists(LayDuongDanDayDu(bucket, duongDan)));
    }

    private string LayThuMucBucket(string bucket)
        => Path.Combine(_tuyChon.ThuMucGoc, LamSachTenThuMuc(bucket));

    private string LayDuongDanDayDu(string bucket, string duongDan)
    {
        // Chan path traversal: duong dan phai nam trong thu muc bucket.
        var thuMuc = Path.GetFullPath(LayThuMucBucket(bucket));
        var dayDu = Path.GetFullPath(Path.Combine(thuMuc, duongDan));

        if (!dayDu.StartsWith(thuMuc, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Đường dẫn tệp không hợp lệ.");
        }

        return dayDu;
    }

    private static string LamSachTenThuMuc(string ten)
    {
        var kyTuCam = Path.GetInvalidFileNameChars();
        return new string(ten.Where(c => !kyTuCam.Contains(c)).ToArray());
    }
}
