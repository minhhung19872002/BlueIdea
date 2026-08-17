using BlueIdea.Api.Chung;
using BlueIdea.Application.DanhGia;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

public sealed class PhanCongDto
{
    public Guid HoiDongId { get; set; }

    public List<Guid> SangKienIds { get; set; } = new();

    public List<Guid>? ThanhVienIds { get; set; }

    public DateTimeOffset? HanHoanThanh { get; set; }

    public bool TuDongChiaDeu { get; set; } = true;
}

/// <summary>Chức năng 33–35 — Phân công chấm, chấm điểm, tổng hợp điểm hội đồng.</summary>
[ApiController]
[Route("api/v1/danh-gia")]
[Authorize]
[Produces("application/json")]
public sealed class DanhGiaController : ControllerBase
{
    private readonly DichVuDanhGia _dichVu;

    public DanhGiaController(DichVuDanhGia dichVu) => _dichVu = dichVu;

    /// <summary>Chức năng 33 — Danh sách hồ sơ được phân công cho tôi ("Việc của tôi").</summary>
    [HttpGet("viec-cua-toi")]
    public async Task<IActionResult> LayViecCuaToiAsync(
        [FromQuery] ThamSoPhanTrang thamSo, [FromQuery] string? trangThai, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<PhanCongChamDto>.Tu(
            await _dichVu.LayViecCuaToiAsync(thamSo, trangThai, ct)));

    /// <summary>Phân công thành viên hội đồng chấm hồ sơ (loại trừ xung đột lợi ích).</summary>
    [HttpPost("phan-cong")]
    [Authorize(Policy = MaQuyen.DanhGiaPhanCong)]
    public async Task<IActionResult> PhanCongAsync(
        [FromBody] PhanCongDto duLieu, CancellationToken ct)
    {
        var ketQua = await _dichVu.PhanCongAsync(
            duLieu.HoiDongId, duLieu.SangKienIds, duLieu.ThanhVienIds,
            duLieu.HanHoanThanh, duLieu.TuDongChiaDeu, ct);

        return Ok(PhanHoiApi<DichVuDanhGia.KetQuaPhanCong>.Ok(
            ketQua, $"Đã tạo {ketQua.SoLuotPhanCong} lượt phân công"));
    }

    /// <summary>Chức năng 34 — Lấy phiếu chấm (kèm bộ tiêu chí render động).</summary>
    [HttpGet("phieu")]
    [Authorize(Policy = MaQuyen.DanhGiaChamDiem)]
    public async Task<IActionResult> LayPhieuAsync(
        [FromQuery] Guid sangKienId, [FromQuery] Guid hoiDongId, CancellationToken ct)
        => Ok(PhanHoiApi<PhieuDanhGiaDto>.Ok(
            await _dichVu.LayPhieuChamAsync(sangKienId, hoiDongId, ct)));

    /// <summary>Lưu nháp phiếu chấm.</summary>
    [HttpPost("phieu/luu-nhap")]
    [Authorize(Policy = MaQuyen.DanhGiaChamDiem)]
    public async Task<IActionResult> LuuNhapAsync(
        [FromBody] PhieuChamDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<PhieuDanhGiaDto>.Ok(
            await _dichVu.LuuPhieuAsync(duLieu, guiChinhThuc: false, ct), "Đã lưu nháp"));

    /// <summary>Gửi phiếu chấm chính thức (sau khi gửi chỉ thư ký mới mở lại được).</summary>
    [HttpPost("phieu/gui")]
    [Authorize(Policy = MaQuyen.DanhGiaChamDiem)]
    public async Task<IActionResult> GuiPhieuAsync(
        [FromBody] PhieuChamDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<PhieuDanhGiaDto>.Ok(
            await _dichVu.LuuPhieuAsync(duLieu, guiChinhThuc: true, ct), "Đã gửi phiếu đánh giá"));

    /// <summary>Thư ký mở lại phiếu đã gửi để thành viên sửa.</summary>
    [HttpPost("phieu/{id:guid}/mo-lai")]
    [Authorize(Policy = MaQuyen.DanhGiaMoLaiPhieu)]
    public async Task<IActionResult> MoLaiPhieuAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.MoLaiPhieuAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã mở lại phiếu"));
    }

    /// <summary>Chức năng 32 — Tổng hợp điểm của hội đồng cho một hồ sơ.</summary>
    [HttpPost("tong-hop")]
    [Authorize(Policy = MaQuyen.DanhGiaTongHop)]
    public async Task<IActionResult> TongHopAsync(
        [FromQuery] Guid sangKienId, [FromQuery] Guid hoiDongId,
        [FromQuery] Guid? phienHopId, CancellationToken ct)
        => Ok(PhanHoiApi<KetQuaTongHopDto>.Ok(
            await _dichVu.TongHopDiemAsync(sangKienId, hoiDongId, phienHopId, ct),
            "Đã tổng hợp điểm"));

    /// <summary>Chức năng 35 — Bảng ma trận điểm (hàng = hồ sơ, cột = thành viên).</summary>
    [HttpGet("ma-tran-diem")]
    [Authorize(Policy = MaQuyen.DanhGiaTongHop)]
    public async Task<IActionResult> LayMaTranDiemAsync(
        [FromQuery] Guid hoiDongId, [FromQuery] Guid? dotDeNghiId, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DongMaTranDiem>>.Ok(
            await _dichVu.LayMaTranDiemAsync(hoiDongId, dotDeNghiId, ct)));
}
