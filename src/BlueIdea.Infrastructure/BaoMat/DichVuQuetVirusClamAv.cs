using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using BlueIdea.Application.Chung;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.BaoMat;

/// <summary>
/// Quet virus bang ClamAV qua giao thuc INSTREAM cua clamd.
///
/// Dung INSTREAM (day noi dung qua socket) chu KHONG dung SCAN theo duong dan: hai ben khong
/// bat buoc chung he tep, va tep chua duoc luu xuong dia luc quet — quet truoc khi ghi thi
/// tep nhiem KHONG BAO GIO cham vao kho luu tru.
///
/// ClamAV chay trong mang rieng cua docker-compose, khong expose ra ngoai.
/// </summary>
public sealed class DichVuQuetVirusClamAv : IDichVuQuetVirus
{
    /// <summary>Kich thuoc goi day sang clamd. clamd mac dinh gioi han 256KB moi goi.</summary>
    private const int KichThuocGoi = 64 * 1024;

    private readonly string _host;
    private readonly int _cong;
    private readonly TimeSpan _thoiGianCho;
    private readonly ILogger<DichVuQuetVirusClamAv> _logger;

    public DichVuQuetVirusClamAv(IConfiguration cauHinh, ILogger<DichVuQuetVirusClamAv> logger)
    {
        _host = cauHinh["QuetVirus:Host"] ?? "clamav";
        _cong = cauHinh.GetValue("QuetVirus:Cong", 3310);
        _thoiGianCho = TimeSpan.FromSeconds(cauHinh.GetValue("QuetVirus:SoGiayCho", 60));
        _logger = logger;
    }

    public async Task<KetQuaQuetVirus> QuetAsync(Stream noiDung, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(noiDung);

        try
        {
            using var ketNoi = new TcpClient();

            using var huy = CancellationTokenSource.CreateLinkedTokenSource(ct);
            huy.CancelAfter(_thoiGianCho);

            await ketNoi.ConnectAsync(_host, _cong, huy.Token).ConfigureAwait(false);

            await using var luong = ketNoi.GetStream();

            // Tien to 'z' = lenh ket thuc bang NUL, tra loi cung ket thuc bang NUL.
            await luong.WriteAsync("zINSTREAM\0"u8.ToArray(), huy.Token).ConfigureAwait(false);

            var dem = new byte[KichThuocGoi];
            var doDai = new byte[4];

            while (true)
            {
                var soByte = await noiDung.ReadAsync(dem, huy.Token).ConfigureAwait(false);
                if (soByte == 0)
                {
                    break;
                }

                BinaryPrimitives.WriteInt32BigEndian(doDai, soByte);
                await luong.WriteAsync(doDai, huy.Token).ConfigureAwait(false);
                await luong.WriteAsync(dem.AsMemory(0, soByte), huy.Token).ConfigureAwait(false);
            }

            // Goi do dai 0 bao ket thuc luong.
            BinaryPrimitives.WriteInt32BigEndian(doDai, 0);
            await luong.WriteAsync(doDai, huy.Token).ConfigureAwait(false);
            await luong.FlushAsync(huy.Token).ConfigureAwait(false);

            var traLoi = await DocTraLoiAsync(luong, huy.Token).ConfigureAwait(false);

            return PhanTich(traLoi);
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            // Khong ket noi duoc bo quet: KHONG coi la sach. Tang goi quyet dinh chan hay cho qua
            // dua tren cau hinh, o day chi bao trung thuc la chua quet duoc.
            _logger.LogWarning(ex, "Không kết nối được ClamAV tại {Host}:{Cong}.", _host, _cong);

            return new KetQuaQuetVirus
            {
                TrangThai = TrangThaiQuetVirus.KhongQuetDuoc,
                ThongBao = "Không kết nối được dịch vụ quét virus."
            };
        }
    }

    private static async Task<string> DocTraLoiAsync(NetworkStream luong, CancellationToken ct)
    {
        var bo = new MemoryStream();
        var dem = new byte[256];

        while (true)
        {
            var soByte = await luong.ReadAsync(dem, ct).ConfigureAwait(false);
            if (soByte == 0)
            {
                break;
            }

            bo.Write(dem, 0, soByte);

            // Tra loi ket thuc bang NUL.
            if (dem[soByte - 1] == 0)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(bo.ToArray()).TrimEnd('\0', '\n');
    }

    /// <summary>
    /// clamd tra ve mot trong ba dang:
    ///   "stream: OK"                              -> sach
    ///   "stream: Eicar-Test-Signature FOUND"      -> nhiem
    ///   "... ERROR"                               -> loi phia bo quet
    /// </summary>
    private KetQuaQuetVirus PhanTich(string traLoi)
    {
        if (traLoi.EndsWith("OK", StringComparison.Ordinal))
        {
            return new KetQuaQuetVirus { TrangThai = TrangThaiQuetVirus.Sach };
        }

        if (traLoi.EndsWith("FOUND", StringComparison.Ordinal))
        {
            // Dang: "stream: <ten ma doc> FOUND"
            var i = traLoi.IndexOf(": ", StringComparison.Ordinal);
            var ten = i >= 0
                ? traLoi[(i + 2)..].Replace(" FOUND", string.Empty, StringComparison.Ordinal)
                : traLoi;

            _logger.LogWarning("ClamAV phát hiện mã độc: {TenMaDoc}", ten);

            return new KetQuaQuetVirus
            {
                TrangThai = TrangThaiQuetVirus.Nhiem,
                TenMaDoc = ten,
                ThongBao = $"Phát hiện mã độc: {ten}"
            };
        }

        _logger.LogWarning("ClamAV trả về phản hồi không nhận diện được: {TraLoi}", traLoi);

        return new KetQuaQuetVirus
        {
            TrangThai = TrangThaiQuetVirus.KhongQuetDuoc,
            ThongBao = traLoi
        };
    }
}

/// <summary>
/// Cai dat thay the khi TAT quet virus (moi truong phat trien, kiem thu tu dong).
/// Bao ro la KHONG QUET chu khong bao "sach" — de nhat ky khong ghi sai su that.
/// </summary>
public sealed class DichVuQuetVirusTat : IDichVuQuetVirus
{
    public Task<KetQuaQuetVirus> QuetAsync(Stream noiDung, CancellationToken ct = default)
        => Task.FromResult(new KetQuaQuetVirus
        {
            TrangThai = TrangThaiQuetVirus.KhongQuetDuoc,
            ThongBao = "Chức năng quét virus đang tắt."
        });
}
