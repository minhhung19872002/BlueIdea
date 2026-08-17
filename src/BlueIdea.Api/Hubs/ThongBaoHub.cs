using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BlueIdea.Api.Hubs;

/// <summary>
/// Hub thong bao realtime: cap nhat trang thai ho so, chuong thong bao,
/// bang diem truc tiep trong phong hop hoi dong (Muc 5 - Nhom IV, VI).
/// </summary>
[Authorize]
public sealed class ThongBaoHub : Hub
{
    /// <summary>Ten su kien client lang nghe.</summary>
    public static class SuKien
    {
        public const string ThongBaoMoi = "ThongBaoMoi";
        public const string CapNhatTrangThaiHoSo = "CapNhatTrangThaiHoSo";
        public const string CapNhatBangDiem = "CapNhatBangDiem";
        public const string KetQuaKiemTraTrungLap = "KetQuaKiemTraTrungLap";
    }

    public override async Task OnConnectedAsync()
    {
        var nguoiDungId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(nguoiDungId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenNhomNguoiDung(nguoiDungId))
                .ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>Tham gia nhom theo dõi mot ho so cu the (man hinh chi tiet ho so).</summary>
    public Task ThamGiaHoSo(string sangKienId)
        => Groups.AddToGroupAsync(Context.ConnectionId, TenNhomHoSo(sangKienId));

    public Task RoiHoSo(string sangKienId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, TenNhomHoSo(sangKienId));

    /// <summary>Tham gia phong hop hoi dong de nhan bang diem realtime.</summary>
    public Task ThamGiaPhienHop(string phienHopId)
        => Groups.AddToGroupAsync(Context.ConnectionId, TenNhomPhienHop(phienHopId));

    public Task RoiPhienHop(string phienHopId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, TenNhomPhienHop(phienHopId));

    public static string TenNhomNguoiDung(string nguoiDungId) => $"nguoi-dung:{nguoiDungId}";

    public static string TenNhomHoSo(string sangKienId) => $"ho-so:{sangKienId}";

    public static string TenNhomPhienHop(string phienHopId) => $"phien-hop:{phienHopId}";
}
