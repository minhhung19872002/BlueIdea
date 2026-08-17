using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XacThuc;

/// <summary>Chuc nang 21 - Dang nhap noi bo bang tai khoan/mat khau.</summary>
public sealed record DangNhapCommand(string TenDangNhap, string MatKhau, string? MaMfa = null)
    : IRequest<KetQuaDangNhap>;

public sealed class DangNhapCommandValidator : AbstractValidator<DangNhapCommand>
{
    public DangNhapCommandValidator()
    {
        RuleFor(x => x.TenDangNhap)
            .NotEmpty().WithMessage("Vui lòng nhập tên đăng nhập")
            .MaximumLength(100).WithMessage("Tên đăng nhập tối đa 100 ký tự");

        RuleFor(x => x.MatKhau)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu");
    }
}

public sealed class DangNhapCommandHandler : IRequestHandler<DangNhapCommand, KetQuaDangNhap>
{
    /// <summary>So lan dang nhap sai toi da truoc khi khoa tam (Muc 5 - chuc nang 21).</summary>
    private const int SoLanSaiToiDaMacDinh = 5;

    private const int SoPhutKhoaMacDinh = 15;

    private readonly IAppDbContext _db;
    private readonly IDichVuMatKhau _matKhau;
    private readonly IDichVuToken _token;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDungHienTai;
    private readonly IDichVuCauHinh _cauHinh;

    public DangNhapCommandHandler(
        IAppDbContext db,
        IDichVuMatKhau matKhau,
        IDichVuToken token,
        IDongHoHeThong dongHo,
        INguoiDungHienTai nguoiDungHienTai,
        IDichVuCauHinh cauHinh)
    {
        _db = db;
        _matKhau = matKhau;
        _token = token;
        _dongHo = dongHo;
        _nguoiDungHienTai = nguoiDungHienTai;
        _cauHinh = cauHinh;
    }

    public async Task<KetQuaDangNhap> Handle(DangNhapCommand request, CancellationToken ct)
    {
        var bayGio = _dongHo.BayGio;
        var tenDangNhap = request.TenDangNhap.Trim().ToLowerInvariant();

        var nguoiDung = await _db.NguoiDung
            .FirstOrDefaultAsync(x => x.TenDangNhap == tenDangNhap, ct)
            .ConfigureAwait(false);

        if (nguoiDung is null)
        {
            // Ghi nhat ky nhung KHONG tiet lo tai khoan co ton tai hay khong.
            await GhiNhatKyDangNhapAsync(tenDangNhap, null, false, "Tài khoản không tồn tại", ct)
                .ConfigureAwait(false);
            throw new NghiepVuException(MaLoiHeThong.SaiTaiKhoanMatKhau,
                "Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        if (nguoiDung.DangBiKhoaTam(bayGio))
        {
            var conLai = (int)Math.Ceiling((nguoiDung.KhoaDen!.Value - bayGio).TotalMinutes);
            await GhiNhatKyDangNhapAsync(tenDangNhap, nguoiDung.Id, false, "Tài khoản đang bị khóa tạm", ct)
                .ConfigureAwait(false);
            throw new NghiepVuException(MaLoiHeThong.TaiKhoanBiKhoa,
                $"Tài khoản đang bị khóa, vui lòng thử lại sau {conLai} phút.");
        }

        if (nguoiDung.TrangThaiTaiKhoan == TrangThaiNguoiDung.Khoa)
        {
            await GhiNhatKyDangNhapAsync(tenDangNhap, nguoiDung.Id, false, "Tài khoản bị khóa", ct)
                .ConfigureAwait(false);
            throw new NghiepVuException(MaLoiHeThong.TaiKhoanBiKhoa,
                "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        }

        if (nguoiDung.TrangThaiTaiKhoan == TrangThaiNguoiDung.ChoKichHoat)
        {
            await GhiNhatKyDangNhapAsync(tenDangNhap, nguoiDung.Id, false, "Tài khoản chưa kích hoạt", ct)
                .ConfigureAwait(false);
            throw new NghiepVuException(MaLoiHeThong.TaiKhoanChuaKichHoat,
                "Tài khoản chưa được kích hoạt.");
        }

        var dungMatKhau = !string.IsNullOrEmpty(nguoiDung.MatKhauHash)
                          && _matKhau.KiemTra(request.MatKhau, nguoiDung.MatKhauHash,
                              nguoiDung.MatKhauSalt ?? string.Empty);

        if (!dungMatKhau)
        {
            await XuLyDangNhapSaiAsync(nguoiDung, bayGio, ct).ConfigureAwait(false);
            throw new NghiepVuException(MaLoiHeThong.SaiTaiKhoanMatKhau,
                "Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        // Dat lai bo dem sai sau khi dang nhap thanh cong.
        nguoiDung.SoLanDangNhapSai = 0;
        nguoiDung.KhoaDen = null;
        nguoiDung.LanDangNhapCuoi = bayGio;

        var (vaiTro, quyen) = await LayVaiTroVaQuyenAsync(nguoiDung.Id, ct).ConfigureAwait(false);

        var accessToken = _token.TaoAccessToken(nguoiDung, vaiTro, quyen);
        var (refreshToken, hash) = _token.TaoRefreshToken();

        _db.RefreshToken.Add(new RefreshToken
        {
            NguoiDungId = nguoiDung.Id,
            TokenHash = hash,
            HetHan = bayGio.AddDays(_token.SoNgayHetHanRefreshToken),
            DiaChiIp = _nguoiDungHienTai.DiaChiIp,
            UserAgent = _nguoiDungHienTai.UserAgent
        });

        await GhiNhatKyDangNhapAsync(tenDangNhap, nguoiDung.Id, true, null, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var tenDonVi = nguoiDung.DonViId.HasValue
            ? await _db.DonVi.Where(x => x.Id == nguoiDung.DonViId.Value)
                .Select(x => x.Ten).FirstOrDefaultAsync(ct).ConfigureAwait(false)
            : null;

        return new KetQuaDangNhap(
            accessToken,
            refreshToken,
            _token.SoGiayHetHanAccessToken,
            new ThongTinNguoiDungDto(
                nguoiDung.Id, nguoiDung.TenDangNhap, nguoiDung.HoTen, nguoiDung.Email,
                nguoiDung.ChucVu, nguoiDung.DonViId, tenDonVi,
                vaiTro.ToList(), quyen.ToList(), nguoiDung.MfaEnabled),
            nguoiDung.BuocDoiMatKhau);
    }

    private async Task XuLyDangNhapSaiAsync(NguoiDung nguoiDung, DateTimeOffset bayGio, CancellationToken ct)
    {
        var soLanToiDa = await _cauHinh
            .LayAsync(KhoaCauHinh.SoLanDangNhapSaiToiDa, SoLanSaiToiDaMacDinh, ct)
            .ConfigureAwait(false);

        var soPhutKhoa = await _cauHinh
            .LayAsync(KhoaCauHinh.ThoiGianKhoaTaiKhoanPhut, SoPhutKhoaMacDinh, ct)
            .ConfigureAwait(false);

        nguoiDung.SoLanDangNhapSai++;
        if (nguoiDung.SoLanDangNhapSai >= soLanToiDa)
        {
            nguoiDung.KhoaDen = bayGio.AddMinutes(soPhutKhoa);
            nguoiDung.SoLanDangNhapSai = 0;
        }

        await GhiNhatKyDangNhapAsync(nguoiDung.TenDangNhap, nguoiDung.Id, false, "Sai mật khẩu", ct)
            .ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<(IReadOnlyCollection<string> VaiTro, IReadOnlyCollection<string> Quyen)>
        LayVaiTroVaQuyenAsync(Guid nguoiDungId, CancellationToken ct)
    {
        var homNay = _dongHo.HomNay;

        var vaiTroIds = await _db.NguoiDungVaiTro.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDungId
                        && (x.TuNgay == null || x.TuNgay <= homNay)
                        && (x.DenNgay == null || x.DenNgay >= homNay))
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
            .Join(_db.Quyen.AsNoTracking(), vq => vq.QuyenId, q => q.Id, (vq, q) => q.Ma)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (vaiTro, quyen);
    }

    private Task GhiNhatKyDangNhapAsync(
        string tenDangNhap, Guid? nguoiDungId, bool thanhCong, string? lyDo, CancellationToken ct)
    {
        _db.NhatKyDangNhap.Add(new NhatKyDangNhap
        {
            TenDangNhap = tenDangNhap,
            NguoiDungId = nguoiDungId,
            ThanhCong = thanhCong,
            LyDoThatBai = lyDo,
            DiaChiIp = _nguoiDungHienTai.DiaChiIp,
            UserAgent = _nguoiDungHienTai.UserAgent,
            ThoiGian = _dongHo.BayGio
        });

        _ = ct;
        return Task.CompletedTask;
    }
}
