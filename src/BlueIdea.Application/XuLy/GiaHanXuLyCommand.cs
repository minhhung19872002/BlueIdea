using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XuLy;

/// <summary>
/// Chuc nang 29–30 — Gia han xu ly cho buoc hien tai cua mot ho so.
///
/// Han xu ly von do may chay quy trinh dat khi ho so vao buoc, tinh theo so ngay khai trong quy
/// trinh. Thuc te hanh chinh thi luon co truong hop chinh dang phai keo dai: can bo nghi om, dot
/// cao diem, phai cho y kien don vi khac. Truoc day khong co loi nao ngoai hai loi xau: de ho so
/// qua han (bao do + nhac moi sang), hoac thu hoi buoc roi lam lai tu dau.
///
/// Gia han KHONG phai thay doi quy trinh: no chi doi moc thoi gian cua chinh luot xu ly dang mo,
/// co ly do bat buoc va co ghi nhat ky — de sau nay con doi chieu duoc vi sao ho so nay cham.
/// </summary>
public sealed record GiaHanXuLyCommand(Guid SangKienId, DateTimeOffset HanMoi, string LyDo)
    : IRequest<Unit>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.XuLyGiaHan;

    public Guid? DoiTuongId => SangKienId;

    public string HanhDongNhatKy => "GIA_HAN_XU_LY";

    public string ModuleNhatKy => "XU_LY";
}

public sealed class GiaHanXuLyValidator : AbstractValidator<GiaHanXuLyCommand>
{
    public GiaHanXuLyValidator()
    {
        RuleFor(x => x.SangKienId).NotEmpty();

        RuleFor(x => x.LyDo)
            .NotEmpty().WithMessage("Phải nêu lý do gia hạn.")
            .MaximumLength(1000);
    }
}

public sealed class GiaHanXuLyCommandHandler : IRequestHandler<GiaHanXuLyCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDichVuThongBao? _thongBao;

    public GiaHanXuLyCommandHandler(
        IAppDbContext db, IDongHoHeThong dongHo, INguoiDungHienTai nguoiDung,
        IDichVuThongBao? thongBao = null)
    {
        _db = db;
        _dongHo = dongHo;
        _nguoiDung = nguoiDung;
        _thongBao = thongBao;
    }

    public async Task<Unit> Handle(GiaHanXuLyCommand request, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.FirstOrDefaultAsync(x => x.Id == request.SangKienId, ct)
                       .ConfigureAwait(false)
                   ?? throw new KhongTimThayException("hồ sơ sáng kiến", request.SangKienId);

        if (hoSo.BuocHienTaiId is null)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                "Hồ sơ không nằm ở bước xử lý nào nên không có hạn để gia hạn.");
        }

        var bayGio = _dongHo.BayGio;

        if (request.HanMoi <= bayGio)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Hạn mới phải sau thời điểm hiện tại.");
        }

        // Chi cho keo DAI. Rut ngan han cua nguoi dang xu ly la mot viec khac han ve nghiep vu
        // (ep tien do), khong duoc nup duoi cai ten "gia han".
        if (hoSo.HanXuLyHienTai.HasValue && request.HanMoi <= hoSo.HanXuLyHienTai.Value)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Hạn mới phải muộn hơn hạn hiện tại "
                + $"({hoSo.HanXuLyHienTai.Value:dd/MM/yyyy HH:mm}).");
        }

        var hanCu = hoSo.HanXuLyHienTai;

        // Luot xu ly dang mo cua chinh buoc hien tai: doi ca o day, neu khong thi timeline va co
        // "qua han" van hien theo moc cu.
        var luotDangMo = await _db.SangKienXuLy
            // ThoiGianXuLy == null tuc la luot chua dong. Khong dung DaHoanThanh: do la thuoc
            // tinh TINH trong .NET, EF khong dich sang SQL duoc.
            .Where(x => x.SangKienId == hoSo.Id
                        && x.BuocId == hoSo.BuocHienTaiId.Value
                        && x.ThoiGianXuLy == null)
            .OrderByDescending(x => x.ThuTu)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        hoSo.HanXuLyHienTai = request.HanMoi;

        if (luotDangMo is not null)
        {
            luotDangMo.HanXuLy = request.HanMoi;
            luotDangMo.QuaHan = false;
        }

        _db.SangKienLichSu.Add(new SangKienLichSu
        {
            SangKienId = hoSo.Id,
            HanhDong = "GIA_HAN",
            TruongThayDoi = new List<string> { nameof(hoSo.HanXuLyHienTai) },
            GiaTriTruoc = new Dictionary<string, string?>
            {
                ["hanXuLy"] = hanCu?.ToString("O")
            },
            GiaTriSau = new Dictionary<string, string?>
            {
                ["hanXuLy"] = request.HanMoi.ToString("O"),
                ["lyDo"] = request.LyDo.Trim()
            },
            NguoiThucHienId = _nguoiDung.Id,
            ThoiGian = bayGio,
            DiaChiIp = _nguoiDung.DiaChiIp
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await BaoNguoiDangXuLyAsync(hoSo, luotDangMo?.NguoiXuLyId, request, ct)
            .ConfigureAwait(false);

        return Unit.Value;
    }

    /// <summary>
    /// Bao cho nguoi dang giu buoc biet han da doi.
    ///
    /// Nuot loi co y: han da ghi vao CSDL roi, mot lan gui thong bao hong khong duoc phep lam
    /// hong ca thao tac gia han.
    /// </summary>
    private async Task BaoNguoiDangXuLyAsync(
        HoSoSangKien hoSo, Guid? nguoiXuLyId, GiaHanXuLyCommand request, CancellationToken ct)
    {
        if (_thongBao is null || nguoiXuLyId is not { } nguoiNhan || nguoiNhan == _nguoiDung.Id)
        {
            return;
        }

        try
        {
            await _thongBao.GuiTrongUngDungAsync(
                nguoiNhan,
                $"Hồ sơ {hoSo.MaHoSo} được gia hạn xử lý",
                $"Hạn mới: {request.HanMoi:dd/MM/yyyy HH:mm}. Lý do: {request.LyDo.Trim()}",
                DuongDanGiaoDien.ChiTietHoSo(hoSo.Id),
                "BINH_THUONG",
                ct: ct).ConfigureAwait(false);
        }
        catch
        {
            // Bo qua co y: xem chu thich tren.
        }
    }
}
