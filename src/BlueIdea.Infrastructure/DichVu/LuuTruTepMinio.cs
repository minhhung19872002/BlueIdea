using BlueIdea.Application.Chung;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace BlueIdea.Infrastructure.DichVu;

/// <summary>
/// Luu tru tep tren MinIO (S3-compatible), dung khi <c>LuuTru:Loai = MINIO</c>.
///
/// Ban trien khai mac dinh van la <see cref="LuuTruTepCucBo"/>: may chu san xuat hien tai chi co
/// 1 vCPU / 2GB RAM nen khong chay them MinIO (xem ghi chu dau deploy/docker-compose.prod.yml).
/// Lop nay danh cho cac trien khai co kho doi tuong rieng, nhat la khi chay nhieu ban API — luc
/// do tep khong con nam tren dia cua mot may cu the duoc nua.
///
/// URL tai xuong dung PRESIGNED URL cua chinh MinIO: trinh duyet tai thang tu kho doi tuong,
/// khong bat luong tep di xuyen qua tien trinh API.
/// </summary>
public sealed class LuuTruTepMinio : ILuuTruTep
{
    /// <summary>MinIO khong ky duoc URL song qua 7 ngay — gioi han cua giao thuc S3.</summary>
    private static readonly TimeSpan ThoiHanToiDa = TimeSpan.FromDays(7);

    private readonly IMinioClient _minio;
    private readonly ILogger<LuuTruTepMinio> _log;

    public LuuTruTepMinio(IOptions<TuyChonLuuTru> tuyChon, ILogger<LuuTruTepMinio> log)
    {
        ArgumentNullException.ThrowIfNull(tuyChon);
        var t = tuyChon.Value;

        if (string.IsNullOrWhiteSpace(t.MinioEndpoint)
            || string.IsNullOrWhiteSpace(t.MinioAccessKey)
            || string.IsNullOrWhiteSpace(t.MinioSecretKey))
        {
            // Bao ngay luc khoi dong thay vi de den luc nguoi dung tai tep len moi vo: thieu cau
            // hinh kho luu tru la loi trien khai, khong phai loi nguoi dung.
            throw new InvalidOperationException(
                "Đã chọn LuuTru:Loai = MINIO nhưng thiếu MinioEndpoint / MinioAccessKey / MinioSecretKey.");
        }

        _minio = new MinioClient()
            .WithEndpoint(t.MinioEndpoint)
            .WithCredentials(t.MinioAccessKey, t.MinioSecretKey)
            .WithSSL(t.MinioSuDungSsl)
            .Build();

        _log = log;
    }

    public async Task<string> TaiLenAsync(
        Stream noiDung, string tenLuuTru, string? mimeType, string bucket,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(noiDung);

        await BaoDamBucketAsync(bucket, ct).ConfigureAwait(false);

        // Chia theo ngay de mot bucket khong phinh thanh mot thu muc phang hang tram nghin tep,
        // vua cham khi liet ke vua kho don dep theo moc thoi gian.
        var duongDan = $"{DateTimeOffset.UtcNow:yyyy/MM/dd}/{tenLuuTru}";

        await _minio.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(duongDan)
                .WithStreamData(noiDung)
                .WithObjectSize(noiDung.CanSeek ? noiDung.Length : -1)
                .WithContentType(mimeType ?? "application/octet-stream"),
            ct).ConfigureAwait(false);

        return duongDan;
    }

    public async Task<Stream> TaiXuongAsync(
        string bucket, string duongDan, CancellationToken ct = default)
    {
        // Doc ra bo nho tam roi tra ve: luong cua MinIO chi song trong pham vi callback, tra
        // thang ra ngoai thi nguoi goi doc duoc mot luong da dong.
        var bo = new MemoryStream();

        await _minio.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(duongDan)
                .WithCallbackStream(async (luong, huy) =>
                    await luong.CopyToAsync(bo, huy).ConfigureAwait(false)),
            ct).ConfigureAwait(false);

        bo.Position = 0;
        return bo;
    }

    public async Task XoaAsync(string bucket, string duongDan, CancellationToken ct = default)
        => await _minio.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(bucket).WithObject(duongDan), ct)
            .ConfigureAwait(false);

    public async Task<string> TaoUrlCoThoiHanAsync(
        string bucket, string duongDan, TimeSpan thoiHan, CancellationToken ct = default)
    {
        _ = ct;

        var giay = (int)Math.Clamp(thoiHan.TotalSeconds, 1, ThoiHanToiDa.TotalSeconds);

        return await _minio.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(duongDan)
                .WithExpiry(giay))
            .ConfigureAwait(false);
    }

    public async Task<bool> TonTaiAsync(
        string bucket, string duongDan, CancellationToken ct = default)
    {
        try
        {
            await _minio.StatObjectAsync(
                new StatObjectArgs().WithBucket(bucket).WithObject(duongDan), ct)
                .ConfigureAwait(false);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }

    private async Task BaoDamBucketAsync(string bucket, CancellationToken ct)
    {
        var coRoi = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket), ct).ConfigureAwait(false);

        if (coRoi) return;

        _log.LogInformation("Tạo bucket MinIO {Bucket}", bucket);

        await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct)
            .ConfigureAwait(false);
    }
}
