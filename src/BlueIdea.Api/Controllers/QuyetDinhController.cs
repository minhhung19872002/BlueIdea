using BlueIdea.Api.Chung;
using BlueIdea.Application.BanHanh;
using BlueIdea.Domain.Chung;
using BlueIdea.Reporting;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>Chức năng 8, 31, 32, 36 — Ban hành quyết định công nhận và công bố kết quả.</summary>
[ApiController]
[Route("api/v1/quyet-dinh")]
[Authorize]
[Produces("application/json")]
public sealed class QuyetDinhController : ControllerBase
{
    private readonly DichVuQuyetDinh _dichVu;

    public QuyetDinhController(DichVuQuyetDinh dichVu) => _dichVu = dichVu;

    /// <summary>Danh sách quyết định đã ban hành.</summary>
    [HttpGet]
    [Authorize(Policy = MaQuyen.QuyetDinhXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoPhanTrang thamSo,
        [FromQuery] string? tuKhoa,
        [FromQuery] Guid? dotDeNghiId,
        CancellationToken ct)
        => Ok(PhanHoiPhanTrang<QuyetDinhDto>.Tu(
            await _dichVu.DanhSachAsync(thamSo.Trang, thamSo.SoDong, tuKhoa, dotDeNghiId, ct)));

    /// <summary>Chi tiết quyết định kèm danh sách sáng kiến được công nhận.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.QuyetDinhXem)]
    public async Task<IActionResult> LayChiTietAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<QuyetDinhChiTietDto>.Ok(await _dichVu.ChiTietAsync(id, ct)));

    /// <summary>
    /// Sáng kiến đủ điều kiện đưa vào quyết định: đã có kết quả Đạt và chưa nằm trong quyết định nào.
    /// </summary>
    [HttpGet("ho-so-du-dieu-kien")]
    [Authorize(Policy = MaQuyen.QuyetDinhBanHanh)]
    public async Task<IActionResult> LayHoSoDuDieuKienAsync(
        [FromQuery] Guid? dotDeNghiId, [FromQuery] Guid? quyetDinhDangSua, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<HoSoDuDieuKienDto>>.Ok(
            await _dichVu.HoSoDuDieuKienAsync(dotDeNghiId, quyetDinhDangSua, ct)));

    [HttpPost]
    [Authorize(Policy = MaQuyen.QuyetDinhBanHanh)]
    public async Task<IActionResult> BanHanhAsync([FromBody] LuuQuyetDinhDto duLieu, CancellationToken ct)
    {
        var id = await _dichVu.TaoAsync(duLieu, ct);
        return Ok(PhanHoiApi<Guid>.Ok(id, "Đã ban hành quyết định"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.QuyetDinhBanHanh)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuQuyetDinhDto duLieu, CancellationToken ct)
    {
        await _dichVu.CapNhatAsync(id, duLieu, ct);
        return Ok(PhanHoiApi.Ok("Đã cập nhật quyết định"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.QuyetDinhBanHanh)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xoá quyết định"));
    }

    /// <summary>Chức năng 32 — Công bố kết quả hàng loạt cho toàn bộ sáng kiến trong quyết định.</summary>
    [HttpPost("{id:guid}/cong-bo")]
    [Authorize(Policy = MaQuyen.QuyetDinhBanHanh)]
    public async Task<IActionResult> CongBoAsync(
        Guid id, [FromQuery] bool congKhai, CancellationToken ct)
    {
        var soLuong = await _dichVu.CongBoAsync(id, congKhai, ct);
        return Ok(PhanHoiApi.Ok($"Đã công bố kết quả cho {soLuong} sáng kiến"));
    }

    /// <summary>Xuất quyết định ra PDF theo mẫu văn bản hành chính.</summary>
    [HttpGet("{id:guid}/xuat-pdf")]
    [Authorize(Policy = MaQuyen.QuyetDinhXem)]
    public async Task<IActionResult> XuatPdfAsync(Guid id, CancellationToken ct)
    {
        var (quyetDinh, tenDonVi, tieuDeVanBan, danhSach) = await _dichVu.DuLieuXuatAsync(id, ct);

        var bang = new BangPdf(
            "DANH SÁCH SÁNG KIẾN ĐƯỢC CÔNG NHẬN",
            new[] { "TT", "Mã hồ sơ", "Tên sáng kiến", "Tác giả chính", "Đơn vị", "Điểm", "Mức công nhận" },
            danhSach.Select((x, i) => new[]
            {
                (i + 1).ToString(),
                x.MaHoSo,
                x.TenSangKien,
                x.TenTacGiaChinh ?? string.Empty,
                x.TenDonVi ?? string.Empty,
                x.TongDiem?.ToString("0.##") ?? string.Empty,
                x.TenMucCongNhan ?? string.Empty
            }).ToList());

        var noiDung = string.IsNullOrWhiteSpace(quyetDinh.TrichYeu)
            ? $"Công nhận {danhSach.Count} sáng kiến có tên trong danh sách kèm theo quyết định này."
            : quyetDinh.TrichYeu;

        var tep = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: tieuDeVanBan ?? string.Empty,
            tenDonVi: tenDonVi ?? string.Empty,
            tieuDe: $"QUYẾT ĐỊNH SỐ {quyetDinh.SoQuyetDinh}",
            phuDe: "Về việc công nhận sáng kiến",
            thongTin: new[]
            {
                new DongThongTin("Số quyết định", quyetDinh.SoQuyetDinh),
                new DongThongTin("Ngày ban hành", quyetDinh.NgayBanHanh.ToString("dd/MM/yyyy")),
                new DongThongTin("Cấp công nhận", quyetDinh.Loai),
                new DongThongTin("Số sáng kiến", danhSach.Count.ToString())
            },
            bang: new[] { bang },
            noiDungThem: noiDung,
            nguoiKy: quyetDinh.NguoiKy,
            chucVuNguoiKy: quyetDinh.ChucVuNguoiKy);

        return File(tep, "application/pdf", $"quyet-dinh-{quyetDinh.SoQuyetDinh.Replace('/', '-')}.pdf");
    }
}
