using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Infrastructure.KySo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BlueIdea.UnitTests.KySo;

/// <summary>
/// Kiem thu ky so PKCS#7 bang chung thu TU KY sinh ngay trong kiem thu.
///
/// Nho vay kiem duoc tron ven duong ky -> xac minh -> phat hien sua doi, ma khong can chung thu
/// that cua nha cung cap CA. Doi sang CA that chi la doi nguon chung thu, thuat toan giu nguyen.
/// </summary>
public sealed class BoKySoPkcs7Tests : IDisposable
{
    private readonly List<string> _tepTam = new();

    [Fact]
    public async Task Ky_Roi_Xac_Minh_Lai_Duoc()
    {
        var (bo, cauHinh) = TaoBoKy();
        var noiDung = Encoding.UTF8.GetBytes("Quyết định công nhận sáng kiến số 125/QĐ-UBND");

        var ketQua = await bo.KyAsync(noiDung, cauHinh);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBaoLoi);
        ketQua.TepDaKy.Should().NotBeNullOrEmpty();
        ketQua.SerialChungThu.Should().NotBeNullOrEmpty();
        ketQua.NguoiCapChungThu.Should().Contain("BlueIdea");

        var xacMinh = await bo.XacMinhAsync(noiDung, ketQua.TepDaKy!);

        xacMinh.CoChuKy.Should().BeTrue();
        xacMinh.HopLe.Should().BeTrue();
        xacMinh.SerialChungThu.Should().Be(ketQua.SerialChungThu);
        xacMinh.ThoiGianKy.Should().NotBeNull("thiếu dấu thời gian thì không chứng minh được "
                                              + "văn bản ký lúc chứng thư còn hiệu lực");
    }

    [Fact]
    public async Task Chu_Ky_Tach_Roi_Nen_Khong_Boc_Noi_Dung_Goc()
    {
        var (bo, cauHinh) = TaoBoKy();

        const string biMat = "NOI DUNG MAT KHONG DUOC NHUNG VAO CHU KY";
        var noiDung = Encoding.UTF8.GetBytes(biMat);

        var ketQua = await bo.KyAsync(noiDung, cauHinh);

        ketQua.ThanhCong.Should().BeTrue();

        // Chữ ký detached: tệp chữ ký KHÔNG chứa nội dung gốc.
        Encoding.UTF8.GetString(ketQua.TepDaKy!).Should().NotContain(biMat);
    }

    [Fact]
    public async Task Tep_Khong_Co_Chu_Ky_Thi_Bao_Khong_Co_Chu_Ky()
    {
        var (bo, _) = TaoBoKy();

        var xacMinh = await bo.XacMinhAsync(
            "nội dung gốc"u8.ToArray(), "chỉ là văn bản thường"u8.ToArray());

        xacMinh.CoChuKy.Should().BeFalse();
        xacMinh.HopLe.Should().BeFalse();
    }

    [Fact]
    public async Task Noi_Dung_Goc_Bi_Sua_Thi_Xac_Minh_That_Bai()
    {
        var (bo, cauHinh) = TaoBoKy();

        var ketQua = await bo.KyAsync("nội dung gốc"u8.ToArray(), cauHinh);

        // Đây là điểm cốt lõi của ký số: sửa văn bản sau khi ký phải bị phát hiện.
        var xacMinh = await bo.XacMinhAsync("nội dung ĐÃ BỊ SỬA"u8.ToArray(), ketQua.TepDaKy!);

        xacMinh.HopLe.Should().BeFalse(
            "sửa nội dung sau khi ký mà chữ ký vẫn hợp lệ thì ký số vô nghĩa");
    }

    [Fact]
    public async Task Chu_Ky_Bi_Sua_Thi_Xac_Minh_That_Bai()
    {
        var (bo, cauHinh) = TaoBoKy();
        var ketQua = await bo.KyAsync("nội dung gốc"u8.ToArray(), cauHinh);

        var hong = ketQua.TepDaKy!.ToArray();

        // Sửa ở CUỐI khối CMS — trong bố cục DER, giá trị chữ ký nằm ở cuối.
        // Lật byte ở giữa sẽ rơi vào phần chứng thư nhúng, không phải giá trị chữ ký.
        for (var i = hong.Length - 8; i < hong.Length; i++)
        {
            hong[i] ^= 0xFF;
        }

        var xacMinh = await bo.XacMinhAsync("nội dung gốc"u8.ToArray(), hong);

        xacMinh.HopLe.Should().BeFalse("chữ ký bị sửa phải bị phát hiện");
    }

    [Fact]
    public async Task Chung_Thu_Het_Han_Thi_Tu_Choi_Ky()
    {
        // Chứng thư đã hết hiệu lực từ hôm qua.
        var (bo, cauHinh) = TaoBoKy(
            hieuLucTu: DateTimeOffset.Now.AddYears(-2),
            hieuLucDen: DateTimeOffset.Now.AddDays(-1));

        var ketQua = await bo.KyAsync("nội dung"u8.ToArray(), cauHinh);

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.ThongBaoLoi.Should().Contain("hết hiệu lực");
        ketQua.TepDaKy.Should().BeNull(
            "văn bản ký bằng chứng thư hết hạn không có giá trị pháp lý");
    }

    [Fact]
    public async Task Khong_Tim_Thay_Chung_Thu_Thi_Bao_Loi_Ro_Rang()
    {
        var cauHinhUngDung = new ConfigurationBuilder().Build();

        var bo = new BoKySoPkcs7(
            Substitute.For<IDichVuMaHoa>(), cauHinhUngDung, NullLogger<BoKySoPkcs7>.Instance);

        var ketQua = await bo.KyAsync("nội dung"u8.ToArray(), new CauHinhChuKySo());

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.ThongBaoLoi.Should().Contain("Không tìm thấy chứng thư");
    }

    // ------------------------------------------------------------------------------------

    private (BoKySoPkcs7 Bo, CauHinhChuKySo CauHinh) TaoBoKy(
        DateTimeOffset? hieuLucTu = null, DateTimeOffset? hieuLucDen = null)
    {
        const string matKhau = "mat-khau-pfx-kiem-thu";

        var duongDan = TaoPfxTuKy(matKhau, hieuLucTu, hieuLucDen);

        var cauHinhUngDung = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KySo:DuongDanPfx"] = duongDan,
                ["KySo:MatKhauPfx"] = matKhau
            })
            .Build();

        var bo = new BoKySoPkcs7(
            Substitute.For<IDichVuMaHoa>(), cauHinhUngDung, NullLogger<BoKySoPkcs7>.Instance);

        return (bo, new CauHinhChuKySo { ThuatToan = "SHA256withRSA", NhaCungCap = "KIEM_THU" });
    }

    /// <summary>Sinh chứng thư tự ký kèm khoá bí mật, lưu ra tệp PFX tạm.</summary>
    private string TaoPfxTuKy(
        string matKhau, DateTimeOffset? hieuLucTu, DateTimeOffset? hieuLucDen)
    {
        using var khoa = RSA.Create(2048);

        var yeuCau = new CertificateRequest(
            "CN=Kiem thu ky so, O=BlueIdea, C=VN",
            khoa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        yeuCau.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, true));

        var tu = hieuLucTu ?? DateTimeOffset.Now.AddDays(-1);
        var den = hieuLucDen ?? DateTimeOffset.Now.AddYears(1);

        using var chungThu = yeuCau.CreateSelfSigned(tu, den);

        var duongDan = Path.Combine(Path.GetTempPath(), $"blueidea-kyso-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(duongDan, chungThu.Export(X509ContentType.Pfx, matKhau));

        _tepTam.Add(duongDan);

        return duongDan;
    }

    public void Dispose()
    {
        foreach (var tep in _tepTam)
        {
            try
            {
                File.Delete(tep);
            }
            catch (IOException)
            {
                // Tệp tạm còn bị giữ — hệ điều hành sẽ dọn sau, không cần làm hỏng kiểm thử.
            }
        }
    }
}
