using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Workflow;
using BlueIdea.Workflow.MoHinh;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Application.XuLy;

/// <summary>Chuc nang 29 - Thuc thi mot buoc xu ly tren ho so.</summary>
public sealed record ThucThiBuocCommand(
    Guid SangKienId,
    Guid TruongHopId,
    string? YKien = null,
    IReadOnlyList<Guid>? TepDinhKemIds = null,
    Guid? NguoiUyQuyenId = null,
    int? PhienBanHoSo = null,
    string? IdempotencyKey = null,
    string? HanhDongNguoiDung = null)
    : IRequest<KetQuaXuLy>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.XuLyThucThi;

    public Guid? DoiTuongId => SangKienId;

    public string HanhDongNhatKy => "XU_LY_BUOC";

    public string ModuleNhatKy => "XU_LY";
}

public sealed class ThucThiBuocCommandHandler : IRequestHandler<ThucThiBuocCommand, KetQuaXuLy>
{
    private readonly IWorkflowEngine _engine;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IAppDbContext _db;
    private readonly IDichVuThongBao _thongBao;
    private readonly IDongHoHeThong _dongHo;
    private readonly DichVuDieuPhaiHanhDong _dieuPhai;
    private readonly ILogger<ThucThiBuocCommandHandler> _logger;

    public ThucThiBuocCommandHandler(
        IWorkflowEngine engine, INguoiDungHienTai nguoiDung, IAppDbContext db,
        IDichVuThongBao thongBao, IDongHoHeThong dongHo,
        DichVuDieuPhaiHanhDong dieuPhai, ILogger<ThucThiBuocCommandHandler> logger)
    {
        _engine = engine;
        _nguoiDung = nguoiDung;
        _db = db;
        _thongBao = thongBao;
        _dongHo = dongHo;
        _dieuPhai = dieuPhai;
        _logger = logger;
    }

    public async Task<KetQuaXuLy> Handle(ThucThiBuocCommand request, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
        {
            throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");
        }

        // Chong double-submit: cung Idempotency-Key da xu ly thi tra ve ket qua truoc do.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var daXuLy = await _db.NhatKyHeThong.AsNoTracking()
                .AnyAsync(x => x.HanhDong == "XU_LY_BUOC"
                               && x.DoiTuongId == request.SangKienId
                               && x.MoTa == request.IdempotencyKey, ct)
                .ConfigureAwait(false);

            if (daXuLy)
            {
                return KetQuaXuLy.Loi(MaLoiHeThong.YeuCauTrungLap,
                    "Yêu cầu này đã được xử lý trước đó.");
            }
        }

        // Chuc nang 15/29 — uy quyen chi hop le khi NGUOI UY QUYEN cung la tac nhan cua buoc do.
        // Khong kiem thi ai cung khai bua mot Id vao truong nay va nhat ky xu ly ghi sai nguoi
        // chiu trach nhiem — dung thu ma ho so nghiem thu dua vao de truy nguoc.
        if (request.NguoiUyQuyenId.HasValue)
        {
            var hoSo = await _db.SangKien.AsNoTracking()
                .Where(x => x.Id == request.SangKienId)
                .Select(x => new { x.BuocHienTaiId })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (hoSo?.BuocHienTaiId is null)
            {
                throw new NghiepVuException(MaLoiHeThong.BuocKhongHopLe,
                    "Hồ sơ không ở bước xử lý nào nên không uỷ quyền được.");
            }

            var uyQuyenHopLe = await _engine
                .KiemTraQuyenXuLyAsync(
                    request.SangKienId, hoSo.BuocHienTaiId.Value, request.NguoiUyQuyenId.Value, ct)
                .ConfigureAwait(false);

            if (!uyQuyenHopLe)
            {
                throw new NghiepVuException(MaLoiHeThong.KhongCoQuyenXuLyBuoc,
                    "Người uỷ quyền không phải tác nhân được cấu hình xử lý bước hiện tại.");
            }
        }

        var ketQua = await _engine.ThucThiAsync(new XuLyBuocRequest
        {
            SangKienId = request.SangKienId,
            NguoiDungId = _nguoiDung.Id.Value,
            TruongHopId = request.TruongHopId,
            YKien = request.YKien,
            TepDinhKemIds = request.TepDinhKemIds ?? Array.Empty<Guid>(),
            NguoiUyQuyenId = request.NguoiUyQuyenId,
            PhienBanHoSo = request.PhienBanHoSo,
            IdempotencyKey = request.IdempotencyKey,
            HanhDongNguoiDung = request.HanhDongNguoiDung,
            DiaChiIp = _nguoiDung.DiaChiIp,
            UserAgent = _nguoiDung.UserAgent
        }, ct).ConfigureAwait(false);

        if (!ketQua.ThanhCong)
        {
            throw new NghiepVuException(ketQua.MaLoi ?? MaLoiHeThong.LoiHeThong, ketQua.ThongBao);
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            _db.NhatKyHeThong.Add(new NhatKyHeThong
            {
                NguoiDungId = _nguoiDung.Id,
                TenDangNhap = _nguoiDung.TenDangNhap,
                HanhDong = "XU_LY_BUOC",
                Module = "XU_LY",
                DoiTuong = nameof(HoSoSangKien),
                DoiTuongId = request.SangKienId,
                MoTa = request.IdempotencyKey,
                DiaChiIp = _nguoiDung.DiaChiIp,
                UserAgent = _nguoiDung.UserAgent,
                ThoiGian = _dongHo.BayGio
            });

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (!ketQua.ChoThemTacNhan)
        {
            await GuiThongBaoAsync(request.SangKienId, ketQua, ct).ConfigureAwait(false);
        }

        await _dieuPhai.DieuPhaiAsync(request.SangKienId, ketQua, ct).ConfigureAwait(false);
        return ketQua;
    }

    /// <summary>
    /// Chon su kien thong bao theo trang thai tong cua ho so.
    ///
    /// Ho so khong dat phat dung su kien "bi tu choi": mau MTB_TU_CHOI neu ro ly do khong duoc
    /// tiep nhan, con MTB_CO_KET_QUA chi noi chung chung "da co ket qua". Truoc day ca hai truong
    /// hop deu dung mot mau nen tac gia bi loai khong biet vi sao ho so cua minh truot.
    ///
    /// CoKetQua van duoc phat, nhung o cho khac: khi cong bo quyet dinh (DichVuQuyetDinh).
    /// </summary>
    public static string SuKienTheoTrangThai(string? trangThaiTong) => trangThaiTong switch
    {
        TrangThaiTongHoSo.YeuCauBoSung => SuKienThongBao.YeuCauBoSung,
        TrangThaiTongHoSo.DaPheDuyet => SuKienThongBao.DaPheDuyet,
        TrangThaiTongHoSo.KhongDat => SuKienThongBao.HoSoBiTuChoi,
        _ => SuKienThongBao.HoSoDuocTiepNhan
    };

    /// <summary>Gui thong bao cho tac gia va cho tac nhan cua buoc tiep theo.</summary>
    private async Task GuiThongBaoAsync(Guid sangKienId, KetQuaXuLy ketQua, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is null)
        {
            return;
        }

        var tacGiaIds = hoSo.DanhSachTacGia
            .Where(t => t.NguoiDungId.HasValue)
            .Select(t => t.NguoiDungId!.Value)
            .Distinct()
            .ToList();

        if (tacGiaIds.Count == 0)
        {
            return;
        }

        var maSuKien = SuKienTheoTrangThai(hoSo.TrangThaiTong);
        var kenhChoPhep = LayKenhChoPhep(ketQua.ChucNangBat);

        var bien = new Dictionary<string, object?>
        {
            ["sangKienId"] = sangKienId,
            ["duongDan"] = DuongDanGiaoDien.ChiTietHoSo(sangKienId),
            ["maHoSo"] = hoSo.MaHoSo,
            ["tenSangKien"] = hoSo.TenSangKien,
            ["trangThai"] = hoSo.TrangThaiTong,
            ["tenBuoc"] = ketQua.TenBuocMoi,
            ["thongBao"] = ketQua.ThongBao
        };

        await _thongBao.GuiTheoSuKienAsync(maSuKien, tacGiaIds, bien, kenhChoPhep, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Map workflow feature toggles (ChucNangBat) to allowed notification channels.
    /// APP is always allowed; EMAIL/SMS only when their feature toggle is enabled.
    /// </summary>
    internal static IReadOnlyCollection<string> LayKenhChoPhep(IReadOnlyList<string>? chucNangBat)
    {
        var kenh = new List<string>(3) { "APP" };

        if (chucNangBat is null or { Count: 0 })
        {
            return kenh;
        }

        if (chucNangBat.Contains(MaChucNangBoSung.GuiEmail))
        {
            kenh.Add("EMAIL");
        }

        if (chucNangBat.Contains(MaChucNangBoSung.GuiSms))
        {
            kenh.Add("SMS");
        }

        return kenh;
    }

}

/// <summary>Chuc nang 29 - Lay danh sach hanh dong kha dung (frontend render nut dong).</summary>
public sealed record LayHanhDongKhaDungQuery(Guid SangKienId) : IRequest<IReadOnlyList<HanhDongKhaDung>>, ICoYeuCauQuyen
{
    public string MaQuyenYeuCau => MaQuyen.SangKienXem;
}

public sealed class LayHanhDongKhaDungQueryHandler
    : IRequestHandler<LayHanhDongKhaDungQuery, IReadOnlyList<HanhDongKhaDung>>
{
    private readonly IWorkflowEngine _engine;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;

    public LayHanhDongKhaDungQueryHandler(
        IWorkflowEngine engine, INguoiDungHienTai nguoiDung,
        IAppDbContext db, IDichVuPhanQuyen phanQuyen)
    {
        _engine = engine;
        _nguoiDung = nguoiDung;
        _db = db;
        _phanQuyen = phanQuyen;
    }

    public async Task<IReadOnlyList<HanhDongKhaDung>> Handle(
        LayHanhDongKhaDungQuery request, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
            return Array.Empty<HanhDongKhaDung>();

        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == request.SangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is null)
            return Array.Empty<HanhDongKhaDung>();

        var phamVi = await _phanQuyen.LayPhamViTruyCapAsync(_nguoiDung.Id.Value, ct).ConfigureAwait(false);
        if (!phamVi.ToanHeThong)
        {
            var laTacGia = hoSo.NguoiTaoId == _nguoiDung.Id.Value
                           || (hoSo.DanhSachTacGia?.Any(t => t.NguoiDungId == _nguoiDung.Id.Value) == true);

            if (phamVi.ChiCaNhan && !laTacGia)
                return Array.Empty<HanhDongKhaDung>();

            var trongDonVi = hoSo.DonViId.HasValue && phamVi.DonViIds.Contains(hoSo.DonViId.Value);
            if (!laTacGia && !trongDonVi)
            {
                var thanhVienIds = await _db.HoiDongThanhVien.AsNoTracking()
                    .Where(x => x.NguoiDungId == _nguoiDung.Id.Value
                                && x.TrangThai == TrangThaiDanhMuc.HoatDong)
                    .Select(x => new { x.Id, x.HoiDongId })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                if (thanhVienIds.Count == 0)
                    return Array.Empty<HanhDongKhaDung>();

                var duocPhanCong = await _db.SangKienPhanCong.AsNoTracking()
                    .AnyAsync(x => x.SangKienId == request.SangKienId
                                   && thanhVienIds.Select(tv => tv.Id).Contains(x.ThanhVienId), ct)
                    .ConfigureAwait(false);

                if (!duocPhanCong)
                {
                    var hoiDongIds = thanhVienIds.Select(tv => tv.HoiDongId).ToList();
                    var hoSoTrongPhienHop = await _db.PhienHopHoSo.AsNoTracking()
                        .AnyAsync(x => x.SangKienId == request.SangKienId
                                       && hoiDongIds.Contains(x.PhienHop!.HoiDongId), ct)
                        .ConfigureAwait(false);

                    if (!hoSoTrongPhienHop)
                        return Array.Empty<HanhDongKhaDung>();
                }
            }
        }

        return await _engine
            .LayHanhDongKhaDungAsync(request.SangKienId, _nguoiDung.Id.Value, ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Xu ly hang loat nhieu ho so cung buoc (chuc nang 29).</summary>
public sealed record ThucThiHangLoatCommand(
    IReadOnlyList<Guid> SangKienIds, Guid TruongHopId, string? YKien = null)
    : IRequest<KetQuaXuLyHangLoat>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.XuLyThucThi;

    public string HanhDongNhatKy => "XU_LY_HANG_LOAT";

    public string ModuleNhatKy => "XU_LY";
}

public sealed class ThucThiHangLoatCommandValidator : AbstractValidator<ThucThiHangLoatCommand>
{
    public ThucThiHangLoatCommandValidator()
    {
        RuleFor(x => x.SangKienIds).NotEmpty()
            .Must(x => x.Count <= 200)
            .WithMessage("Không được xử lý quá 200 hồ sơ trong một lần.");
        RuleFor(x => x.TruongHopId).NotEmpty();
    }
}

public sealed record KetQuaXuLyHangLoat(
    int TongSo, int ThanhCong, int ThatBai, IReadOnlyList<string> ChiTietLoi);

public sealed class ThucThiHangLoatCommandHandler
    : IRequestHandler<ThucThiHangLoatCommand, KetQuaXuLyHangLoat>
{
    private readonly IWorkflowEngine _engine;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDichVuThongBao _thongBao;
    private readonly DichVuDieuPhaiHanhDong _dieuPhai;
    private readonly ILogger<ThucThiHangLoatCommandHandler> _logger;

    public ThucThiHangLoatCommandHandler(
        IWorkflowEngine engine, INguoiDungHienTai nguoiDung, IAppDbContext db,
        IDichVuPhanQuyen phanQuyen, IDichVuThongBao thongBao,
        DichVuDieuPhaiHanhDong dieuPhai,
        ILogger<ThucThiHangLoatCommandHandler> logger)
    {
        _engine = engine;
        _nguoiDung = nguoiDung;
        _db = db;
        _phanQuyen = phanQuyen;
        _thongBao = thongBao;
        _dieuPhai = dieuPhai;
        _logger = logger;
    }

    public async Task<KetQuaXuLyHangLoat> Handle(ThucThiHangLoatCommand request, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
        {
            throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");
        }

        var loi = new List<string>();
        var thanhCong = 0;

        var nguoiDungId = _nguoiDung.Id.Value;
        var phamVi = await _phanQuyen.LayPhamViTruyCapAsync(nguoiDungId, ct)
            .ConfigureAwait(false);

        var truyVan = _db.SangKien.AsNoTracking()
            .Where(x => request.SangKienIds.Contains(x.Id));

        if (!phamVi.ToanHeThong)
        {
            if (phamVi.ChiCaNhan)
            {
                truyVan = truyVan.Where(x => x.NguoiTaoId == nguoiDungId
                    || x.DanhSachTacGia.Any(t => t.NguoiDungId == nguoiDungId));
            }
            else
            {
                var donViIds = phamVi.DonViIds.ToList();
                truyVan = truyVan.Where(x =>
                    (x.DonViId.HasValue && donViIds.Contains(x.DonViId.Value))
                    || x.NguoiTaoId == nguoiDungId
                    || x.DanhSachTacGia.Any(t => t.NguoiDungId == nguoiDungId));
            }
        }

        var uniqueIds = request.SangKienIds.Distinct().ToList();

        var maHoSoMap = await truyVan
            .Select(x => new { x.Id, x.MaHoSo })
            .ToDictionaryAsync(x => x.Id, x => x.MaHoSo ?? x.Id.ToString(), ct)
            .ConfigureAwait(false);

        foreach (var id in uniqueIds)
        {
            if (!maHoSoMap.ContainsKey(id))
            {
                loi.Add($"{id}: không tìm thấy hoặc không có quyền xử lý.");
                continue;
            }

            var maHoSo = maHoSoMap[id];

            try
            {
                var hanhDong = await _engine
                    .LayHanhDongKhaDungAsync(id, _nguoiDung.Id.Value, ct)
                    .ConfigureAwait(false);

                var truongHop = hanhDong.FirstOrDefault(h => h.TruongHopId == request.TruongHopId)
                                ?? hanhDong.FirstOrDefault(h => !h.BiChan);

                if (truongHop is null)
                {
                    loi.Add($"{maHoSo}: không có hành động khả dụng.");
                    continue;
                }

                var ketQua = await _engine.ThucThiAsync(new XuLyBuocRequest
                {
                    SangKienId = id,
                    NguoiDungId = _nguoiDung.Id.Value,
                    TruongHopId = truongHop.TruongHopId,
                    YKien = request.YKien,
                    DiaChiIp = _nguoiDung.DiaChiIp,
                    UserAgent = _nguoiDung.UserAgent
                }, ct).ConfigureAwait(false);

                if (ketQua.ThanhCong)
                {
                    thanhCong++;

                    if (!ketQua.ChoThemTacNhan)
                    {
                        await GuiThongBaoAsync(id, ketQua, ct).ConfigureAwait(false);
                    }

                    await _dieuPhai.DieuPhaiAsync(id, ketQua, ct).ConfigureAwait(false);
                }
                else
                {
                    loi.Add($"{maHoSo}: {ketQua.ThongBao}");
                }
            }
            catch (Exception ex) when (ex is NghiepVuException or KhongTimThayException or KhongCoQuyenException)
            {
                loi.Add($"{maHoSo}: {ex.Message}");
            }
        }

        return new KetQuaXuLyHangLoat(
            uniqueIds.Count, thanhCong, uniqueIds.Count - thanhCong, loi);
    }

    private async Task GuiThongBaoAsync(Guid sangKienId, KetQuaXuLy ketQua, CancellationToken ct)
    {
        try
        {
            var hoSo = await _db.SangKien.AsNoTracking()
                .Include(x => x.DanhSachTacGia)
                .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
                .ConfigureAwait(false);

            if (hoSo is null)
            {
                return;
            }

            var tacGiaIds = hoSo.DanhSachTacGia
                .Where(t => t.NguoiDungId.HasValue)
                .Select(t => t.NguoiDungId!.Value)
                .Distinct()
                .ToList();

            if (tacGiaIds.Count == 0)
            {
                return;
            }

            var maSuKien = ThucThiBuocCommandHandler.SuKienTheoTrangThai(hoSo.TrangThaiTong);
            var kenhChoPhep = ThucThiBuocCommandHandler.LayKenhChoPhep(ketQua.ChucNangBat);

            var bien = new Dictionary<string, object?>
            {
                ["sangKienId"] = sangKienId,
                ["duongDan"] = DuongDanGiaoDien.ChiTietHoSo(sangKienId),
                ["maHoSo"] = hoSo.MaHoSo,
                ["tenSangKien"] = hoSo.TenSangKien,
                ["trangThai"] = hoSo.TrangThaiTong,
                ["tenBuoc"] = ketQua.TenBuocMoi,
                ["thongBao"] = ketQua.ThongBao
            };

            await _thongBao.GuiTheoSuKienAsync(maSuKien, tacGiaIds, bien, kenhChoPhep, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Gửi thông báo hàng loạt thất bại cho sáng kiến {SangKienId}.", sangKienId);
        }
    }

}

/// <summary>Thu hoi buoc da xu ly (chuc nang 29 - nut "Thu hồi").</summary>
public sealed record ThuHoiBuocCommand(Guid SangKienId, string LyDo)
    : IRequest<Unit>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.XuLyThuHoi;

    public Guid? DoiTuongId => SangKienId;

    public string HanhDongNhatKy => "THU_HOI_BUOC";

    public string ModuleNhatKy => "XU_LY";
}

public sealed class ThuHoiBuocCommandHandler : IRequestHandler<ThuHoiBuocCommand, Unit>
{
    private readonly IWorkflowEngine _engine;
    private readonly INguoiDungHienTai _nguoiDung;

    public ThuHoiBuocCommandHandler(IWorkflowEngine engine, INguoiDungHienTai nguoiDung)
    {
        _engine = engine;
        _nguoiDung = nguoiDung;
    }

    public async Task<Unit> Handle(ThuHoiBuocCommand request, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
        {
            throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");
        }

        await _engine
            .ThuHoiAsync(request.SangKienId, _nguoiDung.Id.Value, request.LyDo, ct)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
