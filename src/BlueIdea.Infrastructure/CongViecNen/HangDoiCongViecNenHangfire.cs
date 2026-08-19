using BlueIdea.Application.Chung;
using Hangfire;

namespace BlueIdea.Infrastructure.CongViecNen;

/// <summary>Cai dat <see cref="IHangDoiCongViecNen"/> bang Hangfire (luu job trong PostgreSQL).</summary>
public sealed class HangDoiCongViecNenHangfire : IHangDoiCongViecNen
{
    private readonly IBackgroundJobClient _hangfire;

    public HangDoiCongViecNenHangfire(IBackgroundJobClient hangfire) => _hangfire = hangfire;

    public void XepLichTrichXuatVanBan(Guid tepTinId)
        => _hangfire.Enqueue<CongViecTrichXuatVanBan>(x => x.ChayAsync(tepTinId, CancellationToken.None));

    public void XepLichKiemTraTrungLap(Guid sangKienId)
        => _hangfire.Enqueue<CongViecKiemTraTrungLapNen>(
            x => x.ChayAsync(sangKienId, CancellationToken.None));

    public void XepLichGuiHangDoi()
        => _hangfire.Enqueue<CongViecGuiHangDoi>(x => x.ChayAsync(CancellationToken.None));

    public void XepLichPhanCongCham(Guid sangKienId, Guid buocMoiId)
        => _hangfire.Enqueue<CongViecPhanCongCham>(
            x => x.ChayAsync(sangKienId, buocMoiId, CancellationToken.None));

    public void XepLichTaoBienBan(Guid sangKienId, Guid buocTruocId)
        => _hangfire.Enqueue<CongViecTaoBienBan>(
            x => x.ChayAsync(sangKienId, buocTruocId, CancellationToken.None));

    public void XepLichCongBoKetQua(Guid sangKienId)
        => _hangfire.Enqueue<CongViecCongBoKetQua>(
            x => x.ChayAsync(sangKienId, CancellationToken.None));

    public void XepLichXuatBaoCao(
        string loaiBaoCao, BlueIdea.Application.BaoCao.ThamSoBaoCao thamSo, Guid nguoiYeuCauId)
        => _hangfire.Enqueue<CongViecXuatBaoCaoNen>(
            x => x.ChayAsync(loaiBaoCao, thamSo, nguoiYeuCauId, CancellationToken.None));
}

/// <summary>
/// Cai dat thay the khi TAT cong viec nen (mac dinh cho integration test va moi truong khong co
/// Hangfire). Khong lam gi ca - nghiep vu chinh van chay binh thuong.
/// </summary>
public sealed class HangDoiCongViecNenKhongHoatDong : IHangDoiCongViecNen
{
    public void XepLichTrichXuatVanBan(Guid tepTinId)
    {
    }

    public void XepLichKiemTraTrungLap(Guid sangKienId)
    {
    }

    public void XepLichGuiHangDoi()
    {
    }

    public void XepLichPhanCongCham(Guid sangKienId, Guid buocMoiId)
    {
    }

    public void XepLichTaoBienBan(Guid sangKienId, Guid buocTruocId)
    {
    }

    public void XepLichCongBoKetQua(Guid sangKienId)
    {
    }

    public void XepLichXuatBaoCao(
        string loaiBaoCao, BlueIdea.Application.BaoCao.ThamSoBaoCao thamSo, Guid nguoiYeuCauId)
    {
    }
}
