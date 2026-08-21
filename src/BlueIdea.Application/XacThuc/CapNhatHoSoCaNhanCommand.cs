using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XacThuc;

/// <summary>
/// Chuc nang 21, 43 - Nguoi dung tu cap nhat thong tin ca nhan cua chinh minh.
///
/// CHI cho sua nhung truong "ai cung tu biet ro nhat ve minh": ho ten, ngay sinh, gioi tinh,
/// email, dien thoai, chuc vu, anh dai dien. Don vi, vai tro, trang thai tai khoan va ten dang
/// nhap KHONG nam o day — do la quyet dinh cua to chuc, de nguoi dung tu doi la mo duong cho
/// leo thang dac quyen (tu chuyen minh sang don vi khac de xem ho so don vi do).
/// </summary>
public sealed record CapNhatHoSoCaNhanCommand(
    string HoTen,
    DateOnly? NgaySinh,
    string? GioiTinh,
    string? Email,
    string? DienThoai,
    string? ChucVu,
    Guid? AnhDaiDienId) : IRequest<Unit>, ICoGhiNhatKy
{
    public string HanhDongNhatKy => "CAP_NHAT_HO_SO_CA_NHAN";

    public string ModuleNhatKy => "XAC_THUC";
}

public sealed class CapNhatHoSoCaNhanCommandValidator : AbstractValidator<CapNhatHoSoCaNhanCommand>
{
    public CapNhatHoSoCaNhanCommandValidator()
    {
        RuleFor(x => x.HoTen)
            .NotEmpty().WithMessage("Vui lòng nhập họ và tên")
            .MaximumLength(200).WithMessage("Họ và tên tối đa 200 ký tự");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không hợp lệ")
            .MaximumLength(200).WithMessage("Email tối đa 200 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.DienThoai)
            .Matches(@"^[0-9+()\s.-]{6,20}$").WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrWhiteSpace(x.DienThoai));

        RuleFor(x => x.ChucVu)
            .MaximumLength(200).WithMessage("Chức vụ tối đa 200 ký tự");

        RuleFor(x => x.GioiTinh)
            .Must(x => x is "NAM" or "NU" or "KHAC")
            .WithMessage("Giới tính chỉ nhận NAM, NU hoặc KHAC")
            .When(x => !string.IsNullOrWhiteSpace(x.GioiTinh));
    }
}

public sealed class CapNhatHoSoCaNhanCommandHandler
    : IRequestHandler<CapNhatHoSoCaNhanCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDungHienTai;

    public CapNhatHoSoCaNhanCommandHandler(IAppDbContext db, INguoiDungHienTai nguoiDungHienTai)
    {
        _db = db;
        _nguoiDungHienTai = nguoiDungHienTai;
    }

    public async Task<Unit> Handle(CapNhatHoSoCaNhanCommand request, CancellationToken ct)
    {
        if (_nguoiDungHienTai.Id is null)
        {
            throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");
        }

        var nguoiDung = await _db.NguoiDung
            .FirstOrDefaultAsync(x => x.Id == _nguoiDungHienTai.Id.Value, ct)
            .ConfigureAwait(false)
            ?? throw new KhongTimThayException("người dùng", _nguoiDungHienTai.Id);

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        if (email is not null)
        {
            var trungEmail = await _db.NguoiDung.AsNoTracking()
                .AnyAsync(x => x.Id != nguoiDung.Id && x.Email == email, ct)
                .ConfigureAwait(false);

            if (trungEmail)
            {
                // Email la kenh nhan OTP quen mat khau — hai tai khoan cung email la mot duong
                // nham lan nguy hiem, khong chi la trung du lieu.
                throw new NghiepVuException(
                    MaLoiHeThong.TrungMa, "Email này đã được một tài khoản khác sử dụng.");
            }
        }

        if (request.AnhDaiDienId.HasValue)
        {
            var tonTai = await _db.TepTin.AsNoTracking()
                .AnyAsync(x => x.Id == request.AnhDaiDienId.Value, ct)
                .ConfigureAwait(false);

            if (!tonTai)
            {
                throw new KhongTimThayException("tệp ảnh đại diện", request.AnhDaiDienId.Value);
            }
        }

        nguoiDung.HoTen = request.HoTen.Trim();
        nguoiDung.HoTenKhongDau = VanBanTiengViet.TaoKhongDau(nguoiDung.HoTen);
        nguoiDung.NgaySinh = request.NgaySinh;
        nguoiDung.GioiTinh = string.IsNullOrWhiteSpace(request.GioiTinh) ? null : request.GioiTinh;
        nguoiDung.Email = email;
        nguoiDung.DienThoai = string.IsNullOrWhiteSpace(request.DienThoai)
            ? null
            : request.DienThoai.Trim();
        nguoiDung.ChucVu = string.IsNullOrWhiteSpace(request.ChucVu) ? null : request.ChucVu.Trim();
        nguoiDung.AnhDaiDienId = request.AnhDaiDienId;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Unit.Value;
    }
}
