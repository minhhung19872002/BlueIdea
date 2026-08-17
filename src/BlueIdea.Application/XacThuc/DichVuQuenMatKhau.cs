using System.Security.Cryptography;
using System.Text;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XacThuc;

/// <summary>
/// Chuc nang 21 — Quen mat khau, dat lai bang ma OTP gui qua email.
///
/// Nguyen tac xuyen suot: KHONG BAO GIO tiet lo tai khoan hay email co ton tai hay khong.
/// Ca hai endpoint deu tra ve cung mot thong bao du tai khoan co that hay khong, va deu ton
/// thoi gian tuong duong. Neu phan hoi khac nhau, trang quen mat khau tro thanh cong cu do
/// danh sach can bo cua co quan.
/// </summary>
public sealed class DichVuQuenMatKhau
{
    private const int SoPhutHieuLuc = 15;
    private const int SoLanThuSaiToiDa = 5;

    private readonly IAppDbContext _db;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuMatKhau _matKhau;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDichVuCauHinh _cauHinh;

    public DichVuQuenMatKhau(
        IAppDbContext db,
        IDongHoHeThong dongHo,
        IDichVuMatKhau matKhau,
        INguoiDungHienTai nguoiDung,
        IDichVuCauHinh cauHinh)
    {
        _db = db;
        _dongHo = dongHo;
        _matKhau = matKhau;
        _nguoiDung = nguoiDung;
        _cauHinh = cauHinh;
    }

    /// <summary>
    /// Buoc 1 — nguoi dung nhap ten dang nhap hoac email, he thong gui ma OTP.
    /// Luon hoan tat binh thuong ke ca khi khong tim thay tai khoan.
    /// </summary>
    public async Task YeuCauMaAsync(string dinhDanh, CancellationToken ct = default)
    {
        var chuanHoa = dinhDanh.Trim().ToLowerInvariant();

        var nguoiDung = await _db.NguoiDung
            .FirstOrDefaultAsync(
                x => x.TenDangNhap == chuanHoa
                     || (x.Email != null && x.Email.ToLower() == chuanHoa), ct)
            .ConfigureAwait(false);

        // Ra ve im lang trong ba truong hop: khong co tai khoan, tai khoan khong co email, va
        // tai khoan SSO (khong co mat khau noi bo de dat lai).
        if (nguoiDung is null
            || string.IsNullOrWhiteSpace(nguoiDung.Email)
            || nguoiDung.LoaiTaiKhoan != "NOI_BO"
            || nguoiDung.TrangThaiTaiKhoan != TrangThaiNguoiDung.HoatDong)
        {
            return;
        }

        // Huy moi ma cu con hieu luc: neu de nhieu ma cung song, khong gian doan cua ke tan
        // cong rong gap boi so ma dang mo.
        await HuyMaCuAsync(nguoiDung.TenDangNhap, ct).ConfigureAwait(false);

        var ma = TaoOtp();

        _db.MaXacThucTam.Add(new MaXacThucTam
        {
            Loai = LoaiMaXacThucTam.OtpDatLaiMatKhau,
            Khoa = nguoiDung.TenDangNhap,
            MaBam = Bam(ma),
            HetHan = _dongHo.BayGio.AddMinutes(SoPhutHieuLuc),
            DiaChiIp = _nguoiDung.DiaChiIp
        });

        var tenHeThong = await _cauHinh
            .LayAsync(KhoaCauHinh.TenHeThong, "Nền tảng Sáng kiến", ct)
            .ConfigureAwait(false);

        // Day vao hang doi thay vi goi SMTP truc tiep: SMTP cham hoac chet thi request cua
        // nguoi dung treo theo, va thoi gian phan hoi khac nhau se lo ra tai khoan co ton tai.
        _db.HangDoiGuiTin.Add(new HangDoiGuiTin
        {
            Kenh = "EMAIL",
            NguoiNhan = nguoiDung.Email!,
            TieuDe = $"[{tenHeThong}] Mã đặt lại mật khẩu",
            NoiDung =
                $"Xin chào {nguoiDung.HoTen},\n\n"
                + $"Mã đặt lại mật khẩu của bạn là: {ma}\n\n"
                + $"Mã có hiệu lực trong {SoPhutHieuLuc} phút và chỉ dùng được một lần.\n"
                + "Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này "
                + "và thông báo cho quản trị viên.\n"
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Buoc 2 — doi ma OTP lay mat khau moi.</summary>
    public async Task DatLaiAsync(
        string tenDangNhap, string ma, string matKhauMoi, CancellationToken ct = default)
    {
        var chuanHoa = tenDangNhap.Trim().ToLowerInvariant();

        var banGhi = await _db.MaXacThucTam
            .Where(x => x.Loai == LoaiMaXacThucTam.OtpDatLaiMatKhau && x.Khoa == chuanHoa)
            .OrderByDescending(x => x.NgayTao)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (banGhi is null || !banGhi.ConHieuLuc(_dongHo.BayGio))
        {
            throw new NghiepVuException(MaLoiHeThong.MaXacThucKhongDung,
                "Mã không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới.");
        }

        var khop = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(banGhi.MaBam),
            Encoding.UTF8.GetBytes(Bam(ma)));

        if (!khop)
        {
            banGhi.SoLanThuSai++;

            // Ma chi co 6 chu so: khong chan so lan thu thi do het khong gian ma trong vai phut.
            if (banGhi.SoLanThuSai >= SoLanThuSaiToiDa)
            {
                banGhi.DaDung = true;
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            throw new NghiepVuException(MaLoiHeThong.MaXacThucKhongDung,
                "Mã không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới.");
        }

        var nguoiDung = await _db.NguoiDung
            .FirstOrDefaultAsync(x => x.TenDangNhap == chuanHoa, ct)
            .ConfigureAwait(false);

        if (nguoiDung is null)
        {
            throw new NghiepVuException(MaLoiHeThong.MaXacThucKhongDung,
                "Mã không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới.");
        }

        // Chinh sach mat khau phai ap dung y het luong doi mat khau thong thuong: neu duong
        // dat lai nay long hon, nguoi dung chi can "quen mat khau" la lach duoc moi rang buoc.
        var chinhSach = new ChinhSachMatKhau
        {
            DoDaiToiThieu = await _cauHinh
                .LayAsync(KhoaCauHinh.ChinhSachMatKhauDoDaiToiThieu, 8, ct).ConfigureAwait(false),
            SoLanKhongTrung = await _cauHinh
                .LayAsync(KhoaCauHinh.ChinhSachMatKhauSoLanKhongTrung, 3, ct).ConfigureAwait(false),
            SoNgayHetHan = await _cauHinh
                .LayAsync(KhoaCauHinh.ChinhSachMatKhauSoNgayHetHan, 90, ct).ConfigureAwait(false)
        };

        var loi = _matKhau.KiemTraChinhSach(matKhauMoi, chinhSach);

        if (loi.Count > 0)
        {
            throw new NghiepVuException(
                MaLoiHeThong.MatKhauKhongDatChinhSach, string.Join(" ", loi));
        }

        var lichSu = await _db.LichSuMatKhau.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDung.Id)
            .OrderByDescending(x => x.ThoiGian)
            .Take(chinhSach.SoLanKhongTrung)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (lichSu.Any(x =>
                _matKhau.KiemTra(matKhauMoi, x.MatKhauHash, x.MatKhauSalt ?? string.Empty)))
        {
            throw new NghiepVuException(MaLoiHeThong.MatKhauDaSuDung,
                $"Mật khẩu mới không được trùng {chinhSach.SoLanKhongTrung} mật khẩu gần nhất.");
        }

        var (hash, salt) = _matKhau.BamMatKhau(matKhauMoi);

        _db.LichSuMatKhau.Add(new LichSuMatKhau
        {
            NguoiDungId = nguoiDung.Id,
            MatKhauHash = nguoiDung.MatKhauHash ?? string.Empty,
            MatKhauSalt = nguoiDung.MatKhauSalt,
            ThoiGian = _dongHo.BayGio
        });

        nguoiDung.MatKhauHash = hash;
        nguoiDung.MatKhauSalt = salt;
        nguoiDung.NgayDoiMatKhauCuoi = _dongHo.BayGio;
        nguoiDung.BuocDoiMatKhau = false;

        // Mo khoa luon: nguoi dung thuong den buoc quen mat khau chinh vi da nhap sai nhieu lan
        // va bi khoa tam. Khong mo thi ho dat lai mat khau xong van khong vao duoc.
        nguoiDung.SoLanDangNhapSai = 0;
        nguoiDung.KhoaDen = null;

        banGhi.DaDung = true;

        // Thu hoi moi phien dang mo: neu ke tan cong da chiem duoc tai khoan truoc do, doi mat
        // khau ma khong thu hoi token thi phien cua ho van song them ca tuan.
        var token = await _db.RefreshToken
            .Where(x => x.NguoiDungId == nguoiDung.Id && x.ThoiGianThuHoi == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var t in token)
        {
            t.ThoiGianThuHoi = _dongHo.BayGio;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------

    private async Task HuyMaCuAsync(string tenDangNhap, CancellationToken ct)
    {
        var cu = await _db.MaXacThucTam
            .Where(x => x.Loai == LoaiMaXacThucTam.OtpDatLaiMatKhau
                        && x.Khoa == tenDangNhap
                        && !x.DaDung)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var x in cu)
        {
            x.DaDung = true;
        }
    }

    private static string TaoOtp() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string Bam(string ma)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ma.Trim())));
}
