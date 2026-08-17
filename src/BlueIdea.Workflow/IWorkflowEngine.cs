using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Workflow.MoHinh;

namespace BlueIdea.Workflow;

/// <summary>
/// Hop dong engine quy trinh dong (Muc 5 - Nhom II dac ta).
/// Cai dat nam o tang Infrastructure vi can truy cap CSDL; toan bo nghiep vu chuyen buoc
/// duoc uy quyen cho <see cref="IBoMayQuyTrinh"/> de test duoc doc lap.
/// </summary>
public interface IWorkflowEngine
{
    Task<WorkflowInstance> KhoiTaoAsync(Guid sangKienId, Guid quyTrinhId, CancellationToken ct);

    Task<IReadOnlyList<HanhDongKhaDung>> LayHanhDongKhaDungAsync(
        Guid sangKienId, Guid nguoiDungId, CancellationToken ct);

    Task<KetQuaXuLy> ThucThiAsync(XuLyBuocRequest request, CancellationToken ct);

    Task<bool> KiemTraQuyenXuLyAsync(Guid sangKienId, Guid buocId, Guid nguoiDungId, CancellationToken ct);

    Task ThuHoiAsync(Guid sangKienId, Guid nguoiDungId, string lyDo, CancellationToken ct);

    Task<WorkflowInstance?> LayTrangThaiAsync(Guid sangKienId, CancellationToken ct);
}

/// <summary>Chuyen doi quy trinh sang/tu chuoi JSON snapshot luu trong ho so.</summary>
public interface IBoChuyenDoiSnapshotQuyTrinh
{
    string TaoSnapshot(QuyTrinh quyTrinh);

    QuyTrinh? DocSnapshot(string? snapshot);
}
