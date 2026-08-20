using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.SangKien;

/// <summary>
/// Chuc nang 23, 29 — Huy mot ho so da nop (nop nham dot, nop trung, phat hien sai sot sau khi
/// tiep nhan).
///
/// Khac "rut ho so": rut la viec cua TAC GIA va chi lam duoc khi ho so chua vao buoc cham diem.
/// Huy la viec cua CAN BO dieu phoi, danh cho nhung ho so khong the di tiep nhung tac gia khong
/// con quyen rut. Truoc day trang thai DA_HUY co trong danh muc, hien duoc tren bao cao, nhung
/// khong mot chuc nang nao dat ho so ve trang thai do — tuc la khong co loi ra cho tinh huong nay.
///
/// Khong dung xoa mem: ho so bi huy van phai tra cuu duoc, van phai nam trong bao cao voi dung
/// nhan "Da huy" va van giu nguyen lich su xu ly.
/// </summary>
public sealed record HuyHoSoCommand(Guid Id, string LyDo)
    : IRequest<Unit>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.SangKienHuy;

    public Guid? DoiTuongId => Id;

    public string HanhDongNhatKy => "HUY_HO_SO";

    public string ModuleNhatKy => "SANG_KIEN";
}

public sealed class HuyHoSoCommandValidator : AbstractValidator<HuyHoSoCommand>
{
    public HuyHoSoCommandValidator()
        => RuleFor(x => x.LyDo)
            .NotEmpty().WithMessage("Vui lòng nhập lý do huỷ hồ sơ")
            .MaximumLength(2000);
}

public sealed class HuyHoSoCommandHandler : IRequestHandler<HuyHoSoCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuThongBao? _thongBao;

    public HuyHoSoCommandHandler(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo,
        IDichVuThongBao? thongBao = null)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
        _thongBao = thongBao;
    }

    public async Task<Unit> Handle(HuyHoSoCommand request, CancellationToken ct)
    {
        var hoSo = await _db.SangKien
                       .Include(x => x.DanhSachTacGia)
                       .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
                       .ConfigureAwait(false)
                   ?? throw new KhongTimThayException("hồ sơ sáng kiến", request.Id);

        if (hoSo.TrangThaiTong == TrangThaiTongHoSo.Nhap)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                "Hồ sơ còn ở dạng nháp — tác giả tự xoá được, không cần huỷ.");
        }

        if (hoSo.TrangThaiTong is TrangThaiTongHoSo.DaHuy or TrangThaiTongHoSo.DaRut)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                $"Hồ sơ đang ở trạng thái '{hoSo.TrangThaiTong}'.");
        }

        // Ho so da duoc cong nhan va gan vao mot quyet dinh thi khong huy bang mot nut bam duoc
        // nua: viec do phai di bang huy/thu hoi quyet dinh, co van ban hanh chinh kem theo.
        var daGanQuyetDinh = await _db.QuyetDinhSangKien.AsNoTracking()
            .AnyAsync(x => x.SangKienId == hoSo.Id, ct)
            .ConfigureAwait(false);

        if (daGanQuyetDinh)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                "Hồ sơ đã được gán vào quyết định công nhận — phải xử lý ở cấp quyết định, "
                + "không huỷ trực tiếp trên hồ sơ.");
        }

        var trangThaiCu = hoSo.TrangThaiTong;

        hoSo.TrangThaiTong = TrangThaiTongHoSo.DaHuy;
        hoSo.BuocHienTaiId = null;
        hoSo.HanXuLyHienTai = null;
        hoSo.NgayHoanThanh = _dongHo.BayGio;
        hoSo.PhienBan++;

        // Dong cac luot xu ly con mo: de nguyen thi ho so da huy van hien trong "viec cua toi"
        // cua nguoi dang giu buoc.
        var dangMo = await _db.SangKienXuLy
            // Loc theo ThoiGianXuLy chu khong theo DaHoanThanh: DaHoanThanh la thuoc tinh
            // TINH trong .NET, EF khong dich sang SQL duoc.
            .Where(x => x.SangKienId == hoSo.Id && x.ThoiGianXuLy == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var luot in dangMo)
        {
            // DaHoanThanh la thuoc tinh tinh tu ThoiGianXuLy — dat moc thoi gian la luot dong.
            luot.ThoiGianXuLy ??= _dongHo.BayGio;
            luot.YKien = string.IsNullOrWhiteSpace(luot.YKien)
                ? $"Hồ sơ bị huỷ: {request.LyDo.Trim()}"
                : luot.YKien;
        }

        _db.SangKienLichSu.Add(new SangKienLichSu
        {
            SangKienId = hoSo.Id,
            HanhDong = HanhDongLichSuHoSo.Huy,
            GhiChu = request.LyDo.Trim(),
            TruongThayDoi = new List<string> { nameof(hoSo.TrangThaiTong) },
            GiaTriTruoc = new Dictionary<string, string?> { ["trangThaiTong"] = trangThaiCu },
            GiaTriSau = new Dictionary<string, string?>
            {
                ["trangThaiTong"] = TrangThaiTongHoSo.DaHuy
            },
            NguoiThucHienId = _nguoiDung.Id,
            ThoiGian = _dongHo.BayGio,
            DiaChiIp = _nguoiDung.DiaChiIp,
            UserAgent = _nguoiDung.UserAgent
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await BaoTacGiaAsync(hoSo, request.LyDo.Trim(), ct).ConfigureAwait(false);

        return Unit.Value;
    }

    /// <summary>
    /// Bao cho tac gia. Nuot loi co y: ho so da huy trong CSDL roi, mot lan gui thong bao hong
    /// khong duoc lam hong thao tac da hoan tat.
    /// </summary>
    private async Task BaoTacGiaAsync(HoSoSangKien hoSo, string lyDo, CancellationToken ct)
    {
        if (_thongBao is null)
        {
            return;
        }

        var tacGiaIds = (hoSo.DanhSachTacGia ?? new List<SangKienTacGia>())
            .Where(t => t.NguoiDungId.HasValue)
            .Select(t => t.NguoiDungId!.Value)
            .Concat(hoSo.NguoiTaoId.HasValue ? new[] { hoSo.NguoiTaoId.Value } : Array.Empty<Guid>())
            .Distinct()
            .ToList();

        foreach (var id in tacGiaIds)
        {
            try
            {
                await _thongBao.GuiTrongUngDungAsync(
                    id,
                    $"Hồ sơ {hoSo.MaHoSo} đã bị huỷ",
                    $"Lý do: {lyDo}",
                    DuongDanGiaoDien.ChiTietHoSo(hoSo.Id),
                    "CAO",
                    ct: ct).ConfigureAwait(false);
            }
            catch
            {
                // Bo qua co y: xem chu thich tren.
            }
        }
    }
}
