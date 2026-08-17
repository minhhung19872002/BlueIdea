using System.Security.Cryptography;

namespace BlueIdea.Application.QuanTri;

/// <summary>
/// Sinh mat khau tam khi tao tai khoan hoac dat lai mat khau.
///
/// Dung <see cref="RandomNumberGenerator"/> chu KHONG dung <c>Random</c>: day la thong tin xac thuc
/// that, doan duoc mot mat khau tam nghia la chiem duoc tai khoan.
/// </summary>
public static class BoSinhMatKhauTam
{
    // Bo cac ky tu de nham khi doc/danh may tay: I l 1, O 0.
    private const string ChuHoa = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string ChuThuong = "abcdefghijkmnopqrstuvwxyz";
    private const string ChuSo = "23456789";
    private const string KyTuDacBiet = "@#$%&*!?";
    private const string TatCa = ChuHoa + ChuThuong + ChuSo + KyTuDacBiet;

    /// <summary>Sinh mat khau co du 4 nhom ky tu va dat do dai yeu cau.</summary>
    public static string Sinh(int doDai)
    {
        // Duoi 4 ky tu thi khong the du ca 4 nhom.
        var doDaiThuc = Math.Max(4, doDai);

        var kyTu = new List<char>
        {
            ChuHoa[RandomNumberGenerator.GetInt32(ChuHoa.Length)],
            ChuThuong[RandomNumberGenerator.GetInt32(ChuThuong.Length)],
            ChuSo[RandomNumberGenerator.GetInt32(ChuSo.Length)],
            KyTuDacBiet[RandomNumberGenerator.GetInt32(KyTuDacBiet.Length)]
        };

        while (kyTu.Count < doDaiThuc)
        {
            kyTu.Add(TatCa[RandomNumberGenerator.GetInt32(TatCa.Length)]);
        }

        // Xao tron de 4 ky tu bat buoc khong luon nam o dau chuoi.
        for (var i = kyTu.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (kyTu[i], kyTu[j]) = (kyTu[j], kyTu[i]);
        }

        return new string(kyTu.ToArray());
    }
}
