using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XacThuc;

/// <summary>Lam moi access token bang refresh token (co xoay vong token).</summary>
public sealed record LamMoiTokenCommand(string RefreshToken) : IRequest<KetQuaDangNhap>;

public sealed class LamMoiTokenCommandHandler : IRequestHandler<LamMoiTokenCommand, KetQuaDangNhap>
{
    private readonly IAppDbContext _db;
    private readonly IDichVuToken _token;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDungHienTai;

    public LamMoiTokenCommandHandler(
        IAppDbContext db, IDichVuToken token, IDongHoHeThong dongHo, INguoiDungHienTai nguoiDungHienTai)
    {
        _db = db;
        _token = token;
        _dongHo = dongHo;
        _nguoiDungHienTai = nguoiDungHienTai;
    }

    public async Task<KetQuaDangNhap> Handle(LamMoiTokenCommand request, CancellationToken ct)
    {
        var bayGio = _dongHo.BayGio;
        var hash = _token.BamRefreshToken(request.RefreshToken);

        var banGhi = await _db.RefreshToken
            .FirstOrDefaultAsync(x => x.TokenHash == hash, ct)
            .ConfigureAwait(false);

        if (banGhi is null)
        {
            throw new NghiepVuException(MaLoiHeThong.TokenKhongHopLe, "Refresh token không hợp lệ.");
        }

        if (!banGhi.ConHieuLuc(bayGio))
        {
            // Token da bi thu hoi ma van duoc dung lai -> nghi ngo lo token, thu hoi ca chuoi.
            if (banGhi.ThoiGianThuHoi is not null)
            {
                await ThuHoiToanBoAsync(banGhi.NguoiDungId, bayGio, ct).ConfigureAwait(false);
            }

            throw new NghiepVuException(MaLoiHeThong.TokenHetHan,
                "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.");
        }

        var nguoiDung = await _db.NguoiDung
            .FirstOrDefaultAsync(x => x.Id == banGhi.NguoiDungId, ct)
            .ConfigureAwait(false);

        if (nguoiDung is null || !nguoiDung.ChoPhepDangNhap(bayGio))
        {
            throw new NghiepVuException(MaLoiHeThong.TaiKhoanBiKhoa, "Tài khoản không còn hiệu lực.");
        }

        var homNay = _dongHo.HomNay;
        var vaiTroIds = await _db.NguoiDungVaiTro.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDung.Id
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

        var accessToken = _token.TaoAccessToken(nguoiDung, vaiTro, quyen);
        var (tokenMoi, hashMoi) = _token.TaoRefreshToken();

        // Xoay vong: thu hoi token cu, phat token moi.
        banGhi.ThoiGianThuHoi = bayGio;
        banGhi.ThayTheBoiTokenHash = hashMoi;

        _db.RefreshToken.Add(new RefreshToken
        {
            NguoiDungId = nguoiDung.Id,
            TokenHash = hashMoi,
            HetHan = bayGio.AddDays(_token.SoNgayHetHanRefreshToken),
            DiaChiIp = _nguoiDungHienTai.DiaChiIp,
            UserAgent = _nguoiDungHienTai.UserAgent
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var tenDonVi = nguoiDung.DonViId.HasValue
            ? await _db.DonVi.Where(x => x.Id == nguoiDung.DonViId.Value)
                .Select(x => x.Ten).FirstOrDefaultAsync(ct).ConfigureAwait(false)
            : null;

        return new KetQuaDangNhap(
            accessToken, tokenMoi, _token.SoGiayHetHanAccessToken,
            new ThongTinNguoiDungDto(
                nguoiDung.Id, nguoiDung.TenDangNhap, nguoiDung.HoTen, nguoiDung.Email,
                nguoiDung.ChucVu, nguoiDung.DonViId, tenDonVi, vaiTro, quyen, nguoiDung.MfaEnabled),
            nguoiDung.BuocDoiMatKhau);
    }

    private async Task ThuHoiToanBoAsync(Guid nguoiDungId, DateTimeOffset bayGio, CancellationToken ct)
    {
        var conHieuLuc = await _db.RefreshToken
            .Where(x => x.NguoiDungId == nguoiDungId && x.ThoiGianThuHoi == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var t in conHieuLuc)
        {
            t.ThoiGianThuHoi = bayGio;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Dang xuat - thu hoi refresh token hien tai.</summary>
public sealed record DangXuatCommand(string? RefreshToken) : IRequest<Unit>;

public sealed class DangXuatCommandHandler : IRequestHandler<DangXuatCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IDichVuToken _token;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDung;

    public DangXuatCommandHandler(
        IAppDbContext db, IDichVuToken token, IDongHoHeThong dongHo, INguoiDungHienTai nguoiDung)
    {
        _db = db;
        _token = token;
        _dongHo = dongHo;
        _nguoiDung = nguoiDung;
    }

    public async Task<Unit> Handle(DangXuatCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hash = _token.BamRefreshToken(request.RefreshToken);
            var banGhi = await _db.RefreshToken
                .FirstOrDefaultAsync(x => x.TokenHash == hash && x.ThoiGianThuHoi == null, ct)
                .ConfigureAwait(false);

            if (banGhi is not null)
            {
                banGhi.ThoiGianThuHoi = _dongHo.BayGio;
            }
        }
        else if (_nguoiDung.Id.HasValue)
        {
            var tatCa = await _db.RefreshToken
                .Where(x => x.NguoiDungId == _nguoiDung.Id.Value && x.ThoiGianThuHoi == null)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var t in tatCa)
            {
                t.ThoiGianThuHoi = _dongHo.BayGio;
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}

/// <summary>Chuc nang 21 - Doi mat khau (ap dung chinh sach mat khau cau hinh duoc).</summary>
public sealed record DoiMatKhauCommand(string MatKhauCu, string MatKhauMoi) : IRequest<Unit>, ICoGhiNhatKy
{
    public string HanhDongNhatKy => HanhDongAudit.DoiMatKhau;

    public string ModuleNhatKy => "XAC_THUC";
}

public sealed class DoiMatKhauCommandValidator : AbstractValidator<DoiMatKhauCommand>
{
    public DoiMatKhauCommandValidator()
    {
        RuleFor(x => x.MatKhauCu).NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại");
        RuleFor(x => x.MatKhauMoi).NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới");
        RuleFor(x => x.MatKhauMoi).NotEqual(x => x.MatKhauCu)
            .WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại");
    }
}

public sealed class DoiMatKhauCommandHandler : IRequestHandler<DoiMatKhauCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDungHienTai;
    private readonly IDichVuMatKhau _matKhau;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuCauHinh _cauHinh;

    public DoiMatKhauCommandHandler(
        IAppDbContext db, INguoiDungHienTai nguoiDungHienTai, IDichVuMatKhau matKhau,
        IDongHoHeThong dongHo, IDichVuCauHinh cauHinh)
    {
        _db = db;
        _nguoiDungHienTai = nguoiDungHienTai;
        _matKhau = matKhau;
        _dongHo = dongHo;
        _cauHinh = cauHinh;
    }

    public async Task<Unit> Handle(DoiMatKhauCommand request, CancellationToken ct)
    {
        if (_nguoiDungHienTai.Id is null)
        {
            throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");
        }

        var nguoiDung = await _db.NguoiDung
            .FirstOrDefaultAsync(x => x.Id == _nguoiDungHienTai.Id.Value, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("người dùng", _nguoiDungHienTai.Id);

        if (string.IsNullOrEmpty(nguoiDung.MatKhauHash)
            || !_matKhau.KiemTra(request.MatKhauCu, nguoiDung.MatKhauHash, nguoiDung.MatKhauSalt ?? string.Empty))
        {
            throw new NghiepVuException(MaLoiHeThong.SaiTaiKhoanMatKhau, "Mật khẩu hiện tại không đúng.");
        }

        var chinhSach = await LayChinhSachAsync(ct).ConfigureAwait(false);

        var loi = _matKhau.KiemTraChinhSach(request.MatKhauMoi, chinhSach);
        if (loi.Count > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.MatKhauKhongDatChinhSach, string.Join(" ", loi));
        }

        // Khong cho dung lai N mat khau gan nhat.
        var lichSu = await _db.LichSuMatKhau.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDung.Id)
            .OrderByDescending(x => x.ThoiGian)
            .Take(chinhSach.SoLanKhongTrung)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (lichSu.Any(x => _matKhau.KiemTra(request.MatKhauMoi, x.MatKhauHash, x.MatKhauSalt ?? string.Empty)))
        {
            throw new NghiepVuException(MaLoiHeThong.MatKhauDaSuDung,
                $"Mật khẩu mới không được trùng {chinhSach.SoLanKhongTrung} mật khẩu gần nhất.");
        }

        var (hash, salt) = _matKhau.BamMatKhau(request.MatKhauMoi);

        if (!string.IsNullOrEmpty(nguoiDung.MatKhauHash))
        {
            _db.LichSuMatKhau.Add(new LichSuMatKhau
            {
                NguoiDungId = nguoiDung.Id,
                MatKhauHash = nguoiDung.MatKhauHash,
                MatKhauSalt = nguoiDung.MatKhauSalt,
                ThoiGian = _dongHo.BayGio
            });
        }

        nguoiDung.MatKhauHash = hash;
        nguoiDung.MatKhauSalt = salt;
        nguoiDung.BuocDoiMatKhau = false;
        nguoiDung.NgayDoiMatKhauCuoi = _dongHo.BayGio;

        // Doi mat khau -> thu hoi toan bo phien dang mo.
        var token = await _db.RefreshToken
            .Where(x => x.NguoiDungId == nguoiDung.Id && x.ThoiGianThuHoi == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var t in token)
        {
            t.ThoiGianThuHoi = _dongHo.BayGio;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }

    private async Task<ChinhSachMatKhau> LayChinhSachAsync(CancellationToken ct) => new()
    {
        DoDaiToiThieu = await _cauHinh
            .LayAsync(KhoaCauHinh.ChinhSachMatKhauDoDaiToiThieu, 8, ct).ConfigureAwait(false),
        SoLanKhongTrung = await _cauHinh
            .LayAsync(KhoaCauHinh.ChinhSachMatKhauSoLanKhongTrung, 3, ct).ConfigureAwait(false),
        SoNgayHetHan = await _cauHinh
            .LayAsync(KhoaCauHinh.ChinhSachMatKhauSoNgayHetHan, 90, ct).ConfigureAwait(false)
    };
}
