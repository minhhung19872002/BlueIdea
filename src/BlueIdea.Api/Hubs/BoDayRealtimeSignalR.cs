using BlueIdea.Application.Chung;
using Microsoft.AspNetCore.SignalR;

namespace BlueIdea.Api.Hubs;

/// <summary>
/// Bản cài đặt đẩy realtime bằng SignalR.
///
/// Chỉ gửi **tín hiệu** ("có thông báo mới", "hồ sơ vừa đổi") chứ không gửi kèm nội dung nghiệp
/// vụ: client nhận tín hiệu rồi gọi API để lấy dữ liệu theo đúng quyền của mình. Nhờ vậy không
/// có đường nào rò dữ liệu qua kênh realtime khi người dùng không đủ quyền xem.
/// </summary>
public sealed class BoDayRealtimeSignalR : IBoDayRealtime
{
    private readonly IHubContext<ThongBaoHub> _hub;

    public BoDayRealtimeSignalR(IHubContext<ThongBaoHub> hub) => _hub = hub;

    public Task ThongBaoMoiAsync(Guid nguoiNhanId, string tieuDe, CancellationToken ct = default)
        => _hub.Clients
            .Group(ThongBaoHub.TenNhomNguoiDung(nguoiNhanId.ToString()))
            .SendAsync(ThongBaoHub.SuKien.ThongBaoMoi, new { tieuDe }, ct);

    public Task CapNhatHoSoAsync(Guid sangKienId, CancellationToken ct = default)
        => _hub.Clients
            .Group(ThongBaoHub.TenNhomHoSo(sangKienId.ToString()))
            .SendAsync(ThongBaoHub.SuKien.CapNhatTrangThaiHoSo, new { sangKienId }, ct);

    public Task CapNhatPhienHopAsync(Guid phienHopId, CancellationToken ct = default)
        => _hub.Clients
            .Group(ThongBaoHub.TenNhomPhienHop(phienHopId.ToString()))
            .SendAsync(ThongBaoHub.SuKien.CapNhatBangDiem, new { phienHopId }, ct);

    public Task KetQuaTrungLapAsync(Guid sangKienId, CancellationToken ct = default)
        => _hub.Clients
            .Group(ThongBaoHub.TenNhomHoSo(sangKienId.ToString()))
            .SendAsync(ThongBaoHub.SuKien.KetQuaKiemTraTrungLap, new { sangKienId }, ct);
}
