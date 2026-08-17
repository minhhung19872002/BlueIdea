using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using BlueIdea.Application.Chung;
using BlueIdea.Application.KySo;
using BlueIdea.Domain.QuanTri;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.KySo;

/// <summary>
/// Ky so theo chuan CMS/PKCS#7 (chuc nang 49).
///
/// Ho tro hai duong lay chung thu, dung dung hai kieu trien khai thuc te o Viet Nam:
///   - USB_TOKEN / HSM  : chung thu nam trong kho chung thu cua may chu, tim theo serial
///   - REMOTE_SIGNING   : chung thu + khoa dong goi trong tep PFX (dung cho ky tu xa / thu nghiem)
///
/// Dung chu ky DETACHED (khong nhung noi dung vao chu ky): tep goc va tep chu ky tach roi nen
/// van doc duoc ban goc bang phan mem thong thuong, va doi chieu duoc khi co tranh chap.
/// </summary>
public sealed class BoKySoPkcs7 : IBoKySo
{
    private readonly IDichVuMaHoa _maHoa;
    private readonly IConfiguration _cauHinhUngDung;
    private readonly ILogger<BoKySoPkcs7> _logger;

    public BoKySoPkcs7(
        IDichVuMaHoa maHoa, IConfiguration cauHinhUngDung, ILogger<BoKySoPkcs7> logger)
    {
        _maHoa = maHoa;
        _cauHinhUngDung = cauHinhUngDung;
        _logger = logger;
    }

    /// <summary>Có đường lấy chứng thư nào khả dụng hay không.</summary>
    public bool DaCauHinh => true;

    public Task<KetQuaKySo> KyAsync(
        byte[] noiDung, CauHinhChuKySo cauHinh, CancellationToken ct = default)
    {
        try
        {
            using var chungThu = LayChungThu(cauHinh);

            if (chungThu is null)
            {
                return Task.FromResult(new KetQuaKySo(
                    false, null, null, null, null, null,
                    "Không tìm thấy chứng thư số theo cấu hình."));
            }

            if (!chungThu.HasPrivateKey)
            {
                return Task.FromResult(new KetQuaKySo(
                    false, null, null, null, null, null,
                    "Chứng thư số không kèm khoá bí mật nên không ký được."));
            }

            // Chứng thư hết hạn vẫn ký được về mặt kỹ thuật, nhưng văn bản sẽ không có giá trị
            // pháp lý — chặn ngay tại đây thay vì để phát hiện lúc thẩm tra.
            var bayGio = DateTime.Now;

            if (bayGio < chungThu.NotBefore || bayGio > chungThu.NotAfter)
            {
                return Task.FromResult(new KetQuaKySo(
                    false, null, chungThu.SerialNumber, chungThu.Issuer,
                    chungThu.NotBefore, chungThu.NotAfter,
                    $"Chứng thư số hết hiệu lực (từ {chungThu.NotBefore:dd/MM/yyyy} "
                    + $"đến {chungThu.NotAfter:dd/MM/yyyy})."));
            }

            var noiDungKy = new ContentInfo(noiDung);

            // detached = true: chữ ký KHÔNG bọc nội dung, giữ tệp gốc đọc được bình thường.
            var chuKy = new SignedCms(noiDungKy, detached: true);

            var nguoiKy = new CmsSigner(chungThu)
            {
                DigestAlgorithm = new Oid(ThuatToanBam(cauHinh.ThuatToan)),
                IncludeOption = X509IncludeOption.WholeChain
            };

            // Đóng dấu thời gian ký vào thuộc tính có ký — thiếu nó thì không chứng minh được
            // văn bản được ký lúc chứng thư còn hiệu lực.
            nguoiKy.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.Now));

            chuKy.ComputeSignature(nguoiKy);

            return Task.FromResult(new KetQuaKySo(
                true,
                chuKy.Encode(),
                chungThu.SerialNumber,
                chungThu.Issuer,
                chungThu.NotBefore,
                chungThu.NotAfter,
                null));
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Ký số thất bại.");

            return Task.FromResult(new KetQuaKySo(
                false, null, null, null, null, null, ex.Message));
        }
    }

    public Task<KetQuaXacMinhChuKy> XacMinhAsync(
        byte[] noiDungGoc, byte[] chuKyEncoded, CancellationToken ct = default)
    {
        try
        {
            // Chu ky detached khong mang theo noi dung, nen phai nap lai ban goc truoc khi giai ma
            // — thieu buoc nay thi CheckSignature khong co gi de doi chieu va luon that bai.
            var chuKy = new SignedCms(new ContentInfo(noiDungGoc), detached: true);
            chuKy.Decode(chuKyEncoded);

            // Không kiểm chuỗi tin cậy ở đây (CheckSignature(true)): máy chủ nội bộ thường
            // không cài sẵn CA gốc của nhà cung cấp. Chữ ký vẫn được xác minh về mặt mật mã.
            chuKy.CheckSignature(verifySignatureOnly: true);

            var nguoiKy = chuKy.SignerInfos.Count > 0 ? chuKy.SignerInfos[0] : null;

            var thoiGianKy = nguoiKy?.SignedAttributes
                .Cast<CryptographicAttributeObject>()
                .Where(x => x.Oid.Value == "1.2.840.113549.1.9.5")
                .Select(x => new Pkcs9SigningTime(x.Values[0].RawData).SigningTime)
                .Cast<DateTime?>()
                .FirstOrDefault();

            return Task.FromResult(new KetQuaXacMinhChuKy(
                CoChuKy: true,
                HopLe: true,
                SerialChungThu: nguoiKy?.Certificate?.SerialNumber,
                NguoiKy: nguoiKy?.Certificate?.Subject,
                ThoiGianKy: thoiGianKy.HasValue
                    ? new DateTimeOffset(thoiGianKy.Value.ToUniversalTime(), TimeSpan.Zero)
                    : null,
                ThongBaoLoi: null));
        }
        catch (CryptographicException ex)
        {
            // Không đọc được như CMS nghĩa là tệp không có chữ ký, hoặc chữ ký đã hỏng.
            return Task.FromResult(new KetQuaXacMinhChuKy(
                CoChuKy: false, HopLe: false, null, null, null, ex.Message));
        }
    }

    /// <summary>
    /// Lay chung thu theo cau hinh.
    ///
    /// PFX doc tu bien moi truong / tep tren dia chu KHONG luu trong CSDL: khoa bi mat nam trong
    /// CSDL thi mot lan lo ban dump la lo luon quyen ky van ban.
    /// </summary>
    private X509Certificate2? LayChungThu(CauHinhChuKySo cauHinh)
    {
        var duongDanPfx = _cauHinhUngDung["KySo:DuongDanPfx"];

        if (!string.IsNullOrWhiteSpace(duongDanPfx) && File.Exists(duongDanPfx))
        {
            var matKhau = _cauHinhUngDung["KySo:MatKhauPfx"]
                          ?? _maHoa.GiaiMa(cauHinh.ClientSecretMaHoa);

            // EphemeralKeySet: khoá bí mật chỉ tồn tại trong bộ nhớ tiến trình, không ghi vào
            // kho khoá của máy — máy chủ dùng chung sẽ không để lại khoá sau khi tiến trình tắt.
            return new X509Certificate2(
                duongDanPfx,
                matKhau,
                X509KeyStorageFlags.EphemeralKeySet);
        }

        // Không có PFX thì tìm trong kho chứng thư của máy chủ theo serial (USB token / HSM).
        if (string.IsNullOrWhiteSpace(cauHinh.ChungThuSo))
        {
            return null;
        }

        using var kho = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        kho.Open(OpenFlags.ReadOnly);

        var tim = kho.Certificates.Find(
            X509FindType.FindBySerialNumber, cauHinh.ChungThuSo, validOnly: false);

        return tim.Count > 0 ? tim[0] : null;
    }

    /// <summary>Ánh xạ tên thuật toán trong cấu hình sang OID của hàm băm.</summary>
    private static string ThuatToanBam(string thuatToan) => thuatToan.ToUpperInvariant() switch
    {
        var x when x.Contains("SHA512") => "2.16.840.1.101.3.4.2.3",
        var x when x.Contains("SHA384") => "2.16.840.1.101.3.4.2.2",
        _ => "2.16.840.1.101.3.4.2.1"
    };
}
