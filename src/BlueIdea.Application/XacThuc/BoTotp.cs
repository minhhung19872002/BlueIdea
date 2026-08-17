using System.Security.Cryptography;
using System.Text;

namespace BlueIdea.Application.XacThuc;

/// <summary>
/// Sinh va kiem tra ma TOTP theo RFC 6238 (HMAC-SHA1, 6 chu so, chu ky 30 giay).
///
/// Tu cai chu khong dung thu vien ngoai: thuat toan chi la HMAC + cat bit, .NET da co san
/// <see cref="HMACSHA1"/>, va rang buoc Muc 3.2 E-HSMT yeu cau moi thanh phan xu ly du lieu
/// nhay cam deu chay trong ha tang noi bo.
///
/// Toan bo lop la ham thuan (khong cham CSDL, khong doc dong ho he thong tru khi duoc truyen
/// vao) de kiem chung duoc bang bo vector mau trong Phu luc B cua RFC 6238.
/// </summary>
public static class BoTotp
{
    /// <summary>Do dai chu ky mot ma, tinh bang giay.</summary>
    public const int SoGiayMoiBuoc = 30;

    /// <summary>So chu so cua ma hien cho nguoi dung.</summary>
    public const int SoChuSo = 6;

    private const string BangBase32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>Sinh bi mat ngau nhien 160 bit (do dai khuyen nghi cua RFC 4226 cho HMAC-SHA1).</summary>
    public static string TaoBiMat()
    {
        var bo = RandomNumberGenerator.GetBytes(20);
        return MaHoaBase32(bo);
    }

    /// <summary>
    /// Chi so buoc thoi gian ung voi mot moc thoi diem. Tra ve rieng de tang goi duoc dung
    /// lam moc chong dung lai ma (xem ghi chu trong <c>DichVuMfa</c>).
    /// </summary>
    public static long TinhBuoc(DateTimeOffset thoiDiem)
        => thoiDiem.ToUnixTimeSeconds() / SoGiayMoiBuoc;

    /// <summary>Sinh ma cho mot buoc thoi gian cu the.</summary>
    public static string SinhMa(string biMatBase32, long buoc)
    {
        var khoa = GiaiMaBase32(biMatBase32);

        var dem = BitConverter.GetBytes(buoc);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(dem);
        }

        using var hmac = new HMACSHA1(khoa);
        var bam = hmac.ComputeHash(dem);

        // "Dynamic truncation" theo RFC 4226 muc 5.3.
        var lech = bam[^1] & 0x0F;
        var nhiPhan = ((bam[lech] & 0x7F) << 24)
                      | ((bam[lech + 1] & 0xFF) << 16)
                      | ((bam[lech + 2] & 0xFF) << 8)
                      | (bam[lech + 3] & 0xFF);

        var ma = nhiPhan % (int)Math.Pow(10, SoChuSo);
        return ma.ToString(new string('0', SoChuSo));
    }

    /// <summary>
    /// Kiem tra ma nguoi dung nhap. Tra ve buoc thoi gian khop, hoac <c>null</c> neu khong khop.
    ///
    /// Tra ve BUOC chu khong phai <c>bool</c>: ben goi can biet ma thuoc buoc nao de ghi lai va
    /// tu choi lan dung thu hai cung buoc do. Neu chi tra bool thi mot ma bi nhin trom van dung
    /// lai duoc trong suot cua so con hieu luc.
    /// </summary>
    /// <param name="doLechChoPhep">
    /// So buoc chap nhan lech ve hai phia. Mac dinh 1 (tuong duong ±30 giay) theo khuyen nghi
    /// RFC 6238 muc 5.2 de bu dong ho lech giua may chu va dien thoai.
    /// </param>
    public static long? KiemTra(
        string biMatBase32, string? ma, DateTimeOffset thoiDiem, int doLechChoPhep = 1)
    {
        if (string.IsNullOrWhiteSpace(ma))
        {
            return null;
        }

        var maSach = ma.Trim().Replace(" ", string.Empty);

        if (maSach.Length != SoChuSo || !maSach.All(char.IsAsciiDigit))
        {
            return null;
        }

        var buocHienTai = TinhBuoc(thoiDiem);

        for (var lech = -doLechChoPhep; lech <= doLechChoPhep; lech++)
        {
            var buoc = buocHienTai + lech;

            // So sanh theo thoi gian hang so: so sanh chuoi thong thuong dung sau ky tu dau
            // khac nhau, ro ri thong tin qua thoi gian phan hoi.
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(SinhMa(biMatBase32, buoc)),
                    Encoding.ASCII.GetBytes(maSach)))
            {
                return buoc;
            }
        }

        return null;
    }

    /// <summary>
    /// Chuoi <c>otpauth://</c> de ung dung xac thuc quet ma QR (Google Authenticator,
    /// Microsoft Authenticator, FreeOTP...).
    /// </summary>
    public static string TaoUriGhiDanh(string tenDangNhap, string tenHeThong, string biMatBase32)
    {
        var nhanPhat = Uri.EscapeDataString(tenHeThong);
        var nhan = Uri.EscapeDataString($"{tenHeThong}:{tenDangNhap}");

        return $"otpauth://totp/{nhan}?secret={biMatBase32}&issuer={nhanPhat}"
               + $"&algorithm=SHA1&digits={SoChuSo}&period={SoGiayMoiBuoc}";
    }

    // ------------------------------------------------------------------ Base32 (RFC 4648)

    /// <summary>Ma hoa Base32 (RFC 4648) — dinh dang bi mat ma ung dung xac thuc doc duoc.</summary>
    public static string MaHoaBase32(ReadOnlySpan<byte> du)
    {
        var ket = new StringBuilder();
        int bo = 0, soBit = 0;

        foreach (var b in du)
        {
            bo = (bo << 8) | b;
            soBit += 8;

            while (soBit >= 5)
            {
                ket.Append(BangBase32[(bo >> (soBit - 5)) & 31]);
                soBit -= 5;
            }
        }

        if (soBit > 0)
        {
            ket.Append(BangBase32[(bo << (5 - soBit)) & 31]);
        }

        return ket.ToString();
    }

    /// <summary>Giai ma Base32 (RFC 4648).</summary>
    public static byte[] GiaiMaBase32(string chuoi)
    {
        var sach = chuoi.Trim().TrimEnd('=').ToUpperInvariant();
        var du = new List<byte>(sach.Length * 5 / 8);
        int bo = 0, soBit = 0;

        foreach (var c in sach)
        {
            var chiSo = BangBase32.IndexOf(c);
            if (chiSo < 0)
            {
                throw new FormatException($"Ký tự '{c}' không thuộc bảng mã Base32.");
            }

            bo = (bo << 5) | chiSo;
            soBit += 5;

            if (soBit >= 8)
            {
                du.Add((byte)((bo >> (soBit - 8)) & 0xFF));
                soBit -= 8;
            }
        }

        return du.ToArray();
    }
}
