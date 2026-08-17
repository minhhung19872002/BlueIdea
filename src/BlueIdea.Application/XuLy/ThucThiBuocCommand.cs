using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Workflow;
using BlueIdea.Workflow.MoHinh;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XuLy;

/// <summary>Chuc nang 29 - Thuc thi mot buoc xu ly tren ho so.</summary>
public sealed record ThucThiBuocCommand(
    Guid SangKienId,
    Guid TruongHopId,
    string? YKien = null,
    IReadOnlyList<Guid>? TepDinhKemIds = null,
    Guid? NguoiUyQuyenId = null,
    int? PhienBanHoSo = null,
    string? IdempotencyKey = null)
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

    public ThucThiBuocCommandHandler(
        IWorkflowEngine engine, INguoiDungHienTai nguoiDung, IAppDbContext db,
        IDichVuThongBao thongBao, IDongHoHeThong dongHo)
    {
        _engine = engine;
        _nguoiDung = nguoiDung;
        _db = db;
        _thongBao = thongBao;
        _dongHo = dongHo;
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

        await GuiThongBaoAsync(request.SangKienId, ketQua, ct).ConfigureAwait(false);
        return ketQua;
    }

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

        var maSuKien = hoSo.TrangThaiTong switch
        {
            TrangThaiTongHoSo.YeuCauBoSung => SuKienThongBao.YeuCauBoSung,
            TrangThaiTongHoSo.DaPheDuyet => SuKienThongBao.DaPheDuyet,
            TrangThaiTongHoSo.KhongDat => SuKienThongBao.CoKetQua,
            _ => SuKienThongBao.HoSoDuocTiepNhan
        };

        await _thongBao.GuiTheoSuKienAsync(maSuKien, tacGiaIds, new Dictionary<string, object?>
        {
            ["maHoSo"] = hoSo.MaHoSo,
            ["tenSangKien"] = hoSo.TenSangKien,
            ["trangThai"] = hoSo.TrangThaiTong,
            ["tenBuoc"] = ketQua.TenBuocMoi,
            ["thongBao"] = ketQua.ThongBao
        }, ct).ConfigureAwait(false);
    }
}

/// <summary>Chuc nang 29 - Lay danh sach hanh dong kha dung (frontend render nut dong).</summary>
public sealed record LayHanhDongKhaDungQuery(Guid SangKienId) : IRequest<IReadOnlyList<HanhDongKhaDung>>;

public sealed class LayHanhDongKhaDungQueryHandler
    : IRequestHandler<LayHanhDongKhaDungQuery, IReadOnlyList<HanhDongKhaDung>>
{
    private readonly IWorkflowEngine _engine;
    private readonly INguoiDungHienTai _nguoiDung;

    public LayHanhDongKhaDungQueryHandler(IWorkflowEngine engine, INguoiDungHienTai nguoiDung)
    {
        _engine = engine;
        _nguoiDung = nguoiDung;
    }

    public async Task<IReadOnlyList<HanhDongKhaDung>> Handle(
        LayHanhDongKhaDungQuery request, CancellationToken ct)
        => _nguoiDung.Id is null
            ? Array.Empty<HanhDongKhaDung>()
            : await _engine
                .LayHanhDongKhaDungAsync(request.SangKienId, _nguoiDung.Id.Value, ct)
                .ConfigureAwait(false);
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

public sealed record KetQuaXuLyHangLoat(
    int TongSo, int ThanhCong, int ThatBai, IReadOnlyList<string> ChiTietLoi);

public sealed class ThucThiHangLoatCommandHandler
    : IRequestHandler<ThucThiHangLoatCommand, KetQuaXuLyHangLoat>
{
    private readonly IWorkflowEngine _engine;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IAppDbContext _db;

    public ThucThiHangLoatCommandHandler(
        IWorkflowEngine engine, INguoiDungHienTai nguoiDung, IAppDbContext db)
    {
        _engine = engine;
        _nguoiDung = nguoiDung;
        _db = db;
    }

    public async Task<KetQuaXuLyHangLoat> Handle(ThucThiHangLoatCommand request, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
        {
            throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");
        }

        var loi = new List<string>();
        var thanhCong = 0;

        foreach (var id in request.SangKienIds)
        {
            var maHoSo = await _db.SangKien.AsNoTracking()
                .Where(x => x.Id == id).Select(x => x.MaHoSo)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? id.ToString();

            try
            {
                // Moi ho so co the o quy trinh khac nhau -> tim truong hop tuong ung theo ma.
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
            request.SangKienIds.Count, thanhCong, request.SangKienIds.Count - thanhCong, loi);
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
