using System.Security.Cryptography;
using System.Text.Json;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XacThuc;

/// <summary>Ket qua buoc bat dau ghi danh MFA — du de ve ma QR va nhap tay.</summary>
public sealed record BatDauGhiDanhMfaDto(string BiMat, string UriGhiDanh);

/// <summary>Ket qua bat MFA thanh cong: danh sach ma khoi phuc CHI hien duy nhat mot lan.</summary>
public sealed record KetQuaBatMfaDto(IReadOnlyList<string> MaKhoiPhuc);

/// <summary>Trang thai MFA cua tai khoan dang dang nhap.</summary>
public sealed record TrangThaiMfaDto(bool DaBat, DateTimeOffset? NgayBat, int SoMaKhoiPhucConLai);

/// <summary>
/// Chuc nang 21 — Xac thuc hai lop bang TOTP.
///
/// Ghi danh chia lam HAI buoc co chu dich: buoc mot chi sinh bi mat va tra ve ma QR, phai sang
/// buoc hai nhap dung mot ma sinh tu chinh bi mat do thi MFA moi duoc bat. Neu bat ngay tu buoc
/// mot, nguoi dung quet QR hong hoac luu nham tai khoan se tu khoa minh ra ngoai va chi con
/// cach nho quan tri vien can thiep.
/// </summary>
public sealed class DichVuMfa
{
    private const int SoMaKhoiPhuc = 10;

    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDungHienTai;
    private readonly IDichVuMaHoa _maHoa;
    private readonly IDichVuMatKhau _matKhau;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDichVuNhatKy _nhatKy;

    public DichVuMfa(
        IAppDbContext db,
        INguoiDungHienTai nguoiDungHienTai,
        IDichVuMaHoa maHoa,
        IDichVuMatKhau matKhau,
        IDichVuCauHinh cauHinh,
        IDongHoHeThong dongHo,
        IDichVuPhanQuyen phanQuyen,
        IDichVuNhatKy nhatKy)
    {
        _db = db;
        _nguoiDungHienTai = nguoiDungHienTai;
        _maHoa = maHoa;
        _matKhau = matKhau;
        _cauHinh = cauHinh;
        _dongHo = dongHo;
        _phanQuyen = phanQuyen;
        _nhatKy = nhatKy;
    }

    /// <summary>Trang thai MFA cua chinh nguoi dang dang nhap.</summary>
    public async Task<TrangThaiMfaDto> TrangThaiAsync(CancellationToken ct = default)
    {
        var nguoiDung = await LayChinhMinhAsync(ct).ConfigureAwait(false);

        return new TrangThaiMfaDto(
            nguoiDung.MfaEnabled,
            nguoiDung.MfaNgayBat,
            DemMaKhoiPhucConLai(nguoiDung));
    }

    /// <summary>Buoc 1 — sinh bi mat moi va tra ve chuoi otpauth de quet ma QR.</summary>
    public async Task<BatDauGhiDanhMfaDto> BatDauGhiDanhAsync(CancellationToken ct = default)
    {
        var nguoiDung = await LayChinhMinhAsync(ct).ConfigureAwait(false);

        if (nguoiDung.MfaEnabled)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Tài khoản đã bật xác thực hai lớp. Hãy tắt trước khi ghi danh lại.");
        }

        var biMat = BoTotp.TaoBiMat();

        // Luu ngay o dang ma hoa nhung CHUA bat co MfaEnabled: bi mat phai ton tai de buoc xac
        // nhan doi chieu duoc, con MFA thi chua co hieu luc.
        nguoiDung.MfaSecret = _maHoa.MaHoa(biMat);
        nguoiDung.MfaBuocDaDung = null;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var tenHeThong = await _cauHinh
            .LayAsync(KhoaCauHinh.TenHeThong, "Nền tảng Sáng kiến", ct)
            .ConfigureAwait(false);

        return new BatDauGhiDanhMfaDto(
            biMat, BoTotp.TaoUriGhiDanh(nguoiDung.TenDangNhap, tenHeThong, biMat));
    }

    /// <summary>Buoc 2 — nhap ma tu ung dung xac thuc de bat MFA that su.</summary>
    public async Task<KetQuaBatMfaDto> XacNhanGhiDanhAsync(string ma, CancellationToken ct = default)
    {
        var nguoiDung = await LayChinhMinhAsync(ct).ConfigureAwait(false);

        if (nguoiDung.MfaEnabled)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Tài khoản đã bật xác thực hai lớp.");
        }

        var biMat = _maHoa.GiaiMa(nguoiDung.MfaSecret);

        if (string.IsNullOrWhiteSpace(biMat))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Chưa bắt đầu ghi danh. Hãy quét mã QR trước.");
        }

        var buoc = BoTotp.KiemTra(biMat, ma, _dongHo.BayGio);

        if (buoc is null)
        {
            throw new NghiepVuException(MaLoiHeThong.MaXacThucKhongDung,
                "Mã xác thực không đúng. Kiểm tra lại đồng hồ trên điện thoại rồi thử lại.");
        }

        var maKhoiPhuc = TaoMaKhoiPhuc();

        nguoiDung.MfaEnabled = true;
        nguoiDung.MfaNgayBat = _dongHo.BayGio;
        nguoiDung.MfaBuocDaDung = buoc;
        nguoiDung.MfaMaKhoiPhuc = JsonSerializer.Serialize(
            maKhoiPhuc.Select(x => BamMaKhoiPhuc(x)).ToList());

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new KetQuaBatMfaDto(maKhoiPhuc);
    }

    /// <summary>
    /// Tat MFA. Bat buoc nhap lai mat khau: neu chi can phien dang dang nhap la tat duoc thi
    /// mot may bo quen khoa man hinh du de vo hieu hoa lop bao ve nay.
    /// </summary>
    public async Task TatAsync(string matKhau, string? ma, CancellationToken ct = default)
    {
        var nguoiDung = await LayChinhMinhAsync(ct).ConfigureAwait(false);

        if (!nguoiDung.MfaEnabled)
        {
            return;
        }

        var dungMatKhau = !string.IsNullOrEmpty(nguoiDung.MatKhauHash)
                          && _matKhau.KiemTra(matKhau, nguoiDung.MatKhauHash,
                              nguoiDung.MatKhauSalt ?? string.Empty);

        if (!dungMatKhau)
        {
            throw new NghiepVuException(MaLoiHeThong.SaiTaiKhoanMatKhau, "Mật khẩu không đúng.");
        }

        var biMat = _maHoa.GiaiMa(nguoiDung.MfaSecret);

        if (string.IsNullOrWhiteSpace(biMat))
        {
            throw new NghiepVuException(MaLoiHeThong.MaXacThucKhongDung,
                "Không thể giải mã bí mật MFA. Liên hệ quản trị viên.");
        }

        if (BoTotp.KiemTra(biMat, ma, _dongHo.BayGio) is null
            && !DungMaKhoiPhuc(nguoiDung, ma))
        {
            throw new NghiepVuException(MaLoiHeThong.MaXacThucKhongDung,
                "Mã xác thực không đúng.");
        }

        XoaSachMfa(nguoiDung);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Sinh lai bo ma khoi phuc, huy toan bo bo cu.</summary>
    public async Task<KetQuaBatMfaDto> TaoLaiMaKhoiPhucAsync(
        string matKhau, CancellationToken ct = default)
    {
        var nguoiDung = await LayChinhMinhAsync(ct).ConfigureAwait(false);

        if (!nguoiDung.MfaEnabled)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Tài khoản chưa bật xác thực hai lớp.");
        }

        var dungMatKhau = !string.IsNullOrEmpty(nguoiDung.MatKhauHash)
                          && _matKhau.KiemTra(matKhau, nguoiDung.MatKhauHash,
                              nguoiDung.MatKhauSalt ?? string.Empty);

        if (!dungMatKhau)
        {
            throw new NghiepVuException(MaLoiHeThong.SaiTaiKhoanMatKhau, "Mật khẩu không đúng.");
        }

        var maKhoiPhuc = TaoMaKhoiPhuc();

        nguoiDung.MfaMaKhoiPhuc = JsonSerializer.Serialize(
            maKhoiPhuc.Select(x => BamMaKhoiPhuc(x)).ToList());

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new KetQuaBatMfaDto(maKhoiPhuc);
    }

    /// <summary>
    /// Quan tri vien go MFA cho nguoi khac — dung khi nhan vien mat dien thoai va het ma khoi phuc.
    /// Khong doi ma vi dung nghia la nguoi do khong con cach nao sinh ma.
    /// </summary>
    public async Task GoMfaChoNguoiKhacAsync(Guid nguoiDungId, CancellationToken ct = default)
    {
        if (nguoiDungId == _nguoiDungHienTai.Id)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Dùng luồng tắt MFA thông thường để gỡ MFA của chính mình.");
        }

        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.NguoiDungDatLaiMatKhau, nguoiDungId, ct)
            .ConfigureAwait(false);

        var nguoiDung = await _db.NguoiDung.FirstOrDefaultAsync(x => x.Id == nguoiDungId, ct)
            .ConfigureAwait(false)
            ?? throw new NghiepVuException(MaLoiHeThong.KhongTimThay, "Không tìm thấy tài khoản.");

        await BatBuocNguoiDungTrongPhamViAsync(nguoiDung, ct).ConfigureAwait(false);

        var truoc = new { nguoiDung.MfaEnabled, nguoiDung.MfaNgayBat };

        XoaSachMfa(nguoiDung);

        var bayGio = _dongHo.BayGio;
        var tokenDangMo = await _db.RefreshToken
            .Where(x => x.NguoiDungId == nguoiDungId && x.ThoiGianThuHoi == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var token in tokenDangMo)
        {
            token.ThoiGianThuHoi = bayGio;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("GO_MFA_NGUOI_KHAC", "QUAN_TRI", "NguoiDung", nguoiDungId,
            $"Gỡ MFA cho {nguoiDung.TenDangNhap}", duLieuTruoc: truoc, ct: ct)
            .ConfigureAwait(false);
    }

    // ---------------------------------------------------------------- Dung trong luong dang nhap

    /// <summary>
    /// Kiem tra ma o buoc dang nhap. Tra ve <c>true</c> neu hop le; ben goi tu chiu trach nhiem
    /// dem so lan sai va khoa tai khoan.
    ///
    /// Ham nay CO ghi vao <see cref="NguoiDung.MfaBuocDaDung"/> nhung KHONG tu goi SaveChanges —
    /// ben goi luu chung mot lan cung cac thay doi khac cua luong dang nhap.
    /// </summary>
    public bool KiemTraKhiDangNhap(NguoiDung nguoiDung, string? ma)
    {
        var biMat = _maHoa.GiaiMa(nguoiDung.MfaSecret);

        if (string.IsNullOrWhiteSpace(biMat))
        {
            return false;
        }

        var buoc = BoTotp.KiemTra(biMat, ma, _dongHo.BayGio);

        if (buoc is not null)
        {
            // Tu choi ma thuoc buoc da dung: cung mot ma con hieu luc toi 90 giay, khong chan
            // thi nguoi nhin trom man hinh dien thoai van kip dung lai de mo phien thu hai.
            if (nguoiDung.MfaBuocDaDung is { } daDung && buoc <= daDung)
            {
                return false;
            }

            nguoiDung.MfaBuocDaDung = buoc;
            return true;
        }

        return DungMaKhoiPhuc(nguoiDung, ma);
    }

    // ---------------------------------------------------------------- Ham phu tro

    private async Task BatBuocNguoiDungTrongPhamViAsync(NguoiDung mucTieu, CancellationToken ct)
    {
        var nguoiGoiId = _nguoiDungHienTai.Id
                         ?? throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");

        var phamVi = await _phanQuyen.LayPhamViTruyCapAsync(nguoiGoiId, ct).ConfigureAwait(false);

        if (phamVi.ToanHeThong) return;

        if (phamVi.ChiCaNhan || !mucTieu.DonViId.HasValue
                              || !phamVi.DonViIds.Contains(mucTieu.DonViId.Value))
        {
            throw new NghiepVuException(MaLoiHeThong.KhongTimThay, "Không tìm thấy tài khoản.");
        }
    }

    private async Task<NguoiDung> LayChinhMinhAsync(CancellationToken ct)
    {
        var id = _nguoiDungHienTai.Id
                 ?? throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");

        return await _db.NguoiDung.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
               ?? throw new NghiepVuException(MaLoiHeThong.KhongTimThay, "Không tìm thấy tài khoản.");
    }

    private static void XoaSachMfa(NguoiDung nguoiDung)
    {
        nguoiDung.MfaEnabled = false;
        nguoiDung.MfaSecret = null;
        nguoiDung.MfaBuocDaDung = null;
        nguoiDung.MfaMaKhoiPhuc = null;
        nguoiDung.MfaNgayBat = null;
    }

    /// <summary>
    /// Doi mot ma khoi phuc. Ma dung duoc DUNG MOT LAN: khop thi bi go khoi danh sach ngay.
    /// Ho tro ca dinh dang Argon2id moi ("salt:hash") va SHA-256 hex cu.
    /// </summary>
    private bool DungMaKhoiPhuc(NguoiDung nguoiDung, string? ma)
    {
        if (string.IsNullOrWhiteSpace(ma)
            || !ma.Contains('-', StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(nguoiDung.MfaMaKhoiPhuc))
        {
            return false;
        }

        var chuanHoa = ChuanHoaMaKhoiPhuc(ma);

        List<string>? danhSach;
        try
        {
            danhSach = JsonSerializer.Deserialize<List<string>>(nguoiDung.MfaMaKhoiPhuc);
        }
        catch (JsonException)
        {
            return false;
        }

        if (danhSach is null)
        {
            return false;
        }

        for (var i = 0; i < danhSach.Count; i++)
        {
            if (KhopMaKhoiPhuc(chuanHoa, danhSach[i]))
            {
                danhSach.RemoveAt(i);
                nguoiDung.MfaMaKhoiPhuc = JsonSerializer.Serialize(danhSach);
                return true;
            }
        }

        return false;
    }

    private bool KhopMaKhoiPhuc(string maChuanHoa, string bamDaLuu)
    {
        var viTriDauHai = bamDaLuu.IndexOf(':');
        if (viTriDauHai > 0)
        {
            var salt = bamDaLuu[..viTriDauHai];
            var hash = bamDaLuu[(viTriDauHai + 1)..];
            return _matKhau.KiemTra(maChuanHoa, hash, salt);
        }

        // Legacy SHA-256 hex — ton tai tu phien ban cu, van ho tro de khong khoa nguoi dung
        var bamSha = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(maChuanHoa));
        var bamLuu = Convert.FromHexString(bamDaLuu);
        return CryptographicOperations.FixedTimeEquals(bamLuu, bamSha);
    }

    private static int DemMaKhoiPhucConLai(NguoiDung nguoiDung)
    {
        if (string.IsNullOrWhiteSpace(nguoiDung.MfaMaKhoiPhuc))
        {
            return 0;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(nguoiDung.MfaMaKhoiPhuc)?.Count ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    /// <summary>Ma khoi phuc dang <c>xxxx-xxxx</c>, chi dung chu so va chu cai de khong nham lan.</summary>
    private static List<string> TaoMaKhoiPhuc()
    {
        const string bang = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        var ket = new List<string>(SoMaKhoiPhuc);

        for (var i = 0; i < SoMaKhoiPhuc; i++)
        {
            var ky = new char[8];
            for (var j = 0; j < ky.Length; j++)
            {
                ky[j] = bang[RandomNumberGenerator.GetInt32(bang.Length)];
            }

            ket.Add($"{new string(ky, 0, 4)}-{new string(ky, 4, 4)}");
        }

        return ket;
    }

    private static string ChuanHoaMaKhoiPhuc(string ma)
        => ma.Trim().ToUpperInvariant().Replace(" ", string.Empty);

    /// <summary>
    /// Bam ma khoi phuc bang Argon2id co salt rieng. Tra ve "salt:hash" (Base64).
    /// Phien ban cu dung SHA-256 khong salt — van duoc nhan dien va ho tro trong KhopMaKhoiPhuc.
    /// </summary>
    private string BamMaKhoiPhuc(string ma)
    {
        var (hash, salt) = _matKhau.BamMatKhau(ChuanHoaMaKhoiPhuc(ma));
        return $"{salt}:{hash}";
    }
}
