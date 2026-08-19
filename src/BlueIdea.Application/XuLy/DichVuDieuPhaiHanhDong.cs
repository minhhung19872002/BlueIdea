using BlueIdea.Application.Chung;
using BlueIdea.Application.TichHop;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Workflow.MoHinh;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Application.XuLy;

/// <summary>
/// Dieu phoi toan bo hanh dong tu dong (HanhDongCanChay) sau khi mot buoc quy trinh hoan thanh.
/// Moi hanh dong co try/catch rieng: that bai mot hanh dong khong anh huong cac hanh dong con lai.
/// GUI_EMAIL/GUI_SMS duoc xu ly boi GuiThongBaoAsync qua ChucNangBat, khong di qua day.
/// </summary>
public sealed class DichVuDieuPhaiHanhDong
{
    private readonly IAppDbContext _db;
    private readonly IHangDoiCongViecNen _hangDoi;
    private readonly DichVuDongBoLienThong _dongBo;
    private readonly ILogger<DichVuDieuPhaiHanhDong> _logger;

    public DichVuDieuPhaiHanhDong(
        IAppDbContext db,
        IHangDoiCongViecNen hangDoi,
        DichVuDongBoLienThong dongBo,
        ILogger<DichVuDieuPhaiHanhDong> logger)
    {
        _db = db;
        _hangDoi = hangDoi;
        _dongBo = dongBo;
        _logger = logger;
    }

    public async Task DieuPhaiAsync(
        Guid sangKienId, KetQuaXuLy ketQua, CancellationToken ct)
    {
        foreach (var hanhDong in ketQua.HanhDongCanChay)
        {
            if (hanhDong is HanhDongTuDong.GuiEmail or HanhDongTuDong.GuiSms)
            {
                continue;
            }

            try
            {
                await XuLyMotHanhDongAsync(hanhDong, sangKienId, ketQua, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Hành động tự động {HanhDong} thất bại cho sáng kiến {SangKienId}.",
                    hanhDong, sangKienId);
            }
        }
    }

    private async Task XuLyMotHanhDongAsync(
        string hanhDong, Guid sangKienId, KetQuaXuLy ketQua, CancellationToken ct)
    {
        switch (hanhDong)
        {
            case HanhDongTuDong.DongBoLienThong:
                await DongBoLienThongAsync(sangKienId, ketQua, ct).ConfigureAwait(false);
                break;

            case HanhDongTuDong.KiemTraTrungLap:
                _hangDoi.XepLichKiemTraTrungLap(sangKienId);
                break;

            case HanhDongTuDong.CapNhatKetQua:
                // BoMayQuyTrinh.ChuyenBuoc already writes hoSo.KetQua at the engine level.
                _logger.LogDebug(
                    "CAP_NHAT_KET_QUA bỏ qua: kết quả đã được BoMayQuyTrinh ghi trực tiếp " +
                    "cho sáng kiến {SangKienId}.",
                    sangKienId);
                break;

            case HanhDongTuDong.PhanCongCham:
                if (ketQua.BuocMoiId is not null)
                {
                    _hangDoi.XepLichPhanCongCham(sangKienId, ketQua.BuocMoiId.Value);
                }
                else
                {
                    _logger.LogWarning(
                        "PHAN_CONG_CHAM bỏ qua: không có bước mới (quy trình đã kết thúc?) " +
                        "cho sáng kiến {SangKienId}.",
                        sangKienId);
                }
                break;

            case HanhDongTuDong.TaoBienBan:
                if (ketQua.BuocTruocId is not null)
                {
                    _hangDoi.XepLichTaoBienBan(sangKienId, ketQua.BuocTruocId.Value);
                }
                else
                {
                    _logger.LogWarning(
                        "TAO_BIEN_BAN bỏ qua: không có bước trước (quy trình mới bắt đầu?) " +
                        "cho sáng kiến {SangKienId}.",
                        sangKienId);
                }
                break;

            case HanhDongTuDong.CongBoKetQua:
                _hangDoi.XepLichCongBoKetQua(sangKienId);
                break;

            case HanhDongTuDong.TaoQuyetDinh:
            case HanhDongTuDong.YeuCauKySo:
                _logger.LogWarning(
                    "Hành động tự động {HanhDong} được cấu hình nhưng chưa có bộ xử lý tự động " +
                    "cho sáng kiến {SangKienId}. Cần xử lý thủ công.",
                    hanhDong, sangKienId);
                break;

            default:
                _logger.LogWarning(
                    "Hành động tự động không xác định: {HanhDong} cho sáng kiến {SangKienId}.",
                    hanhDong, sangKienId);
                break;
        }
    }

    private async Task DongBoLienThongAsync(
        Guid sangKienId, KetQuaXuLy ketQua, CancellationToken ct)
    {
        var quyTrinhId = await _db.SangKien.AsNoTracking()
            .Where(x => x.Id == sangKienId)
            .Select(x => x.QuyTrinhId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (quyTrinhId is null)
        {
            return;
        }

        var laPheDuyet = ketQua.TrangThaiTongMoi == TrangThaiTongHoSo.DaPheDuyet;

        var cauHinhs = await _db.QuyTrinhLienThong.AsNoTracking()
            .Where(x => x.QuyTrinhId == quyTrinhId.Value
                && x.TrangThai == TrangThaiDanhMuc.HoatDong
                && (
                    (x.BuocId == ketQua.BuocTruocId && x.SuKien == SuKienLienThong.KhiHoanThanh)
                    || (x.BuocId == ketQua.BuocMoiId && x.SuKien == SuKienLienThong.KhiVaoBuoc)
                    || (x.BuocId == ketQua.BuocTruocId && x.SuKien == SuKienLienThong.KhiPheDuyet && laPheDuyet)
                    || (x.BuocId == null && (
                        x.SuKien == SuKienLienThong.KhiHoanThanh
                        || x.SuKien == SuKienLienThong.KhiVaoBuoc
                        || (x.SuKien == SuKienLienThong.KhiPheDuyet && laPheDuyet)
                    ))
                ))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var cauHinh in cauHinhs)
        {
            try
            {
                await _dongBo.DongBoSangKienAsync(cauHinh.HeThongTichHopId, sangKienId, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Đồng bộ liên thông thất bại cho sáng kiến {SangKienId} sang hệ thống {HeThongId}.",
                    sangKienId, cauHinh.HeThongTichHopId);
            }
        }
    }

}
