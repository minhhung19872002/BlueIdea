using BlueIdea.Api.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Application.HoiDong;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.HoiDong;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

public sealed record DiemDanhDto(Guid ThanhVienId, bool CoMat, string? LyDoVang);

public sealed record KetThucPhienDto(string? KetLuan);

public sealed record YKienHoSoDto(Guid SangKienId, string? KetLuanRieng, string? KetQua);

/// <summary>Chức năng 19–20 — Hội đồng sáng kiến, thành viên, phiên họp, bỏ phiếu.</summary>
[ApiController]
[Route("api/v1/hoi-dong")]
[Authorize]
[Produces("application/json")]
public sealed class HoiDongController : ControllerBase
{
    private readonly DichVuHoiDong _dichVu;

    public HoiDongController(DichVuHoiDong dichVu) => _dichVu = dichVu;

    [HttpGet]
    [Authorize(Policy = MaQuyen.HoiDongXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    /// <summary>Chi tiết hội đồng kèm thành viên và phiên họp.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.HoiDongXem)]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<HoiDongSangKien>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    [Authorize(Policy = MaQuyen.HoiDongCauHinh)]
    public async Task<IActionResult> ThemAsync([FromBody] HoiDongLuuDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(
            DichVuHoiDong.ApDung(new HoiDongSangKien(), duLieu), ct);

        return Ok(PhanHoiApi<HoiDongSangKien>.Ok(banGhi, "Đã tạo hội đồng"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.HoiDongCauHinh)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] HoiDongLuuDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x => DichVuHoiDong.ApDung(x, duLieu), ct);
        return Ok(PhanHoiApi<HoiDongSangKien>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.HoiDongCauHinh)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }

    /// <summary>Chức năng 20 — Lưu danh sách thành viên (bắt buộc đúng 1 chủ tịch).</summary>
    [HttpPut("{id:guid}/thanh-vien")]
    [Authorize(Policy = MaQuyen.HoiDongCauHinh)]
    public async Task<IActionResult> LuuThanhVienAsync(
        Guid id, [FromBody] List<ThanhVienLuuDto> danhSach, CancellationToken ct)
    {
        await _dichVu.LuuThanhVienAsync(id, danhSach, ct);
        return Ok(PhanHoiApi.Ok($"Đã lưu {danhSach.Count} thành viên"));
    }

    // --- Phiên họp -----------------------------------------------------------

    [HttpPost("phien-hop")]
    [Authorize(Policy = MaQuyen.HoiDongHopPhien)]
    public async Task<IActionResult> TaoPhienHopAsync(
        [FromBody] PhienHopLuuDto duLieu, CancellationToken ct)
    {
        var phien = await _dichVu.TaoPhienHopAsync(duLieu, ct);
        return Ok(PhanHoiApi<PhienHopHoiDong>.Ok(phien, "Đã tạo phiên họp"));
    }

    [HttpGet("phien-hop/{id:guid}")]
    [Authorize(Policy = MaQuyen.HoiDongXem)]
    public async Task<IActionResult> LayPhienHopAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<PhienHopHoiDong>.Ok(await _dichVu.LayPhienHopAsync(id, ct)));

    [HttpPost("phien-hop/{id:guid}/diem-danh")]
    [Authorize(Policy = MaQuyen.HoiDongHopPhien)]
    public async Task<IActionResult> DiemDanhAsync(
        Guid id, [FromBody] DiemDanhDto duLieu, CancellationToken ct)
    {
        await _dichVu.DiemDanhAsync(id, duLieu.ThanhVienId, duLieu.CoMat, duLieu.LyDoVang, ct);
        return Ok(PhanHoiApi.Ok("Đã điểm danh"));
    }

    /// <summary>Ghi ý kiến / kết luận riêng cho một hồ sơ trong phiên họp.</summary>
    [HttpPost("phien-hop/{id:guid}/y-kien-ho-so")]
    [Authorize(Policy = MaQuyen.HoiDongHopPhien)]
    public async Task<IActionResult> GhiYKienHoSoAsync(
        Guid id, [FromBody] YKienHoSoDto duLieu, CancellationToken ct)
    {
        await _dichVu.GhiYKienHoSoAsync(id, duLieu.SangKienId, duLieu.KetLuanRieng, duLieu.KetQua, ct);
        return Ok(PhanHoiApi.Ok("Đã ghi ý kiến cho hồ sơ"));
    }

    /// <summary>Bỏ phiếu (kín hoặc công khai) cho một hồ sơ trong phiên họp.</summary>
    [HttpPost("phien-hop/bo-phieu")]
    [Authorize(Policy = MaQuyen.HoiDongBoPhieu)]
    public async Task<IActionResult> BoPhieuAsync([FromBody] BoPhieuDto duLieu, CancellationToken ct)
    {
        await _dichVu.BoPhieuAsync(duLieu, ct);
        return Ok(PhanHoiApi.Ok("Đã ghi nhận phiếu"));
    }

    [HttpGet("phien-hop/{phienHopId:guid}/ket-qua-bo-phieu")]
    [Authorize(Policy = MaQuyen.HoiDongXem)]
    public async Task<IActionResult> LayKetQuaBoPhieuAsync(
        Guid phienHopId, [FromQuery] Guid sangKienId, CancellationToken ct)
        => Ok(PhanHoiApi<KetQuaBoPhieuDto>.Ok(
            await _dichVu.LayKetQuaBoPhieuAsync(phienHopId, sangKienId, ct)));

    [HttpPost("phien-hop/{id:guid}/ket-thuc")]
    [Authorize(Policy = MaQuyen.HoiDongKetLuan)]
    public async Task<IActionResult> KetThucPhienHopAsync(
        Guid id, [FromBody] KetThucPhienDto duLieu, CancellationToken ct)
    {
        await _dichVu.KetThucPhienHopAsync(id, duLieu.KetLuan, ct);
        return Ok(PhanHoiApi.Ok("Đã kết thúc phiên họp"));
    }
}
