using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BlueIdea.Application.Chung;
using BlueIdea.Infrastructure.BaoMat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueIdea.UnitTests.BaoMat;

/// <summary>
/// Kiem thu lop goi ClamAV.
///
/// Dung mot may chu TCP gia noi bo dong vai clamd: kiem duoc CA giao thuc INSTREAM (do dai
/// big-endian, goi 0 ket thuc) LAN cach doc ba dang tra loi — ma khong can container ClamAV.
///
/// Diem quan trong nhat duoc khang dinh o day: bo quet khong ket noi duoc thi KHONG duoc bao
/// "sach". Bao sai o cho nay nghia la moi tep deu lot qua khi ClamAV chet.
/// </summary>
public sealed class DichVuQuetVirusClamAvTests
{
    [Fact]
    public async Task Tra_Loi_OK_Thi_Bao_Sach()
    {
        await using var gia = await ClamdGia.TaoAsync("stream: OK\0");

        var ketQua = await gia.QuetAsync("noi dung sach"u8.ToArray());

        ketQua.TrangThai.Should().Be(TrangThaiQuetVirus.Sach);
        ketQua.Sach.Should().BeTrue();
        ketQua.Nhiem.Should().BeFalse();
    }

    [Fact]
    public async Task Tra_Loi_FOUND_Thi_Bao_Nhiem_Va_Lay_Dung_Ten_Ma_Doc()
    {
        await using var gia = await ClamdGia.TaoAsync("stream: Eicar-Test-Signature FOUND\0");

        var ketQua = await gia.QuetAsync("bat ky"u8.ToArray());

        ketQua.TrangThai.Should().Be(TrangThaiQuetVirus.Nhiem);
        ketQua.Nhiem.Should().BeTrue();
        ketQua.TenMaDoc.Should().Be("Eicar-Test-Signature");
    }

    [Fact]
    public async Task Tra_Loi_ERROR_Thi_Bao_Khong_Quet_Duoc_Chu_Khong_Bao_Sach()
    {
        await using var gia = await ClamdGia.TaoAsync("stream: INSTREAM size limit exceeded. ERROR\0");

        var ketQua = await gia.QuetAsync("bat ky"u8.ToArray());

        ketQua.TrangThai.Should().Be(TrangThaiQuetVirus.KhongQuetDuoc);
        ketQua.Sach.Should().BeFalse("lỗi phía bộ quét không đồng nghĩa với tệp sạch");
        ketQua.Nhiem.Should().BeFalse();
    }

    [Fact]
    public async Task Khong_Ket_Noi_Duoc_Thi_Khong_Bao_Sach()
    {
        // Cong khong co ai lang nghe.
        var dichVu = TaoDichVu("127.0.0.1", 1);

        var ketQua = await dichVu.QuetAsync(new MemoryStream("bat ky"u8.ToArray()));

        ketQua.TrangThai.Should().Be(TrangThaiQuetVirus.KhongQuetDuoc);
        ketQua.Sach.Should().BeFalse(
            "ClamAV chết mà báo sạch thì mọi tệp đều lọt qua — đây là điểm hỏng nguy hiểm nhất");
    }

    [Fact]
    public async Task Gui_Dung_Giao_Thuc_INSTREAM()
    {
        await using var gia = await ClamdGia.TaoAsync("stream: OK\0");

        var noiDung = Encoding.UTF8.GetBytes(new string('a', 100_000));
        await gia.QuetAsync(noiDung);

        var daNhan = gia.DuLieuDaNhan;

        // Lenh mo dau.
        Encoding.ASCII.GetString(daNhan[..10]).Should().Be("zINSTREAM\0");

        // Sau lenh la cac goi: [4 byte do dai big-endian][du lieu] ... roi [4 byte 0].
        var i = 10;
        var gopLai = new List<byte>();

        while (true)
        {
            var doDai = BinaryPrimitives.ReadInt32BigEndian(daNhan.AsSpan(i, 4));
            i += 4;

            if (doDai == 0)
            {
                break;
            }

            gopLai.AddRange(daNhan[i..(i + doDai)]);
            i += doDai;
        }

        gopLai.Should().Equal(noiDung, "nội dung ghép lại từ các gói phải khớp nguyên bản");
        i.Should().Be(daNhan.Length, "sau gói kết thúc không được gửi thêm gì");
    }

    [Fact]
    public async Task Tep_Rong_Van_Gui_Duoc_Goi_Ket_Thuc()
    {
        await using var gia = await ClamdGia.TaoAsync("stream: OK\0");

        var ketQua = await gia.QuetAsync(Array.Empty<byte>());

        ketQua.Sach.Should().BeTrue();
        gia.DuLieuDaNhan.Length.Should().Be(14, "10 byte lệnh + 4 byte gói kết thúc");
    }

    // ------------------------------------------------------------------------------------

    private static DichVuQuetVirusClamAv TaoDichVu(string host, int cong)
    {
        var cauHinh = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QuetVirus:Host"] = host,
                ["QuetVirus:Cong"] = cong.ToString(),
                ["QuetVirus:SoGiayCho"] = "5"
            })
            .Build();

        return new DichVuQuetVirusClamAv(cauHinh, NullLogger<DichVuQuetVirusClamAv>.Instance);
    }

    /// <summary>May chu TCP gia dong vai clamd: ghi lai byte nhan duoc roi tra ve cau tra loi cho san.</summary>
    private sealed class ClamdGia : IAsyncDisposable
    {
        private readonly TcpListener _lang;
        private readonly Task _phucVu;
        private readonly MemoryStream _daNhan = new();
        private readonly DichVuQuetVirusClamAv _dichVu;

        private ClamdGia(TcpListener lang, string traLoi)
        {
            _lang = lang;
            var cong = ((IPEndPoint)lang.LocalEndpoint).Port;
            _dichVu = TaoDichVu("127.0.0.1", cong);
            _phucVu = PhucVuAsync(traLoi);
        }

        public byte[] DuLieuDaNhan => _daNhan.ToArray();

        public static Task<ClamdGia> TaoAsync(string traLoi)
        {
            var lang = new TcpListener(IPAddress.Loopback, 0);
            lang.Start();
            return Task.FromResult(new ClamdGia(lang, traLoi));
        }

        public async Task<KetQuaQuetVirus> QuetAsync(byte[] noiDung)
        {
            var ketQua = await _dichVu.QuetAsync(new MemoryStream(noiDung));
            await _phucVu;
            return ketQua;
        }

        private async Task PhucVuAsync(string traLoi)
        {
            using var ketNoi = await _lang.AcceptTcpClientAsync();
            await using var luong = ketNoi.GetStream();

            var dem = new byte[8192];

            // Doc den khi thay goi do dai 0 (ket thuc luong INSTREAM).
            while (true)
            {
                var soByte = await luong.ReadAsync(dem);
                if (soByte == 0)
                {
                    break;
                }

                _daNhan.Write(dem, 0, soByte);

                if (KetThucLuong(_daNhan.ToArray()))
                {
                    break;
                }
            }

            await luong.WriteAsync(Encoding.ASCII.GetBytes(traLoi));
            await luong.FlushAsync();
        }

        /// <summary>Duyet cac goi de biet da nhan duoc goi do dai 0 hay chua.</summary>
        private static bool KetThucLuong(byte[] daNhan)
        {
            const int doDaiLenh = 10;

            if (daNhan.Length < doDaiLenh + 4)
            {
                return false;
            }

            var i = doDaiLenh;

            while (i + 4 <= daNhan.Length)
            {
                var doDai = BinaryPrimitives.ReadInt32BigEndian(daNhan.AsSpan(i, 4));
                i += 4;

                if (doDai == 0)
                {
                    return true;
                }

                if (i + doDai > daNhan.Length)
                {
                    return false;
                }

                i += doDai;
            }

            return false;
        }

        public async ValueTask DisposeAsync()
        {
            _lang.Stop();
            await _daNhan.DisposeAsync();
        }
    }
}
