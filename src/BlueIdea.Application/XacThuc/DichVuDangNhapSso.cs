using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Application.XacThuc;

/// <summary>Thong tin nguoi dung lay tu nha cung cap SSO sau khi doi ma lay token.</summary>
public sealed record ThongTinNguoiDungSso(
    string Subject,
    string? TenDangNhap,
    string? HoTen,
    string? Email,
    string? DienThoai,
    string? MaDonVi);

/// <summary>
/// Hop dong noi chuyen voi nha cung cap OIDC. Tach ra de kiem thu duoc toan bo phan nghiep vu
/// (khop tai khoan, tao moi, cap token noi bo) ma khong can nha cung cap that.
/// </summary>
public interface IBoXacThucOidc
{
    /// <summary>Dia chi de chuyen huong trinh duyet sang trang dang nhap cua nha cung cap.</summary>
    string TaoDiaChiDangNhap(string state, string codeChallenge, string duongDanTraVe);

    /// <summary>Doi authorization code lay token roi doc thong tin nguoi dung.</summary>
    Task<ThongTinNguoiDungSso> DoiMaLayThongTinAsync(
        string code, string codeVerifier, string duongDanTraVe, CancellationToken ct = default);

    /// <summary>
    /// Dia chi ket thuc phien ben nha cung cap (single logout).
    ///
    /// Tra <c>null</c> khi nha cung cap KHONG cong bo <c>end_session_endpoint</c>: dac ta OIDC
    /// coi day la tuy chon, khong phai nha cung cap nao cung ho tro. Tra null de ben goi biet
    /// ma chi dang xuat cuc bo, thay vi dung mot dia chi doan mo va day nguoi dung sang trang 404.
    /// </summary>
    Task<string?> TaoDiaChiDangXuatAsync(
        string? idToken, string duongDanTraVe, CancellationToken ct = default);

    bool DaCauHinh { get; }
}

/// <summary>
/// Chuc nang 21, 41 - Dang nhap mot lan (SSO) qua OpenID Connect.
///
/// Nguyen tac khop tai khoan: khop theo SUBJECT cua nha cung cap truoc, roi moi den email.
/// Subject la dinh danh khong doi; email thi doi duoc va co the tai su dung cho nguoi khac —
/// khop email truoc se dan den chiem tai khoan khi mot dia chi cu duoc cap lai cho nhan su moi.
/// </summary>
public sealed class DichVuDangNhapSso
{
    private readonly IAppDbContext _db;
    private readonly IBoXacThucOidc _oidc;
    private readonly IDichVuToken _token;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDungHienTai;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly ILogger<DichVuDangNhapSso> _logger;

    public DichVuDangNhapSso(
        IAppDbContext db, IBoXacThucOidc oidc, IDichVuToken token, IDongHoHeThong dongHo,
        INguoiDungHienTai nguoiDungHienTai, IDichVuCauHinh cauHinh,
        ILogger<DichVuDangNhapSso> logger)
    {
        _db = db;
        _oidc = oidc;
        _token = token;
        _dongHo = dongHo;
        _nguoiDungHienTai = nguoiDungHienTai;
        _cauHinh = cauHinh;
        _logger = logger;
    }

    public bool DaCauHinh => _oidc.DaCauHinh;

    public string TaoDiaChiDangNhap(string state, string codeChallenge, string duongDanTraVe)
    {
        BatBuocDaCauHinh();
        return _oidc.TaoDiaChiDangNhap(state, codeChallenge, duongDanTraVe);
    }

    /// <summary>
    /// Chuc nang 41 — Dia chi ket thuc phien ben nha cung cap (single logout).
    ///
    /// KHONG goi <c>BatBuocDaCauHinh</c>: dang xuat phai chay duoc ke ca khi SSO da bi tat sau
    /// khi nguoi dung dang nhap. Nem loi o day thi ho ket o trang dang xuat khong thoat ra duoc.
    /// </summary>
    public async Task<string?> TaoDiaChiDangXuatAsync(
        string? idToken, string duongDanTraVe, CancellationToken ct = default)
        => _oidc.DaCauHinh
            ? await _oidc.TaoDiaChiDangXuatAsync(idToken, duongDanTraVe, ct).ConfigureAwait(false)
            : null;

    /// <summary>Doi authorization code lay tai khoan noi bo va cap token cua he thong.</summary>
    public async Task<KetQuaDangNhap> XuLyTraVeAsync(
        string code, string codeVerifier, string duongDanTraVe, CancellationToken ct = default)
    {
        BatBuocDaCauHinh();

        var thongTin = await _oidc
            .DoiMaLayThongTinAsync(code, codeVerifier, duongDanTraVe, ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(thongTin.Subject))
        {
            throw new NghiepVuException(MaLoiHeThong.TokenKhongHopLe,
                "Nhà cung cấp SSO không trả về định danh người dùng.");
        }

        var nguoiDung = await TimHoacTaoAsync(thongTin, ct).ConfigureAwait(false);

        if (nguoiDung.TrangThaiTaiKhoan == TrangThaiNguoiDung.Khoa)
        {
            throw new NghiepVuException(MaLoiHeThong.TaiKhoanBiKhoa,
                "Tài khoản đã bị khoá trên hệ thống sáng kiến.");
        }

        var bayGio = _dongHo.BayGio;

        nguoiDung.LanDangNhapCuoi = bayGio;
        nguoiDung.SoLanDangNhapSai = 0;
        nguoiDung.KhoaDen = null;

        var (vaiTro, quyen) = await LayVaiTroVaQuyenAsync(nguoiDung.Id, ct).ConfigureAwait(false);

        var accessToken = _token.TaoAccessToken(nguoiDung, vaiTro, quyen);
        var (refreshToken, hash) = _token.TaoRefreshToken();

        _db.RefreshToken.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            NguoiDungId = nguoiDung.Id,
            TokenHash = hash,
            HetHan = bayGio.AddDays(_token.SoNgayHetHanRefreshToken),
            DiaChiIp = _nguoiDungHienTai.DiaChiIp,
            UserAgent = _nguoiDungHienTai.UserAgent
        });

        _db.NhatKyDangNhap.Add(new NhatKyDangNhap
        {
            Id = Guid.NewGuid(),
            NguoiDungId = nguoiDung.Id,
            TenDangNhap = nguoiDung.TenDangNhap,
            ThanhCong = true,
            DiaChiIp = _nguoiDungHienTai.DiaChiIp,
            UserAgent = _nguoiDungHienTai.UserAgent,
            ThietBi = "SSO",
            ThoiGian = bayGio
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var tenDonVi = nguoiDung.DonViId.HasValue
            ? await _db.DonVi.AsNoTracking()
                .Where(x => x.Id == nguoiDung.DonViId.Value)
                .Select(x => x.Ten)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false)
            : null;

        return new KetQuaDangNhap(
            accessToken,
            refreshToken,
            _token.SoGiayHetHanAccessToken,
            new ThongTinNguoiDungDto(
                nguoiDung.Id, nguoiDung.TenDangNhap, nguoiDung.HoTen, nguoiDung.Email,
                nguoiDung.ChucVu, nguoiDung.DonViId, tenDonVi,
                vaiTro.ToList(), quyen.ToList(), nguoiDung.MfaEnabled),

            // Tài khoản SSO không có mật khẩu nội bộ nên không bao giờ phải đổi mật khẩu.
            BuocDoiMatKhau: false);
    }

    private async Task<(List<string> VaiTro, List<string> Quyen)> LayVaiTroVaQuyenAsync(
        Guid nguoiDungId, CancellationToken ct)
    {
        var vaiTroIds = await _db.NguoiDungVaiTro.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDungId)
            .Select(x => x.VaiTroId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var vaiTro = await _db.VaiTro.AsNoTracking()
            .Where(x => vaiTroIds.Contains(x.Id) && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .Select(x => x.Ma)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var quyen = await _db.VaiTroQuyen.AsNoTracking()
            .Where(x => vaiTroIds.Contains(x.VaiTroId))
            .Join(_db.Quyen.AsNoTracking(), x => x.QuyenId, q => q.Id, (_, q) => q.Ma)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (vaiTro, quyen);
    }

    // -----------------------------------------------------------------------------------

    private async Task<NguoiDung> TimHoacTaoAsync(ThongTinNguoiDungSso tt, CancellationToken ct)
    {
        // 1. Khớp theo subject — định danh bền vững nhất.
        var nguoiDung = await _db.NguoiDung
            .FirstOrDefaultAsync(x => x.SsoSubjectId == tt.Subject, ct)
            .ConfigureAwait(false);

        if (nguoiDung is not null)
        {
            return nguoiDung;
        }

        // 2. Chưa từng đăng nhập SSO: khớp tài khoản nội bộ sẵn có theo tên đăng nhập rồi
        //    mới đến email, và gắn subject để lần sau khớp thẳng ở bước 1.
        if (!string.IsNullOrWhiteSpace(tt.TenDangNhap))
        {
            var ten = tt.TenDangNhap.Trim().ToLowerInvariant();

            nguoiDung = await _db.NguoiDung
                .FirstOrDefaultAsync(x => x.TenDangNhap == ten, ct)
                .ConfigureAwait(false);
        }

        if (nguoiDung is null && !string.IsNullOrWhiteSpace(tt.Email))
        {
            var email = tt.Email.Trim();

            nguoiDung = await _db.NguoiDung
                .FirstOrDefaultAsync(x => x.Email == email, ct)
                .ConfigureAwait(false);
        }

        if (nguoiDung is not null)
        {
            nguoiDung.SsoSubjectId = tt.Subject;
            nguoiDung.SsoProvider = "OIDC";

            _logger.LogInformation(
                "Đã gắn SSO subject cho tài khoản sẵn có {TenDangNhap}.", nguoiDung.TenDangNhap);

            return nguoiDung;
        }

        // 3. Chưa có tài khoản nào: chỉ tạo mới khi quản trị viên bật cấp phát tự động.
        //    Mặc định TẮT — cơ quan nhà nước thường muốn kiểm soát ai được vào hệ thống,
        //    tự tạo tài khoản cho mọi người đăng nhập SSO là mở cửa quá rộng.
        var choTaoTuDong = await _cauHinh
            .LayAsync(KhoaCauHinh.SsoTuDongTaoTaiKhoan, false, ct)
            .ConfigureAwait(false);

        if (!choTaoTuDong)
        {
            throw new NghiepVuException(MaLoiHeThong.TaiKhoanChuaKichHoat,
                "Tài khoản SSO của bạn chưa được cấp quyền sử dụng hệ thống sáng kiến. "
                + "Vui lòng liên hệ quản trị viên.");
        }

        return await TaoTaiKhoanMoiAsync(tt, ct).ConfigureAwait(false);
    }

    private async Task<NguoiDung> TaoTaiKhoanMoiAsync(ThongTinNguoiDungSso tt, CancellationToken ct)
    {
        var vaiTroMacDinh = await _db.VaiTro.AsNoTracking()
            .Where(x => x.Ma == MaVaiTro.TacGia)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (vaiTroMacDinh is null)
        {
            throw new NghiepVuException(MaLoiHeThong.LoiHeThong,
                "Chưa cấu hình vai trò mặc định cho tài khoản SSO.");
        }

        var donViId = string.IsNullOrWhiteSpace(tt.MaDonVi)
            ? null
            : await _db.DonVi.AsNoTracking()
                .Where(x => x.Ma == tt.MaDonVi)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

        var tenDangNhap = await TaoTenDangNhapKhongTrungAsync(tt, ct).ConfigureAwait(false);
        var hoTen = string.IsNullOrWhiteSpace(tt.HoTen) ? tenDangNhap : tt.HoTen.Trim();

        var nguoiDung = new NguoiDung
        {
            Id = Guid.NewGuid(),
            TenDangNhap = tenDangNhap,
            HoTen = hoTen,
            HoTenKhongDau = VanBanTiengViet.TaoKhongDau(hoTen),
            Email = tt.Email?.Trim(),
            DienThoai = tt.DienThoai?.Trim(),
            DonViId = donViId,
            LoaiTaiKhoan = "SSO",
            SsoSubjectId = tt.Subject,
            SsoProvider = "OIDC",
            TrangThaiTaiKhoan = TrangThaiNguoiDung.HoatDong,

            // Tài khoản SSO KHÔNG có mật khẩu nội bộ: để trống thay vì đặt mật khẩu ngẫu nhiên,
            // như vậy không tồn tại đường đăng nhập thứ hai nằm ngoài tầm kiểm soát của SSO.
            MatKhauHash = null,
            MatKhauSalt = null,
            BuocDoiMatKhau = false
        };

        _db.NguoiDung.Add(nguoiDung);

        _db.NguoiDungVaiTro.Add(new NguoiDungVaiTro
        {
            Id = Guid.NewGuid(),
            NguoiDungId = nguoiDung.Id,
            VaiTroId = vaiTroMacDinh.Value,
            DonViId = donViId
        });

        _logger.LogInformation("Đã tạo tài khoản SSO mới: {TenDangNhap}", tenDangNhap);

        return nguoiDung;
    }

    private async Task<string> TaoTenDangNhapKhongTrungAsync(
        ThongTinNguoiDungSso tt, CancellationToken ct)
    {
        var goc = tt.TenDangNhap
                  ?? tt.Email?.Split('@')[0]
                  ?? $"sso.{tt.Subject}";

        goc = new string(goc.Trim().ToLowerInvariant()
            .Select(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '.' or '_' or '-'
                ? c
                : '.')
            .ToArray()).Trim('.');

        if (string.IsNullOrWhiteSpace(goc))
        {
            goc = "sso.nguoi.dung";
        }

        var ten = goc;
        var lan = 1;

        while (await _db.NguoiDung.AsNoTracking().AnyAsync(x => x.TenDangNhap == ten, ct)
                   .ConfigureAwait(false))
        {
            ten = $"{goc}{++lan}";
        }

        return ten;
    }

    private void BatBuocDaCauHinh()
    {
        if (!_oidc.DaCauHinh)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Hệ thống chưa cấu hình đăng nhập SSO.");
        }
    }
}
